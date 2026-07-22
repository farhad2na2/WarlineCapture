using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public sealed class MapBuildingPlacementConfigEntry
    {
        [SerializeField] private string sourcePath;
        [SerializeField] private string category;
        [SerializeField] private GameObject buildingPrefab;
        [SerializeField] private byte factionId;
        [SerializeField] private Vector3 worldCenter;
        [SerializeField] private Vector3 worldPosition;
        [SerializeField] private Vector3 worldEulerAngles;
        [SerializeField] private Vector3 worldScale = Vector3.one;
        [SerializeField] private float yawDegrees;
        [SerializeField] private bool rotateVertical;

        public string SourcePath => sourcePath;
        public string Category => category;
        public GameObject BuildingPrefab => buildingPrefab;
        public byte FactionId => factionId;
        public Vector3 WorldCenter => worldCenter;
        public Vector3 WorldPosition => worldPosition;
        public Vector3 WorldEulerAngles => worldEulerAngles;
        public Vector3 WorldScale => worldScale;
        public float YawDegrees => yawDegrees;
        public bool RotateVertical => rotateVertical;

        public MapBuildingPlacementConfigEntry(
            string sourcePath,
            string category,
            GameObject buildingPrefab,
            byte factionId,
            Vector3 worldCenter,
            Vector3 worldPosition,
            Vector3 worldEulerAngles,
            Vector3 worldScale,
            float yawDegrees,
            bool rotateVertical)
        {
            this.sourcePath = sourcePath;
            this.category = category;
            this.buildingPrefab = buildingPrefab;
            this.factionId = factionId;
            this.worldCenter = worldCenter;
            this.worldPosition = worldPosition;
            this.worldEulerAngles = worldEulerAngles;
            this.worldScale = worldScale;
            this.yawDegrees = yawDegrees;
            this.rotateVertical = rotateVertical;
        }
    }

    [CreateAssetMenu(menuName = "Game/Scene Config/Map Building Placements")]
    public sealed class MapBuildingPlacementConfig : ScriptableObject
    {
        [SerializeField] private bool spawnOnMatchStart = true;
        [SerializeField] private bool hideAuthoringVisualsAfterSpawn = true;
        [SerializeField] private bool useExistingStaticPresentationWhenAuthoringVisualMissing;
        [SerializeField] private List<MapBuildingPlacementConfigEntry> placements = new();

        public bool SpawnOnMatchStart => spawnOnMatchStart;
        public bool HideAuthoringVisualsAfterSpawn => hideAuthoringVisualsAfterSpawn;
        public bool UseExistingStaticPresentationWhenAuthoringVisualMissing =>
            useExistingStaticPresentationWhenAuthoringVisualMissing;
        public IReadOnlyList<MapBuildingPlacementConfigEntry> Placements => placements;

    #if UNITY_EDITOR
        public void EditorSetPlacements(List<MapBuildingPlacementConfigEntry> newPlacements)
        {
            placements = newPlacements ?? new List<MapBuildingPlacementConfigEntry>();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void EditorSetUseExistingStaticPresentationWhenAuthoringVisualMissing(bool value)
        {
            useExistingStaticPresentationWhenAuthoringVisualMissing = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    #endif
    }
}
