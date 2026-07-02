using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class RoadBuildDefinitionProjectionSystem : SystemBase
    {
        protected override void OnCreate()
        {
            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }

        public void BuildDefinitions(
            GameObject soldierBasePrefab,
            Vector2Int soldierBaseFootprintCells,
            RoadBuildPlacementStorageCompositionSystemHelper storageSystem)
        {
            var soldierBaseDefinition = new BuildingDefinition
            {
                DisplayName = "Soldier Base",
                Prefab = soldierBasePrefab,
                FootprintCells = new Vector2Int(
                    Mathf.Max(1, soldierBaseFootprintCells.x),
                    Mathf.Max(1, soldierBaseFootprintCells.y))
            };

            CacheBuildingBounds(soldierBaseDefinition);
            storageSystem.SetSoldierBaseDefinition(soldierBaseDefinition);
        }

        private void CacheBuildingBounds(BuildingDefinition definition)
        {
            if (definition == null || definition.Prefab == null || definition.HasLocalBounds)
                return;

            GameObject temp = UnityEngine.Object.Instantiate(definition.Prefab);
            temp.hideFlags = HideFlags.HideAndDontSave;
            if (TryGetLocalBounds(temp, out Bounds localBounds))
            {
                definition.LocalBounds = localBounds;
                definition.HasLocalBounds = true;
            }

            UnityEngine.Object.Destroy(temp);
        }

        private static bool TryGetLocalBounds(GameObject target, out Bounds bounds)
        {
            bounds = default;
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Matrix4x4 worldToLocal = target.transform.worldToLocalMatrix;
            foreach (Renderer renderer in renderers)
            {
                Bounds rendererBounds = renderer.bounds;
                Vector3 min = rendererBounds.min;
                Vector3 max = rendererBounds.max;
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int z = 0; z < 2; z++)
                        {
                            Vector3 corner = new(
                                x == 0 ? min.x : max.x,
                                y == 0 ? min.y : max.y,
                                z == 0 ? min.z : max.z);
                            Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corner);
                            if (!hasBounds)
                            {
                                bounds = new Bounds(localCorner, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                bounds.Encapsulate(localCorner);
                            }
                        }
                    }
                }
            }

            return hasBounds;
        }
    }
}
