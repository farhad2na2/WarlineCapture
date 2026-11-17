using System;

namespace Game.UI.Runtime
{
    public static partial class TheatreSeatGeometry
    {
        // Small compact districts need visible terrain around the native portrait.
        // Open inset corners and use a small free tail only where every existing
        // frame, aspect and halo constraint holds. Cards and pins never move.
        private static Vec2[] GrowSagaPortraitCoast(Vec2[] shape, Box card, Box pin,
            Vec2[][] others, Box cue, float width, float height, Box detail, Box hint, float expected)
        {
            if (shape == null || card.W < 1 || width / Math.Max(1f,height) >= 1.55f)
                return shape;
            double twiceArea = 0;
            for (int i=0;i<shape.Length;i++)
            { var a=shape[i]; var b=shape[(i+1)%shape.Length]; twiceArea += a.X*b.Y-b.X*a.Y; }
            if (Math.Abs(twiceArea)*.5 >= 40000 || !CoastVisualCentre(shape,out float cx,out float cy))
                return shape;
            BoundsOf(shape,out float left,out float top,out float right,out float bottom);
            // Keep the existing frame: open the coast's inset corners so the
            // portrait has visible terrain around it without consuming halo gaps.
            foreach(float amount in new[]{.5f,.4f,.3f,.29f,.28f,.27f,.26f,.25f,.24f,.23f,.22f,.21f,.2f,.1f})
            {
                float mx=(left+right)*.5f, my=(top+bottom)*.5f;
                float rx=(right-left)*.5f, ry=(bottom-top)*.5f;
                var grown=new Vec2[shape.Length];
                for(int i=0;i<shape.Length;i++)
                {
                    float x=(shape[i].X-mx)/rx, y=(shape[i].Y-my)/ry;
                    grown[i]=new Vec2(mx+rx*(x+amount*x*(1-x*x)),my+ry*(y+amount*y*(1-y*y)));
                }
                foreach (float stretch in new[]{1.033f,1f})
                {
                    var candidate=new Vec2[grown.Length];
                    for(int i=0;i<grown.Length;i++) candidate[i]=new Vec2(grown[i].X,top+(grown[i].Y-top)*stretch);
                    if (!FrameAndCentre(candidate,card,width,height,detail,hint,expected) ||
                        TerritoryReadsRectangular(candidate) || !RestsInside(card,candidate,InsideMargin) ||
                        cue.W>1 && PolygonHitsRect(candidate,cue)) continue;
                    bool clear=true;
                    if (others != null) foreach(var other in others)
                        if(other!=null && DrawnDistrictGap(candidate,other)+.05f<12f) { clear=false; break; }
                    if (clear) return candidate;
                }
            }
            return shape;
        }
    }
}
