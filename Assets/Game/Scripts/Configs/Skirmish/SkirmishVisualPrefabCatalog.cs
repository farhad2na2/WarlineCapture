using System.Collections.Generic;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    public sealed class SkirmishVisualPrefabCatalog
    {
        private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private readonly List<GameObject> owned = new List<GameObject>();

        public int Count => prefabs.Count;
        public bool BoundFromRegistry { get; private set; }

        public void Bind(string key, GameObject prefab, bool takeOwnership = false)
        {
            if (string.IsNullOrEmpty(key) || prefab == null)
                return;
            prefabs[key] = prefab;
            if (takeOwnership)
                owned.Add(prefab);
        }

        public void BindRegistry(UnitPrefabRegistryAuthoringConfig registry)
        {
            if (registry?.UnitSpawnPrefabs == null)
                return;
            int bound = 0;
            for (int i = 0; i < registry.UnitSpawnPrefabs.Count; i++)
            {
                GameObject prefab = registry.UnitSpawnPrefabs[i];
                if (prefab == null)
                    continue;
                Bind(prefab.name, prefab);
                bound++;
            }

            BoundFromRegistry = bound > 0;
        }

        public bool TryGet(string key, out GameObject prefab)
        {
            return prefabs.TryGetValue(key ?? string.Empty, out prefab) && prefab != null;
        }

        public bool Contains(string key) => !string.IsNullOrEmpty(key) && prefabs.ContainsKey(key);

        public void Dispose()
        {
            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i] != null)
                    SkirmishVisualLifecycle.DestroyOwned(owned[i]);
            }

            owned.Clear();
            prefabs.Clear();
        }

        public static SkirmishVisualPrefabCatalog CreatePresentationStandIns()
        {
            var catalog = new SkirmishVisualPrefabCatalog();
            GameObject staging = SkirmishGroundStagingBuilder.BuildHierarchy();
            staging.SetActive(false);
            catalog.Bind(SkirmishStructureIds.VisualKey(SkirmishStructureIds.GroundStaging), staging, true);
            catalog.Bind(SkirmishStructureIds.VisualKey(SkirmishStructureIds.Barracks),
                CreateStandIn("Building_Barrack", PrimitiveType.Cube, new Color(0.55f, 0.48f, 0.32f), new Vector3(4f, 2.2f, 4f)), true);
            BindStandIn(catalog, "Unit_Chr_Soldier_Male_02_Alt_04", PrimitiveType.Capsule, new Color(0.28f, 0.55f, 0.32f), new Vector3(0.7f, 1.6f, 0.7f));
            BindStandIn(catalog, "Unit_Chr_Soldier_Male_01", PrimitiveType.Capsule, new Color(0.24f, 0.48f, 0.3f), new Vector3(0.7f, 1.6f, 0.7f));
            BindStandIn(catalog, "Unit_Chr_Ghillie_Male_01", PrimitiveType.Capsule, new Color(0.3f, 0.42f, 0.2f), new Vector3(0.7f, 1.6f, 0.7f));
            BindStandIn(catalog, "Unit_Veh_Light_Armored_Car", PrimitiveType.Cube, new Color(0.4f, 0.45f, 0.28f), new Vector3(2.2f, 1f, 1.4f));
            BindStandIn(catalog, "Unit_Veh_APC_Slow", PrimitiveType.Cube, new Color(0.36f, 0.4f, 0.26f), new Vector3(3.2f, 1.3f, 1.8f));
            BindStandIn(catalog, "Unit_Veh_Tank_USA", PrimitiveType.Cube, new Color(0.3f, 0.38f, 0.22f), new Vector3(3.6f, 1.4f, 2.1f));
            return catalog;
        }

        public static SkirmishVisualPrefabCatalog CreateS002TestRegistry()
        {
            var catalog = new SkirmishVisualPrefabCatalog();
            catalog.Bind("Unit_Veh_Tank_USA", CreateStandIn("Unit_Veh_Tank_USA", PrimitiveType.Cube, Color.green, new Vector3(2f, 1f, 1.4f)), true);
            catalog.Bind("Unit_Veh_APC_Slow", CreateStandIn("Unit_Veh_APC_Slow", PrimitiveType.Cube, Color.cyan, new Vector3(1.8f, 0.9f, 1.2f)), true);
            catalog.Bind("Unit_Chr_Soldier_Male_02_Alt_04", CreateStandIn("Unit_Chr_Soldier_Male_02_Alt_04", PrimitiveType.Capsule, Color.yellow, Vector3.one), true);
            catalog.Bind("Unit_Chr_Soldier_Male_01", CreateStandIn("Unit_Chr_Soldier_Male_01", PrimitiveType.Capsule, new Color(0.9f, 0.7f, 0.2f), Vector3.one), true);
            catalog.Bind("Unit_Chr_Ghillie_Male_01", CreateStandIn("Unit_Chr_Ghillie_Male_01", PrimitiveType.Capsule, new Color(0.4f, 0.55f, 0.2f), Vector3.one), true);
            catalog.Bind("Unit_Veh_Light_Armored_Car", CreateStandIn("Unit_Veh_Light_Armored_Car", PrimitiveType.Cube, new Color(0.2f, 0.6f, 0.4f), new Vector3(1.6f, 0.8f, 1.1f)), true);
            GameObject staging = SkirmishGroundStagingBuilder.BuildHierarchy();
            staging.SetActive(false);
            catalog.Bind("Building_GroundStaging", staging, true);
            catalog.Bind("Building_Barrack", CreateStandIn("Building_Barrack", PrimitiveType.Cube, Color.gray, new Vector3(2f, 2f, 2f)), true);
            return catalog;
        }

        public static SkirmishVisualPrefabCatalog CreateS003AirRegistry()
        {
            SkirmishVisualPrefabCatalog catalog = CreateS002TestRegistry();
            BindStandIn(catalog, "Unit_Veh_Missle_Launcher_Air", PrimitiveType.Cube, new Color(0.45f, 0.32f, 0.18f), new Vector3(2.4f, 1.1f, 1.4f));
            BindStandIn(catalog, "Unit_Veh_Helicopter_Attack_Small", PrimitiveType.Capsule, new Color(0.25f, 0.45f, 0.62f), new Vector3(1.4f, 0.8f, 1.4f));
            BindStandIn(catalog, "Unit_Veh_Helicopter_Attack", PrimitiveType.Capsule, new Color(0.2f, 0.35f, 0.55f), new Vector3(1.8f, 0.9f, 1.6f));
            BindStandIn(catalog, "Unit_Veh_Helicopter_Transport", PrimitiveType.Capsule, new Color(0.35f, 0.5f, 0.55f), new Vector3(1.8f, 0.9f, 1.8f));
            BindStandIn(catalog, "Unit_Veh_Jet_02", PrimitiveType.Cube, new Color(0.55f, 0.58f, 0.62f), new Vector3(3.2f, 0.6f, 2.2f));
            BindStandIn(catalog, "Building_Helipad", PrimitiveType.Cube, new Color(0.32f, 0.34f, 0.3f), new Vector3(6f, 0.4f, 6f));
            return catalog;
        }

        private static void BindStandIn(
            SkirmishVisualPrefabCatalog catalog,
            string key,
            PrimitiveType type,
            Color color,
            Vector3 scale)
        {
            catalog.Bind(key, CreateStandIn(key, type, color, scale), true);
        }

        private static GameObject CreateStandIn(string key, PrimitiveType type, Color color, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = key;
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = new Material(renderer.sharedMaterial) { color = color };
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
            go.SetActive(false);
            return go;
        }
    }

    public static class SkirmishVisualLifecycle
    {
        public static void DestroyOwned(Object instance)
        {
            if (instance == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(instance);
            else
                Object.DestroyImmediate(instance);
        }
    }
}
