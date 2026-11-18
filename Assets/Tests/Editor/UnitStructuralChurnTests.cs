#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Tests.Editor
{
    /// <summary>
    /// 2026-11-17 allocator fix: per-hit and camera-driven unit state is enableable, so repeated hits, health-bar expiry
    /// and render-budget flips create no archetypes after the first occurrence (each new archetype is matched against
    /// every query shape in the World's fixed query block allocator).
    /// </summary>
    public sealed class UnitStructuralChurnTests
    {
        [Test]
        public void ToggledUnitStateTypesAreEnableable()
        {
            foreach (var type in new[] { typeof(RecentAttacker), typeof(RecentDamageHealthBarVisibility), typeof(UnitRenderBudgetCulledUnitTag) })
                Assert.IsTrue(typeof(IEnableableComponent).IsAssignableFrom(type), type.Name + " must stay enableable");
        }

        [Test]
        public void HitExpireAndCullCyclesCreateNoArchetypesAfterTheFirst()
        {
            using var world = new World(nameof(HitExpireAndCullCyclesCreateNoArchetypesAfterTheFirst));
            var em = world.EntityManager;
            var unit = em.CreateEntity(typeof(UnitMovementBehavior), typeof(UnitHealth), typeof(UnitFootprint), typeof(LocalTransform));
            em.SetComponentData(unit, new UnitHealth { Current = 50, Max = 100 });
            em.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(unit, LocalTransform.Identity);
            SystemHandle ecbSystem = world.CreateSystem<EndSimulationEntityCommandBufferSystem>();
            SystemHandle healthBars = world.CreateSystem<UnitHealthBarSystem>();
            em.CreateEntity(typeof(HealthBarFill)); // UnitHealthBarSystem requires a health-bar fill entity to update

            // First occurrence adds each component once (the only structural changes).
            em.AddComponentData(unit, new RecentAttacker { Attacker = Entity.Null });
            em.AddComponentData(unit, new RecentDamageHealthBarVisibility { TimeRemaining = 0.05f });
            em.AddComponent<UnitRenderBudgetCulledUnitTag>(unit);
            world.SetTime(new TimeData(0d, 0.1f)); // creates the world time singleton once, before the baseline
            int archetypes = CountArchetypes(em);

            for (int cycle = 0; cycle < 4; cycle++)
            {
                world.SetTime(new TimeData(1d + cycle, 0.1f));
                healthBars.Update(world.Unmanaged);   // expiry disables the window through the ECB
                ecbSystem.Update(world.Unmanaged);
                em.CompleteAllTrackedJobs();
                Assert.IsFalse(em.IsComponentEnabled<RecentDamageHealthBarVisibility>(unit), "expired, cycle " + cycle);

                em.SetComponentEnabled<RecentAttacker>(unit, false);                     // retaliation consumed
                UnitRenderBudgetCulledUnitTagState.Clear(em, unit);                      // shown detailed
                Assert.IsFalse(UnitRenderBudgetCulledUnitTagState.IsCulled(em, unit));
                em.SetComponentData(unit, new RecentDamageHealthBarVisibility { TimeRemaining = 0.05f }); // next hit
                em.SetComponentEnabled<RecentDamageHealthBarVisibility>(unit, true);
                em.SetComponentEnabled<RecentAttacker>(unit, true);
                em.SetComponentEnabled<UnitRenderBudgetCulledUnitTag>(unit, true);       // far again
                Assert.IsTrue(UnitRenderBudgetCulledUnitTagState.IsCulled(em, unit));
                Assert.AreEqual(archetypes, CountArchetypes(em), "no new archetype, cycle " + cycle);
            }
        }

        private static int CountArchetypes(EntityManager em)
        {
            using var list = new NativeList<EntityArchetype>(Allocator.Temp);
            em.GetAllArchetypes(list);
            return list.Length;
        }
    }
}
#endif
