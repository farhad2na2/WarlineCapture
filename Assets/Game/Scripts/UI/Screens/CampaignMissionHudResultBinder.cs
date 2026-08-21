using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CampaignMissionHudResultBinder : MonoBehaviour
    {
        [SerializeField] private RectTransform modalOverlay;
        [SerializeField] private GameObject missionResultPopupPrefab;

        private MissionResultPopupView activeView;
        private UiMissionResultPopupModel activeModel;
        private UIShellRegionView popupRegion;
        private uint appliedVersion;
        private bool appliedActionEnabled;

        public void RefreshPresentation()
        {
            if (!UiShellRuntimeGateway.TryReadMissionResult(out UiMissionResultPopupModel model))
            {
                Close();
                return;
            }
            if (activeView == null && !Open())
                return;
            if (appliedVersion == model.Version && appliedActionEnabled == model.PrimaryActionEnabled)
                return;
            activeModel = model;
            appliedVersion = model.Version;
            appliedActionEnabled = model.PrimaryActionEnabled;
            activeView.Apply(in model);
        }

        private bool Open()
        {
            if (modalOverlay == null || missionResultPopupPrefab == null)
                return false;
            popupRegion?.ResetVisualState();
            GameObject instance = Instantiate(missionResultPopupPrefab, modalOverlay, false);
            FitPopupToOverlay(instance);
            activeView = instance.GetComponent<MissionResultPopupView>();
            if (activeView == null)
            {
                Destroy(instance);
                return false;
            }
            activeView.Bind(OnPrimaryRequested, OnRetryRequested);
            modalOverlay.gameObject.SetActive(true);
            appliedVersion = 0;
            return true;
        }

        private static void FitPopupToOverlay(GameObject instance)
        {
            if (instance == null || !instance.TryGetComponent(out RectTransform rect))
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private void Close()
        {
            if (activeView != null) Destroy(activeView.gameObject);
            activeView = null;
            appliedVersion = 0;
        }

        private void OnPrimaryRequested()
        {
            UiMissionResultActionKind action = activeModel.Outcome == UiMissionResultOutcome.Victory
                ? UiMissionResultActionKind.Continue : UiMissionResultActionKind.Retry;
            bool queued = UiShellRuntimeGateway.TryEnqueueMissionResultAction(action);
            if (queued && action == UiMissionResultActionKind.Continue)
            {
                UiShellRuntimeGateway.TryEnqueueRouteRequest(
                    UiShellRouteIntent.ReturnToMainMenu,
                    UIRoute.MainMenu,
                    pushHistory: false);
            }
        }

        private void OnRetryRequested() =>
            UiShellRuntimeGateway.TryEnqueueMissionResultAction(UiMissionResultActionKind.Retry);

        public void Configure(
            RectTransform overlay,
            GameObject popupPrefab,
            UIShellRegionView presentationRegion = null)
        {
            modalOverlay = overlay;
            missionResultPopupPrefab = popupPrefab;
            popupRegion = presentationRegion;
        }
    }
}
