using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private Sprite availableNodeSprite, completedNodeSprite, lockedNodeSprite;
        [SerializeField] private Sprite completedNodeIcon, lockedNodeIcon;
        [SerializeField] private Image[] nodeStateImages;
        [SerializeField] private TMP_Text[] nodeNumberLabels, nodeIdLabels;
        [SerializeField] private V3GradientGraphic[] nodeLabelPanels;

        private void ApplyNodeAppearance(int index, bool available, bool completed, bool selected)
        {
            if (missionNodeButtons == null || index >= missionNodeButtons.Length || missionNodeButtons[index] == null) return;
            Color accent = selected ? new Color32(255, 190, 0, 255) : completed ? new Color32(76, 175, 80, 255) : new Color32(140, 158, 164, 255);
            if (nodeIdLabels != null && index < nodeIdLabels.Length && nodeIdLabels[index] != null) nodeIdLabels[index].color = accent;
            if (nodeLabelPanels != null && index < nodeLabelPanels.Length && nodeLabelPanels[index] != null)
                nodeLabelPanels[index].SetBorder(selected ? accent : new Color32(69, 81, 85, 255), 2f);
            var image = missionNodeButtons[index].image;
            if (atlasMissionArtwork != null && index < atlasMissionArtwork.Length && atlasMissionArtwork[index] != null)
            {
                if (image != null)
                {
                    image.sprite = null;
                    image.color = selected ? new Color32(247, 179, 0, 255) :
                        available ? new Color32(20, 42, 48, 255) : new Color32(30, 36, 39, 255);
                }
                atlasMissionArtwork[index].color = available ? Color.white : new Color(0.55f, 0.58f, 0.59f, 1f);
                if (nodeNumberLabels != null && index < nodeNumberLabels.Length && nodeNumberLabels[index] != null)
                    nodeNumberLabels[index].gameObject.SetActive(false);
                if (nodeStateImages != null && index < nodeStateImages.Length && nodeStateImages[index] != null)
                    nodeStateImages[index].gameObject.SetActive(false);
                return;
            }
            Sprite sprite = !available ? lockedNodeSprite : completed && !selected ? completedNodeSprite : availableNodeSprite;
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
                image.color = selected ? new Color(1f, 0.86f, 0.35f) : Color.white;
            }
            FitSelectedNode(missionNodeButtons[index].GetComponent<RectTransform>(), selected);
            if (nodeLabelPanels != null && index < nodeLabelPanels.Length && nodeLabelPanels[index] != null)
            {
                TMP_Text name = nodeLabelPanels[index].transform.Find("Name")?.GetComponent<TMP_Text>();
                if (name != null)
                    name.color = selected ? new Color32(255, 214, 120, 255) : new Color32(245, 246, 238, 255);
            }
            if (nodeStateImages != null && index < nodeStateImages.Length && nodeStateImages[index] != null)
            {
                var state = nodeStateImages[index];
                state.sprite = completed ? completedNodeIcon : lockedNodeIcon;
                // A UI Image with no sprite draws a solid white rectangle.
                state.enabled = state.sprite != null;
                state.gameObject.SetActive(!available || completed && !selected);
            }
            if (nodeNumberLabels != null && index < nodeNumberLabels.Length && nodeNumberLabels[index] != null)
                nodeNumberLabels[index].gameObject.SetActive(available && (!completed || selected));
        }

        private static void FitSelectedNode(RectTransform rect, bool selected)
        {
            if (rect == null || rect.pivot.x > 0.2f)
                return;
            float next = selected ? 118f : 110f;
            Vector2 center = rect.anchoredPosition + new Vector2(rect.sizeDelta.x * 0.5f, -rect.sizeDelta.y * 0.5f);
            rect.sizeDelta = new Vector2(next, next);
            rect.anchoredPosition = new Vector2(center.x - next * 0.5f, center.y + next * 0.5f);
        }
    }
}
