using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.UI.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Composition
{
    internal sealed class FirstLaunchNarrativePanelPresentationSystemHelper
    {
        private readonly NarrativePanelAssetResidencyPresentationSystemHelper residency = new();
        private IReadOnlyDictionary<string, NarrativeStateRecord> states;
        private NarrativeSequenceView view;
        private NarrativeStateRecord activeState;
        private ulong activeToken;

        public int ResidentAssetCount => residency.ResidentAssetCount;
        public string CurrentKey => residency.CurrentKey;
        public string NextKey => residency.NextKey;

        public void Initialize(
            NarrativeSequenceView sequenceView,
            IReadOnlyDictionary<string, NarrativeStateRecord> stateLookup)
        {
            residency.CurrentReady -= HandleCurrentReady;
            residency.CurrentFailed -= HandleCurrentFailed;
            view = sequenceView;
            states = stateLookup;
            residency.CurrentReady += HandleCurrentReady;
            residency.CurrentFailed += HandleCurrentFailed;
        }

        public void Present(NarrativeStateRecord state, ulong transitionToken)
        {
            activeState = state;
            activeToken = transitionToken;
            AssetReferenceSprite currentReference = ResolvePanelReference(state);
            AssetReferenceSprite nextReference = ResolvePanelReference(FindNextPanelState(state));
            Sprite direct = ResolveDirectPanel(state);
            Sprite panel = !IsReferenceValid(currentReference)
                ? residency.KeepCurrentAndPrepareNext(nextReference, transitionToken)
                : residency.RequestCurrentAndPrepareNext(
                    currentReference,
                    nextReference,
                    transitionToken,
                    direct);
            if (panel != null)
                ApplyPanel(state, panel);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else if (!IsReferenceValid(currentReference) && direct == null && RequiresPanel(state))
                Debug.LogError($"[FirstLaunchPanelPresentation] Missing panel for state '{state.StateId}'.");
#endif
        }

        public void Clear()
        {
            activeState = null;
            activeToken = 0;
            residency.ReleaseAll();
        }

        private void HandleCurrentReady(ulong transitionToken, Sprite panel)
        {
            if (transitionToken != activeToken || activeState == null || panel == null)
                return;
            ApplyPanel(activeState, panel);
        }

        private void HandleCurrentFailed(ulong transitionToken)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (transitionToken == activeToken && RequiresPanel(activeState) && ResolveDirectPanel(activeState) == null)
                Debug.LogError($"[FirstLaunchPanelPresentation] Panel load failed for state '{activeState.StateId}'.");
#endif
        }

        private void ApplyPanel(NarrativeStateRecord state, Sprite panel)
        {
            view?.ApplyPanel(new NarrativePanelPresentationModel
            {
                StateId = state.StateId,
                PanelSprite = panel,
                Tint = Color.white
            });
        }

        private NarrativeStateRecord FindNextPanelState(NarrativeStateRecord state)
        {
            string nextId = state?.ContinueStateId;
            for (int i = 0; i < states.Count && !string.IsNullOrEmpty(nextId); i++)
            {
                if (!states.TryGetValue(nextId, out NarrativeStateRecord candidate))
                    return null;
                if (IsReferenceValid(ResolvePanelReference(candidate)) || ResolveDirectPanel(candidate) != null)
                    return candidate;
                nextId = candidate.ContinueStateId;
            }
            return null;
        }

        private static AssetReferenceSprite ResolvePanelReference(NarrativeStateRecord state)
        {
            if (state == null)
                return null;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            if (aspect >= 2f && IsReferenceValid(state.Panel20x9Reference))
                return state.Panel20x9Reference;
            return IsReferenceValid(state.Panel16x9Reference)
                ? state.Panel16x9Reference
                : state.Panel20x9Reference;
        }

        private static Sprite ResolveDirectPanel(NarrativeStateRecord state)
        {
            if (state == null)
                return null;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            return aspect >= 2f && state.Panel20x9 != null ? state.Panel20x9 : state.Panel16x9;
        }

        private static bool IsReferenceValid(AssetReferenceSprite reference) =>
            reference != null && reference.RuntimeKeyIsValid();

        private static bool RequiresPanel(NarrativeStateRecord state) =>
            state != null && (state.Kind == NarrativeStateKind.PanelDialogue ||
                              state.Kind == NarrativeStateKind.InteractiveIdentity);
    }
}
