using System;
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
        public const string BarracksConfigPath =
            "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset";
        public const string RequiredRiflePrefabPath =
            "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab";

        [MenuItem("Game/Campaign/M02/Configure Barracks Production")]
        public static void ConfigureBarracksProductionMenu() => ConfigureBarracksProduction();

        [MenuItem("Game/Campaign/M02/Build Mission Definition")]
        public static void BuildMissionDefinitionMenu() => BuildMissionDefinition();

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

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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
            SerializedProperty array = target.FindProperty(name);
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
