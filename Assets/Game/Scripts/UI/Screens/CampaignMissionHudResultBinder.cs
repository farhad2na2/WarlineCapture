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
            GameObject instance = Instantiate(missionResultPopupPrefab, modalOverlay, false);
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
            UiShellRuntimeGateway.TryEnqueueMissionResultAction(action);
        }

        private void OnRetryRequested() =>
            UiShellRuntimeGateway.TryEnqueueMissionResultAction(UiMissionResultActionKind.Retry);

#if UNITY_EDITOR
        public void Configure(RectTransform overlay, GameObject popupPrefab)
        {
            modalOverlay = overlay;
            missionResultPopupPrefab = popupPrefab;
        }
#endif
    }
}
