using NUnit.Framework;

namespace FindMe.Tests
{
    public class SpotFocusTests
    {
        [Test]
        public void NoCurrentFocus_TakesAnyCandidate() =>
            Assert.IsTrue(SpotFocus.ShouldSwitch(false, 0f, 13f, 3f));

        [Test]
        public void SlightlyCloserCandidate_DoesNotStealFocus() =>
            Assert.IsFalse(SpotFocus.ShouldSwitch(true, 8f, 6f, 3f));    // only 2° closer: stay, no flicker

        [Test]
        public void ClearlyCloserCandidate_TakesFocus() =>
            Assert.IsTrue(SpotFocus.ShouldSwitch(true, 8f, 5f, 3f));     // 3° closer: switch
    }
}
