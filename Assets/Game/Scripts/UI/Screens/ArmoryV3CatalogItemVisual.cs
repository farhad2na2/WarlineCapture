using UnityEngine;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ArmoryV3CatalogItemVisual : MonoBehaviour
    {
        [SerializeField] private V3GradientGraphic frame;

        private static readonly Color Top = new Color32(27, 36, 41, 255);
        private static readonly Color Bottom = new Color32(5, 11, 14, 255);
        private static readonly Color Border = new Color32(84, 101, 107, 255);
        private static readonly Color SelectedBorder = new Color32(26, 191, 239, 255);

        public void Configure(V3GradientGraphic targetFrame)
        {
            frame = targetFrame;
        }

        public void SetSelected(bool selected)
        {
            frame?.Configure(Top, Bottom, selected ? SelectedBorder : Border, 3f);
        }
    }
}
