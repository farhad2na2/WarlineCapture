using System;
using System.Collections.Generic;
using System.IO;
using Game.Composition;
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
                RunStage(report, "surface-data", MapSurfaceAuthoringEditor.BakeActiveSceneSurfaceData);
                AssetDatabase.SaveAssets();

                EditorSceneManager.CloseScene(mapScene, removeScene: true);
                mapScene = default;

                RunStage(
                    report,
                    "presentation-chunks",
                    StaticMapPresentationBaker.BakeCurrentStagedOperationMapPresentation);
                CloseCurrentMapSceneIfLoaded();
                RunStage(report, "minimap-raster", OperationMapMinimapRasterBaker.Run);
                RunStage(report, "spatial-bindings", OperationMapCurrentStagedSpatialBindingValidator.Run);
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

        private static void WriteReport(BakeReport report)
        {
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        }
    }
}
