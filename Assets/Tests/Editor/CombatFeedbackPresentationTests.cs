using Game.Rendering;
using Game.Runtime;

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;

public sealed class CombatFeedbackPresentationTests
{
    [Test]
    public void PlayerEnemyExchange_RequiresPlayerAndHostile()
    {
        Assert.IsTrue(CombatCameraShakeHelper.IsPlayerEnemyExchange(1, 2));
        Assert.IsTrue(CombatCameraShakeHelper.IsPlayerEnemyExchange(3, 1));
        Assert.IsFalse(CombatCameraShakeHelper.IsPlayerEnemyExchange(1, 1));
        Assert.IsFalse(CombatCameraShakeHelper.IsPlayerEnemyExchange(2, 3));
        Assert.IsFalse(CombatCameraShakeHelper.IsPlayerEnemyExchange(0, 2));
    }

    [Test]
    public void ContactTrauma_OrdinaryFireIsQuietAndBaseHitsScaleBounded()
    {
        Assert.AreEqual(0f, CombatCameraShakeHelper.ResolveContactTrauma(false, false, 50, 1000));
        Assert.AreEqual(CombatCameraShakeHelper.FirstContactTrauma,
            CombatCameraShakeHelper.ResolveContactTrauma(true, false, 5, 100));
        float light = CombatCameraShakeHelper.ResolveContactTrauma(false, true, 1, 1000);
        float heavy = CombatCameraShakeHelper.ResolveContactTrauma(false, true, 400, 1000);
        float huge = CombatCameraShakeHelper.ResolveContactTrauma(false, true, 1000, 1000);
        Assert.AreEqual(CombatCameraShakeHelper.MainBaseHitMinimumTrauma, light, 0.01f);
        Assert.Greater(heavy, light);
        Assert.AreEqual(CombatCameraShakeHelper.MainBaseHitMaximumTrauma, huge, 0.0001f);
        Assert.AreEqual(0f, CombatCameraShakeHelper.ResolveContactTrauma(false, true, 0, 1000));
    }

    [Test]
    public void OrderMarkerPop_StartsEnlargedAndSettlesToRestingScale()
    {
        Assert.AreEqual(OrderMarkerPopMotion.PeakScale, OrderMarkerPopMotion.Scale(0f), 0.0001f);
        Assert.Less(OrderMarkerPopMotion.Scale(0.15f), OrderMarkerPopMotion.PeakScale);
        Assert.Greater(OrderMarkerPopMotion.Scale(0.15f), 1f);
        Assert.AreEqual(1f, OrderMarkerPopMotion.Scale(OrderMarkerPopMotion.DurationSeconds));
        Assert.AreEqual(1f, OrderMarkerPopMotion.Scale(-1f));
    }
}
#endif
