using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public enum HudButtonIcon
    {
        Guide, Play, Stop, Camera, Continue, Alert, Skip, Back,
        Team, Landing, Departure, Cancel, YourBase, EnemyBase, Confirm, Commands
    }

    public enum HudButtonRole { Information, Play, Stop, Alert, Navigation, Neutral }

    /// <summary>Shared presentation for HUD actions; never changes their input handlers.</summary>
    [DisallowMultipleComponent]
    public sealed class HudIconButtonView : MonoBehaviour
    {
        public const float LabelSize = 24;
        private static readonly Sprite[] icons = new Sprite[16];
        private static Texture2D atlas;
        // Visible artwork centres measured from the v01 atlas (alpha > 100).
        // Cells include unequal transparent padding; centring the cell alone shifts the glyph.
        private static readonly float[] verticalArtworkOffsets =
        {
            .07894737f, .08213716f, .08532695f, .08692185f,
            .07894737f, .06937799f, .07097289f, .07256778f,
            .01674641f, -.00239234f, .00079745f, -.00079745f,
            -.05023923f, -.04864434f, -.03269537f, -.04385965f
        };
        private Button button;
        private TMP_Text label;
        private Image icon;
        private V3GradientGraphic surface;
        private string english, persian;
        private bool baseHeading;
        private HudButtonIcon kind;
        private HudButtonRole currentRole;
        private bool configured;

        public static Sprite LoadIcon(HudButtonIcon kind)
        {
            int index = (int)kind;
            if (icons[index] != null) return icons[index];
            if (atlas == null) atlas = Resources.Load<Texture2D>("HudButtonIcons/hud-action-icons-v01");
            if (atlas == null) return null;
            float width = atlas.width / 4f, height = atlas.height / 4f;
            icons[index] = Sprite.Create(atlas,
                new Rect(index % 4 * width, (3 - index / 4) * height, width, height),
                new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            icons[index].name = "HudAction_" + kind;
            return icons[index];
        }

        public static void Apply(Button target, HudButtonIcon kind, HudButtonRole role,
            string english = null, string persian = null, TMP_Text heading = null, bool baseHeading = false)
        {
            if (target == null) return;
            var view = target.GetComponent<HudIconButtonView>() ?? target.gameObject.AddComponent<HudIconButtonView>();
            view.button = target;
            view.label = heading != null ? heading : target.GetComponentInChildren<TMP_Text>(true);
            view.english = english; view.persian = persian; view.baseHeading = baseHeading;
            bool iconChanged = view.kind != kind;
            view.kind = kind;
            if (view.icon == null || view.surface == null) view.EnsureGraphics();
            else if (iconChanged) view.icon.sprite = LoadIcon(kind);
            if (!view.configured || view.currentRole != role) view.ApplyRole(role);
            view.currentRole = role; view.configured = true;
            view.Refresh();
        }

        private void EnsureGraphics()
        {
            if (surface == null)
            {
                var prior = transform.Find("HudRoleSurface");
                if (prior != null) surface = prior.GetComponent<V3GradientGraphic>();
                if (surface == null)
                {
                    var go = new GameObject("HudRoleSurface", typeof(RectTransform), typeof(V3GradientGraphic));
                    go.transform.SetParent(transform, false); go.transform.SetAsFirstSibling();
                    surface = go.GetComponent<V3GradientGraphic>();
                    var rect = (RectTransform)go.transform;
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
            }
            // The original selectable owns the hit target, locks and callbacks.
            surface.raycastTarget = true;
            foreach (var gradient in GetComponentsInChildren<V3GradientGraphic>(true))
                if (gradient != surface) gradient.enabled = false;
            button.targetGraphic = surface;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.82f, 1, 1);
            colors.pressedColor = new Color(.55f, .75f, .8f);
            colors.selectedColor = new Color(.8f, 1, 1);
            colors.disabledColor = new Color(.55f, .55f, .55f, .8f);
            button.colors = colors;
            if (icon == null)
            {
                var prior = transform.Find("HudActionIcon");
                if (prior != null) icon = prior.GetComponent<Image>();
                if (icon == null)
                {
                    var go = new GameObject("HudActionIcon", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(transform, false); icon = go.GetComponent<Image>();
                }
            }
            // Replace any inherited decorative icon, avoiding two icons on cloned controls.
            foreach (var image in GetComponentsInChildren<Image>(true))
                if (image != icon && image.transform != transform &&
                    (image.name == "Icon" || image.name == "ButtonIcon")) image.gameObject.SetActive(false);
            icon.sprite = LoadIcon(kind); icon.preserveAspect = true; icon.raycastTarget = false;
        }

        private void ApplyRole(HudButtonRole role)
        {
            var top = new Color(.03f, .32f, .42f);
            var bottom = new Color(.015f, .12f, .17f);
            var border = new Color(0, .78f, .94f);
            switch (role)
            {
                case HudButtonRole.Play:
                    top = new Color(.12f, .46f, .17f); bottom = new Color(.025f, .16f, .07f);
                    border = new Color(.3f, .85f, .4f); break;
                case HudButtonRole.Stop:
                    top = new Color(.65f, .12f, .06f); bottom = new Color(.2f, .025f, .01f);
                    border = new Color(1, .35f, .15f); break;
                case HudButtonRole.Alert:
                    top = new Color(.55f, .34f, .025f); bottom = new Color(.2f, .12f, .015f);
                    border = new Color(1, .7f, .05f); break;
                case HudButtonRole.Navigation:
                    top = new Color(.12f, .24f, .34f); bottom = new Color(.025f, .09f, .16f);
                    border = new Color(.3f, .65f, .9f); break;
                case HudButtonRole.Neutral:
                    top = new Color(.2f, .25f, .27f); bottom = new Color(.065f, .095f, .11f);
                    border = new Color(.48f, .6f, .64f); break;
            }
            surface.Configure(top, bottom, border, 2);
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (label == null || icon == null || button == null) return;
            bool rtl = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (english != null)
            {
                string caption = rtl && persian != null ? persian : english;
                string current = label is RTLTMPro.RTLTextMeshPro localized ? localized.OriginalText : label.text;
                if (current != caption) UiLocalizedText.Set(label, caption);
            }
            label.enableAutoSizing = false;
            label.fontSize = label.fontSizeMin = label.fontSizeMax = LabelSize;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.margin = Vector4.zero;
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0, baseHeading ? .5f : 0); rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(rtl ? 12 : 76, 4);
            rect.offsetMax = new Vector2(rtl ? -76 : -12, -4);
            var imageRect = icon.rectTransform;
            imageRect.anchorMin = imageRect.anchorMax = imageRect.pivot = new Vector2(rtl ? 1 : 0, .5f);
            imageRect.anchoredPosition = new Vector2(rtl ? -8 : 8, 64f * verticalArtworkOffsets[(int)kind]);
            imageRect.sizeDelta = new Vector2(64, 64);
            icon.color = button.IsInteractable() ? Color.white : new Color(.6f, .6f, .6f, .85f);
            label.color = button.IsInteractable() ? Color.white : new Color(.65f, .7f, .72f);
        }
    }
}
