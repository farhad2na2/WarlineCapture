using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class BuildDrawerTabView
{
    [SerializeField] private BuildDrawerCategory category;
    [SerializeField] private Button button;
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private GameObject disabledOverlay;

    public BuildDrawerCategory Category => category;
    public Button Button => button;
    public Image Frame => frame;
    public TMP_Text LabelText => labelText;
    public TMP_Text CountText => countText;

    public void Apply(
        bool selected,
        bool interactable,
        int itemCount,
        Sprite selectedFrameSprite,
        Sprite normalFrameSprite)
    {
        if (button != null)
            button.interactable = interactable;

        if (frame != null)
        {
            Sprite target = selected ? selectedFrameSprite : normalFrameSprite;
            if (target != null)
                frame.sprite = target;
        }

        if (countText != null)
            countText.text = itemCount > 0 ? itemCount.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;

        if (disabledOverlay != null)
            disabledOverlay.SetActive(!interactable);
    }
}
