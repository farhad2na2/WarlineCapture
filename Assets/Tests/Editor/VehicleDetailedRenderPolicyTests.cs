using Game.Components;
using Game.Rendering;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

public sealed class VehicleDetailedRenderPolicyTests
{
    [TestCase(false, false, 10f)]
    [TestCase(false, true, 100f)]
    [TestCase(true, false, 65f)]
    [TestCase(true, true, 65f)]
    [TestCase(true, true, 500f)]
    public void VehiclesKeepCompleteModelsAcrossDistanceAndMovement(bool enemy, bool moving, float distance)
    {
        using var world = new World("Vehicle render policy");
        using var ecb = new EntityCommandBuffer(Allocator.Temp);
        using var ready = new NativeHashSet<Entity>(1, Allocator.Temp);
        foreach (bool hasLods in new[] { false, true })
        {
            var result = new UnitRenderBudgetVisualPlan().CreateDesiredVisualPlan(world.EntityManager,
                ecb, ready, default, new UnitRenderBudgetVisualPlan.Request
                {
                    IsCharacter = false, IsEnemyUnit = enemy, IsMovingUnit = moving,
                    Visible = 1, DistanceSq = distance * distance, EnemyImpostorDistanceSq = 64f * 64f,
                    HasMidLodInstance = hasLods, HasLowLodInstance = hasLods, LowBand = true
                }, new UnitRenderBudgetCharacterPolicy(), new UnitRenderBudgetReadiness(),
                new UnitRenderBudgetAnimationReadiness(), new UnitRenderBudgetRenderableState());
            Assert.AreEqual(UnitRenderVisualKind.Detail, result.DesiredVisual);
            Assert.IsTrue(result.ShouldShowDetail && result.ForceImmediateDetailVisual);
            Assert.IsFalse(result.ShouldShowFar || result.ShouldShowMid || result.ShouldShowLow);
        }
    }
}
