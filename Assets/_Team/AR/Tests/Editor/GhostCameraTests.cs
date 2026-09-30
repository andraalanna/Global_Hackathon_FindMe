using NUnit.Framework;
using UnityEngine;

namespace FindMe.Tests
{
    public class GhostCameraTests
    {
        GameObject go;
        GhostCamera ghost;
        CapturePose pose;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("GhostTest", typeof(Camera), typeof(GhostCamera));
            ghost = go.GetComponent<GhostCamera>();

            // A phone-like portrait pose: 1080×2340 (bigger than any Editor screen on purpose), 60° vertical FOV,
            // at an arbitrary spot, tilted down and turned, so nothing lines up with world axes by accident.
            pose = new CapturePose
            {
                position = new Vector3(1f, 1.4f, -2f),
                rotation = Quaternion.Euler(20f, 35f, 0f),
                projection = Matrix4x4.Perspective(60f, 1080f / 2340f, 0.1f, 20f),
                screenWidth = 1080,
                screenHeight = 2340,
            };
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(go);

        [Test]
        public void CenterRay_EqualsCameraForward()
        {
            Ray ray = ghost.ScreenRay(pose, new Vector2(540f, 1170f));
            Vector3 forward = pose.rotation * Vector3.forward;

            Assert.Less(Vector3.Angle(ray.direction, forward), 0.01f, "center ray must point straight ahead");
            // The ray starts at the near plane, on the camera's forward line.
            Vector3 fromCam = ray.origin - pose.position;
            Assert.Less(Vector3.Cross(forward, fromCam).magnitude, 1e-4f, "center ray must start on the forward axis");
        }

        [Test]
        public void TopCenterRay_IsHalfVerticalFovAboveForward()
        {
            Ray ray = ghost.ScreenRay(pose, new Vector2(540f, 2340f));   // top edge, Unity screen (bottom-left origin)
            Vector3 forward = pose.rotation * Vector3.forward;
            Vector3 up = pose.rotation * Vector3.up;

            Assert.AreEqual(30f, Vector3.Angle(ray.direction, forward), 0.01f);
            Assert.Greater(Vector3.Dot(ray.direction, up), 0f, "top of the screen must be above forward");
        }

        [Test]
        public void RayThroughProjectedPoint_HitsThatPoint()
        {
            // 2 m in front of the camera, a bit right and down: off-centre but inside the view.
            Vector3 world = pose.position + pose.rotation * new Vector3(0.2f, -0.5f, 2f);
            Vector3 vp = ghost.WorldToViewport(pose, world);
            Assert.That(vp.z > 0f && vp.x is > 0f and < 1f && vp.y is > 0f and < 1f, "test point must be in view");

            var px = new Vector2(vp.x * pose.screenWidth, vp.y * pose.screenHeight);
            Ray ray = ghost.ScreenRay(pose, px);

            float distToRay = Vector3.Cross(ray.direction, world - ray.origin).magnitude;
            Assert.Less(distToRay, 1e-3f, "pixel → ray must pass back through the original 3D point");
        }
    }
}
