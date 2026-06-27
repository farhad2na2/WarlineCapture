using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleScenarioLabPlayBootstrap : MonoBehaviour
{
    [SerializeField] private BattleScenarioDefinition scenarioDefinition;
    [SerializeField] private BattleScenarioLabOverlayView overlayView;
    [SerializeField] private BattleScenarioLabVisualPlayback visualPlayback;
    [SerializeField] private Dropdown variantDropdown;
    [SerializeField] private bool runOnStart = true;

    private void Start()
    {
        PopulateVariantDropdown();
        if (!runOnStart)
        {
            overlayView?.ShowPending(scenarioDefinition);
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
            visualPlayback?.Play(playbackVariant, playbackMetrics);
        }
        catch (Exception ex)
        {
            overlayView?.ShowError(ex.Message);
            Debug.LogError($"[BattleScenarioLab] Scenario run failed: {ex}");
        }
    }

    private BattleScenarioResult RunSelectedScenario(
        out BattleScenarioVariant playbackVariant,
        out BattleScenarioMetrics playbackMetrics)
    {
        BattleScenarioDefinition definition = scenarioDefinition;
        BattleScenarioVariant[] variants = definition != null && definition.ScenarioVariants.Length > 0
            ? definition.ScenarioVariants
            : BattleScenarioAd001Runner.CreateDefaultVariants();

        if (definition == null)
        {
            BattleScenarioResult defaultResult = BattleScenarioAd001Runner.RunDefault();
            SelectPlaybackVariant(variants, defaultResult, out playbackVariant, out playbackMetrics);
            return defaultResult;
        }

        int selectedVariantIndex = variantDropdown != null ? variantDropdown.value - 1 : -1;
        if (selectedVariantIndex < 0 || selectedVariantIndex >= variants.Length)
        {
            BattleScenarioResult result = BattleScenarioAd001Runner.RunDefinition(definition);
            SelectPlaybackVariant(variants, result, out playbackVariant, out playbackMetrics);
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

    private static void SelectPlaybackVariant(
        BattleScenarioVariant[] variants,
        BattleScenarioResult result,
        out BattleScenarioVariant playbackVariant,
        out BattleScenarioMetrics playbackMetrics)
    {
        int preferredIndex = FindPreferredPlaybackIndex(variants);
        if (variants.Length == 0)
            variants = BattleScenarioAd001Runner.CreateDefaultVariants();

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
        var options = new List<Dropdown.OptionData>
        {
            new("All AD-001 variants")
        };

        BattleScenarioVariant[] variants = scenarioDefinition != null
            ? scenarioDefinition.ScenarioVariants
            : BattleScenarioAd001Runner.CreateDefaultVariants();
        for (int i = 0; i < variants.Length; i++)
        {
            BattleScenarioVariant variant = variants[i];
            string label = !string.IsNullOrWhiteSpace(variant.Label) ? variant.Label : variant.VariantId;
            options.Add(new Dropdown.OptionData(label));
        }

        variantDropdown.AddOptions(options);
        variantDropdown.value = 0;
        variantDropdown.RefreshShownValue();
    }
}
