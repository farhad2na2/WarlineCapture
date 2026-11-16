using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Materializes and compiles pinned authored inputs only. This does not
    // publish a map, initialize a world, or grant runtime/native admission.
    internal sealed class SourceExpandedAuthoredSetupInput
    {
        internal readonly SkirmishScenarioDefinitionConfig Definition;
        internal readonly SkirmishExpansionCatalogConfig Catalog;
        internal readonly SourceConfigMaterializer Assets;
        internal readonly List<SkirmishSetupMatrixRow> Matrix;
        internal readonly SkirmishContentManifest Manifest;

        private SourceExpandedAuthoredSetupInput(SkirmishScenarioDefinitionConfig definition,
            SkirmishExpansionCatalogConfig catalog, SourceConfigMaterializer assets,
            List<SkirmishSetupMatrixRow> matrix, SkirmishContentManifest manifest)
        { Definition = definition; Catalog = catalog; Assets = assets; Matrix = matrix; Manifest = manifest; }

        internal static SourceExpandedAuthoredSetupInput Prepare(string graphPath, string graphSha256,
            string matrixPath, string matrixSha256, string repositoryRoot, string mission)
        {
            if (!Path.IsPathFullyQualified(repositoryRoot ?? "") || !Path.IsPathFullyQualified(graphPath ?? "") ||
                !Path.IsPathFullyQualified(matrixPath ?? "") || !SkirmishMissionDispatch.Canonical(mission))
                throw new InvalidDataException("Exact source root, pinned inputs and canonical mission required");
            var graph = JObject.Parse(new UTF8Encoding(false, true).GetString(AutonomousWorld.PinnedFile(graphPath, graphSha256, 16 * 1024 * 1024)),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if ((string)graph["authority"] != "AuthoredConfigAssets" || (bool?)graph["compiled_setup"] != false ||
                (bool?)graph["world_initialized"] != false || (bool?)graph["native_world_state_used"] != false)
                throw new InvalidDataException("Uncompiled authored source graph required");
            var pins = graph["source_pins"] as JObject;
            if (pins == null || pins.Count == 0) throw new InvalidDataException("Authored source pins required");
            string root = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (var pin in pins.Properties())
            {
                if (!Path.IsPathFullyQualified(pin.Name) || !Path.GetFullPath(pin.Name).StartsWith(root, StringComparison.Ordinal))
                    throw new InvalidDataException("Authored source pin outside the repository");
                AutonomousWorld.PinnedFile(pin.Name, (string)pin.Value, 256L * 1024 * 1024);
            }
            var matrix = SourceExternalSetupMatrix.ReadVerified(AutonomousWorld.PinnedFile(matrixPath, matrixSha256, 4 * 1024 * 1024), matrixSha256);
            var assets = new SourceConfigMaterializer(graph);
            var definition = assets.ByPath<SkirmishScenarioDefinitionConfig>((string)graph["definition_path"]);
            var catalog = assets.ByPath<SkirmishExpansionCatalogConfig>((string)graph["catalog_path"]);
            if (definition == null || definition.CatalogId != mission || catalog == null ||
                !catalog.TryGetDefinition(mission, definition.ContentVersion, out var selected) || !ReferenceEquals(selected, definition))
                throw new InvalidDataException("Exact authored catalog and definition identity required");
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
            return new SourceExpandedAuthoredSetupInput(definition, catalog, assets, matrix, manifest);
        }

        internal SkirmishResolvedSetup Compile(SkirmishDifficultyId difficulty, SkirmishSizeId size, int seed)
        {
            if (!ReferenceEquals(SkirmishExpansionCatalogConfig.Load(), Catalog))
                throw new InvalidDataException("The prepared source catalog must be bound before compilation");
            SourceExternalSetupMatrix.Select(Matrix, Definition.CatalogId, size);
            if (!SkirmishSetupCompiler.TryCompile(Definition, difficulty, size, seed, Manifest, Matrix, out var setup, out var reasons) || setup == null ||
                setup.CatalogId != Definition.CatalogId || setup.DifficultyId != difficulty || setup.SizeId != size || setup.Seed != seed ||
                setup.SetupHash != SkirmishSetupCompiler.ComputeHash(setup, seed))
                throw new InvalidDataException("Original setup compiler rejected the selected source input: " + String.Join(" | ", reasons));
            return setup;
        }

        // Resolve only explicitly named operation-map resources from this same
        // already pinned graph. Other resource types remain unsupported.
        internal IReadOnlyDictionary<string, object> MaterializeOperationMapResources(JToken specification)
            => MaterializeOperationMapResources(Assets, Definition, specification);

        // Resolves authored Grid/Surface assets through the map's own GUID and
        // content hashes. This is source materialization, not a Unity bake or
        // evidence that a physical scene has loaded.
        internal static (GridAuthoringSceneConfigAsset Grid, MapSurfaceDataAsset Surface) ResolvePhysicalInputs(
            JObject graph, SourceConfigMaterializer assets, OperationMapDefinition map,
            JToken selectedDescriptor, JToken staticMapToken)
        {
            var source = staticMapToken as JObject;
            if (graph == null || assets == null || map == null || source == null || source.Count != 4 ||
                (string)source["operation_map_id"] != map.OperationMapId ||
                !JToken.DeepEquals(source["definition"], selectedDescriptor))
                throw new InvalidDataException("Exact selected source map and physical input descriptors required");
            T Resolve<T>(string field, string guid, string digest) where T : class
            {
                var descriptor = source[field] as JObject;
                if (descriptor == null || descriptor.Count != 2 || descriptor["path"]?.Type != JTokenType.String ||
                    descriptor["sha256"]?.Type != JTokenType.String || (string)descriptor["sha256"] != digest)
                    throw new InvalidDataException("Exact map physical input descriptor required: " + field);
                string path = (string)descriptor["path"];
                AutonomousWorld.PinnedFile(path, digest, 4 * 1024 * 1024);
                var nodes = (graph["nodes"] as JArray ?? throw new InvalidDataException("Source graph nodes required"))
                    .OfType<JObject>().Where(n => (string)n["asset_guid"] == guid &&
                        (string)n["source_type"] == typeof(T).Name && (string)n["source_sha256"] == digest &&
                        path.Replace('\\', '/').EndsWith("/" + (string)n["source_path"], StringComparison.Ordinal)).ToArray();
                if ((string)graph["source_pins"]?[path] != digest || nodes.Length != 1)
                    throw new InvalidDataException("Map physical input differs from pinned authored graph: " + field);
                return assets.ByPath<T>((string)nodes[0]["source_path"]) ??
                    throw new InvalidDataException("Map physical input materialization failed: " + field);
            }
            return (Resolve<GridAuthoringSceneConfigAsset>("grid_config", map.GridMetadata.AssetGuid, map.GridMetadata.ContentHash),
                Resolve<MapSurfaceDataAsset>("surface_data", map.SurfaceMetadata.AssetGuid, map.SurfaceMetadata.ContentHash));
        }

        internal static IReadOnlyDictionary<string, object> LoadOperationMapResources(
            SourceConfigMaterializer assets, SkirmishScenarioDefinitionConfig definition, JToken descriptor)
        {
            var value = descriptor as JObject;
            if (value == null || value.Count != 2 || value["path"]?.Type != JTokenType.String ||
                value["sha256"]?.Type != JTokenType.String)
                throw new InvalidDataException("Exact pinned source resource bindings required");
            var bytes = AutonomousWorld.PinnedFile((string)value["path"], (string)value["sha256"], 1024 * 1024);
            var rows = JArray.Parse(new UTF8Encoding(false, true).GetString(bytes),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            return MaterializeOperationMapResources(assets, definition, rows);
        }

        internal static IReadOnlyDictionary<string, object> MaterializeOperationMapResources(
            SourceConfigMaterializer assets, SkirmishScenarioDefinitionConfig definition, JToken specification)
        {
            var rows = specification as JArray;
            if (assets == null || definition == null || rows == null || rows.Count > 16)
                throw new InvalidDataException("Verified source context and bounded operation-map resource list required");
            var resources = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var item in rows)
            {
                var row = item as JObject;
                if (row == null || row.Count != 2 || row["name"]?.Type != JTokenType.String || row["asset_path"]?.Type != JTokenType.String)
                    throw new InvalidDataException("Exact operation-map resource identity required");
                string name = (string)row["name"], path = (string)row["asset_path"];
                if (String.IsNullOrWhiteSpace(name) || String.IsNullOrWhiteSpace(path) || path.Contains('\\') ||
                    !path.StartsWith("Assets/Game/Resources/", StringComparison.Ordinal) || !path.EndsWith(".asset", StringComparison.Ordinal) ||
                    path.Split('/').Any(part => part == "" || part == "." || part == "..") ||
                    Path.GetFileNameWithoutExtension(path) != name || resources.ContainsKey(name))
                    throw new InvalidDataException("Canonical distinct authored operation-map resource required");
                var map = assets.ByPath<OperationMapDefinition>(path);
                if (map == null || definition.MapLayout == null || map.OperationMapId != definition.MapLayout.OperationMapId)
                    throw new InvalidDataException("Operation-map resource differs from the selected authored layout");
                resources.Add(name, map);
            }
            return resources;
        }
    }
}
