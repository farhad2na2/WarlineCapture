using Game.Components;
using UnityEngine;

namespace Game.Configs
{
    /// <summary>One baked set of local road faces, shared by every placement of that mesh.</summary>
    public sealed class MapSurfaceMeshGeometryAsset : ScriptableObject
    {
        [System.NonSerialized] private Mesh source;
        [SerializeField] private int subMeshIndex=-1;
        [SerializeField] private MapSurfaceMeshTriangle[] triangles;
        public Mesh Source=>source;
        public int SubMeshIndex=>subMeshIndex;
        public MapSurfaceMeshTriangle[] Triangles=>triangles;
        public void Configure(Mesh mesh,int submesh,MapSurfaceMeshTriangle[] faces)
        {source=mesh;subMeshIndex=submesh;triangles=faces;}
    }
}
