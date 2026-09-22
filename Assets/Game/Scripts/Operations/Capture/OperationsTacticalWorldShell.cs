using System.Collections.Generic;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Tactical;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Operations.Capture
{
    /// <summary>
    /// Operations-owned Active tactical presentation: greybox map, units, sites, selection.
    /// Built from Loop public actors + map greybox — not the shipping Match scene / Demo 2 meshes.
    /// </summary>
    public sealed class OperationsTacticalWorldShell : MonoBehaviour
    {
        const float UnitY = 0.6f;
        const float SiteY = 0.35f;
        const float GroundY = 0f;

        Transform _root;
        Transform _actorsRoot;
        Transform _selection;
        Camera _camera;
        readonly Dictionary<string, Transform> _actorViews = new();
        string _mapId = string.Empty;
        string _selectedId = string.Empty;
        bool _built;
        bool _preserveSceneCameras;

        public static OperationsTacticalWorldShell Ensure(Transform host)
        {
            if (host == null)
                return null;
            OperationsTacticalWorldShell shell = host.GetComponent<OperationsTacticalWorldShell>();
            if (shell == null)
                shell = host.gameObject.AddComponent<OperationsTacticalWorldShell>();
            return shell;
        }

        public void Sync(OperationsLoopSession loop, string selectedActorOrFocus, bool preserveSceneCameras = false)
        {
            _preserveSceneCameras = preserveSceneCameras;
            if (loop == null || !loop.HasMission)
                return;

            string mapId = loop.MapId;
            if (!_built || !string.Equals(_mapId, mapId, System.StringComparison.Ordinal))
                RebuildMap(mapId);

            OperationsTacticalActorState[] actors = loop.CopyPublicActors();
            SyncActors(actors);
            UpdateSelection(selectedActorOrFocus, actors);
            FrameCamera(actors);
        }

        public void PlaceInFrontOf(Camera camera)
        {
            if (_root == null || camera == null)
                return;
            Vector3 forward = camera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();
            Vector3 position = camera.transform.position + (forward * 36f);
            _root.position = new Vector3(position.x, 0f, position.z);
        }

        public void Clear()
        {
            if (_root != null)
                Destroy(_root.gameObject);
            _root = null;
            _actorsRoot = null;
            _selection = null;
            _actorViews.Clear();
            _mapId = string.Empty;
            _selectedId = string.Empty;
            _built = false;
        }

        void RebuildMap(string mapId)
        {
            Clear();
            _mapId = mapId ?? string.Empty;
            if (!OperationsMapGreyboxCatalog.TryGet(_mapId, out OperationsMapGreybox map))
                return;

            _root = new GameObject("OpsTacticalWorld").transform;
            _root.SetParent(transform, false);
            _actorsRoot = new GameObject("Actors").transform;
            _actorsRoot.SetParent(_root, false);

            Bounds bounds = ComputeBounds(map);
            CreateGround(bounds);
            CreateGrid(bounds);
            CreateBlockers(map);
            CreateAnchors(map);
            ConfigureCamera();
            _built = true;
        }

        void CreateGround(Bounds bounds)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(_root, false);
            float width = Mathf.Max(24f, bounds.size.x + 16f);
            float depth = Mathf.Max(24f, bounds.size.z + 16f);
            ground.transform.localPosition = new Vector3(bounds.center.x, GroundY - 0.15f, bounds.center.z);
            ground.transform.localScale = new Vector3(width, 0.3f, depth);
            ApplyColor(ground, new Color(0.22f, 0.36f, 0.24f));
        }

        void CreateGrid(Bounds bounds)
        {
            const float step = 8f;
            Color line = new Color(0.45f, 0.62f, 0.4f);
            float minX = bounds.min.x - 4f;
            float maxX = bounds.max.x + 4f;
            float minZ = bounds.min.z - 4f;
            float maxZ = bounds.max.z + 4f;
            for (float x = minX; x <= maxX + 0.1f; x += step)
                CreateGridLine(new Vector3(x, 0.04f, (minZ + maxZ) * 0.5f), new Vector3(0.18f, 0.04f, maxZ - minZ), line);
            for (float z = minZ; z <= maxZ + 0.1f; z += step)
                CreateGridLine(new Vector3((minX + maxX) * 0.5f, 0.04f, z), new Vector3(maxX - minX, 0.04f, 0.18f), line);
        }

        void CreateGridLine(Vector3 position, Vector3 scale, Color color)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "Grid";
            line.transform.SetParent(_root, false);
            line.transform.localPosition = position;
            line.transform.localScale = scale;
            ApplyColor(line, color);
        }

        void CreateBlockers(OperationsMapGreybox map)
        {
            int count = Mathf.Min(
                map.BlockerAx.Length,
                Mathf.Min(map.BlockerAz.Length, Mathf.Min(map.BlockerBx.Length, map.BlockerBz.Length)));
            for (int index = 0; index < count; index++)
            {
                float ax = map.BlockerAx[index];
                float az = map.BlockerAz[index];
                float bx = map.BlockerBx[index];
                float bz = map.BlockerBz[index];
                float dx = bx - ax;
                float dz = bz - az;
                float length = Mathf.Sqrt(dx * dx + dz * dz);
                if (length < 0.2f)
                    continue;

                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Blocker_" + index;
                wall.transform.SetParent(_root, false);
                wall.transform.localPosition = new Vector3((ax + bx) * 0.5f, 1.1f, (az + bz) * 0.5f);
                wall.transform.localScale = new Vector3(Mathf.Max(1.2f, length), 2.2f, 1.1f);
                float yaw = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
                wall.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                ApplyColor(wall, new Color(0.32f, 0.34f, 0.3f));
            }
        }

        void CreateAnchors(OperationsMapGreybox map)
        {
            for (int index = 0; index < map.Anchors.Length; index++)
            {
                OperationsGreyboxAnchor anchor = map.Anchors[index];
                if (anchor.Alias != null &&
                    (anchor.Alias.StartsWith("spawn.", System.StringComparison.Ordinal) ||
                     anchor.Alias.StartsWith("exit.", System.StringComparison.Ordinal)))
                {
                    GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pad.name = "Pad_" + anchor.Alias;
                    pad.transform.SetParent(_root, false);
                    pad.transform.localPosition = new Vector3(anchor.X, 0.05f, anchor.Z);
                    pad.transform.localScale = new Vector3(2.4f, 0.08f, 2.4f);
                    Color color = anchor.Alias.StartsWith("exit.", System.StringComparison.Ordinal)
                        ? new Color(0.35f, 0.7f, 0.45f)
                        : new Color(0.35f, 0.45f, 0.65f);
                    ApplyColor(pad, color);
                }
            }
        }

        void SyncActors(OperationsTacticalActorState[] actors)
        {
            var live = new HashSet<string>();
            for (int index = 0; index < actors.Length; index++)
            {
                OperationsTacticalActorState actor = actors[index];
                if (!actor.Spawned || !actor.Alive)
                    continue;

                live.Add(actor.ObjectId);
                if (!_actorViews.TryGetValue(actor.ObjectId, out Transform view) || view == null)
                {
                    view = CreateActorView(actor).transform;
                    _actorViews[actor.ObjectId] = view;
                }

                float y = actor.Body == OperationsTacticalBodyKind.Site ||
                          actor.Body == OperationsTacticalBodyKind.Evidence
                    ? SiteY
                    : UnitY;
                view.localPosition = new Vector3(actor.X, y, actor.Z);
            }

            var remove = new List<string>();
            foreach (KeyValuePair<string, Transform> pair in _actorViews)
            {
                if (!live.Contains(pair.Key))
                    remove.Add(pair.Key);
            }

            for (int index = 0; index < remove.Count; index++)
            {
                string id = remove[index];
                if (_actorViews.TryGetValue(id, out Transform stale) && stale != null)
                    Destroy(stale.gameObject);
                _actorViews.Remove(id);
            }
        }

        GameObject CreateActorView(OperationsTacticalActorState actor)
        {
            PrimitiveType primitive = actor.Body == OperationsTacticalBodyKind.Site ||
                                      actor.Body == OperationsTacticalBodyKind.Evidence
                ? PrimitiveType.Cube
                : actor.Body == OperationsTacticalBodyKind.Cargo || actor.Body == OperationsTacticalBodyKind.Vehicle
                    ? PrimitiveType.Cube
                    : PrimitiveType.Capsule;
            GameObject go = GameObject.CreatePrimitive(primitive);
            go.name = actor.ObjectId;
            go.transform.SetParent(_actorsRoot, false);
            if (primitive == PrimitiveType.Capsule)
                go.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            else if (actor.Body == OperationsTacticalBodyKind.Cargo || actor.Body == OperationsTacticalBodyKind.Vehicle)
                go.transform.localScale = new Vector3(2.4f, 1.2f, 1.6f);
            else
                go.transform.localScale = new Vector3(2.2f, 1.4f, 2.2f);
            ApplyColor(go, ColorFor(actor));
            return go;
        }

        void UpdateSelection(string focus, OperationsTacticalActorState[] actors)
        {
            string selected = focus ?? string.Empty;
            if (selected.Length == 0)
            {
                for (int index = 0; index < actors.Length; index++)
                {
                    if (actors[index].Spawned &&
                        actors[index].Alive &&
                        actors[index].Faction == OperationsTacticalFaction.Player &&
                        actors[index].Commandable)
                    {
                        selected = actors[index].ObjectId;
                        break;
                    }
                }
            }

            if (_selection == null && _root != null)
            {
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Selection";
                ring.transform.SetParent(_root, false);
                ring.transform.localScale = new Vector3(3.4f, 0.08f, 3.4f);
                ApplyColor(ring, new Color(0.95f, 0.85f, 0.25f));
                Collider collider = ring.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);
                _selection = ring.transform;
            }

            _selectedId = selected;
            if (_selection == null)
                return;
            if (_actorViews.TryGetValue(selected, out Transform view) && view != null)
            {
                _selection.gameObject.SetActive(true);
                Vector3 p = view.localPosition;
                _selection.localPosition = new Vector3(p.x, 0.2f, p.z);
            }
            else
            {
                _selection.gameObject.SetActive(false);
            }
        }

        void FrameCamera(OperationsTacticalActorState[] actors)
        {
            if (_preserveSceneCameras)
                return;
            ConfigureCamera();
            if (_camera == null)
                return;

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            int counted = 0;
            for (int index = 0; index < actors.Length; index++)
            {
                OperationsTacticalActorState actor = actors[index];
                if (!actor.Spawned || !actor.Alive)
                    continue;
                counted++;
                if (actor.X < minX) minX = actor.X;
                if (actor.X > maxX) maxX = actor.X;
                if (actor.Z < minZ) minZ = actor.Z;
                if (actor.Z > maxZ) maxZ = actor.Z;
            }

            if (counted == 0 && _root != null)
            {
                Renderer[] renderers = _root.GetComponentsInChildren<Renderer>();
                for (int index = 0; index < renderers.Length; index++)
                {
                    Bounds item = renderers[index].bounds;
                    if (index == 0 && counted == 0)
                    {
                        minX = item.min.x;
                        maxX = item.max.x;
                        minZ = item.min.z;
                        maxZ = item.max.z;
                        counted = 1;
                        continue;
                    }

                    if (item.min.x < minX) minX = item.min.x;
                    if (item.max.x > maxX) maxX = item.max.x;
                    if (item.min.z < minZ) minZ = item.min.z;
                    if (item.max.z > maxZ) maxZ = item.max.z;
                }
            }

            float cx = counted == 0 ? 8f : (minX + maxX) * 0.5f;
            float cz = counted == 0 ? 4f : (minZ + maxZ) * 0.5f;
            float span = counted == 0 ? 24f : Mathf.Max(maxX - minX, maxZ - minZ, 16f);
            _camera.orthographic = true;
            _camera.orthographicSize = Mathf.Clamp(span * 0.62f, 14f, 32f);
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 250f;
            _camera.transform.position = new Vector3(cx, 32f, cz - 10f);
            _camera.transform.rotation = Quaternion.Euler(68f, 0f, 0f);
            _camera.backgroundColor = new Color(0.1f, 0.13f, 0.11f, 1f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
        }

        void ConfigureCamera()
        {
            if (_preserveSceneCameras)
                return;
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
            {
                var cameraObject = new GameObject("OperationsAriaCaptureCamera");
                _camera = cameraObject.AddComponent<Camera>();
                _camera.tag = "MainCamera";
                DontDestroyOnLoad(cameraObject);
            }

            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < cameras.Length; index++)
            {
                if (cameras[index] != null && cameras[index] != _camera)
                    cameras[index].enabled = false;
            }

            _camera.enabled = true;
            _camera.orthographic = true;
            _camera.depth = 100f;
            _camera.cullingMask = ~0;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.1f, 0.13f, 0.11f, 1f);
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            UniversalAdditionalCameraData data = _camera.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Base;
            data.renderShadows = false;
            data.renderPostProcessing = false;
        }

        static Bounds ComputeBounds(OperationsMapGreybox map)
        {
            float minX = 0f;
            float maxX = 0f;
            float minZ = 0f;
            float maxZ = 0f;
            for (int index = 0; index < map.Anchors.Length; index++)
            {
                OperationsGreyboxAnchor anchor = map.Anchors[index];
                if (index == 0)
                {
                    minX = maxX = anchor.X;
                    minZ = maxZ = anchor.Z;
                    continue;
                }

                if (anchor.X < minX) minX = anchor.X;
                if (anchor.X > maxX) maxX = anchor.X;
                if (anchor.Z < minZ) minZ = anchor.Z;
                if (anchor.Z > maxZ) maxZ = anchor.Z;
            }

            var bounds = new Bounds();
            bounds.SetMinMax(new Vector3(minX, 0f, minZ), new Vector3(maxX, 0f, maxZ));
            return bounds;
        }

        static Color ColorFor(OperationsTacticalActorState actor)
        {
            if (actor.Faction == OperationsTacticalFaction.Hostile)
                return new Color(0.78f, 0.28f, 0.22f);
            if (actor.Body == OperationsTacticalBodyKind.Site)
                return new Color(0.55f, 0.7f, 0.85f);
            if (actor.Body == OperationsTacticalBodyKind.Evidence)
                return new Color(0.9f, 0.75f, 0.25f);
            if (actor.Body == OperationsTacticalBodyKind.Cargo)
                return new Color(0.7f, 0.55f, 0.25f);
            if (actor.RosterRole == OperationsRosterRoleKind.RepairSpecialist)
                return new Color(0.35f, 0.75f, 0.7f);
            if (actor.RosterRole == OperationsRosterRoleKind.ReconInfantry)
                return new Color(0.4f, 0.7f, 0.95f);
            return new Color(0.3f, 0.55f, 0.95f);
        }

        static void ApplyColor(GameObject go, Color color)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;
            // Built-in primitive materials do not draw in URP and showed up as a flat cyan slab.
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                Debug.LogError("[OperationsTacticalWorldShell] missing URP unlit shader");
                return;
            }

            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void OnDestroy()
        {
            Clear();
        }
    }
}
