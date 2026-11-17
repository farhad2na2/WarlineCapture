using Game.UI.Runtime;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Roster tray paging (S002 gate 3, 104731): 11 groups in a 5-card viewport. Page 1 aligns at
    // 5 card strides but the last page is clamped to the maximum scroll (6 strides), so page 1 owns
    // only the band 2.5..5.5 strides. ARIA's page swipe drags 0.8 viewport (about 4 strides) and the
    // ScrollRect adds inertia, so it lands past 5.5 (page 2) going forward and below 2.5 (page 0)
    // going back: the page 0 -> 2 -> 0 loop. ARIA now taps the tray's page chevron (exactly one page).
    public sealed class MatchHudSquadTrayPagingTests
    {
        const float Stride = 100f;
        const float Maximum = 6f * Stride; // 11 cards, 5 visible

        [Test]
        public void PageOneIsOneCardStrideWideAndSwipesSkipIt()
        {
            Assert.AreEqual(0, MatchHudSquadTouchNavigation.NearestPage(0f, 11, Stride, Maximum));
            Assert.AreEqual(1, MatchHudSquadTouchNavigation.NearestPage(5f * Stride, 11, Stride, Maximum));
            Assert.AreEqual(2, MatchHudSquadTouchNavigation.NearestPage(5.6f * Stride, 11, Stride, Maximum), "a 4-stride swipe plus 1.6 strides of inertia from page 0");
            Assert.AreEqual(0, MatchHudSquadTouchNavigation.NearestPage(6f * Stride - 3.7f * Stride, 11, Stride, Maximum), "the same swipe back from page 2");
            Assert.AreEqual(1, MatchHudSquadTouchNavigation.NearestPage(2.6f * Stride, 11, Stride, Maximum));
            Assert.AreEqual(1, MatchHudSquadTouchNavigation.NearestPage(5.4f * Stride, 11, Stride, Maximum));
        }

        [Test]
        public void PresentedChevronAlwaysWinsOverASwipe()
        {
            Assert.IsTrue(MatchHudSquadTouchNavigation.PreferPageButton(true));
            Assert.IsFalse(MatchHudSquadTouchNavigation.PreferPageButton(false), "swipe fallback when no chevron is shown");
        }
    }
}
