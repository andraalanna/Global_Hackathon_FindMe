using NUnit.Framework;

namespace FindMe.Tests
{
    public class SearchSessionTests
    {
        [TestCase(161f, 4, "Found in 2m 41s · 4 spots checked")]   // the plan's own example
        [TestCase(72f, 1, "Found in 1m 12s · 1 spot checked")]
        [TestCase(45.4f, 0, "Found in 45s · 0 spots checked")]
        [TestCase(59.6f, 2, "Found in 1m 0s · 2 spots checked")]    // rounds, and rolls into minutes
        public void FormatSummary(float seconds, int checkedCount, string expected) =>
            Assert.AreEqual(expected, SearchSession.FormatSummary(seconds, checkedCount));

        [Test]
        public void CheckedCount_CountsCheckedAndFound_NotOpenSpots()
        {
            var s = new SearchSession();
            s.spots.Add(new SearchSession.SpotRecord { state = SpotState.Checked });
            s.spots.Add(new SearchSession.SpotRecord { state = SpotState.Found });
            s.spots.Add(new SearchSession.SpotRecord { state = SpotState.ToCheck });
            s.spots.Add(new SearchSession.SpotRecord { state = SpotState.NeedsOpen });
            Assert.AreEqual(2, s.CheckedCount);
        }
    }
}
