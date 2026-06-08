using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class VisualQualitySettingsSystem : IDisposable
{
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
    private bool _initialized;
    private bool _premiumApplied;

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
        Apply(_premiumProfile != null ? _premiumProfile.Quality : UIGraphicsQuality.High);
    }

    public void Update()
    {
        if (!_initialized)
            return;

        Apply(_premiumProfile != null ? _premiumProfile.Quality : UIGraphicsQuality.High);
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
        _initialized = false;
        _premiumApplied = false;
    }

    private void Apply(UIGraphicsQuality quality)
    {
        if (!_initialized)
            return;

        if (quality == UIGraphicsQuality.Ultra)
        {
            ApplyPremiumProfile();
            return;
        }

        RestoreBaseline();
    }

    private void ApplyPremiumProfile()
    {
        if (_premiumProfile == null)
            return;

        if (_premiumProfile.RenderPipelineAsset != null)
        {
            QualitySettings.renderPipeline = _premiumProfile.RenderPipelineAsset;
            if (_premiumProfile.HasCameraRenderScaleOverride)
                _premiumProfile.RenderPipelineAsset.renderScale = Mathf.Max(0.1f, _premiumProfile.CameraRenderScaleOverride);
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

        _premiumApplied = true;
    }

    private void RestoreBaseline()
    {
        if (!_premiumApplied)
            return;

        QualitySettings.renderPipeline = _originalRenderPipeline;

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

        _premiumApplied = false;
    }
}
