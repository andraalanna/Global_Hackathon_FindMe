# UI lead rules (for Claude Code)

You are helping the **UI/UX lead** of FIND ME. Read `Docs/UI_LEAD_GUIDE.md` and `Docs/design/DESIGN_SPEC.md` first.
The visual source of truth is `Docs/design/screens/*.png` (look at the images) and `Docs/design/html/*.dc.html` (exact colors, sizes, copy).

## You may edit
- `Assets/_Team/UI/**` only (scripts, prefabs, art, a `UI_Test` scene).

## You must not edit
- The Main scene, `Assets/_Team/AR/**`, `Assets/_Team/AI/**`, `ProjectSettings/`, `Packages/`.
- `Assets/_Team/AR/Scripts/ISearchUI.cs` is a shared contract: **implement it, don't change it.** If a method is missing, write it in your summary for the AR lead.

## How the UI connects
- Build `UIManager : MonoBehaviour, ISearchUI` in `Assets/_Team/UI/Scripts/`.
- All screens live inside one prefab `Assets/_Team/UI/Prefabs/UIRoot.prefab`. The AR lead drops it into Main and replaces `DevUI`.
- Buttons call the AR lead's `SearchController` public methods (StartSearch, ScanArea, MarkChecked, MarkFound, UndoChecked, ConfirmMatch, StopSearch). Check the real names in `SearchController.cs` before wiring.
- Test in your own scene `Assets/_Team/UI/Scenes/UI_Test.unity` with a `FakeSearchDriver` that calls your UIManager methods with fake data.

## Unity UI rules
- Canvas Scaler: Scale With Screen Size, reference 1080×2400, match 0.5. Wrap content in a SafeArea panel.
- TextMeshPro only. Font assets from `Art/Fonts` (Fredoka for headings, Nunito for body).
- Colors only from a `UITheme` ScriptableObject (values in DESIGN_SPEC.md).
- Rounded shapes: 9-sliced sprites in `Art/Sprites`. No blur, no glassmorphism, no gradients.
- Animations with a small coroutine tween helper (no new packages). Respect a "reduce motion" flag.
- Touch targets ≥ 56 px tall, text ≥ 32 px at 1080 width (≈16 sp).

## Style
Small, commented scripts. After each task: changed files, how to test in `UI_Test`, screenshots description, known issues.

## Design device: iPhone 13 (overrides the 1080×2400 numbers above)
- Design frames are 390×844 pt = iPhone 13 logical size. Canvas Scaler reference resolution is **1170×2532** (exactly 3×), Scale With Screen Size, match 0.5. 1 design px = 3 Unity px (DESIGN_SPEC's 2.77× becomes 3×).
- Game view test resolution: 1170×2532.
- iPhone 13 safe area: top 47 pt (notch), bottom 34 pt (home indicator). Always use the SafeArea panel; never hard-code these.
- The real test phone (Galaxy A80) is taller (~20:9): anchor to top/bottom, let the middle stretch, keep the middle 60% clear for AR cues.
