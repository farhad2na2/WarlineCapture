using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed class NarrativeLocationIntroView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;

        public void Apply(in NarrativeLocationPresentationModel model)
        {
            if (titleText != null)
                titleText.text = model.Title ?? string.Empty;
            if (subtitleText != null)
                subtitleText.text = model.Subtitle ?? string.Empty;
            if (group == null)
                return;

            group.alpha = model.Visible ? 1f : 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
}
