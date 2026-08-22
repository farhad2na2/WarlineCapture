using System.Collections.Generic;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class MainMenuPlayUI
    {
        private sealed class GuidedHudRuntime
        {
        private readonly Dictionary<Selectable, SelectableState> _selectableStates = new();
        private UIShellContentView _shellContent;
        private MatchOverlayCommandControlsView _recoveredCommandControls;
        private bool _cinematicHudLocked;
        private int _boundContentVersion = -1;
        private int _lockedContentVersion = -1;

        private sealed class SelectableState
        {
            public bool Interactable;
            public CanvasGroup CanvasGroup;
            public float Alpha;
            public bool GroupInteractable;
            public bool BlocksRaycasts;
        }

        internal void Tick(
            MainMenuPlayUI owner,
            MatchOverlayCommandControlsView boundCommandControls)
        {
            RecoverLateCommandControls(owner, boundCommandControls);
            RefreshCinematicInteractionLock(owner);
        }

        internal bool BindShellContent(UIShellContentView shellContent)
        {
            int contentVersion = shellContent != null ? shellContent.ContentVersion : -1;
            if (_shellContent == shellContent && _boundContentVersion == contentVersion)
                return false;

            RestoreCinematicHudInteraction();
            _shellContent = shellContent;
            _boundContentVersion = contentVersion;
            _recoveredCommandControls = null;
            return true;
        }

        internal void Dispose()
        {
            RestoreCinematicHudInteraction();
            _shellContent = null;
            _boundContentVersion = -1;
            _recoveredCommandControls = null;
        }

        private void RecoverLateCommandControls(
            MainMenuPlayUI owner,
            MatchOverlayCommandControlsView boundCommandControls)
        {
            if (owner == null || _shellContent == null || boundCommandControls != null ||
                !_shellContent.TryGetRegionContentRoot(
                    UIShellRegionId.FooterRegion, out RectTransform footerRoot) ||
                footerRoot == null)
            {
                return;
            }

            MatchOverlayCommandControlsView discovered =
                footerRoot.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            if (discovered == null || discovered == _recoveredCommandControls)
                return;

            _recoveredCommandControls = discovered;
            owner.BindMatchHudCommandControls(discovered);
        }

        private void RefreshCinematicInteractionLock(MainMenuPlayUI owner)
        {
            bool shouldLock =
                UiShellRuntimeGateway.TryReadMissionHudRestrictions(
                    out UiMissionHudRestrictionsModel restrictions) &&
                restrictions.CinematicInteractionLocked;
            if (!shouldLock)
            {
                RestoreCinematicHudInteraction();
                return;
            }

            if (_shellContent == null)
                return;
            if (!_cinematicHudLocked || _lockedContentVersion != _shellContent.ContentVersion)
            {
                RestoreCinematicHudInteraction();
                owner?._matchHudAssistantUiSystem.SuspendForCinematic();
                owner?._matchHudSquadTrayView?.ClearActiveSlot();
                CaptureAndDisableMatchHudSelectables();
                _cinematicHudLocked = true;
                _lockedContentVersion = _shellContent.ContentVersion;
            }
            ReapplyCinematicHudInteractionLock();
        }

        private void CaptureAndDisableMatchHudSelectables()
        {
            CaptureAndDisableRegion(UIShellRegionId.HeaderRegion);
            CaptureAndDisableRegion(UIShellRegionId.LeftRegion);
            CaptureAndDisableRegion(UIShellRegionId.RightRegion);
            CaptureAndDisableRegion(UIShellRegionId.FooterRegion);
        }

        private void CaptureAndDisableRegion(UIShellRegionId regionId)
        {
            if (!_shellContent.TryGetRegionContentRoot(regionId, out RectTransform root) || root == null)
                return;

            foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                if (selectable == null || _selectableStates.ContainsKey(selectable))
                    continue;

                CanvasGroup group = selectable.GetComponent<CanvasGroup>();
                if (group == null)
                    group = selectable.gameObject.AddComponent<CanvasGroup>();
                _selectableStates.Add(selectable, new SelectableState
                {
                    Interactable = selectable.interactable,
                    CanvasGroup = group,
                    Alpha = group.alpha,
                    GroupInteractable = group.interactable,
                    BlocksRaycasts = group.blocksRaycasts
                });
                ApplyDisabledState(selectable, group);
            }
        }

        private void ReapplyCinematicHudInteractionLock()
        {
            foreach (KeyValuePair<Selectable, SelectableState> entry in _selectableStates)
            {
                Selectable selectable = entry.Key;
                SelectableState saved = entry.Value;
                if (selectable != null)
                    ApplyDisabledState(selectable, saved.CanvasGroup);
            }
        }

        private static void ApplyDisabledState(Selectable selectable, CanvasGroup group)
        {
            UiDisabledMaterialUtility.SetSelectableDisabled(
                selectable, UiDisabledVisualReason.CinematicInteractionLock, true);
            UiDisabledMaterialUtility.SetDisabled(
                selectable.gameObject, UiDisabledVisualReason.CinematicInteractionLock, true);
            selectable.interactable = false;
            if (group == null)
                return;

            // Preserve authored opacity; the disabled material owns grayscale presentation.
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private void RestoreCinematicHudInteraction()
        {
            if (!_cinematicHudLocked && _selectableStates.Count == 0)
                return;

            foreach (KeyValuePair<Selectable, SelectableState> entry in _selectableStates)
            {
                Selectable selectable = entry.Key;
                SelectableState saved = entry.Value;
                if (selectable != null)
                {
                    UiDisabledMaterialUtility.SetDisabled(
                        selectable.gameObject, UiDisabledVisualReason.CinematicInteractionLock, false);
                    UiDisabledMaterialUtility.SetSelectableDisabled(
                        selectable, UiDisabledVisualReason.CinematicInteractionLock, false);
                    selectable.interactable = saved.Interactable;
                }
                if (saved.CanvasGroup == null)
                    continue;

                saved.CanvasGroup.alpha = saved.Alpha;
                saved.CanvasGroup.interactable = saved.GroupInteractable;
                saved.CanvasGroup.blocksRaycasts = saved.BlocksRaycasts;
            }

            _selectableStates.Clear();
            _cinematicHudLocked = false;
            _lockedContentVersion = -1;
        }
        }
    }
}
