using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.Composition
{
    /// <summary>Shared Campaign source presentation. The legacy type name is retained for existing bindings.</summary>
    public sealed class SkirmishS004CampaignLighting : MonoBehaviour
    {
        private readonly List<(Light light,bool enabled)> matchLights = new();
        private Scene previousActive;
        private EnvironmentState previous;
        private EnvironmentState authoredEnvironment;
        private Scene sourceScene;
        private bool configured;
        private RenderPipelineAsset previousPipeline;
        private UniversalRenderPipelineAsset matchPipeline;
        public static bool Supports(Game.Configs.OperationMapDefinition map) => map != null &&
            (map.OperationMapId == "opmap.skirmish.cityedgeairfield_prepared" ||
             map.SourceBinding.SourceOperationMapId == "opmap.skirmish.cityedgeairfield_prepared" ||
             map.OperationMapId == "opmap.skirmish.desert_base_01" ||
             map.SourceBinding.SourceOperationMapId == "opmap.skirmish.desert_base_01");
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
            authoredEnvironment=authored;
            sourceScene=campaignScene;
            authored.Apply();
            // Keep the Campaign sun and palette. A short tactical match needs
            // clear ground detail and shadows beyond the camera's height.
            RenderSettings.fog=false;
            // A fixed daylight fill separates warm buildings from cool shadows.
            RenderSettings.ambientMode=AmbientMode.Trilight;
            ApplyDaylightFill();
            ApplySelectedRenderingProfile();
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(light.type==LightType.Directional && light.gameObject.scene==matchScene)
                { matchLights.Add((light,light.enabled));light.enabled=false; }
            configured=true;
        }
        public static void ApplyDaylightFill()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.79f,.86f,.96f);
            RenderSettings.ambientEquatorColor=new Color(.72f,.73f,.74f);
            RenderSettings.ambientGroundColor=new Color(.48f,.42f,.34f);
            // URP samples the ambient SH probe. Set the fixed fill explicitly:
            // changing sky colours alone can leave the loaded scene's old probe.
            var fill=new SphericalHarmonicsL2();
            fill.AddAmbientLight(new Color(.20f,.23f,.28f));
            RenderSettings.ambientProbe=fill;
        }
        private void LateUpdate()
        {
            if(!configured || !sourceScene.IsValid() || !sourceScene.isLoaded) return;
            // Loading/Continue and disabling day/night can restore an earlier
            // scene's environment after Configure. The loaded Campaign owner
            // keeps its fixed daylight after those one-time state changes.
            if(RenderSettings.fog || RenderSettings.ambientMode!=AmbientMode.Trilight ||
                RenderSettings.ambientSkyColor!=new Color(.79f,.86f,.96f) ||
                RenderSettings.ambientEquatorColor!=new Color(.72f,.73f,.74f) ||
                RenderSettings.ambientGroundColor!=new Color(.48f,.42f,.34f) ||
                RenderSettings.skybox!=authoredEnvironment.skybox)
            {
                authoredEnvironment.Apply();
                RenderSettings.fog=false;
                ApplyDaylightFill();
            }
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
            matchPipeline.name="Campaign Map Rendering";
            matchPipeline.hideFlags=HideFlags.DontSave;
            // The oblique desktop camera sees town frontage beyond 160 metres.
            // Keep those silhouettes inside its shadow coverage; retain the
            // existing mobile distance until a device profile is accepted.
            matchPipeline.shadowDistance=Mathf.Max(pipeline.shadowDistance,Application.isMobilePlatform?160f:260f);
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
            public SphericalHarmonicsL2 ambientProbe;
            public static EnvironmentState Capture()=>new() { mode=RenderSettings.ambientMode,sky=RenderSettings.ambientSkyColor,
                equator=RenderSettings.ambientEquatorColor,ground=RenderSettings.ambientGroundColor,ambientIntensity=RenderSettings.ambientIntensity,
                reflectionIntensity=RenderSettings.reflectionIntensity,sun=RenderSettings.sun,skybox=RenderSettings.skybox,
                ambientProbe=RenderSettings.ambientProbe,fog=RenderSettings.fog,fogColor=RenderSettings.fogColor,fogMode=RenderSettings.fogMode,
                fogDensity=RenderSettings.fogDensity,fogStart=RenderSettings.fogStartDistance,fogEnd=RenderSettings.fogEndDistance };
            public void Apply()
            {
                RenderSettings.ambientMode=mode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;
                RenderSettings.ambientGroundColor=ground;RenderSettings.ambientIntensity=ambientIntensity;
                RenderSettings.reflectionIntensity=reflectionIntensity;RenderSettings.sun=sun;RenderSettings.skybox=skybox;
                RenderSettings.ambientProbe=ambientProbe;
                RenderSettings.fog=fog;RenderSettings.fogColor=fogColor;RenderSettings.fogMode=fogMode;
                RenderSettings.fogDensity=fogDensity;RenderSettings.fogStartDistance=fogStart;RenderSettings.fogEndDistance=fogEnd;
            }
        }
    }
}
