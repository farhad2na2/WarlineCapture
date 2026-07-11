using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class NarrativeSkipConfirmationView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text accessibleLabel;

        private Action confirmHandler;
        private Action cancelHandler;
        private bool bound;

        private void Awake() => EnsureBindings();
        private void OnDestroy()
        {
            if (bound)
            {
                confirmButton?.onClick.RemoveListener(HandleConfirm);
                cancelButton?.onClick.RemoveListener(HandleCancel);
            }
            confirmHandler = null;
            cancelHandler = null;
        }

        public void Bind(Action onConfirm, Action onCancel)
        {
            EnsureBindings();
            confirmHandler = onConfirm;
            cancelHandler = onCancel;
        }

        public void Unbind()
        {
            confirmHandler = null;
            cancelHandler = null;
        }

        public void SetVisible(bool visible)
        {
            if (group == null)
                return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        public void SetAccessibleLabel(string value)
        {
            if (accessibleLabel != null)
                accessibleLabel.text = string.IsNullOrWhiteSpace(value) ? "Confirm skip to gameplay" : value;
        }

        private void EnsureBindings()
        {
            if (bound)
                return;
            confirmButton?.onClick.AddListener(HandleConfirm);
            cancelButton?.onClick.AddListener(HandleCancel);
            bound = true;
        }

        private void HandleConfirm() => confirmHandler?.Invoke();
        private void HandleCancel() => cancelHandler?.Invoke();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (group == null || confirmButton == null || cancelButton == null || accessibleLabel == null)
                Debug.LogWarning($"[{nameof(NarrativeSkipConfirmationView)}] Missing required serialized reference on {name}.", this);
        }
#endif
    }
}
