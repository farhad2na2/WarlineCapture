using System.Collections.Generic;
using Game.Operations.Loop;
using Game.Operations.Tactical;
using UnityEngine;

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

        public static OperationsTacticalWorldShell Ensure(Transform host)
        {
            if (host == null)
                return null;
            OperationsTacticalWorldShell shell = host.GetComponent<OperationsTacticalWorldShell>();
            if (shell == null)
                shell = host.gameObject.AddComponent<OperationsTacticalWorldShell>();
            return shell;
        }

        public void Sync(OperationsLoopSession loop, string selectedActorOrFocus)
        {
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
            CreateBlockers(map);
            CreateAnchors(map);
            EnsureCamera();
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
            ApplyColor(ground, new Color(0.18f, 0.28f, 0.22f));
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
                go.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            else if (actor.Body == OperationsTacticalBodyKind.Cargo || actor.Body == OperationsTacticalBodyKind.Vehicle)
                go.transform.localScale = new Vector3(1.6f, 0.9f, 1.1f);
            else
                go.transform.localScale = new Vector3(1.4f, 1.1f, 1.4f);
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
                ring.transform.localScale = new Vector3(2.2f, 0.05f, 2.2f);
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
                _selection.localPosition = new Vector3(p.x, 0.08f, p.z);
            }
            else
            {
                _selection.gameObject.SetActive(false);
            }
        }

        void FrameCamera(OperationsTacticalActorState[] actors)
        {
            EnsureCamera();
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

            if (counted == 0)
            {
                _camera.transform.position = new Vector3(8f, 28f, -8f);
                _camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
                _camera.orthographicSize = 18f;
                return;
            }

            float cx = (minX + maxX) * 0.5f;
            float cz = (minZ + maxZ) * 0.5f;
            float span = Mathf.Max(maxX - minX, maxZ - minZ, 12f);
            _camera.orthographic = true;
            _camera.orthographicSize = Mathf.Clamp(span * 0.65f + 4f, 10f, 28f);
            _camera.transform.position = new Vector3(cx, 26f, cz - span * 0.35f);
            _camera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            _camera.backgroundColor = new Color(0.12f, 0.16f, 0.14f, 1f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
        }

        void EnsureCamera()
        {
            if (_camera != null)
                return;
            _camera = Camera.main;
            if (_camera != null)
                return;
            var cameraObject = new GameObject("OperationsAriaCaptureCamera");
            _camera = cameraObject.AddComponent<Camera>();
            _camera.tag = "MainCamera";
            _camera.orthographic = true;
            _camera.depth = 100f;
            DontDestroyOnLoad(cameraObject);
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
            Material material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
            material.color = color;
            renderer.sharedMaterial = material;
        }

        void OnDestroy()
        {
            Clear();
        }
    }
}
