#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Configs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Authoring
{
    public sealed partial class OperationMapBuildingAuthoring
    {
        internal static void RequireFootprintGridCount(int count)
        {
            if (count < 0 || count > 1)
                throw new InvalidOperationException("At most one owning-scene GridAuthoring is supported; found " + count + ".");
        }

        internal static void RequireFootprintGridFrame(Vector3 origin, float cellSize, int width, int height)
        {
            if (width <= 0 || height <= 0 || cellSize <= 0 ||
                float.IsNaN(cellSize) || float.IsInfinity(cellSize) ||
                float.IsNaN(origin.x) || float.IsInfinity(origin.x) ||
                float.IsNaN(origin.y) || float.IsInfinity(origin.y) ||
                float.IsNaN(origin.z) || float.IsInfinity(origin.z))
                throw new InvalidOperationException("A finite positive owning-scene grid frame is required.");
        }

        internal static void ProjectAuthoredFootprint(Bounds bounds, Vector3 gridOrigin, float cellSize,
            ref int2 origin, ref int2 footprint)
        {
            var min = (bounds.min-gridOrigin)/cellSize;
            var max = (bounds.max-gridOrigin)/cellSize;
            origin = new int2(Mathf.FloorToInt(min.x+.0001f),Mathf.FloorToInt(min.z+.0001f));
            footprint = math.max(new int2(1),new int2(Mathf.CeilToInt(max.x-.0001f),Mathf.CeilToInt(max.z-.0001f))-origin);
        }

        private sealed partial class OperationMapBuildingBaker
        {
            private void ResolveAuthoredFootprint(OperationMapBuildingAuthoring authoring, ref int2 origin, ref int2 footprint)
            {
                if (authoring.intactVisualRoot == null) return;
                var scene = authoring.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                    throw new InvalidOperationException("Operation-map footprint requires its owning loaded scene.");
                var candidates = new List<GridAuthoring>();
                foreach (var root in scene.GetRootGameObjects())
                    candidates.AddRange(GetComponentsInChildren<GridAuthoring>(root));
                RequireFootprintGridCount(candidates.Count);
                GridAuthoringConfig grid;
                if (candidates.Count == 0)
                {
                    // Existing preparation slices without a scene grid retain the original global frame.
                    grid = AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(
                        "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset");
                }
                else
                {
                    var owner = candidates[0];
                    if (owner.gameObject.scene != scene)
                        throw new InvalidOperationException("Operation-map footprint grid belongs to another scene.");
                    DependsOn(owner);
                    using var serialized = new SerializedObject(owner);
                    var property = serialized.FindProperty("config");
                    grid = property?.objectReferenceValue as GridAuthoringConfig;
                }
                if (grid == null)
                    throw new InvalidOperationException("Operation-map footprint requires the selected grid's configuration.");
                RequireFootprintGridFrame(grid.Origin, grid.CellSize, grid.Width, grid.Height);
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
                ProjectAuthoredFootprint(bounds, grid.Origin, grid.CellSize, ref origin, ref footprint);
            }
        }
    }
}
#endif
