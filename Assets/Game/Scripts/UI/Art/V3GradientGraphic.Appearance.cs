namespace Game.UI.Runtime
{
    public sealed partial class V3GradientGraphic
    {
        // Copy only existing chrome values. Never clone the source Button,
        // listeners, labels, portrait children or control hierarchy.
        public void CopyAppearanceFrom(V3GradientGraphic source)
        {
            if(source==null)return;
            ConfigureCorners(source.topLeftColor,source.topRightColor,source.bottomLeftColor,source.bottomRightColor,source.borderColor,source.borderWidth);
            color=source.color;material=source.material;maskable=source.maskable;
        }
    }
}
