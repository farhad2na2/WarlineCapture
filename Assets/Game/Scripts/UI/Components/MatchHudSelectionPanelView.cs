using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MatchHudSelectionPanelView : MonoBehaviour
{
    [SerializeField] private GameObject selectedSquadPanel;
    [SerializeField] private Image selectedPortraitImage;

    private void Awake()
    {
        HideSelection();
    }

    private void OnEnable()
    {
        HideSelection();
    }

    public void ShowSelection()
    {
        SetSelectionVisible(true);
    }

    public void HideSelection()
    {
        SetSelectionVisible(false);
    }

    public void SetSelectionVisible(bool visible)
    {
        if (selectedSquadPanel != null)
            selectedSquadPanel.SetActive(visible);
    }

    public void SetSelectionVisible(bool visible, Sprite portraitSprite)
    {
        SetSelectionVisible(visible);
        if (visible)
            SetSelectionPortrait(portraitSprite);
    }

    public void SetSelectionPortrait(Sprite portraitSprite)
    {
        if (selectedPortraitImage == null)
            return;

        selectedPortraitImage.sprite = portraitSprite;
        selectedPortraitImage.enabled = portraitSprite != null;
        selectedPortraitImage.preserveAspect = true;
    }
}
