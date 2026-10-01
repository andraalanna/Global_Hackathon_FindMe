# UI/UX Lead Guidebook · FIND ME!

**Your mission:** turn the design prototype into real Unity screens that feel friendly, clear, and a bit magical, while the AR lead (Andra) handles the camera, 3D cues, and search logic.

Your setup: laptop + one of our **Android phones** for testing. Deadline: screens ready to merge **Thu 12:00**, polish until **Thu 22:00** freeze.

---

## 1. Setup (~45 min, mostly downloads)
1. **Unity Hub** → install **Unity 6000.5.10f1** exactly, with **Android Build Support** (+ OpenJDK, Android SDK & NDK). If Unity asks to upgrade the project: **cancel**.
2. **GitHub Desktop** → accept Andra's invite → **Clone** `Global_Hackathon_FindMe` → open in Unity Hub.
3. **Claude Code** (VS Code extension or desktop app). Start sessions from the repo root.
4. Phone: Settings → About phone → Software information → tap Build number 7× → Developer options → **USB debugging** on. Install **Google Play Services for AR** from the Play Store.

## 2. Where the design lives
- `Docs/design/screens/*.png`: every screen of the prototype (390×844 design frames). Claude Code can look at these images.
- `Docs/design/html/*.dc.html`: the HTML/CSS source of each screen (exact colors, sizes, copy).
- `Docs/design/DESIGN_SPEC.md`: colors, fonts, components, motion, and **which screens to build** (Section 8).
- `Assets/_Team/UI/Art/`: ready-to-use fonts, icons (grape + white), mascot PNGs, rounded sprites, logo.
- Ask Andra for the **live design canvas link** if you want to click through the prototype.

> Rule of thumb: Unity reference resolution 1080×2400 = design px × 2.77.

## 3. How your UI connects to the app
```
SearchController (AR lead)  ──calls──▶  ISearchUI  ◀──implements──  UIManager (you)
UIManager buttons           ──call──▶  SearchController.StartSearch / ScanArea / MarkChecked /
                                        MarkFound / UndoChecked / ConfirmMatch / StopSearch
```
- **Read `Assets/_Team/AR/Scripts/ISearchUI.cs` first** and implement every method exactly.
- Right now `DevUI` (AR folder) implements it as a placeholder. Your `UIManager` replaces it.
- Build everything inside **one prefab**: `Assets/_Team/UI/Prefabs/UIRoot.prefab`. Andra drops it into the Main scene. **You never open/edit Main.**
- Test in **your own scene** `Assets/_Team/UI/Scenes/UI_Test.unity` with a `FakeSearchDriver` script (buttons/keys that call your UIManager methods with fake data: profile, readiness 0→1, spot panel, toast, match, found, summary).

## 4. Foundation (do first, ~1.5 h)
- [ ] `UITheme` ScriptableObject with every color token from DESIGN_SPEC §2.
- [ ] TMP font assets: Fredoka SemiBold (headings/buttons), Nunito Regular/Bold/ExtraBold (body). Atlas 1024–2048, Latin only.
- [ ] `UIRoot` prefab: Canvas (Screen Space Overlay), Canvas Scaler (Scale With Screen Size, 1080×2400, match 0.5), **SafeArea** panel, one child panel per screen with a `CanvasGroup`.
- [ ] `ScreenRouter`: show one screen at a time with fade + 24 px slide (0.25 s).
- [ ] `Tweens.cs`: tiny coroutine helper (fade, move, scale, punch) with ease-out / ease-out-back curves + global `ReduceMotion` flag.
- [ ] **ChunkyButton** prefab variants: Primary (pink), Success (mint), Calm (lavender), Secondary (white), Dark (grape). Face + 5 px edge; press = face moves down 4 px.
- [ ] Reusable: Chip, Card, Toast, BottomSheet, ProgressBar, IconTile.

**Done when:** UI_Test shows a sample of every component that matches `Docs/design/html` colors and shapes.

## 5. Screens (priority order)
Map to DESIGN_SPEC §8. Check each against its PNG.

| # | Screen | Design file | Must have |
|---|---|---|---|
| 1 | Input | `WhatLooking.png` | Title, text field ("Black leather wallet, a bit worn"), mic icon (decorative for now), Continue → `StartSearch(text)` |
| 2 | Analyzing | `Analyzing.png` | Mascot `look_scan`, "Getting to know your {item}", 3 animated check rows |
| 3 | Item card | `ItemCard.png` | Item tile + name + chips (shape, size), "Likely hiding spots" numbered list, **Start scanning** |
| 4 | Readiness | `ScanStart.png` | White card on camera: icon, "Move your phone slowly", progress bar from `SetReadiness(p01)` |
| 5 | Scan HUD | `ScanHUD.png` | Top bar (item icon, name, "2 of 5 checked" + bar), legend chips, bottom: stop (white circle), big **Scan this area** (pink) or **I found it** (mint), calm (lavender circle); status line |
| 6 | Spot panel | `PartialPopup.png` | Bottom sheet: mascot, state chip ("Only glanced"/"Looking…"), label, tip, dwell ring (from `dwell01`), manual **✓ Checked** / **Found it** |
| 7 | Checked toast | `AlreadyChecked.png` | Slides from top, soda/tangerine, label + **Undo** (4 s) |
| 8 | Match sheet | `PossibleMatch.png` | "Is this your wallet?" + **Not it** / **Yes, that's it!** → `ConfirmMatch(bool)` |
| 9 | Found | `Found.png` | Confetti, mascot `happy_wave` bounce, "Found it!", sentence, **Nice, done** |
| 10 | Summary | `Summary.png` | 3 stat tiles (time, spots checked, scans), "Back home" |
| 11 | All checked | `NotFound.png` (simplified) | Sheet: "All spots checked. Scan another area?" + Scan button |

**HUD rule:** the camera view must stay visible. Keep overlays at the top and bottom; the middle 60% of the screen is for the AR lead's 3D cues.

## 6. Polish (after merge, Thu 13:30–22:00)
- [ ] Button press feel + light haptic hook (`FeedbackFx` is the AR lead's; call it, don't rewrite it).
- [ ] Sounds on UI events: tap, sheet open, toast, found (ask Andra for the `FeedbackFx` event names).
- [ ] Confetti tuned (1.2 s, 40 pieces, theme colors).
- [ ] Count-up on progress numbers.
- [ ] Empty/edge states: Gemini slow → "Still looking…" after 3 s; error → "Hmm, I couldn't see clearly. Try again or mark it yourself."
- [ ] Accessibility: text ≥ 16 sp, buttons ≥ 56 px, icon + color for every state, `ReduceMotion` toggle.
- [ ] Optional if time: Home screen (`Home.png`) and Calm breathing (`Breathing.png`).

## 7. Testing on the phone
- Do most work in **UI_Test** in the Editor at 1080×2400 (Game view).
- To see your UI on the phone: build **UI_Test** alone (File → Build Settings → add only UI_Test, Build And Run). **Don't commit Build Settings changes**: revert them in GitHub Desktop.
- Full-app tests happen on Andra's builds (only 3 planned). Be there for them and note any UI bugs.

## 8. Timeline
| When | Do | Checkpoint |
|---|---|---|
| Wed night | Setup + Foundation | Components match the design in UI_Test |
| Thu 09:00–12:00 | Screens 1–8 | **12:00:** `UIRoot` prefab implements `ISearchUI`, works with FakeSearchDriver → hand to Andra |
| Thu 13:30–16:00 | Screens 9–11, fixes from merge | Works in Andra's Build 2 |
| Thu 16:00–22:00 | Polish | **22:00 freeze** |

## 9. Git rules
- Only edit `Assets/_Team/UI/`. Never edit the Main scene, `ProjectSettings/`, `Packages/`.
- Pull before you start, commit + push every hour ("UI: match sheet + toast").
- `ISearchUI` missing something? Tell Andra; don't change it yourself.

## 10. Prompts for Claude Code
**Start of session:**
```
Read CLAUDE.md, Assets/_Team/UI/CLAUDE.md, Docs/UI_LEAD_GUIDE.md and Docs/design/DESIGN_SPEC.md.
Look at Assets/_Team/AR/Scripts/ISearchUI.cs and SearchController.cs public methods.
I'm the UI lead. Propose the folder structure and script list for Section 4 (Foundation).
Don't write code yet.
```
**Foundation:**
```
Implement Section 4 of Docs/UI_LEAD_GUIDE.md in Assets/_Team/UI/: UITheme, TMP font assets
setup steps, UIRoot prefab structure, ScreenRouter, Tweens, ChunkyButton variants and the
reusable components. Create UI_Test scene with a sample of each component.
Match Docs/design/html colors and radii. Explain key lines.
```
**Screens (one or two at a time):**
```
Build screens 1–3 from Section 5. Look at Docs/design/screens/WhatLooking.png, Analyzing.png,
ItemCard.png and their HTML in Docs/design/html/ for exact layout and copy.
Implement the matching ISearchUI methods in UIManager and add FakeSearchDriver buttons to
trigger them in UI_Test.
```
(Repeat for 4–5, 6–8, 9–11.)
**Handoff:**
```
Check UIManager implements every ISearchUI method, list any missing pieces, and write
short wiring steps for Andra to replace DevUI with UIRoot in the Main scene.
```
