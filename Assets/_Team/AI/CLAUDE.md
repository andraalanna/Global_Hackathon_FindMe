# AI lead rules (for Claude Code)

You are helping the **AI lead** of FIND ME. Read `Docs/AI_LEAD_GUIDE.md` first; it has the task list.

## You may edit
- `Assets/_Team/AR/Scripts/Gemini/**` (GeminiClient, MockGeminiService, GeminiServiceProvider, IGeminiService)
- `Assets/_Team/AI/**` (prompt tests, mock fixtures, configs)
- `Docs/ai/**` (test notes)

## You must not edit
- Anything else under `Assets/_Team/AR/`, `Assets/_Team/UI/`, the Main scene, `ProjectSettings/`, `Packages/` (unless the AI lead confirms the AR lead agreed, e.g. the iOS test branch).

## Contract you must keep
- `IGeminiService` method names and parameters stay the same.
- Model classes in `Assets/_Team/AR/Scripts/Models/` (ItemProfile, HidingSpot, SpotCheckResult): **don't rename or remove fields**. Adding an optional field is allowed; mention it in your summary.
- `box_2d` / `itemBox` / `spotBox` format: `[ymin, xmin, ymax, xmax]`, integers 0–1000, origin top-left.
- Models live in `Assets/_Team/AR/Settings/GeminiConfig.asset`, not in code. **`gemini-2.5-*` returns 404 for our key**; we use `gemini-3.5-flash-lite` (profile, check, fallback) and `gemini-3.8-flash` (scan). Gemini 3.x rejects `thinkingBudget`: use `thinkingLevel` (`minimal` for lite, `low` for flash). All calls: `responseMimeType: application/json`, a response schema with `propertyOrdering`, temperature ~0.2.
- Prompts are text files in `Assets/_Team/AI/Prompts/Resources/FindMePrompts/` (+ knowledge base `lost_items.json`). Edit those, not C# strings.
- Before and after changing a prompt, schema or `GeminiClient` logic: run `python Docs/ai/eval/run_eval.py` (mirror any logic change in that script). Read `Docs/ai/HOW_THE_AI_WORKS.md` first.
- Optional fields added by the AR lead (Oct 2026): `ItemProfile.category/appearance/ambiguous/referenceJpg`, `HidingSpot.seen/isItem`, `SpotCheckResult.seen/itemEvidence/closed/insideVisible/mightBeHidden/spotBox`, and `IGeminiService.GetItemProfileFromPhoto`.
- Never block the main thread. One retry max for Calls 1–2, none for Call 3. Timeouts 8 s / 5 s.
- Key: `Resources.Load<TextAsset>("gemini_key")` from `Assets/_Team/AI/Secrets/Resources/gemini_key.txt`. Missing key → mock.

## Style
Small, commented code. After each task, explain the key lines and give Editor test steps.
