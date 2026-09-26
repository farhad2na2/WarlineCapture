using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal sealed partial class AssistantHighlightPresentationSystemHelper
    {
        internal bool HasDirectTutorialTarget => _directTutorialCue &&
            (_selectionBoxRequested || _directTutorialTarget != null && _commandCueActive && _directTutorialTarget.gameObject.activeInHierarchy ||
             _directTutorialTarget == null && _worldRingRoot != null && _worldRingRoot.activeSelf);
        internal bool HasDirectTutorialControlTarget => HasDirectTutorialTarget && _directTutorialTarget!=null;
        internal bool HasVisibleDirectTutorialTarget
        {
            get
            {
                if(!HasDirectTutorialTarget) return false;
                if(_selectionBoxRequested)return TryObserveVisibleSelectionDrag(out _, out _);
                if(_directTutorialTarget!=null) return _screenTargetIndicator!=null && _screenTargetIndicator.gameObject.activeInHierarchy;
                if(_worldCamera==null || _worldRingRenderer==null) return false;
                var point=_worldCamera.WorldToViewportPoint(_directTutorialWorldTarget);
                return point.z>0 && point.x>.18f && point.x<.72f && point.y>.27f && point.y<.86f;
            }
        }
        private bool _directTutorialCue;
        private RectTransform _directTutorialTarget;
        private Vector3 _directTutorialWorldTarget;
        private string _directCaptionKey, _directCaptionLocale;
        internal Button ResolveSquadTutorialControl() => _squadGuidanceButton;
        internal bool IsBuildDrawerOpen => _buildDrawerView != null && _buildDrawerView.IsOpen;
        private IBuildingUiQuery _productionQuery;
        private readonly System.Collections.Generic.List<BuildingPendingProductionUiEntry> _productionQueue = new();
        private float _nextProductionQueryAt;
        internal IBuildingUiQuery ProductionQuery => _productionQuery;
        internal bool HasProducedUnitsThisAttempt =>
            _productionQuery is IBuildingProductionProgressQuery progress && progress.HasProducedUnitsThisAttempt;
        internal void BindProductionQuery(IBuildingUiQuery query)
        {
            _productionQuery = query;
            _productionQueue.Clear();
            _nextProductionQueryAt = 0;
        }
        internal bool HasPendingProduction
        {
            get
            {
                if (_buildDrawerCatalogRuntimeView?.ProductionQuery != null)
                    _productionQuery = _buildDrawerCatalogRuntimeView.ProductionQuery;
                if (_productionQuery == null)
                    return _buildDrawerCatalogRuntimeView != null && _buildDrawerCatalogRuntimeView.HasPendingProduction;
                if (Time.unscaledTime >= _nextProductionQueryAt)
                {
                    _nextProductionQueryAt = Time.unscaledTime + .2f;
                    _productionQuery.GetFriendlyPendingProductionUiEntries(_productionQueue);
                }
                return _productionQueue.Count > 0 ||
                    _productionQuery is IBuildingProductionProgressQuery progress && progress.HasProductionDeliveryInProgress;
            }
        }

        internal void TickAttention(float time)
        {
            TutorialAttentionPulseView.Present(_screenTargetIndicator, time,
                _commandCueActive, _directTutorialTarget != null ? _directTutorialTarget.GetEntityId().GetHashCode() : 0, 0f);
            if (_worldRingRenderer != null && _worldRingRoot.activeSelf)
                _worldRingRenderer.widthMultiplier = WorldRingWidth * (1f + .5f *
                    TutorialAttentionPulseView.Opacity(time, 0, SettingsService.LoadReducedMotionPreference()));
        }

        internal void ShowTutorialControl(Button button, string captionKey)
        {
            HideSelectionDrag();
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
                var binding = _screenTargetLabel.GetComponent<V3LocalizedTextBindingView>();
                binding.Configure(captionKey, UiShellRuntimeGateway.Localization.Get(captionKey));
                binding.ApplyLocalization();
                _directCaptionKey=captionKey; _directCaptionLocale=locale;
            }
            var canvas = button.GetComponentInParent<Canvas>();
            while (canvas != null && !canvas.isRootCanvas && !canvas.overrideSorting)
                canvas = canvas.transform.parent?.GetComponentInParent<Canvas>();
            var cueCanvas = _screenTargetIndicator.GetComponent<Canvas>();
            cueCanvas.sortingLayerID = canvas != null ? canvas.sortingLayerID : _screenTargetCanvas.sortingLayerID;
            cueCanvas.sortingOrder = Mathf.Max(_screenTargetCanvas.sortingOrder + 50, (canvas != null ? canvas.sortingOrder : 0) + 2);
            TutorialAttentionPulseView.BindFrame(_screenTargetIndicator, TickCommandCue);
            TickCommandCue();
        }

        internal static void ClampCaptionToCanvas(RectTransform caption, Vector2 cueCenter, Rect bounds)
        {
            if (caption == null) return;
            caption.sizeDelta = new Vector2(Mathf.Min(caption.sizeDelta.x, bounds.width - 16f), caption.sizeDelta.y);
            float halfWidth = caption.rect.width * .5f;
            var position = caption.anchoredPosition;
            position.x = Mathf.Clamp(cueCenter.x, bounds.xMin + halfWidth + 8f,
                bounds.xMax - halfWidth - 8f) - cueCenter.x;
            caption.anchoredPosition = position;
        }

        private void ConfigureControlCaption(RectTransform caption,RectTransform button,Vector2 frameSize)
        {
            bool aria=button.GetComponentInParent<AriaTutorialBriefingView>()!=null;
            // Guidance owns a pixel-space overlay; do not inherit the HUD's reference-canvas scale.
            float scale=Mathf.Clamp(Screen.height/1080f,.75f,1.2f);
            var pointer = caption.GetComponentInParent<TutorialTapPointerView>();
            pointer?.SetCaptionSize(new Vector2((aria?320:360)*scale,(aria?44:56)*scale));
            _screenTargetLabel.fontSizeMin=18*scale;
            _screenTargetLabel.fontSizeMax=24*scale;
        }

        internal void ShowTutorialWorld(Vector3 target)
        {
            HideSelectionDrag();
            _directTutorialCue = true;
            _directTutorialTarget = null;
            _directTutorialWorldTarget = target + Vector3.up * WorldRingHeightOffset;
            _commandCueActive = false;
            if (_screenTargetIndicator != null) _screenTargetIndicator.gameObject.SetActive(false);
            ApplyWorldRing(new UiAssistantHighlightModel(0,true,0,0,0,3,target.x,target.y,target.z,1), true);
            SetWorldMarkerDecoration(true);
            if(_worldRingRenderer!=null) _worldRingRenderer.startColor=_worldRingRenderer.endColor=V3GuidanceYellow;
            // A guidance target must remain visible when a transport or prop occludes the person.
            if(_worldRingMaterial!=null) _worldRingMaterial.SetInt("_ZTest",(int)UnityEngine.Rendering.CompareFunction.Always);
        }

        internal void ShowTutorialArea(Vector3 center, float radius, bool defensive = false)
        {
            ShowTutorialWorld(center);
            WriteWorldMarker(center + Vector3.up * WorldRingHeightOffset, Mathf.Max(.35f, radius));
            SetWorldMarkerDecoration(false);
            _worldRingRenderer.startColor = _worldRingRenderer.endColor = defensive
                ? V3GuidanceYellow : new Color32(94,224,79,255);
        }

        private void SetWorldMarkerDecoration(bool active)
        {
            SetLinesActive(_worldAccentRenderers, active);
            SetLinesActive(_worldBracketRenderers, active);
            SetLinesActive(_worldCrosshairRenderers, active);
        }

        private static void SetLinesActive(LineRenderer[] lines, bool active)
        {
            if (lines != null) foreach (var line in lines) line.enabled = active;
        }

        internal void ClearDirectTutorialCue()
        {
            HideSelectionDrag();
            if (!_directTutorialCue) return;
            _directTutorialCue = false;
            if(_worldRingMaterial!=null) _worldRingMaterial.SetInt("_ZTest",(int)UnityEngine.Rendering.CompareFunction.LessEqual);
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
