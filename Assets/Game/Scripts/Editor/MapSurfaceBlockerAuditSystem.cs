using Game.Authoring;

namespace Game.Editor
{
    #if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class MapSurfaceBlockerAuditSystem
    {
        private const string MapPrefabPath = "Assets/Game/Prefabs/Maps/Map.prefab";
        private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
        private const string ReportPath = "Design/AgentReports/map_surface_blocker_audit.md";
        private const string MatchSceneReportPath = "Design/AgentReports/map_surface_match_scene_blocker_audit.md";
        private const int MaxRows = 80;
        private static readonly Bounds TentCampAuditBounds = new(
            new Vector3(890f, 5f, 115f),
            new Vector3(110f, 40f, 90f));

        public static void AuditMapBlockers()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MapPrefabPath);
            try
            {
                MapSurfaceAuthoring authoring = prefabRoot.GetComponent<MapSurfaceAuthoring>();
                if (authoring == null)
                    authoring = prefabRoot.GetComponentInChildren<MapSurfaceAuthoring>(true);

                if (authoring == null)
                    throw new MissingReferenceException($"No MapSurfaceAuthoring found in {MapPrefabPath}.");

                List<Row> rows = CollectBlockerRows(authoring.transform);
                rows.Sort((a, b) => b.XzArea.CompareTo(a.XzArea));

                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, BuildReport(rows, MapPrefabPath));
                Debug.Log($"[MapSurfaceBlockerAudit] Wrote {ReportPath} blockerMeshes={rows.Count}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        public static void AuditMatchSceneMapBlockers()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
            try
            {
                MapSurfaceAuthoring authoring = null;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length && authoring == null; i++)
                    authoring = roots[i].GetComponentInChildren<MapSurfaceAuthoring>(true);

                if (authoring == null)
                    throw new MissingReferenceException($"No MapSurfaceAuthoring found in {MatchScenePath}.");

                List<Row> rows = CollectBlockerRows(authoring.transform);
                rows.Sort((a, b) => b.XzArea.CompareTo(a.XzArea));

                Directory.CreateDirectory(Path.GetDirectoryName(MatchSceneReportPath));
                File.WriteAllText(MatchSceneReportPath, BuildReport(rows, MatchScenePath));
                Debug.Log($"[MapSurfaceBlockerAudit] Wrote {MatchSceneReportPath} blockerMeshes={rows.Count}");
            }
            finally
            {
                if (previousScene.IsValid() &&
                    !string.IsNullOrEmpty(previousScene.path) &&
                    previousScene.path != scene.path)
                {
                    EditorSceneManager.OpenScene(previousScene.path, OpenSceneMode.Single);
                }
            }
        }

        private static List<Row> CollectBlockerRows(Transform root)
        {
            var rows = new List<Row>(512);
            MapBakeGroupAuthoring[] groups = root.GetComponentsInChildren<MapBakeGroupAuthoring>(true);
            for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
            {
                MapBakeGroupAuthoring group = groups[groupIndex];
                if (group == null || group.Role != MapBakeGroupRole.Blocker)
                    continue;

                MeshFilter[] filters = group.GetComponentsInChildren<MeshFilter>(group.IncludeInactiveChildren);
                for (int filterIndex = 0; filterIndex < filters.Length; filterIndex++)
                {
                    MeshFilter filter = filters[filterIndex];
                    if (filter == null || filter.sharedMesh == null || !IsOwnedByGroup(filter, group))
                        continue;

                    Renderer renderer = filter.GetComponent<Renderer>();
                    if (renderer == null)
                        continue;

                    Bounds bounds = renderer.bounds;
                    if (bounds.size.sqrMagnitude <= 0.0001f)
                        continue;

                    rows.Add(new Row(
                        BuildPath(filter.transform, root),
                        BuildPath(group.transform, root),
                        filter.sharedMesh.name,
                        bounds.center,
                        bounds.size,
                        bounds.size.x * bounds.size.z));
                }
            }

            return rows;
        }

        private static bool IsOwnedByGroup(Component component, MapBakeGroupAuthoring ownerGroup)
        {
            if (component == null || ownerGroup == null)
                return false;

            MapBakeGroupAuthoring nearestGroup = component.GetComponentInParent<MapBakeGroupAuthoring>(true);
            return nearestGroup == ownerGroup;
        }

        private static string BuildPath(Transform transform, Transform root)
        {
            if (transform == null)
                return string.Empty;

            var names = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Push(current.name);
                if (current == root)
                    break;

                current = current.parent;
            }

            return string.Join("/", names);
        }

        private static string BuildReport(List<Row> rows, string sourcePath)
        {
            var builder = new StringBuilder(8192);
            builder.AppendLine("# Map Surface Blocker Audit");
            builder.AppendLine();
            builder.AppendLine($"Source: `{sourcePath}`");
            builder.AppendLine($"Total blocker meshes: {rows.Count}");
            builder.AppendLine();
            builder.AppendLine("Largest blocker meshes by top-down XZ bounds area:");
            builder.AppendLine();
            builder.AppendLine("| Rank | XZ Area | Size XYZ | Center XYZ | Group | Mesh | Path |");
            builder.AppendLine("| ---: | ---: | --- | --- | --- | --- | --- |");

            int count = Math.Min(MaxRows, rows.Count);
            for (int i = 0; i < count; i++)
            {
                Row row = rows[i];
                builder.AppendLine(
                    $"| {i + 1} | {row.XzArea:0.##} | {Format(row.Size)} | {Format(row.Center)} | `{Escape(row.GroupPath)}` | `{Escape(row.MeshName)}` | `{Escape(row.Path)}` |");
            }

            builder.AppendLine();
            builder.AppendLine("Tent camp local blockers near the screenshot area:");
            builder.AppendLine();
            builder.AppendLine($"Audit bounds center/size: `{Format(TentCampAuditBounds.center)}` / `{Format(TentCampAuditBounds.size)}`");
            builder.AppendLine();
            builder.AppendLine("| XZ Area | Size XYZ | Center XYZ | Mesh | Path |");
            builder.AppendLine("| ---: | --- | --- | --- | --- |");
            int localRows = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                if (!IntersectsXZ(row.Bounds, TentCampAuditBounds))
                    continue;

                builder.AppendLine(
                    $"| {row.XzArea:0.##} | {Format(row.Size)} | {Format(row.Center)} | `{Escape(row.MeshName)}` | `{Escape(row.Path)}` |");
                localRows++;
            }

            if (localRows == 0)
                builder.AppendLine("| 0 | none | none | none | none |");

            builder.AppendLine();
            builder.AppendLine("Camp/tent/building blocker meshes:");
            builder.AppendLine();
            builder.AppendLine("| XZ Area | Size XYZ | Center XYZ | Mesh | Path |");
            builder.AppendLine("| ---: | --- | --- | --- | --- |");
            int campRows = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                if (!IsCampOrBuildingRow(row))
                    continue;

                builder.AppendLine(
                    $"| {row.XzArea:0.##} | {Format(row.Size)} | {Format(row.Center)} | `{Escape(row.MeshName)}` | `{Escape(row.Path)}` |");
                campRows++;
                if (campRows >= MaxRows)
                    break;
            }

            return builder.ToString();
        }

        private static bool IsCampOrBuildingRow(Row row)
        {
            return Contains(row.MeshName, "Tent") ||
                   Contains(row.Path, "Tent") ||
                   Contains(row.MeshName, "CamoNet") ||
                   Contains(row.Path, "CamoNet") ||
                   Contains(row.MeshName, "Hangar") ||
                   Contains(row.Path, "Hangar") ||
                   Contains(row.MeshName, "Barrack") ||
                   Contains(row.Path, "Barrack");
        }

        private static bool IntersectsXZ(Bounds a, Bounds b)
        {
            return a.min.x <= b.max.x &&
                   a.max.x >= b.min.x &&
                   a.min.z <= b.max.z &&
                   a.max.z >= b.min.z;
        }

        private static bool Contains(string value, string pattern)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Format(Vector3 value)
        {
            return $"{value.x:0.##}, {value.y:0.##}, {value.z:0.##}";
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("|", "\\|", StringComparison.Ordinal);
        }

        private readonly struct Row
        {
            public readonly string Path;
            public readonly string GroupPath;
            public readonly string MeshName;
            public readonly Vector3 Center;
            public readonly Vector3 Size;
            public readonly Bounds Bounds;
            public readonly float XzArea;

            public Row(string path, string groupPath, string meshName, Vector3 center, Vector3 size, float xzArea)
            {
                Path = path;
                GroupPath = groupPath;
                MeshName = meshName;
                Center = center;
                Size = size;
                Bounds = new Bounds(center, size);
                XzArea = xzArea;
            }
        }
    }
    #endif
}
