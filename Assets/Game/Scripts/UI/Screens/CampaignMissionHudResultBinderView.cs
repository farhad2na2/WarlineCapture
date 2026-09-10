using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CampaignMissionHudResultBinderView : MonoBehaviour
    {
        [SerializeField] private RectTransform modalOverlay;
        [SerializeField] private GameObject missionResultPopupPrefab;

        private MissionResultPopupView activeView;
        private UiMissionResultPopupModel activeModel;
        [SerializeField] private UIShellRegionView popupRegion;
        private uint appliedVersion;
        private bool appliedActionEnabled;
        private string appliedLocale;

        private void Awake()
        {
            // Existing Menu scenes predate the serialized region reference. Restore the
            // bounded parent binding so a guide's hide animation cannot hide later results.
            if (popupRegion == null && modalOverlay != null)
                popupRegion = modalOverlay.GetComponentInParent<UIShellRegionView>(true);
        }

        public void RefreshPresentation()
        {
            if (!UiShellRuntimeGateway.TryReadMissionResult(out UiMissionResultPopupModel model))
            {
                Close();
                return;
            }
            // The shell owns this shared modal region while the guide is open. Recreating
            // the result after InstallRoot clears it would cover the guide on the next frame.
            if((model.Defense.Applicable || model.Extraction.Applicable) && UiShellRuntimeGateway.IsMissionFieldGuidePresenting())
            {Close(); return;}
            if (activeView == null && !Open())
                return;
            if (appliedVersion == model.Version && appliedActionEnabled == model.PrimaryActionEnabled && appliedLocale==UiShellRuntimeGateway.Localization.CurrentLocaleCode)
                return;
            activeModel = model;
            appliedVersion = model.Version;
            appliedActionEnabled = model.PrimaryActionEnabled;
            appliedLocale = UiShellRuntimeGateway.Localization.CurrentLocaleCode;
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
            if(activeModel.SettlementFailed)
            {
                UiShellRuntimeGateway.TryEnqueueMissionResultAction(UiMissionResultActionKind.RetrySave);
                return;
            }
            UiMissionResultActionKind action = activeModel.Outcome == UiMissionResultOutcome.Victory
                ? UiMissionResultActionKind.Continue : UiMissionResultActionKind.Retry;
            bool queued = UiShellRuntimeGateway.TryEnqueueMissionResultAction(action);
            if (queued && action == UiMissionResultActionKind.Continue && !activeModel.DebriefRequired)
            {
                UiShellRuntimeGateway.TryEnqueueRouteRequest(
                    UiShellRouteIntent.ReturnToMainMenu,
                    activeModel.MissionId != UiCampaignMissionProjectionIds.M01
                        ? UIRoute.Campaign : UIRoute.MainMenu,
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
