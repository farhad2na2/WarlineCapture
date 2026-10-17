using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    // Logical areas on an existing Campaign source. These are not physical map assets.
    public enum SkirmishObjectiveAreaRole : byte
    {
        None = 0, FrontlineA = 1, FrontlineB = 2, FrontlineC = 3,
        BreakthroughCorridorA = 4, BreakthroughCorridorB = 5, BreakthroughExit = 6,
        ConvoyDestination = 7
    }
    public enum SkirmishObjectiveAreaShape : byte { None = 0, AxisAlignedRectangle = 1, Polygon = 2 }
    [Serializable]
    public struct SkirmishObjectiveAreaPoint
    {
        public float WorldX, WorldZ;
        public SkirmishObjectiveAreaPoint(float x, float z) { WorldX = x; WorldZ = z; }
    }
    [Serializable]
    public struct SkirmishObjectiveAreaBinding
    {
        // Produced from the selected definition.CampaignMap and validated measured
        // layout by its compiler owner. Never derive these from name/mission number.
        public string DefinitionId, SourceConfigId, SourceAssetGuid, SourceContentHash;
        public string SourceSemanticFingerprint, LayoutId, LayoutBindingFingerprint;
        public float SourceMinX, SourceMinZ, SourceMaxX, SourceMaxZ;
    }
    [Serializable]
    public struct SkirmishObjectiveAreaEvidence
    {
        public string ArtifactId, ArtifactSha256, GeometryFingerprint;
    }
    [Serializable]
    public struct SkirmishObjectiveAreaQualificationRecord
    {
        // Stored OUTSIDE gameplay config/candidate inputs, after native capture.
        public string LogicalFingerprint;
        public SkirmishObjectiveAreaEvidence NativeSourceFit;
    }
    [Serializable]
    public struct SkirmishObjectiveArea
    {
        public string StableAreaId;
        public SkirmishObjectiveAreaRole Role;
        public SkirmishObjectiveAreaShape Shape;
        // Rectangle: exactly [minimum XZ, maximum XZ]. Polygon: simple perimeter
        // vertices in either winding, without a repeated closing point. No radius.
        public SkirmishObjectiveAreaPoint[] Points;
    }
    [Serializable]
    public sealed class SkirmishObjectiveAreaConfig
    {
        public const int CurrentVersion = 1;
        public int SchemaVersion = CurrentVersion;
        public string AreaSetId;
        public SkirmishObjectiveKind ObjectiveKind;
        public SkirmishObjectiveAreaBinding Binding;
        public SkirmishObjectiveArea[] Areas;
        // Required measured logical footprint evidence, pinned to exact geometry.
        public SkirmishObjectiveAreaEvidence Measurement;
    }

    public sealed class SkirmishCompiledObjectiveAreas
    {
        private readonly SkirmishObjectiveArea[] areas;
        public string LogicalFingerprint { get; }
        public string GeometryFingerprint { get; }
        public SkirmishObjectiveKind ObjectiveKind { get; }
        internal SkirmishCompiledObjectiveAreas(SkirmishObjectiveAreaConfig config, string geometry, string logical)
        {
            areas = config.Areas.Select(Clone).OrderBy(area => area.Role).ToArray();
            GeometryFingerprint = geometry; LogicalFingerprint = logical; ObjectiveKind = config.ObjectiveKind;
        }
        private static SkirmishObjectiveArea Clone(SkirmishObjectiveArea area)
        { area.Points = (SkirmishObjectiveAreaPoint[])area.Points.Clone(); return area; }
        public bool TryRead(SkirmishObjectiveAreaRole role, out SkirmishObjectiveArea area)
        {
            foreach (var value in areas) if (value.Role == role) { area = Clone(value); return true; }
            area = default; return false;
        }
        // Occupancy, renderer and public focus consumers share this exact perimeter.
        public bool Contains(SkirmishObjectiveAreaRole role, float worldX, float worldZ)
        {
            if (!SkirmishObjectiveAreaCompiler.Finite(worldX) || !SkirmishObjectiveAreaCompiler.Finite(worldZ)) return false;
            foreach (var area in areas) if (area.Role == role)
                return SkirmishObjectiveAreaCompiler.Contains(area, new SkirmishObjectiveAreaPoint(worldX, worldZ));
            return false;
        }
    }

    public static class SkirmishObjectiveAreaCompiler
    {
        public static bool TryResolve(IReadOnlyList<SkirmishObjectiveAreaConfig> candidates,
            in SkirmishObjectiveAreaBinding expected, SkirmishObjectiveKind kind,
            out SkirmishCompiledObjectiveAreas compiled, out string error)
        {
            compiled = null; error = "Exact logical objective area set missing.";
            if (candidates == null) return false;
            SkirmishObjectiveAreaConfig found = null;
            foreach (var config in candidates)
                if (config != null && config.Binding.DefinitionId == expected.DefinitionId && config.Binding.LayoutId == expected.LayoutId)
                {
                    if (found != null) { error = "Ambiguous logical objective area set."; return false; }
                    found = config;
                }
            return found != null && TryCompile(found, expected, kind, out compiled, out error);
        }

        public static bool TryCompile(SkirmishObjectiveAreaConfig config, in SkirmishObjectiveAreaBinding expected,
            SkirmishObjectiveKind kind, out SkirmishCompiledObjectiveAreas compiled, out string error)
        {
            compiled = null; error = "Invalid logical area identity, schema or source binding.";
            if (config == null || config.SchemaVersion != SkirmishObjectiveAreaConfig.CurrentVersion ||
                string.IsNullOrWhiteSpace(config.AreaSetId) || config.ObjectiveKind != kind || !ValidBinding(expected) ||
                !ValidBinding(config.Binding) || !SameBinding(config.Binding, expected)) return false;
            var roles = RequiredRoles(kind);
            if (roles.Length == 0 || config.Areas == null || config.Areas.Length != roles.Length)
            { error = "Exact objective-specific area roles required."; return false; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var remaining = new HashSet<SkirmishObjectiveAreaRole>(roles);
            var validated = new List<SkirmishObjectiveArea>();
            foreach (var area in config.Areas)
            {
                if (string.IsNullOrWhiteSpace(area.StableAreaId) || !ids.Add(area.StableAreaId) || !remaining.Remove(area.Role))
                { error = "Duplicate, missing or wrong objective area identity/role."; return false; }
                if (!ValidGeometry(area, expected)) { error = "Invalid, degenerate, self-intersecting or out-of-source area geometry."; return false; }
                if (validated.Any(previous => SamePerimeter(previous, area)))
                { error = "Distinct objective roles require distinct footprints, not reverse/reordered equivalents."; return false; }
                validated.Add(area);
            }
            string geometry = ComputeGeometryFingerprint(config);
            if (!Evidence(config.Measurement, geometry)) { error = "Measured footprint proof absent or bound to different geometry."; return false; }
            compiled = new SkirmishCompiledObjectiveAreas(config, geometry, Fingerprint(config, true));
            error = null; return true;
        }

        public static bool HasBoundNativeFitDeclaration(in SkirmishObjectiveAreaQualificationRecord record,
            SkirmishCompiledObjectiveAreas compiled) => compiled != null && record.LogicalFingerprint == compiled.LogicalFingerprint &&
            Evidence(record.NativeSourceFit, compiled.GeometryFingerprint);

        // Call only on authored data to prepare a measurement artifact. This is a
        // digest, NOT validation or proof that an area is walkable/source-fit.
        public static string ComputeGeometryFingerprint(SkirmishObjectiveAreaConfig config) => Fingerprint(config, false);
        private static string Fingerprint(SkirmishObjectiveAreaConfig config, bool evidence)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write("SkirmishObjectiveAreas/v1"); writer.Write(config.SchemaVersion);
                writer.Write(config.AreaSetId ?? ""); writer.Write((byte)config.ObjectiveKind); WriteBinding(writer, config.Binding);
                var sorted = (config.Areas ?? Array.Empty<SkirmishObjectiveArea>()).OrderBy(area => area.Role).ThenBy(area => area.StableAreaId, StringComparer.Ordinal).ToArray();
                writer.Write(sorted.Length);
                foreach (var area in sorted)
                {
                    writer.Write(area.StableAreaId ?? ""); writer.Write((byte)area.Role); writer.Write((byte)area.Shape);
                    var points = area.Points ?? Array.Empty<SkirmishObjectiveAreaPoint>(); writer.Write(points.Length);
                    foreach (var point in points) { writer.Write(point.WorldX == 0 ? 0f : point.WorldX); writer.Write(point.WorldZ == 0 ? 0f : point.WorldZ); }
                }
                if (evidence) WriteEvidence(writer, config.Measurement);
            }
            using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
        }
        private static void WriteEvidence(BinaryWriter writer, in SkirmishObjectiveAreaEvidence value)
        { writer.Write(value.ArtifactId ?? ""); writer.Write(value.ArtifactSha256 ?? ""); writer.Write(value.GeometryFingerprint ?? ""); }
        private static void WriteBinding(BinaryWriter writer, in SkirmishObjectiveAreaBinding value)
        {
            writer.Write(value.DefinitionId ?? ""); writer.Write(value.SourceConfigId ?? ""); writer.Write(value.SourceAssetGuid ?? "");
            writer.Write(value.SourceContentHash ?? ""); writer.Write(value.SourceSemanticFingerprint ?? "");
            writer.Write(value.LayoutId ?? ""); writer.Write(value.LayoutBindingFingerprint ?? "");
            writer.Write(value.SourceMinX); writer.Write(value.SourceMinZ); writer.Write(value.SourceMaxX); writer.Write(value.SourceMaxZ);
        }
        private static bool SameBinding(in SkirmishObjectiveAreaBinding a, in SkirmishObjectiveAreaBinding b) =>
            a.DefinitionId == b.DefinitionId && a.SourceConfigId == b.SourceConfigId && a.SourceAssetGuid == b.SourceAssetGuid &&
            a.SourceContentHash == b.SourceContentHash && a.SourceSemanticFingerprint == b.SourceSemanticFingerprint &&
            a.LayoutId == b.LayoutId && a.LayoutBindingFingerprint == b.LayoutBindingFingerprint &&
            a.SourceMinX == b.SourceMinX && a.SourceMinZ == b.SourceMinZ && a.SourceMaxX == b.SourceMaxX && a.SourceMaxZ == b.SourceMaxZ;
        private static bool ValidBinding(in SkirmishObjectiveAreaBinding value) =>
            !string.IsNullOrWhiteSpace(value.DefinitionId) && !string.IsNullOrWhiteSpace(value.SourceConfigId) &&
            Hex(value.SourceAssetGuid, 32) && !string.IsNullOrWhiteSpace(value.SourceContentHash) &&
            Hex(value.SourceSemanticFingerprint, 64) && !string.IsNullOrWhiteSpace(value.LayoutId) && Hex(value.LayoutBindingFingerprint, 64) &&
            Finite(value.SourceMinX) && Finite(value.SourceMinZ) && Finite(value.SourceMaxX) && Finite(value.SourceMaxZ) &&
            value.SourceMaxX > value.SourceMinX && value.SourceMaxZ > value.SourceMinZ;
        private static bool Hex(string value, int length) => value != null && value.Length == length &&
            value.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
        private static bool Evidence(in SkirmishObjectiveAreaEvidence value, string geometry) =>
            !string.IsNullOrWhiteSpace(value.ArtifactId) && Hex(value.ArtifactSha256, 64) && Hex(geometry, 64) && value.GeometryFingerprint == geometry;
        private static SkirmishObjectiveAreaRole[] RequiredRoles(SkirmishObjectiveKind kind)
        {
            switch (kind)
            {
                case SkirmishObjectiveKind.FrontlineControl: return new[] { SkirmishObjectiveAreaRole.FrontlineA, SkirmishObjectiveAreaRole.FrontlineB, SkirmishObjectiveAreaRole.FrontlineC };
                case SkirmishObjectiveKind.Breakthrough: return new[] { SkirmishObjectiveAreaRole.BreakthroughCorridorA, SkirmishObjectiveAreaRole.BreakthroughCorridorB, SkirmishObjectiveAreaRole.BreakthroughExit };
                case SkirmishObjectiveKind.ConvoyEscort: return new[] { SkirmishObjectiveAreaRole.ConvoyDestination };
                default: return Array.Empty<SkirmishObjectiveAreaRole>();
            }
        }
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool ValidGeometry(in SkirmishObjectiveArea area, in SkirmishObjectiveAreaBinding binding)
        {
            if (area.Points == null) return false;
            foreach (var p in area.Points)
                if (!Finite(p.WorldX) || !Finite(p.WorldZ) || p.WorldX < binding.SourceMinX || p.WorldX > binding.SourceMaxX ||
                    p.WorldZ < binding.SourceMinZ || p.WorldZ > binding.SourceMaxZ) return false;
            if (area.Shape == SkirmishObjectiveAreaShape.AxisAlignedRectangle)
                return area.Points.Length == 2 && area.Points[0].WorldX < area.Points[1].WorldX && area.Points[0].WorldZ < area.Points[1].WorldZ;
            if (area.Shape != SkirmishObjectiveAreaShape.Polygon || area.Points.Length < 3) return false;
            var points = area.Points; double twiceArea = 0;
            for (int i = 0; i < points.Length; i++)
            {
                int next = (i + 1) % points.Length;
                if (Same(points[i], points[next])) return false;
                var previous = points[(i + points.Length - 1) % points.Length];
                if (Cross(previous, points[i], points[next]) == 0 &&
                    ((double)points[i].WorldX - previous.WorldX) * ((double)points[next].WorldX - points[i].WorldX) +
                    ((double)points[i].WorldZ - previous.WorldZ) * ((double)points[next].WorldZ - points[i].WorldZ) <= 0) return false;
                twiceArea += (double)points[i].WorldX * points[next].WorldZ - (double)points[next].WorldX * points[i].WorldZ;
                for (int j = i + 1; j < points.Length; j++)
                {
                    int otherNext = (j + 1) % points.Length;
                    if (next == j || otherNext == i) continue;
                    if (Intersects(points[i], points[next], points[j], points[otherNext])) return false;
                }
            }
            return twiceArea != 0;
        }
        private static bool Same(in SkirmishObjectiveAreaPoint a, in SkirmishObjectiveAreaPoint b) => a.WorldX == b.WorldX && a.WorldZ == b.WorldZ;
        private static List<SkirmishObjectiveAreaPoint> Perimeter(in SkirmishObjectiveArea area)
        {
            var points = area.Shape == SkirmishObjectiveAreaShape.AxisAlignedRectangle ?
                new List<SkirmishObjectiveAreaPoint> { area.Points[0], new SkirmishObjectiveAreaPoint(area.Points[1].WorldX, area.Points[0].WorldZ),
                    area.Points[1], new SkirmishObjectiveAreaPoint(area.Points[0].WorldX, area.Points[1].WorldZ) } : area.Points.ToList();
            for (int i = points.Count - 1; i >= 0 && points.Count > 3; i--)
                if (Cross(points[(i + points.Count - 1) % points.Count], points[i], points[(i + 1) % points.Count]) == 0) points.RemoveAt(i);
            return points;
        }
        private static bool SamePerimeter(in SkirmishObjectiveArea a, in SkirmishObjectiveArea b)
        {
            var first = Perimeter(a); var second = Perimeter(b);
            if (first.Count != second.Count) return false;
            for (int start = 0; start < second.Count; start++) foreach (int direction in new[] { 1, -1 })
            {
                bool same = true;
                for (int i = 0; i < first.Count; i++) if (!Same(first[i], second[(start + i * direction + second.Count) % second.Count])) { same = false; break; }
                if (same) return true;
            }
            return false;
        }
        private static double Cross(in SkirmishObjectiveAreaPoint a, in SkirmishObjectiveAreaPoint b, in SkirmishObjectiveAreaPoint c) =>
            ((double)b.WorldX - a.WorldX) * ((double)c.WorldZ - a.WorldZ) - ((double)b.WorldZ - a.WorldZ) * ((double)c.WorldX - a.WorldX);
        private static bool OnSegment(in SkirmishObjectiveAreaPoint a, in SkirmishObjectiveAreaPoint b, in SkirmishObjectiveAreaPoint p) =>
            Cross(a, b, p) == 0 && p.WorldX >= Math.Min(a.WorldX, b.WorldX) && p.WorldX <= Math.Max(a.WorldX, b.WorldX) &&
            p.WorldZ >= Math.Min(a.WorldZ, b.WorldZ) && p.WorldZ <= Math.Max(a.WorldZ, b.WorldZ);
        private static bool Intersects(in SkirmishObjectiveAreaPoint a, in SkirmishObjectiveAreaPoint b, in SkirmishObjectiveAreaPoint c, in SkirmishObjectiveAreaPoint d)
        {
            double abC = Cross(a, b, c), abD = Cross(a, b, d), cdA = Cross(c, d, a), cdB = Cross(c, d, b);
            return (abC == 0 && OnSegment(a, b, c)) || (abD == 0 && OnSegment(a, b, d)) ||
                (cdA == 0 && OnSegment(c, d, a)) || (cdB == 0 && OnSegment(c, d, b)) ||
                ((abC > 0) != (abD > 0) && (cdA > 0) != (cdB > 0));
        }
        internal static bool Contains(in SkirmishObjectiveArea area, in SkirmishObjectiveAreaPoint point)
        {
            var points = area.Points;
            if (area.Shape == SkirmishObjectiveAreaShape.AxisAlignedRectangle)
                return point.WorldX >= points[0].WorldX && point.WorldX <= points[1].WorldX && point.WorldZ >= points[0].WorldZ && point.WorldZ <= points[1].WorldZ;
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                var a = points[j]; var b = points[i]; if (OnSegment(a, b, point)) return true;
                if ((a.WorldZ > point.WorldZ) != (b.WorldZ > point.WorldZ) &&
                    point.WorldX < ((double)b.WorldX - a.WorldX) * (point.WorldZ - a.WorldZ) / ((double)b.WorldZ - a.WorldZ) + a.WorldX) inside = !inside;
            }
            return inside;
        }
    }
}
