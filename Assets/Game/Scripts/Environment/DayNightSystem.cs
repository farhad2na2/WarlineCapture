using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Game.Configs;

namespace Game.Runtime
{
    public sealed partial class DayNightSystem : SystemBase
    {
        private const float MinFullDayDurationMinutes = 0.1f;
        private const float VisualRefreshIntervalSeconds = 0.25f;

        private DayNightSystemConfig config;
        private float fullDayDurationMinutes = 5f;
        private float startHour = 9f;
        private Light directionalLight;
        private Volume globalVolume;
        private float sunYaw = 170f;
        private bool animateDirectionalLight;
        private float nightStartsAtHour = 19f;
        private float morningStartsAtHour = 6f;
        private float nightVisionStartHour = 19f;
        private float nightVisionEndHour = 6f;
        private float nightVisionPostExposure = 2.2f;
        private Color nightVisionColorFilter = new(0.55f, 1f, 0.58f, 1f);
        private float nightVisionTemperature = -80f;
        private float nightVisionTint = -55f;
        private float nightVisionBloomIntensity = 0.02f;
        private float nightVisionBloomThreshold = 2f;
        private bool affectFog = true;
        private bool affectVolume = true;
        private bool updateDynamicGI;
        private float dynamicGIRefreshIntervalSeconds = 30f;

        private Material _runtimeSkyboxMaterial;
        private Material _originalSkyboxMaterial;
        private bool _originalFogEnabled;
        private Color _originalFogColor;
        private float _originalFogDensity;
        private Color _originalAmbientSkyColor;
        private Color _originalAmbientEquatorColor;
        private Color _originalAmbientGroundColor;
        private float _originalAmbientIntensity;
        private float _originalReflectionIntensity;
        private bool _originalDirectionalLightCaptured;
        private Color _originalDirectionalLightColor;
        private float _originalDirectionalLightIntensity;
        private float _originalDirectionalLightShadowStrength;
        private Quaternion _originalDirectionalLightRotation;
        private bool _originalVolumeWeightCaptured;
        private float _originalVolumeWeight;
        private VolumeProfile _originalVolumeSharedProfile;
        private VolumeProfile _originalVolumeInstantiatedProfile;
        private float _currentHour;
        private int _dayCount = 1;
        private float _nextEnvironmentRefreshTime;
        private ColorAdjustments _colorAdjustments;
        private WhiteBalance _whiteBalance;
        private Bloom _bloom;
        private VolumeProfile _runtimeVolumeProfile;
        private VolumeProfile _sourceVolumeProfile;
        private bool _runtimeVisualsEnabled = true;
        private bool _initialEnvironmentStateCaptured;
        private float _nextVisualRefreshTime;
        private float _qualityShadowStrengthCap = 1f;

        public float FullDayDurationMinutes => fullDayDurationMinutes;
        public float CurrentHour => _currentHour;
        public int DayCount => _dayCount;
        public int Hour24 => GetHour24();
        public int Minute => GetMinute();
        public bool IsNightTime => IsHourWithinWrappedRange(_currentHour, nightStartsAtHour, morningStartsAtHour);
        public string FormattedTimeText => $"Day {_dayCount}  {GetHour24():00}:{GetMinute():00}";
        public bool RuntimeVisualsEnabled => _runtimeVisualsEnabled;
        public float QualityShadowStrengthCap => _qualityShadowStrengthCap;

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

        public void Init(DayNightSystemConfig configAsset, Light sceneDirectionalLight, Volume sceneGlobalVolume)
        {
            config = configAsset;
            directionalLight = sceneDirectionalLight;
            globalVolume = sceneGlobalVolume;
            ApplyConfigIfAvailable();

            CaptureInitialEnvironmentState();
            PrepareRuntimeSkybox();
            PrepareVolumeOverrides();

            _currentHour = Mathf.Repeat(startHour, 24f);
            _dayCount = 1;
            ApplyVisualState();
            _nextVisualRefreshTime = UnityEngine.Time.unscaledTime + VisualRefreshIntervalSeconds;
        }

        public void SetRuntimeVisualsEnabled(bool enabled)
        {
            if (_runtimeVisualsEnabled == enabled)
                return;

            _runtimeVisualsEnabled = enabled;
            if (!enabled)
            {
                ReleaseRuntimeVolumeProfile();
                RestoreInitialEnvironmentState();
                return;
            }

            CaptureInitialEnvironmentState();
            PrepareRuntimeSkybox();
            PrepareVolumeOverrides();
            ApplyVisualState();
            _nextVisualRefreshTime = UnityEngine.Time.unscaledTime + VisualRefreshIntervalSeconds;
        }

        public void ReapplyVisualStateAfterQualityChange()
        {
            if (!_runtimeVisualsEnabled || !_initialEnvironmentStateCaptured)
                return;

            PrepareVolumeOverrides(rebuildProfile: true);
            ApplyVisualState();
            _nextVisualRefreshTime = UnityEngine.Time.unscaledTime + VisualRefreshIntervalSeconds;
        }

        public void SetQualityShadowStrengthCap(float shadowStrengthCap)
        {
            _qualityShadowStrengthCap = Mathf.Clamp01(shadowStrengthCap);
        }

        private void ApplyConfigIfAvailable()
        {
            if (config == null)
                return;

            fullDayDurationMinutes = config.FullDayDurationMinutes;
            startHour = config.StartHour;
            if (config.DirectionalLight != null)
                directionalLight = config.DirectionalLight;
            if (config.GlobalVolume != null)
                globalVolume = config.GlobalVolume;
            sunYaw = config.SunYaw;
            animateDirectionalLight = config.AnimateDirectionalLight;
            nightStartsAtHour = Mathf.Repeat(config.NightStartsAtHour, 24f);
            morningStartsAtHour = Mathf.Repeat(config.MorningStartsAtHour, 24f);
            nightVisionStartHour = Mathf.Repeat(config.NightVisionStartHour, 24f);
            nightVisionEndHour = Mathf.Repeat(config.NightVisionEndHour, 24f);
            nightVisionPostExposure = config.NightVisionPostExposure;
            nightVisionColorFilter = config.NightVisionColorFilter;
            nightVisionTemperature = config.NightVisionTemperature;
            nightVisionTint = config.NightVisionTint;
            nightVisionBloomIntensity = Mathf.Max(0f, config.NightVisionBloomIntensity);
            nightVisionBloomThreshold = Mathf.Max(0f, config.NightVisionBloomThreshold);
            affectFog = config.AffectFog;
            affectVolume = config.AffectVolume;
            updateDynamicGI = config.UpdateDynamicGI;
            dynamicGIRefreshIntervalSeconds = Mathf.Max(1f, config.DynamicGIRefreshIntervalSeconds);
        }

        public new void Update()
        {
            if (!_runtimeVisualsEnabled)
                return;

            float clampedDurationMinutes = Mathf.Max(MinFullDayDurationMinutes, fullDayDurationMinutes);
            float hoursPerSecond = 24f / (clampedDurationMinutes * 60f);
            float previousHour = _currentHour;
            _currentHour = Mathf.Repeat(_currentHour + (UnityEngine.Time.deltaTime * hoursPerSecond), 24f);
            if (_currentHour < previousHour)
                _dayCount++;

            float now = UnityEngine.Time.unscaledTime;
            if (now < _nextVisualRefreshTime && (!updateDynamicGI || now < _nextEnvironmentRefreshTime))
                return;

            ApplyVisualState();
            _nextVisualRefreshTime = now + VisualRefreshIntervalSeconds;
        }

        public void Dispose()
        {
            ReleaseRuntimeVolumeProfile();
            RestoreInitialEnvironmentState();
            _initialEnvironmentStateCaptured = false;
        }

        private void CaptureInitialEnvironmentState()
        {
            _originalSkyboxMaterial = RenderSettings.skybox;
            _originalFogEnabled = RenderSettings.fog;
            _originalFogColor = RenderSettings.fogColor;
            _originalFogDensity = RenderSettings.fogDensity;
            _originalAmbientSkyColor = RenderSettings.ambientSkyColor;
            _originalAmbientEquatorColor = RenderSettings.ambientEquatorColor;
            _originalAmbientGroundColor = RenderSettings.ambientGroundColor;
            _originalAmbientIntensity = RenderSettings.ambientIntensity;
            _originalReflectionIntensity = RenderSettings.reflectionIntensity;

            _originalDirectionalLightCaptured = directionalLight != null;
            if (_originalDirectionalLightCaptured)
            {
                _originalDirectionalLightColor = directionalLight.color;
                _originalDirectionalLightIntensity = directionalLight.intensity;
                _originalDirectionalLightShadowStrength = directionalLight.shadowStrength;
                _originalDirectionalLightRotation = directionalLight.transform.rotation;
            }

            if (globalVolume != null)
            {
                _originalVolumeWeight = globalVolume.weight;
                _originalVolumeWeightCaptured = true;
                _originalVolumeSharedProfile = globalVolume.sharedProfile;
                _originalVolumeInstantiatedProfile = globalVolume.HasInstantiatedProfile()
                    ? globalVolume.profile
                    : null;
            }

            _initialEnvironmentStateCaptured = true;
        }

        private void RestoreInitialEnvironmentState()
        {
            if (!_initialEnvironmentStateCaptured)
                return;

            if (_originalSkyboxMaterial != null)
                RenderSettings.skybox = _originalSkyboxMaterial;

            RenderSettings.fog = _originalFogEnabled;
            RenderSettings.fogColor = _originalFogColor;
            RenderSettings.fogDensity = _originalFogDensity;
            RenderSettings.ambientSkyColor = _originalAmbientSkyColor;
            RenderSettings.ambientEquatorColor = _originalAmbientEquatorColor;
            RenderSettings.ambientGroundColor = _originalAmbientGroundColor;
            RenderSettings.ambientIntensity = _originalAmbientIntensity;
            RenderSettings.reflectionIntensity = _originalReflectionIntensity;

            if (_originalDirectionalLightCaptured && directionalLight != null)
            {
                directionalLight.color = _originalDirectionalLightColor;
                directionalLight.intensity = _originalDirectionalLightIntensity;
                directionalLight.shadowStrength = _originalDirectionalLightShadowStrength;
                directionalLight.transform.rotation = _originalDirectionalLightRotation;
            }

            if (globalVolume != null && _originalVolumeWeightCaptured)
            {
                globalVolume.weight = _originalVolumeWeight;
                globalVolume.sharedProfile = _originalVolumeSharedProfile;
                globalVolume.profile = _originalVolumeInstantiatedProfile;
            }

            if (_runtimeSkyboxMaterial != null)
            {
                DestroyRuntimeObject(_runtimeSkyboxMaterial);
                _runtimeSkyboxMaterial = null;
            }
        }

        private void PrepareRuntimeSkybox()
        {
            if (RenderSettings.skybox == null)
                return;

            _runtimeSkyboxMaterial = Object.Instantiate(RenderSettings.skybox);
            _runtimeSkyboxMaterial.name = $"{RenderSettings.skybox.name}_RuntimeDayNight";
            RenderSettings.skybox = _runtimeSkyboxMaterial;
        }

        private void PrepareVolumeOverrides(bool rebuildProfile = false)
        {
            if (!affectVolume || globalVolume == null)
                return;

            VolumeProfile sourceProfile = globalVolume.sharedProfile;
            if (rebuildProfile || _runtimeVolumeProfile == null || _sourceVolumeProfile != sourceProfile)
            {
                ReleaseRuntimeVolumeProfile();
                _sourceVolumeProfile = sourceProfile;
                _runtimeVolumeProfile = sourceProfile != null
                    ? Object.Instantiate(sourceProfile)
                    : ScriptableObject.CreateInstance<VolumeProfile>();
                _runtimeVolumeProfile.name = sourceProfile != null
                    ? $"{sourceProfile.name}_RuntimeDayNight"
                    : "RuntimeDayNightVolume";
                globalVolume.profile = _runtimeVolumeProfile;
            }

            VolumeProfile profile = _runtimeVolumeProfile;
            if (profile == null)
                return;

            if (!profile.TryGet(out _colorAdjustments))
                _colorAdjustments = profile.Add<ColorAdjustments>(true);
            if (!profile.TryGet(out _whiteBalance))
                _whiteBalance = profile.Add<WhiteBalance>(true);
            if (!profile.TryGet(out _bloom))
                _bloom = profile.Add<Bloom>(true);

            _colorAdjustments.active = true;
            _colorAdjustments.postExposure.overrideState = true;
            _colorAdjustments.colorFilter.overrideState = true;

            _whiteBalance.active = true;
            _whiteBalance.temperature.overrideState = true;
            _whiteBalance.tint.overrideState = true;

            _bloom.active = true;
            _bloom.intensity.overrideState = true;
            _bloom.threshold.overrideState = true;
        }

        private void ReleaseRuntimeVolumeProfile()
        {
            _colorAdjustments = null;
            _whiteBalance = null;
            _bloom = null;

            if (globalVolume != null)
                globalVolume.profile = null;

            DestroyRuntimeObject(_runtimeVolumeProfile);
            _runtimeVolumeProfile = null;
            _sourceVolumeProfile = null;
        }

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }

        private bool IsNightVisionActive(float hour)
        {
            return IsHourWithinWrappedRange(hour, nightVisionStartHour, nightVisionEndHour);
        }

        private float ComputeDaylightFromConfig(float hour)
        {
            const float transitionHours = 1f;

            float sunriseEnd = morningStartsAtHour + transitionHours;
            float sunsetBegin = nightStartsAtHour - transitionHours;

            float sunriseBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(morningStartsAtHour, sunriseEnd, hour));
            float sunsetBlend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(sunsetBegin, nightStartsAtHour, hour));

            if (hour < morningStartsAtHour || hour >= nightStartsAtHour)
                return 0f;

            if (hour < sunriseEnd)
                return sunriseBlend;

            if (hour >= sunsetBegin)
                return sunsetBlend;

            return 1f;
        }

        private float ComputeTwilightFromConfig(float hour)
        {
            const float transitionHours = 1f;

            float dawn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(morningStartsAtHour, morningStartsAtHour + transitionHours, hour));
            float dusk = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(nightStartsAtHour - transitionHours, nightStartsAtHour, hour));

            if (hour >= morningStartsAtHour && hour < morningStartsAtHour + transitionHours)
                return dawn;

            if (hour >= nightStartsAtHour - transitionHours && hour < nightStartsAtHour)
                return dusk;

            return 0f;
        }

        private float ComputeDirectionalElevationFromConfig(float hour)
        {
            float sunriseHour = morningStartsAtHour;
            float sunsetHour = nightStartsAtHour;

            if (sunriseHour < sunsetHour)
            {
                if (hour >= sunriseHour && hour < sunsetHour)
                {
                    float dayProgress = Mathf.InverseLerp(sunriseHour, sunsetHour, hour);
                    return Mathf.Lerp(0f, 180f, dayProgress);
                }

                if (hour < sunriseHour)
                {
                    float nightProgress = Mathf.InverseLerp(0f, sunriseHour, hour);
                    return Mathf.Lerp(-90f, 0f, nightProgress);
                }

                float eveningProgress = Mathf.InverseLerp(sunsetHour, 24f, hour);
                return Mathf.Lerp(180f, 270f, eveningProgress);
            }

            float normalizedDay = hour / 24f;
            return (normalizedDay * 360f) - 90f;
        }

        private static bool IsHourWithinWrappedRange(float hour, float startHour, float endHour)
        {
            if (Mathf.Approximately(startHour, endHour))
                return false;

            if (startHour < endHour)
                return hour >= startHour && hour < endHour;

            return hour >= startHour || hour < endHour;
        }

        private int GetHour24()
        {
            return Mathf.FloorToInt(_currentHour) % 24;
        }

        private int GetMinute()
        {
            return Mathf.FloorToInt((_currentHour - Mathf.Floor(_currentHour)) * 60f);
        }
    }
}
