using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Native vector status marks; no new texture atlas or gameplay state.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MissionAtlasStatusGraphic : MaskableGraphic
    {
        public enum Kind { Check, Selected, Medal }
        [SerializeField] private Kind kind;
        [SerializeField] private bool earned;
        [SerializeField] private int tier;
        public void Configure(Kind value,bool shown,int medalTier=0)
        {kind=value;earned=shown;tier=medalTier;raycastTarget=false;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!earned)return;
            Rect r=rectTransform.rect;Vector2 center=r.center;float radius=Mathf.Min(r.width,r.height)*.47f;
            Color ink=new Color32(4,24,30,255);
            if(kind==Kind.Medal)
            {
                Color[] colors={new Color32(187,113,66,255),new Color32(213,226,235,255),new Color32(250,197,47,255),new Color32(221,66,60,255)};
                Color c=colors[Mathf.Clamp(tier,0,3)];
                for(int i=0;i<10;i++)
                {
                    float a=(90+i*36)*Mathf.Deg2Rad,b=(90+(i+1)*36)*Mathf.Deg2Rad;
                    float ra=radius*(i%2==0?.72f:.32f),rb=radius*((i+1)%2==0?.72f:.32f);
                    Triangle(vh,center,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*ra,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*rb,c);
                }
                return;
            }
            Color face=kind==Kind.Check?new Color32(92,214,34,255):MissionAtlasScreenView.Cyan;
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;
                Triangle(vh,center,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,face);
            }
            Line(vh,center+new Vector2(-.45f,-.02f)*radius,center+new Vector2(-.12f,-.34f)*radius,radius*.22f,ink);
            Line(vh,center+new Vector2(-.12f,-.34f)*radius,center+new Vector2(.49f,.37f)*radius,radius*.22f,ink);
        }
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Triangle(vh,a+n,a-n,b+n,color);Triangle(vh,b+n,a-n,b-n,color);}
        private static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color color)
        {int i=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);}
    }
}
