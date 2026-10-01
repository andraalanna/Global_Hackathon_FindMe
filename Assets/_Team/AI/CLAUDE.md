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
- `box_2d` / `itemBox` format: `[ymin, xmin, ymax, xmax]`, integers 0–1000, origin top-left.
- Calls 1–2 use `gemini-2.5-flash`, Call 3 uses `gemini-2.5-flash-lite`, all with `responseMimeType: application/json`, a response schema, `thinkingBudget: 0`, temperature ~0.2.
- Never block the main thread. One retry max for Calls 1–2, none for Call 3. Timeouts 8 s / 5 s.
- Key: `Resources.Load<TextAsset>("gemini_key")` from `Assets/_Team/AI/Secrets/Resources/gemini_key.txt`. Missing key → mock.

## Style
Small, commented code. After each task, explain the key lines and give Editor test steps.
