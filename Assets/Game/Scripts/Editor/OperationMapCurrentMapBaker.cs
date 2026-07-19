using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Game.Editor
{
    public static class OperationMapCurrentMapBaker
    {
        internal const string ReportPath = "/private/tmp/operation-map-current-map-bake-report.json";

        [Serializable]
        private sealed class BakeReport
        {
            public string operationMapId;
            public string scenePath;
            public string result;
            public string failure;
            public List<BakeStageReport> stages = new();
        }

        [Serializable]
        private sealed class BakeStageReport
        {
            public string name;
            public long elapsedMilliseconds;
        }

        [MenuItem("Game/Operation Maps/Bake Current Map (All)")]
        public static void Run()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            BakeReport report = new()
            {
                operationMapId = StaticMapPresentationBaker.CurrentOperationMapId,
                scenePath = StaticMapPresentationBaker.CurrentStagedOperationMapScenePath,
                result = "Failed"
            };
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene mapScene = default;
            try
            {
                mapScene = EditorSceneManager.OpenScene(report.scenePath, OpenSceneMode.Single);
                OperationMapSceneView mapView = RequireCurrentMapView(mapScene);

                RunStage(report, "building-placements", () =>
                    MapBuildingPlacementBakeEditor.BakeOperationMapBuildingPlacements(mapScene, mapView));
                RunStage(report, "vehicle-placements", () =>
                    MapVehiclePlacementBakeEditor.BakeOperationMapVehiclePlacements(mapScene, mapView));
                RunStage(report, "surface-data", () =>
                {
                    MapSurfaceAuthoringEditor.BakeActiveSceneSurfaceData();
                    RefreshSurfaceMetadata(mapView);
                });
                AssetDatabase.SaveAssets();

                EditorSceneManager.CloseScene(mapScene, removeScene: true);
                mapScene = default;

                RunStage(
                    report,
                    "presentation-chunks",
                    StaticMapPresentationBaker.BakeCurrentStagedOperationMapPresentation);
                CloseCurrentMapSceneIfLoaded();
                RunStage(
                    report,
                    "runtime-definition",
                    OperationMapAddressablesLayoutBuilder.PrepareRuntimeDefinition);
                RunStage(report, "minimap-raster", OperationMapMinimapRasterBaker.Run);
                RunStage(report, "spatial-bindings", OperationMapCurrentStagedSpatialBindingValidator.Run);
                RunStage(report, "runtime-binding-scene", OperationMapRuntimeBindingSceneBuilder.Run);
                RunStage(report, "addressables-layout", OperationMapAddressablesLayoutBuilder.Run);
                RunStage(report, "local-addressables", OperationMapAddressablesBuildReportBuilder.Run);

                report.result = "Passed";
                WriteReport(report);
                Debug.Log(
                    $"[OperationMapCurrentMapBake] result=Passed map={report.operationMapId} " +
                    $"stages={report.stages.Count} report={ReportPath}");
            }
            catch (Exception exception)
            {
                report.failure = exception.Message;
                WriteReport(report);
                Debug.LogError(
                    $"[OperationMapCurrentMapBake] result=Failed map={report.operationMapId} " +
                    $"failure={exception.Message} report={ReportPath}");
                throw;
            }
            finally
            {
                if (mapScene.IsValid() && mapScene.isLoaded)
                    EditorSceneManager.CloseScene(mapScene, removeScene: true);
                if (previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static OperationMapSceneView RequireCurrentMapView(Scene scene)
        {
            OperationMapSceneView found = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                OperationMapSceneView[] views = roots[i].GetComponentsInChildren<OperationMapSceneView>(true);
                for (int viewIndex = 0; viewIndex < views.Length; viewIndex++)
                {
                    if (found != null)
                        throw new InvalidOperationException("Operation-map scene contains multiple scene views.");
                    found = views[viewIndex];
                }
            }

            if (found == null)
                throw new InvalidOperationException("Operation-map scene view is missing.");
            if (!found.TryValidate(out string error))
                throw new InvalidOperationException(error);
            if (!string.Equals(
                    found.OperationMapId,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Operation-map scene view has an unexpected map id.");
            }

            return found;
        }

        private static void CloseCurrentMapSceneIfLoaded()
        {
            Scene loaded = SceneManager.GetSceneByPath(
                StaticMapPresentationBaker.CurrentStagedOperationMapScenePath);
            if (!loaded.IsValid() || !loaded.isLoaded)
                return;

            if (SceneManager.sceneCount == 1)
            {
                Scene empty = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive);
                SceneManager.SetActiveScene(empty);
            }

            if (!EditorSceneManager.CloseScene(loaded, removeScene: true))
            {
                throw new InvalidOperationException(
                    "Operation-map source scene could not be closed before validation.");
            }
        }

        private static void RunStage(BakeReport report, string name, Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            report.stages.Add(new BakeStageReport
            {
                name = name,
                elapsedMilliseconds = stopwatch.ElapsedMilliseconds
            });
        }

        private static void RefreshSurfaceMetadata(OperationMapSceneView mapView)
        {
            MapSurfaceDataAsset surface = mapView.MapSurfaceAuthoring.BakedSurfaceData;
            OperationMapDefinition definition = mapView.Definition;
            if (surface == null || definition == null)
                throw new InvalidOperationException("Operation-map surface metadata bindings are incomplete.");

            string surfacePath = AssetDatabase.GetAssetPath(surface);
            if (string.IsNullOrWhiteSpace(surfacePath))
                throw new InvalidOperationException("Operation-map surface asset path is missing.");

            AssetDatabase.SaveAssetIfDirty(surface);
            GetSurfaceHeightRange(surface, out float minimumHeight, out float maximumHeight);
            OperationMapSurfaceMetadataConfig metadata = new(
                AssetDatabase.AssetPathToGUID(surfacePath),
                ComputeFileHash(surfacePath),
                surface.ComputeRuntimeBlobHash().ToString(),
                surface.SurfaceCount,
                surface.PayloadVersion,
                surface.PayloadEncoding,
                minimumHeight,
                maximumHeight);
            definition.EditorSetSurfaceMetadata(metadata);
            if (!definition.TryValidateMetadata(out string error))
                throw new InvalidOperationException(error);

            OperationMapCatalogConfig catalog =
                AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(
                    OperationMapAddressablesLayoutBuilder.CatalogPath);
            if (catalog == null ||
                !catalog.TryResolve(mapView.OperationMapId, out OperationMapDefinition runtimeDefinition))
            {
                throw new InvalidOperationException(
                    "Operation-map catalog does not resolve the current map definition.");
            }

            runtimeDefinition.EditorSetSurfaceMetadata(metadata);
            if (!runtimeDefinition.TryValidateMetadata(out error) || !catalog.TryValidate(out error))
                throw new InvalidOperationException(error);
            AssetDatabase.SaveAssetIfDirty(definition);
            if (runtimeDefinition != definition)
                AssetDatabase.SaveAssetIfDirty(runtimeDefinition);
        }

        private static void GetSurfaceHeightRange(
            MapSurfaceDataAsset surface,
            out float minimumHeight,
            out float maximumHeight)
        {
            if (!surface.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> blob))
                throw new InvalidOperationException("Operation-map surface blob could not be created.");

            try
            {
                minimumHeight = float.PositiveInfinity;
                maximumHeight = float.NegativeInfinity;
                ref MapSurfaceBlob value = ref blob.Value;
                if (value.RuntimeEncoding == MapSurfaceRuntimeEncoding.SingleLayerCompact)
                {
                    for (int i = 0; i < value.CompactSamples.Length; i++)
                    {
                        float height = value.CompactMinHeight +
                                       value.CompactSamples[i].PackedHeight * value.CompactHeightStep;
                        minimumHeight = Math.Min(minimumHeight, height);
                        maximumHeight = Math.Max(maximumHeight, height);
                    }
                }
                else
                {
                    for (int i = 0; i < value.Samples.Length; i++)
                    {
                        minimumHeight = Math.Min(minimumHeight, value.Samples[i].Height);
                        maximumHeight = Math.Max(maximumHeight, value.Samples[i].Height);
                    }
                }

                if (float.IsInfinity(minimumHeight) || float.IsInfinity(maximumHeight))
                    throw new InvalidOperationException("Operation-map surface blob contains no finite heights.");
            }
            finally
            {
                blob.Dispose();
            }
        }

        private static string ComputeFileHash(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string physicalPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            using SHA256 algorithm = SHA256.Create();
            using FileStream stream = File.OpenRead(physicalPath);
            byte[] hash = algorithm.ComputeHash(stream);
            StringBuilder builder = new(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2"));
            return builder.ToString();
        }

        private static void WriteReport(BakeReport report)
        {
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        }
    }
}
