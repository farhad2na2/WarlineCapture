using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuickCustomScreenView : UIScreenView
{
    private readonly QuickCustomScreenFlowSystem flowSystem = new();

    [SerializeField] private TMP_Dropdown presetDropdown;
    [SerializeField] private TMP_Dropdown enemyTypeDropdown;
    [SerializeField] private UISegmentedControlView enemyCountStepper;
    [SerializeField] private UISegmentedControlView difficultySegmented;
    [SerializeField] private UISegmentedControlView startingMoneySegmented;
    [SerializeField] private UISliderRowView startingMoneySlider;
    [SerializeField] private UISliderRowView incomeMultiplierSlider;
    [SerializeField] private UISegmentedControlView buildSpeedSegmented;
    [SerializeField] private UISliderRowView buildSpeedSlider;
    [SerializeField] private UISegmentedControlView unitProductionSpeedSegmented;
    [SerializeField] private UISegmentedControlView attackGroupSizeSegmented;
    [SerializeField] private UISegmentedControlView attackFrequencySegmented;
    [SerializeField] private UISegmentedControlView aggressionSegmented;
    [SerializeField] private UISliderRowView aggressionSlider;
    [SerializeField] private UISegmentedControlView expansionSegmented;
    [SerializeField] private TMP_Dropdown targetPriorityDropdown;
    [SerializeField] private UIToggleRowView playerAutoToggle;
    [SerializeField] private TMP_Dropdown winConditionDropdown;
    [SerializeField] private UIToggleRowView fogOfWarToggle;
    [SerializeField] private UIToggleRowView intelRevealToggle;
    [SerializeField] private TMP_Dropdown startingResourcesDropdown;
    [SerializeField] private TMP_InputField seedInput;
    [SerializeField] private TMP_Text mapNameText;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button launchButton;

    private static readonly string[] DifficultyLabels = { "EASY", "NORMAL", "HARD", "BRUTAL" };
    private static readonly string[] StartingMoneyLabels = { "LOW", "NORMAL", "HIGH" };
    private static readonly string[] SpeedLabels = { "SLOW", "NORMAL", "FAST" };
    private static readonly string[] AttackGroupSizeLabels = { "SMALL", "NORMAL", "LARGE" };
    private static readonly string[] AttackFrequencyLabels = { "RARE", "NORMAL", "FREQUENT" };
    private static readonly string[] AggressionLabels = { "DEFENSIVE", "BALANCED", "AGGRESSIVE" };
    private static readonly string[] ExpansionLabels = { "OFF", "SLOW", "NORMAL", "FAST" };

    private QuickGameConfig _config;
    private IQuickCustomGameConfigStore _configStore;
    private IMatchLaunchCommand _launchCommand;

    private void Awake()
    {
        WireEvents();
        flowSystem.Initialize(this, _configStore);
    }

    private void OnDestroy()
    {
        if (launchButton != null)
            launchButton.onClick.RemoveListener(LaunchMatch);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetToDefaults);
    }

    public void Bind(QuickGameConfig config)
    {
        _config = config;
        SetDropdownValue(presetDropdown, 0);
        SetDropdownValue(enemyTypeDropdown, (int)config.EnemyType);
        BindEnemyCountStepper(Mathf.Clamp(config.EnemyCount, 1, 3));
        BindSegment(difficultySegmented, DifficultyLabels, (int)config.Difficulty);
        BindSegment(startingMoneySegmented, StartingMoneyLabels, (int)config.StartingMoney);
        BindStartingMoneySlider(config.StartingMoney);
        incomeMultiplierSlider?.Bind("Income Multiplier", Mathf.Clamp(config.IncomeMultiplier, 0.5f, 3f), 0.5f, 3f, "0.0");
        BindSegment(buildSpeedSegmented, SpeedLabels, (int)config.BuildSpeed);
        BindSpeedSlider(buildSpeedSlider, "Build Speed", config.BuildSpeed);
        BindSegment(unitProductionSpeedSegmented, SpeedLabels, (int)config.UnitProductionSpeed);
        BindSegment(attackGroupSizeSegmented, AttackGroupSizeLabels, (int)config.AttackGroupSize);
        BindSegment(attackFrequencySegmented, AttackFrequencyLabels, (int)config.AttackFrequency);
        BindSegment(aggressionSegmented, AggressionLabels, (int)config.Aggression);
        BindAggressionSlider(config.Aggression);
        BindSegment(expansionSegmented, ExpansionLabels, (int)config.Expansion);
        SetDropdownValue(targetPriorityDropdown, (int)config.TargetPriority);
        playerAutoToggle?.Bind("Player Auto AI", "Let the AI control your faction for simulation tests.", config.PlayerAutoAIEnabled);
        SetDropdownValue(winConditionDropdown, (int)config.WinCondition);
        fogOfWarToggle?.Bind("FOG OF WAR", "Hide unexplored areas.", config.FogOfWar);
        intelRevealToggle?.Bind("INTEL REVEAL", "Reveal enemy tech on scout.", config.IntelReveal);
        SetDropdownValue(startingResourcesDropdown, (int)config.StartingResources);

        if (seedInput != null)
            seedInput.SetTextWithoutNotify(config.MapSeed.ToString());

        if (mapNameText != null)
            mapNameText.text = ResolveMapName(config);
    }

    public void BindRuntimeDependencies(
        IQuickCustomGameConfigStore configStore,
        IMatchLaunchCommand launchCommand)
    {
        _configStore = configStore;
        _launchCommand = launchCommand;
        flowSystem.Initialize(this, _configStore);
    }

    public QuickGameConfig ReadConfigFromControls()
    {
        _config.EnemyType = (QuickGameEnemyType)GetDropdownValue(enemyTypeDropdown, (int)_config.EnemyType);
        _config.EnemyCount = ReadEnemyCountStepper(_config.EnemyCount);
        _config.Difficulty = (AIDifficultySetting)GetSelectedSegment(difficultySegmented, (int)_config.Difficulty);
        _config.StartingMoney = startingMoneySlider != null
            ? ReadStartingMoneySlider(startingMoneySlider)
            : (AIStartingMoneySetting)GetSelectedSegment(startingMoneySegmented, (int)_config.StartingMoney);
        _config.IncomeMultiplier = GetSliderValue(incomeMultiplierSlider, _config.IncomeMultiplier);
        _config.BuildSpeed = buildSpeedSlider != null
            ? ReadSpeedSlider(buildSpeedSlider)
            : (AISpeedSetting)GetSelectedSegment(buildSpeedSegmented, (int)_config.BuildSpeed);
        _config.UnitProductionSpeed = unitProductionSpeedSegmented != null
            ? (AISpeedSetting)GetSelectedSegment(unitProductionSpeedSegmented, (int)_config.UnitProductionSpeed)
            : _config.UnitProductionSpeed;
        _config.AttackGroupSize = (AIAttackGroupSizeSetting)GetSelectedSegment(attackGroupSizeSegmented, (int)_config.AttackGroupSize);
        _config.AttackFrequency = (AIAttackFrequencySetting)GetSelectedSegment(attackFrequencySegmented, (int)_config.AttackFrequency);
        _config.Aggression = aggressionSlider != null
            ? ReadAggressionSlider(aggressionSlider)
            : (AIAggressionSetting)GetSelectedSegment(aggressionSegmented, (int)_config.Aggression);
        _config.Expansion = (AIExpansionSetting)GetSelectedSegment(expansionSegmented, (int)_config.Expansion);
        _config.TargetPriority = (AITargetPriority)GetDropdownValue(targetPriorityDropdown, (int)_config.TargetPriority);
        _config.PlayerAutoAIEnabled = GetToggleValue(playerAutoToggle, _config.PlayerAutoAIEnabled);
        _config.WinCondition = (QuickGameWinCondition)GetDropdownValue(winConditionDropdown, (int)_config.WinCondition);
        _config.FogOfWar = GetToggleValue(fogOfWarToggle, _config.FogOfWar);
        _config.IntelReveal = GetToggleValue(intelRevealToggle, _config.IntelReveal);
        _config.StartingResources = (QuickGameStartingResources)GetDropdownValue(startingResourcesDropdown, (int)_config.StartingResources);
        _config.MapSeed = ReadSeed(_config.MapSeed);
        return _config;
    }

    public void ApplyCurrentConfigToRuntime()
    {
        flowSystem.ApplyCurrentConfig(this, _configStore);
    }

    public void LaunchMatch()
    {
        flowSystem.LaunchMatch(this, _configStore, _launchCommand);
    }

    private void WireEvents()
    {
        if (launchButton != null)
            launchButton.onClick.AddListener(LaunchMatch);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetToDefaults);

        WireEnemyCountStepper();
        WireSegment(difficultySegmented, DifficultyLabels);
        WireSegment(startingMoneySegmented, StartingMoneyLabels);
        WireSegment(buildSpeedSegmented, SpeedLabels);
        WireSegment(unitProductionSpeedSegmented, SpeedLabels);
        WireSegment(attackGroupSizeSegmented, AttackGroupSizeLabels);
        WireSegment(attackFrequencySegmented, AttackFrequencyLabels);
        WireSegment(aggressionSegmented, AggressionLabels);
        WireSegment(expansionSegmented, ExpansionLabels);
    }

    private void ResetToDefaults()
    {
        flowSystem.ResetToDefaults(this, _configStore);
    }

    private void WireEnemyCountStepper()
    {
        if (enemyCountStepper?.SegmentButtons == null || enemyCountStepper.SegmentButtons.Length < 3)
            return;

        Button minusButton = enemyCountStepper.SegmentButtons[0];
        Button plusButton = enemyCountStepper.SegmentButtons[2];
        if (minusButton != null)
            minusButton.onClick.AddListener(() => AdjustEnemyCount(-1));
        if (plusButton != null)
            plusButton.onClick.AddListener(() => AdjustEnemyCount(1));
    }

    private void AdjustEnemyCount(int delta)
    {
        int current = ReadEnemyCountStepper(_config.EnemyCount);
        _config.EnemyCount = Mathf.Clamp(current + delta, 1, 3);
        BindEnemyCountStepper(_config.EnemyCount);
    }

    private void BindEnemyCountStepper(int count)
    {
        if (enemyCountStepper == null)
            return;

        count = Mathf.Clamp(count, 1, 3);
        enemyCountStepper.Bind(new[] { "-", count.ToString(), "+" }, 1);
        if (enemyCountStepper.SegmentButtons == null || enemyCountStepper.SegmentButtons.Length < 3)
            return;

        if (enemyCountStepper.SegmentButtons[0] != null)
            enemyCountStepper.SegmentButtons[0].interactable = count > 1;
        if (enemyCountStepper.SegmentButtons[1] != null)
            enemyCountStepper.SegmentButtons[1].interactable = false;
        if (enemyCountStepper.SegmentButtons[2] != null)
            enemyCountStepper.SegmentButtons[2].interactable = count < 3;
    }

    private int ReadEnemyCountStepper(int fallback)
    {
        if (enemyCountStepper?.SegmentLabels == null || enemyCountStepper.SegmentLabels.Length < 2 || enemyCountStepper.SegmentLabels[1] == null)
            return Mathf.Clamp(fallback, 1, 3);

        return int.TryParse(enemyCountStepper.SegmentLabels[1].text, out int value)
            ? Mathf.Clamp(value, 1, 3)
            : Mathf.Clamp(fallback, 1, 3);
    }

    private void WireSegment(UISegmentedControlView view, string[] labels)
    {
        if (view?.SegmentButtons == null)
            return;

        for (int i = 0; i < view.SegmentButtons.Length; i++)
        {
            int index = i;
            Button button = view.SegmentButtons[i];
            if (button != null)
                button.onClick.AddListener(() => BindSegment(view, labels, index));
        }
    }

    private static void BindSegment(UISegmentedControlView view, string[] labels, int selectedIndex)
    {
        view?.Bind(labels, Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, labels.Length - 1)));
    }

    private static int GetSelectedSegment(UISegmentedControlView view, int fallback)
    {
        if (view?.SegmentButtons == null || view.SegmentButtons.Length == 0)
            return fallback;

        for (int i = 0; i < view.SegmentButtons.Length; i++)
        {
            Button button = view.SegmentButtons[i];
            if (button != null && !button.interactable)
                return i;
        }

        return fallback;
    }

    private static float GetSliderValue(UISliderRowView row, float fallback)
    {
        return row != null && row.Slider != null ? row.Slider.value : fallback;
    }

    private void BindStartingMoneySlider(AIStartingMoneySetting setting)
    {
        if (startingMoneySlider == null)
            return;

        float value = setting switch
        {
            AIStartingMoneySetting.Low => 0f,
            AIStartingMoneySetting.High => 100f,
            _ => 50f
        };

        startingMoneySlider.Bind("Starting Money", value, 0f, 100f);
        if (startingMoneySlider.ValueText != null)
        {
            startingMoneySlider.ValueText.text = setting switch
            {
                AIStartingMoneySetting.Low => "5,000",
                AIStartingMoneySetting.High => "20,000",
                _ => "10,000"
            };
        }
    }

    private static AIStartingMoneySetting ReadStartingMoneySlider(UISliderRowView row)
    {
        float value = GetSliderValue(row, 50f);
        if (value < 33f)
            return AIStartingMoneySetting.Low;

        return value > 67f ? AIStartingMoneySetting.High : AIStartingMoneySetting.Normal;
    }

    private static void BindSpeedSlider(UISliderRowView row, string label, AISpeedSetting setting)
    {
        if (row == null)
            return;

        float value = setting switch
        {
            AISpeedSetting.Slow => 0f,
            AISpeedSetting.Fast => 100f,
            _ => 50f
        };

        row.Bind(label, value, 0f, 100f);
        if (row.ValueText != null)
        {
            row.ValueText.text = setting switch
            {
                AISpeedSetting.Slow => "0.75x",
                AISpeedSetting.Fast => "1.25x",
                _ => "1.00x"
            };
        }
    }

    private static AISpeedSetting ReadSpeedSlider(UISliderRowView row)
    {
        float value = GetSliderValue(row, 50f);
        if (value < 33f)
            return AISpeedSetting.Slow;

        return value > 67f ? AISpeedSetting.Fast : AISpeedSetting.Normal;
    }

    private void BindAggressionSlider(AIAggressionSetting setting)
    {
        if (aggressionSlider == null)
            return;

        float value = setting switch
        {
            AIAggressionSetting.Defensive => 25f,
            AIAggressionSetting.Aggressive => 75f,
            _ => 50f
        };

        aggressionSlider.Bind("Aggression", value, 0f, 100f);
        if (aggressionSlider.ValueText != null)
            aggressionSlider.ValueText.text = $"{Mathf.RoundToInt(value)}%";
    }

    private static AIAggressionSetting ReadAggressionSlider(UISliderRowView row)
    {
        float value = GetSliderValue(row, 50f);
        if (value < 38f)
            return AIAggressionSetting.Defensive;

        return value > 62f ? AIAggressionSetting.Aggressive : AIAggressionSetting.Balanced;
    }

    private static bool GetToggleValue(UIToggleRowView row, bool fallback)
    {
        return row != null && row.Toggle != null ? row.Toggle.isOn : fallback;
    }

    private static int GetDropdownValue(TMP_Dropdown dropdown, int fallback)
    {
        return dropdown != null ? dropdown.value : fallback;
    }

    private static void SetDropdownValue(TMP_Dropdown dropdown, int value)
    {
        if (dropdown != null)
            dropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, Mathf.Max(0, dropdown.options.Count - 1)));
    }

    private int ReadSeed(int fallback)
    {
        if (seedInput == null || string.IsNullOrWhiteSpace(seedInput.text))
            return fallback;

        return int.TryParse(seedInput.text, out int seed) ? Mathf.Max(1, seed) : fallback;
    }

    private static string ResolveMapName(QuickGameConfig config)
    {
        return config.EnemyType switch
        {
            QuickGameEnemyType.Defensive => "DISTRICT FORTRESS",
            QuickGameEnemyType.Air => "AIRFIELD EDGE",
            QuickGameEnemyType.Swarm => "DENSE CITY GRID",
            QuickGameEnemyType.Random => "RANDOMIZED CITY",
            _ => "PROJECT CITY OUTSKIRTS"
        };
    }
}
