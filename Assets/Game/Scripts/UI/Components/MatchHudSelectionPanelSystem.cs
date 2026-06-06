using UnityEngine;

[DisallowMultipleComponent]
public sealed class MatchHudSelectionPanelSystem : MonoBehaviour
{
    private static MatchHudSelectionPanelSystem activeSystem;

    [SerializeField] private GameObject selectedSquadPanel;

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

    private void ResolveSelectedSquadPanel()
    {
        if (selectedSquadPanel != null)
            return;

        Transform panel = transform.Find("SelectedSquadPanel");
        if (panel == null)
            panel = FindChildRecursive(transform, "SelectedSquadPanel");
        selectedSquadPanel = panel != null ? panel.gameObject : null;
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
