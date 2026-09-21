#if UNITY_EDITOR
namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Game.Authoring;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Removes the industrial (refinery) building placements that the placement bake mis-created inside the
    /// City Crossroads playable window, where they read as oil refineries standing in residential city blocks.
    ///
    /// <para>The bake applied one prefab per category folder to every child under
    /// <c>Map/Buildings/Building_Refinery/</c>, so bare plinth meshes became full refinery assemblies.
    /// Repairing the bake itself would also re-categorise Desert Base content and move campaign anchors, so
    /// this pass only deletes the retained placements inside the City Crossroads window and leaves every
    /// industrial placement outside it untouched.</para>
    ///
    /// <para><see cref="RemoveIndustrialPlacementsInWindow"/> is deliberately a single self-contained,
    /// fail-closed method taking an explicit expected count, so the future durable step inside
    /// <c>DenseCityCandidateAuthoringTransaction</c> can call exactly this code next to
    /// <c>RemoveDetachedElevatedBuildingAttachments</c> without reimplementing the predicate.</para>
    /// </summary>
    public static class CityCrossroadsIndustrialPlacementCleanup
    {
        /// <summary>Prefab GUIDs of the industrial building definitions.</summary>
        public const string RefineryDefinitionGuid = "93f73d4f2fb8e428c887ae423fbfe4f3";
        public const string RefineryBigDefinitionGuid = "3745b7c959c8348a68e93131b571bba7";

        private const string EvidenceDirectory = CityCrossroadsShelfGeometryAudit.EvidenceDirectory;

        /// <summary>City Crossroads playable window in grid cells, matching OperationMap_CityCrossroads.</summary>
        public static RectInt PlayableWindow => new(900, 260, 400, 615);

        /// <summary>True when the placement uses one of the industrial building definitions.</summary>
        public static bool IsIndustrialPlacement(OperationMapBuildingAuthoring placement)
        {
            BuildingDefinitionAuthoring definition = placement != null ? placement.Definition : null;
            if (definition == null)
                return false;

            string path = AssetDatabase.GetAssetPath(definition);
            if (string.IsNullOrEmpty(path))
                return false;

            string guid = AssetDatabase.AssetPathToGUID(path);
            return string.Equals(guid, RefineryDefinitionGuid, StringComparison.Ordinal) ||
                   string.Equals(guid, RefineryBigDefinitionGuid, StringComparison.Ordinal);
        }

        public static bool IsInsideWindow(OperationMapBuildingAuthoring placement, RectInt window)
        {
            Vector2Int cell = placement.OriginCell;
            return cell.x >= window.xMin && cell.x <= window.xMax &&
                   cell.y >= window.yMin && cell.y <= window.yMax;
        }

        /// <summary>
        /// Collects every industrial placement under <paramref name="root"/> whose origin cell falls inside
        /// <paramref name="window"/>.
        /// </summary>
        public static List<OperationMapBuildingAuthoring> CollectIndustrialPlacementsInWindow(
            Transform root,
            RectInt window)
        {
            var found = new List<OperationMapBuildingAuthoring>();
            OperationMapBuildingAuthoring[] placements =
                root.GetComponentsInChildren<OperationMapBuildingAuthoring>(true);
            for (int i = 0; i < placements.Length; i++)
            {
                if (!IsIndustrialPlacement(placements[i]) || !IsInsideWindow(placements[i], window))
                    continue;
                found.Add(placements[i]);
            }

            return found;
        }

        /// <summary>
        /// Deletes the industrial placements inside <paramref name="window"/> and returns how many were
        /// removed. Fail-closed both ways: throws when nothing matched (the predicate has drifted, or the
        /// pass ran twice against already-clean content and the caller's expectation is wrong), when the
        /// removed count differs from <paramref name="expectedRemoved"/>, and when anything remains.
        /// </summary>
        public static int RemoveIndustrialPlacementsInWindow(
            Transform root,
            RectInt window,
            int expectedRemoved)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            List<OperationMapBuildingAuthoring> targets = CollectIndustrialPlacementsInWindow(root, window);
            if (targets.Count == 0)
            {
                throw new InvalidOperationException(
                    "City Crossroads industrial cleanup found no industrial placements inside the window.");
            }

            if (expectedRemoved >= 0 && targets.Count != expectedRemoved)
            {
                throw new InvalidOperationException(
                    "City Crossroads industrial cleanup expected " +
                    $"{expectedRemoved} placements inside the window but found {targets.Count}.");
            }

            int removed = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null)
                    continue;
                UnityEngine.Object.DestroyImmediate(targets[i].gameObject);
                removed++;
            }

            int remaining = CollectIndustrialPlacementsInWindow(root, window).Count;
            if (remaining != 0)
            {
                throw new InvalidOperationException(
                    $"City Crossroads industrial cleanup left {remaining} industrial placements in the window.");
            }

            return removed;
        }

        // -----------------------------------------------------------------------------------------------
        // Editor entry points
        // -----------------------------------------------------------------------------------------------

        /// <summary>
        /// Reports the industrial-looking meshes still present in the window and what owns them, so a mesh
        /// that survives the placement-level cleanup can be attributed rather than guessed at.
        /// </summary>
        public static void SurveyIndustrialMeshes()
        {
            Scene scene = CityCrossroadsShelfGeometryAudit.OpenPresentationScene();
            RectInt window = PlayableWindow;
            string[] families =
            {
                "GasTower", "Refinery", "OilPump", "Silo", "Tank_Large", "Industrial",
                "WaterTank", "Pipeline_Tank", "Fuel"
            };

            var report = new StringBuilder();
            report.AppendLine("# City Crossroads industrial mesh survey");
            report.AppendLine();
            report.AppendLine("| name | world centre | inWindow | owning placement | definition |");
            report.AppendLine("|---|---|---|---|---|");

            int inWindow = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                MeshRenderer[] renderers = roots[r].GetComponentsInChildren<MeshRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    string name = renderers[i].name;
                    bool match = false;
                    for (int f = 0; f < families.Length; f++)
                    {
                        if (name.IndexOf(families[f], StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            match = true;
                            break;
                        }
                    }

                    if (!match)
                        continue;

                    Bounds bounds = renderers[i].bounds;
                    bool inside = bounds.center.x >= window.xMin && bounds.center.x <= window.xMax &&
                                  bounds.center.z >= window.yMin && bounds.center.z <= window.yMax;
                    if (inside)
                        inWindow++;

                    var owner = renderers[i].GetComponentInParent<OperationMapBuildingAuthoring>();
                    string ownerName = owner != null ? owner.name : "none";
                    string definitionName = owner != null && owner.Definition != null
                        ? owner.Definition.name
                        : "none";
                    report.AppendLine(
                        $"| `{name}` | ({bounds.center.x:0.0}, {bounds.center.y:0.0}, {bounds.center.z:0.0}) | " +
                        $"{inside} | `{ownerName}` | `{definitionName}` |");
                }
            }

            Directory.CreateDirectory(EvidenceDirectory);
            File.WriteAllText(Path.Combine(EvidenceDirectory, "cc-industrial-meshes.md"), report.ToString());
            Debug.Log(
                $"[CityCrossroadsIndustrialMeshSurvey] result=Completed inWindow={inWindow} " +
                $"report={Path.Combine(EvidenceDirectory, "cc-industrial-meshes.md")}");
        }

        /// <summary>Measures the in-window industrial placements without changing anything.</summary>
        public static void Survey()
        {
            Scene scene = CityCrossroadsShelfGeometryAudit.OpenPresentationScene();
            RectInt window = PlayableWindow;

            var inWindow = new List<OperationMapBuildingAuthoring>();
            var outsideWindow = new List<OperationMapBuildingAuthoring>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                OperationMapBuildingAuthoring[] placements =
                    roots[i].GetComponentsInChildren<OperationMapBuildingAuthoring>(true);
                for (int k = 0; k < placements.Length; k++)
                {
                    if (!IsIndustrialPlacement(placements[k]))
                        continue;
                    if (IsInsideWindow(placements[k], window))
                        inWindow.Add(placements[k]);
                    else
                        outsideWindow.Add(placements[k]);
                }
            }

            var report = new StringBuilder();
            report.AppendLine("# City Crossroads industrial placement survey");
            report.AppendLine();
            report.AppendLine($"- Window: x {window.xMin}..{window.xMax}, z {window.yMin}..{window.yMax}");
            report.AppendLine($"- Industrial placements inside window: {inWindow.Count}");
            report.AppendLine($"- Industrial placements outside window (left alone): {outsideWindow.Count}");
            report.AppendLine();
            report.AppendLine("| # | name | originCell | faction | blockerPolicy | definition |");
            report.AppendLine("|---:|---|---|---:|---|---|");
            for (int i = 0; i < inWindow.Count; i++)
            {
                OperationMapBuildingAuthoring placement = inWindow[i];
                string definitionName = placement.Definition != null ? placement.Definition.name : "none";
                report.AppendLine(
                    $"| {i + 1} | `{placement.name}` | ({placement.OriginCell.x},{placement.OriginCell.y}) | " +
                    $"{placement.FactionId} | {placement.BlockerPolicy} | `{definitionName}` |");
            }

            Directory.CreateDirectory(EvidenceDirectory);
            File.WriteAllText(Path.Combine(EvidenceDirectory, "cc-industrial-survey.md"), report.ToString());

            Debug.Log(
                $"[CityCrossroadsIndustrialSurvey] result=Completed inWindow={inWindow.Count} " +
                $"outsideWindow={outsideWindow.Count} " +
                $"report={Path.Combine(EvidenceDirectory, "cc-industrial-survey.md")}");
        }

        /// <summary>
        /// Applies the cleanup to the authored candidate scene and saves it. The expected count is supplied
        /// by <c>WARLINE_CC_INDUSTRIAL_EXPECTED</c> so the measured number is pinned explicitly rather than
        /// whatever happens to be present.
        /// </summary>
        public static void ApplyCleanup()
        {
            string expectedText = Environment.GetEnvironmentVariable("WARLINE_CC_INDUSTRIAL_EXPECTED");
            if (string.IsNullOrWhiteSpace(expectedText) ||
                !int.TryParse(expectedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int expected))
            {
                throw new InvalidOperationException(
                    "Set WARLINE_CC_INDUSTRIAL_EXPECTED to the measured in-window industrial placement count.");
            }

            Scene scene = CityCrossroadsShelfGeometryAudit.OpenPresentationScene();
            RectInt window = PlayableWindow;

            int removed = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                List<OperationMapBuildingAuthoring> targets =
                    CollectIndustrialPlacementsInWindow(roots[i].transform, window);
                if (targets.Count == 0)
                    continue;
                removed += RemoveIndustrialPlacementsInWindow(roots[i].transform, window, targets.Count);
            }

            if (removed != expected)
            {
                throw new InvalidOperationException(
                    $"City Crossroads industrial cleanup removed {removed} placements, expected {expected}.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Failed to save the cleaned presentation scene.");

            Debug.Log(
                $"[CityCrossroadsIndustrialCleanup] result=Applied removed={removed} expected={expected} " +
                $"remaining=0 window=[x {window.xMin}..{window.xMax}, z {window.yMin}..{window.yMax}] " +
                $"scene={CityCrossroadsShelfGeometryAudit.PresentationScenePath}");
        }
    }
}
#endif
