#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Play Mode capture of Skirmish library cards. Launches every published scenario through the
    /// shipping Skirmish queue (SkirmishLaunchProjection, the same path as the S002 harness) with a fixed
    /// seed and Regular/Standard settings, waits for setup to complete (Phase=Playing, simulation active,
    /// enemy base entities spawned), then renders a 2x supersampled MSAA card framed on enemy base + player start.
    /// Entry point: Game.Editor.SkirmishLibraryPlayCapture.Run. Pass marker:
    /// [SkirmishLibraryPlayCapture] result=Passed
    /// </summary>
    [InitializeOnLoad]
    public static class SkirmishLibraryPlayCapture
    {
        private const string Key = "Warline.SkirmishLibraryPlayCapture";
        private const string QueueKey = Key + ".Queue";
        private const string IndexKey = Key + ".Index";
        private const string FailedKey = Key + ".Failed";
        private const string DoneKey = Key + ".Done";
        private const string QuitKey = Key + ".Quit";
        private const int Seed = 104729;
        private const int CardWidth = 1024;
        private const int CardHeight = 576;
        private const int Supersample = 2;
        private const double ScenarioTimeoutSeconds = 240d;
        private const double SettleSeconds = 4d;
        private const double CloseUpSettleSeconds = 12d;

        // Cards reframed as a low, close oblique on the enemy base with its defenders in view.
        private static bool IsCloseUp(string id) => id == "S002" || id == "S003" || id == "S025";

        private static int stage;
        private static double deadline;
        private static double next;
        private static double readyAt;
        private static double diagnosticAt;

        static SkirmishLibraryPlayCapture()
        {
            if (SessionState.GetBool(Key, false))
                EditorApplication.update += Tick;
            if (SessionState.GetBool(QuitKey, false))
                EditorApplication.update += QuitWhenEditMode;
        }

        public static void Run()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit Play Mode before running the library card capture.");
            var ids = new List<string>();
            SkirmishBattleCatalogConfig catalog = SkirmishBattleCatalogConfig.Load();
            if (catalog == null)
                throw new InvalidOperationException("SkirmishBattleCatalog is missing.");
            string only = Environment.GetEnvironmentVariable("WARLINE_PLAY_CAPTURE_ONLY");
            for (int i = 0; i < catalog.Entries.Count; i++)
                if (catalog.Entries[i].IsPlayable &&
                    (string.IsNullOrEmpty(only) || Array.IndexOf(only.Split(','), catalog.Entries[i].ScenarioId) >= 0))
                    ids.Add(catalog.Entries[i].ScenarioId);
            if (ids.Count == 0)
                throw new InvalidOperationException("No published scenarios.");

            string profileRoot = Path.Combine(Path.GetTempPath(), "skirmish-library-play-capture-profile");
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT", profileRoot);
            var save = new SaveService(new JsonSaveRepository(profileRoot));
            var profile = save.LoadProfile();
            profile.firstLaunchStatus = FirstLaunchProfileState.Completed;
            profile.firstLaunchWatched = true;
            save.SaveProfile(profile);

            SessionState.SetString(QueueKey, string.Join(",", ids));
            SessionState.SetInt(IndexKey, 0);
            SessionState.SetString(FailedKey, string.Empty);
            SessionState.SetString(DoneKey, string.Empty);
            SessionState.SetBool(Key, true);
            Debug.Log("[SkirmishLibraryPlayCapture] start scenarios=" + string.Join(",", ids) + " seed=" + Seed +
                      " difficulty=Normal resources=Standard fog=off");
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            BeginScenario();
        }

        private static string[] Queue() => SessionState.GetString(QueueKey, string.Empty)
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        private static string Current()
        {
            string[] q = Queue();
            int i = SessionState.GetInt(IndexKey, 0);
            return i < q.Length ? q[i] : null;
        }

        private static void BeginScenario()
        {
            stage = 0;
            deadline = 0d;
            next = 0d;
            readyAt = 0d;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false))
            {
                EditorApplication.update -= Tick;
                return;
            }

            string id = Current();
            if (id == null)
            {
                Complete();
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                // Between scenarios: start the next one once Play Mode has fully exited.
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    BeginScenario();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (deadline == 0d)
                deadline = now + ScenarioTimeoutSeconds;
            if (now > deadline)
            {
                Fail(id, "timeout stage=" + stage);
                return;
            }

            if (now < next)
                return;
            next = now + 0.25d;

            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated)
                    return;
                EntityManager em = world.EntityManager;
                foreach (var view in UnityEngine.Object.FindObjectsByType<MatchSceneView>(FindObjectsSortMode.None))
                {
                    if (view.OperationMapContentFailureCode != OperationMapLoadResultCode.None)
                    {
                        Fail(id, "operationMapFailure=" + view.OperationMapContentFailureCode);
                        return;
                    }
                }

                if (stage == 0)
                {
                    if (!SkirmishLaunchProjection.IsShellReadyToEnterMatch(em))
                        return;
                    if (SkirmishLaunchProjection.TryGet(em, out _, out _))
                        return;
                    if (!TryResolveIndex(id, out int scenarioIndex))
                    {
                        Fail(id, "notPublished");
                        return;
                    }

                    QuickGameConfig config = QuickGameConfig.Defaults;
                    config.ScenarioIndex = scenarioIndex;
                    config.MapSeed = Seed;
                    config.FogOfWar = false;
                    if (!SkirmishLaunchProjection.TryQueue(em, config))
                    {
                        Fail(id, "queueRejected index=" + scenarioIndex);
                        return;
                    }

                    // Same as the game's Launch (UiRuntimeAdapters.QueueMatchRoute): queue, then route the shell to Match.
                    if (!Game.UI.Runtime.UiShellRuntimeGateway.TryEnqueueRouteRequest(
                            Game.UI.Contracts.UiShellRouteIntent.EnterMatch, Game.UI.Contracts.UIRoute.Match, false))
                    {
                        Fail(id, "matchRouteRejected");
                        return;
                    }
                    Debug.Log("[SkirmishLibraryPlayCapture] queued scenario=" + id + " index=" + scenarioIndex + " seed=" + Seed);
                    stage = 1;
                    return;
                }

                if (!SkirmishLaunchProjection.TryGet(em, out Entity session, out SkirmishMatchState match))
                    return;
                if (match.StartupFailure != SkirmishStartupFailureCode.None)
                {
                    Fail(id, "startupFailure=" + match.StartupFailure);
                    return;
                }

                if (stage == 1)
                {
                    bool simulation = SkirmishLaunchProjection.IsSimulationActive(em);
                    Gather(em, out List<Vector3> enemyBuildings, out List<Vector3> player, out List<Vector3> enemyAll);
                    if (now >= diagnosticAt)
                    {
                        diagnosticAt = now + 5d;
                        Debug.Log("[SkirmishLibraryPlayCapture] waiting scenario=" + id + " phase=" + match.Phase +
                                  " simulation=" + (simulation ? 1 : 0) + " enemyBuildings=" + enemyBuildings.Count +
                                  " enemyEntities=" + enemyAll.Count + " playerEntities=" + player.Count + " " + ExpandedDiagnostic(em));
                    }

                    if (match.Phase != SkirmishPhase.Playing || player.Count == 0 || enemyAll.Count == 0)
                        return;
                    if (enemyBuildings.Count == 0 && now - deadline + ScenarioTimeoutSeconds < 60d)
                        return;
                    readyAt = now;
                    stage = 2;
                    Debug.Log("[SkirmishLibraryPlayCapture] ready scenario=" + id + " phase=Playing enemyBuildings=" +
                              enemyBuildings.Count + " playerEntities=" + player.Count);
                    return;
                }

                if (stage == 2)
                {
                    // Preview cards show the authored enemy base, so lift the player's fog on it.
                    RevealSessionContacts(em);
                    // Close-up cards show the defenders, so let enemy units spawn and settle longer.
                    double settle = IsCloseUp(id) ? CloseUpSettleSeconds : SettleSeconds;
                    if (now - readyAt < settle || ShaderUtil.anythingCompiling)
                        return;
                    // Wait (bounded) for the session's structure visuals to finish binding.
                    if (VisualPending(em) && now - readyAt < SettleSeconds + 40d)
                        return;
                    Debug.Log("[SkirmishLibraryPlayCapture] settle scenario=" + id + " seconds=" +
                              (now - readyAt).ToString("0.0", CultureInfo.InvariantCulture) + " " + ExpandedDiagnostic(em) +
                              " visualPending=" + (VisualPending(em) ? 1 : 0));
                    Time.timeScale = 0f;
                    Gather(em, out List<Vector3> enemyBuildings, out List<Vector3> player, out List<Vector3> enemyAll);
                    Capture(id, enemyBuildings, player, enemyAll);
                    SessionState.SetString(DoneKey, (SessionState.GetString(DoneKey, string.Empty) + "," + id).Trim(','));
                    Advance();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Fail(id, "exception=" + exception.GetType().Name);
            }
        }

        private static string ExpandedDiagnostic(EntityManager em)
        {
            string text = "expanded=none";
            using (var q = em.CreateEntityQuery(typeof(Game.Components.SkirmishExpandedSessionComponent)))
            {
                if (q.CalculateEntityCount() == 1)
                {
                    var st = q.GetSingleton<Game.Components.SkirmishExpandedSessionComponent>();
                    text = "expandedPhase=" + st.Phase + " init=" + st.InitializationComplete + " spawn=" + st.SpawnComplete +
                           " failure=" + st.FailureCode + " legacy=" + st.IsLegacy;
                }
            }

            using (var sq = em.CreateEntityQuery(typeof(Game.Components.SkirmishExpandedSessionComponent)))
            {
                if (sq.CalculateEntityCount() == 1)
                {
                    Entity se = sq.GetSingletonEntity();
                    text += " sharedReady=" + (em.HasComponent<SkirmishSharedBuildingsReady>(se) ? 1 : 0) +
                            " buildingRequests=" + (em.HasBuffer<SkirmishStartingBuildingRequest>(se) ? em.GetBuffer<SkirmishStartingBuildingRequest>(se).Length : -1);
                }
            }
            using var map = em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
            using var surface = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            using var prefabs = em.CreateEntityQuery(typeof(UnitPrefabRegistryEntry));
            return text + " map=" + map.CalculateEntityCountWithoutFiltering() + " surface=" +
                   surface.CalculateEntityCountWithoutFiltering() + " prefabs=" + prefabs.CalculateEntityCountWithoutFiltering();
        }

        private static bool TryResolveIndex(string id, out int index)
        {
            index = -1;
            SkirmishBattleCatalogConfig catalog = SkirmishBattleCatalogConfig.Load();
            for (int i = 0; catalog != null && i < catalog.Entries.Count; i++)
            {
                if (catalog.Entries[i].IsPlayable && catalog.Entries[i].ScenarioId == id)
                {
                    index = catalog.Entries[i].PlayableScenarioIndex;
                    return true;
                }
            }

            return false;
        }

        private static void Gather(EntityManager em, out List<Vector3> enemyBuildings, out List<Vector3> player, out List<Vector3> enemyAll)
        {
            enemyBuildings = new List<Vector3>();
            player = new List<Vector3>();
            enemyAll = new List<Vector3>();
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<LocalToWorld>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                byte faction = em.GetComponentData<Faction>(entities[i]).Id;
                Vector3 p = em.GetComponentData<LocalToWorld>(entities[i]).Position;
                if (faction == FactionIdentity.PlayerFactionId)
                    player.Add(p);
                else if (faction >= FactionIdentity.EnemyFactionId)
                {
                    enemyAll.Add(p);
                }
            }

            // Runtime buildings carry their owner and grid footprint on the combat info;
            // place them from the grid so the framing follows the visible structures.
            using EntityQuery grids = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            if (grids.CalculateEntityCount() != 1) return;
            GridConfig grid = grids.GetSingleton<GridConfig>();
            using EntityQuery buildings = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeBuildingCombatInfo>());
            using NativeArray<RuntimeBuildingCombatInfo> infos = buildings.ToComponentDataArray<RuntimeBuildingCombatInfo>(Allocator.Temp);
            for (int i = 0; i < infos.Length; i++)
            {
                RuntimeBuildingCombatInfo info = infos[i];
                if (info.IsWall != 0 || info.OwnerFactionId < FactionIdentity.EnemyFactionId) continue;
                enemyBuildings.Add(new Vector3(
                    grid.Origin.x + (info.OriginCell.x + info.FootprintCells.x * 0.5f) * grid.CellSize,
                    grid.Origin.y,
                    grid.Origin.z + (info.OriginCell.y + info.FootprintCells.y * 0.5f) * grid.CellSize));
            }
        }

        private static bool VisualPending(EntityManager em)
        {
            using EntityQuery q = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishExpandedSessionComponent>());
            return q.CalculateEntityCount() == 1 && q.GetSingleton<SkirmishExpandedSessionComponent>().SpawnVisualPending != 0;
        }

        private static void RevealSessionContacts(EntityManager em)
        {
            // Freeze the shared fog pass so it cannot re-hide revealed contacts before the render.
            World world = World.DefaultGameObjectInjectionWorld;
            SystemHandle fog = world.GetExistingSystem<SkirmishSharedFogUpdateSystem>();
            if (fog != SystemHandle.Null)
                world.Unmanaged.ResolveSystemStateRef(fog).Enabled = false;
            using EntityQuery owned = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>());
            using NativeArray<Entity> contacts = owned.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < contacts.Length; i++)
                SkirmishFogService.Reveal(em, contacts[i]);
        }

        private static Vector3 Median(List<Vector3> points)
        {
            var xs = new List<float>();
            var zs = new List<float>();
            var ys = new List<float>();
            foreach (Vector3 p in points) { xs.Add(p.x); ys.Add(p.y); zs.Add(p.z); }
            xs.Sort(); ys.Sort(); zs.Sort();
            return new Vector3(xs[xs.Count / 2], ys[ys.Count / 2], zs[zs.Count / 2]);
        }

        private static List<Vector3> Near(List<Vector3> points, Vector3 center, float radius)
        {
            var result = new List<Vector3>();
            foreach (Vector3 p in points)
                if (new Vector2(p.x - center.x, p.z - center.z).magnitude <= radius)
                    result.Add(p);
            return result.Count > 0 ? result : points;
        }

        private const float KnotRadius = 55f;

        private static Vector3 BestStructureKnot(List<Vector3> structures, Vector3 fallback)
        {
            Vector3 best = fallback;
            int bestCount = -1;
            foreach (Vector3 c in structures)
            {
                int n = 0;
                foreach (Vector3 q in structures)
                    if (new Vector2(q.x - c.x, q.z - c.z).magnitude <= KnotRadius) n++;
                if (n > bestCount) { bestCount = n; best = c; }
            }
            return best;
        }

        private static void Capture(string id, List<Vector3> enemyBuildings, List<Vector3> player, List<Vector3> enemyAll)
        {
            List<Vector3> enemySource = enemyBuildings.Count > 0 ? enemyBuildings : enemyAll;
            Vector3 enemy = Median(enemySource);
            List<Vector3> enemyCluster = Near(enemySource, enemy, 160f);
            Vector3 home = Median(player);
            List<Vector3> playerCluster = Near(player, home, 160f);

            // Fit the enemy base and the player start together, viewed obliquely from behind the player.
            var bounds = new Bounds(enemy, Vector3.one * 30f);
            foreach (Vector3 p in enemyCluster) bounds.Encapsulate(p);
            float separation = new Vector2(enemy.x - home.x, enemy.z - home.z).magnitude;
            // S001 and S025 start close to the enemy; framing both bases made the enemy base tiny, so frame the enemy base alone.
            bool closeUp = IsCloseUp(id);
            bool enemyOnly = closeUp || id == "S001";
            bool both = !enemyOnly && separation < 380f;
            int closeUpCount = 0;
            if (closeUp)
            {
                // These bases are sparse, so frame one tight group of visible structures (with any units beside it)
                // instead of the whole spread. S003 keeps its own low side angle (-95 deg) so it never repeats S002's view.
                Vector3 knot = BestStructureKnot(enemySource, enemy);
                bounds = new Bounds(knot, Vector3.one * 16f);
                foreach (Vector3 q in enemySource)
                    if (new Vector2(q.x - knot.x, q.z - knot.z).magnitude <= KnotRadius) { bounds.Encapsulate(q); closeUpCount++; }
                enemy = new Vector3(bounds.center.x, enemy.y, bounds.center.z);
            }

            Vector3 back = home - enemy;
            back.y = 0f;
            if (back.sqrMagnitude < 1f) back = Vector3.back;
            back.Normalize();
            // S002 and S003 share the Desert Base enemy pads; give S003 (Field) its own low side angle.
            bool fieldAngle = id == "S003";
            back = Quaternion.AngleAxis(fieldAngle ? -95f : id == "S002" ? 40f : 25f, Vector3.up) * back;
            const float fov = 40f;
            float pitch = id == "S002" ? 38f : id == "S025" ? 36f : fieldAngle ? 35f : 55f;
            float aspect = CardWidth / (float)CardHeight;
            Vector3 size = bounds.size;
            float radius = Mathf.Max(size.x, size.z) * 0.5f + 25f;
            float fit = radius / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) / Mathf.Min(1f, aspect * 0.75f);
            float distance = closeUp
                ? Mathf.Clamp((Mathf.Max(size.x, size.z) * 0.5f + 10f) * 0.85f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad), 40f, 95f)
                : Mathf.Clamp(fit * (enemyOnly ? 0.5f : 0.62f), enemyOnly ? 90f : 120f, 420f);
            Vector3 look = both ? Vector3.Lerp(bounds.center, enemy, 0.35f) : enemyOnly ? bounds.center : enemy;
            look.y = Mathf.Min(enemy.y, home.y);
            Vector3 dir = (-back * Mathf.Cos(pitch * Mathf.Deg2Rad) + Vector3.down * Mathf.Sin(pitch * Mathf.Deg2Rad)).normalized;
            Vector3 position = look - dir * distance;

            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                canvas.enabled = false;
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour != null && behaviour.GetType().Name == "UIDocument")
                    behaviour.enabled = false;
            RenderSettings.fog = false;

            var cameraObject = new GameObject("SkirmishLibraryPlayCaptureCamera");
            try
            {
                // Render through the match's own main camera (its URP data, stack and volumes),
                // with its controllers frozen, so the explicit framing below is honoured.
                Camera main = Camera.main;
                Camera camera;
                if (main != null)
                {
                    foreach (MonoBehaviour behaviour in main.GetComponents<MonoBehaviour>())
                        if (behaviour != null && !behaviour.GetType().Name.StartsWith("UniversalAdditional", StringComparison.Ordinal))
                            behaviour.enabled = false;
                    if (main.transform.parent != null)
                        main.transform.SetParent(null, true);
                    camera = main;
                }
                else
                {
                    camera = cameraObject.AddComponent<Camera>();
                }

                int uiLayer = LayerMask.NameToLayer("UI");
                camera.cullingMask = uiLayer >= 0 ? camera.cullingMask & ~(1 << uiLayer) : camera.cullingMask;
                camera.orthographic = false;
                camera.fieldOfView = fov;
                camera.nearClipPlane = 0.5f;
                camera.farClipPlane = 6000f;
                camera.allowMSAA = true;
                camera.allowHDR = true;
                camera.aspect = aspect;
                camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(dir, Vector3.up));
                // Match camera controllers can pin explicit view/projection matrices; drop them so the transform above is used.
                camera.ResetWorldToCameraMatrix();
                camera.ResetProjectionMatrix();
                camera.ResetCullingMatrix();
                Debug.Log("[SkirmishLibraryPlayCapture] camera=" + camera.name + " pos=" + Fmt(camera.transform.position) + " y=" + camera.transform.position.y.ToString("0", CultureInfo.InvariantCulture) +
                          " viewPos=" + Fmt(camera.cameraToWorldMatrix.GetColumn(3)) + " stack=" + Camera.allCamerasCount);
                Type dataType = Type.GetType(
                    "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
                if (dataType != null)
                {
                    Component data = camera.GetComponent(dataType) ?? camera.gameObject.AddComponent(dataType);
                    dataType.GetProperty("renderPostProcessing")?.SetValue(data, true);
                }

                // Close-up cards use the same grade as the DB plate (daylight fill + Military_Demo_Gameplay volume).
                GameObject gradeVolume = null;
                if (closeUp)
                {
                    SkirmishS004CampaignLighting.ApplyDaylightFill();
                    gradeVolume = SkirmishLibraryPreviewCapture.EnsureMatchVolume();
                }

                byte[] png = null;
                for (int pass = 0; pass < 3; pass++)
                    png = RenderPng(camera);
                if (gradeVolume != null)
                    UnityEngine.Object.DestroyImmediate(gradeVolume);
                string output = SkirmishLibraryPreviewAssets.PreviewPath(id);
                string absolute = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, output);
                Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? absolute);
                File.WriteAllBytes(absolute, png);
                Debug.Log("[SkirmishLibraryPlayCapture] card=" + id + " enemyBase=" + Fmt(enemy) + " playerStart=" + Fmt(home) +
                          " enemyBuildings=" + enemyBuildings.Count + " enemyEntities=" + enemyAll.Count +
                          " playerEntities=" + player.Count + " framedBoth=" + (both ? 1 : 0) + " closeUpPoints=" + closeUpCount +
                          " distance=" + distance.ToString("0", CultureInfo.InvariantCulture) +
                          " size=" + CardWidth + "x" + CardHeight + " output=" + output);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static byte[] RenderPng(Camera camera)
        {
            var desc = new RenderTextureDescriptor(CardWidth * Supersample, CardHeight * Supersample, RenderTextureFormat.ARGB32, 24)
            {
                msaaSamples = 8,
                sRGB = true
            };
            var hi = new RenderTexture(desc);
            var resolved = new RenderTexture(CardWidth * Supersample, CardHeight * Supersample, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB) { filterMode = FilterMode.Bilinear };
            var lo = new RenderTexture(CardWidth, CardHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                filterMode = FilterMode.Bilinear
            };
            RenderTexture previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                camera.targetTexture = hi;
                camera.Render();
                camera.targetTexture = null;
                Graphics.Blit(hi, resolved);
                Graphics.Blit(resolved, lo);
                RenderTexture.active = lo;
                image = new Texture2D(CardWidth, CardHeight, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, CardWidth, CardHeight), 0, 0);
                image.Apply();
                return image.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                foreach (RenderTexture t in new[] { hi, resolved, lo })
                {
                    t.Release();
                    UnityEngine.Object.DestroyImmediate(t);
                }
            }
        }

        private static void Fail(string id, string reason)
        {
            Debug.LogError("[SkirmishLibraryPlayCapture] card=" + id + " result=Failed reason=" + reason);
            SessionState.SetString(FailedKey, (SessionState.GetString(FailedKey, string.Empty) + "," + id + ":" + reason).Trim(','));
            Advance();
        }

        private static void Advance()
        {
            Time.timeScale = 1f;
            SessionState.SetInt(IndexKey, SessionState.GetInt(IndexKey, 0) + 1);
            stage = 99;
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
        }

        private static void Complete()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                return;
            }

            EditorApplication.update -= Tick;
            SessionState.SetBool(Key, false);
            string done = SessionState.GetString(DoneKey, string.Empty);
            string failed = SessionState.GetString(FailedKey, string.Empty);
            foreach (string id in done.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string path = SkirmishLibraryPreviewAssets.PreviewPath(id);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = false;
                    importer.maxTextureSize = 2048;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.SaveAndReimport();
                }
            }

            bool passed = string.IsNullOrEmpty(failed) && done.Length > 0;
            Debug.Log("[SkirmishLibraryPlayCapture] result=" + (passed ? "Passed" : "Failed") + " cards=" + done +
                      (passed ? string.Empty : " failed=" + failed));
            if (Application.isBatchMode || Environment.GetEnvironmentVariable("WARLINE_PLAY_CAPTURE_QUIT") == "1")
            {
                SessionState.SetInt(QuitKey + ".Code", passed ? 0 : 2);
                EditorApplication.Exit(passed ? 0 : 2);
            }
        }

        private static void QuitWhenEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= QuitWhenEditMode;
            EditorApplication.Exit(SessionState.GetInt(QuitKey + ".Code", 0));
        }

        private static string Fmt(Vector3 v) =>
            "(" + v.x.ToString("0", CultureInfo.InvariantCulture) + "," + v.z.ToString("0", CultureInfo.InvariantCulture) + ")";
    }
}
#endif
