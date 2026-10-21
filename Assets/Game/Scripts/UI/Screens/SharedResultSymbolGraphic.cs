using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    // Native symbols following the existing military UI, with semantic status colors.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SharedResultSymbolGraphic : MaskableGraphic
    {
        public enum Kind { Target, Squad, Shield, Document, Check, Cross, Minus, OutcomeSuccess, OutcomeFailure, Replay, Clock, Save, Experience }
        [SerializeField] private Kind symbol;
        public void Configure(Kind value, Color ink) { symbol = value; color = ink; raycastTarget = false; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();
            if (symbol == Kind.Target)
            {
                Ring(v, new Vector2(.5f,.5f), .31f, .255f, 32);
                Line(v,new(.5f,.04f),new(.5f,.31f),.055f); Line(v,new(.5f,.69f),new(.5f,.96f),.055f);
                Line(v,new(.04f,.5f),new(.31f,.5f),.055f); Line(v,new(.69f,.5f),new(.96f,.5f),.055f);
            }
            else if (symbol == Kind.Squad)
            {
                foreach(float x in new[]{.2f,.5f,.8f})
                {
                    Disc(v,new(x,.75f),.115f);Quad(v,x-.12f,.14f,.24f,.32f);
                    Triangle(v,new(x-.12f,.46f),new(x-.06f,.56f),new(x+.12f,.46f));Triangle(v,new(x+.12f,.46f),new(x-.06f,.56f),new(x+.06f,.56f));
                }
            }
            else if (symbol is Kind.Shield or Kind.OutcomeSuccess or Kind.OutcomeFailure)
            {
                Vector2[] p={new(.5f,.94f),new(.9f,.75f),new(.85f,.32f),new(.5f,.05f),new(.15f,.32f),new(.1f,.75f)};
                for(int i=0;i<p.Length;i++)Line(v,p[i],p[(i+1)%p.Length],.065f);
                if(symbol==Kind.OutcomeSuccess)
                {
                    Vector2[] face={new(.5f,.80f),new(.72f,.69f),new(.69f,.39f),new(.5f,.23f),new(.31f,.39f),new(.28f,.69f)};
                    for(int i=0;i<face.Length;i++)Triangle(v,new(.5f,.55f),face[i],face[(i+1)%face.Length]);
                }
                if(symbol==Kind.OutcomeFailure)Cross(v,.70f);
            }
            else if(symbol==Kind.Document)
            {
                Vector2[] p={new(.19f,.08f),new(.19f,.92f),new(.63f,.92f),new(.82f,.73f),new(.82f,.08f)};
                for(int i=0;i<p.Length;i++)Triangle(v,new(.5f,.5f),p[i],p[(i+1)%p.Length]);
                for(int i=0;i<3;i++)LineInk(v,new(.32f,.29f+i*.17f),new(.69f,.29f+i*.17f),.065f,new Color32(4,20,28,255));
            }
            else if(symbol==Kind.Clock)
            {Ring(v,new(.5f,.5f),.43f,.38f,48);Line(v,new(.5f,.5f),new(.5f,.79f),.06f);Line(v,new(.5f,.5f),new(.71f,.38f),.06f);}
            else if(symbol==Kind.Save)
            {Line(v,new(.16f,.08f),new(.16f,.92f),.07f);Line(v,new(.16f,.92f),new(.70f,.92f),.07f);Line(v,new(.70f,.92f),new(.85f,.76f),.07f);Line(v,new(.85f,.76f),new(.85f,.08f),.07f);Line(v,new(.85f,.08f),new(.16f,.08f),.07f);Quad(v,.30f,.61f,.32f,.20f);Quad(v,.30f,.20f,.40f,.27f);}
            else if(symbol==Kind.Experience)
            {for(int i=0;i<2;i++){float y=.22f+i*.33f;Line(v,new(.15f,y),new(.5f,y+.31f),.12f);Line(v,new(.5f,y+.31f),new(.85f,y),.12f);}}
            else if(symbol==Kind.Check)Check(v,.8f);
            else if(symbol==Kind.Cross)Cross(v,.7f);
            else if(symbol==Kind.Minus)Line(v,new(.2f,.5f),new(.8f,.5f),.09f);
            else if(symbol==Kind.Replay)
            {Ring(v,new(.5f,.5f),.35f,.28f,30);Triangle(v,new(.12f,.83f),new(.13f,.52f),new(.40f,.69f));}
        }
        void Check(VertexHelper v,float scale){Vector2 C(Vector2 p)=>new Vector2(.5f,.5f)+(p-new Vector2(.5f,.5f))*scale;Line(v,C(new(.15f,.47f)),C(new(.41f,.23f)),.14f*scale);Line(v,C(new(.41f,.23f)),C(new(.88f,.78f)),.14f*scale);}
        void Cross(VertexHelper v,float scale){Vector2 a=new(.5f-scale*.36f,.5f-scale*.36f),b=new(.5f+scale*.36f,.5f+scale*.36f);Line(v,a,b,.09f*scale);Line(v,new(a.x,b.y),new(b.x,a.y),.14f*scale);}
        void Quad(VertexHelper v,float x,float y,float w,float h){Triangle(v,new(x,y),new(x+w,y),new(x,y+h));Triangle(v,new(x+w,y),new(x+w,y+h),new(x,y+h));}
        void Disc(VertexHelper v,Vector2 c,float r){for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Triangle(v,c,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r);}}
        void Ring(VertexHelper v,Vector2 c,float outer,float inner,int count){for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;Vector2 p=new(Mathf.Cos(a),Mathf.Sin(a)),q=new(Mathf.Cos(b),Mathf.Sin(b));Triangle(v,c+p*outer,c+p*inner,c+q*outer);Triangle(v,c+q*outer,c+p*inner,c+q*inner);}}
        void Line(VertexHelper v,Vector2 a,Vector2 b,float w){Vector2 d=b-a,n=new Vector2(-d.y,d.x).normalized*w*.5f;Triangle(v,a+n,a-n,b+n);Triangle(v,b+n,a-n,b-n);}
        void LineInk(VertexHelper v,Vector2 a,Vector2 b,float width,Color ink){Vector2 d=b-a,n=new Vector2(-d.y,d.x).normalized*width*.5f;TriangleInk(v,a+n,a-n,b+n,ink);TriangleInk(v,b+n,a-n,b-n,ink);}
        void Triangle(VertexHelper v,Vector2 a,Vector2 b,Vector2 c)=>TriangleInk(v,a,b,c,color);
        void TriangleInk(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Color ink){Rect r=rectTransform.rect;Vector2 P(Vector2 p)=>r.min+Vector2.Scale(r.size,p);int i=v.currentVertCount;v.AddVert(P(a),ink,Vector2.zero);v.AddVert(P(b),ink,Vector2.zero);v.AddVert(P(c),ink,Vector2.zero);v.AddTriangle(i,i+1,i+2);}
    }
}
