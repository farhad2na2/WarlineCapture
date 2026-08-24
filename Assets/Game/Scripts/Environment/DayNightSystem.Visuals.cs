using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class DayNightSystem
    {
        private void ApplyVisualState()
        {
            float daylight = ComputeDaylightFromConfig(_currentHour);
            float twilight = ComputeTwilightFromConfig(_currentHour);

            ApplyDirectionalLight(_currentHour, daylight, twilight);
            ApplyAmbientAndFog(daylight, twilight);
            ApplySkybox(daylight, twilight);
            ApplyVolume(daylight, twilight, IsNightVisionActive(_currentHour));

            if (updateDynamicGI && UnityEngine.Time.unscaledTime >= _nextEnvironmentRefreshTime)
            {
                DynamicGI.UpdateEnvironment();
                _nextEnvironmentRefreshTime = UnityEngine.Time.unscaledTime + dynamicGIRefreshIntervalSeconds;
            }
        }

        private void ApplyDirectionalLight(float hour, float daylight, float twilight)
        {
            if (directionalLight == null)
                return;

            float elevation = ComputeDirectionalElevationFromConfig(hour);
            if (animateDirectionalLight)
                directionalLight.transform.rotation = Quaternion.Euler(elevation, sunYaw, 0f);

            Color nightColor = new(0.34f, 0.40f, 0.52f);
            Color dawnColor = new(1f, 0.67f, 0.42f);
            Color noonColor = new(1f, 0.97f, 0.87f);
            Color blended = Color.Lerp(nightColor, dawnColor, twilight);
            directionalLight.color = Color.Lerp(blended, noonColor, daylight);
            directionalLight.intensity = Mathf.Lerp(0.10f, 1.15f, daylight) + (twilight * 0.08f);
            directionalLight.shadowStrength = Mathf.Min(
                Mathf.Lerp(0.35f, 1f, daylight),
                _qualityShadowStrengthCap);
        }

        private void ApplyAmbientAndFog(float daylight, float twilight)
        {
            Color nightSky = new(0.045f, 0.065f, 0.12f);
            Color dawnSky = new(0.55f, 0.34f, 0.28f);
            Color daySky = new(0.212f, 0.227f, 0.259f);
            Color nightEquator = new(0.03f, 0.04f, 0.08f);
            Color dawnEquator = new(0.42f, 0.22f, 0.18f);
            Color dayEquator = new(0.114f, 0.125f, 0.133f);
            Color nightGround = new(0.015f, 0.018f, 0.03f);
            Color dawnGround = new(0.17f, 0.11f, 0.08f);
            Color dayGround = new(0.047f, 0.043f, 0.035f);

            RenderSettings.ambientSkyColor = Color.Lerp(Color.Lerp(nightSky, dawnSky, twilight), daySky, daylight);
            RenderSettings.ambientEquatorColor = Color.Lerp(Color.Lerp(nightEquator, dawnEquator, twilight), dayEquator, daylight);
            RenderSettings.ambientGroundColor = Color.Lerp(Color.Lerp(nightGround, dawnGround, twilight), dayGround, daylight);
            RenderSettings.ambientIntensity = Mathf.Lerp(0.40f, 1f, daylight);
            RenderSettings.reflectionIntensity = Mathf.Lerp(0.35f, 1f, daylight);

            if (!affectFog)
                return;

            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(
                new Color(0.05f, 0.07f, 0.13f),
                new Color(0.74f, 0.58f, 0.45f),
                twilight);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, new Color(0.70f, 0.77f, 0.85f), daylight);
            RenderSettings.fogDensity = Mathf.Lerp(0.0065f, 0.0012f, daylight);
        }

        private void ApplySkybox(float daylight, float twilight)
        {
            if (_runtimeSkyboxMaterial == null)
                return;

            if (_runtimeSkyboxMaterial.HasProperty("_SkyTint"))
            {
                Color tint = Color.Lerp(new Color(0.06f, 0.10f, 0.19f), new Color(0.78f, 0.42f, 0.26f), twilight);
                tint = Color.Lerp(tint, new Color(0.5f, 0.62f, 0.78f), daylight);
                _runtimeSkyboxMaterial.SetColor("_SkyTint", tint);
            }

            if (_runtimeSkyboxMaterial.HasProperty("_GroundColor"))
            {
                Color ground = Color.Lerp(new Color(0.03f, 0.03f, 0.05f), new Color(0.25f, 0.18f, 0.14f), twilight);
                ground = Color.Lerp(ground, new Color(0.37f, 0.35f, 0.30f), daylight);
                _runtimeSkyboxMaterial.SetColor("_GroundColor", ground);
            }

            if (_runtimeSkyboxMaterial.HasProperty("_Exposure"))
                _runtimeSkyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(0.22f, 1.1f, daylight) + (twilight * 0.12f));

            if (_runtimeSkyboxMaterial.HasProperty("_AtmosphereThickness"))
                _runtimeSkyboxMaterial.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.65f, 1.1f, daylight));
        }

        private void ApplyVolume(float daylight, float twilight, bool nightVisionActive)
        {
            if (!affectVolume || globalVolume == null || _colorAdjustments == null || _whiteBalance == null || _bloom == null)
                return;

            globalVolume.weight = 1f;
            if (nightVisionActive)
            {
                _colorAdjustments.postExposure.value = nightVisionPostExposure;
                _colorAdjustments.colorFilter.value = nightVisionColorFilter;
                _whiteBalance.temperature.value = nightVisionTemperature;
                _whiteBalance.tint.value = nightVisionTint;
                _bloom.intensity.value = nightVisionBloomIntensity;
                _bloom.threshold.value = nightVisionBloomThreshold;
                return;
            }

            _colorAdjustments.postExposure.value = Mathf.Lerp(-1.0f, 0.15f, daylight) + (twilight * 0.12f);
            _colorAdjustments.colorFilter.value = Color.Lerp(
                Color.Lerp(new Color(0.62f, 0.72f, 0.95f), new Color(1.0f, 0.76f, 0.58f), twilight),
                Color.white,
                daylight);

            _whiteBalance.temperature.value = Mathf.Lerp(-18f, 4f, daylight) + (twilight * 14f);
            _whiteBalance.tint.value = Mathf.Lerp(6f, 0f, daylight);

            _bloom.intensity.value = Mathf.Lerp(0.55f, 0.18f, daylight);
            _bloom.threshold.value = Mathf.Lerp(0.85f, 1.15f, daylight);
        }
    }
}
