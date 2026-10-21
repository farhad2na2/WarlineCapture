using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    // Native symbols following the existing military UI, with semantic status colors.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SharedResultSymbolGraphic : MaskableGraphic
    {
        public enum Kind { Target, Squad, Shield, Document, Check, Cross, Minus, OutcomeSuccess, OutcomeFailure, Replay }
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
                foreach(float x in new[]{.2f,.5f,.8f}) { Disc(v,new(x,.72f),.115f); Quad(v,x-.12f,.14f,.24f,.40f); }
            }
            else if (symbol is Kind.Shield or Kind.OutcomeSuccess or Kind.OutcomeFailure)
            {
                Vector2[] p={new(.5f,.94f),new(.9f,.75f),new(.85f,.32f),new(.5f,.05f),new(.15f,.32f),new(.1f,.75f)};
                for(int i=0;i<p.Length;i++)Line(v,p[i],p[(i+1)%p.Length],.065f);
                if(symbol==Kind.OutcomeSuccess)Check(v,.38f);
                if(symbol==Kind.OutcomeFailure)Cross(v,.34f);
            }
            else if(symbol==Kind.Document)
            {Line(v,new(.22f,.08f),new(.22f,.91f),.06f);Line(v,new(.22f,.91f),new(.65f,.91f),.06f);Line(v,new(.65f,.91f),new(.82f,.72f),.06f);Line(v,new(.82f,.72f),new(.82f,.08f),.06f);Line(v,new(.82f,.08f),new(.22f,.08f),.06f);for(int i=0;i<3;i++)Line(v,new(.33f,.31f+i*.16f),new(.7f,.31f+i*.16f),.05f);}
            else if(symbol==Kind.Check)Check(v,.8f);
            else if(symbol==Kind.Cross)Cross(v,.7f);
            else if(symbol==Kind.Minus)Line(v,new(.2f,.5f),new(.8f,.5f),.09f);
            else if(symbol==Kind.Replay)
            {Ring(v,new(.5f,.5f),.35f,.28f,30);Triangle(v,new(.12f,.83f),new(.13f,.52f),new(.40f,.69f));}
        }
        void Check(VertexHelper v,float scale){Vector2 C(Vector2 p)=>new Vector2(.5f,.5f)+(p-new Vector2(.5f,.5f))*scale;Line(v,C(new(.15f,.47f)),C(new(.41f,.23f)),.09f*scale);Line(v,C(new(.41f,.23f)),C(new(.88f,.78f)),.09f*scale);}
        void Cross(VertexHelper v,float scale){Vector2 a=new(.5f-scale*.36f,.5f-scale*.36f),b=new(.5f+scale*.36f,.5f+scale*.36f);Line(v,a,b,.09f*scale);Line(v,new(a.x,b.y),new(b.x,a.y),.09f*scale);}
        void Quad(VertexHelper v,float x,float y,float w,float h){Triangle(v,new(x,y),new(x+w,y),new(x,y+h));Triangle(v,new(x+w,y),new(x+w,y+h),new(x,y+h));}
        void Disc(VertexHelper v,Vector2 c,float r){for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Triangle(v,c,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r);}}
        void Ring(VertexHelper v,Vector2 c,float outer,float inner,int count){for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;Vector2 p=new(Mathf.Cos(a),Mathf.Sin(a)),q=new(Mathf.Cos(b),Mathf.Sin(b));Triangle(v,c+p*outer,c+p*inner,c+q*outer);Triangle(v,c+q*outer,c+p*inner,c+q*inner);}}
        void Line(VertexHelper v,Vector2 a,Vector2 b,float w){Vector2 d=b-a,n=new Vector2(-d.y,d.x).normalized*w*.5f;Triangle(v,a+n,a-n,b+n);Triangle(v,b+n,a-n,b-n);}
        void Triangle(VertexHelper v,Vector2 a,Vector2 b,Vector2 c){Rect r=rectTransform.rect;Vector2 P(Vector2 p)=>r.min+Vector2.Scale(r.size,p);int i=v.currentVertCount;v.AddVert(P(a),color,Vector2.zero);v.AddVert(P(b),color,Vector2.zero);v.AddVert(P(c),color,Vector2.zero);v.AddTriangle(i,i+1,i+2);}
    }
}
