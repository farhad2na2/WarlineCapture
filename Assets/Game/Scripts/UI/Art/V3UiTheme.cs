using UnityEngine;

namespace Game.UI.Runtime
{
    [CreateAssetMenu(menuName = "Game/UI/V3 Theme", fileName = "V3UiTheme")]
    public sealed class V3UiTheme : ScriptableObject
    {
        [Header("Surfaces")]
        [SerializeField] private Color canvas = new Color32(7, 12, 15, 255);
        [SerializeField] private Color surface = new Color32(17, 25, 30, 255);
        [SerializeField] private Color surfaceRaised = new Color32(25, 35, 41, 255);
        [SerializeField] private Color linePrimary = new Color32(178, 190, 194, 255);

        [Header("Text")]
        [SerializeField] private Color textPrimary = new Color32(245, 246, 242, 255);
        [SerializeField] private Color textMuted = new Color32(160, 172, 176, 255);

        [Header("Semantic accents")]
        [SerializeField] private Color cyan = new Color32(0, 188, 224, 255);
        [SerializeField] private Color blue = new Color32(28, 123, 194, 255);
        [SerializeField] private Color green = new Color32(48, 166, 69, 255);
        [SerializeField] private Color amber = new Color32(243, 174, 0, 255);
        [SerializeField] private Color orangeRed = new Color32(226, 63, 22, 255);
        [SerializeField] private Color violet = new Color32(111, 76, 185, 255);

        [Header("Interaction")]
        [SerializeField] private Color normal = new Color32(255, 255, 255, 255);
        [SerializeField] private Color highlighted = new Color32(220, 247, 255, 255);
        [SerializeField] private Color pressed = new Color32(140, 216, 235, 255);
        [SerializeField] private Color selected = new Color32(0, 188, 224, 255);
        [SerializeField] private Color disabled = new Color32(94, 104, 108, 160);
        [SerializeField] private Color dimmer = new Color32(0, 0, 0, 150);

        public Color Canvas => canvas;
        public Color Surface => surface;
        public Color SurfaceRaised => surfaceRaised;
        public Color LinePrimary => linePrimary;
        public Color TextPrimary => textPrimary;
        public Color TextMuted => textMuted;
        public Color Cyan => cyan;
        public Color Blue => blue;
        public Color Green => green;
        public Color Amber => amber;
        public Color OrangeRed => orangeRed;
        public Color Violet => violet;
        public Color Normal => normal;
        public Color Highlighted => highlighted;
        public Color Pressed => pressed;
        public Color Selected => selected;
        public Color Disabled => disabled;
        public Color Dimmer => dimmer;
    }
}
