using Unity.Entities;
using UnityEngine;

// Managed component used to pass Mesh/Material object references from baking to runtime setup.
public sealed class UnitPoseMeshesSetup : IComponentData
{
    public Mesh IdleMesh;
    public Mesh WalkMesh;
    public Mesh AttackMesh;
    public Material Material;
}
