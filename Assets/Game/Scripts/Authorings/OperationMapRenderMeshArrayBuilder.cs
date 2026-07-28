using Game.Configs;
using Unity.Rendering;
using UnityEngine;

namespace Game.Authoring
{
    public static class OperationMapRenderMeshArrayBuilder
    {
        public static bool TryBuild(
            OperationMapRenderDatabaseBakeConfig databaseConfig,
            out RenderMeshArray renderMeshArray,
            out string error)
        {
            renderMeshArray = default;
            if (databaseConfig == null)
            {
                error = "Shared render-mesh array requires a generated database config.";
                return false;
            }

            if (!databaseConfig.TryValidateSchema(out error))
                return false;

            Material[] materials = new Material[databaseConfig.Materials.Count];
            for (int index = 0; index < materials.Length; index++)
                materials[index] = databaseConfig.Materials[index].Material;

            Mesh[] meshes = new Mesh[databaseConfig.Meshes.Count];
            for (int index = 0; index < meshes.Length; index++)
                meshes[index] = databaseConfig.Meshes[index].Mesh;

            for (int index = 0; index < databaseConfig.Parts.Count; index++)
            {
                OperationMapRenderPrototypePartConfigRecord part = databaseConfig.Parts[index];
                if (part.MeshIndex < 0 ||
                    part.MeshIndex >= meshes.Length ||
                    part.MaterialIndex < 0 ||
                    part.MaterialIndex >= materials.Length ||
                    part.SubMeshIndex < 0 ||
                    part.SubMeshIndex >= meshes[part.MeshIndex].subMeshCount)
                {
                    error =
                        $"parts[{index}] cannot resolve its mesh/material/submesh indices " +
                        "against the shared render-mesh array.";
                    return false;
                }
            }

            renderMeshArray = new RenderMeshArray(materials, meshes);
            error = null;
            return true;
        }
    }
}
