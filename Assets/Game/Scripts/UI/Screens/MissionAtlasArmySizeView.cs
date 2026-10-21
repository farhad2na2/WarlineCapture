using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Atlas-only presentation for the existing, bound army-size choices.</summary>
    [ExecuteAlways]
    public sealed class MissionAtlasArmySizeView : MonoBehaviour
    {
        [SerializeField] private UISegmentedControlView control;
        [SerializeField] private V3GradientGraphic[] faces;
        [SerializeField] private MissionAtlasStatusGraphic[] checks;
        [SerializeField] private Image[] locks;
        private int lastState = int.MinValue;
        private float lastWidth = -1;

        public void Author(UISegmentedControlView value)
        {
            control = value;
            faces = new V3GradientGraphic[control.SegmentButtons.Length];
            checks = new MissionAtlasStatusGraphic[faces.Length];
            locks = new Image[faces.Length];
            for (int i = 0; i < faces.Length; i++)
            {
                var button = control.SegmentButtons[i];
                // Keep the real Button and its gameplay listeners. The face receives its tint.
                button.transition = Selectable.Transition.ColorTint;
                var colors = ColorBlock.defaultColorBlock;
                colors.highlightedColor = new Color(.88f, 1f, 1f, 1f);
                colors.pressedColor = new Color(.65f, .85f, 1f, 1f);
                colors.disabledColor = Color.white;
                button.colors = colors;
                if (button.GetComponent<Image>() is Image legacy) { legacy.color = Color.clear; legacy.raycastTarget = false; }
                var face = new GameObject("ChoiceFace", typeof(RectTransform), typeof(V3GradientGraphic));
                face.transform.SetParent(button.transform, false);
                face.transform.SetAsFirstSibling();
                faces[i] = face.GetComponent<V3GradientGraphic>();
                MissionAtlasScreenView.Fill(faces[i].rectTransform);
                button.targetGraphic = faces[i];
                var mark = new GameObject("SelectedChoice", typeof(RectTransform), typeof(MissionAtlasStatusGraphic));
                mark.transform.SetParent(button.transform, false);
                checks[i] = mark.GetComponent<MissionAtlasStatusGraphic>();
                checks[i].Configure(MissionAtlasStatusGraphic.Kind.Selected, true);
                var locked = new GameObject("UnavailableChoice", typeof(RectTransform), typeof(Image));
                locked.transform.SetParent(button.transform, false);
                locks[i] = locked.GetComponent<Image>();
                locks[i].sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.ResourceRoot+"icon_lock");
                locks[i].preserveAspect = true;
                locks[i].raycastTarget = false;
            }
            lastState = int.MinValue;
            Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (control == null || faces == null || checks == null) return;
            int state = (control.SelectedIndex + 1) * 2 + (control.SegmentLabels.Length > 0 && control.SegmentLabels[0].isRightToLeftText ? 1 : 0);
            foreach (var button in control.SegmentButtons) state = state * 3 + (button != null && button.interactable ? 1 : 0);
            float width = ((RectTransform)transform).rect.width;
            if (state == lastState && Mathf.Approximately(width, lastWidth)) return;
            lastState = state; lastWidth = width;
            for (int i = 0; i < faces.Length; i++)
            {
                bool selected = i == control.SelectedIndex;
                bool available = selected || control.SegmentButtons[i].interactable;
                faces[i].Configure(selected ? new Color32(3,77,116,255) : MissionAtlasScreenView.Top,
                    selected ? new Color32(2,35,59,255) : MissionAtlasScreenView.Bottom,
                    selected ? MissionAtlasScreenView.Cyan : MissionAtlasScreenView.Border, selected ? 3 : 2);
                faces[i].color = available ? Color.white : new Color(.65f,.65f,.65f,1f);
                checks[i].gameObject.SetActive(selected);
                float w = ((RectTransform)control.SegmentButtons[i].transform).rect.width;
                float h = ((RectTransform)control.SegmentButtons[i].transform).rect.height;
                MissionAtlasScreenView.Place(checks[i].rectTransform, w-23, 5, 18, 18);
                locks[i].gameObject.SetActive(!available);
                MissionAtlasScreenView.Place(locks[i].rectTransform, w-23, 5, 18, 18);
                TMP_Text label = control.SegmentLabels[i];
                MissionAtlasScreenView.Place(label.rectTransform, 12, 14, w-24, h-18);
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = label.isRightToLeftText ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
                label.color = available ? Color.white : MissionAtlasScreenView.Muted;
            }
        }
    }
}
