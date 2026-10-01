# How the FIND ME AI works (and how we made it smarter)

FIND ME asks Gemini three questions. The phone does the 3D work; Gemini only looks at photos and answers in strict JSON.

```
You type "glasses"  ─or─  show a photo of them
        │
        ▼
 ① PROFILE   "What is it, how big, what does it look like, where do these usually end up?"
        │
 you tap Scan → photo of the room
        ▼
 ② SPOTS     "Is the item visible right here? If not, where in THIS photo could it hide?"
        │      → item seen?   →  "There it is! Is that it?"
        │      → spots        →  3D cues on the furniture
 you hold the phone on a cue (1.2 s) or tap Check here → close-up photo
        ▼
 ③ CHECK     "Is it here? Is the spot open? Could it still be hidden?"
               → found / checked ✓ / "open it" / "look closer"
```

---

## 1. What we send

Every call is one HTTPS request to `generativelanguage.googleapis.com/v1beta/models/<model>:generateContent`:

| Part | What | Why |
|---|---|---|
| Reference photo (optional) | 384 px photo you showed with **Use photo**, captioned "Reference photo of the lost item" | Gemini compares against *your* item, not a generic one |
| Photo | Room photo (scan, 768 px) or close-up (check, 512 px). Upright even if you held the phone sideways | What we ask about |
| Prompt | A text file from `Assets/_Team/AI/Prompts/Resources/FindMePrompts/` with `{placeholders}` filled in | Instructions + rules |
| Knowledge | Item-specific facts from `lost_items.json` (see §4) pasted into the prompt | Common sense the model lacks |
| `responseSchema` | The exact JSON fields we want, **in order** (`propertyOrdering`) | We can always parse the answer; field order forces "look first, decide second" |
| `thinkingConfig` | `thinkingLevel: minimal/low` | Least thinking = fastest |
| `temperature` | 0.2 | Consistent answers |

**Models** (in `Assets/_Team/AR/Settings/GeminiConfig.asset`): profile and check use `gemini-3.5-flash-lite` (fast). Scans use `gemini-3.8-flash`; when it's overloaded (often, on the free tier) the app retries once on flash-lite. `gemini-2.5-*` (in the original plan) returns 404 for new API keys.

## 2. What Gemini answers (real examples from our test set)

**① Profile** (`profile.txt`, or `profile_photo.txt` with a photo)
```json
{ "objectsSeen": ["eyeglasses", "glasses case"],
  "name": "teal glasses", "category": "glasses",
  "appearance": "teal rectangular plastic frames, thin temples, clear lenses",
  "sizeCm": [14, 5], "shape": "folded glasses frame",
  "likelyPlaces": ["glasses case", "nightstand", "desk"] }
```

**② Spots** (`spots.txt`)
```json
{ "itemEvidence": "none", "itemVisible": false, "itemConfidence": 1, "itemBox": null,
  "spots": [
    { "seen": "closed patterned glasses case", "label": "glasses case",
      "box_2d": [120, 40, 860, 960], "likelihood": 3, "hint": "Open the case" } ] }
```
Boxes are `[ymin, xmin, ymax, xmax]`, 0–1000, origin top-left of the photo.

**③ Check** (`check.txt`)
```json
{ "seen": "closed patterned glasses case on a white surface", "itemEvidence": "none",
  "itemVisible": false, "itemBox": null, "closed": true, "insideVisible": false,
  "mightBeHidden": false, "spotBox": [80, 30, 900, 970], "tip": "Open the case" }
```

## 3. How the app processes the answers

**Profile** (`GeminiClient.ApplyKnowledge`): if the item is in the knowledge base, its size, shape and typical places come from there (reliable); the name keeps your words ("pink wallet"). Photo with several things and no words ("backpack + pineapple") → the app asks which one instead of guessing.

**Spots** (`GeminiClient.ParseSpots`, then `SearchController`):
1. *Item seen?* Only counts if the evidence points at something (colour + place), not our own description echoed back. Confidence 3 → **"There it is!"** straight away. Confidence 2 → a top-priority cue "maybe glasses / Look closely here". 1 → ignored.
2. *Impossible spots are removed on the phone* (`LostItemKnowledge.IsImpossiblePlace`): food/packaging/trash/screens, places listed as nonsense for this item, and other small objects (a chapstick can't hide glasses; glasses can't hide keys). Containers (case, bag, drawer…) always pass.
3. Broken boxes dropped, sorted by likelihood, max 5.
4. Each box → 3D: at photo time the phone measured a 10 × 20 grid of distances (ARCore depth / planes / feature points). The grid cells inside the box are clustered; the biggest cluster is the object's surface → cue goes there. Works looking down, up, at walls or tables.

**Check** (`GeminiClient.ParseCheck`, then `SearchController.Spots`):

| Gemini says | Cue becomes | You see |
|---|---|---|
| item visible (+ real evidence) | Candidate | "Is this your glasses?" |
| closed | Needs opening | Gemini's tip, e.g. "Open the case" |
| inside visible, but piled/dark/similar-looking things | Needs opening | "Move the clothes around" (look closer) |
| inside visible, nothing like the item | **Checked ✓** automatically | "Glasses case checked" + Undo |

"Checked" needs proof: `insideVisible && !closed && !mightBeHidden && !itemVisible`. A closed glasses case can't pass even if the model is careless with one field. The close-up also returns `spotBox`, so the cue is re-measured from up close.

## 4. How we made it smarter

Training a model wasn't practical (thousands of labelled photos, and Gemini 3.x Flash can't be fine-tuned this way). Instead, five layers:

1. **Knowledge base** — `lost_items.json`: 32 commonly lost items (glasses, keys, wallet, AirPods, remote, ring, passport, chapstick…), each with size, shape, where it usually ends up, what it fits in, what never makes sense, what it can be confused with, and whether it lives in its own case. Plus a global "never a place" list (snack bags, cups, trash, plants, screens…).
2. **Strict rules in the prompts** — a place must be really in the photo, physically fit the item, be somewhere a person would leave it, and never be food/packaging/trash. An empty answer is allowed.
3. **Grounding** — the schema makes Gemini *describe what it sees first* (`seen`, `itemEvidence`, `objectsSeen`) before deciding. Models decide better after describing.
4. **Code guards on the phone** — the app re-checks Gemini's answer (impossible places, weak evidence, closed ≠ checked, confidence levels, ambiguity). If the model slips, the user still never sees it.
5. **A test set we iterate against** — `Docs/ai/eval/`: 45 cases on real photos, run against real Gemini with the *same* prompt files and the same post-processing as the app.

### Bugs the test set found (and fixes)

| Found | Fix |
|---|---|
| A closed glasses case was marked checked (your bug) | `closed` + `insideVisible` fields; "checked" needs both; case-specific rule |
| Snack bag / chapstick suggested for glasses (your bug) | Knowledge base + global never-list + size rule, enforced on the phone |
| Shelf/cabinet invented on a bare wall | "Never invent furniture", empty list allowed, `seen` first |
| Glasses suggested as a hiding place for keys | Other small objects are never places unless 2× bigger |
| Its own case dropped as "the item itself" | Containers of the item are places |
| Basket of dark clothes marked "not here" | `mightBeHidden`: piles, darkness, same colour → "look closer" |
| Glasses "seen" in a bedroom with none (evidence = our own words) | Evidence must give colour + place; echoes are rejected |
| Same, with plausible-sounding evidence | Confidence: only "certain" triggers "There it is!" |
| Photo of backpack + pineapple, no words → searched for the pineapple | `objectsSeen` first; 2+ objects and no words → ask which |

### Results

| Run (each case twice) | Passed | Wrong answers | Errors |
|---|---|---|---|
| First version of the new prompts | 37 / 38 (once) | 1 (my own filter dropped "glasses case") | 0 |
| After fixes, full set | **88 / 90** | **0** | 2 × rate limit (429) |
| Final run | 55 / 90 | 1 (S21: "radio" / "tape dispenser" for keys, now blocked on the phone) | 34 × 429: **daily free quota used up** |

Median latency: **~2.0–2.2 s per call** on `gemini-3.5-flash-lite` (worst ~15 s). `gemini-3.8-flash` was overloaded (503) about half the time, so the app's fallback did most of the work.
The free tier allows a limited number of calls per day; a full eval run (~90 calls) plus phone testing can use it up. Run the eval with `--only` while iterating, or use a paid key.

## 5. Tuning it yourself

1. Edit a prompt (`FindMePrompts/*.txt`) or the knowledge base (`lost_items.json`). No code needed.
2. Run `python Docs/ai/eval/run_eval.py` (add `--runs 3` to check consistency, `--only C0` for some cases).
3. New edge case? Put a photo in `Docs/ai/eval/images/` and add a line to `cases.json`.
4. Changing logic in `GeminiClient.cs` or `LostItemKnowledge.cs`? Mirror it in `run_eval.py` (it's kept small on purpose).

## Files

| File | Role |
|---|---|
| `Assets/_Team/AI/Prompts/Resources/FindMePrompts/*.txt` | The prompts |
| `Assets/_Team/AI/Prompts/Resources/FindMePrompts/lost_items.json` | Knowledge base |
| `Assets/_Team/AR/Scripts/Gemini/GeminiClient.cs` | HTTP, schemas, parsing, guards |
| `Assets/_Team/AR/Scripts/Gemini/LostItemKnowledge.cs` | Knowledge base + impossible-place filter |
| `Assets/_Team/AR/Scripts/Gemini/PromptLibrary.cs` | Loads/fills prompt files |
| `Assets/_Team/AR/Scripts/SearchController*.cs` | What each answer does to cues and screens |
| `Docs/ai/eval/` | Test set, runner, latest results |
