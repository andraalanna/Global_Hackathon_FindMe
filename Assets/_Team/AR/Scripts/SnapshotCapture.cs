using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FindMe
{
    /// <summary>
    /// Captures what the camera sees WITHOUT our cues/UI, downscales it to a JPEG, and records the exact
    /// camera pose it was taken from (plan Phase 3). Gemini's boxes are relative to this image, and
    /// GhostCamera uses the pose later to turn those boxes back into 3D rays.
    /// </summary>
    public class SnapshotCapture : MonoBehaviour
    {
        /// <summary>True while a capture is in progress. IMGUI dev screens check this and draw nothing.</summary>
        public static bool IsCapturing { get; private set; }

        [SerializeField] Camera arCamera;
        [Tooltip("Layers the AR camera stops rendering during capture (our 3D cues and overlays).")]
        [SerializeField] string[] hiddenLayers = { "Cues", "Overlay" };
        [SerializeField, Range(1, 100)] int jpgQuality = 70;
        [SerializeField] float flashSeconds = 0.12f;
        [Tooltip("Optional shutter sound. Section 8 (FeedbackFx) will own sounds later.")]
        [SerializeField] AudioSource shutterSound;
        [Tooltip("Measures surface distances at photo time, so boxes can be placed in 3D exactly.")]
        [SerializeField] SurfaceGridSampler surfaceGrid;

        public bool IsBusy { get; private set; }

        readonly List<Canvas> hiddenCanvases = new();
        int savedCullingMask;
        float flashUntil;

        void Awake()
        {
            if (arCamera == null) arCamera = Camera.main;
        }

        /// <summary>
        /// Coroutine. Calls <paramref name="onDone"/> with the snapshot, or with null if a capture is already running.
        /// The caller owns <see cref="Snapshot.texture"/> and must Destroy it when done.
        /// </summary>
        public IEnumerator Capture(int targetWidth, bool flash, Action<Snapshot> onDone)
        {
            if (IsBusy) { onDone?.Invoke(null); yield break; }
            IsBusy = true;

            HideOverlays();
            // Wait one full frame so the NEXT rendered frame is drawn without cues/UI. If we only waited for
            // end of this frame, the 3D cues were already rendered before we changed the culling mask.
            yield return null;
            // Pitfall 5: the back buffer is only complete after all rendering, i.e. at end of frame.
            yield return new WaitForEndOfFrame();

            // Record the pose at the same moment as the pixels, so image and camera match exactly.
            // (The plan records it before hiding; that would be one frame older than the image.)
            var pose = new CapturePose
            {
                position = arCamera.transform.position,
                rotation = arCamera.transform.rotation,
                projection = arCamera.projectionMatrix,   // ARCameraManager sets this from ARCore every frame
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                // Held sideways? Then Gemini gets an upright copy (people and models read rooms ceiling-up).
                up = ImageOrientation.Detect(arCamera.transform),
                time = Time.time,
            };
            Texture2D full = ScreenCapture.CaptureScreenshotAsTexture();
            // Same frame as the pixels and the pose: the grid describes exactly this photo.
            SurfaceGrid grid = surfaceGrid != null ? surfaceGrid.Sample() : null;
            RestoreOverlays();

            // Flash + sound only now, after the pixels are safely captured.
            if (flash)
            {
                flashUntil = Time.unscaledTime + flashSeconds;
                if (shutterSound != null) shutterSound.Play();
            }

            Texture2D small = Downscale(full, targetWidth);
            Destroy(full);
            Texture2D upright = ImageOrientation.MakeUpright(small, pose.up);
            if (upright != small) { Destroy(small); small = upright; }
            byte[] jpg = small.EncodeToJPG(jpgQuality);

            IsBusy = false;
            onDone?.Invoke(new Snapshot { jpg = jpg, pose = pose, texture = small, grid = grid });
        }

        void HideOverlays()
        {
            savedCullingMask = arCamera.cullingMask;
            arCamera.cullingMask &= ~LayerMask.GetMask(hiddenLayers);

            // Screen-space canvases (the UI lead's UI) aren't affected by the culling mask; switch them off.
            // World-space canvases (cue bubbles) sit on the Cues layer and are already hidden by the mask.
            hiddenCanvases.Clear();
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            {
                if (!c.enabled || !c.isRootCanvas || c.renderMode == RenderMode.WorldSpace) continue;
                c.enabled = false;
                hiddenCanvases.Add(c);
            }
            IsCapturing = true;
        }

        void RestoreOverlays()
        {
            arCamera.cullingMask = savedCullingMask;
            foreach (var c in hiddenCanvases) if (c != null) c.enabled = true;
            hiddenCanvases.Clear();
            IsCapturing = false;
        }

        void OnDisable()
        {
            if (IsCapturing) RestoreOverlays();   // never leave the UI hidden if we're stopped mid-capture
            IsBusy = false;
        }

        /// <summary>GPU resize: Blit into a small RenderTexture, then read it back into a Texture2D.</summary>
        static Texture2D Downscale(Texture2D src, int targetWidth)
        {
            int w = Mathf.Min(targetWidth, src.width);
            int h = Mathf.RoundToInt(w * (float)src.height / src.width);   // keep the screen's aspect ratio

            // Same colour space in and out, so Blit doesn't brighten/darken the image (project is Linear).
            var rw = src.isDataSRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, rw);
            Graphics.Blit(src, rt);

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var dst = new Texture2D(w, h, TextureFormat.RGB24, false, !src.isDataSRGB);
            dst.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            dst.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return dst;
        }

        // White shutter flash, drawn on top of everything.
        void OnGUI()
        {
            if (Time.unscaledTime >= flashUntil) return;
            GUI.depth = -1000;
            float a = (flashUntil - Time.unscaledTime) / flashSeconds;   // fades 1 → 0
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
