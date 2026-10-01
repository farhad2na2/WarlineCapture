using System;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH04M04GroundedSignalMapReuseValidation
    {
        public static void Run()
        {
            var definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(CH04M04GroundedSignalConfigBuilder.MapPath);
            Require(definition != null && !definition.SourceBinding.IsConfigured, "Grounded Signal must own a standalone physical map");
            using var world = new World("Grounded standalone map reuse");
            var em = world.EntityManager;
            var missionRoot = em.CreateEntity(typeof(CampaignMissionRootComponent));
            var launches = em.AddBuffer<CampaignMissionLaunchRequestElement>(missionRoot);
            launches.Add(new CampaignMissionLaunchRequestElement { MissionId = CampaignMissionSequence.GroundedSignal,
                ScenarioId = "scenario.ch04.m04.grounded_signal", OperationMapId = definition.OperationMapId });
            using var menu = new OperationMapRuntimeBootstrapSceneSystemHelper(world);
            Unity.Collections.FixedString64Bytes missionId = CampaignMissionSequence.GroundedSignal, scenarioId = "scenario.ch04.m04.grounded_signal";
            Require(menu.TryPublish(definition, in scenarioId, in missionId, 1, OperationMapReadinessFlags.Metadata,
                OperationMapReadinessFlags.Metadata, out var root, out var error), error);
            using var match = new OperationMapRuntimeBootstrapSceneSystemHelper(world);
            Require(match.TryPublish(definition, in scenarioId, in missionId, 1, OperationMapReadinessFlags.Metadata,
                OperationMapReadinessFlags.Metadata, out var reused, out error) && reused == root, error ?? "Same generation strict reuse failed");
            Require(em.GetComponentData<OperationMapMetadataComponent>(root).PhysicalSourceValidated == 0, "Menu reuse prematurely qualified physical source");
            var stale = UnityEngine.Object.Instantiate(definition);
            try
            {
                typeof(OperationMapDefinition).GetField("contentHash", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(stale, new string('a', 64));
                Require(!CampaignMissionOperationMapReuseUtility.TryValidateStandalonePhysicalSource(em, stale, root, out _), "Stale loaded metadata accepted");
                Require(em.GetComponentData<OperationMapMetadataComponent>(root).PhysicalSourceValidated == 0, "Stale scene qualified physical source");
                Require(!match.TryPublish(stale, in scenarioId, in missionId, 1, OperationMapReadinessFlags.Metadata,
                    OperationMapReadinessFlags.Metadata, out _, out error) && error.Contains("monotonically"), "Non-reusable publication bypassed generation guard");
                Require(CampaignMissionOperationMapReuseUtility.TryValidateStandalonePhysicalSource(em, definition, root, out error), error);
                Require(em.GetComponentData<OperationMapMetadataComponent>(root).PhysicalSourceValidated == 1, "Validated loaded source not qualified");
            }
            finally { UnityEngine.Object.DestroyImmediate(stale); }
            using var freshWorld = new World("Fresh standalone map lifetime");
            using var freshBootstrap = new OperationMapRuntimeBootstrapSceneSystemHelper(freshWorld);
            Require(freshBootstrap.TryPublish(definition, in scenarioId, in missionId, 1, OperationMapReadinessFlags.Metadata,
                OperationMapReadinessFlags.Metadata, out _, out error), "Fresh World generation1 rejected: " + error);
            Debug.Log("[GroundedSignalMapReuse] result=Passed standalone=strict same-generation=reused physical-source=scene-only stale=reject monotonic=preserved new-world=generation1");
        }
        private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
    }
}
