using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI.Runtime
{
    public sealed partial class MissionExtractionHudView
    {
        private GameObject markerRoot;
        private Material markerMaterial;
        private LineRenderer landingRing, departureRing;
        private TextMeshPro landingLabel, departureLabel;
        private Quaternion markerRotation;

        private void RefreshWorldMarkers(bool active, in UiMissionExtractionModel model)
        {
            if (!active) { if(markerRoot!=null) markerRoot.SetActive(false); return; }
            if(markerRoot==null) CreateWorldMarkers();
            markerRotation=model.MarkerRotation;
            markerRoot.SetActive(true);
            var landingColor=model.Contested ? new Color32(255,79,28,255) : model.Cleared ? new Color32(94,224,79,255) : new Color32(255,196,36,255);
            DrawZone(landingRing,landingLabel,model.LandingCenter,model.LandingRadius,landingColor,
                UiShellRuntimeGateway.Localization.Get(model.Contested ? "mission.m04.hud.contested" : model.Cleared ? "mission.m04.hud.cleared" : "mission.m04.focus.landing"));
            DrawZone(departureRing,departureLabel,model.DepartureCenter,model.DepartureRadius,
                model.Cleared ? new Color32(94,224,79,255) : new Color32(0,198,235,255),
                UiShellRuntimeGateway.Localization.Get("mission.m04.focus.departure"));
        }

        private void CreateWorldMarkers()
        {
            markerRoot=new GameObject("M04 Rescue Zones");
            var shader=Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Sprites/Default");
            markerMaterial=new Material(shader) {name="M04 Mission Zone",hideFlags=HideFlags.HideAndDontSave,renderQueue=3120};
            markerMaterial.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
            markerMaterial.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            markerMaterial.SetInt("_Cull",(int)CullMode.Off);
            markerMaterial.SetInt("_ZWrite",0);
            markerMaterial.SetInt("_ZTest",(int)CompareFunction.LessEqual);
            CreateZone("Landing",out landingRing,out landingLabel);
            CreateZone("Departure",out departureRing,out departureLabel);
        }

        private void CreateZone(string name,out LineRenderer ring,out TextMeshPro label)
        {
            var zone=new GameObject(name);zone.transform.SetParent(markerRoot.transform,false);
            ring=zone.AddComponent<LineRenderer>();ring.sharedMaterial=markerMaterial;
            ring.loop=true;ring.useWorldSpace=true;ring.positionCount=64;ring.widthMultiplier=.2f;
            ring.shadowCastingMode=ShadowCastingMode.Off;ring.receiveShadows=false;ring.allowOcclusionWhenDynamic=false;
            var caption=new GameObject(name+"Label");caption.transform.SetParent(markerRoot.transform,false);
            label=caption.AddComponent<TextMeshPro>();label.fontSize=18;label.alignment=TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta=new Vector2(30,5);label.color=Color.white;
            label.textWrappingMode=TextWrappingModes.NoWrap;
            // Use the localized binding's Farsi shaping/font fallback, as in the HUD.
            caption.AddComponent<V3LocalizedTextBindingView>();
        }

        private void DrawZone(LineRenderer ring,TextMeshPro label,Vector3 center,float radius,Color color,string text)
        {
            center.y+=.3f;
            for(int i=0;i<64;i++)
            {
                float angle=i*Mathf.PI*2/64;
                ring.SetPosition(i,center+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));
            }
            ring.startColor=ring.endColor=color;
            label.transform.position=center+Vector3.up*3;
            label.transform.rotation=markerRotation;
            UiLocalizedText.Set(label,text);
        }

        private void DestroyWorldMarkers()
        {
            if(markerRoot!=null) Destroy(markerRoot);
            if(markerMaterial!=null) Destroy(markerMaterial);
            markerRoot=null;markerMaterial=null;
        }
    }
}
