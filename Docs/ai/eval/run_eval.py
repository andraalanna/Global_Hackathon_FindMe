"""
FIND ME AI eval: runs the app's REAL prompts on real photos against real Gemini and grades the answers.

Mirrors the C# side exactly:
  prompts      Assets/_Team/AI/Prompts/Resources/FindMePrompts/{profile,spots,check}.txt
  knowledge    .../FindMePrompts/lost_items.json         (LostItemKnowledge.cs)
  post-process impossible-spot filter + "checked needs proof" (GeminiClient.ParseSpots / ParseCheck)
  models       same choices + fallback as GeminiConfig.cs
If you change the C# logic, change it here too (it's small on purpose).

Usage (from the repo root):
  python Docs/ai/eval/run_eval.py                 # all cases once
  python Docs/ai/eval/run_eval.py --runs 3        # each case 3x (consistency)
  python Docs/ai/eval/run_eval.py --only C0       # case ids starting with C0
  python Docs/ai/eval/run_eval.py --check-model gemini-3.8-flash
Needs: Assets/_Team/AI/Secrets/Resources/gemini_key.txt, photos in Docs/ai/eval/images/ (fetch_images.py).
"""
import argparse, base64, json, os, re, sys, time, urllib.error, urllib.request

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
PROMPTS = os.path.join(ROOT, "Assets", "_Team", "AI", "Prompts", "Resources", "FindMePrompts")
IMAGES = os.path.join(os.path.dirname(__file__), "images")
KEY = open(os.path.join(ROOT, "Assets", "_Team", "AI", "Secrets", "Resources", "gemini_key.txt")).read().strip()
KB = json.load(open(os.path.join(PROMPTS, "lost_items.json"), encoding="utf-8"))

MINIMAL, LOW = {"thinkingLevel": "minimal"}, {"thinkingLevel": "low"}
MODELS = {"profile": ("gemini-3.5-flash-lite", MINIMAL), "spots": ("gemini-3.8-flash", LOW),
          "check": ("gemini-3.5-flash-lite", MINIMAL), "fallback": ("gemini-3.5-flash-lite", MINIMAL)}

# ---------- knowledge (mirror of LostItemKnowledge.cs) ----------
def normalize(s): return " ".join(re.sub(r"[^0-9a-z]", " ", s.lower()).split())

def contains_words(text, phrase):
    if not text or not phrase or not text.strip() or not phrase.strip(): return False
    t, p = " " + normalize(text) + " ", normalize(phrase)
    return (" " + p + " ") in t or (" " + p + "s ") in t or (" " + p + "es ") in t

def match(text, category=None):
    for i in KB["items"]:
        if category and i["id"] == category: return i
    best, best_len = None, 0
    for i in KB["items"]:
        for n in i["names"]:
            if len(n) > best_len and contains_words(text, n): best, best_len = i, len(n)
    return best

def max_size(i): return max(i["sizeCm"]) if i else 10.0
CONTAINERS = ["case", "bag", "backpack", "box", "pouch", "pocket", "drawer", "basket", "tray", "dish", "bowl", "jar",
              "cabinet", "wallet", "purse", "folder", "bin", "hamper", "closet", "wardrobe", "shoe", "boot"]

def list_has(lst, label): return any(contains_words(p, label) for p in (lst or []))

def impossible_place(label, item, item_max):
    if not label or not label.strip(): return True
    if item and (list_has(item["typical"], label) or list_has(item["fitsIn"], label)): return False
    if any(contains_words(label, w) for w in KB["neverPlaces"]): return True
    if item and any(contains_words(label, w) for w in item.get("never") or []): return True
    if not any(contains_words(label, w) for w in CONTAINERS):
        for o in KB["items"]:
            if o is item or max(o["sizeCm"]) >= item_max * 2: continue
            if any(contains_words(label, n) for n in o["names"]): return True
    return False

def is_container(s): return any(contains_words(s, w) for w in CONTAINERS)
def is_the_item(label, name): return contains_words(label, name) and (not is_container(label) or is_container(name))

def spots_block(name, i):
    if not i: return ""
    s = f"What we know about a {name}:\n- It usually ends up: {', '.join(i['typical'])}.\n- It fits in: {', '.join(i['fitsIn'])}.\n"
    if i.get("never"): s += f"- Never a place for it: {', '.join(i['never'])}.\n"
    if i.get("inCase"):
        s += (f"- A {name} is often kept in its {i['inCase']}. If you see a {i['inCase']}, list it as a place "
              f"(likelihood 3, hint \"Open the case\"). The {i['inCase']} itself is not the {name}.\n")
    return s + "Places where it usually ends up get likelihood 3; other places that fit get 1-2.\n"

def check_block(spot, i):
    if not i or not i.get("inCase") or not contains_words(spot, "case"): return ""
    return (f"- This spot is a {i['inCase']}. It is only checked when it is OPEN and you can see inside it. "
            "A closed case means closed = true and insideVisible = false.\n")

def fill(name, **kw):
    s = open(os.path.join(PROMPTS, name + ".txt"), encoding="utf-8").read()
    for k, v in kw.items(): s = s.replace("{" + k + "}", v or "")
    return s

# ---------- schemas (mirror of GeminiClient.cs) ----------
def S(): return {"type": "STRING"}
def N(): return {"type": "NUMBER"}
def I(): return {"type": "INTEGER"}
def B(): return {"type": "BOOLEAN"}
def A(x): return {"type": "ARRAY", "items": x}
def O(*props):
    req = [k for k, v in props if not v.get("nullable")]
    return {"type": "OBJECT", "properties": dict(props), "required": req, "propertyOrdering": [k for k, _ in props]}
def nullable(x): x = dict(x); x["nullable"] = True; return x

# ---------- HTTP with the app's fallback rule ----------
def generate(call, prompt, schema, jpg=None, model_override=None):
    model, thinking = MODELS[call]
    if model_override: model, thinking = model_override, (MINIMAL if "lite" in model_override else LOW)
    # Same order as GeminiClient.Generate: [reference caption, reference,] [image,] prompt text.
    parts = []
    if jpg: parts.append({"inline_data": {"mime_type": "image/jpeg", "data": base64.b64encode(jpg).decode()}})
    parts.append({"text": prompt})
    for attempt, (m, th) in enumerate([(model, thinking), MODELS["fallback"]]):
        body = {"contents": [{"role": "user", "parts": parts}], "generationConfig": {
            "temperature": 0.2, "responseMimeType": "application/json", "responseSchema": schema, "thinkingConfig": th}}
        req = urllib.request.Request(f"https://generativelanguage.googleapis.com/v1beta/models/{m}:generateContent",
                                     data=json.dumps(body).encode(), headers={"x-goog-api-key": KEY, "Content-Type": "application/json"})
        t = time.time()
        try:
            with urllib.request.urlopen(req, timeout=20) as r:
                d = json.load(r)
            text = "".join(p.get("text", "") for p in d["candidates"][0]["content"]["parts"] if not p.get("thought"))
            return m, json.loads(text.strip().strip("`").removeprefix("json")), time.time() - t
        except urllib.error.HTTPError as e:
            if e.code == 429 and attempt == 1: time.sleep(20)
            if not (e.code == 429 or e.code >= 500) or attempt == 1: raise
        except (TimeoutError, OSError):
            if attempt == 1: raise

# ---------- the three calls (mirror of GeminiClient.cs) ----------
def profile(text):
    prompt = fill("profile", userText=text, categories=", ".join(i["id"] for i in KB["items"]))
    schema = O(("name", S()), ("category", S()), ("sizeCm", A(N())), ("shape", S()), ("likelyPlaces", A(S())))
    m, p, dt = generate("profile", prompt, schema)
    info = match(text, p.get("category")) or match(p.get("name", ""))
    if info: p.update(category=info["id"], sizeCm=info["sizeCm"], shape=info["shape"], likelyPlaces=info["typical"])
    else: p["category"] = "other"
    return m, p, dt

def profile_photo(jpg, text):
    prompt = fill("profile_photo", userText=text or "(nothing)", categories=", ".join(i["id"] for i in KB["items"]))
    schema = O(("objectsSeen", A(S())), ("name", S()), ("category", S()), ("appearance", S()), ("sizeCm", A(N())), ("shape", S()), ("likelyPlaces", A(S())))
    m, p, dt = generate("spots", prompt, schema, jpg)
    not_separate = ["case", "surface", "table", "desk", "floor", "wall", "shelf", "counter", "background", "hand"]
    real = [o for o in p.get("objectsSeen") or [] if not any(contains_words(o, w) for w in not_separate)]
    p["ambiguous"] = len(real) >= 2 and not (text or "").strip()
    info = match(p.get("name", "") or text, p.get("category"))
    if info: p.update(category=info["id"], sizeCm=info["sizeCm"], shape=info["shape"], likelyPlaces=info["typical"])
    else: p["category"] = "other"
    return m, p, dt

def item_profile(name):
    i = match(name)
    return {"name": name, "category": i["id"] if i else "other", "sizeCm": i["sizeCm"] if i else [10, 10],
            "shape": i["shape"] if i else "small object"}

def describe(p): return f"{p['shape']}, about {p['sizeCm'][0]:.0f} x {p['sizeCm'][1]:.0f} cm"

def evidence_weak(ev, p):
    if not ev or len(ev.strip()) < 12 or ev.strip().lower() == "none": return True
    return contains_words(describe(p), ev) or contains_words(p.get("shape") or "~", ev)

def spots(jpg, name):
    p = item_profile(name); info = match(name, p["category"])
    prompt = fill("spots", itemName=name, itemDescription=describe(p), size=f"{p['sizeCm'][0]:.0f} x {p['sizeCm'][1]:.0f} cm",
                  lookalike=info["lookalike"] if info else f"the actual {name}, not something that only looks similar",
                  knowledge=spots_block(name, info))
    schema = O(("itemEvidence", S()), ("itemVisible", B()), ("itemConfidence", I()), ("itemBox", nullable(A(I()))),
               ("spots", A(O(("seen", S()), ("label", S()), ("box_2d", A(I())), ("likelihood", I()), ("hint", S())))))
    m, r, dt = generate("spots", prompt, schema, jpg)
    if r.get("itemVisible") and evidence_weak(r.get("itemEvidence"), p): r["itemVisible"], r["weakEvidence"] = False, True
    # Mirror ParseSpots: only "certain" (3) is a sighting; "probably" (2) becomes a top spot "maybe <item>".
    if r.get("itemVisible") and (r.get("itemConfidence") or 3) < 3:
        if r.get("itemConfidence") == 2:
            r.setdefault("spots", []).insert(0, {"label": "maybe " + name, "box_2d": r.get("itemBox"), "likelihood": 3})
        r["itemVisible"] = False
    kept, dropped = [], []
    for s in r.get("spots") or []:
        (dropped if impossible_place(s.get("label", ""), info, max(p["sizeCm"])) or is_the_item(s.get("label", ""), name) else kept).append(s)
    r["spots"], r["dropped"] = kept, dropped
    return m, r, dt

def check(jpg, name, spot, model_override=None):
    p = item_profile(name); info = match(name, p["category"])
    prompt = fill("check", itemName=name, itemDescription=describe(p), spotLabel=spot,
                  lookalike=info["lookalike"] if info else f"the actual {name}", checkKnowledge=check_block(spot, info), note="")
    schema = O(("seen", S()), ("itemEvidence", S()), ("itemVisible", B()), ("itemBox", nullable(A(I()))), ("closed", B()),
               ("insideVisible", B()), ("mightBeHidden", B()), ("spotBox", nullable(A(I()))), ("tip", S()))
    m, r, dt = generate("check", prompt, schema, jpg, model_override)
    if r.get("itemVisible") and evidence_weak(r.get("itemEvidence") or r.get("seen"), p): r["itemVisible"], r["weakEvidence"] = False, True
    r["fullyVisible"] = bool(r.get("insideVisible")) and not r.get("closed") and not r.get("itemVisible") and not r.get("mightBeHidden")
    return m, r, dt

# ---------- grading ----------
def grade(case, r):
    e, fails = case["expect"], []
    if case["call"] == "profile_photo":
        if "category" in e and r.get("category") != e["category"]: fails.append(f"category {r.get('category')} != {e['category']}")
        if "ambiguous" in e and bool(r.get("ambiguous")) != e["ambiguous"]: fails.append(f"ambiguous {r.get('ambiguous')} != {e['ambiguous']}")
        for w in e.get("nameAny", []) and [e["nameAny"]]:
            if not any(x in (r.get("name") or "").lower() for x in w): fails.append(f"name '{r.get('name')}' lacks any of {w}")
        if not (r.get("appearance") or "").strip(): fails.append("no appearance")
        if e.get("appearanceAny") and not any(w in (r.get("appearance") or "").lower() for w in e["appearanceAny"]):
            fails.append(f"appearance '{r.get('appearance')}' lacks any of {e['appearanceAny']}")
        return fails
    if case["call"] == "profile":
        if "category" in e and r.get("category") != e["category"]: fails.append(f"category {r.get('category')} != {e['category']}")
        for w in e.get("nameAll", []):
            if w not in (r.get("name") or "").lower(): fails.append(f"name '{r.get('name')}' lacks '{w}'")
        if "maxSizeCm" in e and max(r.get("sizeCm") or [99]) > e["maxSizeCm"]: fails.append(f"size {r.get('sizeCm')} too big")
        return fails
    if "itemVisible" in e and bool(r.get("itemVisible")) != e["itemVisible"]:
        fails.append(f"itemVisible {r.get('itemVisible')} (evidence: {r.get('itemEvidence') or r.get('seen')})")
    if case["call"] == "spots":
        labels = [s.get("label", "") for s in r.get("spots", [])]
        for w in e.get("noSpotWords", []):
            bad = [l for l in labels if contains_words(l, w)]
            if bad: fails.append(f"bad spot {bad} (word '{w}')")
        if e.get("spotAny") and not any(contains_words(l, w) for l in labels for w in e["spotAny"]):
            fails.append(f"none of {e['spotAny']} in {labels}")
        if "maxSpots" in e and len(labels) > e["maxSpots"]: fails.append(f"{len(labels)} spots > {e['maxSpots']}: {labels}")
    if case["call"] == "check":
        if "fullyVisible" in e and r["fullyVisible"] != e["fullyVisible"]:
            fails.append(f"checked={r['fullyVisible']} (closed={r.get('closed')}, inside={r.get('insideVisible')}, hidden={r.get('mightBeHidden')}, seen='{r.get('seen')}')")
        if e.get("tipAny") and not any(w in (r.get("tip") or "").lower() for w in e["tipAny"]):
            fails.append(f"tip '{r.get('tip')}' lacks any of {e['tipAny']}")
    return fails

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--runs", type=int, default=1)
    ap.add_argument("--only", default="")
    ap.add_argument("--check-model", default=None)
    args = ap.parse_args()
    cases = [c for c in json.load(open(os.path.join(os.path.dirname(__file__), "cases.json"), encoding="utf-8"))["cases"]
             if c["id"].startswith(args.only)]
    total = passed = 0
    for c in cases:
        for run in range(args.runs):
            total += 1
            try:
                if c["call"] == "profile": m, r, dt = profile(c["text"])
                elif c["call"] == "profile_photo":
                    m, r, dt = profile_photo(open(os.path.join(IMAGES, c["image"]), "rb").read(), c.get("text", ""))
                else:
                    jpg = open(os.path.join(IMAGES, c["image"]), "rb").read()
                    m, r, dt = spots(jpg, c["item"]) if c["call"] == "spots" else check(jpg, c["item"], c["spot"], args.check_model)
                fails = grade(c, r)
            except Exception as ex:
                m, r, dt, fails = "-", {}, 0, [f"ERROR {ex}"]
            passed += not fails
            extra = ""
            if c["call"] == "spots":
                extra = f" spots={[s.get('label') for s in r.get('spots', [])]}" + (f" dropped={[s.get('label') for s in r.get('dropped', [])]}" if r.get("dropped") else "")
            elif c["call"] == "profile_photo":
                extra = f" objects={r.get('objectsSeen')} name='{r.get('name')}' appearance='{r.get('appearance')}'"
            elif c["call"] == "check":
                extra = f" seen='{r.get('seen')}' tip='{r.get('tip')}'"
            print(f"{'PASS' if not fails else 'FAIL'} {c['id']} [{m}, {dt:.1f}s]{extra}")
            for f in fails: print(f"     - {f}")
            time.sleep(1.5)   # stay under the free-tier rate limit
    print(f"\n{passed}/{total} passed")
    sys.exit(0 if passed == total else 1)

if __name__ == "__main__":
    main()
