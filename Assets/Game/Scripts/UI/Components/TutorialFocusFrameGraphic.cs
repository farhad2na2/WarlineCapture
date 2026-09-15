using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Two gold keylines separated by a dark gutter. No scale or thickness animation.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialFocusFrameGraphic : MaskableGraphic
    {
        private float intensity = 1;
        internal void SetIntensity(float value)
        {
            if (Mathf.Abs(intensity-value)<.005f) return;
            intensity=value; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r=rectTransform.rect;
            float pixel=1/Mathf.Max(.01f,canvas!=null ? canvas.rootCanvas.scaleFactor : 1);
            Ring(vh,r,9*pixel,new Color(.035f,.045f,.035f,1));
            Ring(vh,Inset(r,pixel),2*pixel,new Color(1,.77f,.015f,intensity));
            Ring(vh,Inset(r,6*pixel),2*pixel,new Color(1,.92f,.19f,intensity));
        }
        private static Rect Inset(Rect r,float amount) => Rect.MinMaxRect(r.xMin+amount,r.yMin+amount,r.xMax-amount,r.yMax-amount);
        private static void Ring(VertexHelper vh,Rect r,float width,Color color)
        {
            Quad(vh,new Rect(r.xMin,r.yMin,r.width,width),color);
            Quad(vh,new Rect(r.xMin,r.yMax-width,r.width,width),color);
            Quad(vh,new Rect(r.xMin,r.yMin+width,width,r.height-2*width),color);
            Quad(vh,new Rect(r.xMax-width,r.yMin+width,width,r.height-2*width),color);
        }
        private static void Quad(VertexHelper vh,Rect r,Color color)
        {
            int i=vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin,r.yMin),color,Vector2.zero); vh.AddVert(new Vector2(r.xMin,r.yMax),color,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),color,Vector2.zero); vh.AddVert(new Vector2(r.xMax,r.yMin),color,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
        }
    }
}
