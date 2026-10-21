using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class EntitySceneGridSourceBindingTests
    {
        sealed class Fixture : IDisposable
        {
            public readonly World World = new World("real-map-producer-binding-controls");
            public readonly OperationMapRuntimeBootstrapSceneSystemHelper Producer;
            public readonly Entity Root;
            public EntityManager Em => World.EntityManager;
            public Fixture()
            {
                Producer = new OperationMapRuntimeBootstrapSceneSystemHelper(World);
                var definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>("Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset");
                Assert.IsNotNull(definition);
                FixedString64Bytes scenario = "scenario.skirmish.desert_base_standard", mission = "skirmish";
                Assert.IsTrue(Producer.TryPublish(definition, in scenario, in mission, 7,
                    OperationMapReadinessFlags.Metadata, OperationMapReadinessFlags.Metadata, out var root, out var error), error);
                Root = root;
            }
            public void Dispose() { Producer.Dispose(); World.Dispose(); }
        }
        [Test] public void ActualPublishedRootIsBoundToItsCreatedWorld()
        {
            using var f = new Fixture(); using var foreign = new World("foreign-map-world");
            Assert.IsTrue(f.Producer.TryGetPublishedRoot(f.World, out var root)); Assert.AreEqual(f.Root, root);
            Assert.IsFalse(f.Producer.TryGetPublishedRoot(foreign, out _));
        }
        [Test] public void ReplacementSingletonCannotBeAdoptedAsPublishedOwner()
        {
            using var f = new Fixture(); var active = f.Em.GetComponentData<ActiveOperationMapComponent>(f.Root);
            var metadata = f.Em.GetComponentData<OperationMapMetadataComponent>(f.Root);
            f.Em.DestroyEntity(f.Root);
            var replacement = f.Em.CreateEntity(typeof(OperationMapRootComponent), typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
            f.Em.SetComponentData(replacement, active); f.Em.SetComponentData(replacement, metadata);
            Assert.AreNotEqual(f.Root, replacement); Assert.IsFalse(f.Producer.TryGetPublishedRoot(f.World, out _));
        }
        [Test] public void ActualMetadataAndActiveGenerationsCannotChangeUnderBinding()
        {
            using var f = new Fixture(); var active = f.Em.GetComponentData<ActiveOperationMapComponent>(f.Root);
            active.Generation++; f.Em.SetComponentData(f.Root, active); Assert.IsFalse(f.Producer.TryGetPublishedRoot(f.World, out _));
            active.Generation--; f.Em.SetComponentData(f.Root, active);
            var metadata = f.Em.GetComponentData<OperationMapMetadataComponent>(f.Root); metadata.Generation++;
            f.Em.SetComponentData(f.Root, metadata); Assert.IsFalse(f.Producer.TryGetPublishedRoot(f.World, out _));
        }
        [Test] public void ClearingActualProducerInvalidatesItsRootRead()
        {
            using var f = new Fixture(); f.Producer.ClearPublishedState();
            Assert.IsFalse(f.Producer.TryGetPublishedRoot(f.World, out _));
        }
        [Test] public void UnboundViewAndDefaultProofCannotWaiveHelperOrLifecycle()
        {
            using var world = new World("unbound-view-binding-refusal"); var go = new GameObject("inactive-binding-control"); go.SetActive(false);
            try
            {
                var view = go.AddComponent<MatchSceneView>();
                Assert.IsFalse(view.TryReadEntitySceneGridBinding(world, Entity.Null, Entity.Null, out _, out var reason));
                Assert.AreEqual("NormalSourceLifecycleUnavailable", reason);
                Assert.AreEqual("EntitySceneBindingUnavailable", S002RuntimeBlockerWitness.RevalidateEntitySceneAdmission(world.EntityManager, default, default));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void DisabledHelperPathStillDemandsAllMapReadinessFlags()
        {
            var all = S002RuntimeBlockerWitness.MandatorySourceReadiness;
            foreach (OperationMapReadinessFlags flag in Enum.GetValues(typeof(OperationMapReadinessFlags)))
            {
                if (flag == OperationMapReadinessFlags.None) continue;
                var readiness = new OperationMapReadinessComponent { Generation = 7, RequiredFlags = all & ~flag, ReadyFlags = all & ~flag };
                Assert.IsNotNull(S002RuntimeBlockerWitness.MapReadinessUnavailableReason(7, 7, 1, readiness), flag.ToString());
            }
        }
        public static void RunFocusedValidation()
        {
            var t = new EntitySceneGridSourceBindingTests();
            try
            {
                t.ActualPublishedRootIsBoundToItsCreatedWorld(); t.ReplacementSingletonCannotBeAdoptedAsPublishedOwner();
                t.ActualMetadataAndActiveGenerationsCannotChangeUnderBinding(); t.ClearingActualProducerInvalidatesItsRootRead();
                t.UnboundViewAndDefaultProofCannotWaiveHelperOrLifecycle(); t.DisabledHelperPathStillDemandsAllMapReadinessFlags();
                Debug.Log("[EntitySceneGridSourceBindingTests] result=Passed controls=6 normalEntitySceneObserved=0 sourceFitCertified=0 missionReady=0");
            }
            catch (Exception error) { Debug.LogError("[EntitySceneGridSourceBindingTests] result=Failed\n" + error); throw; }
        }
    }
}
