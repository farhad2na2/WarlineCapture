using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Game.Composition
{
    /// <summary>Fixed, reversible desert daylight for the short S004 match.</summary>
    public sealed class SkirmishS004Daylight : MonoBehaviour
    {
        private Light sun, previousSun;
        private Color color, sky, equator, ground;
        private float intensity, strength;
        private Quaternion rotation;
        private LightShadows shadows;
        private AmbientMode ambient;
        private bool fog, configured;
        private readonly Dictionary<Renderer,Material[]> originalGround=new();
        private readonly Dictionary<Material,Material> groundMaterials=new();

        public void Configure(Light directionalLight)
        {
            if (configured || directionalLight == null) return;
            sun=directionalLight; previousSun=RenderSettings.sun;
            color=sun.color; intensity=sun.intensity; rotation=sun.transform.rotation;
            shadows=sun.shadows; strength=sun.shadowStrength;
            sky=RenderSettings.ambientSkyColor; equator=RenderSettings.ambientEquatorColor;
            ground=RenderSettings.ambientGroundColor; ambient=RenderSettings.ambientMode; fog=RenderSettings.fog;
            configured=true;
            sun.color=new Color(1f,.98f,.92f); sun.intensity=1.85f;
            sun.transform.rotation=Quaternion.Euler(52,-35,0);
            sun.shadows=LightShadows.Soft; sun.shadowStrength=.85f;
            RenderSettings.sun=sun;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.53f,.61f,.69f);
            RenderSettings.ambientEquatorColor=new Color(.46f,.47f,.48f);
            RenderSettings.ambientGroundColor=new Color(.27f,.25f,.22f);
            RenderSettings.fog=false;
            var palette=Resources.Load<Material>("SkirmishS004SandGround");
            if(palette!=null)
                foreach(var renderer in GetComponentsInChildren<MeshRenderer>(true))
                {
                    bool isGround=false;
                    for(var parent=renderer.transform; parent!=null && parent!=transform.parent; parent=parent.parent)
                        if(parent.name=="Ground" || parent.name=="GroundHills") { isGround=true; break; }
                    if(!isGround) continue;
                    var materials=renderer.sharedMaterials;
                    originalGround.Add(renderer,materials);
                    var revised=(Material[])materials.Clone();
                    for(int i=0;i<revised.Length;i++)
                    {
                        var original=materials[i]; if(original==null) continue;
                        if(!groundMaterials.TryGetValue(original,out var replacement))
                        {
                            replacement=new Material(palette) {hideFlags=HideFlags.DontSave};
                            // Keep the atlas for this source mesh, while adding continuous sand detail.
                            if(original.HasProperty("_BaseMap")) replacement.SetTexture("_BaseMap",original.GetTexture("_BaseMap"));
                            else if(original.HasProperty("_MainTex")) replacement.SetTexture("_BaseMap",original.GetTexture("_MainTex"));
                            groundMaterials.Add(original,replacement);
                        }
                        revised[i]=replacement;
                    }
                    renderer.sharedMaterials=revised;
                }
            Debug.Log("[S004Daylight] result=Applied fixed=1 groundRenderers="+originalGround.Count);
        }

        private void OnDestroy()
        {
            if (!configured) return;
            configured=false;
            foreach(var entry in originalGround) if(entry.Key!=null) entry.Key.sharedMaterials=entry.Value;
            originalGround.Clear();
            foreach(var material in groundMaterials.Values)
                if(Application.isPlaying) Destroy(material); else DestroyImmediate(material);
            groundMaterials.Clear();
            if(sun!=null) { sun.color=color; sun.intensity=intensity; sun.transform.rotation=rotation;
                sun.shadows=shadows; sun.shadowStrength=strength; }
            RenderSettings.sun=previousSun; RenderSettings.ambientMode=ambient;
            RenderSettings.ambientSkyColor=sky; RenderSettings.ambientEquatorColor=equator;
            RenderSettings.ambientGroundColor=ground; RenderSettings.fog=fog;
        }
    }
}
