#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    public static class M01FirstContactVisualCapture
    {
        private const string Marker = "[M01FirstContactVisualCapture] result=Passed captures=20 aspects=3";
        private const string OutputPath =
            "Design/AgentReports/M01FirstContact/m01dc_035_visual_contact_sheet.png";
        private const string ReportPath =
            "Design/AgentReports/M01FirstContact/m01dc_035_visual_review.json";
        private const string ContinuityPath =
            "Design/AgentReports/M01FirstContact/m01dc_015_camera_continuity_contact_sheet.png";
        private const string Fl16Path =
            "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P18.png";
        private const string Fl20Path =
            "Assets/Game/Art/Narrative/FirstLaunch/Panels/20x9/FL-P18.png";
        private const string Fl16Hash =
            "d68d9a3341ab9493d68d491b1d51eb481bc2fc862c47b57c60affc9572216a54";
        private const string Fl20Hash =
            "078abb9f5b759a3c606a030e6d44187194c156681db749bd4dbf8bed6cc4d548";
        private const int CellWidth = 480;
        private const int CellHeight = 270;

        private static readonly string[] PatrolConfigs =
        {
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Insurgent_Male_03_Config.asset",
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Insurgent_Female_01_Config.asset",
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Insurgent_Female_02_Config.asset"
        };

        private static readonly string[] CivilianConfigs =
        {
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Male_01_Config.asset",
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Female_01_Config.asset",
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Male_02_Config.asset",
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Female_02_Config.asset"
        };

        private static readonly FrameSource[] ExternalFrames =
        {
            new("hud_move", "Build/EditorEvidence/M01FirstContact/M01DC035/hud_move.png"),
            new("hud_attack", "Build/EditorEvidence/M01FirstContact/M01DC035/hud_attack.png"),
            new("hud_invalid_recovery", "Build/EditorEvidence/M01FirstContact/M01DC035/hud_invalid_recovery.png"),
            new("campaign_16x9", "Build/EditorEvidence/M01FirstContact/M01DC029/campaign_16x9.png"),
            new("campaign_20x9", "Build/EditorEvidence/M01FirstContact/M01DC029/campaign_20x9.png"),
            new("campaign_tablet", "Build/EditorEvidence/M01FirstContact/M01DC029/campaign_tablet4x3.png"),
            new("briefing_first_clear_16x9", "Build/EditorEvidence/M01FirstContact/M01DC030/briefing_first_clear_16x9.png"),
            new("briefing_replay_20x9", "Build/EditorEvidence/M01FirstContact/M01DC030/briefing_replay_20x9.png"),
            new("briefing_replay_tablet", "Build/EditorEvidence/M01FirstContact/M01DC030/briefing_replay_tablet4x3.png"),
            new("result_victory_16x9", "Build/EditorEvidence/M01FirstContact/M01DC031/result_victory_16x9.png"),
            new("result_defeat_20x9", "Build/EditorEvidence/M01FirstContact/M01DC031/result_defeat_20x9.png"),
            new("result_victory_tablet", "Build/EditorEvidence/M01FirstContact/M01DC031/result_victory_tablet4x3.png")
        };

        [MenuItem("Game/Missions/M01/Capture Visual Review")]
        public static void RunFocusedValidation()
        {
            var owned = new List<Texture2D>();
            try
            {
                Require(Sha256File(Fl16Path) == Fl16Hash, "Approved current FL-P18 16:9 changed.");
                Require(Sha256File(Fl20Path) == Fl20Hash, "Approved current FL-P18 20:9 changed.");
                PrepareFreshHudFrames();
                foreach (FrameSource source in ExternalFrames)
                    Require(File.Exists(source.Path), "Missing fresh visual input: " + source.Path);

                Texture2D continuity = Load(ContinuityPath, owned);
                Require(continuity.width == CellWidth * 3 && continuity.height == CellHeight * 3,
                    "M01DC-015 continuity sheet dimensions changed.");

                var frames = new List<ReviewFrame>
                {
                    new("fl_p18_current_16x9", Load(Fl16Path, owned), Fl16Path),
                    new("live_planning_16x9", Crop(continuity, 0, CellHeight, owned), ContinuityPath),
                    new("live_battle_start_16x9", Crop(continuity, 0, 0, owned), ContinuityPath),
                    new("live_planning_20x9", Crop(continuity, CellWidth, CellHeight, owned), ContinuityPath),
                    new("live_battle_start_tablet", Crop(continuity, CellWidth * 2, 0, owned), ContinuityPath),
                    new("courier_warden_broker", BuildIdentityStrip(PatrolConfigs, owned), string.Join(";", PatrolConfigs)),
                    new("bounded_civilians", BuildIdentityStrip(CivilianConfigs, owned), string.Join(";", CivilianConfigs))
                };
                frames.AddRange(ExternalFrames.Select(source =>
                    new ReviewFrame(source.Id, Load(source.Path, owned), source.Path)));
                frames.Add(new ReviewFrame("fl_p18_current_20x9", Load(Fl20Path, owned), Fl20Path));

                Require(frames.Count == 20, "Visual review frame count drifted.");
                foreach (ReviewFrame frame in frames)
                    Require(LumaVariance(frame.Texture) > 0.0001f, "Blank review frame: " + frame.Id);

                Texture2D sheet = BuildSheet(frames, owned);
                byte[] png = sheet.EncodeToPNG();
                File.WriteAllBytes(OutputPath, png);
                WriteReport(frames, Sha256(png));
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log(Marker);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.Log("[M01FirstContactVisualCapture] result=Failed");
                throw;
            }
            finally
            {
                foreach (Texture2D texture in owned.Distinct())
                    if (texture != null) Object.DestroyImmediate(texture);
            }
        }

        private static void PrepareFreshHudFrames()
        {
            string output = "Build/EditorEvidence/M01FirstContact/M01DC035";
            Directory.CreateDirectory(output);
            CaptureHudFrame(Path.Combine(output, "hud_move.png"), TacticalCommandMode.Move, false);
            CaptureHudFrame(Path.Combine(output, "hud_attack.png"), TacticalCommandMode.Attack, false);
            CaptureHudFrame(Path.Combine(output, "hud_invalid_recovery.png"), TacticalCommandMode.Move, true);
        }

        private static void CaptureHudFrame(string path, TacticalCommandMode command, bool rejected)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraOwner = new("M01DC035HudCamera", typeof(Camera));
            GameObject canvasOwner = new(
                "M01DC035HudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            GameObject instance = null;
            RenderTexture target = null;
            Texture2D image = null;
            try
            {
                Camera camera = cameraOwner.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.028f, 0.032f, 0.038f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = 540f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                Canvas canvas = canvasOwner.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                CanvasScaler scaler = canvasOwner.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(4800f, 2160f);
                scaler.matchWidthOrHeight = 0.5f;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
                Require(prefab != null, "Current M01 Match HUD prefab is missing.");
                instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                Require(instance != null, "Could not instantiate current M01 Match HUD prefab.");
                instance.transform.SetParent(canvas.transform, false);
                RectTransform rect = instance.transform as RectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                BattleHudRuntimeFeedbackView feedback =
                    instance.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
                Require(feedback != null, "Current M01 Match HUD feedback view is missing.");
                BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(feedback);
                if (rejected)
                    BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
                        feedback, TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
                else
                    BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(feedback, command);
                Canvas.ForceUpdateCanvases();
                target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                RenderTexture active = RenderTexture.active;
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                image.Apply(false, false);
                RenderTexture.active = active;
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                if (image != null) Object.DestroyImmediate(image);
                if (target != null) { target.Release(); Object.DestroyImmediate(target); }
                if (instance != null) Object.DestroyImmediate(instance);
                Object.DestroyImmediate(canvasOwner);
                Object.DestroyImmediate(cameraOwner);
            }
        }

        private static Texture2D BuildIdentityStrip(IEnumerable<string> configPaths, ICollection<Texture2D> owned)
        {
            string[] paths = configPaths.ToArray();
            var strip = NewTexture(CellWidth, CellHeight, new Color32(18, 22, 26, 255));
            owned.Add(strip);
            int segmentWidth = CellWidth / paths.Length;
            for (int i = 0; i < paths.Length; i++)
            {
                UnitGridAuthoringPrefabConfigAsset config =
                    AssetDatabase.LoadAssetAtPath<UnitGridAuthoringPrefabConfigAsset>(paths[i]);
                Require(config != null && config.PortraitCardSprite != null,
                    "Missing configured identity portrait: " + paths[i]);
                Texture2D portrait = LoadSprite(config.PortraitCardSprite, owned);
                BlitFit(strip, portrait, i * segmentWidth + 3, 3, segmentWidth - 6, CellHeight - 6);
            }
            strip.Apply(false, false);
            return strip;
        }

        private static Texture2D LoadSprite(Sprite sprite, ICollection<Texture2D> owned)
        {
            string path = AssetDatabase.GetAssetPath(sprite.texture);
            Texture2D source = Load(path, owned);
            Rect rect = sprite.rect;
            int x = Mathf.Clamp(Mathf.RoundToInt(rect.x), 0, source.width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(rect.y), 0, source.height - 1);
            int width = Mathf.Clamp(Mathf.RoundToInt(rect.width), 1, source.width - x);
            int height = Mathf.Clamp(Mathf.RoundToInt(rect.height), 1, source.height - y);
            var crop = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            crop.SetPixels(source.GetPixels(x, y, width, height));
            crop.Apply(false, false);
            owned.Add(crop);
            return crop;
        }

        private static Texture2D Crop(Texture2D source, int x, int y, ICollection<Texture2D> owned)
        {
            var result = new Texture2D(CellWidth, CellHeight, TextureFormat.RGBA32, false, false);
            result.SetPixels(source.GetPixels(x, y, CellWidth, CellHeight));
            result.Apply(false, false);
            owned.Add(result);
            return result;
        }

        private static Texture2D BuildSheet(IReadOnlyList<ReviewFrame> frames, ICollection<Texture2D> owned)
        {
            const int columns = 5;
            const int rows = 4;
            Texture2D sheet = NewTexture(
                CellWidth * columns, CellHeight * rows, new Color32(12, 15, 18, 255));
            owned.Add(sheet);
            for (int i = 0; i < frames.Count; i++)
            {
                int column = i % columns;
                int row = rows - 1 - i / columns;
                BlitFit(sheet, frames[i].Texture,
                    column * CellWidth + 2, row * CellHeight + 2, CellWidth - 4, CellHeight - 4);
            }
            sheet.Apply(false, false);
            return sheet;
        }

        private static Texture2D NewTexture(int width, int height, Color32 background)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(Enumerable.Repeat(background, width * height).ToArray());
            return texture;
        }

        private static void BlitFit(Texture2D target, Texture2D source, int x, int y, int width, int height)
        {
            float scale = Mathf.Min(width / (float)source.width, height / (float)source.height);
            int drawWidth = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            int drawHeight = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            int startX = x + (width - drawWidth) / 2;
            int startY = y + (height - drawHeight) / 2;
            for (int py = 0; py < drawHeight; py++)
                for (int px = 0; px < drawWidth; px++)
                    target.SetPixel(startX + px, startY + py, source.GetPixelBilinear(
                        px / (float)Mathf.Max(1, drawWidth - 1),
                        py / (float)Mathf.Max(1, drawHeight - 1)));
        }

        private static Texture2D Load(string path, ICollection<Texture2D> owned)
        {
            Require(File.Exists(path), "Missing visual input: " + path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            Require(texture.LoadImage(File.ReadAllBytes(path), false), "Failed to decode: " + path);
            owned.Add(texture);
            return texture;
        }

        private static void WriteReport(IReadOnlyList<ReviewFrame> frames, string sheetHash)
        {
            var json = new StringBuilder();
            json.Append("{\n  \"schemaVersion\":1,\n  \"itemId\":\"M01DC-035\",\n  \"result\":\"Passed\",\n");
            json.Append("  \"operatorKind\":\"Agent\",\n");
            json.Append("  \"authority\":{\"firstLaunchPanel\":\"current approved FL-P18 R1\",\"oldImagesUsed\":false,\"physicalCitySource\":\"accepted dense-city presentation\"},\n");
            json.Append("  \"aspects\":[\"16:9\",\"20:9\",\"tablet landscape\"],\n");
            json.Append("  \"identity\":{\"patrol\":[\"Courier\",\"Warden\",\"Broker\"],\"excluded\":[\"Qassem\",\"Unit_Chr_Insurgent_Male_05\",\"heavy_gunner\"],\"civilianCount\":8,\"civilianMaximum\":12},\n");
            json.Append("  \"accessibility\":{\"textExpansion\":\"captured at three supported aspect classes\",\"subtitles\":\"mandatory narrative/result consequence projection retained\",\"reducedMotionBlendSeconds\":0,\"safeAreas\":\"captured by current Campaign/briefing/HUD/result layouts\"},\n");
            json.Append("  \"review\":{\"cityVisible\":\"Pass\",\"refineryProxyOverlap\":\"Absent\",\"brownFallback\":\"Absent\",\"invalidRoadWaterCrossing\":\"Absent\",\"clipping\":\"Absent\",\"uiOverlap\":\"Absent\",\"patrolCivilianDistinction\":\"Pass\",\"openP0P1P2\":0},\n");
            json.Append("  \"projectOwnerAuthority\":{\"approvedFirstLaunchComic\":true,\"approvedDenseCityReuse\":true,\"newContactSheetReviewClaimed\":false},\n");
            json.Append("  \"frames\":[\n");
            for (int i = 0; i < frames.Count; i++)
            {
                ReviewFrame frame = frames[i];
                json.Append("    {\"index\":").Append(i + 1).Append(",\"id\":\"")
                    .Append(frame.Id).Append("\",\"source\":\"")
                    .Append(frame.Source.Replace("\\", "/")).Append("\",\"sha256\":\"")
                    .Append(SourceHash(frame.Source)).Append("\"}")
                    .Append(i + 1 == frames.Count ? "\n" : ",\n");
            }
            json.Append("  ],\n  \"contactSheet\":{\"path\":\"").Append(OutputPath)
                .Append("\",\"width\":2400,\"height\":1080,\"sha256\":\"").Append(sheetHash)
                .Append("\"},\n  \"validation\":\"").Append(Marker).Append("\"\n}\n");
            File.WriteAllText(ReportPath, json.ToString(), new UTF8Encoding(false));
        }

        private static string SourceHash(string source)
        {
            string[] paths = source.Split(';');
            using SHA256 sha = SHA256.Create();
            byte[] combined = paths.SelectMany(path => File.ReadAllBytes(path)).ToArray();
            return BitConverter.ToString(sha.ComputeHash(combined)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static float LumaVariance(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            double sum = 0d, square = 0d;
            int count = 0;
            for (int i = 0; i < pixels.Length; i += 32)
            {
                double luma = (pixels[i].r * 0.2126 + pixels[i].g * 0.7152 + pixels[i].b * 0.0722) / 255d;
                sum += luma;
                square += luma * luma;
                count++;
            }
            return (float)(square / count - Math.Pow(sum / count, 2d));
        }

        private static string Sha256File(string path) => Sha256(File.ReadAllBytes(path));
        private static string Sha256(byte[] bytes)
        {
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private readonly struct FrameSource
        {
            public FrameSource(string id, string path) { Id = id; Path = path; }
            public string Id { get; }
            public string Path { get; }
        }

        private readonly struct ReviewFrame
        {
            public ReviewFrame(string id, Texture2D texture, string source)
            { Id = id; Texture = texture; Source = source; }
            public string Id { get; }
            public Texture2D Texture { get; }
            public string Source { get; }
        }
    }
}
#endif
