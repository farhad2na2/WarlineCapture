using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class M01InfantryOnlyHudScopeSystem : MonoBehaviour
{
    private const string BuildButtonName = "BuildButton";
    private const string SpecialButtonName = "SpecialButton";
    private const float CommandRailReferenceWidth = 704f;
    private const float CommandRailReferenceHeight = 164f;
    private const float CommandButtonWidth = 98f;
    private const float CommandButtonHeight = 124f;
    private const float CommandButtonTop = 24f;

    [SerializeField] private GameObject[] hiddenDuringM01 = Array.Empty<GameObject>();
    [SerializeField] private GameObject[] shownDuringM01 = Array.Empty<GameObject>();

    public bool IsM01ScopeActive { get; private set; }
    public int HiddenRootCount => hiddenDuringM01 != null ? hiddenDuringM01.Length : 0;

    private void OnEnable()
    {
        Refresh();
    }

    private void LateUpdate()
    {
        if (IsM01ScopeActive)
            ApplyM01NoSelectionHudState();
    }

    public void Refresh()
    {
        bool m01Active = WarlineCaptureMissionSession.HasActiveMission &&
            WarlineCaptureMissionSession.ActiveMissionId == ChapterOneMissionCatalog.FirstContactMissionId;
        IsM01ScopeActive = m01Active;

        ApplyM01SuppressedRoots(hiddenDuringM01, m01Active);
        SetActive(shownDuringM01, true);
        if (m01Active)
            ApplyM01NoSelectionHudState();
        else
            SetRowActive("CommandBar/SelectButton", false);
    }

    public bool AreM01SuppressedRootsHidden()
    {
        if (!IsM01ScopeActive || hiddenDuringM01 == null)
            return false;

        for (int i = 0; i < hiddenDuringM01.Length; i++)
        {
            GameObject root = hiddenDuringM01[i];
            if (root == null)
                continue;

            if (IsBuildButtonRoot(root))
            {
                Button button = root.GetComponent<Button>();
                if (button != null && root.activeSelf && !button.interactable)
                    continue;
            }

            if (root.activeSelf)
                return false;
        }

        return true;
    }

    public bool IsM01BuildButtonDisabled()
    {
        if (!IsM01ScopeActive || hiddenDuringM01 == null)
            return false;

        for (int i = 0; i < hiddenDuringM01.Length; i++)
        {
            GameObject root = hiddenDuringM01[i];
            if (root == null || !IsBuildButtonRoot(root))
                continue;

            Button button = root.GetComponent<Button>();
            return root.activeSelf && button != null && !button.interactable;
        }

        return false;
    }

    private static void ApplyM01SuppressedRoots(GameObject[] roots, bool m01Active)
    {
        if (roots == null)
            return;

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
                continue;

            if (IsBuildButtonRoot(root))
            {
                root.SetActive(!m01Active);
                SetButtonInteractable(root, !m01Active);
                continue;
            }

            if (m01Active && string.Equals(root.name, SpecialButtonName, StringComparison.Ordinal))
            {
                root.SetActive(true);
                SetButtonInteractable(root, false);
                continue;
            }

            root.SetActive(!m01Active);
        }
    }

    private static void SetActive(GameObject[] roots, bool active)
    {
        if (roots == null)
            return;

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null)
                roots[i].SetActive(active);
        }
    }

    private static bool IsBuildButtonRoot(GameObject root)
    {
        return root != null && string.Equals(root.name, BuildButtonName, StringComparison.Ordinal);
    }

    private static void SetButtonInteractable(GameObject root, bool interactable)
    {
        Button button = root != null ? root.GetComponent<Button>() : null;
        if (button != null)
            button.interactable = interactable;
    }

    private void ApplyM01NoSelectionHudState()
    {
        SetRowActive("ObjectivePanel/Objective_1", true);
        SetRowActive("ObjectivePanel/Objective_2", false);
        SetRowActive("ObjectivePanel/Objective_3", false);
        SetText("ObjectivePanel/Objective_1/LabelText", "Destroy hostile patrol");
        SetText("ObjectivePanel/StarGoal_1/LabelText", "Complete Mission");
        SetText("ThreatFeedPanel/Threat_1/LabelText", "12:00   Mission Start");
        for (int i = 2; i <= 5; i++)
            SetRowActive($"ThreatFeedPanel/Threat_{i}", false);

        SetRowActive("AssistantLayer/AssistantEntryButton", false);
        SetRowActive("AssistantLayer/AssistantPanelDock", false);
        HideM01NoSelectionTransientSurfaces();

        ConfigureM01SquadCard("SquadTray/Squad_Rifle", true);
        ConfigureM01SquadCard("SquadTray/Squad_APC", false);
        ConfigureM01SquadCard("SquadTray/Squad_Tank", false);
        ConfigureM01SquadCard("SquadTray/Squad_Helicopter", false);

        ConfigureCommandButton("CommandBar/SelectButton", "SELECT", 12f, 0, false);
        ConfigureCommandButton("CommandBar/MoveButton", "MOVE", 115f, 1, false);
        ConfigureCommandButton("CommandBar/AttackButton", "ATTACK", 218f, 2, false);
        ConfigureCommandButton("CommandBar/StopButton", "STOP", 320f, 3, false);
        ConfigureCommandButton("CommandBar/HoldButton", "HOLD", 422f, 4, false);
        SetRowActive("CommandBar/SpecialButton", false);
        ConfigureBuildUnavailable();
    }

    private void HideM01NoSelectionTransientSurfaces()
    {
        SetRowActive("WorldCommandMarkerLayer", false);
        SetRowActive("SelectedEntityPanel", false);
        SetRowActive("CommandModeBanner", false);
        SetRowActive("InvalidCommandToast", false);
        SetRowActive("CommandWheelCanvas", false);
        SetRowActive("BuildDrawerCanvas", false);
    }

    private void ConfigureCommandButton(string path, string label, float x, int siblingIndex, bool interactable)
    {
        Transform target = FindCommandButton(path);
        if (target == null)
            return;

        target.gameObject.SetActive(true);
        Transform commandRail = transform.Find("CommandBar/CommandRailArt");
        if (commandRail != null)
            commandRail.SetSiblingIndex(0);
        target.SetSiblingIndex(siblingIndex + 1);
        if (target.TryGetComponent(out Button button))
            button.interactable = interactable;
        ApplyNeutralCommandChrome(target);
        SetText(target, "LabelText", label);
        RectTransform rect = target as RectTransform;
        if (rect != null)
            ConfigureCommandButtonRect(rect, x);
    }

    private static void ConfigureCommandButtonRect(RectTransform rect, float x)
    {
        rect.anchorMin = new Vector2(x / CommandRailReferenceWidth, 1f - (CommandButtonTop + CommandButtonHeight) / CommandRailReferenceHeight);
        rect.anchorMax = new Vector2((x + CommandButtonWidth) / CommandRailReferenceWidth, 1f - CommandButtonTop / CommandRailReferenceHeight);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private void ApplyNeutralCommandChrome(Transform target)
    {
        Image image = target != null ? target.GetComponent<Image>() : null;
        Image neutralImage = FindCommandButton("CommandBar/StopButton")?.GetComponent<Image>();
        if (image != null && neutralImage != null)
            image.sprite = neutralImage.sprite;

        Animator animator = target != null ? target.GetComponent<Animator>() : null;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.Play("Normal", 0, 1f);
            animator.Update(0f);
        }
    }

    private void ConfigureM01SquadCard(string path, bool interactable)
    {
        Transform target = transform.Find(path);
        if (target == null)
            return;

        target.gameObject.SetActive(true);
        if (target.TryGetComponent(out Button button))
            button.interactable = interactable;
    }

    private void ConfigureBuildUnavailable()
    {
        Transform target = FindBuildButton();
        if (target == null)
            return;

        target.gameObject.SetActive(true);
        if (target.TryGetComponent(out Button button))
            button.interactable = false;
        SetText(target, "LabelText", "BUILD\nLOCKED");
        target.gameObject.SetActive(false);
    }

    private Transform FindBuildButton()
    {
        Transform direct = transform.Find(BuildButtonName);
        if (direct != null)
            return direct;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (string.Equals(children[i].name, BuildButtonName, StringComparison.Ordinal))
                return children[i];
        }

        return null;
    }

    private Transform FindCommandButton(string path)
    {
        Transform direct = transform.Find(path);
        if (direct != null)
            return direct;

        int slash = path.LastIndexOf('/');
        string buttonName = slash >= 0 ? path[(slash + 1)..] : path;
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (string.Equals(children[i].name, buttonName, StringComparison.Ordinal))
                return children[i];
        }

        return null;
    }

    private void EnsureCommandLabelFallback(string label, float x, int siblingIndex)
    {
        Transform commandBar = FindChildByName("CommandBar");
        if (commandBar == null)
            return;

        string objectName = $"M01CommandLabel_{label}";
        Transform existing = commandBar.Find(objectName);
        GameObject labelObject = existing != null ? existing.gameObject : new GameObject(objectName, typeof(RectTransform));
        if (existing == null)
            labelObject.transform.SetParent(commandBar, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(92f, 0f);
        rect.anchoredPosition = new Vector2(x + 46f, 0f);
        labelObject.transform.SetSiblingIndex(Mathf.Min(siblingIndex, commandBar.childCount - 1));
        labelObject.SetActive(true);

        TMP_Text text = labelObject.GetComponent<TMP_Text>();
        if (text == null)
            text = labelObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 18f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.color = new Color(0.50f, 0.70f, 0.73f, 0.86f);
    }

    private void EnsureRootCommandLabelFallback(string label, int slotIndex)
    {
        string objectName = $"M01RootCommandLabel_{label}";
        Transform existing = transform.Find(objectName);
        GameObject labelObject = existing != null ? existing.gameObject : new GameObject(objectName, typeof(RectTransform));
        if (existing == null)
            labelObject.transform.SetParent(transform, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(122f, 32f);
        rect.anchoredPosition = new Vector2(800f + slotIndex * 95f, 105f);
        labelObject.transform.SetAsLastSibling();
        labelObject.SetActive(true);

        TMP_Text text = labelObject.GetComponent<TMP_Text>();
        if (text == null)
            text = labelObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 20f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 20f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.color = new Color(0.62f, 0.82f, 0.85f, 0.92f);
    }

    private Transform FindChildByName(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (string.Equals(children[i].name, childName, StringComparison.Ordinal))
                return children[i];
        }

        return null;
    }

    private void SetRowActive(string path, bool active)
    {
        Transform target = transform.Find(path);
        if (target != null)
            target.gameObject.SetActive(active);
    }

    private void SetText(string path, string value)
    {
        Transform target = transform.Find(path);
        if (target != null && target.TryGetComponent(out TMP_Text text))
            text.text = value;
    }

    private static void SetText(Transform root, string childPath, string value)
    {
        Transform child = root != null ? root.Find(childPath) : null;
        if (child != null && child.TryGetComponent(out TMP_Text text))
            text.text = value;
    }
}
