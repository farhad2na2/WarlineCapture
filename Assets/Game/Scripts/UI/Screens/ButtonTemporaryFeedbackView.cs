using TMPro;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonTemporaryFeedbackView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string feedbackText;
        [SerializeField, Min(.25f)] private float duration = 1.8f;
        private Button _button;
        private string _defaultText;
        private CancellationTokenSource restoreCancellation;

        public void Configure(TMP_Text configuredLabel, string configuredFeedbackText, float configuredDuration = 1.8f)
        {
            label = configuredLabel;
            feedbackText = configuredFeedbackText;
            duration = Mathf.Max(.25f, configuredDuration);
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            _defaultText = label != null ? label.text : string.Empty;
            _button.onClick.AddListener(ShowFeedback);
        }



        private async void RestoreAfterDelay()
        {
            restoreCancellation?.Cancel();
            using var pending = new CancellationTokenSource();
            restoreCancellation = pending;
            try
            {
                float deadline = Time.unscaledTime + duration;
                while (Time.unscaledTime < deadline)
                    await Awaitable.NextFrameAsync(pending.Token);
                if(label != null) label.text = _defaultText;
            }
            catch(OperationCanceledException) { }
            finally { if(ReferenceEquals(restoreCancellation, pending)) restoreCancellation = null; }
        }

        private void OnDisable()
        {
            restoreCancellation?.Cancel();
            if(label != null && !string.IsNullOrEmpty(_defaultText)) label.text = _defaultText;
        }

        private void OnDestroy()
        {
            _button?.onClick.RemoveListener(ShowFeedback);
        }

        public void ShowFeedback()
        {
            if (label == null)
                return;
            if (string.IsNullOrEmpty(_defaultText))
                _defaultText = label.text;
            label.text = feedbackText;
            RestoreAfterDelay();
        }
    }
}
