#if UNITY_EDITOR
using Game.Configs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Authoring
{
    public sealed partial class OperationMapBuildingAuthoring
    {
        private sealed partial class OperationMapBuildingBaker
        {
            private void ResolveAuthoredFootprint(OperationMapBuildingAuthoring authoring, ref int2 origin, ref int2 footprint)
            {
                if (authoring.intactVisualRoot == null) return;
                var grid = AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(
                    "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset");
                if (grid == null || grid.CellSize <= 0) return;
                DependsOn(grid);
                bool found = false;
                Bounds bounds = default;
                foreach (var renderer in GetComponentsInChildren<MeshRenderer>(authoring.intactVisualRoot))
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                        !renderer.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null) continue;
                    DependsOn(renderer);
                    DependsOn(filter.sharedMesh);
                    DependsOn(renderer.transform);
                    Bounds mesh = filter.sharedMesh.bounds;
                    for (int x=0;x<2;x++) for (int y=0;y<2;y++) for (int z=0;z<2;z++)
                    {
                        var point = renderer.transform.TransformPoint(new Vector3(x==0 ? mesh.min.x : mesh.max.x,
                            y==0 ? mesh.min.y : mesh.max.y, z==0 ? mesh.min.z : mesh.max.z));
                        if (!found) {bounds=new Bounds(point,Vector3.zero);found=true;} else bounds.Encapsulate(point);
                    }
                }
                if (!found) return;
                // Authored map instances can have their own rotation and scale. Never
                // combine a historical origin with a newly resized catalog footprint.
                var min = (bounds.min-grid.Origin)/grid.CellSize;
                var max = (bounds.max-grid.Origin)/grid.CellSize;
                origin = new int2(Mathf.FloorToInt(min.x+.0001f),Mathf.FloorToInt(min.z+.0001f));
                footprint = math.max(new int2(1),new int2(Mathf.CeilToInt(max.x-.0001f),Mathf.CeilToInt(max.z-.0001f))-origin);
            }
        }
    }
}
#endif
