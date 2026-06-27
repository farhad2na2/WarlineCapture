using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleScenarioLabPlayBootstrap : MonoBehaviour
{
    [SerializeField] private BattleScenarioDefinition scenarioDefinition;
    [SerializeField] private BattleScenarioDefinition[] scenarioDefinitions = Array.Empty<BattleScenarioDefinition>();
    [SerializeField] private BattleScenarioLabOverlayView overlayView;
    [SerializeField] private BattleScenarioLabVisualPlayback visualPlayback;
    [SerializeField] private Dropdown scenarioDropdown;
    [SerializeField] private Dropdown variantDropdown;
    [SerializeField] private bool runOnStart = true;

    private int selectedScenarioIndex;

    private void Start()
    {
        NormalizeScenarioDefinitions();
        PopulateScenarioDropdown();
        PopulateVariantDropdown();
        if (scenarioDropdown != null)
            scenarioDropdown.onValueChanged.AddListener(_ => SelectScenarioFromDropdown());
        if (variantDropdown != null)
            variantDropdown.onValueChanged.AddListener(_ => RunScenario());
        if (!runOnStart)
        {
            overlayView?.ShowPending(CurrentScenarioDefinition);
            return;
        }

        RunScenario();
    }

    public void RunScenario()
    {
        try
        {
            BattleScenarioResult result = RunSelectedScenario(
                out BattleScenarioVariant playbackVariant,
                out BattleScenarioMetrics playbackMetrics);
            overlayView?.ShowResult(result);
            if (visualPlayback != null && visualPlayback.CanPlay(CurrentScenarioDefinition))
            {
                visualPlayback.Play(CurrentScenarioDefinition, playbackVariant, playbackMetrics);
            }
            else
            {
                visualPlayback?.StopPlaybackAndClear();
            }
        }
        catch (Exception ex)
        {
            overlayView?.ShowError(ex.Message);
            Debug.LogError($"[BattleScenarioLab] Scenario run failed: {ex}");
        }
    }

    public void SelectNextScenario()
    {
        if (ScenarioCount <= 0)
            return;

        if (TrySelectNextVisualVariant())
            return;

        SelectScenario((selectedScenarioIndex + 1) % ScenarioCount, runScenario: true);
    }

    public void SelectPreviousScenario()
    {
        if (ScenarioCount <= 0)
            return;

        SelectScenario((selectedScenarioIndex + ScenarioCount - 1) % ScenarioCount, runScenario: true);
    }

    public void SelectScenarioFromDropdown()
    {
        if (scenarioDropdown == null)
            return;

        SelectScenario(scenarioDropdown.value, runScenario: true);
    }

    private void SelectScenario(int index, bool runScenario)
    {
        if (ScenarioCount <= 0)
            return;

        selectedScenarioIndex = Mathf.Clamp(index, 0, ScenarioCount - 1);
        visualPlayback?.StopPlaybackAndClear();
        scenarioDefinition = CurrentScenarioDefinition;
        if (scenarioDropdown != null && scenarioDropdown.value != selectedScenarioIndex)
        {
            scenarioDropdown.SetValueWithoutNotify(selectedScenarioIndex);
            scenarioDropdown.RefreshShownValue();
        }

        PopulateVariantDropdown();
        overlayView?.ShowPending(CurrentScenarioDefinition);
        if (runScenario)
            RunScenario();
    }

    private bool TrySelectNextVisualVariant()
    {
        BattleScenarioDefinition definition = CurrentScenarioDefinition;
        if (!ShouldAdvanceVariantsWithNext(definition) ||
            variantDropdown == null ||
            definition.ScenarioVariants == null ||
            definition.ScenarioVariants.Length == 0)
        {
            return false;
        }

        int currentValue = variantDropdown.value;
        if (currentValue <= 0)
            currentValue = FindPreferredPlaybackIndex(definition.ScenarioVariants) + 1;

        int nextValue = currentValue + 1;
        if (nextValue > definition.ScenarioVariants.Length)
            return false;

        visualPlayback?.StopPlaybackAndClear();
        variantDropdown.SetValueWithoutNotify(nextValue);
        variantDropdown.RefreshShownValue();
        overlayView?.ShowPending(CurrentScenarioDefinition);
        RunScenario();
        return true;
    }

    private static bool ShouldAdvanceVariantsWithNext(BattleScenarioDefinition definition)
    {
        return definition != null &&
               string.Equals(
                   definition.ScenarioId,
                   BattleScenarioAd011Runner.ScenarioId,
                   StringComparison.Ordinal);
    }

    private BattleScenarioResult RunSelectedScenario(
        out BattleScenarioVariant playbackVariant,
        out BattleScenarioMetrics playbackMetrics)
    {
        BattleScenarioDefinition definition = CurrentScenarioDefinition;
        BattleScenarioVariant[] variants = definition != null && definition.ScenarioVariants.Length > 0
            ? definition.ScenarioVariants
            : BattleScenarioAd001Runner.CreateDefaultVariants();
        int selectedVariantIndex = variantDropdown != null ? variantDropdown.value - 1 : -1;

        if (definition == null)
        {
            BattleScenarioResult defaultResult = BattleScenarioAd001Runner.RunDefault();
            SelectPlaybackVariant(variants, defaultResult, -1, out playbackVariant, out playbackMetrics);
            return defaultResult;
        }

        if (!BattleScenarioLabRuntimeRunner.SupportsSingleVariantPlayback(definition))
        {
            BattleScenarioResult result = BattleScenarioLabRuntimeRunner.RunDefinition(definition);
            SelectPlaybackVariant(variants, result, selectedVariantIndex, out playbackVariant, out playbackMetrics);
            return result;
        }

        if (selectedVariantIndex < 0 || selectedVariantIndex >= variants.Length)
        {
            BattleScenarioResult result = BattleScenarioLabRuntimeRunner.RunDefinition(definition);
            SelectPlaybackVariant(variants, result, -1, out playbackVariant, out playbackMetrics);
            return result;
        }

        BattleScenarioMetrics metrics = BattleScenarioAd001Runner.RunVariant(variants[selectedVariantIndex]);
        playbackVariant = variants[selectedVariantIndex];
        playbackMetrics = metrics;
        return new BattleScenarioResult
        {
            ScenarioId = definition.ScenarioId,
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            FixedDeltaTime = definition.FixedDeltaTime,
            Variants = new[] { metrics },
            Comparisons = Array.Empty<BattleScenarioComparison>(),
            Passed = metrics.Intercepted && !metrics.IncomingThreatImpacted,
            FailureReason = metrics.FailureReason
        };
    }

    private BattleScenarioDefinition CurrentScenarioDefinition
    {
        get
        {
            NormalizeScenarioDefinitions();
            if (scenarioDefinitions.Length > 0)
                return scenarioDefinitions[Mathf.Clamp(selectedScenarioIndex, 0, scenarioDefinitions.Length - 1)];

            return scenarioDefinition;
        }
    }

    private int ScenarioCount
    {
        get
        {
            NormalizeScenarioDefinitions();
            return scenarioDefinitions.Length;
        }
    }

    private void NormalizeScenarioDefinitions()
    {
        if (scenarioDefinitions != null && scenarioDefinitions.Length > 0)
            return;

        scenarioDefinitions = scenarioDefinition != null
            ? new[] { scenarioDefinition }
            : Array.Empty<BattleScenarioDefinition>();
        selectedScenarioIndex = 0;
    }

    private void PopulateScenarioDropdown()
    {
        if (scenarioDropdown == null)
            return;

        NormalizeScenarioDefinitions();
        scenarioDropdown.ClearOptions();
        var options = new List<Dropdown.OptionData>(scenarioDefinitions.Length);
        for (int i = 0; i < scenarioDefinitions.Length; i++)
        {
            BattleScenarioDefinition definition = scenarioDefinitions[i];
            string label = definition != null && !string.IsNullOrWhiteSpace(definition.DisplayName)
                ? definition.DisplayName
                : definition != null && !string.IsNullOrWhiteSpace(definition.ScenarioId)
                    ? definition.ScenarioId
                    : $"Scenario {i + 1}";
            options.Add(new Dropdown.OptionData(label));
        }

        scenarioDropdown.AddOptions(options);
        selectedScenarioIndex = Mathf.Clamp(selectedScenarioIndex, 0, Mathf.Max(0, scenarioDefinitions.Length - 1));
        scenarioDropdown.SetValueWithoutNotify(selectedScenarioIndex);
        scenarioDropdown.RefreshShownValue();
    }

    private static void SelectPlaybackVariant(
        BattleScenarioVariant[] variants,
        BattleScenarioResult result,
        int requestedVariantIndex,
        out BattleScenarioVariant playbackVariant,
        out BattleScenarioMetrics playbackMetrics)
    {
        if (variants.Length == 0)
            variants = BattleScenarioAd001Runner.CreateDefaultVariants();

        int preferredIndex = requestedVariantIndex >= 0 && requestedVariantIndex < variants.Length
            ? requestedVariantIndex
            : FindPreferredPlaybackIndex(variants);
        preferredIndex = Mathf.Clamp(preferredIndex, 0, variants.Length - 1);
        playbackVariant = variants[preferredIndex];

        BattleScenarioMetrics[] metrics = result != null ? result.Variants : Array.Empty<BattleScenarioMetrics>();
        playbackMetrics = metrics.Length > preferredIndex ? metrics[preferredIndex] : null;
        if (playbackMetrics == null && metrics.Length > 0)
            playbackMetrics = metrics[0];
    }

    private static int FindPreferredPlaybackIndex(BattleScenarioVariant[] variants)
    {
        for (int i = 0; i < variants.Length; i++)
        {
            BattleScenarioVariant variant = variants[i];
            if (variant.SupportMode == BattleScenarioSupportMode.RadarNear &&
                variant.IncomingThreatSpeedMultiplier > 1.1f)
                return i;
        }

        for (int i = 0; i < variants.Length; i++)
        {
            if (variants[i].SupportMode == BattleScenarioSupportMode.RadarNear)
                return i;
        }

        return 0;
    }

    private void PopulateVariantDropdown()
    {
        if (variantDropdown == null)
            return;

        variantDropdown.ClearOptions();
        BattleScenarioDefinition definition = CurrentScenarioDefinition;
        bool supportsSingleVariant = BattleScenarioLabRuntimeRunner.SupportsSingleVariantPlayback(definition);
        bool supportsVisualPlayback = visualPlayback != null && visualPlayback.CanPlay(definition);
        string allLabel = supportsSingleVariant || supportsVisualPlayback
            ? "All variants + selected live playback"
            : "Run all variants";
        var options = new List<Dropdown.OptionData>
        {
            new(allLabel)
        };

        BattleScenarioVariant[] variants = definition != null
            ? definition.ScenarioVariants
            : BattleScenarioAd001Runner.CreateDefaultVariants();
        for (int i = 0; i < variants.Length; i++)
        {
            BattleScenarioVariant variant = variants[i];
            string label = !string.IsNullOrWhiteSpace(variant.Label) ? variant.Label : variant.VariantId;
            options.Add(new Dropdown.OptionData(label));
        }

        variantDropdown.AddOptions(options);
        variantDropdown.SetValueWithoutNotify(0);
        variantDropdown.RefreshShownValue();
    }
}
