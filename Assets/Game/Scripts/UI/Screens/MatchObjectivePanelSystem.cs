using TMPro;
using UnityEngine;

public sealed class MatchObjectivePanelSystem : MonoBehaviour
{
    [SerializeField] private GameObject objectivePanel;
    [SerializeField] private GameObject[] objectiveRows = System.Array.Empty<GameObject>();
    [SerializeField] private TMP_Text[] objectiveLabels = System.Array.Empty<TMP_Text>();
    [SerializeField] private GameObject[] starGoalRows = System.Array.Empty<GameObject>();
    [SerializeField] private TMP_Text[] starGoalLabels = System.Array.Empty<TMP_Text>();

    private string[] _objectiveFallbacks;
    private string[] _starGoalFallbacks;
    private float _nextRefreshTime;

    private void Awake()
    {
        CacheFallbackText();
    }

    private void OnEnable()
    {
        CacheFallbackText();
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + 0.25f;
        Refresh();
    }

    public void RefreshForTests()
    {
        CacheFallbackText();
        Refresh();
    }

    private void Refresh()
    {
        if (objectivePanel != null)
            objectivePanel.SetActive(true);

        if (!new ActiveMissionSession().HasActiveMission)
        {
            RestoreFallbackText();
            SetRowsActive(objectiveRows, true);
            SetRowsActive(starGoalRows, true);
            return;
        }

        MissionConfig mission = new ActiveMissionSession().ActiveMission;
        GameRuntimeStats.Snapshot snapshot = GameRuntimeStats.GetSnapshot();
        var manager = new ObjectiveManager();
        manager.Initialize(mission);
        var states = manager.Evaluate(snapshot);

        for (int i = 0; i < objectiveRows.Length; i++)
        {
            bool hasObjective = i < states.Count;
            SetRowActive(objectiveRows, i, hasObjective);
            if (!hasObjective)
                continue;

            ObjectiveRuntimeState state = states[i];
            SetLabel(objectiveLabels, i, FormatObjective(state));
            SetLabelColor(objectiveLabels, i, state.Complete ? new Color(0.58f, 1f, 0.76f, 1f) : new Color(0.91f, 0.95f, 0.96f, 1f));
        }

        StarGoalConfig[] starGoals = mission.StarGoals;
        for (int i = 0; i < starGoalRows.Length; i++)
        {
            bool hasStarGoal = i < starGoals.Length && starGoals[i] != null;
            SetRowActive(starGoalRows, i, hasStarGoal);
            if (!hasStarGoal)
                continue;

            StarGoalConfig starGoal = starGoals[i];
            int currentAmount = ObjectiveManager.ResolveProgress(starGoal.Type, snapshot);
            bool complete = ObjectiveManager.IsComplete(starGoal.Type, currentAmount, starGoal.TargetAmount);
            SetLabel(starGoalLabels, i, FormatStarGoal(starGoal, currentAmount));
            SetLabelColor(starGoalLabels, i, complete ? new Color(1f, 0.83f, 0.28f, 1f) : new Color(0.91f, 0.95f, 0.96f, 1f));
        }
    }

    private static string FormatObjective(ObjectiveRuntimeState state)
    {
        return $"{state.DisplayName}  {FormatProgress(state.Type, state.CurrentAmount, state.TargetAmount)}";
    }

    private static string FormatStarGoal(StarGoalConfig starGoal, int currentAmount)
    {
        return $"{starGoal.DisplayName}  {FormatProgress(starGoal.Type, currentAmount, starGoal.TargetAmount)}";
    }

    private static string FormatProgress(ObjectiveType type, int currentAmount, int targetAmount)
    {
        int current = Mathf.Max(0, currentAmount);
        int target = Mathf.Max(0, targetAmount);
        return type switch
        {
            ObjectiveType.KeepUnitLossesBelow => $"{current} / {target} max",
            ObjectiveType.SurviveDuration => $"{current}s / {target}s",
            _ => $"{current} / {target}"
        };
    }

    private void CacheFallbackText()
    {
        _objectiveFallbacks ??= CaptureText(objectiveLabels);
        _starGoalFallbacks ??= CaptureText(starGoalLabels);
    }

    private void RestoreFallbackText()
    {
        RestoreText(objectiveLabels, _objectiveFallbacks, new Color(0.91f, 0.95f, 0.96f, 1f));
        RestoreText(starGoalLabels, _starGoalFallbacks, new Color(0.91f, 0.95f, 0.96f, 1f));
    }

    private static string[] CaptureText(TMP_Text[] labels)
    {
        if (labels == null)
            return System.Array.Empty<string>();

        string[] values = new string[labels.Length];
        for (int i = 0; i < labels.Length; i++)
            values[i] = labels[i] != null ? labels[i].text : string.Empty;

        return values;
    }

    private static void RestoreText(TMP_Text[] labels, string[] values, Color color)
    {
        if (labels == null || values == null)
            return;

        int count = Mathf.Min(labels.Length, values.Length);
        for (int i = 0; i < count; i++)
        {
            if (labels[i] == null)
                continue;

            labels[i].text = values[i];
            labels[i].color = color;
        }
    }

    private static void SetRowsActive(GameObject[] rows, bool active)
    {
        if (rows == null)
            return;

        for (int i = 0; i < rows.Length; i++)
            SetRowActive(rows, i, active);
    }

    private static void SetRowActive(GameObject[] rows, int index, bool active)
    {
        if (rows == null || index < 0 || index >= rows.Length || rows[index] == null)
            return;

        rows[index].SetActive(active);
    }

    private static void SetLabel(TMP_Text[] labels, int index, string value)
    {
        if (labels == null || index < 0 || index >= labels.Length || labels[index] == null)
            return;

        labels[index].text = value;
    }

    private static void SetLabelColor(TMP_Text[] labels, int index, Color color)
    {
        if (labels == null || index < 0 || index >= labels.Length || labels[index] == null)
            return;

        labels[index].color = color;
    }
}
