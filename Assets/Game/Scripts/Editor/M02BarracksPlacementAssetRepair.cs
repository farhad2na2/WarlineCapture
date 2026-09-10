using System;
using Game.Composition;
using Game.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M02BarracksPlacementAssetRepair
    {
        private const string Path = "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab";

        public static void Repair()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(Path);
            try
            {
                Transform model = contents.transform.Find("Model");
                if (model == null) throw new InvalidOperationException("Barracks model is missing.");
                // Keep the enlarged model along the long axis of its authored 40x20 reservation.
                // Doors and other attached geometry rotate together with the model.
                model.localRotation = Quaternion.Euler(0, 90, 0);
                PrefabUtility.SaveAsPrefabAsset(contents, Path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            var definitions = new BuildingDefinitionPrefabSystemHelper();
            definitions.ConfigureAuthoringMetadataResolvers(
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
            var definition = definitions.CreateRuntimeBuildingDefinition(prefab, "Barracks", "", Vector2Int.one, 1, new BuildingRunwaySystem());
            if (definition.FootprintCells != new Vector2Int(40, 20) || definition.LocalBounds.size.x > 40 || definition.LocalBounds.size.z > 20)
                throw new InvalidOperationException($"Barracks still exceeds the mission lot: footprint={definition.FootprintCells} bounds={definition.LocalBounds}");
            Debug.Log($"[M02BarracksPlacementAssetRepair] result=Passed footprint={definition.FootprintCells} bounds={definition.LocalBounds}");
        }
    }
}
