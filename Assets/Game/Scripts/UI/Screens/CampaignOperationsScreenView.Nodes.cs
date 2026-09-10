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
            Sprite sprite = !available ? lockedNodeSprite : completed && !selected ? completedNodeSprite : availableNodeSprite;
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
                image.color = selected ? Color.white : available && !completed ? new Color(.55f, 1f, .65f) : Color.white;
            }
            if (nodeStateImages != null && index < nodeStateImages.Length && nodeStateImages[index] != null)
            {
                var state = nodeStateImages[index];
                state.sprite = completed ? completedNodeIcon : lockedNodeIcon;
                state.gameObject.SetActive(!available || completed && !selected);
            }
            if (nodeNumberLabels != null && index < nodeNumberLabels.Length && nodeNumberLabels[index] != null)
                nodeNumberLabels[index].gameObject.SetActive(available && (!completed || selected));
        }
    }
}
