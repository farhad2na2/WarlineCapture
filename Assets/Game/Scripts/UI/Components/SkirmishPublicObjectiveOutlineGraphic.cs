using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    // Screen-space display of the shared authored public perimeter only.
    public sealed class SkirmishPublicObjectiveOutlineGraphic : MaskableGraphic
    {
        private Vector2[] points=System.Array.Empty<Vector2>();
        public void Present(Vector2[] localPoints)
        {points=localPoints??System.Array.Empty<Vector2>();raycastTarget=false;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();if(points.Length<3)return;
            for(int i=0;i<points.Length;i++)
            {
                Vector2 a=points[i],b=points[(i+1)%points.Length],delta=b-a;
                if(delta.sqrMagnitude<.01f)continue;
                Vector2 normal=new Vector2(-delta.y,delta.x).normalized*1.5f;int start=helper.currentVertCount;
                helper.AddVert(a-normal,color,Vector2.zero);helper.AddVert(a+normal,color,Vector2.zero);
                helper.AddVert(b+normal,color,Vector2.zero);helper.AddVert(b-normal,color,Vector2.zero);
                helper.AddTriangle(start,start+1,start+2);helper.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
