using UnityEngine;

namespace FindMe
{
    /// <summary>
    /// Everything needed to rebuild the camera exactly as it was when a snapshot was taken.
    /// The boxes Gemini returns are relative to that old camera, not the current one.
    /// </summary>
    public struct CapturePose
    {
        public Vector3 position;
        public Quaternion rotation;
        public Matrix4x4 projection;   // ARCore's per-frame projection (FOV + aspect + crop)
        public int screenWidth, screenHeight;
        public float time;
    }

    /// <summary>One captured frame: the JPEG we send, the pose it was taken from, and a small texture for debug.</summary>
    public class Snapshot
    {
        public byte[] jpg;
        public CapturePose pose;
        public Texture2D texture;      // owned by whoever keeps the Snapshot; Destroy() it when replaced
    }
}
