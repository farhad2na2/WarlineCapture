using System;
using Game.Authoring;
using Game.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class RoadGatePlacementAssetRepair
    {
        public const string PrefabPath = "Assets/Game/Prefabs/Buildings/Building_Road_Barrier.prefab";
        public static void RepairAndValidateTutorial()
        { Repair(); M03RadarWarningEditorLaunchProbe.RunRoadGateTutorial(); }

        public static void Repair()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var authoring = root.GetComponent<BuildingDefinitionAuthoring>();
                var arm = Array.Find(root.GetComponentsInChildren<Transform>(true), item => item.name == "Door_Z");
                if (authoring == null || arm == null) throw new InvalidOperationException("Gate authoring or arm missing.");
                var rotation = arm.localRotation;
                var euler = arm.localEulerAngles; euler.z = 0; arm.localEulerAngles = euler;
                if (!BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds(root, out var closedBounds))
                    throw new InvalidOperationException("Cannot measure the closed gate.");
                arm.localRotation = rotation;
                var footprint = new Vector2Int(Mathf.CeilToInt(closedBounds.size.x), Mathf.Max(2, Mathf.CeilToInt(closedBounds.size.z)));
                var data = new SerializedObject(authoring);
                data.FindProperty("footprintCells").vector2IntValue = footprint;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[RoadGatePlacementAssetRepair] result=Passed footprint="+footprint+" closedBounds="+closedBounds);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
