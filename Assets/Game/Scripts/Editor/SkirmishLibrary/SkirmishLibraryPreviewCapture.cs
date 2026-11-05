#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Skirmish.Contracts;
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
        private const string MountainPassPlayableMapPath =
            "Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Definition.asset";
        private const string AirfieldPlainsPlayableMapPath =
            "Assets/Game/GeneratedOperationMaps/CH04M04GroundedSignal/Definition.asset";
        private const string MatchVolumeProfilePath =
            "Assets/Game/Art/MapBeautify/CampaignVisualDetails/Refinement/Lighting/Military_Demo_Gameplay.asset";

        // Match.unity "Directional Light". Desert Base's runtime scene has no sun, so a
        // match keeps this light while SkirmishS004CampaignLighting supplies the fill.
        private static readonly Quaternion MatchSunRotation =
            new Quaternion(-0.048195288f, 0.74310285f, -0.6193119f, 0.24885356f);
        private static readonly Color MatchSunColor = new Color(1f, 0.977f, 0.935f, 1f);
        private const float MatchSunIntensity = 1.25f;

        private const int CardWidth = 1024;
        private const int CardHeight = 576;
        private const int PlateWidth = 1920;
        private const int PlateHeight = 1080;
        private const int Supersample = 2;
        private const int MaxImportSize = 2048;
        private static readonly string[] RefineryKeywords =
            { "refinery", "industrial", "silo", "storagetank", "storage_tank", "fueltank", "fuel_tank", "chimney", "smokestack", "pipeline", "factory", "cracking" };

        private const string DenseSectorKey = "__dense_sector__";

        private static readonly string[] BattlefieldOrder = { "DB", "CC", "MP", "IB", "AP" };

        public static void RunCards()
        {
            bool passed=false;
            try { Run(false);passed=true; }
            catch(Exception error) { Debug.LogException(error); }
            finally { MissionEditorValidationExit.Complete(passed); }
        }

        public static void Run()=>Run(true);
        // A screenshot of the existing Campaign geometry at the real O001 objective.
        // No physical map, terrain, road or gameplay configuration is created or changed.
        public static void CaptureOperationsHomePreview()
        {
            var definition=Resources.Load<OperationsReconMissionConfig>(OperationsReconMissionConfig.ResourcePath);
            if(definition==null)throw new InvalidOperationException("Missing published Operations definition.");
            string restore=SceneManager.GetActiveScene().path;
            try
            {
                string path=MainMenuModeCardsAuthoring.OperationsPreviewPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var shots=new List<Shot>{new Shot{
                    ScenePath=RequireScene(definition.operationMap,"O001"),
                    OutputAssetPath=path,
                    Camera=FrameCard("O001",definition.exitPosition,definition.scanPositions[1],true),
                    Width=CardWidth,Height=CardHeight,CardSubject=definition.scanPositions[1],RefineCard=true,
                    LogLine="[MainMenuMissionPreview] id=operation.o001 source=published-definition actualGeometry=true"
                }};
                RenderGrouped(shots);ImportSprites(shots);
            }
            finally{RestoreScene(restore);}
        }


        private static void Run(bool includePlates)
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
                int plates = includePlates ? CollectPlates(shots) : 0;
                RenderGrouped(shots);
                ImportSprites(shots);
                RestoreScene(restore);
                restored = true;
                Debug.Log(
                    "[SkirmishLibraryPreviewCapture] result=Passed previews=" + previews +
                    " plates=" + plates +
                    " card=" + CardWidth + "x" + CardHeight +
                    " plate=" + PlateWidth + "x" + PlateHeight +
                    " supersample=" + Supersample + "x msaa=8");
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

            SkirmishExpansionCatalogConfig expansion = SkirmishExpansionCatalogConfig.Load();
            int count = 0;
            IReadOnlyList<SkirmishBattleCatalogEntry> entries = catalog.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                SkirmishBattleCatalogEntry entry = entries[i];
                if (!entry.IsPlayable)
                    continue;
                SkirmishScenarioDefinitionConfig definition = null;
                if (expansion != null)
                    expansion.TryGetDefinition(entry.ScenarioId, out definition);
                OperationMapDefinition map = ResolveScenarioMap(entry);
                string scenePath = RequireScene(map, entry.ScenarioId);
                if (!TryCardFocus(entry, map, definition, out Vector3 player, out Vector3 enemy, out bool established))
                    throw new InvalidOperationException(
                        "No card focus for published scenario " + entry.ScenarioId + ".");
                OperationMapCameraConfig camera = FrameCard(entry.ScenarioId, player, enemy, established);
                RejectPlateDuplicate(map, camera, entry.ScenarioId);
                string output = SkirmishLibraryPreviewAssets.PreviewPath(entry.ScenarioId);
                shots.Add(new Shot
                {
                    ScenePath = scenePath,
                    OutputAssetPath = output,
                    Camera = camera,
                    Width = CardWidth,
                    Height = CardHeight,
                    CardSubject = enemy,
                    RefineCard = true,
                    LogLine = "[SkirmishLibraryPreviewCapture] preview=" + entry.ScenarioId +
                              " framing=card" +
                              " established=" + (established ? "1" : "0") +
                              " player=" + Format(player) +
                              " enemy=" + Format(enemy) +
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
                        if (TryPlayablePlate(mapId, shots))
                        {
                            count++;
                            continue;
                        }

                        Debug.Log(
                            "[SkirmishLibraryPreviewCapture] plate=Placeholder map=" + mapId +
                            " reason=NoPlayableMap");
                        continue;
                    }

                    throw new InvalidOperationException("No operation map for battlefield " + mapId + ".");
                }

                string scenePath = RequireScene(map, mapId);
                OperationMapCameraConfig camera = RequirePlanningCamera(map);
                string output = SkirmishLibraryPreviewAssets.PlatePath(mapId);
                bool refinery = mapId == "IB";
                bool dense = mapId == "CC";
                Vector3 hint = camera.Position;
                if ((refinery || dense) && TryMapFocus(map, false, out Vector3 ibPlayer, out Vector3 ibEnemy))
                    hint = (ibPlayer + ibEnemy) * 0.5f;
                shots.Add(new Shot
                {
                    ScenePath = scenePath,
                    OutputAssetPath = output,
                    Camera = camera,
                    Width = PlateWidth,
                    Height = PlateHeight,
                    FocusKeywords = refinery ? RefineryKeywords : dense ? new[] { DenseSectorKey } : null,
                    FocusHint = hint,
                    LogLine = "[SkirmishLibraryPreviewCapture] plate=" + mapId +
                              " framing=plate" +
                              " scene=" + scenePath +
                              " camera=" + camera.CameraId +
                              " output=" + output
                });
                count++;
            }

            return count;
        }

        private static bool TryPlayablePlate(string mapId, List<Shot> shots)
        {
            string assetPath = mapId == "MP" ? MountainPassPlayableMapPath : AirfieldPlainsPlayableMapPath;
            OperationMapDefinition map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(assetPath);
            string scenePath = map != null ? ScenePathOrNull(map) : null;
            if (map == null || string.IsNullOrEmpty(scenePath))
                return false;

            bool overview = TryOverviewCamera(map, out OperationMapCameraConfig camera);
            if (!overview)
            {
                PlateBounds(map, out Vector3 min, out Vector3 max);
                camera = FrameTopDown(mapId, min, max);
            }

            string output = SkirmishLibraryPreviewAssets.PlatePath(mapId);
            shots.Add(new Shot
            {
                ScenePath = scenePath,
                OutputAssetPath = output,
                Camera = camera,
                Width = PlateWidth,
                Height = PlateHeight,
                FocusKeywords = Array.Empty<string>(),
                GradeContrast = mapId == "MP" ? 1.14f : 1f,
                GradeExposure = mapId == "MP" ? 0.93f : 1f,
                GradeSaturation = mapId == "MP" ? 1.12f : 1f,
                FocusHint = camera.Position,
                LogLine = "[SkirmishLibraryPreviewCapture] plate=" + mapId +
                          " framing=" + (overview ? "overview" : "top-down") +
                          " source=" + map.OperationMapId +
                          " scene=" + scenePath +
                          " camera=" + camera.CameraId +
                          " hud=off fog=off output=" + output
            });
            return true;
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
            string path = ScenePathOrNull(map);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException(
                    "Planning scene missing for " + label + " map " + map.OperationMapId + ".");
            return path;
        }

        private static string ScenePathOrNull(OperationMapDefinition map)
        {
            string guid = map.SourceSceneReference != null ? map.SourceSceneReference.AssetGUID : null;
            string path = string.IsNullOrEmpty(guid) ? null : AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !File.Exists(ProjectPath(path)))
                return null;
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

        private static bool TryOverviewCamera(OperationMapDefinition map, out OperationMapCameraConfig camera)
        {
            ReadOnlySpan<OperationMapCameraConfig> cameras = map.Cameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                string id = cameras[i].CameraId ?? string.Empty;
                if (id.IndexOf("overview", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    camera = cameras[i];
                    return true;
                }
            }

            camera = default;
            return false;
        }

        private static bool TryCardFocus(
            SkirmishBattleCatalogEntry entry,
            OperationMapDefinition map,
            SkirmishScenarioDefinitionConfig definition,
            out Vector3 player,
            out Vector3 enemy,
            out bool established)
        {
            player = default;
            enemy = default;
            bool air = IsAir(entry, definition);
            established = IsEstablished(entry, definition);
            if (definition != null &&
                definition.MapLayout != null &&
                TryLayoutFocus(definition, map, air, established, out player, out enemy))
                return true;
            return TryMapFocus(map, air, out player, out enemy);
        }

        private static bool TryLayoutFocus(
            SkirmishScenarioDefinitionConfig definition,
            OperationMapDefinition map,
            bool air,
            bool established,
            out Vector3 player,
            out Vector3 enemy)
        {
            player = default;
            enemy = default;
            SkirmishMapLayoutConfig layout = definition.MapLayout;
            string playerRole = null;
            if (air && layout.TryGetAnchor("air.player", out _))
                playerRole = "air.player";
            else if (established && layout.TryGetAnchor(SkirmishObjectiveIds.BasePlayer, out _))
                playerRole = SkirmishObjectiveIds.BasePlayer;
            else if (layout.TryGetAnchor("staging.player", out _))
                playerRole = "staging.player";
            else if (layout.TryGetAnchor(SkirmishObjectiveIds.BasePlayer, out _))
                playerRole = SkirmishObjectiveIds.BasePlayer;
            if (playerRole == null || !layout.TryGetAnchor(playerRole, out SkirmishLayoutAnchorConfig playerAnchor))
                return false;

            string enemyRole = definition.ObjectiveConfig != null &&
                               !string.IsNullOrEmpty(definition.ObjectiveConfig.EnemyBaseRoleId)
                ? definition.ObjectiveConfig.EnemyBaseRoleId
                : SkirmishObjectiveIds.BaseEnemy;
            if (!layout.TryGetAnchor(enemyRole, out SkirmishLayoutAnchorConfig enemyAnchor) &&
                !layout.TryGetAnchor(SkirmishObjectiveIds.BaseEnemy, out enemyAnchor))
                return false;

            player = new Vector3(playerAnchor.WorldX, AnchorHeight(map, 1), playerAnchor.WorldZ);
            enemy = new Vector3(enemyAnchor.WorldX, AnchorHeight(map, 2), enemyAnchor.WorldZ);
            return true;
        }

        private static bool TryMapFocus(OperationMapDefinition map, bool air, out Vector3 player, out Vector3 enemy)
        {
            player = default;
            enemy = default;
            if (map == null)
                return false;
            bool havePlayer = air &&
                              (TryAnchor(map, OperationMapAnchorKind.Helipad, 1, out player) ||
                               TryAnchor(map, OperationMapAnchorKind.Runway, 1, out player));
            if (!havePlayer)
                havePlayer = TryAnchor(map, OperationMapAnchorKind.Deployment, 1, out player);
            bool haveEnemy = TryAnchor(map, OperationMapAnchorKind.Deployment, 2, out enemy) ||
                             TryAnchor(map, OperationMapAnchorKind.Base, 2, out enemy) ||
                             TryAnchor(map, OperationMapAnchorKind.Objective, 2, out enemy);
            return havePlayer && haveEnemy;
        }

        private static bool TryAnchor(
            OperationMapDefinition map,
            OperationMapAnchorKind kind,
            int factionId,
            out Vector3 position)
        {
            ReadOnlySpan<OperationMapAnchorConfig> anchors = map.Anchors;
            for (int i = 0; i < anchors.Length; i++)
            {
                if (anchors[i].Kind == kind && anchors[i].FactionId == factionId)
                {
                    position = anchors[i].Position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        private static float AnchorHeight(OperationMapDefinition map, int factionId)
        {
            if (map != null && TryAnchor(map, OperationMapAnchorKind.Deployment, factionId, out Vector3 deployment))
                return deployment.y;
            return 0f;
        }

        private static bool IsAir(SkirmishBattleCatalogEntry entry, SkirmishScenarioDefinitionConfig definition)
        {
            if (definition != null && definition.ArmyProfileConfig != null)
                return definition.ArmyProfileConfig.Kind == SkirmishArmyProfileId.AirMobile;
            return string.Equals(entry.ArmyProfile, "A", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEstablished(SkirmishBattleCatalogEntry entry, SkirmishScenarioDefinitionConfig definition)
        {
            if (definition != null && definition.StartPackageConfig != null)
                return definition.StartPackageConfig.Kind == SkirmishStartPackageId.EstablishedBase;
            return string.Equals(entry.StartProfile, "E", StringComparison.OrdinalIgnoreCase);
        }

        private static OperationMapCameraConfig FrameCard(
            string scenarioId,
            Vector3 player,
            Vector3 enemy,
            bool established)
        {
            // The scenario objective (enemy base / target) is the subject; approach from the player's side.
            Vector3 look = Vector3.Lerp(enemy, player, 0.12f) + Vector3.up * 6f;
            Vector3 back = player - enemy;
            back.y = 0f;
            if (back.sqrMagnitude < 0.01f)
                back = Vector3.back;
            back.Normalize();

            // Deterministic per-scenario azimuth so cards that share a map and objective stay distinguishable.
            int hash = 17;
            string id = scenarioId ?? string.Empty;
            for (int i = 0; i < id.Length; i++)
                hash = hash * 31 + id[i];
            float azimuth = ((hash & 0x7fffffff) % 7 - 3) * 14f + (established ? 38f : -22f);
            back = Quaternion.AngleAxis(azimuth, Vector3.up) * back;

            float separation = Vector2.Distance(new Vector2(player.x, player.z), new Vector2(enemy.x, enemy.z));
            float distance = Mathf.Clamp(separation * 0.13f, 65f, 95f) * (established ? 0.85f : 1f);
            float pitch = established ? 30f : 26f;
            Vector3 position = look + back * (distance * Mathf.Cos(pitch * Mathf.Deg2Rad)) +
                               Vector3.up * (distance * Mathf.Sin(pitch * Mathf.Deg2Rad));
            Quaternion rotation = Quaternion.LookRotation(look - position, Vector3.up);
            return new OperationMapCameraConfig(
                "camera.skirmish.card." + id.ToLowerInvariant(),
                position,
                rotation.eulerAngles,
                false,
                42f,
                0f,
                false);
        }

        private static void RejectPlateDuplicate(
            OperationMapDefinition map,
            OperationMapCameraConfig card,
            string scenarioId)
        {
            OperationMapCameraConfig plate = RequirePlanningCamera(map);
            float positionGap = Vector3.Distance(plate.Position, card.Position);
            float angleGap = Quaternion.Angle(Quaternion.Euler(plate.EulerAngles), Quaternion.Euler(card.EulerAngles));
            if (positionGap < 5f && angleGap < 2f)
                throw new InvalidOperationException(
                    "Card camera for " + scenarioId + " matches the plate camera " + plate.CameraId + ".");
        }

        private static void PlateBounds(OperationMapDefinition map, out Vector3 min, out Vector3 max)
        {
            min = map.Bounds.PlayableMin;
            max = map.Bounds.PlayableMax;
            if (max.x - min.x < 1f || max.z - min.z < 1f)
            {
                min = map.Bounds.WorldMin;
                max = map.Bounds.WorldMax;
            }
        }

        private static OperationMapCameraConfig FrameTopDown(string mapId, Vector3 min, Vector3 max)
        {
            // Oblique overview (not flat ortho) so relief and structures read with depth like the match camera.
            Vector3 center = (min + max) * 0.5f;
            float width = Mathf.Max(1f, max.x - min.x);
            float depth = Mathf.Max(1f, max.z - min.z);
            float extent = Mathf.Max(width / (PlateWidth / (float)PlateHeight), depth) * 0.5f;
            const float fov = 40f;
            const float pitch = 52f;
            float distance = extent / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * 0.95f;
            Vector3 dir = Quaternion.Euler(pitch, 20f, 0f) * Vector3.forward;
            Vector3 position = center - dir * distance;
            return new OperationMapCameraConfig(
                "camera.skirmish." + mapId.ToLowerInvariant() + ".overview",
                position,
                Quaternion.LookRotation(dir, Vector3.up).eulerAngles,
                false,
                fov,
                0f,
                false);
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
            int subScenes = OpenSubScenesForPreview(opened);
            Debug.Log("[SkirmishLibraryPreviewCapture] scene=" + scenePath + " subscenes=" + subScenes +
                      " renderers=" + UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length);

            ApplyMatchPresentation(opened, out GameObject createdSun, out GameObject createdVolume);
            DisableHudAndFog();

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
            ApplyMatchCamera(camera);

            for (int i = 0; i < shots.Count; i++)
            {
                if (!shots[i].RefineCard)
                    continue;
                Shot card = shots[i];
                card.Camera = CenterOnStructures(card.Camera, card.CardSubject, out string cardLog);
                card.LogLine += " " + cardLog;
                shots[i] = card;
            }

            for (int i = 0; i < shots.Count; i++)
            {
                if (shots[i].FocusKeywords == null)
                    continue;
                Shot focused = shots[i];
                focused.Camera = FrameKeywordFocus(focused, out string focusLog);
                focused.LogLine += " " + focusLog;
                shots[i] = focused;
            }

            try
            {
                // Warm shaders and lighting first; the first editor render of a freshly opened scene is unreliable.
                for (int warm = 0; warm < 3; warm++)
                    RenderShot(camera, shots[0], write: false);
                for (int i = 0; i < shots.Count; i++)
                    RenderShot(camera, shots[i], write: true);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                if (createdSun != null)
                    UnityEngine.Object.DestroyImmediate(createdSun);
                if (createdVolume != null)
                    UnityEngine.Object.DestroyImmediate(createdVolume);
                for (int i = 0; i < canvases.Length; i++)
                {
                    if (canvases[i] != null)
                        canvases[i].enabled = enabled[i];
                }
            }
        }

        private static void ApplyMatchPresentation(Scene opened, out GameObject createdSun, out GameObject createdVolume)
        {
            createdSun = null;
            createdVolume = null;
            Scene daylight = FindDaylightScene(opened);
            if (daylight.IsValid() && daylight.isLoaded && SceneManager.GetActiveScene() != daylight)
                SceneManager.SetActiveScene(daylight);

            RenderSettings.fog = false;
            SkirmishS004CampaignLighting.ApplyDaylightFill();

            Light sun = FindDirectional(daylight);
            if (sun == null)
            {
                createdSun = new GameObject("SkirmishLibraryMatchSun");
                sun = createdSun.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = MatchSunColor;
                sun.intensity = MatchSunIntensity;
                sun.shadows = LightShadows.Soft;
                createdSun.transform.rotation = MatchSunRotation;
            }

            RenderSettings.sun = sun;
            DisableOtherDirectionals(sun);
            createdVolume = EnsureMatchVolume();
            Debug.Log(
                "[SkirmishLibraryPreviewCapture] lighting=MatchDaylight fog=off post=on volume=Military_Demo_Gameplay sun=" +
                sun.intensity.ToString("0.00", CultureInfo.InvariantCulture));
        }

        private static Scene FindDaylightScene(Scene opened)
        {
            if (SkirmishS004CampaignLighting.HasAuthoredDaylight(opened))
                return opened;
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (SkirmishS004CampaignLighting.HasAuthoredDaylight(scene))
                    return scene;
            }

            return opened;
        }

        private static Light FindDirectional(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Light light in root.GetComponentsInChildren<Light>(false))
                {
                    if (light.enabled && light.type == LightType.Directional && light.intensity > 0f)
                        return light;
                }
            }

            return null;
        }

        private static void DisableOtherDirectionals(Light keep)
        {
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i] != keep && lights[i].type == LightType.Directional)
                    lights[i].enabled = false;
            }
        }

        internal static GameObject EnsureMatchVolume()
        {
            ScriptableObject profile = AssetDatabase.LoadAssetAtPath<ScriptableObject>(MatchVolumeProfilePath);
            if (profile == null)
                throw new InvalidOperationException("Match volume profile missing at " + MatchVolumeProfilePath + ".");

            Type volumeType = Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            if (volumeType == null)
                throw new InvalidOperationException("URP Volume type is unavailable.");

            // Every battlefield gets the identical match grade: mute scene volumes, then add one global match volume.
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null && behaviour.GetType() == volumeType)
                    volumeType.GetProperty("weight")?.SetValue(behaviour, 0f);
            }

            var volumeObject = new GameObject("SkirmishLibraryMatchVolume");
            AssignVolume(volumeObject.AddComponent(volumeType), profile);
            return volumeObject;
        }

        private static void AssignVolume(Component volume, ScriptableObject profile)
        {
            var serialized = new SerializedObject(volume);
            SerializedProperty isGlobal = serialized.FindProperty("m_IsGlobal") ?? serialized.FindProperty("isGlobal");
            SerializedProperty weight = serialized.FindProperty("weight");
            SerializedProperty sharedProfile = serialized.FindProperty("sharedProfile");
            if (sharedProfile == null)
                throw new InvalidOperationException("Volume.sharedProfile is not serialized.");
            if (isGlobal != null)
                isGlobal.boolValue = true;
            if (weight != null)
                weight.floatValue = 1f;
            SerializedProperty priority = serialized.FindProperty("m_Priority") ?? serialized.FindProperty("priority");
            if (priority != null)
                priority.floatValue = 1000f;
            sharedProfile.objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyMatchCamera(Camera camera)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            camera.cullingMask = uiLayer >= 0 ? ~(1 << uiLayer) : ~0;
            camera.clearFlags = RenderSettings.skybox != null
                ? CameraClearFlags.Skybox
                : CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.70f, 0.78f, 1f);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 8000f;
            camera.allowHDR = true;
            camera.allowMSAA = true;

            Type dataType = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (dataType == null)
                throw new InvalidOperationException("URP camera data is unavailable.");
            Component data = camera.GetComponent(dataType) ?? camera.gameObject.AddComponent(dataType);
            dataType.GetProperty("renderPostProcessing")?.SetValue(data, true);
            Type aaType = Type.GetType(
                "UnityEngine.Rendering.Universal.AntialiasingMode, Unity.RenderPipelines.Universal.Runtime");
            PropertyInfo aa = dataType.GetProperty("antialiasing");
            if (aaType != null && aa != null)
                aa.SetValue(data, Enum.Parse(aaType, "SubpixelMorphologicalAntiAliasing"));
        }

        private static void DisableHudAndFog()
        {
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;
                string typeName = behaviour.GetType().Name;
                if (typeName.IndexOf("Fog", StringComparison.OrdinalIgnoreCase) >= 0)
                    behaviour.enabled = false;
                if (typeName == "Volume")
                    MuteFogVolume(behaviour);
            }
        }

        private static void MuteFogVolume(MonoBehaviour volume)
        {
            string objectName = volume.gameObject != null ? volume.gameObject.name : string.Empty;
            object profile = volume.GetType().GetProperty("sharedProfile")?.GetValue(volume)
                             ?? volume.GetType().GetProperty("profile")?.GetValue(volume);
            string profileName = profile != null ? profile.ToString() : string.Empty;
            if (objectName.IndexOf("Fog", StringComparison.OrdinalIgnoreCase) < 0 &&
                profileName.IndexOf("Fog", StringComparison.OrdinalIgnoreCase) < 0)
                return;
            volume.GetType().GetProperty("weight")?.SetValue(volume, 0f);
        }

        private static int OpenSubScenesForPreview(Scene root)
        {
            int count = 0;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (GameObject go in root.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null)
                        continue;
                    if (behaviour.GetType().Name != "SubScene")
                    {
                        // Campaign runtime bindings reference their prepared presentation scene by GUID.
                        SerializedProperty sourceGuid = new SerializedObject(behaviour).FindProperty("presentationSourceSceneGuid");
                        string sourcePath = sourceGuid != null && sourceGuid.propertyType == SerializedPropertyType.String
                            ? AssetDatabase.GUIDToAssetPath(sourceGuid.stringValue)
                            : null;
                        if (!string.IsNullOrEmpty(sourcePath) && sourcePath.EndsWith(".unity", StringComparison.Ordinal) &&
                            seen.Add(sourcePath) &&
                            EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive).IsValid())
                            count++;
                        continue;
                    }

                    var serialized = new SerializedObject(behaviour);
                    SerializedProperty asset = serialized.FindProperty("_SceneAsset");
                    string path = asset != null && asset.objectReferenceValue != null
                        ? AssetDatabase.GetAssetPath(asset.objectReferenceValue)
                        : null;
                    if (string.IsNullOrEmpty(path) || !seen.Add(path))
                        continue;
                    Scene sub = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    if (sub.IsValid())
                        count++;
                }
            }

            return count;
        }

        private static void RenderShot(Camera camera, Shot shot, bool write)
        {
            ApplyMatchCamera(camera);
            OperationMapCameraConfig pose = shot.Camera;
            int width = shot.Width > 0 ? shot.Width : CardWidth;
            int height = shot.Height > 0 ? shot.Height : CardHeight;
            camera.orthographic = pose.Orthographic;
            camera.fieldOfView = pose.FieldOfView > 0f ? pose.FieldOfView : 60f;
            camera.orthographicSize = pose.OrthographicSize > 0f ? pose.OrthographicSize : 50f;
            camera.aspect = width / (float)height;
            camera.transform.SetPositionAndRotation(pose.Position, Quaternion.Euler(pose.EulerAngles));

            // Render at 2x with 8x MSAA, then downsample in two linear-filtered halving steps (box filter).
            var hiDesc = new RenderTextureDescriptor(width * Supersample, height * Supersample, RenderTextureFormat.ARGB32, 24)
            {
                msaaSamples = 8,
                sRGB = true
            };
            var hi = new RenderTexture(hiDesc) { filterMode = FilterMode.Bilinear };
            var lo = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                filterMode = FilterMode.Bilinear
            };
            var resolved = new RenderTexture(width * Supersample, height * Supersample, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB) { filterMode = FilterMode.Bilinear };
            Texture2D image = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                hi.Create();
                lo.Create();
                resolved.Create();
                camera.targetTexture = hi;
                camera.Render();
                camera.targetTexture = null;
                Graphics.Blit(hi, resolved);
                Graphics.Blit(resolved, lo);
                RenderTexture.active = lo;
                image = new Texture2D(width, height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply();
                if (!write)
                    return;
                ApplyGrade(image, shot);
                string absolute = ProjectPath(shot.OutputAssetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? absolute);
                File.WriteAllBytes(absolute, image.EncodeToPNG());
                Debug.Log(shot.LogLine + " size=" + width + "x" + height);
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                if (image != null)
                    UnityEngine.Object.DestroyImmediate(image);
                foreach (RenderTexture texture in new[] { hi, lo, resolved })
                {
                    texture.Release();
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        // Small per-plate trim so the paler campaign-derived plates sit at the same exposure/contrast as DB.
        private static void ApplyGrade(Texture2D image, Shot shot)
        {
            float contrast = shot.GradeContrast <= 0f ? 1f : shot.GradeContrast;
            float exposure = shot.GradeExposure <= 0f ? 1f : shot.GradeExposure;
            float saturation = shot.GradeSaturation <= 0f ? 1f : shot.GradeSaturation;
            if (Mathf.Approximately(contrast, 1f) && Mathf.Approximately(exposure, 1f) && Mathf.Approximately(saturation, 1f))
                return;
            Color32[] pixels = image.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float luma = c.r * 0.2126f + c.g * 0.7152f + c.b * 0.0722f;
                Color g = Color.Lerp(new Color(luma, luma, luma, 1f), c, saturation);
                g.r = Mathf.Clamp01(((g.r * exposure) - 0.5f) * contrast + 0.5f);
                g.g = Mathf.Clamp01(((g.g * exposure) - 0.5f) * contrast + 0.5f);
                g.b = Mathf.Clamp01(((g.b * exposure) - 0.5f) * contrast + 0.5f);
                g.a = 1f;
                pixels[i] = g;
            }

            image.SetPixels32(pixels);
            image.Apply();
        }

        private static OperationMapCameraConfig FrameKeywordFocus(Shot shot, out string log)
        {
            if (shot.FocusKeywords.Length == 1 && shot.FocusKeywords[0] == DenseSectorKey)
                return FrameDenseSector(shot, out log);
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var points = new List<Vector3>();
            bool all = shot.FocusKeywords.Length == 0;
            float radius = all ? float.MaxValue : 700f;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;
                if (!all && !MatchesKeyword(renderer.transform, shot.FocusKeywords))
                    continue;
                Vector3 c = renderer.bounds.center;
                if (new Vector2(c.x - shot.FocusHint.x, c.z - shot.FocusHint.z).magnitude > radius)
                    continue;
                points.Add(c);
            }

            if (points.Count < 3)
            {
                log = "focus=keywords matches=" + points.Count + " fallback=planning";
                return shot.Camera;
            }

            float[] xs = new float[points.Count];
            float[] zs = new float[points.Count];
            float[] ys = new float[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                xs[i] = points[i].x;
                zs[i] = points[i].z;
                ys[i] = points[i].y;
            }

            Array.Sort(xs);
            Array.Sort(zs);
            Array.Sort(ys);
            float Pct(float[] a, float p) => a[Mathf.Clamp(Mathf.RoundToInt(p * (a.Length - 1)), 0, a.Length - 1)];
            float lowP = all ? 0.2f : 0.1f;
            var min = new Vector3(Pct(xs, lowP), Pct(ys, 0.5f), Pct(zs, lowP));
            var max = new Vector3(Pct(xs, 1f - lowP), Pct(ys, 0.5f), Pct(zs, 1f - lowP));
            var center = new Vector3((min.x + max.x) * 0.5f, Pct(ys, 0.25f), (min.z + max.z) * 0.5f);
            float extent = Mathf.Clamp(Mathf.Max((max.x - min.x) / (PlateWidth / (float)PlateHeight), max.z - min.z) * 0.5f, 60f, all ? 520f : 260f);
            const float fov = 40f;
            float pitch = all ? 50f : 38f;
            float distance = extent / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * 1.15f;
            Vector3 dir = Quaternion.Euler(pitch, 35f, 0f) * Vector3.forward;
            log = "focus=" + (all ? "content" : "refinery") + " matches=" + points.Count + " center=" + Format(center) + " extent=" +
                  extent.ToString("0", CultureInfo.InvariantCulture);
            return new OperationMapCameraConfig(
                shot.Camera.CameraId + (all ? ".content" : ".refinery"),
                center - dir * distance,
                Quaternion.LookRotation(dir, Vector3.up).eulerAngles,
                false,
                fov,
                0f,
                false);
        }

        // Runtime-spawned bases are absent from the static scene, so slide the card onto the nearest built-up
        // cluster around the objective to keep a readable subject instead of empty sand.
        private static OperationMapCameraConfig CenterOnStructures(OperationMapCameraConfig pose, Vector3 subject, out string log)
        {
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var xs = new List<float>();
            var zs = new List<float>();
            const float radius = 160f;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;
                Bounds b = renderer.bounds;
                if (b.size.y < 4f || b.size.x > 80f || b.size.z > 80f)
                    continue;
                if (new Vector2(b.center.x - subject.x, b.center.z - subject.z).magnitude > radius)
                    continue;
                xs.Add(b.center.x);
                zs.Add(b.center.z);
            }

            if (xs.Count < 12)
            {
                log = "card_subject=anchor structures=" + xs.Count;
                return pose;
            }

            xs.Sort();
            zs.Sort();
            var target = new Vector3(xs[xs.Count / 2], subject.y, zs[zs.Count / 2]);
            Vector3 shift = target - subject;
            shift.y = 0f;
            log = "card_subject=structures structures=" + xs.Count + " shift=" + Format(shift);
            return new OperationMapCameraConfig(
                pose.CameraId,
                pose.Position + shift,
                pose.EulerAngles,
                pose.Orthographic,
                pose.FieldOfView,
                pose.OrthographicSize,
                false);
        }

        // City Crossroads: pick the most built-up 120 m sectors (tall structures) near the battlefield and frame them.
        private static OperationMapCameraConfig FrameDenseSector(Shot shot, out string log)
        {
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            const float cell = 80f;
            const float radius = 650f;
            var counts = new Dictionary<Vector2Int, int>();
            var heights = new Dictionary<Vector2Int, float>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;
                Bounds b = renderer.bounds;
                if (b.size.y < 7f || b.size.x > 90f || b.size.z > 90f)
                    continue;
                if (new Vector2(b.center.x - shot.FocusHint.x, b.center.z - shot.FocusHint.z).magnitude > radius)
                    continue;
                var key = new Vector2Int(Mathf.FloorToInt(b.center.x / cell), Mathf.FloorToInt(b.center.z / cell));
                counts.TryGetValue(key, out int n);
                counts[key] = n + 1;
                heights.TryGetValue(key, out float h);
                heights[key] = h + b.size.y;
            }

            Vector2Int best = default;
            float bestScore = -1f;
            foreach (KeyValuePair<Vector2Int, int> pair in counts)
            {
                // Score a 2x2 sector block so the frame covers a connected built-up area.
                // 3x3 block centred on the sector; an empty (sand) sector anywhere in the
                // block zeroes most of its score so the frame lands on continuous city fabric.
                float score = 0f;
                int filled = 0;
                for (int dx = -1; dx <= 1; dx++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    var k = new Vector2Int(pair.Key.x + dx, pair.Key.y + dz);
                    if (counts.TryGetValue(k, out int c) && c >= 6) { filled++; score += heights[k]; }
                }
                score *= filled == 9 ? 1f : filled / 30f;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = pair.Key;
                }
            }

            if (bestScore <= 0f)
            {
                log = "focus=dense sectors=0 fallback=planning";
                return shot.Camera;
            }

            // Pull the centre toward the building mass of the 3x3 block (height-weighted) so open sand
            // at the block edge drops out of frame, and frame slightly tighter.
            Vector2 weighted = Vector2.zero;
            float weight = 0f;
            for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                var k = new Vector2Int(best.x + dx, best.y + dz);
                if (!heights.TryGetValue(k, out float h)) continue;
                weighted += new Vector2((k.x + 0.5f) * cell, (k.y + 0.5f) * cell) * h;
                weight += h;
            }
            Vector2 c2 = weight > 0f ? weighted / weight : new Vector2((best.x + 0.5f) * cell, (best.y + 0.5f) * cell);
            var center = new Vector3(c2.x, shot.FocusHint.y, c2.y);
            const float fov = 40f;
            const float pitch = 48f;
            float distance = 72f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            Vector3 dir = Quaternion.Euler(pitch, 30f, 0f) * Vector3.forward;
            log = "focus=dense sectors=" + counts.Count + " center=" + Format(center) + " score=" +
                  bestScore.ToString("0", CultureInfo.InvariantCulture);
            return new OperationMapCameraConfig(
                shot.Camera.CameraId + ".dense",
                center - dir * distance,
                Quaternion.LookRotation(dir, Vector3.up).eulerAngles,
                false,
                fov,
                0f,
                false);
        }

        private static bool MatchesKeyword(Transform transform, string[] keywords)
        {
            for (int depth = 0; transform != null && depth < 4; depth++, transform = transform.parent)
            {
                string name = transform.name;
                for (int k = 0; k < keywords.Length; k++)
                {
                    if (name.IndexOf(keywords[k], StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
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
                importer.alphaIsTransparency = false;
                importer.maxTextureSize = MaxImportSize;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }

            SkirmishLibrarySpriteRegistryBuilder.Rebuild();
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

        private static string Format(Vector3 value) =>
            "(" + value.x.ToString("0.0", CultureInfo.InvariantCulture) + "," +
            value.y.ToString("0.0", CultureInfo.InvariantCulture) + "," +
            value.z.ToString("0.0", CultureInfo.InvariantCulture) + ")";

        private struct Shot
        {
            public string ScenePath;
            public string OutputAssetPath;
            public OperationMapCameraConfig Camera;
            public string LogLine;
            public int Width;
            public int Height;
            public string[] FocusKeywords;
            public Vector3 FocusHint;
            public bool RefineCard;
            public float GradeContrast;
            public float GradeExposure;
            public float GradeSaturation;
            public Vector3 CardSubject;
        }
    }
}
#endif
