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
        public bool Earned => kind==Kind.Medal&&earned;
        public void Configure(Kind value,bool shown,int medalTier=0)
        {kind=value;earned=shown;tier=medalTier;raycastTarget=false;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!earned&&kind!=Kind.Medal)return;
            Rect r=rectTransform.rect;Vector2 center=r.center;float radius=Mathf.Min(r.width,r.height)*.47f;
            Color ink=new Color32(4,24,30,255);
            if(kind==Kind.Medal)
            {
                if(!earned){Ring(vh,center,radius,radius*.86f,new Color32(151,183,200,255),new Color32(45,78,96,255));return;}
                Color[] colors={new Color32(220,123,56,255),new Color32(211,232,247,255),new Color32(255,199,20,255),new Color32(247,58,48,255)};
                Color c=colors[Mathf.Clamp(tier,0,3)];
                Color light=Color.Lerp(c,Color.white,.65f),dark=Color.Lerp(c,ink,.55f);
                Ring(vh,center,radius,radius*.78f,light,dark);
                Disc(vh,center,radius*.78f,Color.Lerp(c,ink,.22f),dark);
                for(int i=0;i<10;i++)
                {
                    float a=(90+i*36)*Mathf.Deg2Rad,b=(90+(i+1)*36)*Mathf.Deg2Rad;
                    float ra=radius*(i%2==0?.74f:.32f),rb=radius*((i+1)%2==0?.74f:.32f);
                    Triangle(vh,center,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*ra,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*rb,i%2==0?light:c);
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
        private static void Ring(VertexHelper vh,Vector2 center,float outer,float inner,Color top,Color bottom)
        {
            for(int i=0;i<40;i++)
            {
                float a=i*Mathf.PI/20,b=(i+1)*Mathf.PI/20;
                Vector2 pa=new(Mathf.Cos(a),Mathf.Sin(a)),pb=new(Mathf.Cos(b),Mathf.Sin(b));
                Color c=Color.Lerp(bottom,top,(pa.y+1)*.5f);
                Triangle(vh,center+pa*outer,center+pa*inner,center+pb*outer,c);Triangle(vh,center+pb*outer,center+pa*inner,center+pb*inner,c);
            }
        }
        private static void Disc(VertexHelper vh,Vector2 center,float radius,Color top,Color bottom)
        {for(int i=0;i<40;i++){float a=i*Mathf.PI/20,b=(i+1)*Mathf.PI/20;Triangle(vh,center,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,Color.Lerp(bottom,top,(Mathf.Sin(a)+1)*.5f));}}
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Triangle(vh,a+n,a-n,b+n,color);Triangle(vh,b+n,a-n,b-n,color);}
        private static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color color)
        {int i=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);}
    }
}
