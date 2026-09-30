using System.Collections.Generic;
using NUnit.Framework;

namespace FindMe.Tests
{
    public class SortAndCapTests
    {
        static HidingSpot Spot(string label, int likelihood, int[] box = null) =>
            new() { label = label, likelihood = likelihood, box_2d = box ?? new[] { 100, 100, 200, 200 } };

        [Test]
        public void SortsByLikelihood_KeepsOrderOnTies_AndCaps()
        {
            var spots = new List<HidingSpot>
            {
                Spot("shelf", 1), Spot("drawer", 2), Spot("pile", 3), Spot("bag", 2),
                Spot("desk", 1), Spot("coat", 3),
            };
            var result = SearchController.SortAndCap(spots, 5);

            CollectionAssert.AreEqual(new[] { "pile", "coat", "drawer", "bag", "shelf" }, result.ConvertAll(s => s.label));
        }

        [Test]
        public void DropsSpotsWithoutBoxOrLabel_AndFixesFlippedBoxes()
        {
            var spots = new List<HidingSpot>
            {
                Spot("no box", 3, new[] { 1, 2, 3 }),
                Spot("", 3),
                null,
                Spot("flipped", 2, new[] { 800, 900, 200, 1200 }),
            };
            var result = SearchController.SortAndCap(spots, 5);

            Assert.AreEqual(1, result.Count);
            CollectionAssert.AreEqual(new[] { 200, 900, 800, 1000 }, result[0].box_2d);   // swapped + clamped
        }
    }
}
