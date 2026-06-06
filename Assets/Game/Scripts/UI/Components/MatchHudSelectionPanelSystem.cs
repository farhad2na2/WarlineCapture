using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MatchHudSelectionPanelSystem : MonoBehaviour
{
    private static MatchHudSelectionPanelSystem activeSystem;

    [SerializeField] private GameObject selectedSquadPanel;
    [SerializeField] private Image selectedPortraitImage;

    private void Awake()
    {
        HideSelection();
    }

    private void OnEnable()
    {
        activeSystem = this;
        HideSelection();
    }

    private void OnDisable()
    {
        if (activeSystem == this)
            activeSystem = null;
    }

    public static void SetActiveSelectionVisible(bool visible)
    {
        if (activeSystem == null)
            return;

        activeSystem.SetSelectionVisible(visible);
    }

    public static void SetActiveSelectionVisible(bool visible, Sprite portraitSprite)
    {
        if (activeSystem == null)
            return;

        activeSystem.SetSelectionVisible(visible, portraitSprite);
    }

    public static void SetActiveSelectionPortrait(Sprite portraitSprite)
    {
        if (activeSystem == null)
            return;

        activeSystem.SetSelectionPortrait(portraitSprite);
    }

    public void ShowSelection()
    {
        SetSelectionVisible(true);
    }

    public void HideSelection()
    {
        SetSelectionVisible(false);
    }

    private void SetSelectionVisible(bool visible)
    {
        ResolveSelectedSquadPanel();
        if (selectedSquadPanel != null)
            selectedSquadPanel.SetActive(visible);
    }

    private void SetSelectionVisible(bool visible, Sprite portraitSprite)
    {
        SetSelectionVisible(visible);
        if (visible)
            SetSelectionPortrait(portraitSprite);
    }

    private void SetSelectionPortrait(Sprite portraitSprite)
    {
        ResolveSelectedPortraitImage();
        if (selectedPortraitImage == null)
            return;

        selectedPortraitImage.sprite = portraitSprite;
        selectedPortraitImage.enabled = portraitSprite != null;
        selectedPortraitImage.preserveAspect = true;
    }

    private void ResolveSelectedSquadPanel()
    {
        if (selectedSquadPanel != null)
            return;

        Transform panel = transform.Find("SelectedSquadPanel");
        if (panel == null)
            panel = FindChildRecursive(transform, "SelectedSquadPanel");
        selectedSquadPanel = panel != null ? panel.gameObject : null;
    }

    private void ResolveSelectedPortraitImage()
    {
        if (selectedPortraitImage != null)
            return;

        ResolveSelectedSquadPanel();
        Transform frame = selectedSquadPanel != null ? selectedSquadPanel.transform.Find("Frame") : null;
        Transform portrait = frame != null ? frame.Find("PortraitFrame") : null;
        if (portrait == null && selectedSquadPanel != null)
            portrait = FindChildRecursive(selectedSquadPanel.transform, "PortraitFrame");
        selectedPortraitImage = portrait != null ? portrait.GetComponent<Image>() : null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform match = FindChildRecursive(child, childName);
            if (match != null)
                return match;
        }

        return null;
    }
}
