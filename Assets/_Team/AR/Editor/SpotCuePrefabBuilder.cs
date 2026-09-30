using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.EditorTools
{
    /// <summary>
    /// FindMe/4. Build SpotCue Prefab: bakes the cue sprites and (re)builds Assets/_Team/AR/Prefabs/SpotCue.prefab.
    /// Layout (plan Phase 6):
    ///   SpotCue (root, at the spot, layer Cues)
    ///     Ground        flat world-space canvas on the surface (24 cm across)
    ///       GroundRing    state-coloured outline
    ///       ProgressRing  radial fill 0→1 = dwell progress
    ///     Stem          18 cm line up to the bubble
    ///     Bubble        world-space canvas; SpotCue turns it to face you and keeps it a constant size
    ///       Panel > Badge (number / icon), Label, Hint;  NextTag
    ///     FoundFx       confetti burst (pink, mint, lemon, lavender squares)
    /// Safe to re-run: the prefab is overwritten; tweak colours/sizes in SpotCue.cs, layout here.
    /// </summary>
    public static class SpotCuePrefabBuilder
    {
        public const string PrefabPath = "Assets/_Team/AR/Prefabs/SpotCue.prefab";
        const string MaterialPath = "Assets/_Team/AR/Materials/CueUnlit.mat";

        static readonly Color Grape = new Color32(0x3A, 0x2E, 0x6B, 0xFF);
        static readonly Color Lemon = new Color32(0xFF, 0xE0, 0x66, 0xFF);

        [MenuItem("FindMe/4. Build SpotCue Prefab")]
        public static void BuildMenu()
        {
            Build();
            Debug.Log($"[FindMe] Built {PrefabPath}");
        }

        public static SpotCue Build()
        {
            CueArtBaker.BakeAll();
            var mat = EnsureMaterial();

            var root = new GameObject("SpotCue");
            var cue = root.AddComponent<SpotCue>();

            // --- Ground: a canvas lying flat on the surface. 240 px × 1 mm/px = 24 cm (12 cm radius).
            var ground = NewCanvas("Ground", root.transform, new Vector2(240f, 240f), 0.001f, 0);
            ground.localRotation = Quaternion.Euler(90f, 0f, 0f);     // canvas faces up
            ground.localPosition = new Vector3(0f, 0.003f, 0f);       // hair above the surface, no z-fighting
            var groundRing = NewImage("GroundRing", ground, CueArtBaker.Ring, Color.white);
            Stretch(groundRing.rectTransform);
            var progress = NewImage("ProgressRing", ground, CueArtBaker.Ring, Color.white);
            Stretch(progress.rectTransform, 30f);                     // a bit smaller: sits inside the outline
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Radial360;
            progress.fillOrigin = (int)Image.Origin360.Top;
            progress.fillClockwise = true;
            progress.fillAmount = 0f;

            // --- Stem: thin line from the ring up to the bubble.
            var stemGo = new GameObject("Stem");
            stemGo.transform.SetParent(root.transform, false);
            var stem = stemGo.AddComponent<LineRenderer>();
            stem.useWorldSpace = false;
            stem.positionCount = 2;
            stem.SetPositions(new[] { Vector3.zero, new Vector3(0f, 0.18f, 0f) });
            stem.widthMultiplier = 0.004f;
            stem.sharedMaterial = mat;
            stem.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            stem.receiveShadows = false;

            // --- Bubble: pivot at the bottom-centre so it grows upward from the stem top.
            var bubble = NewCanvas("Bubble", root.transform, new Vector2(380f, 130f), 0.0005f, 1);
            ((RectTransform)bubble).pivot = new Vector2(0.5f, 0f);
            bubble.localPosition = new Vector3(0f, 0.18f, 0f);
            var group = bubble.gameObject.AddComponent<CanvasGroup>();

            var panel = NewImage("Panel", bubble, CueArtBaker.Bubble, Color.white);
            panel.type = Image.Type.Sliced;
            Stretch(panel.rectTransform);

            var badge = NewImage("Badge", panel.transform, CueArtBaker.Disc, Color.white);
            Place(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(66f, 0f), new Vector2(100f, 100f));
            var badgeText = NewText("BadgeText", badge.transform, 54f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(badgeText.rectTransform);
            var badgeIcon = NewImage("BadgeIcon", badge.transform, CueArtBaker.Check, Grape);
            Place(badgeIcon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(66f, 66f));

            var label = NewText("Label", panel.transform, 44f, FontStyles.Bold, TextAlignmentOptions.BottomLeft);
            label.enableAutoSizing = true;       // "Pile of clothes" shrinks to fit instead of "Pile of c…"
            label.fontSizeMin = 26f;
            label.fontSizeMax = 44f;
            label.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.offsetMin = new Vector2(130f, -4f);
            label.rectTransform.offsetMax = new Vector2(-18f, -10f);
            var hint = NewText("Hint", panel.transform, 30f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            hint.enableAutoSizing = true;
            hint.fontSizeMin = 22f;
            hint.fontSizeMax = 30f;
            hint.rectTransform.anchorMin = new Vector2(0f, 0f);
            hint.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            hint.rectTransform.offsetMin = new Vector2(130f, 10f);
            hint.rectTransform.offsetMax = new Vector2(-18f, 0f);

            var nextTag = NewImage("NextTag", bubble, CueArtBaker.Bubble, Lemon);
            nextTag.type = Image.Type.Sliced;
            Place(nextTag.rectTransform, new Vector2(0f, 1f), new Vector2(75f, 32f), new Vector2(130f, 56f));
            var nextText = NewText("Text", nextTag.transform, 32f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(nextText.rectTransform);
            nextText.text = "Next";
            nextText.color = Grape;

            // --- Found confetti: a one-off burst of little squares.
            var fx = BuildConfetti(root.transform, mat);

            // --- Wire the SpotCue's private fields.
            var so = new SerializedObject(cue);
            so.FindProperty("groundRing").objectReferenceValue = groundRing;
            so.FindProperty("progressRing").objectReferenceValue = progress;
            so.FindProperty("stem").objectReferenceValue = stem;
            so.FindProperty("bubble").objectReferenceValue = bubble;
            so.FindProperty("bubbleGroup").objectReferenceValue = group;
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("badgeIcon").objectReferenceValue = badgeIcon;
            so.FindProperty("badgeText").objectReferenceValue = badgeText;
            so.FindProperty("labelText").objectReferenceValue = label;
            so.FindProperty("hintText").objectReferenceValue = hint;
            so.FindProperty("nextTag").objectReferenceValue = nextTag.gameObject;
            so.FindProperty("foundFx").objectReferenceValue = fx;
            so.FindProperty("checkIcon").objectReferenceValue = CueArtBaker.Check;
            so.FindProperty("starIcon").objectReferenceValue = CueArtBaker.Star;
            so.ApplyModifiedPropertiesWithoutUndo();

            int layer = LayerMask.NameToLayer(FindMeProjectSetup.CuesLayer);
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<SpotCue>();
        }

        static Material EnsureMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat != null) return mat;
            // Unlit, alpha-blended, vertex-coloured: fine for the stem line and confetti squares in URP.
            mat = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(mat, MaterialPath);
            return mat;
        }

        static Transform NewCanvas(string name, Transform parent, Vector2 size, float scale, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = order;                 // bubble draws over its own ring
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one * scale;
            return rt;
        }

        static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;                   // cues aren't tapped; skip UI raycasts
            return img;
        }

        static TextMeshProUGUI NewText(string name, Transform parent, float size, FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = Grape;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;  // long labels end in "…" instead of spilling out
            t.raycastTarget = false;
            return t;
        }

        static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static ParticleSystem BuildConfetti(Transform parent, Material mat)
        {
            var go = new GameObject("FoundFx");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = 1.2f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.03f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI);
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            var colors = new Gradient();
            colors.SetKeys(
                new[] { new GradientColorKey(new Color32(0xFF, 0x7E, 0xB6, 255), 0f), new GradientColorKey(new Color32(0x4F, 0xE3, 0xA8, 255), 0.33f),
                        new GradientColorKey(Lemon, 0.66f), new GradientColorKey(new Color32(0xC3, 0xB1, 0xFF, 255), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;                          // untextured quads = little squares
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }
    }
}
