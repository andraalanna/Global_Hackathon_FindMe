# UI handoff for the AR lead (Andra)

The whole UI is one prefab: `Assets/_Team/UI/Prefabs/UIRoot.prefab`. It replaces `DevUI`.
Nothing in the AR folder or the Main scene was changed by the UI side.

## Wiring it into Main (about 2 minutes)
1. Drag `UIRoot.prefab` into the Main scene (root level is fine).
2. Select the **SearchController** object and drag the `UIRoot` object into its **Ui** field
   (the field accepts any component that implements `ISearchUI`; `UIManager` is on the `UIRoot` root).
3. **Disable the `DevUI` GameObject.** `SearchController.FindSearchUI()` takes the first `ISearchUI` it finds, so two
   of them is ambiguous, and DevUI's IMGUI would also draw on top of the real screens.
4. Make sure the scene has one **EventSystem with an `InputSystemUIInputModule`** (the project uses the new Input
   System only). If the XR template's EventSystem uses the XR UI module, UI buttons still work, but check a tap on
   the phone. UI_Test shows the expected setup.
5. `UIManager.search` can stay empty: it finds the SearchController on its own.

## What the UI expects from SearchController (all already true today)
| UI action | SearchController call |
|---|---|
| Continue (input screen) | `StartSearch(text)` |
| Back / Stop / Cancel / Back home | `StopSearch()` |
| Scan this area / Scan another area | `ScanArea()` |
| I found it / Found it | `MarkFound()` |
| Checked (manual) | `MarkChecked()` |
| Undo (toast) | `UndoChecked()` |
| Yes / Not it (match sheet) | `ConfirmMatch(bool)` |

The UI decides which screen to show from the `ISearchUI` calls only, it never reads `SearchState`:
- `ShowStatus("Getting to know ...")` shows the analyzing screen, `ShowItemCard` the item card.
- After the user taps **Start scanning**: `SetReadiness` fills the card; the first `SetScanEnabled(true)` switches to the HUD.
- `SetProgress(done, total)` with `total > 0` turns the main button into **I found it**.
- `ShowStatus("All spots checked. Scan another area?")` (exact text) opens the "all checked" sheet.
- `ShowFound` then `ShowSummary` (called back to back) show the found screen; the summary opens on **Nice, done**.

## Things to know
- `ShowStatus` messages show in a dark line above the bottom buttons. Keep them short.
- The camera must stay visible: HUD and sheets have no dim layer. The middle 60 % is empty on purpose.
- Test without the phone: open `Assets/_Team/UI/Scenes/UI_Test.unity`, press Play. `FakeSearchDriver` plays a fake search.
  Keys after the first scan: C = check next spot, U = undo, P = spot panel demo, H = hide it, M = match sheet.
- Design reference is iPhone 13 (390x844 pt, Canvas 1170x2532). The Galaxy A80 is taller; layouts are anchored to the
  top and bottom and the middle stretches.

## Known gaps
- The calm (breathing) button on the HUD does nothing yet (optional screen).
- The microphone button on the first screen is decorative.
- Photo input ("Take a photo / Upload a photo") from the design is not built (the app has no photo search).
- No sounds or haptics yet: `FeedbackFx` event names are needed from the AR side.
