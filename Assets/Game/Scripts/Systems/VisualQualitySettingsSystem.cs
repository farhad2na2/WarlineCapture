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
        private VolumeProfile _originalVolumeProfile;
        private bool _originalCameraPostProcessing;
        private AntialiasingMode _originalCameraAntialiasing;
        private bool _hasOriginalCameraData;
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
            _originalVolumeProfile = globalVolume != null ? globalVolume.sharedProfile : null;

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
            _originalVolumeProfile = null;
            _hasOriginalCameraData = false;
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
                SharedRuntimeRenderPipelineLease.SelectQuality(
                    _premiumProfile.LowRenderPipelineAsset,
                    Mathf.Clamp(_premiumProfile.LowRenderScaleOverride, MinimumLowRenderScale, 1f));
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
                SharedRuntimeRenderPipelineLease.SelectQuality(
                    _premiumProfile.MediumRenderPipelineAsset,
                    Mathf.Clamp(_premiumProfile.MediumRenderScaleOverride, MinimumMediumRenderScale, 1f));
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
                SharedRuntimeRenderPipelineLease.SelectQuality(
                    _premiumProfile.HighRenderPipelineAsset,
                    Mathf.Clamp(_premiumProfile.HighRenderScaleOverride, MinimumHighRenderScale, 1f));
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
                SharedRuntimeRenderPipelineLease.SelectQuality(
                    _premiumProfile.RenderPipelineAsset,
                    _premiumProfile.HasCameraRenderScaleOverride
                        ? Mathf.Clamp(_premiumProfile.CameraRenderScaleOverride, MinimumUltraRenderScale, 1f)
                        : _premiumProfile.RenderPipelineAsset.renderScale);
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

    }
}
