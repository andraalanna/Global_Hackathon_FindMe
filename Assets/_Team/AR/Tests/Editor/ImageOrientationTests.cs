using NUnit.Framework;
using UnityEngine;

namespace FindMe.Tests
{
    public class ImageOrientationTests
    {
        static void AssertNear(Vector2 expected, Vector2 actual) =>
            Assert.That(Vector2.Distance(expected, actual), Is.LessThan(1e-4f), $"expected {expected}, got {actual}");

        [TestCase(ImageUp.Top)]
        [TestCase(ImageUp.Right)]
        [TestCase(ImageUp.Bottom)]
        [TestCase(ImageUp.Left)]
        public void UprightToScreen_And_Back_AreInverses(ImageUp up)
        {
            var p = new Vector2(0.2f, 0.7f);
            AssertNear(p, ImageOrientation.ScreenToUpright(ImageOrientation.UprightToScreen(p, up), up));
        }

        [Test]
        public void UpIsScreenRight_TopOfUprightImage_IsRightEdgeOfScreen()
        {
            // Phone turned so its right edge points at the ceiling: the ceiling (top of the upright photo)
            // is on the screen's right edge.
            AssertNear(new Vector2(1f, 0.5f), ImageOrientation.UprightToScreen(new Vector2(0.5f, 0f), ImageUp.Right));
            // ...and the floor (bottom of the upright photo) is on the screen's left edge.
            AssertNear(new Vector2(0f, 0.5f), ImageOrientation.UprightToScreen(new Vector2(0.5f, 1f), ImageUp.Right));
        }

        [Test]
        public void UpIsScreenLeft_TopOfUprightImage_IsLeftEdgeOfScreen()
        {
            AssertNear(new Vector2(0f, 0.5f), ImageOrientation.UprightToScreen(new Vector2(0.5f, 0f), ImageUp.Left));
            AssertNear(new Vector2(1f, 0.5f), ImageOrientation.UprightToScreen(new Vector2(0.5f, 1f), ImageUp.Left));
        }

        [Test]
        public void Detect_UsesWorldUpInCameraSpace()
        {
            var go = new GameObject("cam");
            try
            {
                go.transform.rotation = Quaternion.identity;                       // held normally
                Assert.AreEqual(ImageUp.Top, ImageOrientation.Detect(go.transform));
                go.transform.rotation = Quaternion.Euler(0f, 0f, 90f);             // rolled 90° CCW: right edge up
                Assert.AreEqual(ImageUp.Right, ImageOrientation.Detect(go.transform));
                go.transform.rotation = Quaternion.Euler(0f, 0f, -90f);            // rolled 90° CW: left edge up
                Assert.AreEqual(ImageUp.Left, ImageOrientation.Detect(go.transform));
                go.transform.rotation = Quaternion.Euler(0f, 0f, 30f);             // tilted a bit: still upright
                Assert.AreEqual(ImageUp.Top, ImageOrientation.Detect(go.transform));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void MakeUpright_MovesPixelsTheSameWayAsBoxes()
        {
            // 4×2 screen image (portrait would be taller, but any size works), one red pixel at the top-right.
            var screen = new Texture2D(4, 2, TextureFormat.RGBA32, false);
            var px = new Color32[8];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 255);
            px[1 * 4 + 3] = new Color32(255, 0, 0, 255);   // row 1 = TOP row (texture rows go bottom-up), column 3 = right
            screen.SetPixels32(px);
            screen.Apply();

            var upright = ImageOrientation.MakeUpright(screen, ImageUp.Right);
            try
            {
                Assert.AreEqual(2, upright.width);
                Assert.AreEqual(4, upright.height);
                // Where should the red pixel be? Ask the same mapping the boxes use.
                Vector2 u = ImageOrientation.ScreenToUpright(new Vector2(3.5f / 4f, 0.5f / 2f), ImageUp.Right);
                int ui = (int)(u.x * upright.width), uj = (int)((1f - u.y) * upright.height);
                Assert.AreEqual(255, upright.GetPixel(ui, uj).r * 255f, 1f, "red pixel must follow the box mapping");
            }
            finally
            {
                Object.DestroyImmediate(screen);
                Object.DestroyImmediate(upright);
            }
        }

        [Test]
        public void BoxPointToScreen_Sideways_TopOfBoxIsRightSideOfScreen()
        {
            var pose = new CapturePose { screenWidth = 1080, screenHeight = 2340, up = ImageUp.Right };
            int[] full = { 0, 0, 1000, 1000 };
            // Top-middle of the upright photo → right-middle of the screen, in Unity pixels.
            Vector2 px = SpotPlacer.BoxPointToScreen(full, 0.5f, 0f, pose);
            Assert.That(Vector2.Distance(new Vector2(1080f, 1170f), px), Is.LessThan(0.01f), px.ToString());
        }

        [Test]
        public void BoxContainsViewportPoint_Sideways()
        {
            // Upright box in the TOP half of the photo. Phone held with right edge up → that's the screen's RIGHT half.
            int[] topHalf = { 0, 0, 500, 1000 };
            Assert.IsTrue(SpotPlacer.BoxContainsViewportPoint(topHalf, new Vector2(0.8f, 0.5f), 0f, ImageUp.Right));
            Assert.IsFalse(SpotPlacer.BoxContainsViewportPoint(topHalf, new Vector2(0.2f, 0.5f), 0f, ImageUp.Right));
        }
    }
}
