using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Game.Configs;

namespace Game.Runtime
{
    public sealed partial class VisualQualitySettingsSystem : SystemBase
    {
        // Read by Game/Environment/GroundMacroVariation.shader (0 = on, 1 = off).
        private static readonly int GroundVariationDisabledId = Shader.PropertyToID("_GroundVariationDisabled");
        private const float MinimumLowRenderScale = 0.50f;
        private const float MinimumMediumRenderScale = 0.50f;
        private const float MinimumHighRenderScale = 0.50f;
        private const float MinimumUltraRenderScale = 1f;

        private VisualQualityProfileAsset _premiumProfile;
        private Camera _worldCamera;
        private Volume _globalVolume;
        private RenderPipelineAsset _originalRenderPipeline;
        private VolumeProfile _originalVolumeProfile;
        private bool _originalCameraPostProcessing;
        private AntialiasingMode _originalCameraAntialiasing;
        private bool _hasOriginalCameraData;
        private float _originalLowRenderScale;
        private float _originalMediumRenderScale;
        private float _originalHighRenderScale;
        private float _originalPremiumRenderScale;
        private bool _hasOriginalLowRenderScale;
        private bool _hasOriginalMediumRenderScale;
        private bool _hasOriginalHighRenderScale;
        private bool _hasOriginalPremiumRenderScale;
        private VisualQualityRuntimeMode _appliedMode;
        private float _appliedShadowStrengthCap = 1f;
        private bool _hasAppliedMode;
        private bool _initialized;
        private bool _overrideApplied;

        public bool IsInitialized => _initialized;
        public VisualQualityRuntimeMode AppliedMode => _appliedMode;
        public float AppliedShadowStrengthCap => _appliedShadowStrengthCap;

        protected override void OnCreate()
        {
            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnDestroy()
        {
            Dispose();
        }

        public void Initialize(VisualQualityProfileAsset premiumProfile, Camera worldCamera, Light directionalLight, Volume globalVolume)
        {
            Dispose();

            _premiumProfile = premiumProfile;
            _worldCamera = worldCamera;
            _globalVolume = globalVolume;
            _originalRenderPipeline = QualitySettings.renderPipeline;
            _originalVolumeProfile = globalVolume != null ? globalVolume.sharedProfile : null;

            if (premiumProfile != null)
            {
                if (premiumProfile.LowRenderPipelineAsset != null)
                {
                    _originalLowRenderScale = premiumProfile.LowRenderPipelineAsset.renderScale;
                    _hasOriginalLowRenderScale = true;
                }

                if (premiumProfile.MediumRenderPipelineAsset != null)
                {
                    _originalMediumRenderScale = premiumProfile.MediumRenderPipelineAsset.renderScale;
                    _hasOriginalMediumRenderScale = true;
                }

                if (premiumProfile.HighRenderPipelineAsset != null)
                {
                    _originalHighRenderScale = premiumProfile.HighRenderPipelineAsset.renderScale;
                    _hasOriginalHighRenderScale = true;
                }

                if (premiumProfile.RenderPipelineAsset != null)
                {
                    _originalPremiumRenderScale = premiumProfile.RenderPipelineAsset.renderScale;
                    _hasOriginalPremiumRenderScale = true;
                }
            }

            if (worldCamera != null && worldCamera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
            {
                _originalCameraPostProcessing = cameraData.renderPostProcessing;
                _originalCameraAntialiasing = cameraData.antialiasing;
                _hasOriginalCameraData = true;
            }

            _initialized = true;
            Apply(_premiumProfile != null ? _premiumProfile.RuntimeMode : VisualQualityRuntimeMode.High);
        }

        public bool ApplyRuntimeMode(VisualQualityRuntimeMode mode)
        {
            if (!_initialized)
                return false;

            return Apply(mode);
        }

        public void Dispose()
        {
            if (!_initialized)
                return;

            RestoreBaseline();
            _premiumProfile = null;
            _worldCamera = null;
            _globalVolume = null;
            _originalRenderPipeline = null;
            _originalVolumeProfile = null;
            _hasOriginalCameraData = false;
            _hasOriginalLowRenderScale = false;
            _hasOriginalMediumRenderScale = false;
            _hasOriginalHighRenderScale = false;
            _hasOriginalPremiumRenderScale = false;
            _hasAppliedMode = false;
            _appliedShadowStrengthCap = 1f;
            _initialized = false;
            _overrideApplied = false;
        }

        private bool Apply(VisualQualityRuntimeMode mode)
        {
            if (!_initialized)
                return false;

            if (_hasAppliedMode && _appliedMode == mode)
                return false;

            RestoreBaseline();
            ApplyModeStaticSettings(mode);
            _appliedMode = mode;
            _hasAppliedMode = true;
            return true;
        }

        private void ApplyModeStaticSettings(VisualQualityRuntimeMode mode)
        {
            _appliedShadowStrengthCap = ResolveShadowStrengthCap(mode);
            if (_premiumProfile == null)
                return;

            Shader.SetGlobalFloat(GroundVariationDisabledId, _premiumProfile.EnableGroundVariation ? 0f : 1f);

            switch (mode)
            {
                case VisualQualityRuntimeMode.Low:
                    ApplyLowStaticSettings();
                    break;
                case VisualQualityRuntimeMode.Medium:
                    ApplyMediumStaticSettings();
                    break;
                case VisualQualityRuntimeMode.High:
                    ApplyHighStaticSettings();
                    break;
                case VisualQualityRuntimeMode.Ultra:
                    ApplyUltraStaticSettings();
                    break;
            }
        }

        private float ResolveShadowStrengthCap(VisualQualityRuntimeMode mode)
        {
            if (_premiumProfile == null)
                return 1f;

            return mode switch
            {
                VisualQualityRuntimeMode.Low => _premiumProfile.LowSunShadowStrength,
                VisualQualityRuntimeMode.Medium => _premiumProfile.MediumSunShadowStrength,
                VisualQualityRuntimeMode.High => _premiumProfile.HighSunShadowStrength,
                VisualQualityRuntimeMode.Ultra => _premiumProfile.PremiumSunShadowStrength,
                _ => 1f
            };
        }

        private void ApplyLowStaticSettings()
        {
            if (_premiumProfile.LowRenderPipelineAsset != null)
            {
                QualitySettings.renderPipeline = _premiumProfile.LowRenderPipelineAsset;
                _premiumProfile.LowRenderPipelineAsset.renderScale = Mathf.Clamp(_premiumProfile.LowRenderScaleOverride, MinimumLowRenderScale, 1f);
            }

            if (_globalVolume != null)
                _globalVolume.sharedProfile = _originalVolumeProfile;

            if (_worldCamera != null && _worldCamera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
            {
                cameraData.renderPostProcessing = false;
                cameraData.antialiasing = AntialiasingMode.None;
            }

            _overrideApplied = true;
        }

        private void ApplyMediumStaticSettings()
        {
            if (_premiumProfile.MediumRenderPipelineAsset != null)
            {
                QualitySettings.renderPipeline = _premiumProfile.MediumRenderPipelineAsset;
                _premiumProfile.MediumRenderPipelineAsset.renderScale = Mathf.Clamp(_premiumProfile.MediumRenderScaleOverride, MinimumMediumRenderScale, 1f);
            }

            if (_globalVolume != null)
                _globalVolume.sharedProfile = _originalVolumeProfile;

            if (_worldCamera != null && _worldCamera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
            {
                cameraData.renderPostProcessing = false;
                cameraData.antialiasing = _premiumProfile.CameraAntialiasingMode;
            }

            _overrideApplied = true;
        }

        private void ApplyHighStaticSettings()
        {
            if (_premiumProfile.HighRenderPipelineAsset != null)
            {
                QualitySettings.renderPipeline = _premiumProfile.HighRenderPipelineAsset;
                _premiumProfile.HighRenderPipelineAsset.renderScale = Mathf.Clamp(_premiumProfile.HighRenderScaleOverride, MinimumHighRenderScale, 1f);
            }

            if (_globalVolume != null && _premiumProfile.HighVolumeProfile != null)
                _globalVolume.sharedProfile = _premiumProfile.HighVolumeProfile;

            if (_worldCamera != null && _worldCamera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
            {
                cameraData.renderPostProcessing = _premiumProfile.EnableHighCameraPostProcessing;
                cameraData.antialiasing = _premiumProfile.CameraAntialiasingMode;
            }

            _overrideApplied = true;
        }

        private void ApplyUltraStaticSettings()
        {
            if (_premiumProfile.RenderPipelineAsset != null)
            {
                QualitySettings.renderPipeline = _premiumProfile.RenderPipelineAsset;
                if (_premiumProfile.HasCameraRenderScaleOverride)
                    _premiumProfile.RenderPipelineAsset.renderScale = Mathf.Clamp(_premiumProfile.CameraRenderScaleOverride, MinimumUltraRenderScale, 1f);
            }

            if (_globalVolume != null && _premiumProfile.GlobalVolumeProfile != null)
                _globalVolume.sharedProfile = _premiumProfile.GlobalVolumeProfile;

            if (_worldCamera != null && _worldCamera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
            {
                cameraData.renderPostProcessing = _premiumProfile.EnableCameraPostProcessing;
                cameraData.antialiasing = _premiumProfile.CameraAntialiasingMode;
            }

            _overrideApplied = true;
        }

        private void RestoreBaseline()
        {
            if (!_overrideApplied)
                return;

            QualitySettings.renderPipeline = _originalRenderPipeline;
            RestoreRenderPipelineAssetScales();
            Shader.SetGlobalFloat(GroundVariationDisabledId, 0f);

            if (_globalVolume != null)
                _globalVolume.sharedProfile = _originalVolumeProfile;

            if (_hasOriginalCameraData && _worldCamera != null && _worldCamera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
            {
                cameraData.renderPostProcessing = _originalCameraPostProcessing;
                cameraData.antialiasing = _originalCameraAntialiasing;
            }

            _overrideApplied = false;
        }

        private void RestoreRenderPipelineAssetScales()
        {
            if (_premiumProfile == null)
                return;

            if (_hasOriginalLowRenderScale && _premiumProfile.LowRenderPipelineAsset != null)
                _premiumProfile.LowRenderPipelineAsset.renderScale = _originalLowRenderScale;

            if (_hasOriginalMediumRenderScale && _premiumProfile.MediumRenderPipelineAsset != null)
                _premiumProfile.MediumRenderPipelineAsset.renderScale = _originalMediumRenderScale;

            if (_hasOriginalHighRenderScale && _premiumProfile.HighRenderPipelineAsset != null)
                _premiumProfile.HighRenderPipelineAsset.renderScale = _originalHighRenderScale;

            if (_hasOriginalPremiumRenderScale && _premiumProfile.RenderPipelineAsset != null)
                _premiumProfile.RenderPipelineAsset.renderScale = _originalPremiumRenderScale;
        }
    }
}
