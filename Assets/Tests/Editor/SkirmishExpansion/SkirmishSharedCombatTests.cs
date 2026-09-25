using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Tests.Editor
{
    public sealed class SkirmishSharedCombatTests
    {
        [Test]
        public void RuntimeActorAndLinkedVisualSurviveSourceSceneUnload()
        {
            using var world = new World(nameof(RuntimeActorAndLinkedVisualSurviveSourceSceneUnload));
            var em = world.EntityManager;
            var section = em.CreateEntity();
            var prefab = em.CreateEntity(typeof(Prefab), typeof(UnitHealth));
            var child = em.CreateEntity(typeof(Prefab), typeof(LocalTransform));
            em.AddSharedComponent(prefab, new SceneTag { SceneEntity = section });
            em.AddSharedComponent(child, new SceneTag { SceneEntity = section });
            em.AddSharedComponent(prefab, new SceneSection());
            em.AddSharedComponent(child, new SceneSection());
            var linked = em.AddBuffer<LinkedEntityGroup>(prefab);
            linked.Add(new LinkedEntityGroup { Value = prefab });
            linked.Add(new LinkedEntityGroup { Value = child });
            var actor = em.Instantiate(prefab);
            var visual = em.GetBuffer<LinkedEntityGroup>(actor)[1].Value;
            SkirmishRuntimeActorOwnership.DetachFromSourceScene(em, actor);
            Assert.IsTrue(em.HasComponent<SceneTag>(prefab));
            Assert.IsTrue(em.HasComponent<SceneTag>(child));
            Assert.IsFalse(em.HasComponent<SceneTag>(actor));
            Assert.IsFalse(em.HasComponent<SceneTag>(visual));
            Assert.IsFalse(em.HasComponent<SceneSection>(actor));
            Assert.IsFalse(em.HasComponent<SceneSection>(visual));
            using var sourceScene = em.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<SceneTag>() },
                Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities
            });
            sourceScene.SetSharedComponentFilter(new SceneTag { SceneEntity = section });
            em.DestroyEntity(sourceScene);
            Assert.IsTrue(em.Exists(actor));
            Assert.IsTrue(em.Exists(visual));
            em.DestroyEntity(actor);
            Assert.IsFalse(em.Exists(visual), "Attempt cleanup must still own linked presentation.");
        }

        [Test]
        public void AntiAirOverlayBindsTheActualMissileWeapon()
        {
            using var world = new World(nameof(AntiAirOverlayBindsTheActualMissileWeapon));
            var em = world.EntityManager;
            var unit = em.CreateEntity(typeof(AirMissileLauncherComponent), typeof(SkirmishUnitRoleComponent));
            em.SetComponentData(unit, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.AntiAir, Category = SkirmishPopulationCategory.Ground });
            em.SetComponentData(unit, new AirMissileLauncherComponent { AirTargetDamage = 120, BaseDetectionRange = 220, MaxDetectionRange = 420, MaxSupportRangeBonus = 200 });
            Assert.IsTrue(Game.Configs.SkirmishRoleOverlayCatalog.TryGet(Game.Configs.SkirmishRoleOverlayCatalog.CreateAirMobileSlice(), SkirmishRoleKind.AntiAir, out var overlay));
            SkirmishSharedCombatBinding.Bind(em, unit, overlay);
            var weapon = em.GetComponentData<AirMissileLauncherComponent>(unit);
            Assert.AreEqual(overlay.Damage, weapon.AirTargetDamage);
            Assert.AreEqual(overlay.RangeWorld, weapon.BaseDetectionRange);
            Assert.AreEqual(overlay.RangeWorld, weapon.MaxDetectionRange);
            Assert.AreEqual(CombatTargetDomain.Air, em.GetComponentData<CombatTargetPolicy>(unit).AllowedTargets);
        }

        [Test]
        public void RegistryActorsNeverReceiveSupplementalStructureDamage()
        {
            foreach (bool hasWeapon in new[] { false, true })
            {
                using var world = new World(nameof(RegistryActorsNeverReceiveSupplementalStructureDamage));
                var em = world.EntityManager;
                var id = new FixedString64Bytes("single-damage-owner");
                var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
                em.SetComponentData(session, new SkirmishExpandedSessionComponent { SessionId = id });
                var target = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(UnitHealth), typeof(LocalTransform));
                em.SetComponentData(target, new SkirmishAttemptOwnedComponent { SessionId = id, IsStructure = 1, FactionId = 2 });
                em.SetComponentData(target, new UnitHealth { Current = 100, Max = 100 });
                em.SetComponentData(target, LocalTransform.FromPosition(new float3(1, 0, 0)));
                SkirmishFogService.Reveal(em, target);
                var unit = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishVisualSpawnedComponent),
                    typeof(SkirmishMoveIntentComponent), typeof(SkirmishRoleOverlayComponent), typeof(UnitHealth), typeof(LocalTransform));
                em.SetComponentData(unit, new SkirmishAttemptOwnedComponent { SessionId = id, FactionId = 1 });
                em.SetComponentData(unit, new SkirmishVisualSpawnedComponent { Spawned = 1, FromRegistry = 1 });
        em.AddComponent<SkirmishSharedActorTag>(unit);
                em.SetComponentData(unit, new SkirmishMoveIntentComponent { Order = SkirmishGroupOrderKind.Attack, AttackTarget = target });
                em.SetComponentData(unit, new SkirmishRoleOverlayComponent { Damage = 35, RangeWorld = 10, TargetDomains = SkirmishTargetDomain.Structure });
                em.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
                em.SetComponentData(unit, LocalTransform.Identity);
                if (hasWeapon) em.AddComponentData(unit, new UnitAttack { Damage = 35, Range = 10 });
                Assert.AreEqual(0, SkirmishExpandedEngagementService.Step(em, session, 1, false));
                Assert.AreEqual(100, em.GetComponentData<UnitHealth>(target).Current,
                    "A missing shared weapon must not silently restore the expansion damage bypass.");
            }
        }
    }
}
