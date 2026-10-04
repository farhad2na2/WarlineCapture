using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.Composition
{
    /// <summary>Shared CityEdgeAirfield presentation. The legacy type name is retained for existing bindings.</summary>
    public sealed class SkirmishS004CampaignLighting : MonoBehaviour
    {
        private readonly List<(Light light,bool enabled)> matchLights = new();
        private Scene previousActive;
        private EnvironmentState previous;
        private bool configured;
        private RenderPipelineAsset previousPipeline;
        private UniversalRenderPipelineAsset matchPipeline;
        public static bool Supports(Game.Configs.OperationMapDefinition map) => map != null &&
            map.OperationMapId == "opmap.skirmish.cityedgeairfield_prepared";
        public void Configure(Scene matchScene,Scene campaignScene)
        {
            if(configured || !campaignScene.IsValid() || !campaignScene.isLoaded) return;
            previousActive=SceneManager.GetActiveScene();previous=EnvironmentState.Capture();
            EnvironmentState authored;
            try
            {
                SceneManager.SetActiveScene(campaignScene);
                authored=EnvironmentState.Capture();
            }
            finally { SceneManager.SetActiveScene(previousActive); }
            authored.Apply();
            // Keep the Campaign sun and palette. A short tactical match needs
            // clear ground detail and shadows beyond the camera's height.
            RenderSettings.fog=false;
            // A fixed daylight fill separates warm buildings from cool shadows.
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.72f,.79f,.88f);
            RenderSettings.ambientEquatorColor=new Color(.60f,.56f,.46f);
            RenderSettings.ambientGroundColor=new Color(.36f,.32f,.25f);
            ApplySelectedRenderingProfile();
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(light.type==LightType.Directional && light.gameObject.scene==matchScene)
                { matchLights.Add((light,light.enabled));light.enabled=false; }
            configured=true;
        }
        private void LateUpdate()
        {
            // Quality selection can finish after the operation map loads, or
            // change during play. Preserve that selection's scale and features
            // while adapting shadows to this map's tactical camera distance.
            if(configured && GraphicsSettings.currentRenderPipeline!=matchPipeline)
                ApplySelectedRenderingProfile();
        }
        private void ApplySelectedRenderingProfile()
        {
            if(GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset pipeline) return;
            previousPipeline=QualitySettings.renderPipeline;
            var old=matchPipeline;
            matchPipeline=Instantiate(pipeline);
            matchPipeline.name="CityEdgeAirfield Rendering";
            matchPipeline.hideFlags=HideFlags.DontSave;
            matchPipeline.shadowDistance=Mathf.Max(pipeline.shadowDistance,160f);
            if(!Application.isMobilePlatform)
            {
                matchPipeline.mainLightShadowmapResolution=Mathf.Max(pipeline.mainLightShadowmapResolution,2048);
                matchPipeline.shadowCascadeCount=Mathf.Max(pipeline.shadowCascadeCount,2);
            }
            QualitySettings.renderPipeline=matchPipeline;
            if(old!=null) { if(Application.isPlaying) Destroy(old);else DestroyImmediate(old); }
        }
        private void OnDestroy()
        {
            if(!configured) return;
            foreach(var item in matchLights) if(item.light!=null) item.light.enabled=item.enabled;
            if(matchPipeline!=null)
            {
                if(QualitySettings.renderPipeline==matchPipeline) QualitySettings.renderPipeline=previousPipeline;
                if(Application.isPlaying) Destroy(matchPipeline); else DestroyImmediate(matchPipeline);
                matchPipeline=null;
            }
            if(previousActive.IsValid() && previousActive.isLoaded && SceneManager.GetActiveScene()==previousActive) previous.Apply();
            configured=false;
        }
        private struct EnvironmentState
        {
            public AmbientMode mode; public Color sky,equator,ground,fogColor;
            public float ambientIntensity,reflectionIntensity,fogDensity,fogStart,fogEnd;
            public bool fog; public FogMode fogMode;public Light sun;public Material skybox;
            public static EnvironmentState Capture()=>new() { mode=RenderSettings.ambientMode,sky=RenderSettings.ambientSkyColor,
                equator=RenderSettings.ambientEquatorColor,ground=RenderSettings.ambientGroundColor,ambientIntensity=RenderSettings.ambientIntensity,
                reflectionIntensity=RenderSettings.reflectionIntensity,sun=RenderSettings.sun,skybox=RenderSettings.skybox,
                fog=RenderSettings.fog,fogColor=RenderSettings.fogColor,fogMode=RenderSettings.fogMode,
                fogDensity=RenderSettings.fogDensity,fogStart=RenderSettings.fogStartDistance,fogEnd=RenderSettings.fogEndDistance };
            public void Apply()
            {
                RenderSettings.ambientMode=mode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;
                RenderSettings.ambientGroundColor=ground;RenderSettings.ambientIntensity=ambientIntensity;
                RenderSettings.reflectionIntensity=reflectionIntensity;RenderSettings.sun=sun;RenderSettings.skybox=skybox;
                RenderSettings.fog=fog;RenderSettings.fogColor=fogColor;RenderSettings.fogMode=fogMode;
                RenderSettings.fogDensity=fogDensity;RenderSettings.fogStartDistance=fogStart;RenderSettings.fogEndDistance=fogEnd;
            }
        }
    }
}
