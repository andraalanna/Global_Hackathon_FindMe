# AI Lead Guidebook · FIND ME!

**Your mission:** make the AI fast, accurate, and impossible to crash. The AR lead (Andra) already built a first version of the Gemini layer; you now own it and make it demo-ready.

Your setup: **MacBook + iPhone**. Deadline: feature freeze **Thu 22:00**, demo **Fri 09:00**.

---

## 1. Setup (Mac, ~45 min, mostly downloads)
1. **Unity Hub** → install **Unity 6000.5.10f1** exactly (Apple silicon version if your Mac is M-series). Modules: none needed for Editor work. Add **iOS Build Support** only if you do Task 7.
2. **GitHub Desktop** → sign in → accept Andra's invite → **Clone** `Global_Hackathon_FindMe`.
3. Unity Hub → **Add project from disk** → open it. First open takes 5–15 min. If Unity asks to upgrade: **cancel**, you have the wrong version.
4. Put the Gemini key (Andra sends it privately) in:
   `Assets/_Team/AI/Secrets/Resources/gemini_key.txt` (just the key, one line). This folder is git-ignored. Check GitHub Desktop does **not** list it as a change.
5. **Google AI Studio** (aistudio.google.com) in the browser, logged in with the same Google account as the key.
6. **Claude Code** (VS Code extension or desktop app). Open the repo folder, start the session from the repo root.

## 2. What already exists
| File | What it does |
|---|---|
| `Assets/_Team/AR/Scripts/Gemini/IGeminiService.cs` | The contract: `GetItemProfile`, `FindSpots`, `CheckSpot` |
| `.../Gemini/GeminiClient.cs` | Real HTTP calls to Gemini |
| `.../Gemini/MockGeminiService.cs` | Fake answers for offline testing |
| `.../Gemini/GeminiServiceProvider.cs` | Chooses real or mock |
| `Assets/_Team/AR/Scripts/Models/*.cs` | `ItemProfile`, `HidingSpot`, `SpotCheckResult` |

**You own everything in `Scripts/Gemini/`.** Don't rename or remove fields in Models or methods in `IGeminiService` (the AR code depends on them). Adding an optional field is OK: tell Andra.

## 3. The three AI calls (the contract)
| Call | When | Model | Image | Returns |
|---|---|---|---|---|
| 1. Item profile | User types "black wallet" | gemini-2.5-flash | none | `{name, sizeCm:[w,h], shape, likelyPlaces[]}` |
| 2. Find spots | User taps Scan | gemini-2.5-flash | 768 px JPEG | up to 5 × `{label, box_2d:[ymin,xmin,ymax,xmax] 0–1000, likelihood 1–3, hint}` |
| 3. Check spot | User holds phone on a cue 2 s | gemini-2.5-flash-lite | 512 px JPEG | `{itemVisible, itemBox, spotFullyVisible, tip}` |

All calls: `responseMimeType: application/json` + response schema, `thinkingBudget: 0`, temperature ~0.2. Prompts are in `FINDME_BUILD_PLAN.md` Section 6.

**Latency targets on venue Wi-Fi:** Call 1 < 2 s, Call 2 < 3 s, Call 3 < 1.5 s.

---

## 4. Task checklist (in order)

### ☐ Task 1: Collect a test photo set (Wed night, 45 min)
Take portrait photos **in the actual demo room**, with the iPhone, then resize to the app's sizes (768 px wide for scan photos, 512 px for spot checks).
- 5 **room views** (desk area, bed area, door/bag area, shelf, messy mix)
- 10 **spot close-ups**: drawer closed, drawer open (empty), drawer open (wallet inside); clothes pile (covered), clothes lifted; bag closed, bag open with wallet; under bed dark; shelf with wallet; sofa cushion
- Save in `Docs/ai/test_photos/` (small JPEGs only, < 300 KB each).

**Done when:** 15 photos named clearly, e.g. `spot_drawer_open_wallet.jpg`.

### ☐ Task 2: Tune prompts in AI Studio (Thu morning, 2 h)
For each call, paste the prompt + schema into AI Studio, run every relevant photo, and record results in `Docs/ai/prompt_tests.md`:

| Photo | Expected | Got | OK? | Latency |
|---|---|---|---|---|

Focus on:
- **Call 2:** boxes on the right objects? Only real hiding places (not walls)? Max 5? Good `likelihood` order?
- **Call 3:** closed drawer → `spotFullyVisible:false` + tip "Open the drawer". Open drawer, no wallet → `true`. Wallet visible → `itemVisible:true` + box. **No false "found"** on black phones/cases.

Fixes that usually help: list exactly what counts as "fully visible"; say "If unsure, return false"; give 1 short example in the prompt; lower temperature to 0.1.

**Done when:** Call 3 is correct on ≥ 12 of 15 photos, with no false "found".

### ☐ Task 3: Put tuned prompts into `GeminiClient` (Thu, 1 h)
- Move prompt text into one place (e.g. `GeminiPrompts.cs` or a ScriptableObject in `Assets/_Team/AI/`), so tuning doesn't touch logic.
- Keep the JSON shapes exactly the same.

### ☐ Task 4: Speed (Thu, 1 h)
- Log per call: model, image KB, latency ms → visible in the debug overlay (ask Andra where it reads logs).
- Check images really are 768/512 px, JPEG ~70 (40–90 KB).
- `thinkingBudget: 0` on Flash; Flash-Lite for Call 3.
- Reuse one `HttpClient`/request settings; no extra round trips.

**Done when:** averages meet the latency targets on the venue Wi-Fi.

### ☐ Task 5: Reliability (Thu, 1–2 h)
- Timeouts: 8 s (Calls 1–2), 5 s (Call 3). One retry for Calls 1–2 on network errors only.
- Strip stray ```` ```json ```` fences before parsing.
- **Validate output:** clamp boxes to 0–1000, fix swapped min/max, drop boxes smaller than 2% of the image, cap at 5 spots, default `likelihood` to 1, trim long text.
- Return clear errors (e.g. `GeminiError.Timeout / Network / BadJson / NoKey`) so the AR code can show manual buttons. **Never throw into Unity's main loop uncaught.**
- Cancellation: honor the `CancellationToken` (AR cancels a check when a new scan starts).

**Done when:** airplane mode, a wrong key, and a bad JSON all end in a friendly fallback, never a freeze.

### ☐ Task 6: Mock = realistic offline demo (Thu, 1 h)
- Save 3–4 **real** responses from Task 2 as JSON in `Assets/_Team/AI/Resources/MockFixtures/`.
- `MockGeminiService` replays them (same order the demo uses): scan → spots; drawer check → "Open the drawer" then "checked"; bag check → wallet found.
- Add realistic delays (Call 2 ~1.5 s, Call 3 ~0.8 s).

**Done when:** with Wi-Fi off, the full demo story still plays end to end. This is our safety net if the venue Wi-Fi dies.

### ☐ Task 7 (optional, after Thu 12:00): Run the app on your iPhone
Only if Tasks 1–6 are done. Gives us a second test phone. **Do this on a Git branch `ios-test`**, because it changes shared settings.
1. Unity Hub → Manage 6000.5.10f1 → Add **iOS Build Support**. Install **Xcode** (App Store, large) if missing.
2. Ask Andra first, then in Package Manager add **Apple ARKit XR Plugin** (same version family as AR Foundation).
3. Project Settings → XR Plug-in Management → **iOS tab → ARKit** on.
4. Player → iOS: Bundle Identifier `com.aislaves.findme`, **Camera Usage Description** "FIND ME uses the camera to help you search your room", Requires ARKit support on, target iOS 15+.
5. File → Build Settings → iOS → Switch Platform → Build → open the Xcode project.
6. Xcode → Signing & Capabilities → Team = your Apple ID (free "Personal Team") → plug in iPhone → Run.
7. On iPhone: Settings → General → VPN & Device Management → trust your developer profile. (Free-account apps expire after 7 days, fine for the demo.)
8. Test the full search. Report differences vs Android to Andra. Merge the branch only if Andra agrees.

---

## 5. Timeline
| When | Do | Checkpoint |
|---|---|---|
| Wed night | Setup + Task 1 | Project opens, key works (mock off), photos collected |
| Thu 09:00–12:00 | Tasks 2–3 | **12:00:** tuned prompts merged, test table filled |
| Thu 13:30–17:30 | Tasks 4–6 | Latency + reliability + offline mock done |
| Thu 19:00–22:00 | Task 7 (optional), help testing | **22:00 freeze**, bug fixes only |
| Fri morning | Stand by for demo | |

## 6. Git rules
- Pull before you start, commit + push every hour ("AI: tuned Call 3 prompt").
- Only edit `Scripts/Gemini/`, `Assets/_Team/AI/`, `Docs/ai/`.
- Never commit the key. Never touch the Main scene.
- Conflict on anything outside your folders → stop, ask Andra.

## 7. Prompts for Claude Code
**Start of session:**
```
Read CLAUDE.md, Assets/_Team/AI/CLAUDE.md and Docs/AI_LEAD_GUIDE.md.
I'm the AI lead. Summarize the current Gemini code in Assets/_Team/AR/Scripts/Gemini/
(how each call is built, parsed, and how errors are handled) and list gaps vs the guide.
Don't change anything yet.
```
**Task 3:**
```
Move all prompt text and schemas into one file in Assets/_Team/AI/ (GeminiPrompts.cs).
Replace them with these tuned prompts: [paste]. Keep JSON shapes and IGeminiService unchanged.
Explain the key lines and give me an Editor test.
```
**Task 4–5:**
```
Do Tasks 4 and 5 from Docs/AI_LEAD_GUIDE.md: latency logging, image size check,
timeouts, one retry for Calls 1–2, JSON fence stripping, output validation and clear
error types. Never throw uncaught. Show me how to test airplane mode, bad key, bad JSON.
```
**Task 6:**
```
Make MockGeminiService replay the JSON fixtures in Assets/_Team/AI/Resources/MockFixtures/
in the demo order described in Task 6, with realistic delays.
```
