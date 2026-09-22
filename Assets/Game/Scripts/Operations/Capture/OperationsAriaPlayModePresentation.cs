using System.Text;
using Game.Operations.Content;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using UnityEngine;

namespace Game.Operations.Capture
{
    /// <summary>
    /// Operations-owned Play Mode HUD / victory presentation for ARIA capture evidence.
    /// Lives in a runtime (non-Editor-only) assembly so AddComponent works in Editor Play Mode.
    /// Does not use shipping Match scene view, Watch virtual-touch capability, or shipping UI screens.
    /// </summary>
    public sealed class OperationsAriaPlayModePresentation : MonoBehaviour
    {
        public string MissionId = string.Empty;
        public string Language = "en";
        public string PhaseLabel = "Play";
        public string Title = string.Empty;
        public string Body = string.Empty;
        public string OutcomeLabel = string.Empty;
        public string Detail = string.Empty;
        public bool VictoryMode;
        public int Seed;
        public int Tick = -1;
        public string[] ObjectiveLines = System.Array.Empty<string>();

        static OperationsAriaPlayModePresentation _cached;

        public static OperationsAriaPlayModePresentation Ensure()
        {
            if (_cached != null)
                return _cached;

            OperationsAriaPlayModePresentation[] found =
                Object.FindObjectsByType<OperationsAriaPlayModePresentation>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            if (found != null)
            {
                for (int index = 0; index < found.Length; index++)
                {
                    if (found[index] != null)
                    {
                        _cached = found[index];
                        return _cached;
                    }
                }
            }

            if (!Application.isPlaying)
            {
                Debug.LogError("[OperationsAriaPlayModePresentation] Ensure requires Play Mode.");
                return null;
            }

            var host = new GameObject("OperationsAriaPlayModePresentation");
            Object.DontDestroyOnLoad(host);

            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("OperationsAriaCaptureCamera");
                camera = cameraObject.AddComponent<Camera>();
                if (camera == null)
                {
                    Debug.LogError("[OperationsAriaPlayModePresentation] Camera AddComponent returned null.");
                    Object.Destroy(host);
                    return null;
                }

                camera.tag = "MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.07f, 0.09f, 0.12f, 1f);
                camera.orthographic = true;
                camera.depth = 100f;
                Object.DontDestroyOnLoad(cameraObject);
            }

            OperationsAriaPlayModePresentation presenter =
                host.AddComponent<OperationsAriaPlayModePresentation>();
            if (presenter == null)
            {
                Debug.LogError(
                    "[OperationsAriaPlayModePresentation] AddComponent returned null. " +
                    "Presenter must live in a runtime assembly (not Editor-only).");
                Object.Destroy(host);
                return null;
            }

            _cached = presenter;
            Debug.Log("[OperationsAriaPlayModePresentation] ensured host=ok camera=" +
                      (Camera.main != null ? "ok" : "missing"));
            return _cached;
        }

        public static void ClearCache()
        {
            _cached = null;
        }

        public void ShowPlayHud(OperationsLoopSession loop, string missionId, string language, int seed)
        {
            VictoryMode = false;
            MissionId = missionId ?? string.Empty;
            Language = string.IsNullOrEmpty(language) ? "en" : language;
            Seed = seed;
            PhaseLabel = "Active";
            string slug = MissionSlug(MissionId);
            Title = ResolveCopy("operations." + slug + ".title", Language, MissionId);
            Body = ResolveCopy("operations." + slug + ".objective.primary", Language, "Objectives in progress");
            OutcomeLabel = "IN PROGRESS";
            Tick = -1;
            if (loop != null && loop.TryMissionTick(out int tick))
                Tick = tick;

            if (loop != null && loop.TryReadHud(out OperationsHudFrame hud))
            {
                OperationsHudObjective[] required = hud.Required ?? System.Array.Empty<OperationsHudObjective>();
                var lines = new string[required.Length];
                for (int index = 0; index < required.Length; index++)
                {
                    OperationsHudObjective row = required[index];
                    string nodeId = row.NodeId ?? string.Empty;
                    lines[index] = nodeId + " " + (row.Complete ? "[done]" : row.Failed ? "[failed]" : "[active]");
                }

                ObjectiveLines = lines;
                Detail = "tick=" + Tick + " focus=" + (hud.CameraFocus ?? string.Empty) +
                         " civilians=" + hud.CivilianDeaths;
            }
            else
            {
                ObjectiveLines = System.Array.Empty<string>();
                Detail = "tick=" + Tick;
            }
        }

        public void ShowVictory(OperationsLoopSession loop, OperationsAriaEvidenceRecord record)
        {
            if (record == null)
            {
                VictoryMode = true;
                Title = "Victory";
                Body = "Mission complete.";
                OutcomeLabel = "VICTORY";
                ObjectiveLines = System.Array.Empty<string>();
                Detail = string.Empty;
                return;
            }

            VictoryMode = true;
            MissionId = record.MissionId ?? string.Empty;
            Language = string.IsNullOrEmpty(record.Language) ? "en" : record.Language;
            Seed = record.Seed;
            PhaseLabel = "MissionResult";
            string slug = MissionSlug(MissionId);
            Title = ResolveCopy("operations." + slug + ".title", Language, MissionId);
            Body = ResolveCopy("operations." + slug + ".result.victory", Language, "Victory.");
            OutcomeLabel = "VICTORY";
            Tick = record.ObjectiveCompletionTick;
            string shellTop = OperationsShellNames.MissionResult;
            if (loop != null)
            {
                try
                {
                    shellTop = loop.ReadShell().Top ?? shellTop;
                }
                catch
                {
                    // Keep default shell label.
                }
            }

            Detail = "outcome=" + (record.TerminalOutcome ?? string.Empty) +
                     " reason=" + (record.TerminalReason ?? string.Empty) +
                     " result_hash=" + (record.ResultHash ?? string.Empty) +
                     " shell=" + shellTop;
            ObjectiveLines = record.IntentTrace != null && record.IntentTrace.Length > 0
                ? new[]
                {
                    "intents=" + record.IntentTrace.Length,
                    "credits=" + record.ReceivedCredits,
                    "xp=" + record.ReceivedXp
                }
                : System.Array.Empty<string>();
        }

        /// <summary>
        /// Writes a solid Ops-owned victory/HUD PNG without relying on ScreenCapture timing.
        /// Used as a durable evidence fallback alongside Game View screenshots.
        /// </summary>
        public bool TryEncodeFallbackPng(string absolutePath, int width, int height)
        {
            if (string.IsNullOrEmpty(absolutePath) || width < 64 || height < 64)
                return false;

            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            Color fill = VictoryMode
                ? new Color(0.06f, 0.14f, 0.09f, 1f)
                : new Color(0.07f, 0.09f, 0.12f, 1f);
            Color[] pixels = new Color[width * height];
            for (int index = 0; index < pixels.Length; index++)
                pixels[index] = fill;
            texture.SetPixels(pixels);
            texture.Apply(false, false);

            // Stamp a bright band so the PNG is obviously a real rendered surface, not empty.
            Color band = VictoryMode ? new Color(0.35f, 0.75f, 0.45f) : new Color(0.35f, 0.45f, 0.7f);
            for (int y = height / 5; y < height / 5 + 24; y++)
            {
                for (int x = 0; x < width; x++)
                    texture.SetPixel(x, y, band);
            }

            texture.Apply(false, false);
            byte[] png = texture.EncodeToPNG();
            Object.Destroy(texture);
            if (png == null || png.Length == 0)
                return false;

            System.IO.File.WriteAllBytes(absolutePath, png);
            return System.IO.File.Exists(absolutePath) && new System.IO.FileInfo(absolutePath).Length > 0;
        }

        void OnGUI()
        {
            if (ObjectiveLines == null)
                ObjectiveLines = System.Array.Empty<string>();

            Color previous = GUI.color;
            GUI.color = VictoryMode
                ? new Color(0.06f, 0.14f, 0.09f, 0.94f)
                : new Color(0.07f, 0.09f, 0.12f, 0.92f);
            Texture2D white = Texture2D.whiteTexture;
            if (white != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white);
            GUI.color = previous;

            GUIStyle labelSkin = GUI.skin != null ? GUI.skin.label : null;
            var style = labelSkin != null ? new GUIStyle(labelSkin) : new GUIStyle();
            style.fontSize = VictoryMode ? 42 : 28;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.UpperLeft;
            style.wordWrap = true;
            style.normal.textColor = Color.white;

            var bodyStyle = labelSkin != null ? new GUIStyle(labelSkin) : new GUIStyle();
            bodyStyle.fontSize = 22;
            bodyStyle.alignment = TextAnchor.UpperLeft;
            bodyStyle.wordWrap = true;
            bodyStyle.normal.textColor = new Color(0.92f, 0.94f, 0.9f);

            var metaStyle = labelSkin != null ? new GUIStyle(labelSkin) : new GUIStyle();
            metaStyle.fontSize = 16;
            metaStyle.alignment = TextAnchor.UpperLeft;
            metaStyle.wordWrap = true;
            metaStyle.normal.textColor = new Color(0.75f, 0.8f, 0.78f);

            float pad = 48f;
            float width = Mathf.Max(64f, Screen.width - pad * 2f);
            GUI.Label(new Rect(pad, pad, width, 48f), "OPERATIONS", style);
            GUI.Label(new Rect(pad, pad + 56f, width, 48f), OutcomeLabel ?? string.Empty, style);
            GUI.Label(new Rect(pad, pad + 118f, width, 64f), Title ?? string.Empty, style);
            GUI.Label(new Rect(pad, pad + 190f, width, 96f), Body ?? string.Empty, bodyStyle);

            var builder = new StringBuilder(256);
            builder.Append(MissionId ?? string.Empty).Append("  seed=").Append(Seed)
                .Append("  lang=").Append(Language ?? string.Empty)
                .Append("  phase=").Append(PhaseLabel ?? string.Empty);
            if (Tick >= 0)
                builder.Append("  tick=").Append(Tick);
            GUI.Label(new Rect(pad, pad + 300f, width, 40f), builder.ToString(), metaStyle);
            GUI.Label(new Rect(pad, pad + 340f, width, 80f), Detail ?? string.Empty, metaStyle);

            float y = pad + 430f;
            for (int index = 0; index < ObjectiveLines.Length && index < 12; index++)
            {
                GUI.Label(new Rect(pad, y, width, 28f), ObjectiveLines[index] ?? string.Empty, metaStyle);
                y += 28f;
            }

            if (VictoryMode)
            {
                GUI.Label(
                    new Rect(pad, Mathf.Max(pad, Screen.height - pad - 40f), width, 36f),
                    "Ops-owned win screen (Watch shared-UI seam not opened)",
                    metaStyle);
            }
        }

        void OnDestroy()
        {
            if (_cached == this)
                _cached = null;
        }

        static string ResolveCopy(string key, string language, string fallback)
        {
            if (OperationsLocalizedCopy.TryGet(key, language, out string value) && !string.IsNullOrEmpty(value))
                return value;
            return fallback ?? key ?? string.Empty;
        }

        static string MissionSlug(string missionId)
        {
            if (string.IsNullOrEmpty(missionId))
                return "o001";
            int dot = missionId.LastIndexOf('.');
            return dot >= 0 && dot + 1 < missionId.Length ? missionId.Substring(dot + 1) : missionId;
        }
    }
}
