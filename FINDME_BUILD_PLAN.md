# FIND ME! Build Plan for Claude Code

AR lead: Andra · Unity 6000.5.10f1 · AR Foundation + ARCore · Samsung Galaxy A80 (Android) · C#
Deadline: working demo before **Friday 07:00**. Search experience first, UI/UX polish second.

---

## 0. How to use this file with Claude Code

1. Put this file in the repo root as `FINDME_BUILD_PLAN.md`.
2. Create a short `CLAUDE.md` in the repo root (template in Section 16) so Claude Code always loads the rules.
3. Work **one phase at a time** and commit after each phase. Test in the Unity Editor (XR Simulation + mock Gemini) after every phase. **Build to the phone only 3 times** (Section 12a).
4. Paste the kickoff prompt from Section 17 to start.

**Rules for Claude Code (also in CLAUDE.md):**
- Only create or edit files in `Assets/_Team/AR/` and the Main scene. Never touch `Assets/_Team/UI/` or `Assets/_Team/AI/` unless told.
- Keep each script focused and under ~300 lines. Comment the *why*, not the obvious.
- Before adding any package, ask.
- Check the installed AR Foundation version in `Packages/manifest.json` and use the matching API (anchor APIs differ between versions).
- Never commit the Gemini API key.
- After each phase, list: files changed, how to test on the phone, and known issues.

---

## 1. What we are building (scope)

### The search flow (the heart of the demo)
1. User types what they lost ("black wallet").
2. Gemini returns the item profile: name, typical size, shape, likely places.
3. User moves the phone slowly until AR is ready.
4. User taps **Scan this area**. App captures one snapshot + camera pose.
5. Gemini returns up to 5 hiding spots with 2D boxes.
6. App turns each box into a **3D cue anchored next to the real furniture**.
7. User walks to a cue and holds the phone on it for ~2 seconds.
8. App sends that frame to Gemini: is the item visible? is the spot fully visible?
9. Cue updates automatically: **Checked** (with Undo), **Open it** tip, or **Is this your wallet?** then **Found!**
10. Cues keep their state and stay in place for the whole session. User can scan more areas.

### In scope
- Everything above, running live on the Galaxy A80.
- A **mock Gemini mode** for offline testing.
- A debug overlay.
- After the core works: animations, sounds, haptics, UI polish.

### Out of scope (do not build)
FIND ME Dots, depth API, on-device YOLO, saving between sessions, photo input for the item (stretch only), coverage grid (stretch only), backend, login.

---

## 2. Architecture

### Components (all in `Assets/_Team/AR/Scripts/`)

| Script | Responsibility |
|---|---|
| `SearchController.cs` | The brain. Owns the search state machine, connects everything, talks to UI. |
| `ARReadiness.cs` | Decides when AR tracking is good enough to scan. |
| `SnapshotCapture.cs` | Hides overlays, captures the screen, downscales, encodes JPEG, records the camera pose. |
| `GhostCamera.cs` | Recreates the camera exactly as it was at capture time, to cast rays later. |
| `SpotPlacer.cs` | Box → screen point → ray → raycast → anchor → spawn cue. Merges duplicates. |
| `SpotCue.cs` | One cue in the world: visuals, state, animations, sounds. |
| `SpotFocus.cs` | Every frame: which cue is the user looking at, is the phone steady, dwell timer. |
| `SpotChecker.cs` | Runs the automatic "is it here?" check for the focused cue. |
| `OffscreenIndicator.cs` | Screen-edge arrow pointing to the next unchecked spot. |
| `FeedbackFx.cs` | Central place for sounds and haptics. |
| `DebugOverlay.cs` | FPS, tracking state, planes, requests in flight, last latency, snapshot preview with boxes. |
| `Gemini/IGeminiService.cs` | Interface so real and mock clients are swappable. |
| `Gemini/GeminiClient.cs` | Real HTTP client (see Section 6). If the AI lead already wrote one, wrap theirs. |
| `Gemini/MockGeminiService.cs` | Returns canned JSON after a fake delay. |
| `Models/*.cs` | Data classes (Section 5). |

### Data flow

```
[User text] → IGeminiService.GetItemProfile → ItemProfile
[Scan tap] → SnapshotCapture → (jpg + CapturePose)
          → IGeminiService.FindSpots(jpg, item) → List<HidingSpot> (2D boxes)
          → SpotPlacer (uses GhostCamera + ARRaycastManager) → SpotCue objects anchored in world
[Every frame] SpotFocus → focused cue + steady? + dwell progress → SpotCue shows progress ring
[Dwell = 2 s] SpotChecker → SnapshotCapture (512 px) → IGeminiService.CheckSpot
          → SpotCheckResult → SearchController updates SpotCue state + UI
```

### Search state machine (`SearchController`)

```
Idle → Profiling → WaitingForAR → ReadyToScan → Scanning → Searching ⇄ Scanning
                                                         Searching → FoundConfirm → Found
Any state → Error (recoverable, back to previous state)
```

| State | Enter when | UI shows | Exit when |
|---|---|---|---|
| `Idle` | App start | Home / input (UI lead) | `StartSearch(text)` |
| `Profiling` | Text submitted | "Getting to know your wallet…" | Profile returns (or fails → use text as name, default size) |
| `WaitingForAR` | Profile ready | "Move your phone slowly around the room" + readiness meter | `ARReadiness.IsReady` for 1 s |
| `ReadyToScan` | AR ready | Scan button enabled, "Point at part of the room, then tap Scan" | Scan tapped |
| `Scanning` | Scan tapped | Shutter flash, "Looking for hiding spots…", placeholder shimmer | Spots placed or error |
| `Searching` | Spots placed | Cues, progress "2 of 5 checked", Scan-more button | Found confirmed / user stops |
| `FoundConfirm` | Check sees the item | Sheet "Is this your wallet?" | Yes → `Found`, No → `Searching` |
| `Found` | Confirmed | Celebration, summary button | Done |

---

## 3. Project setup (Phase 0)

**Packages** (check versions already installed first):
- AR Foundation, Google ARCore XR Plugin (same major version)
- Newtonsoft Json (`com.unity.nuget.newtonsoft-json`)
- Input System (default in Unity 6)
- TextMeshPro (built in)

**Project Settings:**
- XR Plug-in Management → Android → **ARCore** on.
- Player → Android: Minimum API **24+**, Scripting Backend **IL2CPP**, **ARM64** only, uncheck Auto Graphics API and keep **OpenGLES3**, Internet Access **Require**, Default Orientation **Portrait** (locked; the box math assumes portrait).
- Quality: target 30 FPS (`Application.targetFrameRate = 30`) to reduce heat on the A80.

**Layers:** add `Cues` (world-space cues) and `Overlay` (debug + indicators). Snapshot capture hides both.

**Main scene hierarchy:**
```
AR Session
XR Origin (Mobile AR)
  Camera Offset
    Main Camera  [ARCameraManager, ARCameraBackground, TrackedPoseDriver]
  [ARPlaneManager: detection = Everything, plane prefab = subtle dotted plane]
  [ARRaycastManager] [ARAnchorManager]
GhostCamera  [Camera, disabled, tag none]
Systems
  SearchController, ARReadiness, SnapshotCapture, SpotPlacer, SpotFocus, SpotChecker, FeedbackFx
  GeminiServiceProvider (chooses real or mock)
Cues  (empty parent for spawned cues)
UI  (UI lead's prefab goes here)
DebugOverlay (canvas, Overlay layer)
```

**Plane visual:** very subtle (white dots at 15% opacity). Hide planes entirely once `Searching` starts so cues are the star.

**Acceptance:** empty scene builds to the A80, camera feed shows, planes appear when moving the phone.

---

## 4. Secrets

- Key file: `Assets/_Team/AI/Secrets/Resources/gemini_key.txt` (a `Resources` folder so it is included in the Android build; files outside `Resources` cannot be read at runtime).
- Load: `Resources.Load<TextAsset>("gemini_key").text.Trim()`.
- `.gitignore`: add `Assets/_Team/AI/Secrets/` and `Assets/_Team/AI/Secrets.meta`.
- If the key is missing, automatically use `MockGeminiService` and show "Mock mode" in the debug overlay.

---

## 5. Data models (`Assets/_Team/AR/Scripts/Models/`)

```csharp
public class ItemProfile {
    public string name;            // "black wallet"
    public float[] sizeCm;         // [11, 9]
    public string shape;           // "flat rectangle"
    public List<string> likelyPlaces;
}

public class HidingSpot {          // from Gemini Call 2
    public string label;           // "drawer"
    public int[] box_2d;           // [ymin, xmin, ymax, xmax], 0..1000
    public int likelihood;         // 1..3 (3 = most likely)
    public string hint;            // "Wallets slide under clothes"
}

public class SpotCheckResult {     // from Gemini Call 3
    public bool itemVisible;
    public int[] itemBox;          // nullable
    public bool spotFullyVisible;
    public string tip;             // "Open the drawer"
}

public enum SpotState { ToCheck, Focused, Checking, NeedsOpen, Checked, Candidate, Found }

public struct CapturePose {        // saved at snapshot time
    public Vector3 position;
    public Quaternion rotation;
    public Matrix4x4 projection;
    public int screenWidth, screenHeight;
    public float time;
}
```

---

## 6. Gemini service

### Interface
```csharp
public interface IGeminiService {
    Task<ItemProfile> GetItemProfile(string userText, CancellationToken ct);
    Task<List<HidingSpot>> FindSpots(byte[] jpg, ItemProfile item, CancellationToken ct);
    Task<SpotCheckResult> CheckSpot(byte[] jpg, ItemProfile item, string spotLabel, CancellationToken ct);
}
```

### HTTP details
- Endpoint: `POST https://generativelanguage.googleapis.com/v1beta/models/{MODEL}:generateContent`
- Header: `x-goog-api-key: {KEY}`, `Content-Type: application/json`
- Body shape:
```json
{
  "contents": [{ "role": "user", "parts": [
    { "text": "PROMPT" },
    { "inline_data": { "mime_type": "image/jpeg", "data": "BASE64" } }
  ]}],
  "generationConfig": {
    "temperature": 0.2,
    "responseMimeType": "application/json",
    "responseSchema": { ... },
    "thinkingConfig": { "thinkingBudget": 0 }
  }
}
```
- Read the answer from `candidates[0].content.parts[0].text`, then parse that text as JSON.
- Models: Calls 1 and 2 → `gemini-2.5-flash`. Call 3 → `gemini-2.5-flash-lite`. Keep model names in one config ScriptableObject so they are easy to swap.
- Use `UnityWebRequest` wrapped in async/await (`await request.SendWebRequest()` via an awaiter or `Awaitable`), never block the main thread.
- Timeout: 8 s (Calls 1–2), 5 s (Call 3). One retry on network error for Calls 1–2 only.
- Log every call: model, image size in KB, latency ms, success, raw text (truncated) to the debug overlay.

### Prompts

**Call 1: item profile** (text only)
```
The user lost: "{userText}".
Return JSON with: name (short, lowercase), sizeCm [width, height] using typical real-world size,
shape (2-4 words), likelyPlaces (up to 5 short phrases, most likely first).
```
Schema: object {name: string, sizeCm: array of number, shape: string, likelyPlaces: array of string}.

**Call 2: find hiding spots** (snapshot, 768 px wide)
```
We are looking for a lost {name} ({shape}, about {w} x {h} cm).
In this photo of a room, find up to 5 VISIBLE places where it could be hidden:
drawer, cabinet, under the bed, bag, pile of clothes, sofa cushions, shelf, desk surface, coat.
Only include places big enough to hold it. Prefer containers and piles over open empty surfaces.
For each: label (1-3 words), box_2d [ymin, xmin, ymax, xmax] normalized 0-1000,
likelihood 1-3 (3 = most likely), hint (max 8 words, friendly).
```
Schema: array of {label, box_2d (array of 4 integers), likelihood (integer), hint}.

**Call 3: spot check** (current frame, 512 px wide)
```
We are looking for a lost {name} ({shape}, about {w} x {h} cm).
The user is checking this spot: "{label}".
itemVisible: true only if the {name} itself is clearly visible. If true, give itemBox [ymin, xmin, ymax, xmax] 0-1000.
spotFullyVisible: true only if the inside or underneath of the spot is clearly visible
(drawer open, blanket lifted, bag open). A closed drawer or covered pile is false.
tip: max 6 words telling the user what to do next (e.g. "Open the drawer", "Lift the top shirt").
```
Schema: object {itemVisible: boolean, itemBox: array of integer (nullable), spotFullyVisible: boolean, tip: string}.

### Mock service (build this in Phase 2, before the real one is wired)
- `FindSpots` returns 3–4 spots with boxes spread across the screen: left-middle "pile of clothes" (likelihood 3), center-low "drawer" (2), right "bag" (2), top "shelf" (1). Delay 1.2 s.
- `CheckSpot` cycles per spot: first call → `spotFullyVisible:false, tip:"Open it up"`; second → `spotFullyVisible:true`. For the spot labeled "bag", second call returns `itemVisible:true` with a centered box. Delay 0.8 s.
- Toggle via debug overlay button or a checkbox on `GeminiServiceProvider`.

---

## 7. Phase-by-phase build

Each phase ends with a phone build and a commit.

### Phase 1: AR readiness
`ARReadiness.cs`
- Ready when **all** true for 1 continuous second:
  - `ARSession.state == ARSessionState.SessionTracking`
  - At least 1 plane with area ≥ 0.25 m² (sum `ARPlane.size.x * size.y`)
  - Camera moved ≥ 0.5 m total since start *or* rotated ≥ 60° (proves the user looked around)
- Expose `float Progress01` (0–1) for a readiness meter, and `bool IsReady`.
- **Acceptance:** meter fills while moving the phone, reaches 100% within ~10 s in a textured room.

### Phase 2: Item profile + mock service
- `IGeminiService`, `MockGeminiService`, `GeminiServiceProvider`.
- `SearchController.StartSearch(string text)` → Profiling → calls `GetItemProfile` → `UIManager.ShowItemCard(profile)` → WaitingForAR.
- Temporary test UI if the UI lead's prefab is not ready: an input field + button in a `DevUI` canvas.
- **Acceptance:** typing "wallet" moves through states; debug overlay shows the parsed profile.

### Phase 3: Snapshot + ghost camera
`SnapshotCapture.cs`
- `IEnumerator Capture(int targetWidth, Action<byte[], CapturePose> onDone)`:
  1. Record `CapturePose` from Main Camera **before** hiding anything (position, rotation, `projectionMatrix`, `Screen.width`, `Screen.height`).
  2. Hide overlays: set Main Camera `cullingMask` to exclude `Cues` and `Overlay`; disable UI canvases' `Canvas.enabled`.
  3. `yield return new WaitForEndOfFrame();`
  4. `ScreenCapture.CaptureScreenshotAsTexture()`.
  5. Restore culling mask and canvases immediately.
  6. Downscale with `Graphics.Blit` into a RenderTexture of `targetWidth × (targetWidth * H / W)`, read back, `EncodeToJPG(70)`.
  7. Destroy temporary textures.
- Play shutter sound + white flash (0.12 s) **after** restoring the UI so the flash is not in the image.

`GhostCamera.cs`
- `Ray ScreenRay(CapturePose pose, Vector2 screenPx)`:
  - Move the disabled ghost Camera to `pose.position/rotation`, set `ghost.projectionMatrix = pose.projection`, set its `pixelRect` to match `pose.screenWidth/Height`, then `ghost.ScreenPointToRay(screenPx)`.
- Add an EditMode unit test: with a known pose, the screen center ray equals the camera forward.

**Acceptance:** debug overlay shows the last snapshot thumbnail; no cues/UI in it.

### Phase 4: Find spots + 2D debug view
- `SearchController.ScanArea()`:
  - Only allowed in `ReadyToScan` or `Searching`.
  - Capture at 768 px → `FindSpots` → keep the `CapturePose` with the result.
- `DebugOverlay`: show the snapshot thumbnail with each `box_2d` drawn as a rectangle + label. This is the fastest way to tell whether Gemini or our math is wrong.
- Sort spots by likelihood (desc), cap at 5.
- **Acceptance:** with real Gemini, boxes on the thumbnail sit on the right furniture.

### Phase 5: Place cues in 3D (critical path)
`SpotPlacer.cs`

**Box → screen pixels:**
```csharp
// Gemini: [ymin, xmin, ymax, xmax] in 0..1000, origin top-left
// Unity screen: origin bottom-left, pixels
Vector2 BoxPointToScreen(int[] b, float u, float v, CapturePose p) {
    // u,v in 0..1 inside the box (0.5,0.5 = center)
    float x01 = Mathf.Lerp(b[1], b[3], u) / 1000f;
    float y01 = Mathf.Lerp(b[0], b[2], v) / 1000f;
    return new Vector2(x01 * p.screenWidth, (1f - y01) * p.screenHeight); // flip Y
}
```

**Sample points, not just the center.** Raycast 5 points inside the box: (0.5,0.5), (0.3,0.4), (0.7,0.4), (0.3,0.7), (0.7,0.7). For labels containing "under", use the lower part: v in 0.7–0.9.

**Raycast:**
- `arRaycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon | TrackableType.PlaneEstimated)`
- Collect hit distances, take the **median** hit → cue position. Prefer hits on vertical planes for "drawer/cabinet", horizontal for "bed/desk/pile".
- **No hit fallback:** position = `ray.GetPoint(d)` where `d` = median distance of all planes' centers in view, clamped 0.6–3.5 m; if no planes, 1.5 m.

**Anchor:**
- If the hit has a plane: attach the anchor to that plane (`ARAnchorManager.AttachAnchor(plane, pose)` or the version-appropriate API).
- Else create a free anchor (AR Foundation 6: `TryAddAnchorAsync(pose)`; older: `AddComponent<ARAnchor>()` on a new GameObject). Check the installed version.
- Parent the cue under the anchor's transform so it moves with tracking corrections.

**Merge duplicates:** if a new spot is within 0.35 m of an existing cue *and* labels are similar (same word or one contains the other), keep the existing cue (and its state), update likelihood to the max. Global cap: 8 cues.

**Stagger spawn:** spawn cues 150 ms apart, highest likelihood first, each with a pop animation + "pop" sound (pitch rising slightly per cue).

**Acceptance:** after scanning, walk around the room for 30 s. Every cue stays beside its real object (within ~20 cm).

### Phase 6: Cue visuals (`SpotCue` prefab)

**Structure** (prefab in `Assets/_Team/AR/Prefabs/SpotCue.prefab`, layer `Cues`):
```
SpotCue (root, at the spot)
  GroundRing     flat disc on the surface, radius 12 cm, unlit transparent material
  ProgressRing   same disc as a radial fill (0→1) for dwell progress
  Stem           thin vertical line (LineRenderer) from ring up 18 cm
  Bubble         world-space Canvas (scale ~0.0008) at stem top
    Badge        circle with priority number (1, 2, 3…) or state icon
    Label        TMP, e.g. "Drawer"
    Hint         TMP small, e.g. "Check inside" (hidden when far)
  FoundFx        particle burst (disabled)
```

**Always readable:**
- Bubble billboards to the camera, **yaw only** (stays upright).
- Constant on-screen size: `scale = baseScale * Mathf.Clamp(distance, 0.5f, 4f)`.
- Hint text only shows within 2 m; beyond 5 m the whole cue fades to 40%.
- Cues render on top of the camera feed (no occlusion) so they are never lost behind furniture.

**States and looks** (colors from the design system):

| State | Bubble color | Badge | Ring | Motion | Text |
|---|---|---|---|---|---|
| `ToCheck` | Pink `#FF7EB6` | Priority number | Pink outline | Soft pulse (scale 1.0→1.08, 1.4 s loop) | Label + hint |
| `ToCheck` + next | Pink, brighter | Number + "Next" tag | Pink, thicker | Stronger pulse + small bob | "Next: Drawer" |
| `Focused` | Pink, 1.15× size | Number | ProgressRing fills over 2 s | Pulse stops, gentle glow | "Hold still…" |
| `Checking` | Lavender `#C3B1FF` | 3 animated dots | Ring full, slow spin | — | "Looking…" |
| `NeedsOpen` | Tangerine `#FFA77A` | Hand icon | Tangerine | Small wiggle once | Tip from Gemini: "Open the drawer" |
| `Checked` | Soda blue `#6CC4FF`, 85% opacity | ✓ icon | Solid blue, no pulse | Quick squash-and-settle once | "Checked" |
| `Candidate` | Mint outline | ? icon | Mint pulse | Bounce | "Is this it?" |
| `Found` | Mint `#4FE3A8` | Star icon | Mint burst | Confetti + bounce | "Found it!" |

- Text is dark grape `#3A2E6B` on light bubbles (never white on pastel).
- Every state also has a distinct **icon**, so color-blind users are fine.

**Off-screen indicator** (`OffscreenIndicator.cs`): if the "next" cue is outside the view, show a pink arrow at the screen edge pointing to it, with its label ("Bag →"). Hide when it comes into view.

**Progress chip:** "2 of 5 spots checked" at the top, updates with a small count-up animation.

### Phase 7: Focus + steadiness + dwell (`SpotFocus.cs`)

Every frame:
1. For each cue not in `Checked`/`Found`: `vp = cam.WorldToViewportPoint(cuePos)`.
   - Skip if `vp.z <= 0` (behind the camera) or distance > 2.2 m.
   - Angular offset from screen center: `Vector3.Angle(cam.forward, cuePos - cam.position)`.
2. Candidate = cue with the smallest angle, if angle ≤ 14°.
3. **Hysteresis:** switch focus to a new cue only if it is ≥ 3° closer than the current one, to avoid flicker.
4. **Steadiness:** keep a 0.5 s rolling average of camera angular speed and linear speed. Steady if angular < 25°/s and linear < 0.25 m/s.
5. **Dwell:** while focused and steady, `dwell += dt`; if unsteady, pause; if focus is lost for > 0.3 s, reset to 0.
6. Feed `dwell / 2.0` into the cue's ProgressRing. Play a soft tick at 0%, 50%.
7. At 2.0 s → `SpotChecker.Check(cue)`.

Expose `SpotCue Focused` and `float Dwell01` for UI (the spot panel).

### Phase 8: Automatic check (`SpotChecker.cs`)

- **One request in flight**, globally. If busy, ignore new triggers.
- Per-cue cooldown: 1.5 s after a result before the same cue can be checked again.
- Flow:
  1. Cue → `Checking`. Capture at 512 px (Phase 3 method, keep the `CapturePose`).
  2. `CheckSpot(jpg, item, cue.label)`.
  3. Handle the result:
     - `itemVisible == true` → map `itemBox` center with the saved pose (same math as Phase 5) → place a small mint marker there → cue `Candidate` → `UIManager.ShowMatch(label)` → state `FoundConfirm`.
       - **Yes** → cue `Found`, celebration, `UIManager.ShowFound(label)`, other cues fade to 30%.
       - **Not it** → cue back to `ToCheck`, remove marker, add "not the wallet" to a per-cue note so the next prompt can say "the user said a previous match was wrong".
     - `spotFullyVisible == true` → cue `Checked` + `UIManager.ShowCheckedToast(label)` with **Undo** (4 s). Undo → `ToCheck`.
     - else → cue `NeedsOpen` with `tip`. The user can act; dwell will re-trigger the check after the cooldown.
  4. Timeout or error → cue back to `Focused`, show the spot panel with manual **✓ Checked** / **Found it** buttons.
- After 3 attempts on the same cue without a clear answer → show the manual buttons.
- Manual actions call `SearchController.MarkChecked()`, `MarkFound()`, `UndoChecked()`.

**Next spot logic:** after any state change, the "next" cue = highest likelihood among `ToCheck`/`NeedsOpen`, ties broken by distance.

**All checked, not found:** show "All spots checked. Scan another area?" + Scan button pulse.

### Phase 9: Scan more areas
- `ScanArea()` works in `Searching` too. New spots merge (Phase 5 rules). Existing cues keep their state.
- While scanning, show a scanning sweep effect on screen (a soft gradient line moving top → bottom, 1.2 s loop).

### Phase 10: Session memory + summary data
- `SearchSession` keeps: start time, list of cues (label, state, time checked), number of scans, found label + time.
- On Found: `UIManager.ShowSummary(session)` → "Found in 2m 41s · 4 spots checked".

### Phase 11 (stretch): simple coverage
Only if everything above is stable before Thursday 19:00.
- Plane-attached grid of 20 cm cells; each frame raycast 9 screen points; cells hit while steady for ≥ 0.5 s turn light blue. Keep it faint so cues stay the focus.

---

## 8. UI/UX polish (after the search works)

The UI lead owns screens; you own everything in 3D and the in-camera overlays. Agree on these calls:

| Call | When |
|---|---|
| `UIManager.ShowItemCard(ItemProfile p)` | Profile ready |
| `UIManager.ShowStatus(string msg)` / `HideStatus()` | Waiting, scanning, errors |
| `UIManager.SetReadiness(float p01)` | During WaitingForAR |
| `UIManager.SetScanEnabled(bool on)` | Ready or not |
| `UIManager.ShowSpotPanel(string label, string tip, float dwell01)` | Focus |
| `UIManager.ShowCheckedToast(string label)` | Auto checked (with Undo) |
| `UIManager.ShowMatch(string label)` | Candidate found |
| `UIManager.ShowFound(string label)` | Confirmed |
| `UIManager.SetProgress(int checkedCount, int total)` | Any state change |
| `UIManager.ShowSummary(SearchSession s)` | After Found |

UI buttons call: `StartSearch(text)`, `ScanArea()`, `MarkChecked()`, `MarkFound()`, `UndoChecked()`, `ConfirmMatch(bool yes)`, `StopSearch()`.

### Animations (simple, no extra package)
Write a tiny `Tweens.cs` helper with coroutines + `AnimationCurve` (ease out back, ease in out):
- Cue spawn: scale 0 → 1.1 → 1.0 over 0.35 s (ease out back).
- Pulse: sine on scale.
- Checked: squash (1.15, 0.85) → (1, 1) over 0.25 s.
- NeedsOpen: rotate ±6° twice over 0.4 s.
- Found: bounce 3× + particle burst (pink, mint, lemon, lavender squares) + full-screen confetti from UI lead.
- Toasts slide up 0.2 s, auto hide.
- Respect a "Reduce motion" setting: replace pulses/bounces with fades.

### Sounds (`FeedbackFx.cs`)
Use free **CC0** packs (e.g. Kenney "Interface Sounds" / "UI Audio"). Keep all under 0.5 s, soft and cute, volume ~0.6, one `AudioSource` pool of 4.

| Event | Sound | Haptic |
|---|---|---|
| Scan shutter | soft camera "click-bloop" | light tap |
| Each cue appears | "pop", pitch +5% per cue | none |
| Dwell 50% | tiny tick | none |
| Check starts | soft "boop" | none |
| Checked | gentle two-note chime (up) | light tap |
| NeedsOpen | curious "hmm" boop (down) | double light tap |
| Candidate | sparkle | medium tap |
| Found | happy jingle (≤ 1.2 s) | success pattern |
| Undo | whoosh | none |
| Error | low soft thud | none |

Haptics on Android: short vibrations via `AndroidJavaObject` → `Vibrator.vibrate(VibrationEffect.createOneShot(ms, amplitude))` (API 26+), fallback `Handheld.Vibrate()` only for Found. Add a mute toggle.

### Friendliness rules
- Every wait over 0.3 s shows something (shimmer, dots, message).
- Copy is short and kind: "Desk's done! Peek in the bag next?"
- Never show raw errors; say "Hmm, I couldn't see clearly. Try again or mark it yourself."
- Big touch targets (≥ 56 px), text ≥ 16 sp, grape text on light backgrounds.

---

## 9. Debug overlay (`DebugOverlay.cs`)

Toggle with a 3-finger tap (Input System EnhancedTouch). Shows:
- FPS, `ARSession.state`, plane count + total area
- Current `SearchState`, focused cue, dwell, steadiness values
- Gemini: mode (real/mock), requests in flight, last latency per call, last raw JSON (first 200 chars)
- Last snapshot thumbnail with boxes
- Buttons: Mock on/off, Reset session, Force check focused, Clear cues

---

## 10. Testing

**Editor (no phone):**
- XR Simulation environment (Project Settings → XR Plug-in Management → PC/Standalone → XR Simulation) + mock service. Test placement, focus, states, UI calls.
- EditMode tests: `BoxPointToScreen` (corners, Y flip), ghost ray center = forward, merge rules.

**Phone checklist (do at every checkpoint):**
- [ ] Readiness reaches 100% within 10 s
- [ ] Scan → cues appear within ~3 s, beside the right objects
- [ ] Walk 3 m away and back: cues stay put
- [ ] Focus + 2 s hold triggers a check; unsteady hands pause it
- [ ] Closed drawer → "Open the drawer"; open drawer → Checked + Undo works
- [ ] Item visible → "Is this your wallet?" → Yes → Found celebration
- [ ] Scan a second area: old cues keep state, no duplicates
- [ ] Airplane mode: mock fallback or manual buttons, no freeze
- [ ] 10-minute session: no crash, phone not overheating

**Demo room setup:** textured surfaces (patterned blanket, wooden desk), good light, a black wallet half under a shirt in an open bag, one closed drawer.

---

## 11. Performance budget (Galaxy A80)

- 30 FPS target, no per-frame allocations in `SpotFocus` (reuse lists).
- Snapshots: 768 px (scan), 512 px (check), JPEG 70 → ~40–90 KB each.
- Max 1 Gemini request in flight for checks; scan requests cancel any pending check.
- Hide AR planes during `Searching`.
- Pause checks when the app is backgrounded.

---

## 12. Timeline (AR lead)

| When | Phases | Checkpoint |
|---|---|---|
| Wed 15:00–16:00 | 0 | Camera works on the A80 |
| Wed 19:30–21:30 | 1–2 | Readiness meter + profile (mock) |
| Wed 21:30–00:30 | 3–4 | Snapshot + real Gemini boxes correct on thumbnail |
| Wed 00:30–03:00 | 5–6 (basic look) | **Cues stay beside real furniture** |
| Thu 09:00–12:00 | 6 (states) + 7 | Focus + dwell + state visuals; merge with UI |
| Thu 13:30–16:00 | 8–10 | Auto check, Found flow, scan more, summary |
| Thu 16:00–17:30 | Test in demo room | Backup video recorded |
| Thu 19:00–22:00 | Section 8 polish (sounds, animations, haptics) | **22:00 feature freeze** |
| Thu 22:00–01:00 | Bug fixes, final APK | Demo works 3× in a row |
| Fri 06:00–07:00 | Fresh install, final run | Done |

---

## 12a. Only 3 phone builds

Everything between builds is tested in the Editor with **XR Simulation** (virtual room) + **MockGeminiService**. Each build tests only what the Editor cannot.

| Build | After phases | When | Test on the phone (Editor can't do these) |
|---|---|---|---|
| **Build 1: "It works in a real room"** | 0–5 | Wed ~02:30 | ARCore starts on the A80 · readiness meter in a real room · real Gemini call over Wi-Fi (Internet permission, key loads from Resources) · snapshot has no cues/UI · boxes land on real furniture · **cues stay put while walking** |
| **Build 2: "The full search"** | 6–10 | Thu ~16:00 (in the demo room) | Cue readability at real distances · focus + steadiness with real hand shake · real Gemini spot checks (closed vs open drawer, item visible) · latency feels OK · scan a second area · 10-minute heat test · **record backup video** |
| **Build 3: "Final"** | Polish + UI merge + bug fixes | Thu ~22:00 (freeze) | Sounds, haptics, animations on device · full demo 3× in a row · this APK is the one you present |

**Before Build 1, in the Editor:** readiness logic, state machine, mock profile, snapshot thumbnail (in XR Simulation), box → ray math via EditMode tests, cue spawn + merge with mock boxes.
**Before Build 2, in the Editor:** all cue states and animations, focus/dwell (move the simulated camera), mock check cycle (open → checked → found), Undo, off-screen arrow, scan-more merge.

**If a build fails on the phone:** fix, then use **Build Settings → Development Build + "Patch And Run"** (or "Scripts Only Build") for a quick re-install. Script-only patches take about a minute, so they don't count toward the 3 full builds.

**Faster dev builds (optional):** for Builds 1–2 you can use Scripting Backend **Mono** + **ARMv7** (builds much faster; the A80 runs 32-bit apps). Switch back to **IL2CPP + ARM64** for Build 3.

---

## 13. Known pitfalls

1. **Y flip** between Gemini (top-left origin) and Unity screen (bottom-left). Test it first.
2. Screenshot must not contain cues/UI, or Gemini will label your own cues.
3. Portrait lock: if orientation changes, stored screen size and projection no longer match.
4. Anchor API differs between AR Foundation versions; check before coding.
5. `ScreenCapture` must run after `WaitForEndOfFrame`.
6. Don't parse Gemini text before stripping stray ```json fences.
7. Gemini boxes can be slightly off; that is why we sample 5 points and use the median.
8. Old phone heat: target 30 FPS, avoid continuous requests.

---

## 14. Definition of done (demo)

A judge can watch this in under 60 seconds:
1. Type "black wallet" → item card.
2. Move phone → readiness fills → tap Scan → 4 cues pop in next to the bed pile, drawer, bag, shelf.
3. Walk to the drawer, hold 2 s → "Open the drawer" → open it → ✓ Checked (chime).
4. Look back: drawer cue stays blue and checked.
5. Arrow points to "Bag" → hold on it → "Is this your wallet?" → Yes → confetti + jingle → "Found in 1m 12s".

---

## 15. Stretch ideas (only after freeze-quality demo exists)
- Voice lines for key moments (Android TTS).
- Photo input for the item.
- Coverage grid (Phase 11).
- Calm mode breathing before scanning.

---

## 16. CLAUDE.md template (put in repo root)

```markdown
# FIND ME! (Unity AR prototype)
- Unity 6000.5.10f1, AR Foundation + ARCore, Android (Galaxy A80), portrait only.
- Full plan: FINDME_BUILD_PLAN.md. Work one phase at a time.
- My folder: Assets/_Team/AR/. Do not edit Assets/_Team/UI or Assets/_Team/AI unless asked.
- Only the AR lead edits the Main scene.
- Never commit Assets/_Team/AI/Secrets/.
- Ask before adding packages. Check AR Foundation version before using anchor APIs.
- Keep scripts small and commented. I want to understand the code: explain key lines after each change.
- After each phase: list changed files, how to test on the phone, known issues.
```

---

## 17. Kickoff prompt for Claude Code

```
Read FINDME_BUILD_PLAN.md and CLAUDE.md. Then:
1. Inspect the project (Packages/manifest.json, ProjectSettings, Assets/_Team) and tell me what's
   already set up vs missing for Phase 0. Include the AR Foundation version and which anchor API to use.
2. Propose the exact file list you will create for Phases 0–2.
3. Wait for my OK, then implement Phase 0 and Phase 1 only.
After implementing, explain the key lines of each new script so I understand them,
and give me the phone test steps for the acceptance criteria.
```

Next prompts (one per session): "Implement Phase 2", "Phase 3", and so on. After Phase 5, run the phone checklist before continuing.