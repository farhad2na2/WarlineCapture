using System;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class NarrativeSequenceView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Image panelImage;
        [SerializeField] private AspectRatioFitter panelAspectFitter;
        [SerializeField] private RectTransform panelMotionRoot;
        [SerializeField] private NarrativeDialogueView dialogueView;
        [SerializeField] private NarrativePlaybackControlsView playbackControls;
        [SerializeField] private NarrativeCommanderIdentityView commanderIdentityView;
        [SerializeField] private NarrativeGuidanceChoiceView guidanceChoiceView;
        [SerializeField] private NarrativeSkipConfirmationView skipConfirmationView;
        [SerializeField] private NarrativeReviewerControlsView reviewerControlsView;
        [SerializeField] private GameObject safeAreaPreview;
        [SerializeField] private AudioSource voiceSource;

        private Action<NarrativeUiAction> actionHandler;
        private string sequenceId;
        private string stateId;
        private string lineId;
        private ulong transitionToken;
        private bool childBindingsInitialized;

        public NarrativeDialogueView DialogueView => dialogueView;
        public AudioSource VoiceSource => voiceSource;
        public RectTransform PanelMotionRoot => panelMotionRoot != null ? panelMotionRoot : panelImage != null ? panelImage.rectTransform : null;
        public NarrativeCommanderIdentityView CommanderIdentityView => commanderIdentityView;
        public NarrativeGuidanceChoiceView GuidanceChoiceView => guidanceChoiceView;
        public NarrativeSkipConfirmationView SkipConfirmationView => skipConfirmationView;
        public NarrativeReviewerControlsView ReviewerControlsView => reviewerControlsView;
        public Sprite CurrentPanelSprite => panelImage != null ? panelImage.sprite : null;

        private void Awake()
        {
            EnsureChildBindings();
        }

        private void OnDestroy()
        {
            if (dialogueView != null)
                dialogueView.UnbindInput();
            if (playbackControls != null)
                playbackControls.UnbindSkip();
            reviewerControlsView?.Unbind();
            actionHandler = null;
        }

        public void BindActions(Action<NarrativeUiAction> handler)
        {
            EnsureChildBindings();
            actionHandler = handler;
        }

        public void UnbindActions()
        {
            actionHandler = null;
        }

        public void SetActionContext(string nextSequenceId, string nextStateId, string nextLineId, ulong nextTransitionToken)
        {
            sequenceId = nextSequenceId;
            stateId = nextStateId;
            lineId = nextLineId;
            transitionToken = nextTransitionToken;
        }

        public void ApplyPanel(in NarrativePanelPresentationModel model)
        {
            stateId = model.StateId;
            if (panelImage != null)
            {
                panelImage.sprite = model.PanelSprite;
                panelImage.color = model.Tint.a <= 0f ? Color.white : model.Tint;
                if (panelAspectFitter != null && model.PanelSprite != null && model.PanelSprite.rect.height > 0f)
                    panelAspectFitter.aspectRatio = model.PanelSprite.rect.width / model.PanelSprite.rect.height;
            }
        }

        public void ClearPanel()
        {
            if (panelImage != null)
                panelImage.sprite = null;
        }

        public void SetSafeAreaPreview(bool visible)
        {
            if (safeAreaPreview != null)
                safeAreaPreview.SetActive(visible);
        }

        public void SetVisible(bool visible)
        {
            if (rootGroup == null)
                return;

            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.interactable = visible;
            rootGroup.blocksRaycasts = visible;
        }

        public void SetSkipState(bool visible, bool interactable, string accessibleLabel)
        {
            playbackControls?.SetSkipState(visible, interactable, accessibleLabel);
        }

        public void SetInteractiveState(NarrativeInteractiveStateKind kind)
        {
            if (commanderIdentityView != null)
                commanderIdentityView.gameObject.SetActive(kind == NarrativeInteractiveStateKind.CommanderIdentity);
            if (guidanceChoiceView != null)
                guidanceChoiceView.gameObject.SetActive(kind == NarrativeInteractiveStateKind.GuidanceChoice);
        }

        private void HandleDialogueInput(NarrativeDialoguePhase phase)
        {
            Emit(phase == NarrativeDialoguePhase.Revealing
                ? NarrativeUiActionKind.CompleteText
                : NarrativeUiActionKind.Continue);
        }

        private void HandleSkip()
        {
            Emit(NarrativeUiActionKind.Skip);
        }

        private void Emit(NarrativeUiActionKind kind)
        {
            actionHandler?.Invoke(new NarrativeUiAction
            {
                SequenceId = sequenceId,
                StateId = stateId,
                LineId = lineId,
                Kind = kind,
                TransitionToken = transitionToken
            });
        }

        private void EnsureChildBindings()
        {
            if (childBindingsInitialized)
                return;
            if (dialogueView != null)
                dialogueView.BindInput(HandleDialogueInput);
            if (playbackControls != null)
                playbackControls.BindSkip(HandleSkip);
            childBindingsInitialized = true;
        }
    }
}
