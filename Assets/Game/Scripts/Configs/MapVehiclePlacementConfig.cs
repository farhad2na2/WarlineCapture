using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public sealed class MapVehiclePlacementConfigEntry
    {
        [SerializeField] private string sourcePath;
        [SerializeField] private string category;
        [SerializeField] private GameObject vehiclePrefab;
        [SerializeField] private string vehicleSourceKey;
        [SerializeField] private byte factionId;
        [SerializeField] private Vector3 worldCenter;
        [SerializeField] private Vector3 worldPosition;
        [SerializeField] private Vector3 worldEulerAngles;
        [SerializeField] private Vector3 worldScale = Vector3.one;

        public string SourcePath => sourcePath;
        public string Category => category;
        public GameObject VehiclePrefab => vehiclePrefab;
        public string VehicleSourceKey =>
            !string.IsNullOrWhiteSpace(vehicleSourceKey)
                ? vehicleSourceKey
                : GetVehicleSourceKey(vehiclePrefab);
        public byte FactionId => factionId;
        public Vector3 WorldCenter => worldCenter;
        public Vector3 WorldPosition => worldPosition;
        public Vector3 WorldEulerAngles => worldEulerAngles;
        public Vector3 WorldScale => worldScale;

        public MapVehiclePlacementConfigEntry(
            string sourcePath,
            string category,
            GameObject vehiclePrefab,
            byte factionId,
            Vector3 worldCenter,
            Vector3 worldPosition,
            Vector3 worldEulerAngles,
            Vector3 worldScale)
        {
            this.sourcePath = sourcePath;
            this.category = category;
            this.vehiclePrefab = vehiclePrefab;
            vehicleSourceKey = GetVehicleSourceKey(vehiclePrefab);
            this.factionId = factionId;
            this.worldCenter = worldCenter;
            this.worldPosition = worldPosition;
            this.worldEulerAngles = worldEulerAngles;
            this.worldScale = worldScale;
        }

        private static string GetVehicleSourceKey(GameObject prefab)
        {
            if (prefab == null || string.IsNullOrWhiteSpace(prefab.name))
                return string.Empty;

            return prefab.name.Replace(" (Clone)", string.Empty).Trim().ToLowerInvariant();
        }
    }

    [CreateAssetMenu(menuName = "Game/Scene Config/Map Vehicle Placements")]
    public sealed class MapVehiclePlacementConfig : ScriptableObject
    {
        [SerializeField] private bool spawnOnMatchStart = true;
        [SerializeField] private bool hideAuthoringVisualsAfterSpawn = true;
        [SerializeField] private List<MapVehiclePlacementConfigEntry> placements = new();

        public bool SpawnOnMatchStart => spawnOnMatchStart;
        public bool HideAuthoringVisualsAfterSpawn => hideAuthoringVisualsAfterSpawn;
        public IReadOnlyList<MapVehiclePlacementConfigEntry> Placements => placements;

    #if UNITY_EDITOR
        public void EditorSetPlacements(List<MapVehiclePlacementConfigEntry> newPlacements)
        {
            placements = newPlacements ?? new List<MapVehiclePlacementConfigEntry>();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    #endif
    }
}
