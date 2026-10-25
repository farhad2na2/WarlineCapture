using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Mathematics;
using Game.Components;
using Game.Simulation;

namespace Game.Runtime
{
    // Existing packed Campaign bounds only; this does not assert that additional
    // authored scene renderers/sidewalks have been loaded by the world initializer.
    internal sealed class SourcePackedCampaignGeometry
    {
        readonly struct Prototype
        {
            internal readonly float3 Center, Extents;
            internal Prototype(float3 center, float3 extents) { Center = center; Extents = extents; }
        }
        readonly struct Placement
        {
            internal readonly int Prototype;
            internal readonly DenseCityPresentationSemanticCategory SemanticCategory;
            internal readonly float4x4 WorldMatrix;
            internal Placement(int prototype, DenseCityPresentationSemanticCategory category, float4x4 matrix)
            { Prototype = prototype; SemanticCategory = category; WorldMatrix = matrix; }
        }
        readonly Prototype[] prototypes;
        readonly Placement[] placements;
        internal string OperationMapId { get; }
        internal string SourceAssetSha256 { get; }
        internal string GeometrySha256 { get; }
        internal int PrototypeCount => prototypes.Length;
        internal int PlacementCount => placements.Length;
        SourcePackedCampaignGeometry(string mapId, string sourceHash, string geometryHash, Prototype[] sourcePrototypes, Placement[] sourcePlacements)
        { OperationMapId = mapId; SourceAssetSha256 = sourceHash; GeometrySha256 = geometryHash; prototypes = sourcePrototypes; placements = sourcePlacements; }

        internal static string HashFile(string path, long maximum)
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length < 1 || file.Length > maximum || (file.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new NotSupportedException("Bounded regular source geometry file required");
            using var stream = file.OpenRead(); using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        static int Integer(JToken token, int minimum, int maximum)
        {
            if (token == null || token.Type != JTokenType.Integer) throw new FormatException("Source geometry integer required");
            int value = token.Value<int>();
            if (value < minimum || value > maximum) throw new FormatException("Source geometry integer bound");
            return value;
        }
        static float Scalar(JToken token)
        {
            if (token == null || (token.Type != JTokenType.Float && token.Type != JTokenType.Integer))
                throw new FormatException("Source geometry number required");
            float value = token.Value<float>();
            if (!math.isfinite(value)) throw new FormatException("Finite source geometry scalar required");
            return value;
        }
        internal static SourcePackedCampaignGeometry Load(string path, string geometryHash, string expectedSourceHash)
        {
            if (HashFile(path, 64 * 1024 * 1024) != geometryHash) throw new InvalidOperationException("Packed geometry artifact hash changed");
            var input = JObject.Parse(File.ReadAllText(path), new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (Integer(input["schema_version"], 1, 1) != 1 || (string)input["kind"] != "SourcePackedCampaignGeometry" ||
                (string)input["source_asset_sha256"] != expectedSourceHash ||
                HashFile((string)input["source_asset_path"], 64 * 1024 * 1024) != expectedSourceHash)
                throw new InvalidOperationException("Packed geometry must match its actual Campaign source asset");
            var p = input["prototypes"] as JArray ?? throw new FormatException("Source prototypes required");
            var rows = input["placements"] as JArray ?? throw new FormatException("Source placements required");
            if (p.Count < 1 || p.Count > 65536 || rows.Count < 1 || rows.Count > 262144)
                throw new NotSupportedException("Packed geometry record bound");
            var prototypes = new Prototype[p.Count];
            for (int i = 0; i < p.Count; i++)
            {
                var value = p[i] as JArray ?? throw new FormatException("Source prototype row required");
                if (value.Count != 6) throw new FormatException("Complete source bounds required");
                var center = new float3(Scalar(value[0]), Scalar(value[1]), Scalar(value[2]));
                var extents = new float3(Scalar(value[3]), Scalar(value[4]), Scalar(value[5]));
                if (math.any(extents < 0)) throw new FormatException("Negative source bounds extents");
                prototypes[i] = new Prototype(center, extents);
            }
            var placements = new Placement[rows.Count];
            var identities = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i] as JArray ?? throw new FormatException("Source placement row required");
                if (row.Count != 19) throw new FormatException("Complete source placement matrix required");
                int prototype = Integer(row[0], 0, prototypes.Length - 1);
                var category = (DenseCityPresentationSemanticCategory)Integer(row[1], 0, 8);
                string identity = (string)row[2];
                if (identity == null || identity.Length != 32 || identity.Any(c => c < '0' || c > '9' && c < 'a' || c > 'f') || !identities.Add(identity))
                    throw new FormatException("Unique complete source placement identity required");
                // Exact original BlobBuilder.ToFloat4x4 mapping from row-addressed
                // Unity Matrix4x4 fields into Mathematics column vectors.
                float4x4 matrix = new float4x4(
                    new float4(Scalar(row[3]), Scalar(row[7]), Scalar(row[11]), Scalar(row[15])),
                    new float4(Scalar(row[4]), Scalar(row[8]), Scalar(row[12]), Scalar(row[16])),
                    new float4(Scalar(row[5]), Scalar(row[9]), Scalar(row[13]), Scalar(row[17])),
                    new float4(Scalar(row[6]), Scalar(row[10]), Scalar(row[14]), Scalar(row[18])));
                placements[i] = new Placement(prototype, category, matrix);
            }
            return new SourcePackedCampaignGeometry((string)input["operation_map_id"], expectedSourceHash, geometryHash, prototypes, placements);
        }

        internal sealed class MaskResult
        {
            internal readonly bool[] Cells;
            internal readonly int ReservedPlacements, OccupiedCells;
            internal MaskResult(bool[] cells, int placements)
            { Cells = cells; ReservedPlacements = placements; OccupiedCells = cells.Count(x => x); }
            internal NativeBitArray ToNative(Allocator allocator)
            {
                var result = new NativeBitArray(Cells.Length, allocator);
                for (int i = 0; i < Cells.Length; i++) if (Cells[i]) result.Set(i, true);
                return result;
            }
        }
        internal MaskResult BuildAuthoredMask(GridConfig grid)
        {
            long count = (long)grid.Width * grid.Height;
            if (grid.Width < 1 || grid.Height < 1 || count > 4194304 || !(grid.CellSize > 0) ||
                !math.isfinite(grid.CellSize) || !math.all(math.isfinite(grid.Origin)))
                throw new NotSupportedException("Finite bounded actual gameplay grid required");
            var owner = new SourceReservation(grid, new bool[(int)count]);
            int reserved = 0;
            foreach (var placement in placements)
            {
                var bounds = prototypes[placement.Prototype];
                var matrix = placement.WorldMatrix;
                var transformed = SharedPlacementGeometry.TransformBounds(matrix, bounds.Center, bounds.Extents);
                float3 center = transformed.Center, extents = transformed.Extents;
                bool building = placement.SemanticCategory == DenseCityPresentationSemanticCategory.GameplayBuildingIntact;
                bool solidScenery = placement.SemanticCategory is DenseCityPresentationSemanticCategory.Vegetation or
                    DenseCityPresentationSemanticCategory.Prop or DenseCityPresentationSemanticCategory.BuildingAttachmentIntact;
                // Ground cover and flat decals can stay beneath a foundation; standing scenery cannot.
                if (!SharedPlacementGeometry.ShouldReservePackedBounds(building, solidScenery, extents.y)) continue;
                if (!math.all(math.isfinite(center)) || !math.all(math.isfinite(extents))) throw new NotSupportedException("Transformed source bounds overflow");
                owner.Reserve(center, extents); reserved++;
            }
            return new MaskResult(owner.Mask, reserved);
        }
        sealed class SourceReservation
        {
            readonly GridConfig grid;
            readonly bool[] mask;
            internal bool[] Mask => mask;
            internal SourceReservation(GridConfig layout, bool[] target) { grid = layout; mask = target; }
            internal void Reserve(float3 center, float3 extents)
            {
                SharedPlacementGeometry.Reserve(mask, grid.Width, grid.Height, grid.CellSize, grid.Origin, center, extents);
            }
        }
    }
}
