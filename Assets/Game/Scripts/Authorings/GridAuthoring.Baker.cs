using Game.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Authoring
{
    public partial class GridAuthoring
    {
        [BakingVersion("WarlineCapture", 2)]
        private class GridBaker : Baker<GridAuthoring>
        {
            public override void Bake(GridAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new GridConfig
                {
                    Width = authoring.Width,
                    Height = authoring.Height,
                    CellSize = authoring.CellSize,
                    Origin = authoring.config != null ? (float3)authoring.config.Origin : (float3)authoring.transform.position
                });

                var walkable = AddBuffer<GridWalkable>(entity);
                var roads = AddBuffer<GridRoad>(entity);
                var sidewalks = AddBuffer<GridRoadSidewalk>(entity);
                var dirtRoads = AddBuffer<GridRoadDirt>(entity);
                int size = authoring.Width * authoring.Height;
                walkable.ResizeUninitialized(size);
                roads.ResizeUninitialized(size);
                sidewalks.ResizeUninitialized(size);
                dirtRoads.ResizeUninitialized(size);
                for (int i = 0; i < size; i++)
                {
                    walkable[i] = new GridWalkable { Value = 1 };
                    roads[i] = new GridRoad { Value = 0 };
                    sidewalks[i] = new GridRoadSidewalk { Value = 0 };
                    dirtRoads[i] = new GridRoadDirt { Value = 0 };
                }

                if (authoring.BlockedCells != null)
                {
                    foreach (var v in authoring.BlockedCells)
                    {
                        if ((uint)v.x >= (uint)authoring.Width || (uint)v.y >= (uint)authoring.Height)
                            continue;

                        int index = v.x + v.y * authoring.Width;
                        walkable[index] = new GridWalkable { Value = 0 };
                    }
                }
#if UNITY_EDITOR
                if (authoring.config != null)
                {
                    DependsOn(authoring.config);
                    string path = UnityEditor.AssetDatabase.GetAssetPath(authoring.config);
                    string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                    if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                    {
                        using var sha = System.Security.Cryptography.SHA256.Create();
                        string hash = System.BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
                        AddComponent(entity, GridWalkableSourceWitness.Capture(guid, hash, walkable));
                    }
                }
#endif
            }
        }
    }
}
