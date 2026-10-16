using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class AriaTutorialBriefingView
    {
        private TMP_Text portraitName;
        private float InstructionScale => _missionLayoutLarge ? 1.08f : 1f;
        private float InstructionBodySize => 24f * InstructionScale;

        private void ApplyPortraitHeader(float height, bool hasInstructions)
        {
            float panelWidth = ((RectTransform)transform).rect.width;
            Place(missionPortraitStage, 10, 8, panelWidth - 20, height);
            var surface = missionPortraitStage.GetComponentInChildren<V3GradientGraphic>(true);
            if (surface != null)
                surface.Configure(new Color32(14, 45, 57, 255), new Color32(5, 20, 28, 255),
                    new Color32(22, 98, 114, 255), 1);
            float clipHeight = hasInstructions ? height - 16 : height - 20;
            float clipWidth = hasInstructions ? Mathf.Min(120, clipHeight * 1.1f) : 192;
            Place(missionPortraitClip, (panelWidth - clipWidth) * .5f, 16, clipWidth, clipHeight);
            var fitter = portraitImage.GetComponent<AspectRatioFitter>();
            if (fitter != null) fitter.enabled = false;
            var image = portraitImage.rectTransform;
            image.anchorMin = image.anchorMax = image.pivot = new Vector2(.5f, 1);
            image.anchoredPosition = Vector2.zero;
            float aspect = portraitImage.sprite != null
                ? portraitImage.sprite.rect.width / portraitImage.sprite.rect.height : .75f;
            // Preserve the shared artwork. The mask crops the lower torso, keeping the
            // face readable without letting the image or its hit target escape the header.
            float imageWidth = hasInstructions ? clipWidth * 1.1f : clipHeight * aspect;
            image.sizeDelta = new Vector2(imageWidth, imageWidth / aspect);
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            if (portraitName == null) portraitName = transform.Find("Label")?.GetComponent<TMP_Text>();
            if (portraitName != null)
            {
                Place(portraitName.rectTransform, _rightToLeft ? panelWidth - 120 : 16, 16, 104, 40);
                portraitName.enableAutoSizing = false;
                portraitName.fontSize = _rightToLeft ? 34 : 30;
                portraitName.alignment = _rightToLeft ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            }
            Place(progressText.rectTransform, _rightToLeft ? 16 : panelWidth - 140, 16, 124, 30);
            progressText.alignment = _rightToLeft ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
        }

        private void ApplyInstructionTypography()
        {
            var alignment = _rightToLeft ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            StyleInstructionText(titleText, alignment);
            StyleInstructionText(bodyText, alignment);
            StyleInstructionText(openingCopy, alignment);
            StyleInstructionText(alertCopy, alignment);
            StyleInstructionText(watchExplanation, alignment);
            bodyText.fontSize = InstructionBodySize;
            titleText.fontSize = 26f * InstructionScale;
        }

        private void StyleInstructionText(TMP_Text text, TextAlignmentOptions alignment)
        {
            if (text == null) return;
            text.alignment = alignment;
            text.characterSpacing = _rightToLeft ? 0 : 1;
            text.lineSpacing = 0;
            text.paragraphSpacing = 8;
        }
    }
}
