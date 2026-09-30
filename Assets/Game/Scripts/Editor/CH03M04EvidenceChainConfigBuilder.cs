using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Game.Missions.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M04EvidenceChainConfigBuilder
    {
        public const string MissionId = CampaignMissionSequence.EvidenceChain;
        public const string ScenarioId = "scenario.ch03.m04.evidence_chain";
        public const string MapId = "opmap.ch03.evidence_chain_01";
        public const string MissionPath = "Assets/Game/Configs/Missions/Chapter03/MissionDefinition_Ch03_M04_EvidenceChain.asset";
        public const string ScenarioPath = "Assets/Game/Configs/Scenarios/Chapter03/ScenarioSetup_Ch03_M04_EvidenceChain.asset";
        public const string MapPath = "Assets/Game/Configs/OperationMaps/Chapter03/OperationMap_Ch03_EvidenceChain01.asset";
        private const string PassengerRole = "role.friendly.archive_witness";
        private const string CarrierRole = "role.friendly.evidence_apc";
        private const string AircraftRole = "role.friendly.evidence_airlift";

        [MenuItem("Game/Campaign/Evidence Chain/Build Configuration")]
        public static void Build()
        {
            foreach (string category in new[] {"Missions", "Scenarios", "OperationMaps"})
                Directory.CreateDirectory("Assets/Game/Configs/" + category + "/Chapter03");
            AssetDatabase.Refresh();
            BuildMap(); BuildScenario(); BuildMission(); M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            Require(MissionDefinitionContractValidation.TryValidateCatalog(
                Load<MissionDefinitionCatalogConfig>(M01FirstContactConfigBuilder.CatalogPath), out string error), error);
            AssetDatabase.SaveAssets();
            Debug.Log("[EvidenceChainConfig] result=Passed chapter=3 mission=4 passengers=2 carrier=APC extraction=helicopter");
        }

        private static void BuildMap()
        {
            CH03M04EvidenceChainWorldBuilder.Build();
            // Evidence Chain retains its urban source while Airlift migrates independently.
            var map = Clone<OperationMapDefinition>(M04AirliftConfigBuilder.LegacyMapPath, MapPath);
            var data = new SerializedObject(map);
            ReplaceStrings(data, "ch01.m04", "ch03.m04");
            Set(data, "operationMapId", MapId);
            // The source airlift mission lands in the road. Evidence Chain uses the
            // existing off-road helipad beside that road instead.
            var anchors = data.FindProperty("anchors");
            for (int i = 0; i < anchors.arraySize; i++)
            {
                var anchor = anchors.GetArrayElementAtIndex(i);
                string id = anchor.FindPropertyRelative("anchorId").stringValue;
                if (id == "anchor.ch03.m04.landing" || id == "anchor.ch03.m04.aircraft")
                    anchor.FindPropertyRelative("position").vector3Value =
                        new Vector3(1006.93f, 0.121525705f, 393.26f);
                if (id == "anchor.ch03.m04.aircraft")
                    anchor.FindPropertyRelative("radius").floatValue = 0f;
            }
            data.FindProperty("additionalBuildingPlacements").objectReferenceValue =
                Load<MapBuildingPlacementConfig>(CH03M04EvidenceChainWorldBuilder.PlacementsPath);
            Set(data, "planningCameraId", "camera.ch03.m04.planning");
            Set(data, "battleCameraId", "camera.ch03.m04.battle");
            Set(data.FindProperty("minimap"), "minimapId", "minimap.ch03.m04.evidence_chain");
            using var hash = SHA256.Create();
            string contentHash = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(
                "CH03M04-EvidenceChain-v1:" + map.ContentHash))).Replace("-", string.Empty).ToLowerInvariant();
            Set(data, "contentHash", contentHash); Set(data, "generatedMetadataHash", contentHash);
            data.ApplyModifiedPropertiesWithoutUndo();
            Require(map.TryValidateMetadata(out string error) && map.TryValidateLocalContentReferences(out error), error);
            EditorUtility.SetDirty(map);
        }

        private static void BuildScenario()
        {
            var scenario = Clone<ScenarioSetupConfig>(M04AirliftConfigBuilder.ScenarioPath, ScenarioPath);
            var data = new SerializedObject(scenario);
            ReplaceStrings(data, "ch01.m04", "ch03.m04");
            Set(data, "scenarioId", ScenarioId); Set(data, "operationMapId", MapId);
            Set(data, "deterministicSeed", 3004001);
            var map=Load<OperationMapDefinition>(MapPath);
            var required=data.FindProperty("requiredAnchors");required.arraySize=map.Anchors.Length;
            for(int i=0;i<required.arraySize;i++)
            {
                var entry=required.GetArrayElementAtIndex(i);
                Set(entry,"anchorId",map.Anchors[i].AnchorId);Set(entry,"kind",(int)map.Anchors[i].Kind);
            }
            var groups = data.FindProperty("unitGroups");
            for (int i = 0; i < groups.arraySize; i++)
            {
                var group = groups.GetArrayElementAtIndex(i);
                string groupId = group.FindPropertyRelative("groupId").stringValue;
                var units = group.FindPropertyRelative("units");
                if (groupId.EndsWith(".specialists", StringComparison.Ordinal))
                {
                    units.arraySize = 2;
                    string[] prefabs = {"Chr_Civilian_Male_01", "Chr_Civilian_Female_02"};
                    for (int j = 0; j < 2; j++)
                    {
                        var unit = units.GetArrayElementAtIndex(j);
                        string path = "Assets/Game/Prefabs/Characters/Unit_" + prefabs[j] + ".prefab";
                        Require(Load<GameObject>(path) != null, path);
                        Set(unit, "unitConfigKey", "unit.ch03.m04." + prefabs[j].ToLowerInvariant());
                        Set(unit, "runtimePrefabSourceKey", "Unit_" + prefabs[j]);
                        Set(unit, "expectedAssetGuid", AssetDatabase.AssetPathToGUID(path));
                        Set(unit, "missionRoleId", PassengerRole);
                    }
                }
                else if (groupId.EndsWith(".carrier", StringComparison.Ordinal)) Set(units.GetArrayElementAtIndex(0), "missionRoleId", CarrierRole);
                else if (groupId.EndsWith(".aircraft", StringComparison.Ordinal)) Set(units.GetArrayElementAtIndex(0), "missionRoleId", AircraftRole);
            }
            var extraction = data.FindProperty("extraction");
            Set(extraction, "passengerRoleId", PassengerRole); Set(extraction, "carrierRoleId", CarrierRole);
            Set(extraction, "aircraftRoleId", AircraftRole); Set(extraction, "requiredPassengers", 2);
            Set(extraction, "deadlineMilliseconds", 660000); Set(extraction, "secureHoldMilliseconds", 16000);
            data.ApplyModifiedPropertiesWithoutUndo();
            Require(scenario.TryValidate(out string error), error); EditorUtility.SetDirty(scenario);
        }

        private static void BuildMission()
        {
            var mission = Clone<MissionDefinitionConfig>(M04AirliftConfigBuilder.MissionPath, MissionPath);
            var data = new SerializedObject(mission);
            Set(data, "missionId", MissionId); Set(data, "scenarioId", ScenarioId); Set(data, "operationMapId", MapId);
            Set(data, "displayNameKey", "mission.evidence_chain.name");
            Set(data, "displaySummaryKey", "mission.evidence_chain.summary");
            Set(data, "locationNameKey", "mission.evidence_chain.location");
            Set(data, "briefingSequenceId", "seq.ch03.m04.brief");
            Set(data, "commsSequenceId", "seq.ch03.m04.comms");
            Set(data, "debriefSequenceId", "seq.ch03.m04.debrief");
            string[] names = {"extract", "carrier", "landing"};
            string[] roles = {PassengerRole, CarrierRole, AircraftRole};
            var objectives = data.FindProperty("objectives");
            for (int i = 0; i < objectives.arraySize; i++)
            {
                var objective = objectives.GetArrayElementAtIndex(i);
                Set(objective, "objectiveId", "obj.ch03.m04." + names[i]);
                Set(objective, "displayTextKey", "mission.evidence_chain.objective." + names[i]);
                Set(objective, "missionRoleId", roles[i]);
                Set(objective, "requiredCount", i == 0 ? 2 : 1);
            }
            var stars = data.FindProperty("stars");
            for (int i = 0; i < stars.arraySize; i++)
            {
                Set(stars.GetArrayElementAtIndex(i), "displayTextKey", "mission.evidence_chain.star." + (i + 1));
                if (i == 2) Set(stars.GetArrayElementAtIndex(i), "threshold", 480000);
            }
            var rewards = data.FindProperty("firstClearRewards");
            rewards.arraySize = 2;
            Set(rewards.GetArrayElementAtIndex(0), "rewardConfigId", "reward.commander_xp");
            Set(rewards.GetArrayElementAtIndex(0), "displayTextKey", "mission.reward.commander_xp");
            Set(rewards.GetArrayElementAtIndex(0), "amount", 1500);
            Set(rewards.GetArrayElementAtIndex(1), "rewardConfigId", string.Empty);
            Set(rewards.GetArrayElementAtIndex(1), "displayTextKey", "mission.reward.credits");
            Set(rewards.GetArrayElementAtIndex(1), "amount", 7000);
            data.ApplyModifiedPropertiesWithoutUndo();
            Require(MissionDefinitionContractValidation.TryValidateDefinition(mission, out string error), error);
            EditorUtility.SetDirty(mission);
        }

        private static void ReplaceStrings(SerializedObject data, string from, string to)
        {
            var property = data.GetIterator(); if (!property.Next(true)) return;
            do if (property.propertyType == SerializedPropertyType.String && property.stringValue.IndexOf(from, StringComparison.Ordinal) >= 0)
                property.stringValue = property.stringValue.Replace(from, to);
            while (property.Next(true));
        }
        private static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException(path);
        private static T Clone<T>(string source, string target) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(target);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, target); }
            EditorUtility.CopySerialized(Load<T>(source), asset); asset.name = Path.GetFileNameWithoutExtension(target); return asset;
        }
        private static void Set(SerializedObject data, string field, object value) => Set(data.FindProperty(field), value);
        private static void Set(SerializedProperty parent, string field, object value) => Set(parent.FindPropertyRelative(field), value);
        private static void Set(SerializedProperty property, object value)
        {
            if (property == null) throw new InvalidOperationException("Missing Evidence Chain serialized field.");
            switch (value)
            {
                case string text: property.stringValue = text; break;
                case int number: property.intValue = number; break;
                default: throw new ArgumentException(property.propertyPath);
            }
        }
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
    }
}
