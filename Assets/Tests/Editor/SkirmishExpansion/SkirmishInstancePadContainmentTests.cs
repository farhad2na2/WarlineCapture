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
            Debug.Log("[SkirmishInstancePadContainmentTests] result=Passed cases=3");
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

        // -------------------------------------------------------------------------------------------------
        // executeMethod: Game.Tests.Editor.SkirmishInstancePadContainmentTests.RunAuthoredLayoutValidation
        // Env: WARLINE_SKIRMISH_CATALOG = S026 | S050 | S049 ...
        // Footprints come from the mission draft's structure_footprints (the cells the authoring used).
        // -------------------------------------------------------------------------------------------------
        public static void RunAuthoredLayoutValidation()
        {
            bool quit = Environment.GetCommandLineArgs().Contains("-quit");
            string catalog = Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG") ?? string.Empty;
            int failures = 0, checkedRows = 0;
            void Fail(string text) { failures++; Debug.LogError("[SkirmishInstancePadContainment] catalog=" + catalog + " failure " + text); }
            try
            {
                if (string.IsNullOrWhiteSpace(catalog)) { Fail("WARLINE_SKIRMISH_CATALOG is not set."); }
                else Validate(catalog, Fail, ref checkedRows);
            }
            catch (Exception error) { Fail("exception " + error); }
            string result = failures == 0 && checkedRows > 0 ? "Passed" : "Failed";
            if (failures == 0 && checkedRows == 0) failures = 1;
            Debug.Log("[SkirmishInstancePadContainment] catalog=" + catalog + " result=" + result + " failures=" + failures.ToString(CultureInfo.InvariantCulture) +
                " rows=" + checkedRows.ToString(CultureInfo.InvariantCulture) +
                " note=envelope-only-does-not-prove-scenery-clearance");
            if (quit) EditorApplication.Exit(result == "Passed" ? 0 : 2);
        }

        private static void Validate(string catalog, Action<string> fail, ref int checkedRows)
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string layoutPath = "Assets/Game/Configs/SkirmishExpansion/Scenarios/" + catalog + "/SkirmishLayout_" + catalog + ".asset";
            var layout = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(layoutPath);
            if (layout == null) { fail("missing layout asset " + layoutPath); return; }

            JObject draft = null;
            string draftDir = Path.Combine(project, "Tools", "Missions", "drafts");
            if (Directory.Exists(draftDir))
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
                if (SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout, pad, grid, bounds, rect)) continue;
                SkirmishInstancePadPlanner.EncloseWorldAlignedFootprint(f.x, f.y, a.x, a.y, w * cell, d * cell, cell, out float needW, out float needD);
                string key = row.StructureId + "|" + row.PadId;
                if (reported.Add(key))
                    fail(label + " pad=" + pad.WidthMetres.ToString("F2", CultureInfo.InvariantCulture) + "x" + pad.DepthMetres.ToString("F2", CultureInfo.InvariantCulture) +
                        " needed>=" + needW.ToString("F2", CultureInfo.InvariantCulture) + "x" + needD.ToString("F2", CultureInfo.InvariantCulture) +
                        " footprint=" + w + "x" + d + " preferredOrigin=(" + rect.x + "," + rect.y + ") (outside pad, frame, or playable bounds)");
            }
        }
    }
}
#endif
