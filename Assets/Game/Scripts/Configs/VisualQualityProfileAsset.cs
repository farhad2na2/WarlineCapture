using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Configs
{
    public enum VisualQualityRuntimeMode
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    [CreateAssetMenu(menuName = "Game/Rendering/Visual Quality Profile")]
    public sealed class VisualQualityProfileAsset : ScriptableObject
    {
        [SerializeField] private VisualQualityRuntimeMode runtimeMode = VisualQualityRuntimeMode.Ultra;
        [SerializeField] private UniversalRenderPipelineAsset lowRenderPipelineAsset;
        [SerializeField] private UniversalRenderPipelineAsset mediumRenderPipelineAsset;
        [SerializeField] private UniversalRenderPipelineAsset renderPipelineAsset;
        [SerializeField] private VolumeProfile globalVolumeProfile;
        [SerializeField, Range(0.5f, 1f)] private float lowRenderScaleOverride = 0.72f;
        [SerializeField, Range(0.5f, 1f)] private float mediumRenderScaleOverride = 0.9f;
        [SerializeField] private bool enableCameraPostProcessing = true;
        [SerializeField] private AntialiasingMode cameraAntialiasingMode = AntialiasingMode.FastApproximateAntialiasing;
        [SerializeField, Min(0f)] private float cameraRenderScaleOverride = 1f;
        [SerializeField, Range(0f, 1f)] private float lowSunShadowStrength = 0.25f;
        [SerializeField, Range(0f, 1f)] private float mediumSunShadowStrength = 0.55f;
        [SerializeField] private Color premiumSunColor = new(1f, 0.78f, 0.48f, 1f);
        [SerializeField, Range(0f, 8f)] private float premiumSunIntensity = 3.2f;
        [SerializeField, Range(0f, 1f)] private float premiumSunShadowStrength = 0.88f;
        [SerializeField] private Vector3 premiumSunEulerAngles = new(42f, -32f, 0f);
        [SerializeField] private Color premiumAmbientSkyColor = new(0.58f, 0.68f, 0.78f, 1f);
        [SerializeField] private Color premiumAmbientEquatorColor = new(0.34f, 0.36f, 0.38f, 1f);
        [SerializeField] private Color premiumAmbientGroundColor = new(0.12f, 0.13f, 0.15f, 1f);
        [SerializeField] private bool enableGroundVariation = true;
        [SerializeField] private bool enablePremiumFog = true;
        [SerializeField] private Color premiumFogColor = new(0.44f, 0.50f, 0.55f, 1f);
        [SerializeField, Range(0f, 0.05f)] private float premiumFogDensity = 0.0085f;

        public VisualQualityRuntimeMode RuntimeMode => runtimeMode;
        public UniversalRenderPipelineAsset LowRenderPipelineAsset => lowRenderPipelineAsset;
        public UniversalRenderPipelineAsset MediumRenderPipelineAsset => mediumRenderPipelineAsset;
        public UniversalRenderPipelineAsset RenderPipelineAsset => renderPipelineAsset;
        public VolumeProfile GlobalVolumeProfile => globalVolumeProfile;
        public float LowRenderScaleOverride => lowRenderScaleOverride;
        public float MediumRenderScaleOverride => mediumRenderScaleOverride;
        public bool EnableCameraPostProcessing => enableCameraPostProcessing;
        public AntialiasingMode CameraAntialiasingMode => cameraAntialiasingMode;
        public float CameraRenderScaleOverride => cameraRenderScaleOverride;
        public bool HasCameraRenderScaleOverride => cameraRenderScaleOverride > 0f;
        public float LowSunShadowStrength => lowSunShadowStrength;
        public float MediumSunShadowStrength => mediumSunShadowStrength;
        public Color PremiumSunColor => premiumSunColor;
        public float PremiumSunIntensity => premiumSunIntensity;
        public float PremiumSunShadowStrength => premiumSunShadowStrength;
        public Vector3 PremiumSunEulerAngles => premiumSunEulerAngles;
        public Color PremiumAmbientSkyColor => premiumAmbientSkyColor;
        public Color PremiumAmbientEquatorColor => premiumAmbientEquatorColor;
        public Color PremiumAmbientGroundColor => premiumAmbientGroundColor;
        public bool EnableGroundVariation => enableGroundVariation;
        public bool EnablePremiumFog => enablePremiumFog;
        public Color PremiumFogColor => premiumFogColor;
        public float PremiumFogDensity => premiumFogDensity;

        public bool UsePremiumProfile => runtimeMode == VisualQualityRuntimeMode.Ultra;
    }
}
