using NUnit.Framework;
using UnityEngine;

namespace FindMe.Tests
{
    /// <summary>The "things rest on surfaces" guess: where a downward ray meets the floor height.</summary>
    public class SpotLocatorTests
    {
        [Test]
        public void ChestHeightLookingDown30Degrees_MeetsFloorAt2Point8m()
        {
            // Phone 1.4 m above the floor, tilted 30° down: the floor is 1.4 / sin(30°) = 2.8 m along the ray.
            var ray = new Ray(new Vector3(0f, 1.4f, 0f), Quaternion.Euler(30f, 0f, 0f) * Vector3.forward);
            Assert.IsTrue(SpotLocator.TryIntersectHorizontal(ray, 0f, 0.3f, 6f, out float t));
            Assert.AreEqual(2.8f, t, 0.01f);
            // The old fixed 1.5 m guess would have been ~0.65 m above the floor: the "floating bag" bug.
            Assert.Greater(ray.GetPoint(1.5f).y, 0.6f);
        }

        [Test]
        public void LookingLevelOrUp_NeverMeetsAFloor()
        {
            var level = new Ray(new Vector3(0f, 1.4f, 0f), Vector3.forward);
            var up = new Ray(new Vector3(0f, 1.4f, 0f), Quaternion.Euler(-20f, 0f, 0f) * Vector3.forward);
            Assert.IsFalse(SpotLocator.TryIntersectHorizontal(level, 0f, 0.3f, 6f, out _));
            Assert.IsFalse(SpotLocator.TryIntersectHorizontal(up, 0f, 0.3f, 6f, out _));
        }

        [Test]
        public void FloorTooFarAway_IsRejected()
        {
            // Looking 10° down: the floor is 1.4 / sin(10°) ≈ 8 m away, beyond the 6 m limit.
            // A tiny tilt error there moves the point by metres; better to keep guessing than trust it.
            var ray = new Ray(new Vector3(0f, 1.4f, 0f), Quaternion.Euler(10f, 0f, 0f) * Vector3.forward);
            Assert.IsFalse(SpotLocator.TryIntersectHorizontal(ray, 0f, 0.3f, 6f, out _));
        }
    }
}
