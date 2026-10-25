using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    internal sealed partial class AutonomousWorld
    {
        internal GeneratedUnitProductionMetadata ActualUnitMetadata { get; private set; }

        internal JObject BindActualUnitMetadata(string path, string sha256, string repositoryRoot)
        {
            if (ActualUnitMetadata != null)
                throw new InvalidOperationException("Source unit metadata binds once per fresh world");
            if (SourceDefinitions == null || Setup == null || StorageBank == null)
                throw new InvalidOperationException("Actual source startup and canonical bank must exist first");

            var templates = SourceDefinitions.Assets
                .Where(pair => pair.Value.Template != Unity.Entities.Entity.Null)
                .ToDictionary(pair => pair.Key, pair => pair.Value.Template, StringComparer.Ordinal);
            ActualUnitMetadata = GeneratedUnitProductionMetadata.Load(
                path, sha256, Setup.SetupHash, Setup.Seed, repositoryRoot,
                SourceDefinitions.Assets, templates, SourceDefinitions.AssetIdentities);

            return new JObject
            {
                ["world_id"] = WorldId,
                ["tick"] = Tick,
                ["metadata_artifact_sha256"] = ActualUnitMetadata.ArtifactSha256,
                ["configured_unit_count"] = ActualUnitMetadata.Ordered.Count,
                ["actual_template_count"] = ActualUnitMetadata.Ordered.Count(row => row.HasActualTemplate),
                ["missing_templates"] = new JArray(ActualUnitMetadata.Ordered
                    .Where(row => !row.HasActualTemplate).Select(row => row.Prefab.name)),
                ["paid_owner_bound"] = false,
                ["full_mission"] = false
            };
        }

        // The paid adapter calls this before the original paid owner. Unknown template
        // support must fail before source slot reservation, payment or queue mutation.
        internal GeneratedUnitProductionMetadata.Row RequireActualProductionUnit(SourceUnitPrefab prefab)
        {
            if (ActualUnitMetadata == null)
                throw new NotSupportedException("Actual source unit metadata has not been bound");
            return ActualUnitMetadata.RequireDeliverable(prefab);
        }
    }
}
