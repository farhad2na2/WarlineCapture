using Game.Operations.Content;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Tactical;
using UnityEngine;

namespace Game.Operations.Capture
{
    /// <summary>
    /// Operations-owned Play Mode HUD / victory presentation for O001–O003 mobile-ready capture.
    /// Active: tactical world + phone-mock objective chrome (no seed/tick/bot debug).
    /// Binds Programmer 1 content frames: O001 coach, O002/O003 escort chips, result Trust/Intel/Heat.
    /// Victory: player-facing result card that sells the outcome.
    /// Does not use shipping Match scene view, Watch virtual-touch, or shipping UI screens.
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
        public string PressureLine = string.Empty;
        public string TimerLine = string.Empty;
        public bool VictoryMode;
        public bool ShowDebugChrome;
        public int Seed;
        public int Tick = -1;
        public int Credits;
        public int Xp;
        public string[] ObjectiveLines = System.Array.Empty<string>();
        public bool CoachVisible;
        public string CoachStepLabel = string.Empty;
        public string CoachTitle = string.Empty;
        public string CoachBody = string.Empty;
        public bool WarningVisible;
        public string WarningLabel = string.Empty;
        public string[] ChipLabels = System.Array.Empty<string>();
        public bool ResultDeltasBound;
        public string ContinueLabel = string.Empty;

        OperationsTacticalWorldShell _world;
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
                camera.backgroundColor = new Color(0.12f, 0.16f, 0.14f, 1f);
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

            presenter._world = OperationsTacticalWorldShell.Ensure(host.transform);
            _cached = presenter;
            Debug.Log("[OperationsAriaPlayModePresentation] ensured host=ok camera=" +
                      (Camera.main != null ? "ok" : "missing") + " world=ok");
            return _cached;
        }

        public static void ClearCache()
        {
            _cached = null;
        }

        public void ShowPlayHud(OperationsLoopSession loop, string missionId, string language, int seed)
        {
            VictoryMode = false;
            ShowDebugChrome = false;
            ResultDeltasBound = false;
            ContinueLabel = string.Empty;
            MissionId = missionId ?? string.Empty;
            Language = string.IsNullOrEmpty(language) ? "en" : language;
            Seed = seed;
            PhaseLabel = "Active";
            string slug = MissionSlug(MissionId);
            Title = ResolveCopy("operations." + slug + ".title", Language, MissionId);
            Body = ResolveCopy("operations." + slug + ".objective.primary", Language, "Objectives in progress");
            OutcomeLabel = ResolveCopy("operations.hud.in_progress", Language, "IN PROGRESS");
            Tick = -1;
            PressureLine = string.Empty;
            TimerLine = string.Empty;
            Credits = 0;
            Xp = 0;
            if (loop != null && loop.TryMissionTick(out int tick))
                Tick = tick;

            if (loop != null && loop.TryReadHud(out OperationsHudFrame hud))
            {
                ObjectiveLines = BuildLocalizedObjectives(loop, hud, Language, slug);
                if (hud.TimerVisible)
                {
                    int minutes = hud.TimerRemaining / 60;
                    int seconds = hud.TimerRemaining % 60;
                    TimerLine = ResolveCopy("operations.hud.timer", Language, "Time left") +
                                ": " + minutes.ToString("00") + ":" + seconds.ToString("00");
                }

                PressureLine = BuildPressureLine(loop, hud, Language);
                Detail = string.Empty;
                if (_world == null)
                    _world = OperationsTacticalWorldShell.Ensure(transform);
                _world?.Sync(loop, hud.CameraFocus);
            }
            else
            {
                ObjectiveLines = System.Array.Empty<string>();
                Detail = string.Empty;
            }

            BindCoachAndEscort(loop);
        }

        /// <summary>
        /// Mission-result card from <see cref="OperationsMissionResultProjection"/>:
        /// outcome, Trust / Intel / Heat, and Continue. Uses this shell; no second void HUD.
        /// </summary>
        public void ShowMissionResult(OperationsLoopSession loop)
        {
            if (_world != null)
                _world.Clear();

            VictoryMode = true;
            ShowDebugChrome = false;
            CoachVisible = false;
            WarningVisible = false;
            ChipLabels = System.Array.Empty<string>();
            ResultDeltasBound = false;
            ContinueLabel = ResolveCopy("operations.result.continue", Language, "Continue");
            ObjectiveLines = System.Array.Empty<string>();
            if (loop == null || !OperationsMissionResultProjection.TryRead(loop, out OperationsMissionResultUiFrame ui))
            {
                Title = "Result";
                Body = string.Empty;
                OutcomeLabel = string.Empty;
                return;
            }

            ResultDeltasBound = true;
            MissionId = ui.MissionId ?? string.Empty;
            string slug = MissionSlug(MissionId);
            Title = ResolveCopy("operations." + slug + ".title", Language, MissionId);
            Body = ResolveCopy(ui.OutcomeKey, Language, ui.Outcome.ToString());
            OutcomeLabel = ui.Outcome == OperationsOutcomeKind.Victory
                ? ResolveCopy("operations.hud.victory", Language, "VICTORY")
                : ui.Outcome.ToString().ToUpperInvariant();
            ContinueLabel = ResolveCopy(
                string.IsNullOrEmpty(ui.ContinueKey) ? "operations.result.continue" : ui.ContinueKey,
                Language,
                "Continue");
            PhaseLabel = "MissionResult";
            var lines = new string[ui.Deltas.Length];
            for (int index = 0; index < ui.Deltas.Length; index++)
            {
                OperationsDistrictDeltaLine line = ui.Deltas[index];
                string name = ResolveCopy(line.LabelKey, Language, line.Metric.ToString());
                string sign = line.Delta > 0 ? "+" : string.Empty;
                lines[index] = name + "  " + sign + line.Delta.ToString();
            }

            ObjectiveLines = lines;
            if (ui.PracticeAvailable)
            {
                var withPractice = new string[lines.Length + 1];
                System.Array.Copy(lines, withPractice, lines.Length);
                withPractice[lines.Length] = ResolveCopy(ui.PracticeKey, Language, "Practice");
                ObjectiveLines = withPractice;
            }
        }

        void BindCoachAndEscort(OperationsLoopSession loop)
        {
            CoachVisible = false;
            CoachStepLabel = string.Empty;
            CoachTitle = string.Empty;
            CoachBody = string.Empty;
            WarningVisible = false;
            WarningLabel = string.Empty;
            ChipLabels = System.Array.Empty<string>();
            if (loop == null)
                return;

            if (MissionId == OperationsOnboardingCoach.MissionId)
            {
                var coach = new OperationsOnboardingCoach();
                if (coach.TryRead(loop, out OperationsCoachFrame frame) &&
                    frame.Step != OperationsCoachStepKind.None &&
                    frame.Step != OperationsCoachStepKind.Done)
                {
                    CoachVisible = true;
                    CoachStepLabel = frame.StepIndex.ToString() + "/" + frame.StepCount.ToString();
                    CoachTitle = ResolveCopy(frame.TitleKey, Language, frame.Step.ToString());
                    CoachBody = ResolveCopy(frame.BodyKey, Language, string.Empty);
                }
            }

            if (!OperationsEscortRepairControls.TryRead(loop, out OperationsEscortRepairControlFrame controls))
                return;

            WarningVisible = controls.WarningVisible;
            WarningLabel = ResolveCopy(controls.WarningKey, Language, string.Empty);
            OperationsFatThumbChip[] chips = controls.Chips ?? System.Array.Empty<OperationsFatThumbChip>();
            var labels = new string[chips.Length];
            for (int index = 0; index < chips.Length; index++)
            {
                string label = ResolveCopy(chips[index].LabelKey, Language, chips[index].Kind.ToString());
                labels[index] = chips[index].Enabled ? label : label + " ·";
            }

            ChipLabels = labels;
        }

        public void ShowVictory(OperationsLoopSession loop, OperationsAriaEvidenceRecord record)
        {
            if (_world != null)
                _world.Clear();

            CoachVisible = false;
            WarningVisible = false;
            ChipLabels = System.Array.Empty<string>();
            ResultDeltasBound = false;
            ContinueLabel = string.Empty;

            if (record == null)
            {
                VictoryMode = true;
                ShowDebugChrome = false;
                Title = "Victory";
                Body = "Mission complete.";
                OutcomeLabel = ResolveCopy("operations.hud.victory", Language, "VICTORY");
                ObjectiveLines = System.Array.Empty<string>();
                Detail = string.Empty;
                PressureLine = string.Empty;
                TimerLine = string.Empty;
                return;
            }

            VictoryMode = true;
            ShowDebugChrome = false;
            MissionId = record.MissionId ?? string.Empty;
            Language = string.IsNullOrEmpty(record.Language) ? "en" : record.Language;
            Seed = record.Seed;
            PhaseLabel = "MissionResult";
            string slug = MissionSlug(MissionId);
            Title = ResolveCopy("operations." + slug + ".title", Language, MissionId);
            Body = ResolveCopy("operations." + slug + ".result.victory", Language, "Victory.");
            OutcomeLabel = ResolveCopy("operations.hud.victory", Language, "VICTORY");
            Tick = record.ObjectiveCompletionTick;
            Credits = record.ReceivedCredits;
            Xp = record.ReceivedXp;
            PressureLine = string.Empty;
            TimerLine = string.Empty;
            Detail = string.Empty;
            ObjectiveLines = new[]
            {
                ResolveCopy("operations.hud.reward_credits", Language, "Credits") + ": +" + Credits,
                ResolveCopy("operations.hud.reward_xp", Language, "Commander XP") + ": +" + Xp
            };

            // Keep developer diagnostics available in Editor logs only — not player chrome.
            if (loop != null)
            {
                try
                {
                    string shellTop = loop.ReadShell().Top ?? OperationsShellNames.MissionResult;
                    Debug.Log(
                        "[OperationsAriaPlayModePresentation] victory_debug outcome=" +
                        (record.TerminalOutcome ?? string.Empty) +
                        " reason=" + (record.TerminalReason ?? string.Empty) +
                        " hash=" + (record.ResultHash ?? string.Empty) +
                        " shell=" + shellTop +
                        " runSeed=" + Seed +
                        " completeTick=" + Tick);
                }
                catch
                {
                    // Keep victory presentation even if shell read fails.
                }
            }
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
                ? new Color(0.05f, 0.12f, 0.08f, 1f)
                : new Color(0.12f, 0.16f, 0.14f, 1f);
            Color[] pixels = new Color[width * height];
            for (int index = 0; index < pixels.Length; index++)
                pixels[index] = fill;
            texture.SetPixels(pixels);

            // Phone-mock card band so evidence PNGs read as player-facing chrome.
            Color card = VictoryMode
                ? new Color(0.12f, 0.28f, 0.18f)
                : new Color(0.14f, 0.2f, 0.24f);
            int left = width / 8;
            int right = width - width / 8;
            int top = height / 6;
            int bottom = height - height / 5;
            for (int y = top; y < bottom; y++)
            {
                for (int x = left; x < right; x++)
                    texture.SetPixel(x, y, card);
            }

            Color accent = VictoryMode ? new Color(0.4f, 0.82f, 0.5f) : new Color(0.45f, 0.7f, 0.95f);
            for (int y = top; y < top + 18 && y < height; y++)
            {
                for (int x = left; x < right; x++)
                    texture.SetPixel(x, y, accent);
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

            GUIStyle labelSkin = GUI.skin != null ? GUI.skin.label : null;
            if (VictoryMode)
                DrawVictoryCard(labelSkin);
            else
                DrawActiveChrome(labelSkin);
        }

        void DrawActiveChrome(GUIStyle labelSkin)
        {
            // Phone-mock side panels only — do not paint an opaque void over the world.
            Rect panel = PhonePanelRect();
            Color previous = GUI.color;
            GUI.color = new Color(0.05f, 0.08f, 0.1f, 0.72f);
            Texture2D white = Texture2D.whiteTexture;
            if (white != null)
                GUI.DrawTexture(panel, white);
            GUI.color = previous;

            var titleStyle = Style(labelSkin, 26, FontStyle.Bold, Color.white);
            var bodyStyle = Style(labelSkin, 18, FontStyle.Normal, new Color(0.92f, 0.94f, 0.9f));
            var metaStyle = Style(labelSkin, 16, FontStyle.Normal, new Color(0.78f, 0.86f, 0.8f));
            var accentStyle = Style(labelSkin, 17, FontStyle.Bold, new Color(0.95f, 0.78f, 0.35f));

            float x = panel.x + 18f;
            float y = panel.y + 16f;
            float width = panel.width - 36f;
            GUI.Label(new Rect(x, y, width, 28f), ResolveCopy("operations.hud.shell", Language, "OPERATIONS"), metaStyle);
            y += 28f;
            GUI.Label(new Rect(x, y, width, 32f), OutcomeLabel ?? string.Empty, titleStyle);
            y += 34f;
            GUI.Label(new Rect(x, y, width, 36f), Title ?? string.Empty, titleStyle);
            y += 40f;
            GUI.Label(new Rect(x, y, width, 52f), Body ?? string.Empty, bodyStyle);
            y += 56f;
            if (!string.IsNullOrEmpty(TimerLine))
            {
                GUI.Label(new Rect(x, y, width, 24f), TimerLine, accentStyle);
                y += 26f;
            }

            GUI.Label(new Rect(x, y, width, 24f), ResolveCopy("operations.hud.objectives", Language, "Objectives"), metaStyle);
            y += 26f;
            for (int index = 0; index < ObjectiveLines.Length && index < 8; index++)
            {
                GUI.Label(new Rect(x, y, width, 24f), ObjectiveLines[index] ?? string.Empty, bodyStyle);
                y += 24f;
            }

            if (CoachVisible)
            {
                y += 8f;
                GUI.Label(new Rect(x, y, width, 22f), "Coach " + (CoachStepLabel ?? string.Empty), accentStyle);
                y += 22f;
                GUI.Label(new Rect(x, y, width, 28f), CoachTitle ?? string.Empty, titleStyle);
                y += 30f;
                GUI.Label(new Rect(x, y, width, 72f), CoachBody ?? string.Empty, bodyStyle);
            }

            if (!string.IsNullOrEmpty(PressureLine))
            {
                y += 10f;
                GUI.Label(new Rect(x, y, width, 48f), PressureLine, accentStyle);
            }

            DrawFatThumbBar(labelSkin);
        }

        void DrawFatThumbBar(GUIStyle labelSkin)
        {
            bool hasChips = ChipLabels != null && ChipLabels.Length > 0;
            if (!WarningVisible && !hasChips)
                return;

            float chipHeight = 64f;
            float warningHeight = WarningVisible ? 40f : 0f;
            float width = Mathf.Clamp(Screen.width * 0.92f, 280f, Screen.width - 24f);
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - chipHeight - warningHeight - 16f;
            Color previous = GUI.color;
            Texture2D white = Texture2D.whiteTexture;
            var chipStyle = Style(labelSkin, 18, FontStyle.Bold, Color.white);
            chipStyle.alignment = TextAnchor.MiddleCenter;

            if (WarningVisible && white != null)
            {
                GUI.color = new Color(0.45f, 0.22f, 0.08f, 0.92f);
                GUI.DrawTexture(new Rect(x, y, width, warningHeight - 4f), white);
                GUI.color = previous;
                var warnStyle = Style(labelSkin, 18, FontStyle.Bold, new Color(1f, 0.86f, 0.45f));
                warnStyle.alignment = TextAnchor.MiddleLeft;
                GUI.Label(new Rect(x + 16f, y, width - 32f, warningHeight - 4f), WarningLabel ?? string.Empty, warnStyle);
                y += warningHeight;
            }

            if (!hasChips || white == null)
                return;

            float gap = 8f;
            float chipWidth = (width - gap * (ChipLabels.Length - 1)) / ChipLabels.Length;
            for (int index = 0; index < ChipLabels.Length; index++)
            {
                Rect chip = new Rect(x + index * (chipWidth + gap), y, chipWidth, chipHeight);
                GUI.color = new Color(0.12f, 0.28f, 0.36f, 0.94f);
                GUI.DrawTexture(chip, white);
                GUI.color = previous;
                GUI.Label(chip, ChipLabels[index] ?? string.Empty, chipStyle);
            }
        }

        void DrawVictoryCard(GUIStyle labelSkin)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.04f, 0.1f, 0.07f, 0.82f);
            Texture2D white = Texture2D.whiteTexture;
            if (white != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white);
            GUI.color = previous;

            Rect card = PhonePanelRect();
            card.height = Mathf.Min(card.height, Screen.height * 0.72f);
            card.y = (Screen.height - card.height) * 0.5f;
            GUI.color = new Color(0.08f, 0.18f, 0.12f, 0.94f);
            if (white != null)
                GUI.DrawTexture(card, white);
            GUI.color = previous;

            var titleStyle = Style(labelSkin, 34, FontStyle.Bold, new Color(0.55f, 0.95f, 0.65f));
            var headingStyle = Style(labelSkin, 28, FontStyle.Bold, Color.white);
            var bodyStyle = Style(labelSkin, 20, FontStyle.Normal, new Color(0.92f, 0.95f, 0.9f));
            var rewardStyle = Style(labelSkin, 18, FontStyle.Bold, new Color(0.95f, 0.85f, 0.4f));

            float x = card.x + 28f;
            float y = card.y + 28f;
            float width = card.width - 56f;
            GUI.Label(new Rect(x, y, width, 28f), ResolveCopy("operations.hud.shell", Language, "OPERATIONS"), bodyStyle);
            y += 36f;
            GUI.Label(new Rect(x, y, width, 40f), OutcomeLabel ?? string.Empty, titleStyle);
            y += 48f;
            GUI.Label(new Rect(x, y, width, 40f), Title ?? string.Empty, headingStyle);
            y += 48f;
            GUI.Label(new Rect(x, y, width, 96f), Body ?? string.Empty, bodyStyle);
            y += 110f;
            for (int index = 0; index < ObjectiveLines.Length && index < 6; index++)
            {
                GUI.Label(new Rect(x, y, width, 28f), ObjectiveLines[index] ?? string.Empty, rewardStyle);
                y += 30f;
            }

            y = card.yMax - 64f;
            string continueText = ResultDeltasBound && !string.IsNullOrEmpty(ContinueLabel)
                ? ContinueLabel
                : ResolveCopy("operations.hud.continue", Language, "Tap Continue to return to Operations");
            if (ResultDeltasBound && white != null)
            {
                GUI.color = new Color(0.2f, 0.45f, 0.28f, 0.98f);
                GUI.DrawTexture(new Rect(x, y, width, 40f), white);
                GUI.color = previous;
                var continueStyle = Style(labelSkin, 20, FontStyle.Bold, Color.white);
                continueStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(x, y, width, 40f), continueText, continueStyle);
            }
            else
            {
                GUI.Label(new Rect(x, y, width, 32f), continueText, bodyStyle);
            }
        }

        static Rect PhonePanelRect()
        {
            float width = Mathf.Clamp(Screen.width * 0.36f, 280f, 420f);
            float height = Mathf.Clamp(Screen.height * 0.88f, 420f, Screen.height - 24f);
            float x = 18f;
            float y = (Screen.height - height) * 0.5f;
            return new Rect(x, y, width, height);
        }

        static GUIStyle Style(GUIStyle labelSkin, int size, FontStyle font, Color color)
        {
            var style = labelSkin != null ? new GUIStyle(labelSkin) : new GUIStyle();
            style.fontSize = size;
            style.fontStyle = font;
            style.alignment = TextAnchor.UpperLeft;
            style.wordWrap = true;
            style.normal.textColor = color;
            return style;
        }

        static string[] BuildLocalizedObjectives(
            OperationsLoopSession loop,
            OperationsHudFrame hud,
            string language,
            string missionSlug)
        {
            OperationsHudObjective[] required = hud.Required ?? System.Array.Empty<OperationsHudObjective>();
            var lines = new string[required.Length];
            for (int index = 0; index < required.Length; index++)
            {
                OperationsHudObjective row = required[index];
                string nodeId = row.NodeId ?? string.Empty;
                string label = ResolveObjectiveLabel(nodeId, language, missionSlug);
                string status = row.Complete
                    ? ResolveCopy("operations.hud.done", language, "done")
                    : row.Failed
                        ? ResolveCopy("operations.hud.failed", language, "failed")
                        : ResolveCopy("operations.hud.active", language, "active");

                string progress = string.Empty;
                if (loop.TryNode(nodeId, out OperationsTacticalNodeState state) &&
                    state.TargetCount > 1)
                {
                    int current = state.Phase == OperationsTacticalNodePhase.Complete
                        ? state.TargetCount
                        : state.ProgressCount;
                    progress = " " + current + "/" + state.TargetCount;
                }
                else if (row.Progress > 0 && !row.Complete)
                {
                    progress = " (" + row.Progress + ")";
                }

                lines[index] = "• " + label + progress + " — " + status;
            }

            return lines;
        }

        static string BuildPressureLine(OperationsLoopSession loop, OperationsHudFrame hud, string language)
        {
            OperationsTacticalActorState[] actors = loop.CopyPublicActors();
            for (int index = 0; index < actors.Length; index++)
            {
                OperationsTacticalActorState actor = actors[index];
                if (!actor.Alive || !actor.Spawned || actor.ChannelTicks <= 0)
                    continue;
                string action = ResolveCopy("operations.hud.channeling", language, "Working");
                int remaining = EstimateChannelRemaining(actor.ChannelTicks);
                return action + "… " +
                       ResolveCopy("operations.hud.pressure", language, "Stay on target") +
                       " (" + remaining + "s)";
            }

            for (int index = 0; index < hud.Required.Length; index++)
            {
                OperationsHudObjective row = hud.Required[index];
                if (row.Complete || row.Failed)
                    continue;
                if (!loop.TryNode(row.NodeId, out OperationsTacticalNodeState state))
                    continue;
                if (state.Rule == OperationsObjectiveRuleKind.Hold &&
                    state.Phase == OperationsTacticalNodePhase.Active)
                {
                    return ResolveCopy("operations.hud.hold_refresh", language, "Re-confirm Hold — do not AFK");
                }
            }

            if (hud.TimerVisible && hud.TimerRemaining <= 120)
                return ResolveCopy("operations.hud.deadline_pressure", language, "Deadline pressure — decide now");

            return string.Empty;
        }

        static int EstimateChannelRemaining(int channelTicks)
        {
            int scanLeft = OperationsTacticalRules.ScanSeconds - channelTicks;
            if (scanLeft > 0 && scanLeft <= OperationsTacticalRules.ScanSeconds)
                return scanLeft;
            int repairLeft = OperationsTacticalRules.RepairSeconds - channelTicks;
            if (repairLeft > 0 && repairLeft <= OperationsTacticalRules.RepairSeconds)
                return repairLeft;
            int interactLeft = OperationsTacticalRules.InteractSeconds - channelTicks;
            if (interactLeft > 0)
                return interactLeft;
            return Mathf.Max(1, OperationsTacticalRules.ScanSeconds - channelTicks);
        }

        static string ResolveObjectiveLabel(string nodeId, string language, string missionSlug)
        {
            string keyed = "operations." + missionSlug + ".objective." + nodeId;
            if (OperationsLocalizedCopy.TryGet(keyed, language, out string missionValue) &&
                !string.IsNullOrEmpty(missionValue))
                return missionValue;
            string shared = "operations.objective." + nodeId;
            if (OperationsLocalizedCopy.TryGet(shared, language, out string sharedValue) &&
                !string.IsNullOrEmpty(sharedValue))
                return sharedValue;
            return HumanizeNodeId(nodeId);
        }

        static string HumanizeNodeId(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
                return "Objective";
            return nodeId.Replace('_', ' ');
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
