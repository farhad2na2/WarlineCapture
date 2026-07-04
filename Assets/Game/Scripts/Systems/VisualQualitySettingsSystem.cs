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
        private const float MinimumLowRenderScale = 0.72f;
        private const float MinimumMediumRenderScale = 0.72f;
        private const float MinimumHighRenderScale = 0.72f;
        private const float MinimumUltraRenderScale = 1f;

        private VisualQualityProfileAsset _premiumProfile;
        private Camera _worldCamera;
        private Light _directionalLight;
        private Volume _globalVolume;
        private RenderPipelineAsset _originalRenderPipeline;
        private VolumeProfile _originalVolumeProfile;
        private Color _originalSunColor;
        private float _originalSunIntensity;
        private float _originalSunShadowStrength;
        private Quaternion _originalSunRotation;
        private bool _hasOriginalSunData;
        private AmbientMode _originalAmbientMode;
        private Color _originalAmbientSkyColor;
        private Color _originalAmbientEquatorColor;
        private Color _originalAmbientGroundColor;
        private bool _originalFog;
        private FogMode _originalFogMode;
        private Color _originalFogColor;
        private float _originalFogDensity;
        private bool _originalCameraPostProcessing;
        private AntialiasingMode _originalCameraAntialiasing;
        private bool _hasOriginalCameraData;
        private float _originalLowRenderScale;
        private float _originalMediumRenderScale;
        private float _originalPremiumRenderScale;
        private bool _hasOriginalLowRenderScale;
        private bool _hasOriginalMediumRenderScale;
        private bool _hasOriginalPremiumRenderScale;
        private VisualQualityRuntimeMode _appliedMode;
        private bool _hasAppliedMode;
        private bool _initialized;
        private bool _overrideApplied;

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
            _directionalLight = directionalLight;
            _globalVolume = globalVolume;
            _originalRenderPipeline = QualitySettings.renderPipeline;
            _originalVolumeProfile = globalVolume != null ? globalVolume.sharedProfile : null;
            _originalAmbientMode = RenderSettings.ambientMode;
            _originalAmbientSkyColor = RenderSettings.ambientSkyColor;
            _originalAmbientEquatorColor = RenderSettings.ambientEquatorColor;
            _originalAmbientGroundColor = RenderSettings.ambientGroundColor;
            _originalFog = RenderSettings.fog;
            _originalFogMode = RenderSettings.fogMode;
            _originalFogColor = RenderSettings.fogColor;
            _originalFogDensity = RenderSettings.fogDensity;

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

                if (premiumProfile.RenderPipelineAsset != null)
                {
                    _originalPremiumRenderScale = premiumProfile.RenderPipelineAsset.renderScale;
                    _hasOriginalPremiumRenderScale = true;
                }
            }

            if (directionalLight != null)
            {
                _originalSunColor = directionalLight.color;
                _originalSunIntensity = directionalLight.intensity;
                _originalSunShadowStrength = directionalLight.shadowStrength;
                _originalSunRotation = directionalLight.transform.rotation;
                _hasOriginalSunData = true;
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

        public new void Update()
        {
            if (!_initialized)
                return;

            Apply(_premiumProfile != null ? _premiumProfile.RuntimeMode : VisualQualityRuntimeMode.High);
        }

        public void Dispose()
        {
            if (!_initialized)
                return;

            RestoreBaseline();
            _premiumProfile = null;
            _worldCamera = null;
            _directionalLight = null;
            _globalVolume = null;
            _originalRenderPipeline = null;
            _originalVolumeProfile = null;
            _hasOriginalSunData = false;
            _hasOriginalCameraData = false;
            _hasOriginalLowRenderScale = false;
            _hasOriginalMediumRenderScale = false;
            _hasOriginalPremiumRenderScale = false;
            _hasAppliedMode = false;
            _initialized = false;
            _overrideApplied = false;
        }

        private void Apply(VisualQualityRuntimeMode mode)
        {
            if (!_initialized)
                return;

            if (!_hasAppliedMode || _appliedMode != mode)
            {
                RestoreBaseline();
                ApplyModeStaticSettings(mode);
                _appliedMode = mode;
                _hasAppliedMode = true;
            }

            ApplyModeDynamicSettings(mode);
        }

        private void ApplyModeStaticSettings(VisualQualityRuntimeMode mode)
        {
            if (_premiumProfile == null)
                return;

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

        private void ApplyModeDynamicSettings(VisualQualityRuntimeMode mode)
        {
            if (_premiumProfile == null)
                return;

            Shader.SetGlobalFloat(GroundVariationDisabledId, _premiumProfile.EnableGroundVariation ? 0f : 1f);

            switch (mode)
            {
                case VisualQualityRuntimeMode.Low:
                    ApplyMobileDynamicSettings(_premiumProfile.LowSunShadowStrength);
                    break;
                case VisualQualityRuntimeMode.Medium:
                case VisualQualityRuntimeMode.High:
                    ApplyMobileDynamicSettings(_premiumProfile.MediumSunShadowStrength);
                    break;
                case VisualQualityRuntimeMode.Ultra:
                    ApplyUltraDynamicSettings();
                    break;
            }
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
                cameraData.antialiasing = AntialiasingMode.None;
            }

            _overrideApplied = true;
        }

        private void ApplyHighStaticSettings()
        {
            if (_premiumProfile.MediumRenderPipelineAsset != null)
            {
                QualitySettings.renderPipeline = _premiumProfile.MediumRenderPipelineAsset;
                _premiumProfile.MediumRenderPipelineAsset.renderScale = Mathf.Clamp(_premiumProfile.MediumRenderScaleOverride, MinimumHighRenderScale, 1f);
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

        private void ApplyUltraStaticSettings()
        {
            if (_premiumProfile.RenderPipelineAsset != null)
            {
                QualitySettings.renderPipeline = _premiumProfile.RenderPipelineAsset;
                if (_premiumProfile.HasCameraRenderScaleOverride)
                    _premiumProfile.RenderPipelineAsset.renderScale = Mathf.Clamp(_premiumProfile.CameraRenderScaleOverride, MinimumUltraRenderScale, 1f);
            }

            if (_globalVolume != null && _premiumProfile.GlobalVolumeProfile != null)
            {
                _globalVolume.sharedProfile = _premiumProfile.GlobalVolumeProfile;
                _globalVolume.weight = 1f;
            }

            if (_worldCamera != null && _worldCamera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
            {
                cameraData.renderPostProcessing = _premiumProfile.EnableCameraPostProcessing;
                cameraData.antialiasing = _premiumProfile.CameraAntialiasingMode;
            }

            _overrideApplied = true;
        }

        private void ApplyMobileDynamicSettings(float shadowStrength)
        {
            if (_hasOriginalSunData && _directionalLight != null)
            {
                _directionalLight.color = _originalSunColor;
                _directionalLight.intensity = _originalSunIntensity;
                _directionalLight.shadowStrength = shadowStrength;
                _directionalLight.transform.rotation = _originalSunRotation;
            }

            RenderSettings.ambientMode = _originalAmbientMode;
            RenderSettings.ambientSkyColor = _originalAmbientSkyColor;
            RenderSettings.ambientEquatorColor = _originalAmbientEquatorColor;
            RenderSettings.ambientGroundColor = _originalAmbientGroundColor;
            RenderSettings.fog = false;
        }

        private void ApplyUltraDynamicSettings()
        {
            if (_directionalLight != null)
            {
                _directionalLight.color = _premiumProfile.PremiumSunColor;
                _directionalLight.intensity = _premiumProfile.PremiumSunIntensity;
                _directionalLight.shadowStrength = _premiumProfile.PremiumSunShadowStrength;
                _directionalLight.transform.rotation = Quaternion.Euler(_premiumProfile.PremiumSunEulerAngles);
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = _premiumProfile.PremiumAmbientSkyColor;
            RenderSettings.ambientEquatorColor = _premiumProfile.PremiumAmbientEquatorColor;
            RenderSettings.ambientGroundColor = _premiumProfile.PremiumAmbientGroundColor;
            RenderSettings.fog = _premiumProfile.EnablePremiumFog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = _premiumProfile.PremiumFogColor;
            RenderSettings.fogDensity = _premiumProfile.PremiumFogDensity;
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

            if (_hasOriginalSunData && _directionalLight != null)
            {
                _directionalLight.color = _originalSunColor;
                _directionalLight.intensity = _originalSunIntensity;
                _directionalLight.shadowStrength = _originalSunShadowStrength;
                _directionalLight.transform.rotation = _originalSunRotation;
            }

            RenderSettings.ambientMode = _originalAmbientMode;
            RenderSettings.ambientSkyColor = _originalAmbientSkyColor;
            RenderSettings.ambientEquatorColor = _originalAmbientEquatorColor;
            RenderSettings.ambientGroundColor = _originalAmbientGroundColor;
            RenderSettings.fog = _originalFog;
            RenderSettings.fogMode = _originalFogMode;
            RenderSettings.fogColor = _originalFogColor;
            RenderSettings.fogDensity = _originalFogDensity;

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

            if (_hasOriginalPremiumRenderScale && _premiumProfile.RenderPipelineAsset != null)
                _premiumProfile.RenderPipelineAsset.renderScale = _originalPremiumRenderScale;
        }
    }
}
