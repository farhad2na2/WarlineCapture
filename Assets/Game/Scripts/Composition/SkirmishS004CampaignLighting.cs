using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
        private bool hasRenderingLease;
        public static bool Supports(Game.Configs.OperationMapDefinition map) => map != null &&
            (map.OperationMapId == "opmap.skirmish.cityedgeairfield_prepared" ||
             map.SourceBinding.SourceOperationMapId == "opmap.skirmish.cityedgeairfield_prepared" ||
             map.OperationMapId == "opmap.skirmish.desert_base_01" ||
             map.SourceBinding.SourceOperationMapId == "opmap.skirmish.desert_base_01");
        // Lighting ownership follows the loaded source, independently of the
        // legacy map filter also used for Skirmish camera framing.
        public static bool HasAuthoredDaylight(Scene scene)
        {
            if(!scene.IsValid() || !scene.isLoaded)return false;
            foreach(var root in scene.GetRootGameObjects())
                foreach(var light in root.GetComponentsInChildren<Light>(false))
                    if(light.enabled && light.type==LightType.Directional && light.intensity>0)return true;
            return false;
        }
        public void Configure(Scene matchScene,Scene campaignScene)
        {
            if(configured || !HasAuthoredDaylight(campaignScene)) return;
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
            hasRenderingLease=Game.Runtime.SharedRuntimeRenderPipelineLease.AcquireCampaignShadows(this);
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
            // Quality selection can finish after source binding. The shared
            // runtime asset keeps that selection and its shadow override
            // through Replay without another per-match pipeline switch.
            if(!hasRenderingLease)
                hasRenderingLease=Game.Runtime.SharedRuntimeRenderPipelineLease.AcquireCampaignShadows(this);
            else
                Game.Runtime.SharedRuntimeRenderPipelineLease.EnsureSelected();
        }
        private void OnDestroy()
        {
            if(!configured) return;
            foreach(var item in matchLights) if(item.light!=null) item.light.enabled=item.enabled;
            if(hasRenderingLease)
            {
                Game.Runtime.SharedRuntimeRenderPipelineLease.ReleaseCampaignShadows(this);
                hasRenderingLease=false;
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
