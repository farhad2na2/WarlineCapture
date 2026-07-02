using Unity.Entities;
using UnityEngine;

// Optional pose mesh setup data; object references stay serializable without making this a managed component.

namespace Game.Components
{
    public struct UnitPoseMeshesSetup : IComponentData
    {
        public UnityObjectRef<Mesh> IdleMesh;
        public UnityObjectRef<Mesh> WalkMesh;
        public UnityObjectRef<Mesh> AttackMesh;
        public UnityObjectRef<Material> Material;
    }
}
