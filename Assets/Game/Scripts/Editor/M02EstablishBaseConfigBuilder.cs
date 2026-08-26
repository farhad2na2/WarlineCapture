using System;
using Game.Components;
using Game.Configs;
using Game.Tactical.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M02EstablishBaseConfigBuilder
    {
        public const string MissionId = "saga.ch01.m02.establish_base";
        public const string MissionPath =
            "Assets/Game/Configs/Missions/Chapter01/MissionDefinition_Ch01_M02_EstablishBase.asset";
        public const string ScenarioPath =
            "Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M02_EstablishBase.asset";
        public const string BarracksConfigPath =
            "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset";
        public const string RequiredRiflePrefabPath =
            "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab";
        public const string MissionCatalogPath =
            "Assets/Game/Configs/Campaign/CampaignMissionCatalog.asset";
        public const string OperationMapCatalogPath =
            "Assets/Game/Configs/OperationMaps/Chapter01/OperationMapCatalog_Chapter01.asset";

        [MenuItem("Game/Campaign/M02/Configure Barracks Production")]
        public static void ConfigureBarracksProductionMenu() => ConfigureBarracksProduction();

        [MenuItem("Game/Campaign/M02/Build Mission Definition")]
        public static void BuildMissionDefinitionMenu() => BuildMissionDefinition();

        [MenuItem("Game/Campaign/M02/Build Scenario")]
        public static void BuildScenarioMenu() => BuildScenario();

        [MenuItem("Game/Campaign/M02/Build Canonical Data")]
        public static void BuildCanonicalDataMenu() => BuildCanonicalData();

        public static void BuildCanonicalData()
        {
            BuildMissionDefinition();
            BuildScenario();
            M02EstablishBaseForwardPostWindowValidation.RunFocusedValidation();
            BuildCatalogs();
        }

        public static void BuildCatalogs()
        {
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();

            MissionDefinitionCatalogConfig missions =
                LoadRequired<MissionDefinitionCatalogConfig>(MissionCatalogPath);
            OperationMapCatalogConfig maps =
                LoadRequired<OperationMapCatalogConfig>(OperationMapCatalogPath);
            MissionDefinitionConfig mission = LoadRequired<MissionDefinitionConfig>(MissionPath);
            ScenarioSetupConfig scenario = LoadRequired<ScenarioSetupConfig>(ScenarioPath);
            OperationMapDefinition map = LoadRequired<OperationMapDefinition>(
                M02EstablishBaseForwardPostWindowValidation.DefinitionPath);

            Require(MissionDefinitionContractValidation.TryValidateCatalog(missions, out string error), error);
            Require(maps.TryValidate(out error), error);
            Require(missions.Entries.Length == 2 && maps.Definitions.Length == 2,
                "Chapter 1 catalogs must contain exactly M01 and M02.");
            Require(missions.TryResolve(MissionId, out MissionDefinitionConfig resolvedMission) &&
                    ReferenceEquals(resolvedMission, mission),
                "Campaign catalog did not resolve the canonical M02 mission.");
            Require(maps.TryResolve(map.OperationMapId, out OperationMapDefinition resolvedMap) &&
                    ReferenceEquals(resolvedMap, map),
                "Operation-map catalog did not resolve the canonical M02 map.");
            Require(scenario.TryValidate(out error) && mission.ScenarioId == scenario.ScenarioId &&
                    mission.OperationMapId == scenario.OperationMapId &&
                    scenario.OperationMapId == map.OperationMapId,
                error ?? "M02 mission, scenario, and operation-map identities do not close.");
            Debug.Log(
                "[M02EstablishBaseConfigBuilder] result=Passed scope=Catalogs missions=2 maps=2");
        }

        public static void BuildScenario()
        {
            ScenarioSetupConfig scenario = LoadOrCreate<ScenarioSetupConfig>(ScenarioPath);
            SerializedObject serialized = new(scenario);
            Set(serialized, "scenarioId", "scenario.ch01.m02.establish_base");
            Set(serialized, "operationMapId", "opmap.ch01.forward_post_01");

            string[] anchorNames =
            {
                "friendly_spawn", "camera_start", "forward_post", "build_lot",
                "hostile_spawn", "lane_a", "lane_b", "lane_c", "defense_boundary",
                "civilian_edge", "civilian_evacuation", "minimap_start"
            };
            OperationMapAnchorKind[] anchorKinds =
            {
                OperationMapAnchorKind.Deployment, OperationMapAnchorKind.Camera,
                OperationMapAnchorKind.Base, OperationMapAnchorKind.Build,
                OperationMapAnchorKind.Spawn, OperationMapAnchorKind.Lane,
                OperationMapAnchorKind.Lane, OperationMapAnchorKind.Lane,
                OperationMapAnchorKind.Hostile, OperationMapAnchorKind.Civilian,
                OperationMapAnchorKind.Civilian, OperationMapAnchorKind.Minimap
            };
            SetArray(serialized, "requiredAnchors", anchorNames.Length, (anchor, index) =>
            {
                Set(anchor, "anchorId", "anchor.ch01.m02." + anchorNames[index]);
                Set(anchor, "kind", (int)anchorKinds[index]);
            });

            Set(serialized, "deterministicSeed", 2002001);
            Set(serialized, "encounterStartMilliseconds", 120000);
            SetArray(serialized, "unitGroups", 2, PopulateScenarioGroup);
            SetArray(serialized, "patrolRoutes", 1, (route, _) =>
            {
                Set(route, "routeId", "route.ch01.m02.hostile_patrol");
                Set(route, "unitGroupId", "group.ch01.m02.hostile_patrol");
                SetStringArray(route.FindPropertyRelative("anchorIds"), new[]
                {
                    "anchor.ch01.m02.lane_a",
                    "anchor.ch01.m02.lane_b",
                    "anchor.ch01.m02.lane_c"
                });
                Set(route, "startDelayMilliseconds", 120000);
            });

            SerializedProperty restrictions = serialized.FindProperty("restrictions");
            Set(restrictions, "buildingDisabled", false);
            Set(restrictions, "productionDisabled", false);
            Set(restrictions, "economyDisabled", false);
            Set(restrictions, "transportDisabled", true);
            Set(restrictions, "airDisabled", true);
            SetArray(serialized, "ambientPresentations", 1, (ambient, _) =>
            {
                Set(ambient, "presentationId", "ambient.ch01.m02.civilians");
                Set(ambient, "anchorId", "anchor.ch01.m02.civilian_edge");
                Set(ambient, "routeId", "route.ch01.m02.civilian_evacuation");
                Set(ambient, "instanceCount", 12);
            });

            SerializedProperty runtime = serialized.FindProperty("missionRuntime");
            Set(runtime, "enabled", true);
            Set(runtime, "startingCredits", 55000);
            Set(runtime, "startingMaterials", 120);
            SerializedProperty buildCatalog = runtime.FindPropertyRelative("buildCatalog");
            buildCatalog.arraySize = 1;
            Set(buildCatalog.GetArrayElementAtIndex(0), "buildingConfigId", "Building_Barrack");
            Set(buildCatalog.GetArrayElementAtIndex(0), "maxCount", 1);
            Set(runtime, "requiredProducerConfigId", "Building_Barrack");
            Set(runtime, "requiredUnitConfigId", "Unit_Chr_Soldier_Male_02_Alt_04");
            Set(runtime, "baseMissionRoleId", "role.friendly.forward_post");
            Set(runtime, "baseAnchorId", "anchor.ch01.m02.forward_post");
            SerializedProperty buildZone = runtime.FindPropertyRelative("buildZone");
            Set(buildZone, "anchorId", "anchor.ch01.m02.build_lot");
            Set(buildZone, "halfWidthCells",
                M02EstablishBaseForwardPostWindowValidation.BuildLotSize.x / 2);
            Set(buildZone, "halfHeightCells",
                M02EstablishBaseForwardPostWindowValidation.BuildLotSize.y / 2);
            SerializedProperty wave = runtime.FindPropertyRelative("delayedWave");
            Set(wave, "unitGroupId", "group.ch01.m02.hostile_patrol");
            Set(wave, "routeId", "route.ch01.m02.hostile_patrol");
            Set(wave, "targetMissionRoleId", "role.friendly.forward_post");
            Set(wave, "warningAtMilliseconds", 90000);
            Set(wave, "activationAtMilliseconds", 120000);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (!scenario.TryValidate(out string error))
                throw new InvalidOperationException($"Canonical M02 scenario is invalid: {error}");
            EditorUtility.SetDirty(scenario);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[M02EstablishBaseConfigBuilder] result=Passed scope=Scenario " +
                "scenario=scenario.ch01.m02.establish_base seed=2002001");
        }

        public static void BuildMissionDefinition()
        {
            MissionDefinitionConfig mission = LoadOrCreate<MissionDefinitionConfig>(MissionPath);
            SerializedObject serialized = new(mission);
            Set(serialized, "missionId", MissionId);
            Set(serialized, "schemaVersion", 1);
            Set(serialized, "displayNameKey", "mission.m02.name");
            Set(serialized, "displaySummaryKey", "mission.m02.summary");
            Set(serialized, "locationNameKey", "mission.m02.location");
            Set(serialized, "scenarioId", "scenario.ch01.m02.establish_base");
            Set(serialized, "operationMapId", "opmap.ch01.forward_post_01");
            Set(serialized, "briefingSequenceId", "seq.ch01.m02.brief");
            Set(serialized, "commsSequenceId", "seq.ch01.m02.comms");
            Set(serialized, "debriefSequenceId", "seq.ch01.m02.debrief");
            SetArray(serialized, "objectives", 3, PopulateObjective);
            SetArray(serialized, "stars", 3, PopulateStar);
            SetArray(serialized, "firstClearRewards", 3, PopulateFirstClearReward);
            SetArray(serialized, "replayRewards", 1, (reward, _) =>
            {
                Set(reward, "kind", 1);
                Set(reward, "rewardConfigId", string.Empty);
                Set(reward, "displayTextKey", "mission.reward.credits");
                Set(reward, "amount", 300);
            });

            TacticalCommandMode[] commands =
            {
                TacticalCommandMode.Select,
                TacticalCommandMode.Move,
                TacticalCommandMode.Attack,
                TacticalCommandMode.Hold,
                TacticalCommandMode.Stop,
                TacticalCommandMode.Build
            };
            SerializedProperty commandArray = serialized.FindProperty("commandPolicy")
                .FindPropertyRelative("allowedCommands");
            commandArray.arraySize = commands.Length;
            for (int index = 0; index < commands.Length; index++)
                commandArray.GetArrayElementAtIndex(index).enumValueIndex = (int)commands[index];

            Set(serialized, "replayAllowed", true);
            Set(serialized, "replayTutorialDefaultEnabled", false);
            Set(serialized, "requireOperationMapReady", true);
            Set(serialized, "requireGridReady", true);
            Set(serialized, "requireUnitCatalogReady", true);
            SetStringArray(serialized, "requiredFeatureIds", new[]
            {
                "feature.operation_map",
                "feature.unit_catalog",
                "feature.tactical_commands"
            });
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(mission);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[M02EstablishBaseConfigBuilder] result=Passed " +
                "scope=MissionDefinition mission=saga.ch01.m02.establish_base");
        }

        public static void ConfigureBarracksProduction()
        {
            BuildingDefinitionAuthoringConfig barracks =
                AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(BarracksConfigPath);
            GameObject riflePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RequiredRiflePrefabPath);
            if (barracks == null || riflePrefab == null)
            {
                throw new InvalidOperationException(
                    $"M02 requires the canonical Barracks config and rifle prefab: " +
                    $"'{BarracksConfigPath}', '{RequiredRiflePrefabPath}'.");
            }

            SerializedObject serialized = new(barracks);
            SerializedProperty productions = serialized.FindProperty("productions");
            productions.arraySize = 1;
            productions.GetArrayElementAtIndex(0)
                .FindPropertyRelative("spawnUnitPrefab")
                .objectReferenceValue = riflePrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(barracks);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[M02EstablishBaseConfigBuilder] result=Passed " +
                "scope=BarracksProduction entries=1 unit=Unit_Chr_Soldier_Male_02_Alt_04");
        }

        private static void PopulateObjective(SerializedProperty objective, int index)
        {
            string[] ids =
            {
                "obj.ch01.m02.build_forward_barracks",
                "obj.ch01.m02.produce_rifle_squad",
                "obj.ch01.m02.defend_forward_post"
            };
            string[] textKeys =
            {
                "mission.m02.objective.build_forward_barracks",
                "mission.m02.objective.produce_rifle_squad",
                "mission.m02.objective.defend_forward_post"
            };
            int[] rules = { 3, 4, 5 };
            Set(objective, "objectiveId", ids[index]);
            Set(objective, "displayTextKey", textKeys[index]);
            Set(objective, "rule", rules[index]);
            Set(objective, "missionRoleId", index == 2 ? "role.friendly.forward_post" : string.Empty);
            Set(objective, "targetConfigId", index switch
            {
                0 => "Building_Barrack",
                1 => "Unit_Chr_Soldier_Male_02_Alt_04",
                _ => string.Empty
            });
            Set(objective, "requiredCount", 1);
            Set(objective, "failureOnRuleBreak", index == 2);
        }

        private static void PopulateStar(SerializedProperty star, int index)
        {
            int[] rules = { 1, 4, 3 };
            string[] textKeys =
            {
                "mission.m02.star.complete_mission",
                "mission.m02.star.keep_civilians_safe",
                "mission.m02.star.build_under_five_minutes"
            };
            Set(star, "starIndex", index + 1);
            Set(star, "rule", rules[index]);
            Set(star, "displayTextKey", textKeys[index]);
            Set(star, "threshold", index == 2 ? 300000 : 0);
        }

        private static void PopulateFirstClearReward(SerializedProperty reward, int index)
        {
            bool credits = index == 1;
            Set(reward, "kind", credits ? 1 : 0);
            Set(reward, "rewardConfigId", index switch
            {
                0 => "reward.commander_xp",
                2 => "reward.ch01.m02.production_unlock",
                _ => string.Empty
            });
            Set(reward, "displayTextKey", index switch
            {
                0 => "mission.reward.commander_xp",
                1 => "mission.reward.credits",
                _ => "mission.m02.reward.barracks_unlock"
            });
            Set(reward, "amount", index switch { 0 => 320, 1 => 1500, _ => 1 });
        }

        private static void PopulateScenarioGroup(SerializedProperty group, int index)
        {
            bool friendly = index == 0;
            Set(group, "groupId", friendly
                ? "group.ch01.m02.command_squad"
                : "group.ch01.m02.hostile_patrol");
            Set(group, "factionIndex", friendly ? 1 : 2);
            string[] keys = friendly
                ? new[]
                {
                    "unit.jrc.rifle_male_02_alt_02", "unit.jrc.rifle_male_02_alt_04",
                    "unit.jrc.rifle_female_01_alt_01", "unit.jrc.rifle_female_02_alt_01"
                }
                : new[] { "unit.ash.courier", "unit.ash.warden", "unit.ash.broker" };
            string[] runtimeKeys = friendly
                ? new[]
                {
                    "Unit_Chr_Soldier_Male_02_Alt_02", "Unit_Chr_Soldier_Male_02_Alt_04",
                    "Unit_Chr_Soldier_Female_01_Alt_01", "Unit_Chr_Soldier_Female_02_Alt_01"
                }
                : new[]
                {
                    "Unit_Chr_Insurgent_Male_03", "Unit_Chr_Insurgent_Female_01",
                    "Unit_Chr_Insurgent_Female_02"
                };
            string[] guids = friendly
                ? new[]
                {
                    "70d525bdf4894529869cd1402ff5d62e", "a3d36fd8847164cc596e0b5ba7bd9bb9",
                    "970069fef3e4437195c3225a1615e384", "b9481cc1ec42499c96846b5d161ac7b2"
                }
                : new[]
                {
                    "fe23cbf9678344f4b182169b49fe68b6", "01045d2a58ec4359b395696309684ffa",
                    "8093159068194fc187efe5c356116e9b"
                };
            SerializedProperty units = group.FindPropertyRelative("units");
            units.arraySize = keys.Length;
            for (int unitIndex = 0; unitIndex < keys.Length; unitIndex++)
            {
                SerializedProperty unit = units.GetArrayElementAtIndex(unitIndex);
                Set(unit, "unitConfigKey", keys[unitIndex]);
                Set(unit, "runtimePrefabSourceKey", runtimeKeys[unitIndex]);
                Set(unit, "expectedAssetGuid", guids[unitIndex]);
                Set(unit, "spawnAnchorId", friendly
                    ? "anchor.ch01.m02.friendly_spawn"
                    : "anchor.ch01.m02.hostile_spawn");
                Set(unit, "missionRoleId", friendly
                    ? "role.friendly.command_squad"
                    : "role.hostile.patrol");
                Set(unit, "count", 1);
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Missing M02 canonical dependency '{path}'.");
            return asset;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void SetArray(
            SerializedObject target,
            string name,
            int count,
            Action<SerializedProperty, int> fill)
        {
            SerializedProperty array = target.FindProperty(name);
            array.arraySize = count;
            for (int index = 0; index < count; index++)
                fill(array.GetArrayElementAtIndex(index), index);
        }

        private static void SetStringArray(SerializedObject target, string name, string[] values)
        {
            SetStringArray(target.FindProperty(name), values);
        }

        private static void SetStringArray(SerializedProperty array, string[] values)
        {
            array.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                array.GetArrayElementAtIndex(index).stringValue = values[index];
        }

        private static void Set(SerializedObject target, string name, string value) =>
            Set(target.FindProperty(name), value);
        private static void Set(SerializedObject target, string name, int value) =>
            Set(target.FindProperty(name), value);
        private static void Set(SerializedObject target, string name, bool value) =>
            Set(target.FindProperty(name), value);
        private static void Set(SerializedProperty target, string name, string value) =>
            Set(target.FindPropertyRelative(name), value);
        private static void Set(SerializedProperty target, string name, int value) =>
            Set(target.FindPropertyRelative(name), value);
        private static void Set(SerializedProperty target, string name, bool value) =>
            Set(target.FindPropertyRelative(name), value);
        private static void Set(SerializedProperty property, string value) => property.stringValue = value;
        private static void Set(SerializedProperty property, int value) => property.intValue = value;
        private static void Set(SerializedProperty property, bool value) => property.boolValue = value;
    }
}
