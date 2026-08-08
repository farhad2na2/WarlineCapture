using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal sealed class MenuDiagnosticsUiSystemHelper
    {
        private const int MaxVisibleLogEntries = 50;
        private const double FpsUiUpdateIntervalSeconds = 0.25d;

        private readonly Queue<RuntimeLogEntry> runtimeLogEntries = new(MaxVisibleLogEntries);
        private readonly StringBuilder runtimeLogBuilder = new(8192);
        private bool initialized;
        private bool runtimeLogSubscribed;
        private bool runtimeLogBufferReplayed;
        private double fpsAccumulatedSeconds;
        private int fpsFrames;
        private MenuDiagnosticsView currentView;

        private readonly struct RuntimeLogEntry
        {
            public readonly string Message;
            public readonly LogType Type;

            public RuntimeLogEntry(string message, LogType type)
            {
                Message = message ?? string.Empty;
                Type = type;
            }
        }

        public void Initialize(MenuDiagnosticsView view)
        {
            if (view == null || initialized)
                return;

            initialized = true;
            currentView = view;
            ConfigureView(view);
            BindButtons(view);
            SubscribeRuntimeLog();
        }

        public void Update(MenuDiagnosticsView view, float unscaledDeltaTime)
        {
            if (view == null)
                return;

            if (UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel shellState))
                SuppressRuntimeLogPanelForRoute(view.LogPanel, shellState.ActiveRoute);

            fpsFrames++;
            fpsAccumulatedSeconds += Mathf.Max(0f, unscaledDeltaTime);
            if (fpsAccumulatedSeconds < FpsUiUpdateIntervalSeconds)
                return;

            double fps = fpsAccumulatedSeconds > 0d ? fpsFrames / fpsAccumulatedSeconds : 0d;
            SetFpsText(view, Mathf.RoundToInt((float)fps));
            fpsFrames = 0;
            fpsAccumulatedSeconds = 0d;
        }

        public void Shutdown(MenuDiagnosticsView view)
        {
            if (!initialized)
                return;

            UnbindButtons(view);
            UnsubscribeRuntimeLog();
            initialized = false;
            currentView = null;
            fpsFrames = 0;
            fpsAccumulatedSeconds = 0d;
        }

        private void ConfigureView(MenuDiagnosticsView view)
        {
            if (view.LogPanel != null)
                view.LogPanel.SetActive(false);

            if (view.LogText != null)
            {
                view.LogText.richText = true;
                view.LogText.textWrappingMode = TextWrappingModes.Normal;
                view.LogText.alignment = TextAlignmentOptions.TopLeft;
                view.LogText.overflowMode = TextOverflowModes.Overflow;
            }

            if (view.LogScrollRect != null)
            {
                view.LogScrollRect.horizontal = false;
                view.LogScrollRect.vertical = true;
                view.LogScrollRect.movementType = ScrollRect.MovementType.Clamped;
            }

            SetFpsText(view, 0);
            ReplayBufferedRuntimeLogs();
            RefreshRuntimeLogLabel(view, false);
        }

        private void BindButtons(MenuDiagnosticsView view)
        {
            if (view.FpsButton != null)
                view.FpsButton.onClick.AddListener(ToggleRuntimeLogPanel);
            if (view.CloseButton != null)
                view.CloseButton.onClick.AddListener(HideRuntimeLogPanel);
        }

        private void UnbindButtons(MenuDiagnosticsView view)
        {
            if (view == null)
                return;

            if (view.FpsButton != null)
                view.FpsButton.onClick.RemoveListener(ToggleRuntimeLogPanel);
            if (view.CloseButton != null)
                view.CloseButton.onClick.RemoveListener(HideRuntimeLogPanel);
        }

        private void SubscribeRuntimeLog()
        {
            if (runtimeLogSubscribed)
                return;

            Application.logMessageReceived += HandleRuntimeLogMessage;
            runtimeLogSubscribed = true;
        }

        private void UnsubscribeRuntimeLog()
        {
            if (!runtimeLogSubscribed)
                return;

            Application.logMessageReceived -= HandleRuntimeLogMessage;
            runtimeLogSubscribed = false;
        }

        private void HandleRuntimeLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log && string.IsNullOrWhiteSpace(condition))
                return;

            AddRuntimeLogEntry(BuildRuntimeLogMessage(condition, stackTrace, type), type);
        }

        private void ReplayBufferedRuntimeLogs()
        {
            if (runtimeLogBufferReplayed)
                return;

            runtimeLogBufferReplayed = true;
            IReadOnlyList<RuntimeLogBuffer.Entry> snapshot = RuntimeLogBuffer.Snapshot();
            for (int i = 0; i < snapshot.Count; i++)
            {
                RuntimeLogBuffer.Entry entry = snapshot[i];
                if (entry.Type == LogType.Log && string.IsNullOrWhiteSpace(entry.Condition))
                    continue;

                AddRuntimeLogEntry(BuildRuntimeLogMessage(entry.Condition, entry.StackTrace, entry.Type), entry.Type);
            }
        }

        private void AddRuntimeLogEntry(string message, LogType type)
        {
            while (runtimeLogEntries.Count >= MaxVisibleLogEntries)
                runtimeLogEntries.Dequeue();

            runtimeLogEntries.Enqueue(new RuntimeLogEntry(message, type));
        }

        private void ToggleRuntimeLogPanel()
        {
            MenuDiagnosticsView view = currentView;
            if (view == null || view.LogPanel == null)
                return;

            if (UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel shellState) &&
                SuppressRuntimeLogPanelForRoute(view.LogPanel, shellState.ActiveRoute))
            {
                return;
            }

            bool visible = !view.LogPanel.activeSelf;
            view.LogPanel.SetActive(visible);
            RefreshRuntimeLogLabel(view, visible);
        }

        internal static bool SuppressRuntimeLogPanelForRoute(GameObject logPanel, UIRoute route)
        {
            if (route != UIRoute.Match)
                return false;

            if (logPanel != null && logPanel.activeSelf)
                logPanel.SetActive(false);

            return true;
        }

        private void HideRuntimeLogPanel()
        {
            MenuDiagnosticsView view = currentView;
            if (view == null || view.LogPanel == null)
                return;

            view.LogPanel.SetActive(false);
        }

        private void SetFpsText(MenuDiagnosticsView view, int fps)
        {
            if (view.FpsText != null)
                view.FpsText.text = DiagnosticsFpsText.Get(fps);
        }

        private void RefreshRuntimeLogLabel(MenuDiagnosticsView view, bool scrollToBottom)
        {
            if (view == null || view.LogText == null)
                return;

            runtimeLogBuilder.Clear();
            foreach (RuntimeLogEntry entry in runtimeLogEntries)
            {
                if (runtimeLogBuilder.Length > 0)
                    runtimeLogBuilder.Append('\n').Append('\n');

                string color = GetLogColor(entry.Type);
                if (!string.IsNullOrEmpty(color))
                    runtimeLogBuilder.Append("<color=").Append(color).Append('>');
                runtimeLogBuilder.Append(entry.Message);
                if (!string.IsNullOrEmpty(color))
                    runtimeLogBuilder.Append("</color>");
            }

            view.LogText.text = runtimeLogBuilder.ToString();
            ResizeRuntimeLogContent(view);
            if (scrollToBottom)
                ScrollRuntimeLogToBottom(view);
        }

        private static string BuildRuntimeLogMessage(string condition, string stackTrace, LogType type)
        {
            bool includeStackTrace = type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
            if (!includeStackTrace || string.IsNullOrWhiteSpace(stackTrace))
                return condition ?? string.Empty;

            return $"{condition}\n<size=70%>{stackTrace}</size>";
        }

        private static void ResizeRuntimeLogContent(MenuDiagnosticsView view)
        {
            if (view.LogText == null || view.LogScrollRect == null)
                return;

            RectTransform content = view.LogScrollRect.content;
            RectTransform viewport = view.LogScrollRect.viewport != null
                ? view.LogScrollRect.viewport
                : view.LogScrollRect.GetComponent<RectTransform>();
            if (content == null || viewport == null)
                return;

            RectTransform textRect = view.LogText.rectTransform;
            float textWidth = Mathf.Max(1f, textRect.rect.width);
            Vector2 preferred = view.LogText.GetPreferredValues(view.LogText.text, textWidth, 0f);
            float contentHeight = Mathf.Max(viewport.rect.height, preferred.y + 32f);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(1f, preferred.y));
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private static void ScrollRuntimeLogToBottom(MenuDiagnosticsView view)
        {
            if (view.LogScrollRect == null)
                return;

            view.LogScrollRect.StopMovement();
            view.LogScrollRect.verticalNormalizedPosition = 0f;
        }

        private static string GetLogColor(LogType type)
        {
            return type switch
            {
                LogType.Warning => "#FFA500",
                LogType.Error or LogType.Assert or LogType.Exception => "#FF4040",
                _ => "#FFFFFF"
            };
        }
    }
}
