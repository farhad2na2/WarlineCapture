#if UNITY_EDITOR
// =============================================================================
// SkirmishInstancePadPlanner: pure mapping of starting structure instances to exact
// StructureInstancePads pads for authored (generic, RequiresBinding) Skirmish missions.
//
// Rules (SkirmishStructureInstanceBinding.TryApply / SkirmishStartingGrantPlacementConstraint.TryResolvePad):
//   * one row per (size, difficulty, faction, structure, ordinal); ordinal counts earlier
//     entries of the same faction + structure id in setup.Structures order;
//   * every row owns its own pad and its own anchor within one size/difficulty;
//   * the pad kind is SkirmishMapLayoutBuilder.PadKindForStructure(structure).
// Position source, in order:
//   (a) the first unused measured instance pad (draft "instance_pads") of the same faction and
//       kind, in draft order, so a building gets the same pad in every variant;
//   (b) otherwise a derived pad centred on the position the setup compiler already gave the
//       structure (SpawnWorldX/Z = typed pad + frame offset), with a footprint the caller can
//       prove; with no provable footprint planning fails instead of guessing.
// No Unity API is used apart from enums and value types, so the rules are testable offline.
// =============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;

namespace Game.Editor
{
    public sealed class MeasuredInstancePad
    {
        public string Id;
        public SkirmishLegalPadKind Kind;
        public byte Faction;
        public float X, Z, Width, Depth;
        public string AnchorRole;
    }

    public sealed class InstancePadVariant
    {
        public SkirmishSizeId Size;
        public SkirmishDifficultyId Difficulty;
        public SkirmishResolvedStructureEntry[] Structures;
    }

    public sealed class PlannedInstancePad
    {
        public string Key;            // "m:<measured id>" or "d:<faction>/<structure>/<ordinal>"
        public bool Measured;
        public string MeasuredId;
        public byte Faction;
        public SkirmishLegalPadKind Kind;
        public float X, Z, Width, Depth;
        public string AnchorRole;     // measured: the draft anchor role; derived: null (new anchor)
        public string Slug;           // derived only: stable short id token
    }

    public struct PlannedInstanceRow
    {
        public SkirmishSizeId Size;
        public SkirmishDifficultyId Difficulty;
        public byte Faction;
        public string StructureId;
        public int Ordinal;
        public string PadKey;
    }

    public sealed class InstancePadPlan
    {
        public readonly List<PlannedInstancePad> Pads = new List<PlannedInstancePad>();
        public readonly List<PlannedInstanceRow> Rows = new List<PlannedInstanceRow>();
        public int MeasuredPads => Pads.Count(p => p.Measured);
        public int DerivedPads => Pads.Count(p => !p.Measured);
        public int UnusedMeasured;
    }

    public sealed class InstancePadPlanException : Exception
    {
        public InstancePadPlanException(string message) : base(message) { }
    }

    public static class SkirmishInstancePadPlanner
    {
        // Safety slack for float rounding at touching edges.
        public const float EnvelopeEpsilonMetres = 0.05f;

        /// <summary>
        /// A world-aligned footprint (width x depth metres) stored on a pad whose axes are the layout frame
        /// (width along forward, depth along across; forward/across are map-plane vectors as (x, z)) must be
        /// stored as its enclosing extent along those axes: |f.x|*W + |f.z|*D and |a.x|*W + |a.z|*D.
        /// The runtime snaps the preferred origin to whole cells (floor to cell, then minus footprint/2 with
        /// integer division), which moves the effective rectangle's centre by strictly less than one cell on each
        /// world axis (even footprints shift by (-1, 0] cell, odd by (-0.5, +0.5] cell). One cell is therefore
        /// added on each side of each world axis (W + 2*cell, D + 2*cell) before projecting, plus a tiny epsilon.
        /// The pad centre is unchanged.
        /// </summary>
        public static void EncloseWorldAlignedFootprint(float forwardX, float forwardZ, float acrossX, float acrossZ,
            float footprintWidth, float footprintDepth, float cellSize, out float padWidth, out float padDepth,
            float slackMetres = 0f)
        {
            float cell = cellSize > 0f ? cellSize : 1f;
            float slack = slackMetres > 0f ? slackMetres : 0f;
            float w = footprintWidth + 2f * (cell + slack) + EnvelopeEpsilonMetres;
            float d = footprintDepth + 2f * (cell + slack) + EnvelopeEpsilonMetres;
            padWidth = Math.Abs(forwardX) * w + Math.Abs(forwardZ) * d;
            padDepth = Math.Abs(acrossX) * w + Math.Abs(acrossZ) * d;
        }

        /// <summary>
        /// Relocation slack (metres per side, draft key pad_slack_m). With the one-cell snap margin alone a pad admits
        /// only a 3x3 block of grid origins (the S026/S050/S049 BlockedSpawn finding: 9 origins, all on road, water or
        /// map buildings), so the shared placement search cannot move a building off blocked ground. 12 m is about one
        /// 12 m road width (routes use 12 m): a building can step clear of a road or a map building and still stay
        /// within about one building width of the measured centre. Pads may overlap each other: the exact
        /// StructureInstancePads row picks the pad, and the shared placement still rejects overlap with any building.
        /// </summary>
        public const float DefaultPadSlackMetres = 12f;
        public const float MaxPadSlackMetres = 64f;

        /// <summary>
        /// The largest slack in [0, requested] (0.25 m steps) for which all four corners of the stored pad stay inside
        /// the playable rectangle [min, max] (map x/z). Slack never grows a pad past what the runtime would accept
        /// anyway (candidates must lie in the playable bounds). If the pad is outside even without slack, returns 0.
        /// </summary>
        public static float CapSlackToBounds(float centerX, float centerZ, float forwardX, float forwardZ, float acrossX, float acrossZ,
            float footprintWidth, float footprintDepth, float cellSize, float requestedSlack,
            float minX, float minZ, float maxX, float maxZ)
        {
            for (float slack = requestedSlack > 0f ? requestedSlack : 0f; slack > 0f; slack -= 0.25f)
                if (CornersInside(centerX, centerZ, forwardX, forwardZ, acrossX, acrossZ, footprintWidth, footprintDepth, cellSize, slack,
                        minX, minZ, maxX, maxZ)) return slack;
            return 0f;
        }

        private static bool CornersInside(float cx, float cz, float fx, float fz, float ax, float az,
            float width, float depth, float cell, float slack, float minX, float minZ, float maxX, float maxZ)
        {
            EncloseWorldAlignedFootprint(fx, fz, ax, az, width, depth, cell, out float pw, out float pd, slack);
            foreach (float sf in new[] { -.5f, .5f })
                foreach (float sa in new[] { -.5f, .5f })
                {
                    float x = cx + fx * pw * sf + ax * pd * sa, z = cz + fz * pw * sf + az * pd * sa;
                    if (x < minX || x > maxX || z < minZ || z > maxZ) return false;
                }
            return true;
        }

        public delegate bool FootprintResolver(string structureId, out float widthMetres, out float depthMetres);

        public static bool IsInstancePadKind(SkirmishLegalPadKind kind) =>
            kind == SkirmishLegalPadKind.BaseBarracks || kind == SkirmishLegalPadKind.GroundStaging ||
            kind == SkirmishLegalPadKind.AirReturn || kind == SkirmishLegalPadKind.Supply || kind == SkirmishLegalPadKind.Service;

        // Ordinals exactly as TryApply and TryResolvePad count them.
        public static int[] Ordinals(SkirmishResolvedStructureEntry[] structures)
        {
            var result = new int[structures.Length];
            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < structures.Length; i++)
            {
                string stem = structures[i].FactionId.ToString(CultureInfo.InvariantCulture) + "/" + structures[i].StructureId;
                seen.TryGetValue(stem, out int n);
                result[i] = n; seen[stem] = n + 1;
            }
            return result;
        }

        public static string DerivedSlug(byte faction, string structureId, int ordinal)
        {
            string name = structureId != null && structureId.StartsWith("building.", StringComparison.Ordinal)
                ? structureId.Substring("building.".Length) : structureId ?? string.Empty;
            return "d_" + faction.ToString(CultureInfo.InvariantCulture) + "_" + name.Replace('.', '_') + "_" +
                ordinal.ToString(CultureInfo.InvariantCulture);
        }

        public static InstancePadPlan Plan(IReadOnlyList<InstancePadVariant> variants, IReadOnlyList<MeasuredInstancePad> measured,
            FootprintResolver footprint)
        {
            if (variants == null) throw new ArgumentNullException(nameof(variants));
            measured = measured ?? Array.Empty<MeasuredInstancePad>();
            var plan = new InstancePadPlan();
            var padsByKey = new Dictionary<string, PlannedInstancePad>(StringComparer.Ordinal);
            var usedMeasured = new HashSet<string>(StringComparer.Ordinal);
            foreach (var variant in variants)
            {
                string scope = variant.Size + "/" + variant.Difficulty;
                var structures = variant.Structures ?? Array.Empty<SkirmishResolvedStructureEntry>();
                var ordinals = Ordinals(structures);
                var takenHere = new HashSet<string>(StringComparer.Ordinal);   // measured ids used in this variant
                int rowsBefore = plan.Rows.Count;
                for (int i = 0; i < structures.Length; i++)
                {
                    var structure = structures[i];
                    var kind = Game.Configs.SkirmishMapLayoutBuilder.PadKindForStructure(structure.StructureId);
                    if (!IsInstancePadKind(kind))
                        throw new InstancePadPlanException(scope + ": " + structure.StructureId + " needs a " + kind + " pad, which per-building authoring does not support.");
                    if (structure.FactionId < 1 || structure.FactionId > 2)
                        throw new InstancePadPlanException(scope + ": " + structure.StructureId + " has invalid faction " + structure.FactionId + ".");
                    PlannedInstancePad pad = null;
                    foreach (var candidate in measured)
                        if (candidate.Faction == structure.FactionId && candidate.Kind == kind && !takenHere.Contains(candidate.Id))
                        { takenHere.Add(candidate.Id); pad = MeasuredPad(candidate, padsByKey, plan); usedMeasured.Add(candidate.Id); break; }
                    if (pad == null)
                    {
                        string key = "d:" + structure.FactionId.ToString(CultureInfo.InvariantCulture) + "/" + structure.StructureId + "/" +
                            ordinals[i].ToString(CultureInfo.InvariantCulture);
                        if (padsByKey.TryGetValue(key, out pad))
                        {
                            if (Math.Abs(pad.X - structure.SpawnWorldX) > .001f || Math.Abs(pad.Z - structure.SpawnWorldZ) > .001f)
                                throw new InstancePadPlanException(scope + ": derived pad " + key + " would sit at (" + structure.SpawnWorldX + "," +
                                    structure.SpawnWorldZ + ") here but (" + pad.X + "," + pad.Z + ") in another variant.");
                        }
                        else
                        {
                            float width = 0f, depth = 0f;
                            if (footprint == null || !footprint(structure.StructureId, out width, out depth) || !(width > 0) || !(depth > 0))
                                throw new InstancePadPlanException(scope + ": " + structure.StructureId + " faction " + structure.FactionId + " ordinal " +
                                    ordinals[i] + " has no measured instance pad and its footprint is not provable here; a derived pad needs a Unity-verified " +
                                    "footprint (draft key structure_footprints) or one more measured instance_pads entry of kind " + kind + ".");
                            pad = new PlannedInstancePad
                            {
                                Key = key, Measured = false, Faction = structure.FactionId, Kind = kind, X = structure.SpawnWorldX, Z = structure.SpawnWorldZ,
                                Width = width, Depth = depth, Slug = DerivedSlug(structure.FactionId, structure.StructureId, ordinals[i])
                            };
                            padsByKey.Add(key, pad); plan.Pads.Add(pad);
                        }
                    }
                    plan.Rows.Add(new PlannedInstanceRow
                    {
                        Size = variant.Size, Difficulty = variant.Difficulty, Faction = structure.FactionId,
                        StructureId = structure.StructureId, Ordinal = ordinals[i], PadKey = pad.Key
                    });
                }
                Verify(plan, variant, rowsBefore, scope);
            }
            plan.UnusedMeasured = measured.Count(m => !usedMeasured.Contains(m.Id));
            return plan;
        }

        private static PlannedInstancePad MeasuredPad(MeasuredInstancePad source, Dictionary<string, PlannedInstancePad> byKey, InstancePadPlan plan)
        {
            string key = "m:" + source.Id;
            if (byKey.TryGetValue(key, out var existing)) return existing;
            var pad = new PlannedInstancePad
            {
                Key = key, Measured = true, MeasuredId = source.Id, Faction = source.Faction, Kind = source.Kind,
                X = source.X, Z = source.Z, Width = source.Width, Depth = source.Depth, AnchorRole = source.AnchorRole
            };
            byKey.Add(key, pad); plan.Pads.Add(pad);
            return pad;
        }

        // Count and uniqueness rules TryApply enforces per size/difficulty.
        private static void Verify(InstancePadPlan plan, InstancePadVariant variant, int rowsBefore, string scope)
        {
            int rows = plan.Rows.Count - rowsBefore, instances = variant.Structures?.Length ?? 0;
            if (rows != instances)
                throw new InstancePadPlanException(scope + ": " + rows + " rows for " + instances + " starting structure instances.");
            var pads = new HashSet<string>(StringComparer.Ordinal);
            var anchors = new HashSet<string>(StringComparer.Ordinal);
            var padsByKey = plan.Pads.ToDictionary(p => p.Key, StringComparer.Ordinal);
            for (int i = rowsBefore; i < plan.Rows.Count; i++)
            {
                var pad = padsByKey[plan.Rows[i].PadKey];
                if (!pads.Add(pad.Key)) throw new InstancePadPlanException(scope + ": pad " + pad.Key + " is used by two structure instances.");
                string anchor = pad.Measured ? "a:" + pad.AnchorRole : pad.Key;
                if (!anchors.Add(anchor)) throw new InstancePadPlanException(scope + ": anchor " + anchor + " is used by two structure instances.");
            }
        }
    }
}
#endif
