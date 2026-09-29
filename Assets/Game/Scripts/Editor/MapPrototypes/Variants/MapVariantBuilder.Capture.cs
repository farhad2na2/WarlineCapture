using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.MapVariants
{
    [Serializable]
    internal sealed class MapVariantZone
    {
        public string id;
        public string suggestedMissions;
        public Vector3 center;
        public Vector3 size;
        public string note;
    }

    [Serializable]
    internal sealed class MapVariantLayout
    {
        public string mapId;
        public string scenePath;
        public Vector2 worldMin;
        public Vector2 worldSize;
        public Vector2 playableMin;
        public Vector2 playableSize;
        public float roadGridSize;
        public List<MapVariantZone> zones = new();
    }

    internal readonly struct MapVariantView
    {
        public readonly string Name;
        public readonly Vector3 Position;
        public readonly Vector3 Euler;
        public readonly float FieldOfView;
        public readonly float OrthographicSize;

        public MapVariantView(string name, Vector3 position, Vector3 euler, float fieldOfView, float orthographicSize = 0f)
        {
            Name = name;
            Position = position;
            Euler = euler;
            FieldOfView = fieldOfView;
            OrthographicSize = orthographicSize;
        }

        // Matches the campaign battle camera: 51.6 degree pitch, 55 degree FOV, looking north.
        public static MapVariantView Battle(string name, Vector2 focus, float groundY, float height = 82f) =>
            new(name,
                new Vector3(focus.x, groundY + height, focus.y - height / Mathf.Tan(51.6f * Mathf.Deg2Rad)),
                new Vector3(51.6f, 0f, 0f),
                55f);
    }

    internal sealed partial class MapVariantBuilder
    {
        public const string SceneFolder = "Assets/Game/Scenes/MapPrototypes/Variants";
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        public MapVariantLayout Layout { get; private set; }

        public string ScenePath => $"{SceneFolder}/MapVariant_{MapId}.unity";

        public string ReviewFolder => Path.Combine(ProjectRoot, "Design", "MapVariants", MapId);

        public static Scene NewScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        public void AddZone(string id, string missions, Vector2 center, Vector2 size, string note)
        {
            Layout ??= new MapVariantLayout
            {
                mapId = MapId,
                scenePath = ScenePath,
                worldMin = World.min,
                worldSize = World.size,
                playableMin = Playable.min,
                playableSize = Playable.size,
                roadGridSize = RoadGridSize
            };
            float y = Height.Sample(center.x, center.y);
            Layout.zones.Add(new MapVariantZone
            {
                id = id,
                suggestedMissions = missions,
                center = new Vector3(center.x, y, center.y),
                size = new Vector3(size.x, 0f, size.y),
                note = note
            });
            AddZoneMarker($"Zone_{id}", new Vector3(center.x, 0f, center.y), new Vector3(size.x, 1f, size.y));
        }

        public void Save(Scene scene, MapVariantAuditReport report)
        {
            EnsureFolder(SceneFolder);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException($"[MapVariants] Failed to save {ScenePath}.");
            Directory.CreateDirectory(ReviewFolder);
            File.WriteAllText(Path.Combine(ReviewFolder, "audit.json"), JsonUtility.ToJson(report, true));
            if (Layout != null)
            {
                string layoutPath = Path.Combine(ProjectRoot, GeneratedFolder, $"MapVariant_{MapId}.layout.json");
                File.WriteAllText(layoutPath, JsonUtility.ToJson(Layout, true));
                AssetDatabase.ImportAsset($"{GeneratedFolder}/MapVariant_{MapId}.layout.json");
            }

            AssetDatabase.SaveAssets();
        }

        public List<string> Capture(IEnumerable<MapVariantView> views)
        {
            Directory.CreateDirectory(ReviewFolder);
            var written = new List<string>();
            var cameraObject = new GameObject("MapVariantCaptureCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            var target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };
            var texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.nearClipPlane = 0.5f;
                camera.farClipPlane = 4000f;
                camera.targetTexture = target;
                bool fog = RenderSettings.fog;
                foreach (MapVariantView view in views)
                {
                    camera.transform.SetPositionAndRotation(view.Position, Quaternion.Euler(view.Euler));
                    camera.orthographic = view.OrthographicSize > 0f;
                    camera.orthographicSize = view.OrthographicSize;
                    camera.fieldOfView = view.FieldOfView;
                    // Layout plans are read without haze; perspective views keep the scene's fog.
                    RenderSettings.fog = fog && !camera.orthographic;
                    camera.Render();
                    RenderSettings.fog = fog;
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                    texture.Apply();
                    string path = Path.Combine(ReviewFolder, $"{view.Name}.png");
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                    written.Add(path);
                }
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(texture);
            }

            return written;
        }

        public MapVariantView TopDown(string name, Rect area)
        {
            float size = Mathf.Max(area.height * 0.5f, area.width * 0.5f * CaptureHeight / CaptureWidth) * 1.02f;
            return new MapVariantView(name, new Vector3(area.center.x, 400f, area.center.y), new Vector3(90f, 0f, 0f), 30f, size);
        }
    }
}
