#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Game.Components;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Instance pads are stored in the layout's rotated frame; the building footprint is grid aligned.
    /// These tests and the executeMethod validation prove the stored pad encloses the grid rectangle at the
    /// preferred origin. They cannot prove clearance from scenery, roads, water or other buildings.
    /// </summary>
    public sealed class SkirmishInstancePadContainmentTests
    {
        // S026 frame (see codex-026 analysis): forward (0.222825,-0.974859), across (0.974859,0.222825).
        private static readonly Vector2 Forward = new Vector2(0.222825f, -0.974859f);
        private static readonly Vector2 Across = new Vector2(0.974859f, 0.222825f);

        private struct Case { public string Name; public Vector2 Center; public int W, D; }

        private static readonly Case[] Cases =
        {
            new Case { Name = "barracks", Center = new Vector2(982f, 682.5f), W = 28, D = 15 },
            new Case { Name = "staging", Center = new Vector2(998f, 710.5f), W = 14, D = 7 },
            new Case { Name = "oil pump", Center = new Vector2(984.5f, 716f), W = 15, D = 4 },
        };

        /// <summary>The runtime's preferred origin cell and the resulting effective rectangle (cell size 1, origin 0).</summary>
        public static RectInt PreferredRect(Vector2 center, int w, int d, float cellSize, Vector2 gridOrigin)
        {
            int cx = (int)Math.Floor((center.x - gridOrigin.x) / cellSize);
            int cz = (int)Math.Floor((center.y - gridOrigin.y) / cellSize);
            return new RectInt(cx - w / 2, cz - d / 2, w, d);
        }

        /// <summary>Pure containment: every grid rectangle corner lies inside the oriented pad rectangle.</summary>
        public static bool PadContainsRect(Vector2 padCenter, Vector2 forward, Vector2 across, float padWidth, float padDepth,
            RectInt rect, float cellSize, Vector2 gridOrigin)
        {
            float minX = gridOrigin.x + rect.x * cellSize, minZ = gridOrigin.y + rect.y * cellSize;
            float maxX = minX + rect.width * cellSize, maxZ = minZ + rect.height * cellSize;
            foreach (var corner in new[] { new Vector2(minX, minZ), new Vector2(maxX, minZ), new Vector2(maxX, maxZ), new Vector2(minX, maxZ) })
            {
                var offset = corner - padCenter;
                if (Mathf.Abs(Vector2.Dot(offset, forward)) > padWidth * .5f + 1e-4f ||
                    Mathf.Abs(Vector2.Dot(offset, across)) > padDepth * .5f + 1e-4f) return false;
            }
            return true;
        }

        public static void RunFocusedValidation()
        {
            var test = new SkirmishInstancePadContainmentTests();
            test.RotatedFramePadEnclosesGridRectangleAtPreferredOrigin();
            test.UnconvertedWorldAlignedSizeCannotContainRotatedRectangle();
            test.EnvelopeCoversEveryCellSnapOffset();
            test.SlackGrowsPadAroundUnchangedCentreAndAdmitsManyOrigins();
            test.SlackIsCappedToPlayableBounds();
            Debug.Log("[SkirmishInstancePadContainmentTests] result=Passed cases=5");
        }

        [Test]
        public void RotatedFramePadEnclosesGridRectangleAtPreferredOrigin()
        {
            foreach (var c in Cases)
            {
                SkirmishInstancePadPlanner.EncloseWorldAlignedFootprint(Forward.x, Forward.y, Across.x, Across.y, c.W, c.D, 1f,
                    out float padWidth, out float padDepth);
                var rect = PreferredRect(c.Center, c.W, c.D, 1f, Vector2.zero);
                Assert.IsTrue(PadContainsRect(c.Center, Forward, Across, padWidth, padDepth, rect, 1f, Vector2.zero),
                    c.Name + " pad " + padWidth + "x" + padDepth + " must contain " + rect);
                Assert.GreaterOrEqual(padDepth, Mathf.Abs(Across.x) * c.W + Mathf.Abs(Across.y) * c.D, c.Name + " depth is at least the exact envelope");
            }
        }

        [Test]
        public void UnconvertedWorldAlignedSizeCannotContainRotatedRectangle()
        {
            // Regression of the defect: the draft size stored unchanged in the rotated frame.
            foreach (var c in Cases)
            {
                var rect = PreferredRect(c.Center, c.W, c.D, 1f, Vector2.zero);
                Assert.IsFalse(PadContainsRect(c.Center, Forward, Across, c.W, c.D, rect, 1f, Vector2.zero), c.Name);
            }
        }

        [Test]
        public void EnvelopeCoversEveryCellSnapOffset()
        {
            // Sweep fractional centres and odd/even footprints, including an offset grid origin.
            var origin = new Vector2(3.37f, -2.91f);
            for (float fx = 0; fx < 1f; fx += .125f)
            for (float fz = 0; fz < 1f; fz += .125f)
            foreach (var size in new[] { new int2(28, 15), new int2(7, 4), new int2(5, 5), new int2(14, 7) })
            {
                var center = new Vector2(500f + fx, 300f + fz);
                SkirmishInstancePadPlanner.EncloseWorldAlignedFootprint(Forward.x, Forward.y, Across.x, Across.y, size.x, size.y, 1f,
                    out float padWidth, out float padDepth);
                var rect = PreferredRect(center, size.x, size.y, 1f, origin);
                Assert.IsTrue(PadContainsRect(center, Forward, Across, padWidth, padDepth, rect, 1f, origin), center + " " + size);
            }
        }

        /// <summary>Grid origins whose rectangle the exact pad contains (pure mirror of the runtime constraint's pad test).</summary>
        public static int CountAdmittedOrigins(Vector2 center, Vector2 forward, Vector2 across, float padWidth, float padDepth,
            int w, int d, float cellSize, Vector2 gridOrigin, int radiusCells)
        {
            int cx = (int)Math.Floor((center.x - gridOrigin.x) / cellSize), cz = (int)Math.Floor((center.y - gridOrigin.y) / cellSize);
            int count = 0;
            for (int ox = cx - radiusCells; ox <= cx + radiusCells; ox++)
                for (int oz = cz - radiusCells; oz <= cz + radiusCells; oz++)
                    if (PadContainsRect(center, forward, across, padWidth, padDepth, new RectInt(ox, oz, w, d), cellSize, gridOrigin)) count++;
            return count;
        }

        [Test]
        public void SlackGrowsPadAroundUnchangedCentreAndAdmitsManyOrigins()
        {
            // The BlockedSpawn finding: the one-cell snap margin alone admits only a 3x3 block of origins (9 on the
            // S026 pads); 12 m of slack per side admits hundreds, so the shared search can step off blocked ground.
            foreach (var c in Cases)
            {
                SkirmishInstancePadPlanner.EncloseWorldAlignedFootprint(Forward.x, Forward.y, Across.x, Across.y, c.W, c.D, 1f,
                    out float w0, out float d0);
                SkirmishInstancePadPlanner.EncloseWorldAlignedFootprint(Forward.x, Forward.y, Across.x, Across.y, c.W, c.D, 1f,
                    out float w12, out float d12, SkirmishInstancePadPlanner.DefaultPadSlackMetres);
                Assert.AreEqual(2f * 12f * (Mathf.Abs(Forward.x) + Mathf.Abs(Forward.y)), w12 - w0, 1e-3f, c.Name + " width grows by 2*slack on both world axes");
                Assert.AreEqual(2f * 12f * (Mathf.Abs(Across.x) + Mathf.Abs(Across.y)), d12 - d0, 1e-3f, c.Name + " depth grows by 2*slack on both world axes");
                int before = CountAdmittedOrigins(c.Center, Forward, Across, w0, d0, c.W, c.D, 1f, Vector2.zero, 60);
                int after = CountAdmittedOrigins(c.Center, Forward, Across, w12, d12, c.W, c.D, 1f, Vector2.zero, 60);
                Assert.That(before, Is.InRange(4, 16), c.Name + " without slack only a small block of origins is admitted");
                Assert.Greater(after, 400, c.Name + " with slack many origins are admitted");
                var rect = PreferredRect(c.Center, c.W, c.D, 1f, Vector2.zero);
                Assert.IsTrue(PadContainsRect(c.Center, Forward, Across, w12, d12, rect, 1f, Vector2.zero), c.Name + " preferred origin stays inside");
            }
            SkirmishInstancePadPlanner.EncloseWorldAlignedFootprint(Forward.x, Forward.y, Across.x, Across.y, 28, 15, 1f, out float wNeg, out float dNeg, -5f);
            SkirmishInstancePadPlanner.EncloseWorldAlignedFootprint(Forward.x, Forward.y, Across.x, Across.y, 28, 15, 1f, out float wZero, out float dZero);
            Assert.AreEqual(wZero, wNeg); Assert.AreEqual(dZero, dNeg);
        }

        [Test]
        public void SlackIsCappedToPlayableBounds()
        {
            var c = Cases[0];
            float full = SkirmishInstancePadPlanner.CapSlackToBounds(c.Center.x, c.Center.y, Forward.x, Forward.y, Across.x, Across.y,
                c.W, c.D, 1f, 12f, 0f, 0f, 2000f, 1000f);
            Assert.AreEqual(12f, full, "open ground keeps the requested slack");
            // Bounds that leave 6 m beyond the zero-slack pad: the cap must shrink and the capped pad must fit.
            SkirmishInstancePadPlanner.EncloseWorldAlignedFootprint(Forward.x, Forward.y, Across.x, Across.y, c.W, c.D, 1f, out float w0, out float d0);
            float halfX = (Mathf.Abs(Forward.x) * w0 + Mathf.Abs(Across.x) * d0) * .5f + 6f;
            float capped = SkirmishInstancePadPlanner.CapSlackToBounds(c.Center.x, c.Center.y, Forward.x, Forward.y, Across.x, Across.y,
                c.W, c.D, 1f, 12f, c.Center.x - halfX, 0f, c.Center.x + halfX, 2000f);
            Assert.Less(capped, 12f); Assert.GreaterOrEqual(capped, 0f);
            // Already outside without slack: no slack.
            Assert.AreEqual(0f, SkirmishInstancePadPlanner.CapSlackToBounds(c.Center.x, c.Center.y, Forward.x, Forward.y, Across.x, Across.y,
                c.W, c.D, 1f, 12f, c.Center.x + 1f, 0f, c.Center.x + 2f, 2000f));
        }

        // -------------------------------------------------------------------------------------------------
        // executeMethod: Game.Tests.Editor.SkirmishInstancePadContainmentTests.RunAuthoredLayoutValidation
        // Env: WARLINE_SKIRMISH_CATALOG = S026 | S050 | S049 ... (optional when WARLINE_MISSION_DRAFT is set)
        //      WARLINE_MISSION_DRAFT   = explicit draft path (absolute or repo-relative), e.g. the S006 example draft
        //      WARLINE_MIN_ADMITTED_ORIGINS = minimum admitted grid origins per pad (default 25; the snap margin alone gives 9)
        // Footprints come from the mission draft's structure_footprints (the cells the authoring used).
        // -------------------------------------------------------------------------------------------------
        public static void RunAuthoredLayoutValidation()
        {
            string catalog = Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG") ?? string.Empty;
            string explicitDraft = Environment.GetEnvironmentVariable("WARLINE_MISSION_DRAFT");
            bool passed = ReportAuthoredLayoutValidation(catalog, explicitDraft);
            if (Environment.GetCommandLineArgs().Contains("-quit")) EditorApplication.Exit(passed ? 0 : 2);
        }

        // Use the authoring draft list to validate the same rows in a wrapper-owned Editor.
        public static void RunAuthoredLayoutValidationAndExit()
        {
            bool passed = false;
            try
            {
                string listPath = Environment.GetEnvironmentVariable("WARLINE_MISSION_DRAFT_LIST");
                if (string.IsNullOrWhiteSpace(listPath))
                    throw new InvalidOperationException("WARLINE_MISSION_DRAFT_LIST is not set.");
                var drafts = File.ReadAllLines(listPath, Encoding.UTF8).Select(line => line.Trim())
                    .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal)).ToArray();
                if (drafts.Length == 0)
                    throw new InvalidOperationException("WARLINE_MISSION_DRAFT_LIST contains no drafts.");

                int failed = drafts.Count(draft => !ReportAuthoredLayoutValidation(string.Empty, draft));
                passed = failed == 0;
                Debug.Log($"[SkirmishInstancePadContainmentBatch] result={(passed ? "Passed" : "Failed")} drafts={drafts.Length} failures={failed}");
            }
            catch (Exception error)
            {
                Debug.LogError("[SkirmishInstancePadContainmentBatch] result=Failed " + error.Message);
            }
            MissionEditorValidationExit.Complete(passed);
        }

        private static bool ReportAuthoredLayoutValidation(string catalog, string explicitDraft)
        {
            int failures = 0, checkedRows = 0;
            void Fail(string text) { failures++; Debug.LogError("[SkirmishInstancePadContainment] catalog=" + catalog + " failure " + text); }
            try
            {
                if (string.IsNullOrWhiteSpace(catalog) && string.IsNullOrWhiteSpace(explicitDraft)) { Fail("WARLINE_SKIRMISH_CATALOG or WARLINE_MISSION_DRAFT is not set."); }
                else Validate(catalog, explicitDraft, Fail, ref checkedRows, ref catalog);
            }
            catch (Exception error) { Fail("exception " + error); }
            string result = failures == 0 && checkedRows > 0 ? "Passed" : "Failed";
            if (failures == 0 && checkedRows == 0) failures = 1;
            Debug.Log("[SkirmishInstancePadContainment] catalog=" + catalog + " result=" + result + " failures=" + failures.ToString(CultureInfo.InvariantCulture) +
                " rows=" + checkedRows.ToString(CultureInfo.InvariantCulture) +
                " note=envelope-only-does-not-prove-scenery-clearance");
            return result == "Passed";
        }

        private static void Validate(string catalogArgument, string explicitDraft, Action<string> fail, ref int checkedRows, ref string catalog)
        {
            catalog = catalogArgument;
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            JObject draft = null;
            if (!string.IsNullOrWhiteSpace(explicitDraft))
            {
                string draftPath = Path.IsPathRooted(explicitDraft) ? explicitDraft : Path.Combine(project, explicitDraft);
                if (!File.Exists(draftPath)) { fail("WARLINE_MISSION_DRAFT not found: " + draftPath); return; }
                try { draft = JObject.Parse(File.ReadAllText(draftPath, Encoding.UTF8)); }
                catch (Exception error) { fail("WARLINE_MISSION_DRAFT is not valid JSON: " + error.Message); return; }
                string draftCatalog = (string)draft["catalog_id"] ?? string.Empty;
                if (string.IsNullOrEmpty(catalog)) catalog = draftCatalog;
                else if (!string.Equals(catalog, draftCatalog, StringComparison.OrdinalIgnoreCase))
                { fail("WARLINE_SKIRMISH_CATALOG " + catalog + " differs from the draft catalog_id " + draftCatalog); return; }
            }
            string layoutPath = "Assets/Game/Configs/SkirmishExpansion/Scenarios/" + catalog + "/SkirmishLayout_" + catalog + ".asset";
            var layout = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(layoutPath);
            if (layout == null) { fail("missing layout asset " + layoutPath); return; }

            string draftDir = Path.Combine(project, "Tools", "Missions", "drafts");
            if (draft == null && Directory.Exists(draftDir))
                foreach (string file in Directory.GetFiles(draftDir, "*.json"))
                {
                    JObject candidate;
                    try { candidate = JObject.Parse(File.ReadAllText(file, Encoding.UTF8)); } catch { continue; }
                    if (string.Equals((string)candidate["catalog_id"], catalog, StringComparison.OrdinalIgnoreCase)) { draft = candidate; break; }
                }
            if (draft == null) { fail("no draft with catalog_id " + catalog + " under " + draftDir); return; }
            var footprints = draft["structure_footprints"] as JObject;
            if (footprints == null) { fail("draft has no structure_footprints"); return; }
            string sourcePath = (string)draft["campaign_source"];
            var source = string.IsNullOrEmpty(sourcePath) ? null : AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(sourcePath);
            if (source == null) { fail("missing campaign source " + sourcePath); return; }

            var meta = source.GridMetadata;
            var grid = new GridConfig { Width = meta.Dimensions.x, Height = meta.Dimensions.y, CellSize = meta.CellSize, Origin = new float3(meta.Origin.x, meta.Origin.y, meta.Origin.z) };
            var bounds = source.Bounds;
            var f = new Vector2(layout.WorldBinding.ForwardX, layout.WorldBinding.ForwardZ);
            var a = new Vector2(layout.WorldBinding.AcrossX, layout.WorldBinding.AcrossZ);
            var reported = new HashSet<string>(StringComparer.Ordinal);
            var admittedByPad = new Dictionary<string, int>(StringComparer.Ordinal);
            int minAdmitted = int.TryParse(Environment.GetEnvironmentVariable("WARLINE_MIN_ADMITTED_ORIGINS"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedMin)
                ? parsedMin : 25;

            foreach (var row in layout.StructureInstancePads)
            {
                checkedRows++;
                string label = row.SizeId + "/" + row.DifficultyId + " f" + row.FactionId + " " + row.StructureId + "#" + row.InstanceOrdinal + " pad=" + row.PadId;
                var pad = layout.Pads.FirstOrDefault(p => p.PadId == row.PadId);
                if (pad.PadId == null) { fail(label + " pad not found"); continue; }
                var entry = footprints[row.StructureId] as JArray;
                if (entry == null || entry.Count != 2) { fail(label + " no structure_footprints entry"); continue; }
                float cell = grid.CellSize;
                int w = Mathf.CeilToInt((float)entry[0] / cell - 1e-3f), d = Mathf.CeilToInt((float)entry[1] / cell - 1e-3f);
                if (!layout.TryProjectLocalToMap(pad.CenterX, pad.CenterZ, out float cx, out float cz)) { fail(label + " pad centre does not project"); continue; }
                // Same formula as SkirmishStartingBuildingsService: floor cell of the world centre minus footprint/2 (integer).
                int cellX = (int)Math.Floor((cx - grid.Origin.x) / cell), cellZ = (int)Math.Floor((cz - grid.Origin.z) / cell);
                var rect = new RectInt(cellX - w / 2, cellZ - d / 2, w, d);
                if (admittedByPad.TryGetValue(row.PadId + "|" + w + "x" + d, out int knownAdmitted) == false)
                {
                    knownAdmitted = CountAdmitted(layout, pad, grid, bounds, w, d);
                    admittedByPad[row.PadId + "|" + w + "x" + d] = knownAdmitted;
                    Debug.Log("[SkirmishInstancePadContainment] catalog=" + catalog + " pad=" + row.PadId + " structure=" + row.StructureId +
                        " footprint=" + w + "x" + d + " pad=" + pad.WidthMetres.ToString("F2", CultureInfo.InvariantCulture) + "x" +
                        pad.DepthMetres.ToString("F2", CultureInfo.InvariantCulture) + " admittedOrigins=" + knownAdmitted +
                        " preferredInside=" + (SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout, pad, grid, bounds, rect) ? 1 : 0));
                    if (knownAdmitted < minAdmitted)
                        fail(label + " admits only " + knownAdmitted + " origins (< " + minAdmitted + "): the shared search cannot relocate this building (pad_slack_m too small, or pad at the playable edge)");
                }
                if (SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout, pad, grid, bounds, rect)) continue;
                SkirmishInstancePadPlanner.EncloseWorldAlignedFootprint(f.x, f.y, a.x, a.y, w * cell, d * cell, cell, out float needW, out float needD);
                string key = row.StructureId + "|" + row.PadId;
                if (reported.Add(key))
                    fail(label + " pad=" + pad.WidthMetres.ToString("F2", CultureInfo.InvariantCulture) + "x" + pad.DepthMetres.ToString("F2", CultureInfo.InvariantCulture) +
                        " needed>=" + needW.ToString("F2", CultureInfo.InvariantCulture) + "x" + needD.ToString("F2", CultureInfo.InvariantCulture) +
                        " footprint=" + w + "x" + d + " preferredOrigin=(" + rect.x + "," + rect.y + ") (outside pad, frame, or playable bounds)");
            }
        }

        // Grid origins (lower-left cell of a w x d cell rectangle) the typed-pad constraint admits: the same
        // ContainsFootprint the runtime applies, scanned over the pad's bounding box in cells.
        private static int CountAdmitted(SkirmishMapLayoutConfig layout, SkirmishLegalPadConfig pad, GridConfig grid, OperationMapBoundsConfig bounds, int w, int d)
        {
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            foreach (float sx in new[] { -.5f, .5f })
                foreach (float sz in new[] { -.5f, .5f })
                {
                    if (!layout.TryProjectLocalToMap(pad.CenterX + sx * pad.WidthMetres, pad.CenterZ + sz * pad.DepthMetres, out float x, out float z)) return 0;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z);
                }
            int x0 = Mathf.FloorToInt((minX - grid.Origin.x) / grid.CellSize) - 1, x1 = Mathf.CeilToInt((maxX - grid.Origin.x) / grid.CellSize) + 1;
            int z0 = Mathf.FloorToInt((minZ - grid.Origin.z) / grid.CellSize) - 1, z1 = Mathf.CeilToInt((maxZ - grid.Origin.z) / grid.CellSize) + 1;
            int count = 0;
            for (int ox = x0; ox <= x1; ox++)
                for (int oz = z0; oz <= z1; oz++)
                    if (SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout, pad, grid, bounds, new RectInt(ox, oz, w, d))) count++;
            return count;
        }
    }
}
#endif
