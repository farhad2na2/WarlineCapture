using System;
using Game.Narrative.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;

namespace Game.Composition
{
    internal sealed class FirstLaunchNarrativeInteractivePresentationSystemHelper
    {
        private const string FallbackCallsign = "COMMANDER";
        private const string FallbackDisplayName = "Commander";

        private readonly NarrativeCommanderIdentityView commanderView;
        private readonly NarrativeGuidanceChoiceView guidanceView;
        private Action<NarrativeUiAction> actionHandler;
        private string sequenceId = string.Empty;
        private string stateId = string.Empty;
        private ulong transitionToken;
        private bool commitRequested;

        public FirstLaunchNarrativeInteractivePresentationSystemHelper(
            NarrativeCommanderIdentityView commanderIdentityView,
            NarrativeGuidanceChoiceView guidanceChoiceView)
        {
            commanderView = commanderIdentityView;
            guidanceView = guidanceChoiceView;
        }

        public NarrativeCommanderIdentityData SelectedIdentity => new()
        {
            Callsign = Normalize(commanderView != null ? commanderView.CallsignText : null, FallbackCallsign),
            DisplayName = Normalize(commanderView != null ? commanderView.DisplayNameText : null, FallbackDisplayName)
        };

        public int SelectedPortraitIndex => commanderView != null ? commanderView.SelectedPortraitIndex : -1;
        public NarrativeGuidanceMode SelectedGuidance => guidanceView != null
            ? guidanceView.SelectedGuidance
            : NarrativeGuidanceMode.Full;
        public bool CommitRequested => commitRequested;

        public void Bind(Action<NarrativeUiAction> handler)
        {
            actionHandler = handler;
            commanderView?.BindIntents(HandlePortraitSelected, HandleCommanderContinue);
            guidanceView?.BindIntents(HandleGuidanceSelected, HandleGuidanceContinue);
            commanderView?.SetAccessibilityLabels(
                "Commander callsign",
                "Commander display name",
                "Continue with commander identity",
                CreatePortraitLabels(commanderView.PortraitOptionCount));
            guidanceView?.SetAccessibilityLabels(
                "Full guidance",
                "Contextual guidance",
                "Minimal guidance",
                "Continue with selected guidance");
        }

        public void Unbind()
        {
            commanderView?.UnbindIntents();
            guidanceView?.UnbindIntents();
            actionHandler = null;
            ClearContext();
        }

        public void Enter(string nextSequenceId, string nextStateId, ulong nextTransitionToken)
        {
            sequenceId = nextSequenceId ?? string.Empty;
            stateId = nextStateId ?? string.Empty;
            transitionToken = nextTransitionToken;
            commitRequested = false;
            commanderView?.SetControlsInteractable(true);
            guidanceView?.SetControlsInteractable(true);
        }

        public void ApplyCommanderIdentity(in NarrativeCommanderIdentityData identity, int portraitIndex)
        {
            commanderView?.SetIdentity(
                Normalize(identity.Callsign, FallbackCallsign),
                Normalize(identity.DisplayName, FallbackDisplayName),
                portraitIndex);
            commanderView?.SetControlsInteractable(!commitRequested);
        }

        public void ApplyGuidance(NarrativeGuidanceMode guidance)
        {
            guidanceView?.SetSelectedGuidance(IsSupported(guidance) ? guidance : NarrativeGuidanceMode.Full);
            guidanceView?.SetControlsInteractable(!commitRequested);
        }

        private void HandlePortraitSelected(int portraitIndex)
        {
            if (!commitRequested)
                commanderView?.SetPortraitSelection(portraitIndex);
        }

        private void HandleGuidanceSelected(NarrativeGuidanceMode guidance)
        {
            if (!commitRequested && IsSupported(guidance))
                guidanceView?.SetSelectedGuidance(guidance);
        }

        private void HandleCommanderContinue()
        {
            EmitCommit(NarrativeUiActionKind.CommitCommanderIdentity, commanderView);
        }

        private void HandleGuidanceContinue()
        {
            EmitCommit(NarrativeUiActionKind.CommitGuidance, guidanceView);
        }

        private void EmitCommit(NarrativeUiActionKind kind, UnityEngine.Object sourceView)
        {
            if (commitRequested || actionHandler == null || sourceView == null)
                return;

            commitRequested = true;
            commanderView?.SetControlsInteractable(false);
            guidanceView?.SetControlsInteractable(false);
            actionHandler.Invoke(new NarrativeUiAction
            {
                SequenceId = sequenceId,
                StateId = stateId,
                LineId = string.Empty,
                Kind = kind,
                TransitionToken = transitionToken
            });
        }

        private void ClearContext()
        {
            sequenceId = string.Empty;
            stateId = string.Empty;
            transitionToken = 0UL;
            commitRequested = false;
        }

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static bool IsSupported(NarrativeGuidanceMode guidance)
        {
            return guidance == NarrativeGuidanceMode.Full ||
                   guidance == NarrativeGuidanceMode.Contextual ||
                   guidance == NarrativeGuidanceMode.Minimal;
        }

        private static string[] CreatePortraitLabels(int count)
        {
            string[] labels = new string[Math.Max(0, count)];
            for (int i = 0; i < labels.Length; i++)
                labels[i] = $"Commander portrait {i + 1}";
            return labels;
        }
    }
}
