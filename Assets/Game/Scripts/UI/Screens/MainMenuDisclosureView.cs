using UnityEngine;

namespace Game.UI.Runtime
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class MainMenuDisclosureView : MonoBehaviour
    {
        [SerializeField] private uint requiredCompletedMask;
        private CanvasGroup group;
        public void Configure(uint mask) => requiredCompletedMask = mask;
        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            group ??= GetComponent<CanvasGroup>();
            bool visible = UiShellRuntimeGateway.TryReadCampaignOperations(out var model) &&
                (model.CompletedMissionMask & requiredCompletedMask) == requiredCompletedMask;
            group.alpha = visible ? 1 : 0;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
