using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Game.Authoring
{
    /// <summary>
    /// Candidate-only root for the virtualized presentation database and its one shared,
    /// deterministically ordered mesh/material array.
    /// This baker creates no proxy slots and does not alter source render entities.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OperationMapVirtualizedPresentationAuthoring : MonoBehaviour
    {
        [SerializeField] private OperationMapRenderDatabaseBakeConfig databaseConfig;
        [SerializeField, Min(0)] private int mapGeneration;

        public OperationMapRenderDatabaseBakeConfig DatabaseConfig => databaseConfig;
        public int MapGeneration => mapGeneration;

        public bool TryValidate(out string error)
        {
            if (databaseConfig == null)
            {
                error = "Virtualized presentation root requires a generated database config.";
                return false;
            }

            if (!databaseConfig.TryValidateSchema(out error))
                return false;

            error = null;
            return true;
        }

        private sealed class Baker : Baker<OperationMapVirtualizedPresentationAuthoring>
        {
            public override void Bake(OperationMapVirtualizedPresentationAuthoring authoring)
            {
                if (!OperationMapRenderMeshArrayBuilder.TryBuild(
                        authoring.DatabaseConfig,
                        out RenderMeshArray renderMeshArray,
                        out _))
                {
                    return;
                }

                DependsOn(authoring.DatabaseConfig);
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new OperationMapRenderDatabaseComponent
                {
                    Blob = default,
                    ContentHash = new FixedString128Bytes(authoring.DatabaseConfig.ContentHash),
                    SchemaVersion = authoring.DatabaseConfig.SchemaVersion,
                    MapGeneration = authoring.mapGeneration
                });
                AddSharedComponentManaged(entity, renderMeshArray);
            }
        }
    }
}
