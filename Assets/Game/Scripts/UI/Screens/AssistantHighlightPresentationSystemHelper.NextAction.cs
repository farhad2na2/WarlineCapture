using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal sealed partial class AssistantHighlightPresentationSystemHelper
    {
        internal bool HasDirectTutorialTarget => _directTutorialCue &&
            (_directTutorialTarget != null && _commandCueActive && _directTutorialTarget.gameObject.activeInHierarchy ||
             _directTutorialTarget == null && _worldRingRoot != null && _worldRingRoot.activeSelf);
        private bool _directTutorialCue;
        private RectTransform _directTutorialTarget;
        private string _directCaptionKey, _directCaptionLocale;
        internal bool IsBuildDrawerOpen => _buildDrawerView != null && _buildDrawerView.IsOpen;

        internal void ShowTutorialControl(Button button, string captionKey)
        {
            _directTutorialCue = true;
            if (_worldRingRoot != null && _worldRingRoot.activeSelf) _worldRingRoot.SetActive(false);
            if (button == null || !button.IsActive() || !button.IsInteractable())
            {
                _commandCueActive = false;
                _directTutorialTarget = null;
                if (_screenTargetIndicator != null && _screenTargetIndicator.gameObject.activeSelf)
                    _screenTargetIndicator.gameObject.SetActive(false);
                return;
            }
            EnsureScreenTargetIndicator();
            if (_screenTargetIndicator == null) return;
            _directTutorialCue = true;
            _directTutorialTarget = button.transform as RectTransform;
            _commandCueActive = true;
            if (_worldRingRoot != null) _worldRingRoot.SetActive(false);
            // Resolve/shaping through the shared binding, including Farsi font and direction.
            string locale=UiShellRuntimeGateway.Localization.CurrentLocaleCode;
            if(_directCaptionKey!=captionKey || _directCaptionLocale!=locale)
            {
                _screenTargetLabel.GetComponent<V3LocalizedTextBindingView>().Configure(captionKey,
                    UiShellRuntimeGateway.Localization.Get(captionKey));
                _directCaptionKey=captionKey; _directCaptionLocale=locale;
            }
            var canvas = button.GetComponentInParent<Canvas>();
            while (canvas != null && !canvas.isRootCanvas && !canvas.overrideSorting)
                canvas = canvas.transform.parent?.GetComponentInParent<Canvas>();
            var cueCanvas = _screenTargetIndicator.GetComponent<Canvas>();
            cueCanvas.sortingLayerID = canvas != null ? canvas.sortingLayerID : _screenTargetCanvas.sortingLayerID;
            cueCanvas.sortingOrder = Mathf.Max(_screenTargetCanvas.sortingOrder + 50, (canvas != null ? canvas.sortingOrder : 0) + 2);
            TickCommandCue();
        }

        private void ConfigureControlCaption(RectTransform caption,RectTransform button,Vector2 frameSize)
        {
            bool aria=button.GetComponentInParent<AriaTutorialBriefingView>()!=null;
            float scale=(frameSize.y-20)/Mathf.Max(1,button.rect.height);
            caption.gameObject.SetActive(true);
            // Compact ARIA captions sit on the button's top edge, inside the reserved gap.
            // Other controls retain their existing, larger battlefield/build captions.
            caption.anchoredPosition=aria?new Vector2(0,-10):Vector2.zero;
            caption.sizeDelta=aria ? new Vector2(frameSize.x-20,32*scale) :
                new Vector2(Mathf.Clamp(frameSize.x-20,480,880),96);
            _screenTargetLabel.fontSizeMin=aria?12*scale:36;
            _screenTargetLabel.fontSizeMax=aria?17*scale:52;
        }

        internal void ShowTutorialWorld(Vector3 target)
        {
            _directTutorialCue = true;
            _directTutorialTarget = null;
            _commandCueActive = false;
            if (_screenTargetIndicator != null) _screenTargetIndicator.gameObject.SetActive(false);
            ApplyWorldRing(new UiAssistantHighlightModel(0,true,0,0,0,3,target.x,target.y,target.z,1), true);
        }

        internal void ClearDirectTutorialCue()
        {
            if (!_directTutorialCue) return;
            _directTutorialCue = false;
            _directTutorialTarget = null;
            _commandCueActive = false;
            if (_screenTargetIndicator != null) _screenTargetIndicator.gameObject.SetActive(false);
            if (_worldRingRoot != null) _worldRingRoot.SetActive(false);
        }

        internal Button ResolveBuildTutorialControl(bool defense, bool placement, out string captionKey)
        {
            captionKey = "ui.guidance.open_build";
            if (_buildDrawerView == null || !_buildDrawerView.IsOpen) return _buildGuidanceButton;
            return _buildDrawerCatalogRuntimeView?.ResolveBuildingTutorialTarget(defense, placement, out captionKey);
        }

        internal Button ResolveProductionTutorialControl(out string captionKey)
        {
            captionKey = "ui.guidance.open_build";
            if (_buildDrawerView == null || !_buildDrawerView.IsOpen) return _buildGuidanceButton;
            if (_buildDrawerCatalogRuntimeView == null) return null;
            captionKey = _buildDrawerCatalogRuntimeView.ResolveProductionTutorialCaption();
            return _buildDrawerCatalogRuntimeView.ResolveRifleProductionGuidanceTarget()?.GetComponent<Button>();
        }
    }
}
