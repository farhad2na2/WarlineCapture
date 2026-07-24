using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Authoring;
using Game.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    internal readonly struct DenseCityProtectedAutobahnOccupancyStats
    {
        internal DenseCityProtectedAutobahnOccupancyStats(
            int tileCount,
            int laneCellCount,
            int connectorCellCount,
            int connectorColumnCount)
        {
            TileCount = tileCount;
            LaneCellCount = laneCellCount;
            ConnectorCellCount = connectorCellCount;
            ConnectorColumnCount = connectorColumnCount;
        }

        internal int TileCount { get; }
        internal int LaneCellCount { get; }
        internal int ConnectorCellCount { get; }
        internal int ConnectorColumnCount { get; }
    }

    public static class DenseCityProtectedAutobahnReplacementSceneValidator
    {
        private static readonly HashSet<string> ApprovedRoadPrefabGuids =
            new(StringComparer.Ordinal)
            {
                "095cd66c53a054737955d9773c3d4060",
                "fa0a16026cf90474c84e43de668567d7",
                "8a34e9514dfe04fd7a308e8dded1b154",
                "65241ad8beab543e589a7e3c7334b214",
                "b4e31794b94814524a6f32f65cdd82d4"
            };

        [MenuItem(
            "Game/Maps/Skirmish Desert Base/" +
            "Validate Protected Dense City Autobahn Replacement")]
        public static void ValidateProtectedCandidate()
        {
            if (!TryValidateProtectedCandidate(out string summary, out string error))
            {
                throw new InvalidOperationException(
                    $"Protected dense-city Autobahn replacement validation failed: {error}");
            }

            Debug.Log(
                "[DenseCityProtectedAutobahnReplacementSceneValidation] " +
                $"result=Passed {summary}");
        }

        internal static bool TryValidateProtectedCandidate(
            out string summary,
            out string error)
        {
            summary = string.Empty;
            error = string.Empty;
            string mapPath = DenseCityCandidateAuthoringTransaction.CandidateMapScenePath;
            string entityPath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath;
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                error = "Project root is unavailable.";
                return false;
            }

            string mapPhysicalPath = Path.Combine(projectRoot, mapPath);
            string entityPhysicalPath = Path.Combine(projectRoot, entityPath);
            if (!File.Exists(mapPhysicalPath) || !File.Exists(entityPhysicalPath))
            {
                error = "Protected dense-city candidate scene pair is incomplete.";
                return false;
            }

            string mapHashBefore = ComputeFileHash(mapPhysicalPath);
            string entityHashBefore = ComputeFileHash(entityPhysicalPath);
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene previousActiveScene = SceneManager.GetActiveScene();
            bool mapWasLoaded = IsSceneLoaded(mapPath);
            bool entityWasLoaded = IsSceneLoaded(entityPath);
            bool mapWasDirty = mapWasLoaded && SceneManager.GetSceneByPath(mapPath).isDirty;
            bool entityWasDirty =
                entityWasLoaded && SceneManager.GetSceneByPath(entityPath).isDirty;
            bool validated = false;
            string validationError = string.Empty;
            string validationSummary = string.Empty;

            try
            {
                Scene mapScene = OpenOrGetScene(mapPath);
                Scene entityScene = OpenOrGetScene(entityPath);
                validated = TryValidateScenes(
                    mapScene,
                    entityScene,
                    out validationSummary,
                    out validationError);
            }
            catch (Exception exception)
            {
                validationError = exception.Message;
            }
            finally
            {
                RestoreOpenedScenes(
                    previousActiveScene,
                    mapPath,
                    entityPath,
                    mapWasLoaded,
                    entityWasLoaded);

                string mapHashAfter = ComputeFileHash(mapPhysicalPath);
                string entityHashAfter = ComputeFileHash(entityPhysicalPath);
                SceneSetup[] restoredSetup = EditorSceneManager.GetSceneManagerSetup();
                bool dirtyStateChanged =
                    (mapWasLoaded &&
                     SceneManager.GetSceneByPath(mapPath).isDirty != mapWasDirty) ||
                    (entityWasLoaded &&
                     SceneManager.GetSceneByPath(entityPath).isDirty != entityWasDirty);
                if (!string.Equals(mapHashBefore, mapHashAfter, StringComparison.Ordinal) ||
                    !string.Equals(
                        entityHashBefore,
                        entityHashAfter,
                        StringComparison.Ordinal) ||
                    dirtyStateChanged ||
                    !SceneSetupsEqual(previousSetup, restoredSetup))
                {
                    validationError =
                        "Read-only validation changed a candidate scene, dirty state, " +
                        "or Editor scene setup.";
                }
            }

            if (!validated || !string.IsNullOrEmpty(validationError))
            {
                error = string.IsNullOrEmpty(validationError)
                    ? "Protected dense-city Autobahn replacement validation failed."
                    : validationError;
                return false;
            }

            summary = validationSummary;
            return true;
        }

        internal static bool TryValidateOccupancy(
            DenseCityProtectedAutobahnRouteDescriptor descriptor,
            IReadOnlyList<Vector2Int> occupiedCells,
            out DenseCityProtectedAutobahnOccupancyStats stats,
            out string error)
        {
            stats = default;
            if (!DenseCityProtectedAutobahnReplacementPlanner.TryValidate(
                    descriptor,
                    out error))
            {
                return false;
            }
            if (occupiedCells == null)
            {
                error = "Protected Autobahn replacement occupancy is required.";
                return false;
            }

            var occupancy = new HashSet<Vector2Int>();
            for (int index = 0; index < occupiedCells.Count; index++)
            {
                if (!occupancy.Add(occupiedCells[index]))
                {
                    error =
                        $"Duplicate protected Autobahn replacement cell {occupiedCells[index]}.";
                    return false;
                }
            }

            var laneCells = new HashSet<Vector2Int>(descriptor.Cells);
            foreach (Vector2Int laneCell in laneCells)
            {
                if (!occupancy.Contains(laneCell))
                {
                    string rowsAtColumn = string.Join(
                        ",",
                        occupancy
                            .Where(cell => cell.x == laneCell.x)
                            .Select(cell => cell.y)
                            .OrderBy(row => row));
                    int firstLaneCount = occupancy.Count(cell =>
                        cell.y == descriptor.LaneRanges[0].Row);
                    int secondLaneCount = occupancy.Count(cell =>
                        cell.y == descriptor.LaneRanges[1].Row);
                    error =
                        $"Protected Autobahn replacement lane is missing cell {laneCell}; " +
                        $"loaded={occupancy.Count} laneRows={firstLaneCount}/" +
                        $"{secondLaneCount} rowsAtColumn=[{rowsAtColumn}].";
                    return false;
                }
            }

            DenseCityProtectedAutobahnLaneRange first = descriptor.LaneRanges[0];
            DenseCityProtectedAutobahnLaneRange second = descriptor.LaneRanges[1];
            var connectorRowsByColumn = new Dictionary<int, HashSet<int>>();
            foreach (Vector2Int cell in occupancy)
            {
                if (cell.x < first.MinimumColumn || cell.x > first.MaximumColumn)
                {
                    error =
                        $"Protected Autobahn replacement cell {cell} exceeds the route span.";
                    return false;
                }
                if (laneCells.Contains(cell))
                    continue;

                if (!connectorRowsByColumn.TryGetValue(
                        cell.x,
                        out HashSet<int> rows))
                {
                    rows = new HashSet<int>
                    {
                        first.Row,
                        second.Row
                    };
                    connectorRowsByColumn.Add(cell.x, rows);
                }
                rows.Add(cell.y);
            }

            foreach (KeyValuePair<int, HashSet<int>> crossing in connectorRowsByColumn)
            {
                int minimumRow = crossing.Value.Min();
                int maximumRow = crossing.Value.Max();
                for (int row = minimumRow; row <= maximumRow; row++)
                {
                    if (!crossing.Value.Contains(row))
                    {
                        error =
                            $"Protected Autobahn connector column {crossing.Key} " +
                            $"has a gap at row {row}.";
                        return false;
                    }
                }
            }

            if (occupancy.Count > 0 && !IsCardinallyConnected(occupancy))
            {
                error = "Protected Autobahn replacement occupancy is not cardinally connected.";
                return false;
            }

            int connectorCellCount = occupancy.Count - laneCells.Count;
            stats = new DenseCityProtectedAutobahnOccupancyStats(
                occupancy.Count,
                laneCells.Count,
                connectorCellCount,
                connectorRowsByColumn.Count);
            error = string.Empty;
            return true;
        }

        internal static bool IsApprovedRoadPrefabGuid(string prefabGuid) =>
            !string.IsNullOrEmpty(prefabGuid) &&
            ApprovedRoadPrefabGuids.Contains(prefabGuid);

        private static bool TryValidateScenes(
            Scene mapScene,
            Scene entityScene,
            out string summary,
            out string error)
        {
            summary = string.Empty;
            RuntimeCityRAndDMapView[] views = FindInScene<RuntimeCityRAndDMapView>(mapScene);
            if (views.Length != 1)
            {
                error =
                    $"Protected dense-city map candidate requires one RuntimeCityRAndDMapView; " +
                    $"found {views.Length}.";
                return false;
            }

            OperationMapEntityPresentationIdentityAuthoring[] identities =
                FindInScene<OperationMapEntityPresentationIdentityAuthoring>(entityScene);
            string[] acceptedIds =
            {
                DenseCityProtectedAutobahnReplacementPlanner
                    .AcceptedWestSourceGlobalObjectId,
                DenseCityProtectedAutobahnReplacementPlanner
                    .AcceptedEastSourceGlobalObjectId
            };
            for (int index = 0; index < acceptedIds.Length; index++)
            {
                OperationMapEntityPresentationIdentityAuthoring[] matches = identities
                    .Where(identity => string.Equals(
                        identity.SourceGlobalObjectId,
                        acceptedIds[index],
                        StringComparison.Ordinal))
                    .ToArray();
                if (matches.Length != 1)
                {
                    error =
                        $"Protected Autobahn identity '{acceptedIds[index]}' must resolve " +
                        $"exactly once; found {matches.Length}.";
                    return false;
                }

                OperationMapEntityPresentationIdentityAuthoring anchor = matches[0];
                if (!anchor.TryValidate(out _) ||
                    anchor.Role != OperationMapEntityPresentationRole.RenderOnly ||
                    anchor.PlacementIndex !=
                    OperationMapEntityPresentationIdentityAuthoring.NoPlacementIndex ||
                    anchor.GetComponentsInChildren<Renderer>(true).Length != 0 ||
                    anchor.GetComponentsInChildren<Collider>(true).Length != 0)
                {
                    error =
                        $"Protected Autobahn identity '{acceptedIds[index]}' is not a " +
                        "renderer-free, collider-free RenderOnly anchor.";
                    return false;
                }
            }

            if (!DenseCityProtectedAutobahnReplacementPlanner.TryCreate(
                    acceptedIds,
                    DenseMiddleEasternCityEditModeBuilder.GetRoadGridOrigin(views[0]),
                    out DenseCityProtectedAutobahnRouteDescriptor descriptor,
                    out error))
            {
                return false;
            }

            DenseCityProtectedAutobahnReplacementTileMarker[] mapMarkers =
                FindInScene<DenseCityProtectedAutobahnReplacementTileMarker>(mapScene);
            DenseCityProtectedAutobahnReplacementTileMarker[] markers =
                FindInScene<DenseCityProtectedAutobahnReplacementTileMarker>(entityScene);
            if (mapMarkers.Length != 0)
            {
                error =
                    "Protected Autobahn replacement tile owners must exist only in the " +
                    "entity-presentation candidate scene.";
                return false;
            }
            if (markers.Length != 0)
            {
                error =
                    "Protected Autobahn replacement uses obsolete per-prefab tile markers.";
                return false;
            }

            DenseCityProtectedAutobahnReplacementManifestAuthoring[] manifests =
                FindInScene<DenseCityProtectedAutobahnReplacementManifestAuthoring>(
                    entityScene);
            if (manifests.Length != 1 ||
                manifests[0].GetComponentInParent<DenseCityGeneratedRootAuthoring>() == null)
            {
                error =
                    $"Protected Autobahn replacement requires one generated-root manifest; " +
                    $"found {manifests.Length}.";
                return false;
            }

            IReadOnlyList<DenseCityProtectedAutobahnReplacementManifestEntry> entries =
                manifests[0].Entries;
            var occupiedCells = new List<Vector2Int>(entries.Count);
            var usedPrefabGuids = new HashSet<string>(StringComparer.Ordinal);
            var generatedIdentityByStableId = FindInScene<DenseCityPresentationIdentityAuthoring>(
                    entityScene)
                .ToDictionary(identity => identity.StableId, StringComparer.Ordinal);
            for (int index = 0; index < entries.Count; index++)
            {
                DenseCityProtectedAutobahnReplacementManifestEntry entry = entries[index];
                if (string.IsNullOrEmpty(entry.StableId) ||
                    !generatedIdentityByStableId.TryGetValue(
                        entry.StableId,
                        out DenseCityPresentationIdentityAuthoring identity))
                {
                    error =
                        $"Replacement manifest entry {entry.Cell} does not resolve one " +
                        "generated identity.";
                    return false;
                }
                GameObject owner = identity.gameObject;
                if (PrefabUtility.GetNearestPrefabInstanceRoot(owner) != owner ||
                    owner.GetComponentInParent<DenseCityGeneratedRootAuthoring>() == null ||
                    owner.GetComponentsInChildren<Renderer>(true).Length == 0)
                {
                    error =
                        $"Replacement marker owner '{GetHierarchyPath(owner.transform)}' " +
                        "must be exactly one marker-tagged rendered prefab root under a " +
                        "dense-city generated root.";
                    return false;
                }

                string prefabPath =
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(owner);
                string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
                if (!IsApprovedRoadPrefabGuid(prefabGuid) ||
                    !string.Equals(
                        prefabGuid,
                        entry.PrefabGuid,
                        StringComparison.Ordinal))
                {
                    error =
                        $"Replacement marker owner '{GetHierarchyPath(owner.transform)}' " +
                        $"uses unapproved prefab GUID '{prefabGuid}'.";
                    return false;
                }
                usedPrefabGuids.Add(prefabGuid);

                if (HasGameplaySurfaceAuthoring(owner))
                {
                    error =
                        $"Replacement marker owner '{GetHierarchyPath(owner.transform)}' " +
                        "contains road/gameplay surface authoring.";
                    return false;
                }

                occupiedCells.Add(entry.Cell);
            }

            if (!TryValidateOccupancy(
                    descriptor,
                    occupiedCells,
                    out DenseCityProtectedAutobahnOccupancyStats stats,
                    out error))
            {
                return false;
            }
            if (stats.ConnectorColumnCount == 0)
            {
                error =
                    "Protected Autobahn replacement does not contain any connected crossing.";
                return false;
            }

            summary =
                $"anchors={acceptedIds.Length} tiles={stats.TileCount} " +
                $"laneCells={stats.LaneCellCount} connectorCells={stats.ConnectorCellCount} " +
                $"connectorColumns={stats.ConnectorColumnCount} " +
                $"prefabGuids={usedPrefabGuids.Count}";
            error = string.Empty;
            return true;
        }

        private static bool HasGameplaySurfaceAuthoring(GameObject owner) =>
            owner.GetComponentInChildren<MapSurfaceAuthoring>(true) != null ||
            owner.GetComponentInChildren<BridgeSurfaceAuthoring>(true) != null ||
            owner.GetComponentInChildren<MapBakeGroupAuthoring>(true) != null ||
            owner.GetComponentInChildren<StaticGridBlockerAuthoring>(true) != null;

        private static bool IsCardinallyConnected(HashSet<Vector2Int> occupancy)
        {
            Vector2Int first = occupancy.First();
            var visited = new HashSet<Vector2Int> { first };
            var pending = new Queue<Vector2Int>();
            pending.Enqueue(first);
            while (pending.Count > 0)
            {
                Vector2Int cell = pending.Dequeue();
                Visit(cell + Vector2Int.left);
                Visit(cell + Vector2Int.right);
                Visit(cell + Vector2Int.up);
                Visit(cell + Vector2Int.down);
            }
            return visited.Count == occupancy.Count;

            void Visit(Vector2Int neighbor)
            {
                if (occupancy.Contains(neighbor) && visited.Add(neighbor))
                    pending.Enqueue(neighbor);
            }
        }

        private static T[] FindInScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();

        private static Scene OpenOrGetScene(string path)
        {
            Scene loaded = SceneManager.GetSceneByPath(path);
            return loaded.IsValid() && loaded.isLoaded
                ? loaded
                : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private static bool IsSceneLoaded(string path)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            return scene.IsValid() && scene.isLoaded;
        }

        private static void RestoreOpenedScenes(
            Scene previousActiveScene,
            string mapPath,
            string entityPath,
            bool mapWasLoaded,
            bool entityWasLoaded)
        {
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
            CloseIfOpenedForValidation(mapPath, mapWasLoaded);
            CloseIfOpenedForValidation(entityPath, entityWasLoaded);
        }

        private static void CloseIfOpenedForValidation(string path, bool wasLoaded)
        {
            if (wasLoaded)
                return;
            Scene scene = SceneManager.GetSceneByPath(path);
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static bool SceneSetupsEqual(SceneSetup[] left, SceneSetup[] right)
        {
            if (left.Length != right.Length)
                return false;
            for (int index = 0; index < left.Length; index++)
            {
                if (!string.Equals(
                        left[index].path,
                        right[index].path,
                        StringComparison.Ordinal) ||
                    left[index].isLoaded != right[index].isLoaded ||
                    left[index].isActive != right[index].isActive)
                {
                    return false;
                }
            }
            return true;
        }

        private static string ComputeFileHash(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 sha256 = SHA256.Create();
            return BitConverter
                .ToString(sha256.ComputeHash(stream))
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }
            return string.Join("/", names);
        }
    }
}
