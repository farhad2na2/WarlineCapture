using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class EndOfDayReportPopupView : MonoBehaviour
    {
        [SerializeField] private UIPopupFrameView popupFrame;
        [SerializeField] private Button viewOperationsButton;
        [SerializeField] private Button saveContinueButton;

        private Action _viewOperationsRequested;
        private Action _saveContinueRequested;
        private bool _eventsBound;

        public Button ViewOperationsButton => viewOperationsButton;
        public Button SaveContinueButton => saveContinueButton;

        private void Awake() => BindUnityEvents();

        private void OnDestroy()
        {
            RemoveUnityEvents();
            ClearActions();
        }

        public void BindActions(Action viewOperationsRequested, Action saveContinueRequested)
        {
            BindUnityEvents();
            _viewOperationsRequested = viewOperationsRequested;
            _saveContinueRequested = saveContinueRequested;
        }

        public void ClearActions()
        {
            _viewOperationsRequested = null;
            _saveContinueRequested = null;
        }

        private void BindUnityEvents()
        {
            if (_eventsBound)
                return;
            viewOperationsButton?.onClick.AddListener(HandleViewOperations);
            saveContinueButton?.onClick.AddListener(HandleSaveContinue);
            _eventsBound = true;
        }

        private void RemoveUnityEvents()
        {
            if (!_eventsBound)
                return;
            viewOperationsButton?.onClick.RemoveListener(HandleViewOperations);
            saveContinueButton?.onClick.RemoveListener(HandleSaveContinue);
            _eventsBound = false;
        }

        private void HandleViewOperations()
        {
            if (_viewOperationsRequested != null)
                _viewOperationsRequested.Invoke();
            else
                popupFrame?.Close();
        }

        private void HandleSaveContinue()
        {
            if (_saveContinueRequested != null)
                _saveContinueRequested.Invoke();
            else
                popupFrame?.Close();
        }

#if UNITY_EDITOR
        public void Configure(
            UIPopupFrameView configuredPopupFrame,
            Button configuredViewOperationsButton,
            Button configuredSaveContinueButton)
        {
            popupFrame = configuredPopupFrame;
            viewOperationsButton = configuredViewOperationsButton;
            saveContinueButton = configuredSaveContinueButton;
        }
#endif
    }
}
