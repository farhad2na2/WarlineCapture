using System;
using Game.Components;
using Game.Configs;
using Game.Tactical.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M01FirstContactConfigBuilder
    {
        public const string MissionId = "saga.ch01.m01.first_contact";
        public const string CatalogPath = "Assets/Game/Configs/Campaign/CampaignMissionCatalog.asset";
        public const string MissionPath =
            "Assets/Game/Configs/Missions/Chapter01/MissionDefinition_Ch01_M01_FirstContact.asset";
        public const string ScenarioPath =
            "Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M01_FirstContact.asset";

        [MenuItem("Game/Campaign/Build M01 First Contact Configs")]
        public static void BuildMenu() => Build();

        public static void Build()
        {
            EnsureFolder("Assets/Game/Configs/Campaign");
            EnsureFolder("Assets/Game/Configs/Missions/Chapter01");
            EnsureFolder("Assets/Game/Configs/Scenarios/Chapter01");
            MissionDefinitionConfig mission = LoadOrCreate<MissionDefinitionConfig>(MissionPath);
            ScenarioSetupConfig scenario = LoadOrCreate<ScenarioSetupConfig>(ScenarioPath);
            MissionDefinitionCatalogConfig catalog = LoadOrCreate<MissionDefinitionCatalogConfig>(CatalogPath);
            PopulateMission(mission);
            PopulateScenario(scenario);
            PopulateCatalog(catalog, mission);
            EditorUtility.SetDirty(mission);
            EditorUtility.SetDirty(scenario);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void PopulateMission(MissionDefinitionConfig target)
        {
            SerializedObject serialized = new(target);
            Set(serialized, "missionId", MissionId);
            Set(serialized, "schemaVersion", 1);
            Set(serialized, "displayNameKey", "mission.m01.name");
            Set(serialized, "displaySummaryKey", "mission.m01.summary");
            Set(serialized, "locationNameKey", "mission.m01.location");
            Set(serialized, "scenarioId", "scenario.ch01.m01.first_contact");
            Set(serialized, "operationMapId", "opmap.ch01.district_edge_01");
            Set(serialized, "briefingSequenceId", "seq.ch01.m01.brief");
            Set(serialized, "commsSequenceId", "seq.ch01.m01.comms");
            Set(serialized, "debriefSequenceId", "seq.ch01.m01.debrief");
            SetArray(serialized, "objectives", 2, (item, index) =>
            {
                Set(item, "objectiveId", index == 0
                    ? "obj.ch01.m01.destroy_patrol"
                    : "obj.ch01.m01.keep_command_squad_alive");
                Set(item, "displayTextKey", index == 0
                    ? "mission.m01.objective.secure_corridor"
                    : "mission.m01.failure.command_squad_destroyed");
                Set(item, "rule", index == 0 ? 1 : 2);
                Set(item, "missionRoleId", index == 0 ? "role.hostile.patrol" : "role.friendly.command_squad");
                Set(item, "requiredCount", index == 0 ? 3 : 1);
                Set(item, "failureOnRuleBreak", index == 1);
            });
            SetArray(serialized, "stars", 3, (item, index) =>
            {
                Set(item, "starIndex", index + 1);
                Set(item, "rule", index + 1);
                Set(item, "displayTextKey", index switch
                {
                    0 => "mission.m01.star.complete",
                    1 => "mission.m01.star.no_squad_loss",
                    _ => "mission.m01.star.under_four_minutes"
                });
                Set(item, "threshold", index == 2 ? 240000 : 0);
            });
            SetArray(serialized, "firstClearRewards", 2, (item, index) =>
            {
                Set(item, "kind", index == 0 ? 0 : 1);
                Set(item, "rewardConfigId", index == 0 ? "reward.commander_xp" : string.Empty);
                Set(item, "displayTextKey", index == 0 ? "mission.reward.commander_xp" : "mission.reward.credits");
                Set(item, "amount", index == 0 ? 260 : 1200);
            });
            SetArray(serialized, "replayRewards", 1, (item, _) =>
            {
                Set(item, "kind", 1);
                Set(item, "rewardConfigId", string.Empty);
                Set(item, "displayTextKey", "mission.reward.credits");
                Set(item, "amount", 250);
            });
            SerializedProperty commands = serialized.FindProperty("commandPolicy").FindPropertyRelative("allowedCommands");
            TacticalCommandMode[] modes = { TacticalCommandMode.Select, TacticalCommandMode.Move,
                TacticalCommandMode.Attack, TacticalCommandMode.Hold, TacticalCommandMode.Stop };
            commands.arraySize = modes.Length;
            for (int index = 0; index < modes.Length; index++) commands.GetArrayElementAtIndex(index).enumValueIndex = (int)modes[index];
            Set(serialized, "replayAllowed", true);
            Set(serialized, "replayTutorialDefaultEnabled", false);
            Set(serialized, "requireOperationMapReady", true);
            Set(serialized, "requireGridReady", true);
            Set(serialized, "requireUnitCatalogReady", true);
            SetStringArray(serialized, "requiredFeatureIds", new[]
            {
                "feature.operation_map", "feature.unit_catalog", "feature.tactical_commands"
            });
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PopulateScenario(ScenarioSetupConfig target)
        {
            SerializedObject serialized = new(target);
            Set(serialized, "scenarioId", "scenario.ch01.m01.first_contact");
            Set(serialized, "operationMapId", "opmap.ch01.district_edge_01");
            string[] anchors = { "player_spawn", "camera_start", "move_target", "patrol_spawn",
                "patrol_route_a", "patrol_route_b", "patrol_route_c", "patrol_objective",
                "civilian_safe_zone", "civilian_evacuation", "minimap_start" };
            OperationMapAnchorKind[] kinds = { OperationMapAnchorKind.Deployment, OperationMapAnchorKind.Camera,
                OperationMapAnchorKind.Objective, OperationMapAnchorKind.Spawn, OperationMapAnchorKind.Lane,
                OperationMapAnchorKind.Lane, OperationMapAnchorKind.Lane, OperationMapAnchorKind.Hostile,
                OperationMapAnchorKind.Civilian, OperationMapAnchorKind.Civilian, OperationMapAnchorKind.Minimap };
            SetArray(serialized, "requiredAnchors", anchors.Length, (item, index) =>
            {
                Set(item, "anchorId", "anchor.ch01.m01." + anchors[index]);
                Set(item, "kind", (int)kinds[index]);
            });
            Set(serialized, "deterministicSeed", 1001001);
            Set(serialized, "encounterStartMilliseconds", 3000);
            SetArray(serialized, "unitGroups", 2, (group, index) => PopulateGroup(group, index));
            SetArray(serialized, "patrolRoutes", 1, (route, _) =>
            {
                Set(route, "routeId", "route.ch01.m01.hostile_patrol");
                Set(route, "unitGroupId", "group.ch01.m01.hostile_patrol");
                SetStringArray(route, "anchorIds", new[] { "anchor.ch01.m01.patrol_route_a",
                    "anchor.ch01.m01.patrol_route_b", "anchor.ch01.m01.patrol_route_c" });
                Set(route, "startDelayMilliseconds", 3000);
            });
            SerializedProperty restrictions = serialized.FindProperty("restrictions");
            foreach (string field in new[] { "buildingDisabled", "productionDisabled", "economyDisabled",
                         "transportDisabled", "airDisabled" }) Set(restrictions, field, true);
            SetArray(serialized, "ambientPresentations", 1, (ambient, _) =>
            {
                Set(ambient, "presentationId", "ambient.ch01.m01.civilians");
                Set(ambient, "anchorId", "anchor.ch01.m01.civilian_safe_zone");
                Set(ambient, "routeId", "route.ch01.m01.civilian_evacuation");
                Set(ambient, "instanceCount", 8);
            });
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PopulateGroup(SerializedProperty group, int index)
        {
            bool friendly = index == 0;
            Set(group, "groupId", friendly ? "group.ch01.m01.command_squad" : "group.ch01.m01.hostile_patrol");
            Set(group, "factionIndex", friendly ? 1 : 2);
            string[] keys = friendly
                ? new[] { "unit.jrc.rifle_male_02_alt_02", "unit.jrc.rifle_male_02_alt_04",
                    "unit.jrc.rifle_female_01_alt_01", "unit.jrc.rifle_female_02_alt_01" }
                : new[] { "unit.ash.courier", "unit.ash.warden", "unit.ash.broker" };
            string[] guids = friendly
                ? new[] { "70d525bdf4894529869cd1402ff5d62e", "a3d36fd8847164cc596e0b5ba7bd9bb9",
                    "970069fef3e4437195c3225a1615e384", "b9481cc1ec42499c96846b5d161ac7b2" }
                : new[] { "fe23cbf9678344f4b182169b49fe68b6", "01045d2a58ec4359b395696309684ffa",
                    "8093159068194fc187efe5c356116e9b" };
            SerializedProperty units = group.FindPropertyRelative("units");
            units.arraySize = keys.Length;
            for (int unitIndex = 0; unitIndex < keys.Length; unitIndex++)
            {
                SerializedProperty unit = units.GetArrayElementAtIndex(unitIndex);
                Set(unit, "unitConfigKey", keys[unitIndex]);
                Set(unit, "expectedAssetGuid", guids[unitIndex]);
                Set(unit, "spawnAnchorId", friendly
                    ? "anchor.ch01.m01.player_spawn" : "anchor.ch01.m01.patrol_spawn");
                Set(unit, "missionRoleId", friendly
                    ? "role.friendly.command_squad" : "role.hostile.patrol");
                Set(unit, "count", 1);
            }
        }

        private static void PopulateCatalog(MissionDefinitionCatalogConfig target, MissionDefinitionConfig mission)
        {
            SerializedObject serialized = new(target);
            SetArray(serialized, "entries", 1, (entry, _) =>
            {
                Set(entry, "missionId", MissionId);
                entry.FindPropertyRelative("definition").objectReferenceValue = mission;
            });
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        private static void SetArray(SerializedObject target, string name, int count, Action<SerializedProperty, int> fill) =>
            SetArray(target.FindProperty(name), count, fill);
        private static void SetArray(SerializedProperty array, int count, Action<SerializedProperty, int> fill)
        {
            array.arraySize = count;
            for (int index = 0; index < count; index++) fill(array.GetArrayElementAtIndex(index), index);
        }
        private static void SetStringArray(SerializedObject target, string name, string[] values) =>
            SetStringArray(target.FindProperty(name), values);
        private static void SetStringArray(SerializedProperty target, string name, string[] values) =>
            SetStringArray(target.FindPropertyRelative(name), values);
        private static void SetStringArray(SerializedProperty array, string[] values)
        {
            array.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) array.GetArrayElementAtIndex(index).stringValue = values[index];
        }
        private static void Set(SerializedObject target, string name, string value) => Set(target.FindProperty(name), value);
        private static void Set(SerializedObject target, string name, int value) => Set(target.FindProperty(name), value);
        private static void Set(SerializedObject target, string name, bool value) => Set(target.FindProperty(name), value);
        private static void Set(SerializedProperty target, string name, string value) => Set(target.FindPropertyRelative(name), value);
        private static void Set(SerializedProperty target, string name, int value) => Set(target.FindPropertyRelative(name), value);
        private static void Set(SerializedProperty target, string name, bool value) => Set(target.FindPropertyRelative(name), value);
        private static void Set(SerializedProperty property, string value) => property.stringValue = value;
        private static void Set(SerializedProperty property, int value) => property.intValue = value;
        private static void Set(SerializedProperty property, bool value) => property.boolValue = value;
    }
}
