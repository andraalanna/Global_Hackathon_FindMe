using UnityEditor;
using UnityEngine;

namespace FindMe.Sim
{
    /// <summary>
    /// Builds a small furnished living room as an XR Simulation environment (menu FindMe ▸ Sim ▸ Build living room).
    /// The grey default room has nothing to search; this one has the places FIND ME looks: sofa + cushions,
    /// a pile of clothes, a desk with a drawer, a bag on the floor, a shelf, a coffee table, a side table.
    /// Units are metres. The phone starts 1.4 m up, 1 m in from the front wall, looking at the sofa wall.
    /// </summary>
    public static class LivingRoomBuilder
    {
        const string Folder = "Assets/_Team/AR/Simulation";
        public const string PrefabPath = Folder + "/FindMeLivingRoom.prefab";

        [MenuItem("FindMe/Sim/Build living room environment")]
        public static GameObject Build()
        {
            var root = new GameObject("FindMeLivingRoom");
            // SimulationEnvironment is internal to AR Foundation, so it's added by name; its fields are set below
            // through SerializedObject, exactly like editing them in the Inspector.
            var envType = System.Type.GetType("UnityEngine.XR.Simulation.SimulationEnvironment, Unity.XR.Simulation");
            var env = root.AddComponent(envType);

            // Room shell: 6 m wide, 6 m deep, 2.6 m high.
            var wall = Mat("Wall", new Color32(0xEC, 0xE6, 0xF7, 255));
            var floor = Mat("Floor", new Color32(0xC9, 0xB2, 0x96, 255));
            Box(root, "Floor", floor, new Vector3(0, -0.025f, 0.5f), new Vector3(6, 0.05f, 6));
            Box(root, "Ceiling", wall, new Vector3(0, 2.625f, 0.5f), new Vector3(6, 0.05f, 6));
            Box(root, "WallBack", wall, new Vector3(0, 1.3f, 3.5f), new Vector3(6, 2.6f, 0.1f));
            Box(root, "WallFront", wall, new Vector3(0, 1.3f, -2.5f), new Vector3(6, 2.6f, 0.1f));
            Box(root, "WallLeft", wall, new Vector3(-3, 1.3f, 0.5f), new Vector3(0.1f, 2.6f, 6));
            Box(root, "WallRight", wall, new Vector3(3, 1.3f, 0.5f), new Vector3(0.1f, 2.6f, 6));
            Box(root, "Rug", Mat("Rug", new Color32(0x9A, 0x84, 0xEA, 255)), new Vector3(-0.4f, 0.005f, 1.8f), new Vector3(2.2f, 0.01f, 1.6f));

            // Sofa against the back wall, with two cushions and a pile of clothes on the left end.
            var sofa = Mat("Sofa", new Color32(0x7A, 0x63, 0xB8, 255));
            var cushion = Mat("Cushion", new Color32(0x8D, 0x77, 0xCC, 255));
            var s = Group(root, "Sofa", new Vector3(-0.6f, 0, 2.95f));
            Box(s, "Base", sofa, new Vector3(0, 0.22f, 0), new Vector3(2.0f, 0.44f, 0.9f));
            Box(s, "Back", sofa, new Vector3(0, 0.7f, 0.35f), new Vector3(2.0f, 0.55f, 0.2f));
            Box(s, "ArmL", sofa, new Vector3(-1.05f, 0.55f, 0), new Vector3(0.15f, 0.3f, 0.9f));
            Box(s, "ArmR", sofa, new Vector3(1.05f, 0.55f, 0), new Vector3(0.15f, 0.3f, 0.9f));
            Box(s, "CushionL", cushion, new Vector3(-0.48f, 0.5f, -0.05f), new Vector3(0.92f, 0.13f, 0.75f));
            Box(s, "CushionR", cushion, new Vector3(0.48f, 0.5f, -0.05f), new Vector3(0.92f, 0.13f, 0.75f));
            var clothes = Mat("Clothes", new Color32(0x6C, 0xC4, 0xFF, 255));
            Blob(s, "ClothesPile1", clothes, new Vector3(-0.65f, 0.62f, -0.05f), new Vector3(0.45f, 0.14f, 0.38f));
            Blob(s, "ClothesPile2", Mat("Clothes2", new Color32(0xFF, 0xA7, 0x7A, 255)), new Vector3(-0.5f, 0.7f, 0.0f), new Vector3(0.32f, 0.1f, 0.28f));

            // Coffee table on the rug.
            var wood = Mat("Wood", new Color32(0xA9, 0x8A, 0x6A, 255));
            var t = Group(root, "CoffeeTable", new Vector3(-0.5f, 0, 1.75f));
            Box(t, "Top", wood, new Vector3(0, 0.42f, 0), new Vector3(1.0f, 0.05f, 0.55f));
            foreach (var p in new[] { new Vector2(-0.45f, -0.22f), new Vector2(0.45f, -0.22f), new Vector2(-0.45f, 0.22f), new Vector2(0.45f, 0.22f) })
                Box(t, "Leg", wood, new Vector3(p.x, 0.2f, p.y), new Vector3(0.05f, 0.4f, 0.05f));
            Box(t, "Mug", Mat("Mug", Color.white), new Vector3(0.25f, 0.5f, 0.05f), new Vector3(0.08f, 0.1f, 0.08f));

            // Desk on the right with a drawer under the top, and a bag on the floor beside it.
            var d = Group(root, "Desk", new Vector3(1.75f, 0, 2.7f));
            Box(d, "Top", wood, new Vector3(0, 0.75f, 0), new Vector3(1.2f, 0.05f, 0.6f));
            Box(d, "Drawer", Mat("Drawer", new Color32(0x86, 0x69, 0x4D, 255)), new Vector3(-0.25f, 0.63f, -0.02f), new Vector3(0.5f, 0.17f, 0.55f));
            Box(d, "Handle", Mat("Metal", new Color32(0x3A, 0x2E, 0x6B, 255)), new Vector3(-0.25f, 0.63f, -0.31f), new Vector3(0.14f, 0.025f, 0.02f));
            foreach (var p in new[] { new Vector2(-0.57f, -0.27f), new Vector2(0.57f, -0.27f), new Vector2(-0.57f, 0.27f), new Vector2(0.57f, 0.27f) })
                Box(d, "Leg", wood, new Vector3(p.x, 0.37f, p.y), new Vector3(0.05f, 0.73f, 0.05f));
            Box(d, "Laptop", Mat("Laptop", new Color32(0x65, 0x5A, 0x8A, 255)), new Vector3(0.25f, 0.79f, 0), new Vector3(0.34f, 0.02f, 0.24f));

            var bag = Mat("Bag", new Color32(0xFF, 0x7E, 0xB6, 255));
            var b = Group(root, "Bag", new Vector3(0.75f, 0, 2.15f));
            Box(b, "Body", bag, new Vector3(0, 0.16f, 0), new Vector3(0.42f, 0.32f, 0.2f));
            Box(b, "Flap", Mat("BagFlap", new Color32(0xD9, 0x55, 0x9A, 255)), new Vector3(0, 0.27f, -0.105f), new Vector3(0.42f, 0.12f, 0.02f));
            Box(b, "Strap", bag, new Vector3(0, 0.42f, 0), new Vector3(0.32f, 0.03f, 0.03f));
            // The lost wallet (11 × 9 cm, lying flat on the floor just in front of the bag): the ground truth
            // for the "I see it!" marker accuracy test.
            Box(b, "Wallet", Mat("Wallet", new Color32(0x2B, 0x22, 0x50, 255)), new Vector3(0.05f, 0.01f, -0.28f), new Vector3(0.11f, 0.02f, 0.09f));

            // Shelf on the back wall with books, side table with a lamp next to the sofa.
            var sh = Group(root, "Shelf", new Vector3(1.0f, 0, 3.3f));
            Box(sh, "Board", wood, new Vector3(0, 1.6f, 0), new Vector3(1.2f, 0.04f, 0.3f));
            Color32[] books = { new(0xFF, 0xE4, 0x5C, 255), new(0x6C, 0xC4, 0xFF, 255), new(0xC3, 0xB1, 0xFF, 255), new(0x4F, 0xE3, 0xA8, 255) };
            for (int i = 0; i < books.Length; i++)
                Box(sh, "Book", Mat("Book" + i, books[i]), new Vector3(-0.45f + i * 0.09f, 1.73f, 0), new Vector3(0.07f, 0.22f + (i % 2) * 0.05f, 0.2f));

            var st = Group(root, "SideTable", new Vector3(0.75f, 0, 3.0f));
            Box(st, "Top", wood, new Vector3(0, 0.55f, 0), new Vector3(0.45f, 0.04f, 0.45f));
            Box(st, "Body", Mat("SideBody", new Color32(0x6B, 0x57, 0x78, 255)), new Vector3(0, 0.27f, 0), new Vector3(0.4f, 0.53f, 0.4f));
            Box(st, "Lamp", Mat("Lamp", new Color32(0xFF, 0xE4, 0x5C, 255)), new Vector3(0, 0.78f, 0), new Vector3(0.22f, 0.22f, 0.22f));

            // Coat hook by the door on the left wall.
            Box(root, "CoatHook", Mat("Metal", new Color32(0x3A, 0x2E, 0x6B, 255)), new Vector3(-2.9f, 1.7f, 0.2f), new Vector3(0.08f, 0.05f, 0.4f));
            Box(root, "Coat", Mat("Coat", new Color32(0xFF, 0x7E, 0xB6, 255)), new Vector3(-2.85f, 1.25f, 0.2f), new Vector3(0.12f, 0.8f, 0.45f));

            // Light: a soft sun through a window plus a warm room light.
            var sun = new GameObject("Sun", typeof(Light));
            sun.transform.SetParent(root.transform, false);
            sun.transform.rotation = Quaternion.Euler(50, 30, 0);
            var sl = sun.GetComponent<Light>();
            sl.type = LightType.Directional;
            sl.intensity = 1.1f;
            var lamp = new GameObject("RoomLight", typeof(Light));
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localPosition = new Vector3(0, 2.3f, 1.2f);
            var ll = lamp.GetComponent<Light>();
            ll.type = LightType.Point;
            ll.range = 8f;
            ll.intensity = 1.5f;

            // Simulation settings: where the phone starts, and where it may move (inside the room).
            var so = new SerializedObject(env);
            so.FindProperty("m_CameraStartingPose").FindPropertyRelative("position").vector3Value = new Vector3(0, 1.4f, -1.0f);
            so.FindProperty("m_CameraStartingPose").FindPropertyRelative("rotation").quaternionValue = Quaternion.identity;
            so.FindProperty("m_CameraMovementBounds").boundsValue = new Bounds(new Vector3(0, 1.3f, 0.5f), new Vector3(5.6f, 2.4f, 5.6f));
            so.FindProperty("m_DefaultViewPose").FindPropertyRelative("position").vector3Value = new Vector3(0, 3.5f, -3f);
            so.FindProperty("m_DefaultViewPose").FindPropertyRelative("rotation").quaternionValue = Quaternion.Euler(35, 0, 0);
            so.FindProperty("m_DefaultViewPivot").vector3Value = new Vector3(0, 0.5f, 1.5f);
            so.FindProperty("m_DefaultViewSize").floatValue = 5f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[Sim] Saved " + PrefabPath);
            return prefab;
        }

        static GameObject Group(GameObject parent, string name, Vector3 pos)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent.transform, false);
            g.transform.localPosition = pos;
            return g;
        }

        static GameObject Box(GameObject parent, string name, Material m, Vector3 pos, Vector3 size) =>
            Prim(PrimitiveType.Cube, parent, name, m, pos, size);

        static GameObject Blob(GameObject parent, string name, Material m, Vector3 pos, Vector3 size) =>
            Prim(PrimitiveType.Sphere, parent, name, m, pos, size);

        static GameObject Prim(PrimitiveType type, GameObject parent, string name, Material m, Vector3 pos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());   // the simulation adds its own mesh colliders
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>A URP Lit material saved next to the prefab (reused if it already exists).</summary>
        static Material Mat(string name, Color c)
        {
            string path = $"{Folder}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                System.IO.Directory.CreateDirectory($"{Folder}/Materials");
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
