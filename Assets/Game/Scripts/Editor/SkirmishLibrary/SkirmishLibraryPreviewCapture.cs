#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class SkirmishLibraryPreviewCapture
    {
        private const string DesertBaseMapPath =
            "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";
        private const string CityCrossroadsMapPath =
            "Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset";
        private const string IndustrialBasinMapPath =
            "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset";

        private static readonly string[] BattlefieldOrder = { "DB", "CC", "MP", "IB", "AP" };

        public static void Run()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Skirmish library preview capture requires Edit mode.");

            string restore = SceneManager.GetActiveScene().path;
            bool restored = false;
            try
            {
                Debug.Log("[SkirmishLibraryPreviewCapture] note=EntityScenesMayOmitStaticGeometry");
                var shots = new List<Shot>();
                int previews = CollectPreviews(shots);
                int plates = CollectPlates(shots);
                RenderGrouped(shots);
                ImportSprites(shots);
                RestoreScene(restore);
                restored = true;
                Debug.Log(
                    "[SkirmishLibraryPreviewCapture] result=Passed previews=" + previews +
                    " plates=" + plates +
                    " size=" + SkirmishLibraryPreviewAssets.Width + "x" + SkirmishLibraryPreviewAssets.Height);
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishLibraryPreviewCapture] result=Failed " + exception.Message);
                throw;
            }
            finally
            {
                if (!restored)
                {
                    try
                    {
                        RestoreScene(restore);
                    }
                    catch (Exception restoreException)
                    {
                        Debug.LogError("[SkirmishLibraryPreviewCapture] restore=" + restoreException.Message);
                    }
                }
            }
        }

        private static int CollectPreviews(List<Shot> shots)
        {
            SkirmishBattleCatalogConfig catalog = SkirmishBattleCatalogConfig.Load();
            if (catalog == null)
                throw new InvalidOperationException("SkirmishBattleCatalog is missing.");

            int count = 0;
            IReadOnlyList<SkirmishBattleCatalogEntry> entries = catalog.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                SkirmishBattleCatalogEntry entry = entries[i];
                if (!entry.IsPlayable)
                    continue;
                OperationMapDefinition map = ResolveScenarioMap(entry);
                string scenePath = RequireScene(map, entry.ScenarioId);
                OperationMapCameraConfig camera = RequirePlanningCamera(map);
                string output = SkirmishLibraryPreviewAssets.PreviewPath(entry.ScenarioId);
                shots.Add(new Shot
                {
                    ScenePath = scenePath,
                    OutputAssetPath = output,
                    Camera = camera,
                    LogLine = "[SkirmishLibraryPreviewCapture] preview=" + entry.ScenarioId +
                              " scene=" + scenePath +
                              " camera=" + camera.CameraId +
                              " output=" + output
                });
                count++;
            }

            if (count == 0)
                throw new InvalidOperationException("No published skirmish scenarios to capture.");
            return count;
        }

        private static int CollectPlates(List<Shot> shots)
        {
            int count = 0;
            for (int i = 0; i < BattlefieldOrder.Length; i++)
            {
                string mapId = BattlefieldOrder[i];
                OperationMapDefinition map = ResolveBattlefield(mapId);
                if (map == null)
                {
                    if (mapId == "MP" || mapId == "AP")
                    {
                        Debug.Log("[SkirmishLibraryPreviewCapture] plate=Skipped map=" + mapId + " reason=NoOperationMap");
                        continue;
                    }

                    throw new InvalidOperationException("No operation map for battlefield " + mapId + ".");
                }

                string scenePath = RequireScene(map, mapId);
                OperationMapCameraConfig camera = RequirePlanningCamera(map);
                string output = SkirmishLibraryPreviewAssets.PlatePath(mapId);
                shots.Add(new Shot
                {
                    ScenePath = scenePath,
                    OutputAssetPath = output,
                    Camera = camera,
                    LogLine = "[SkirmishLibraryPreviewCapture] plate=" + mapId +
                              " scene=" + scenePath +
                              " camera=" + camera.CameraId +
                              " output=" + output
                });
                count++;
            }

            return count;
        }

        private static OperationMapDefinition ResolveScenarioMap(SkirmishBattleCatalogEntry entry)
        {
            SkirmishExpansionCatalogConfig catalog = SkirmishExpansionCatalogConfig.Load();
            if (catalog != null &&
                catalog.TryGetDefinition(entry.ScenarioId, out SkirmishScenarioDefinitionConfig definition) &&
                definition != null)
            {
                if (definition.CampaignMap != null)
                    return definition.CampaignMap;
                OperationMapDefinition byId = FindByOperationMapId(definition.OperationMapId, entry.MapId);
                if (byId != null)
                    return byId;
            }

            OperationMapDefinition fallback = ResolveBattlefield(entry.MapId);
            if (fallback == null)
                throw new InvalidOperationException(
                    "No operation map for published scenario " + entry.ScenarioId + " map " + entry.MapId + ".");
            return fallback;
        }

        private static OperationMapDefinition ResolveBattlefield(string mapId)
        {
            OperationMapDefinition fallback = LoadFallback(mapId);
            if (fallback != null)
                return fallback;

            string operationMapId = mapId switch
            {
                "DB" => "opmap.skirmish.desert_base_01",
                "CC" => "opmap.skirmish.city_crossroads",
                "MP" => "opmap.skirmish.mountain_pass",
                "IB" => "opmap.skirmish.industrial_basin",
                "AP" => "opmap.skirmish.airfield_plains",
                _ => null
            };
            return string.IsNullOrEmpty(operationMapId) ? null : FindByOperationMapId(operationMapId, mapId);
        }

        private static OperationMapDefinition LoadFallback(string mapId)
        {
            string path = mapId switch
            {
                "DB" => DesertBaseMapPath,
                "CC" => CityCrossroadsMapPath,
                "IB" => IndustrialBasinMapPath,
                _ => null
            };
            return string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(path);
        }

        private static OperationMapDefinition FindByOperationMapId(string operationMapId, string battlefieldId)
        {
            if (string.IsNullOrEmpty(operationMapId))
                return null;

            OperationMapDefinition preferred = LoadFallback(battlefieldId);
            if (preferred != null &&
                string.Equals(preferred.OperationMapId, operationMapId, StringComparison.Ordinal))
                return preferred;

            string[] guids = AssetDatabase.FindAssets("t:OperationMapDefinition");
            OperationMapDefinition found = null;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                OperationMapDefinition map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(path);
                if (map == null || !string.Equals(map.OperationMapId, operationMapId, StringComparison.Ordinal))
                    continue;
                if (found != null)
                {
                    Debug.Log(
                        "[SkirmishLibraryPreviewCapture] duplicate operation map id=" + operationMapId +
                        " kept=" + AssetDatabase.GetAssetPath(found) +
                        " ignored=" + path);
                    continue;
                }

                found = map;
            }

            return found;
        }

        private static string RequireScene(OperationMapDefinition map, string label)
        {
            string guid = map.SourceSceneReference != null ? map.SourceSceneReference.AssetGUID : null;
            string path = string.IsNullOrEmpty(guid) ? null : AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !File.Exists(ProjectPath(path)))
                throw new InvalidOperationException(
                    "Planning scene missing for " + label + " map " + map.OperationMapId + ".");
            return path;
        }

        private static OperationMapCameraConfig RequirePlanningCamera(OperationMapDefinition map)
        {
            string id = map.PlanningCameraId;
            ReadOnlySpan<OperationMapCameraConfig> cameras = map.Cameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (string.Equals(cameras[i].CameraId, id, StringComparison.Ordinal))
                    return cameras[i];
            }

            throw new InvalidOperationException(
                "Planning camera '" + (id ?? "<null>") + "' missing on " + map.OperationMapId + ".");
        }

        private static void RenderGrouped(List<Shot> shots)
        {
            var groups = new Dictionary<string, List<Shot>>(StringComparer.Ordinal);
            for (int i = 0; i < shots.Count; i++)
            {
                if (!groups.TryGetValue(shots[i].ScenePath, out List<Shot> group))
                {
                    group = new List<Shot>();
                    groups.Add(shots[i].ScenePath, group);
                }

                group.Add(shots[i]);
            }

            foreach (KeyValuePair<string, List<Shot>> group in groups)
                RenderScene(group.Key, group.Value);
        }

        private static void RenderScene(string scenePath, List<Shot> shots)
        {
            Scene opened = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!opened.IsValid())
                throw new InvalidOperationException("Could not open " + scenePath + ".");

            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var enabled = new bool[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                enabled[i] = canvases[i].enabled;
                canvases[i].enabled = false;
            }

            var cameraObject = new GameObject("SkirmishLibraryPreviewCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            int uiLayer = LayerMask.NameToLayer("UI");
            camera.cullingMask = uiLayer >= 0 ? ~(1 << uiLayer) : ~0;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.16f, 1f);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 8000f;
            camera.allowHDR = false;
            camera.allowMSAA = false;

            try
            {
                for (int i = 0; i < shots.Count; i++)
                    RenderShot(camera, shots[i]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                for (int i = 0; i < canvases.Length; i++)
                {
                    if (canvases[i] != null)
                        canvases[i].enabled = enabled[i];
                }
            }
        }

        private static void RenderShot(Camera camera, Shot shot)
        {
            OperationMapCameraConfig pose = shot.Camera;
            camera.orthographic = pose.Orthographic;
            camera.fieldOfView = pose.FieldOfView > 0f ? pose.FieldOfView : 60f;
            camera.orthographicSize = pose.OrthographicSize > 0f ? pose.OrthographicSize : 50f;
            camera.aspect = SkirmishLibraryPreviewAssets.Width / (float)SkirmishLibraryPreviewAssets.Height;
            camera.transform.SetPositionAndRotation(pose.Position, Quaternion.Euler(pose.EulerAngles));

            var target = new RenderTexture(
                SkirmishLibraryPreviewAssets.Width,
                SkirmishLibraryPreviewAssets.Height,
                24,
                RenderTextureFormat.ARGB32);
            Texture2D image = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                target.Create();
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image = new Texture2D(
                    SkirmishLibraryPreviewAssets.Width,
                    SkirmishLibraryPreviewAssets.Height,
                    TextureFormat.RGBA32,
                    false);
                image.ReadPixels(
                    new Rect(0f, 0f, SkirmishLibraryPreviewAssets.Width, SkirmishLibraryPreviewAssets.Height),
                    0,
                    0);
                image.Apply();
                string absolute = ProjectPath(shot.OutputAssetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? absolute);
                File.WriteAllBytes(absolute, image.EncodeToPNG());
                Debug.Log(shot.LogLine);
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                if (image != null)
                    UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void ImportSprites(List<Shot> shots)
        {
            AssetDatabase.Refresh();
            for (int i = 0; i < shots.Count; i++)
            {
                string path = shots[i].OutputAssetPath;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    throw new InvalidOperationException("Sprite importer missing for " + path + ".");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
        }

        private static void RestoreScene(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(ProjectPath(path)))
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static string ProjectPath(string assetPath)
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(root ?? string.Empty, assetPath);
        }

        private struct Shot
        {
            public string ScenePath;
            public string OutputAssetPath;
            public OperationMapCameraConfig Camera;
            public string LogLine;
        }
    }
}
#endif
