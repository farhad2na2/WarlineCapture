#if UNITY_EDITOR
using System.Text;
using Game.Operations.Content;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    /// <summary>
    /// Operations-owned Play Mode HUD / victory presentation for ARIA capture evidence.
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

        public static OperationsAriaPlayModePresentation Ensure()
        {
            OperationsAriaPlayModePresentation existing =
                Object.FindAnyObjectByType<OperationsAriaPlayModePresentation>(FindObjectsInactive.Exclude);
            if (existing != null)
                return existing;

            var host = new GameObject("OperationsAriaPlayModePresentation");
            Object.DontDestroyOnLoad(host);
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("OperationsAriaCaptureCamera");
                cameraObject.AddComponent<Camera>();
                Object.DontDestroyOnLoad(cameraObject);
            }

            return host.AddComponent<OperationsAriaPlayModePresentation>();
        }

        public void ShowPlayHud(OperationsLoopSession loop, string missionId, string language, int seed)
        {
            VictoryMode = false;
            MissionId = missionId ?? string.Empty;
            Language = string.IsNullOrEmpty(language) ? "en" : language;
            Seed = seed;
            PhaseLabel = "Active";
            string slug = MissionSlug(MissionId);
            Title = OperationsLocalizedCopy.Require("operations." + slug + ".title", Language);
            Body = OperationsLocalizedCopy.Require("operations." + slug + ".objective.primary", Language);
            OutcomeLabel = "IN PROGRESS";
            Tick = -1;
            if (loop != null && loop.TryMissionTick(out int tick))
                Tick = tick;

            if (loop != null && loop.TryReadHud(out OperationsHudFrame hud))
            {
                var lines = new string[hud.Required.Length];
                for (int index = 0; index < hud.Required.Length; index++)
                {
                    OperationsHudObjective row = hud.Required[index];
                    lines[index] = row.NodeId + " " + (row.Complete ? "[done]" : row.Failed ? "[failed]" : "[active]");
                }

                ObjectiveLines = lines;
                Detail = "tick=" + Tick + " focus=" + hud.CameraFocus + " civilians=" + hud.CivilianDeaths;
            }
            else
            {
                ObjectiveLines = System.Array.Empty<string>();
                Detail = "tick=" + Tick;
            }
        }

        public void ShowVictory(OperationsLoopSession loop, OperationsAriaEvidenceRecord record)
        {
            VictoryMode = true;
            MissionId = record.MissionId;
            Language = record.Language;
            Seed = record.Seed;
            PhaseLabel = "MissionResult";
            string slug = MissionSlug(MissionId);
            Title = OperationsLocalizedCopy.Require("operations." + slug + ".title", Language);
            Body = OperationsLocalizedCopy.Require("operations." + slug + ".result.victory", Language);
            OutcomeLabel = "VICTORY";
            Tick = record.ObjectiveCompletionTick;
            Detail = "outcome=" + record.TerminalOutcome +
                     " reason=" + record.TerminalReason +
                     " result_hash=" + record.ResultHash +
                     " shell=" + (loop != null ? loop.ReadShell().Top : OperationsShellNames.MissionResult);
            ObjectiveLines = record.IntentTrace != null && record.IntentTrace.Length > 0
                ? new[]
                {
                    "intents=" + record.IntentTrace.Length,
                    "credits=" + record.ReceivedCredits,
                    "xp=" + record.ReceivedXp
                }
                : System.Array.Empty<string>();
        }

        void OnGUI()
        {
            Color previous = GUI.color;
            GUI.color = VictoryMode
                ? new Color(0.06f, 0.14f, 0.09f, 0.94f)
                : new Color(0.07f, 0.09f, 0.12f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = VictoryMode ? 42 : 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            var bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.94f, 0.9f) }
            };
            var metaStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = new Color(0.75f, 0.8f, 0.78f) }
            };

            float pad = 48f;
            float width = Screen.width - pad * 2f;
            GUI.Label(new Rect(pad, pad, width, 48f), "OPERATIONS", style);
            GUI.Label(new Rect(pad, pad + 56f, width, 48f), OutcomeLabel, style);
            GUI.Label(new Rect(pad, pad + 118f, width, 64f), Title, style);
            GUI.Label(new Rect(pad, pad + 190f, width, 96f), Body, bodyStyle);

            var builder = new StringBuilder(256);
            builder.Append(MissionId).Append("  seed=").Append(Seed)
                .Append("  lang=").Append(Language)
                .Append("  phase=").Append(PhaseLabel);
            if (Tick >= 0)
                builder.Append("  tick=").Append(Tick);
            GUI.Label(new Rect(pad, pad + 300f, width, 40f), builder.ToString(), metaStyle);
            GUI.Label(new Rect(pad, pad + 340f, width, 80f), Detail ?? string.Empty, metaStyle);

            float y = pad + 430f;
            for (int index = 0; index < ObjectiveLines.Length && index < 12; index++)
            {
                GUI.Label(new Rect(pad, y, width, 28f), ObjectiveLines[index], metaStyle);
                y += 28f;
            }

            if (VictoryMode)
            {
                GUI.Label(
                    new Rect(pad, Screen.height - pad - 40f, width, 36f),
                    "Ops-owned win screen (Watch shared-UI seam not opened)",
                    metaStyle);
            }
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
#endif
