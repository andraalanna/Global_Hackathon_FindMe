# FIND ME! Design Spec (for the Unity UI)

Source of truth: `screens/*.png` (390×844 design frames rendered at 2×) and `html/*.dc.html` (exact CSS).
Unity reference resolution is **1080×2400**, so multiply design px by **~2.77** (1080/390). Example: 16 px text → ~44 px in Unity.

## 1. Vibe
"A calm, cheerful friend who helps you look." Young, soft, bright. Flat shapes, chunky rounded buttons with a solid bottom edge (Duolingo-like), lots of white space, one main action per screen. **No gradients, no blur, no glassmorphism, no emoji as icons.**

## 2. Colors ("Sherbet Pop")
| Token | Hex | Use |
|---|---|---|
| grape | `#3A2E6B` | All text on light backgrounds, dark cards, dark buttons |
| grapeDark | `#2B2250` | HUD chips on camera |
| bubblegum | `#FF7EB6` | Brand, primary button, "to check" cues |
| bubblegumEdge | `#D9559A` | Primary button bottom edge |
| mint | `#4FE3A8` | Success, "Found", "I found it" button |
| mintEdge | `#26B07B` | Mint button edge |
| lavender | `#C3B1FF` | Calm mode, secondary cards |
| lavenderEdge | `#9A84EA` | Lavender button edge |
| lemon | `#FFE45C` | "Glanced" state, highlights |
| soda | `#6CC4FF` | "Checked" state |
| tangerine | `#FFA77A` | Gentle nudges ("Open the drawer") |
| marshmallow | `#FFF7FB` | App background |
| cloud | `#E4E0F7` | Borders, dividers, empty bars |
| muted | `#655A8A` | Secondary text |
| white | `#FFFFFF` | Cards, sheets |

Text on pastel = grape, never white. Never use red.

## 3. Type
| Style | Font | Design px | Unity px (1080) |
|---|---|---|---|
| Display | Fredoka SemiBold | 40–48 | 110–130 |
| H1 | Fredoka SemiBold | 28–32 | 78–88 |
| H2 | Fredoka SemiBold | 20–24 | 55–66 |
| Button | Fredoka SemiBold | 18 | 50 |
| Body | Nunito Regular/Bold | 15–17 | 42–47 |
| Small | Nunito Bold | 12–13 | 33–36 |

Fonts: `Assets/_Team/UI/Art/Fonts/` (Latin subset, OFL licensed).

## 4. Shape, spacing, depth
- Spacing scale: 8 / 16 / 24 / 32 design px.
- Radius: buttons 18, cards 22–32, sheets 32 (top corners), chips/pills full.
- Depth: **solid bottom edge**, not shadow blur. Buttons: face + 5 px edge in the darker color. Pressed: face moves down 4 px, edge shrinks.
- Sprites: `Art/Sprites/rounded_18.png`, `rounded_28.png`, `pill.png`, `circle.png` (white, tint with Image color, 9-slice borders = radius).

## 5. Components
| Component | Spec |
|---|---|
| Primary button | bubblegum face, bubblegumEdge, grape label, height 56 design px, full width minus 24 margins |
| Success button | mint / mintEdge ("I found it", "Yes, that's it!") |
| Calm button | lavender / lavenderEdge |
| Secondary button | white face, 2 px cloud border, edge `#D8D2EE` |
| Dark button | grape face, white label |
| Chip | 40 px tall pill, white + 2 px cloud border; selected = grape fill, white text |
| Input | 52 px, white, 2 px cloud border, radius 16 |
| Card | white, 2 px cloud border, radius 22, padding 14–16 |
| Bottom sheet | white, radius 32 top, grab handle 44×5 cloud, padding 24 |
| Toast | tangerine (nudge) or soda (checked), radius 20, icon tile left, slides from top |
| Progress bar | cloud track, colored fill, radius full |
| HUD top bar | white card radius 20 on camera, item icon tile + name + % |

## 6. Icons and mascot
- Icons: `Art/Icons/icon_*.png` (grape) and `Art/Icons/white/*.png`. Line style, 2.2 stroke.
- Mascot "Keyo" (the peeking key, name optional): `Art/Mascot/`
  - `open` (default/wave on Home), `look` (scanning, analyzing), `happy` (found, success), `calm` (calm mode, all set), `look_scan` (analyzing with scan brackets).
- Logo: `Art/Sprites/logo.jpg`.

## 7. Motion
| Moment | Animation |
|---|---|
| Screen change | fade + slide 24 px, 0.25 s ease-out |
| Button press | face down 4 px, 0.08 s; release spring back |
| Toast | slide down from top 0.2 s, auto-hide 3–4 s |
| Sheet | slide up 0.3 s ease-out-back |
| Found | mascot bounce ×3, confetti (pink, mint, lemon, lavender, soda rectangles) 1.2 s |
| Progress count | number counts up 0.4 s |
| Reduce motion | replace movement with 0.15 s fades |

## 8. Screens: what to build in Unity
Build these (MVP). Each maps to `ISearchUI` calls from the AR lead.

| Design file | Unity panel | Shown when (ISearchUI call) |
|---|---|---|
| `WhatLooking.png` | InputScreen | App start / after Home. Continue → `SearchController.StartSearch(text)` |
| `Analyzing.png` | AnalyzingScreen | `ShowStatus("Getting to know your …")` during profiling |
| `ItemCard.png` | ItemCardScreen | `ShowItemCard(profile)`. Button "Start scanning" (Calm/Quick cards optional; Quick = start) |
| `ScanStart.png` | ReadinessOverlay | `SetReadiness(p01)` + `ShowStatus("Move your phone slowly…")` |
| `ScanHUD.png` | ScanHUD | Top bar (item, % / "2 of 5 checked"), legend chips, bottom bar: stop · **Scan this area** / "I found it" · calm |
| `PartialPopup.png` | SpotPanel (sheet) | `ShowSpotPanel(label, tip, dwell01)` incl. manual ✓ Checked / Found buttons |
| `AlreadyChecked.png` | Toast | `ShowCheckedToast(label)` with **Undo** |
| `PossibleMatch.png` | MatchSheet | `ShowMatch(label)` → Yes / Not it → `ConfirmMatch(bool)` |
| `Found.png` | FoundScreen | `ShowFound(label)` |
| `Summary.png` | SummaryScreen | `ShowSummary(session)` |
| `NotFound.png` | AllCheckedSheet | When all spots checked: "Scan another area?" |

Prototype-only (do **not** build in Unity now): Main, Welcome, SignUp, ProfileSetup, Permissions, Home*, MyItems, ItemDetail, Breathing*, Retrace*, Circles, Replace, ReminderSettings, Notification, LockScreen, EveningCheck, CheckDone, History, Settings, CarePlan, Dots*.
(*) Home and Breathing are nice extras only if time remains after Thursday 19:00.

**Important:** 3D cues on the camera (pink bubbles, checked ✓, found burst) are built by the **AR lead**, not the UI. The UI only does 2D overlays.

## 9. Copy (keep it short and kind)
- "Move your phone slowly around the room"
- "Point at part of the room, then tap Scan"
- "Looking for hiding spots…"
- "Hold still…" / "Looking…"
- "Desk's done! Peek in the bag next?"
- "Hmm, I couldn't see clearly. Try again or mark it yourself."
- "Is this your wallet?" · "Yes, that's it!" · "Not it"
- "Found it! Your wallet was hiding in your bag."
