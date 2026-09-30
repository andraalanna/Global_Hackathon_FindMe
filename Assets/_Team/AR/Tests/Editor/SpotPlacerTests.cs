using NUnit.Framework;
using UnityEngine;

namespace FindMe.Tests
{
    public class BoxPointToScreenTests
    {
        // A Galaxy A80-sized portrait screen.
        static readonly CapturePose Pose = new() { screenWidth = 1080, screenHeight = 2340 };
        static readonly int[] FullScreen = { 0, 0, 1000, 1000 };   // [ymin, xmin, ymax, xmax]

        static void AssertPx(Vector2 expected, Vector2 actual) =>
            Assert.That(Vector2.Distance(expected, actual), Is.LessThan(0.01f), $"expected {expected}, got {actual}");

        [Test]
        public void Center_IsScreenCenter()
        {
            AssertPx(new Vector2(540f, 1170f), SpotPlacer.BoxPointToScreen(FullScreen, 0.5f, 0.5f, Pose));
        }

        [Test]
        public void GeminiTopLeft_IsUnityTopLeft_YFlipped()
        {
            // Gemini (0,0) is the TOP-left of the image. Unity screen y=0 is the BOTTOM, so the top is y = height.
            AssertPx(new Vector2(0f, 2340f), SpotPlacer.BoxPointToScreen(FullScreen, 0f, 0f, Pose));
        }

        [Test]
        public void GeminiBottomRight_IsUnityBottomRight()
        {
            AssertPx(new Vector2(1080f, 0f), SpotPlacer.BoxPointToScreen(FullScreen, 1f, 1f, Pose));
        }

        [Test]
        public void TopRightAndBottomLeftCorners()
        {
            AssertPx(new Vector2(1080f, 2340f), SpotPlacer.BoxPointToScreen(FullScreen, 1f, 0f, Pose));
            AssertPx(new Vector2(0f, 0f), SpotPlacer.BoxPointToScreen(FullScreen, 0f, 1f, Pose));
        }

        [Test]
        public void BoxNearTopOfImage_MapsToUpperHalfOfScreen()
        {
            // A shelf in the top 10-30% of the photo: [ymin=100, xmin=200, ymax=300, xmax=400].
            int[] shelf = { 100, 200, 300, 400 };
            Vector2 c = SpotPlacer.BoxPointToScreen(shelf, 0.5f, 0.5f, Pose);

            AssertPx(new Vector2(0.3f * 1080f, 0.8f * 2340f), c);   // x = 30%, y = 1 - 20% = 80% up from bottom
            Assert.Greater(c.y, Pose.screenHeight / 2f, "top of the photo must be the top of the screen");
        }

        [Test]
        public void BoxCorners_MatchBoxEdges()
        {
            int[] box = { 250, 100, 750, 600 };
            AssertPx(new Vector2(108f, 0.75f * 2340f), SpotPlacer.BoxPointToScreen(box, 0f, 0f, Pose));   // top-left
            AssertPx(new Vector2(648f, 0.25f * 2340f), SpotPlacer.BoxPointToScreen(box, 1f, 1f, Pose));   // bottom-right
        }
    }

    public class LabelsSimilarTests
    {
        [TestCase("drawer", "drawer", true)]
        [TestCase("drawer", "top drawer", true)]            // one contains the other
        [TestCase("Drawers", "drawer", true)]               // case + plural
        [TestCase("pile of clothes", "clothes pile", true)] // shared word
        [TestCase("bag", "shelf", false)]
        [TestCase("top shelf", "top drawer", false)]        // "top" is filler, not a match
        [TestCase("under the bed", "under the desk", false)]
        [TestCase("", "bag", false)]
        public void LabelsSimilar(string a, string b, bool expected)
        {
            Assert.AreEqual(expected, SpotPlacer.LabelsSimilar(a, b));
        }
    }
}
