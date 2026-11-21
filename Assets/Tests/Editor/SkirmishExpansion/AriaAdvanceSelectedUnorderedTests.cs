using Game.Simulation;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // W41a (S002 @130365 FindBase loop): the open full map presents the parked column, so the global quorum
    // (Close >= 4) vetoed the advance and the open map never offered its drag, while the selected groups
    // at home had no accepted Approach order. The ordinary branch now also prefers the advance when a
    // selected assault group is unordered. Industrial Basin keeps its own rule.
    public sealed class AriaAdvanceSelectedUnorderedTests
    {
        static AriaAssaultDistances Distances(int close, int assault = 0, int required = 4) =>
            new AriaAssaultDistances(new AriaGroundPosition(0, 0, 0), close > 0 ? 100f : float.MaxValue, close, assault, required);

        // close, selected, assault, accepted -> expected (ordinary map)
        [TestCase(4, 0b11000, 0b11111, 0b00111, true)] // Quorum met, selected assault groups unordered -> advance
        [TestCase(6, 0b00001, 0b00001, 0b00000, true)] // Quorum met, one selected unordered -> advance
        [TestCase(4, 0b11000, 0b11111, 0b11000, false)] // Quorum met, selected groups all accepted -> unchanged false
        [TestCase(8, 0b00000, 0b11111, 0b00000, false)] // Quorum met, nothing selected -> unchanged false
        [TestCase(5, 0b10000, 0b01111, 0b00000, false)] // Quorum met, selected group is not assault -> unchanged false
        [TestCase(3, 0b00000, 0b11111, 0b00000, true)] // Quorum not met, nothing selected -> unchanged true
        [TestCase(0, 0b11000, 0b11111, 0b11000, true)] // Quorum not met, selected accepted -> unchanged true
        [TestCase(3, 0b11000, 0b11111, 0b00000, true)] // Quorum not met, selected unordered -> unchanged true
        public void OrdinaryMapAdvancesWhileSelectedAssaultGroupsAreUnordered(int close, int selected, int assault, int accepted, bool expected)
        {
            var d = Distances(close);
            Assert.AreEqual(expected, AriaGroundApproachPolicy.AdvancePreferred(false, d, selected, assault, accepted));
            Assert.AreEqual((selected & assault & ~accepted) != 0, AriaGroundApproachPolicy.SelectedUnordered(selected, assault, accepted));
            if (accepted == (selected & assault) || selected == 0)
                Assert.AreEqual(AriaGroundApproachPolicy.AdvancePreferred(false, d), AriaGroundApproachPolicy.AdvancePreferred(false, d, selected, assault, accepted),
                    "no unordered selected assault group: the original quorum rule decides");
        }

        [Test]
        public void IndustrialBasinIsUnchanged()
        {
            int[] masks = { 0, 1, 0b11000, 0b11111 };
            for (int close = 0; close <= 10; close++)
            for (int assault = 0; assault <= 10; assault += 5)
            for (int required = 1; required <= 8; required += 7)
            foreach (int selected in masks)
            foreach (int assaultMask in masks)
            foreach (int accepted in masks)
            {
                var d = Distances(close, assault, required);
                Assert.AreEqual(AriaGroundApproachPolicy.AdvancePreferred(true, d),
                    AriaGroundApproachPolicy.AdvancePreferred(true, d, selected, assaultMask, accepted),
                    $"IB close={close} assault={assault} required={required} selected={selected} assaultMask={assaultMask} accepted={accepted}");
            }
        }

        [Test]
        public void OrdinaryRuleNeverRefusesWhatTheQuorumAllowed()
        {
            int[] masks = { 0, 1, 0b11000, 0b11111 };
            for (int close = 0; close <= 10; close++)
            foreach (int selected in masks)
            foreach (int assaultMask in masks)
            foreach (int accepted in masks)
            {
                var d = Distances(close);
                if (AriaGroundApproachPolicy.AdvancePreferred(false, d))
                    Assert.IsTrue(AriaGroundApproachPolicy.AdvancePreferred(false, d, selected, assaultMask, accepted));
            }
        }
    }
}
