using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed class SplitFrontLauncherHudView : MonoBehaviour
    {
        private void LateUpdate()
        {
            bool active=UiShellRuntimeGateway.TryReadSplitFrontLauncher(out var model)&&model.Active;
            PresentRings(active,in model);
        }
        private LineRenderer[] rings;
        private Material ringMaterial;
        private void PresentRings(bool active,in Game.UI.Contracts.UiSplitFrontLauncherModel model)
        {
            if(rings==null&&active)
            {
                rings=new LineRenderer[4];ringMaterial=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.HideAndDontSave};
                for(int i=0;i<4;i++)
                {
                    var root=new GameObject(new[]{"Minimum missile range","Maximum missile range","Missile impact area","Protected civilian area"}[i]);
                    root.transform.SetParent(transform,false);var line=root.AddComponent<LineRenderer>();rings[i]=line;line.sharedMaterial=ringMaterial;
                    line.useWorldSpace=true;line.loop=true;line.positionCount=96;line.widthMultiplier=.35f;
                    line.startColor=line.endColor=i==0?new Color(1,.55f,.15f,.85f):i==1?new Color(0,.8f,1,.65f):i==2?new Color(1,.25f,.12f,.95f):new Color(.55f,.85f,.3f,.9f);
                }
            }
            if(rings==null)return;
            for(int i=0;i<4;i++)
            {
                bool visible=active&&(i<2?model.ShowRange:i==2?model.ShowImpact:true);rings[i].enabled=visible;if(!visible)continue;
                Vector3 center=i<2?model.Launcher:i==2?model.Impact:model.ProtectedCenter;
                float radius=i==0?model.MinimumRange:i==1?model.MaximumRange:i==2?model.ImpactRadius:model.ProtectedRadius;
                center.y+=.6f;for(int n=0;n<96;n++){float a=n*Mathf.PI*2/96;rings[i].SetPosition(n,center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
            }
        }
        private void OnDestroy(){if(ringMaterial!=null)Destroy(ringMaterial);}
    }
}
