using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class RuntimeBuildingAttackPickingTests
    {
        [Test]
        public void VisibleGateFaceIsTappableAtMobileAspectsAndCameraAngles()
        {
            using var world = new World("GateScreenPickingTest");
            var em = world.EntityManager;
            var gridEntity = em.CreateEntity(typeof(GridConfig));
            em.SetComponentData(gridEntity, new GridConfig { Width = 256, Height = 256, CellSize = 1 });
            var gate = em.CreateEntity(typeof(RuntimeBuildingCombatTag), typeof(RuntimeBuildingCombatInfo),
                typeof(Faction), typeof(UnitHealth), typeof(LocalTransform), typeof(UnitSelectionHitbox));
            em.SetComponentData(gate, new RuntimeBuildingCombatInfo
            {
                OriginCell = new int2(100, 100), FootprintCells = new int2(8, 1), IsGate = 1, OwnerFactionId = 2
            });
            em.SetComponentData(gate, new Faction { Id = 2 });
            em.SetComponentData(gate, new UnitHealth { Current = 1000, Max = 1000 });
            em.SetComponentData(gate, LocalTransform.FromPosition(new float3(104, 0, 100.5f)));
            em.SetComponentData(gate, new UnitSelectionHitbox { Center = new float3(0, 2, 0), Extents = new float3(4, 2, .5f) });
            var go = new GameObject("Gate picking camera");
            try
            {
                var camera = go.AddComponent<Camera>();
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(2400, 1080) })
                foreach (float yaw in new[] { -35f, 0f, 35f })
                {
                    camera.pixelRect = new Rect(0, 0, size.x, size.y);
                    camera.aspect = (float)size.x / size.y;
                    Vector3 center = new(104, 0, 100.5f);
                    camera.transform.position = center + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 35, -40);
                    camera.transform.LookAt(center);
                    var picker = new RtsSelectionPointerTargetCommandCompositionSystemHelper();
                    var context = PickingContext(camera);
                    for (int height = 0; height <= 4; height++)
                    {
                        Vector2 tap = camera.WorldToScreenPoint(center + Vector3.up * height);
                        Assert.That(picker.TryGetClickedAttackTargetEntity(context, tap, em, out Entity target), Is.True,
                            $"Gate face missed: {size} yaw={yaw} height={height}");
                        Assert.That(target, Is.EqualTo(gate));
                    }
                    Vector2 far = camera.WorldToScreenPoint(center + Vector3.right * 30);
                    Assert.That(picker.TryGetClickedAttackTargetEntity(context, far, em, out _), Is.False, "Empty ground must not attack gate");
                }
                Vector2 face = camera.WorldToScreenPoint(new Vector3(104, 2, 100.5f));
                var resolver = new RtsSelectionPointerTargetCommandCompositionSystemHelper();
                var ctx = PickingContext(camera);
                em.SetComponentData(gate, new Faction { Id = 0 });
                Assert.That(resolver.TryGetClickedAttackTargetEntity(ctx, face, em, out _), Is.False, "Neutral walls must stay protected");
                em.SetComponentData(gate, new Faction { Id = 1 });
                Assert.That(resolver.TryGetClickedAttackTargetEntity(ctx, face, em, out _), Is.False, "Friendly buildings must stay protected");
                em.SetComponentData(gate, new Faction { Id = 2 });
                em.SetComponentData(gate, new UnitHealth { Current = 0, Max = 1000 });
                Assert.That(resolver.TryGetClickedAttackTargetEntity(ctx, face, em, out _), Is.False, "Destroyed gate must not intercept taps");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static RtsSelectionPointerTargetCommandCompositionSystemHelper.Context PickingContext(Camera camera)
        {
            // Populate only the dependencies used by the production screen picker.
            var ctor = typeof(RtsSelectionPointerTargetCommandCompositionSystemHelper.Context).GetConstructors()[0];
            var parameters = ctor.GetParameters();
            var args = new object[parameters.Length];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = parameters[i].ParameterType.IsValueType ? Activator.CreateInstance(parameters[i].ParameterType) : null;
                if (parameters[i].Name == "worldCamera") args[i] = camera;
                if (parameters[i].Name == "focusableUnitLookupSystem") args[i] = new FocusableUnitLookupCameraSystemHelper();
            }
            return (RtsSelectionPointerTargetCommandCompositionSystemHelper.Context)ctor.Invoke(args);
        }

        public static void RunFocusedValidation()
        {
            new RuntimeBuildingAttackPickingTests().VisibleGateFaceIsTappableAtMobileAspectsAndCameraAngles();
            Debug.Log("[RuntimeBuildingAttackPickingTests] result=Passed gate face, camera angles, mobile aspects, neutral/friendly/dead exclusions");
        }
    }
}
