using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Compact, faceted objective symbols drawn in the menu's cyan/silver palette.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MissionAtlasObjectiveGraphic : MaskableGraphic
    {
        [SerializeField] private string objectiveId;
        public string ObjectiveId => objectiveId;
        public void Configure(string value) {objectiveId=value;raycastTarget=false;SetVerticesDirty();}
        private static readonly Color Light=new Color32(238,248,252,255), Silver=new Color32(150,181,197,255), Shade=new Color32(64,103,125,255), Ink=new Color32(5,27,39,255);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(objectiveId=="BA")Star(vh,5,new Color32(10,204,250,255),new Color32(119,233,255,255),new Color32(0,105,168,255),.46f);
            else if(objectiveId=="BT")
            {
                Star(vh,9,Light,Color.white,Silver,.42f);
                Polygon(vh,Ink,new Vector2(.23f,.06f),new Vector2(.37f,.27f),new Vector2(.48f,.15f),new Vector2(.62f,.31f),new Vector2(.77f,.06f));
                Polygon(vh,Silver,new Vector2(.14f,.04f),new Vector2(.30f,.15f),new Vector2(.42f,.09f),new Vector2(.56f,.20f),new Vector2(.68f,.08f),new Vector2(.88f,.04f));
            }
            else if(objectiveId=="FC")
            {
                Quad(vh,.17f,.08f,.045f,.81f,Silver);Quad(vh,.17f,.08f,.018f,.81f,Light);
                Polygon(vh,Light,new Vector2(.215f,.83f),new Vector2(.40f,.89f),new Vector2(.61f,.78f),new Vector2(.89f,.82f),new Vector2(.89f,.39f),new Vector2(.65f,.36f),new Vector2(.42f,.48f),new Vector2(.215f,.43f));
                Polygon(vh,Silver,new Vector2(.42f,.48f),new Vector2(.61f,.36f),new Vector2(.65f,.78f),new Vector2(.40f,.89f));
                Polygon(vh,Color.white,new Vector2(.215f,.83f),new Vector2(.40f,.89f),new Vector2(.42f,.78f),new Vector2(.215f,.72f));
            }
            else if(objectiveId=="CE")
            {
                Quad(vh,.06f,.28f,.55f,.51f,Light);Quad(vh,.08f,.30f,.51f,.46f,Silver);Quad(vh,.10f,.38f,.47f,.36f,Light);
                Polygon(vh,Light,new Vector2(.61f,.28f),new Vector2(.95f,.28f),new Vector2(.95f,.52f),new Vector2(.80f,.72f),new Vector2(.61f,.72f));
                Polygon(vh,Shade,new Vector2(.66f,.51f),new Vector2(.89f,.51f),new Vector2(.78f,.66f),new Vector2(.66f,.66f));
                Quad(vh,.04f,.24f,.93f,.07f,Silver);Quad(vh,.64f,.37f,.07f,.03f,Shade);
                Disc(vh,new Vector2(.25f,.24f),.115f,Ink);Disc(vh,new Vector2(.77f,.24f),.115f,Ink);
                Disc(vh,new Vector2(.25f,.24f),.073f,Light);Disc(vh,new Vector2(.77f,.24f),.073f,Light);
                Disc(vh,new Vector2(.25f,.24f),.038f,Silver);Disc(vh,new Vector2(.77f,.24f),.038f,Silver);
            }
        }
        private void Star(VertexHelper vh,int points,Color face,Color highlight,Color shadow,float inner)
        {
            Vector2 c=new(.5f,.5f);float radius=.47f;
            for(int i=0;i<points*2;i++)
            {
                float a=(90+i*180f/points)*Mathf.Deg2Rad,b=(90+(i+1)*180f/points)*Mathf.Deg2Rad;
                Vector2 pa=c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*(i%2==0?1:inner),pb=c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius*(i%2==0?inner:1);
                Triangle(vh,c,pa,pb,i%2==0?highlight:Color.Lerp(face,shadow,.35f));
            }
        }
        private void Quad(VertexHelper vh,float x,float y,float w,float h,Color color)=>Polygon(vh,color,new Vector2(x,y),new Vector2(x,y+h),new Vector2(x+w,y+h),new Vector2(x+w,y));
        private void Polygon(VertexHelper vh,Color color,params Vector2[] p)
        {for(int i=1;i<p.Length-1;i++)Triangle(vh,p[0],p[i],p[i+1],color);}
        private void Disc(VertexHelper vh,Vector2 c,float radius,Color color)
        {for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Triangle(vh,c,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,color);}}
        private void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color color)
        {
            Rect r=rectTransform.rect;Vector2 P(Vector2 p)=>r.min+Vector2.Scale(r.size,p);
            int i=vh.currentVertCount;vh.AddVert(P(a),color,Vector2.zero);vh.AddVert(P(b),color,Vector2.zero);vh.AddVert(P(c),color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);
        }
    }
}
