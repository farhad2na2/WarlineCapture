using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Projects the real interaction radius; decoration never consumes input.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OperationsScanAreaGraphic : MaskableGraphic
    {
        private const int Segments = 48;
        private readonly Vector2[,] points = new Vector2[3,Segments];
        private readonly bool[] visible = new bool[3];
        public void Present(int index, Camera camera, Vector3 center, float radius, bool complete)
        {
            if (index < 0 || index >= 3 || camera == null) return;
            visible[index] = !complete;
            for (int i=0;i<Segments;i++)
            {
                float angle = i*Mathf.PI*2/Segments;
                var screen = camera.WorldToScreenPoint(center + new Vector3(Mathf.Cos(angle)*radius,.12f,Mathf.Sin(angle)*radius));
                if (screen.z <= 0) visible[index] = false;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screen,null,out points[index,i]);
            }
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Color32 tint = new Color32(0,195,242,190);
            for (int site=0;site<3;site++)
                if (visible[site]) for (int i=0;i<Segments;i++)
                {
                    Vector2 a=points[site,i], b=points[site,(i+1)%Segments];
                    Rect bounds=rectTransform.rect;
                    if (!bounds.Contains(a) || !bounds.Contains(b) ||
                        a.y < bounds.yMin+bounds.height*.25f || b.y < bounds.yMin+bounds.height*.25f ||
                        a.y > bounds.yMax-bounds.height*.12f || b.y > bounds.yMax-bounds.height*.12f ||
                        a.x > bounds.xMax-520 || b.x > bounds.xMax-520) continue;
                    Vector2 delta=b-a; Vector2 normal=new Vector2(-delta.y,delta.x).normalized*1.8f;
                    int v=vh.currentVertCount;
                    vh.AddVert(a-normal,tint,Vector2.zero); vh.AddVert(a+normal,tint,Vector2.zero);
                    vh.AddVert(b+normal,tint,Vector2.zero); vh.AddVert(b-normal,tint,Vector2.zero);
                    vh.AddTriangle(v,v+1,v+2); vh.AddTriangle(v,v+2,v+3);
                }
        }
    }
}
