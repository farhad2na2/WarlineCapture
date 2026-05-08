using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureOperationModalFlow : MonoBehaviour
{
    [SerializeField] private Transform modalOverlay;
    [SerializeField] private GameObject confirmRaidPopupPrefab;
    [SerializeField] private GameObject endOfDayReportPopupPrefab;
    [SerializeField] private GameObject intelRevealPopupPrefab;

    private GameObject _activePopup;

    public bool HasActivePopup => _activePopup != null;

    public void ShowEndOfDayReport(OperationSaveData state, Action onContinue)
    {
        GameObject popup = ShowPopup(endOfDayReportPopupPrefab);
        if (popup == null)
            return;

        int totalStability = Average(state, district => district.stability);
        int totalThreat = Average(state, district => district.threat);
        int totalTrust = Average(state, district => district.trust);
        int totalSecurity = Average(state, district => district.security);
        int totalHeat = Average(state, district => district.heat);
        int totalCivilianRisk = Average(state, district => district.civilianRisk);
        SetText(popup, "Frame/Header/DayTag/DayText", $"DAY {state.operationDay}");
        SetText(popup, "Frame/BodyRoot/DeltaSummary/LabelText", "OPERATION DAY RESOLVED");
        SetText(popup, "Frame/BodyRoot/DeltaSummary/ValueText", $"{totalStability}");
        SetText(popup, "Frame/BodyRoot/DeltaSummary/DeltaText", $"Stability {totalStability}. Trust {totalTrust}. Heat {totalHeat}. Civilian risk {totalCivilianRisk}.");
        SetText(popup, "Frame/BodyRoot/TrustStabilityPanel/CivilianTrustRow/ValueText", $"{totalTrust}");
        SetText(popup, "Frame/BodyRoot/TrustStabilityPanel/CivilianTrustRow/DeltaText", "-1 day pressure");
        SetText(popup, "Frame/BodyRoot/TrustStabilityPanel/RegionStabilityRow/ValueText", $"{totalSecurity}");
        SetText(popup, "Frame/BodyRoot/TrustStabilityPanel/RegionStabilityRow/DeltaText", "Security watch");
        SetText(popup, "Frame/BodyRoot/EnemyActivityPanel/ThreatValueText", $"{totalThreat}");
        SetText(popup, "Frame/BodyRoot/EnemyActivityPanel/TrendLabelText", $"Enemy pressure rose. Avg Heat {totalHeat}, civilian risk {totalCivilianRisk}.");
        SetText(popup, "Frame/BodyRoot/SaveStatusRow/StatusText", "Operation state updated locally.");

        BindCloseButton(popup, "Frame/ButtonRow/SaveContinueButton", onContinue);
    }

    public void ShowIntelReveal(DistrictStateData district, Action onClose)
    {
        GameObject popup = ShowPopup(intelRevealPopupPrefab);
        if (popup == null)
            return;

        string districtName = OperationDashboardScreenController.FormatDistrictName(district.districtId);
        OperationIntelEvidenceData evidence = WarlineCaptureOperationRuntime.LatestEvidence(district.districtId);
        SetText(popup, "Frame/Header/TitleText", "INTEL REVEALED");
        SetText(popup, "Frame/BodyRoot/SubheadingText", evidence != null ? evidence.body : $"{districtName} scan raised confidence to {district.intel}.");
        SetText(popup, "Frame/BodyRoot/SupplyLedgerCard/TitleText", "SUPPLY LEDGER");
        SetText(popup, "Frame/BodyRoot/SupplyLedgerCard/ConfidenceChip/ConfidenceText", $"{district.intel}%");
        SetText(popup, "Frame/BodyRoot/CargoManifestCard/TitleText", evidence != null ? evidence.title.ToUpperInvariant() : "CARGO MANIFEST");
        SetText(popup, "Frame/BodyRoot/CargoManifestCard/ConfidenceChip/ConfidenceText", $"{Mathf.Clamp(district.intel - 8, 0, 100)}%");
        SetText(popup, "Frame/BodyRoot/RadioInterceptCard/TitleText", "RADIO INTERCEPT");
        SetText(popup, "Frame/BodyRoot/RadioInterceptCard/ConfidenceChip/ConfidenceText", $"{Mathf.Clamp(district.intel + 6, 0, 100)}%");
        SetText(popup, "Frame/BodyRoot/NoticeBar/NoticeText", "Intel improves raid certainty but does not remove operational risk.");

        BindCloseButton(popup, "Frame/CloseButton", onClose);
        BindCloseButton(popup, "Frame/ButtonRow/CloseButton", onClose);
        BindCloseButton(popup, "Frame/ButtonRow/ViewIntelButton", () =>
        {
            if (evidence != null)
                WarlineCaptureOperationRuntime.MarkEvidenceRead(evidence.evidenceId);

            onClose?.Invoke();
        });
    }

    public void ShowConfirmRaid(DistrictStateData district, Action onConfirm)
    {
        GameObject popup = ShowPopup(confirmRaidPopupPrefab);
        if (popup == null)
            return;

        string districtName = OperationDashboardScreenController.FormatDistrictName(district.districtId);
        SetText(popup, "Frame/BodyRoot/TargetPanel/TargetNameText", "BREACH ASSAULT");
        SetText(popup, "Frame/BodyRoot/TargetPanel/TargetInfoCard/DistrictText", districtName);
        SetText(popup, "Frame/BodyRoot/TargetPanel/TargetInfoCard/ThreatText", $"Threat {district.threat} / Heat {district.heat}");
        SetText(popup, "Frame/BodyRoot/RiskPanel/IntelConfidenceRow/ValueText", $"{district.intel}%");
        SetText(popup, "Frame/BodyRoot/RiskPanel/CollateralRiskRow/ValueText", RiskLabel(district.heat));
        SetText(popup, "Frame/BodyRoot/RiskPanel/CivilianDensityRow/ValueText", RiskLabel(district.civilianRisk));
        SetText(popup, "Frame/BodyRoot/RiskPanel/WarningTextPanel/WarningText", $"Security {district.security}. Trust {district.trust}. Raid may raise heat and civilian risk before returning to Operation Dashboard.");

        BindCloseButton(popup, "Frame/CloseButton", null);
        BindCloseButton(popup, "Frame/ButtonRow/CancelButton", null);
        BindCloseButton(popup, "Frame/ButtonRow/ConfirmButton", onConfirm);
    }

    public void CloseActivePopup()
    {
        if (_activePopup != null)
        {
            DestroyPopup(_activePopup);
            _activePopup = null;
        }

        if (modalOverlay != null)
            modalOverlay.gameObject.SetActive(false);
    }

    private GameObject ShowPopup(GameObject prefab)
    {
        if (prefab == null)
            return null;

        CloseActivePopup();
        if (modalOverlay == null)
            modalOverlay = transform;

        modalOverlay.gameObject.SetActive(true);
        _activePopup = Instantiate(prefab, modalOverlay, false);
        RectTransform rect = _activePopup.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        return _activePopup;
    }

    private void BindCloseButton(GameObject popup, string path, Action afterClose)
    {
        Transform target = popup.transform.Find(path);
        if (target == null || !target.TryGetComponent(out Button button))
            return;

        button.onClick.AddListener(() =>
        {
            CloseActivePopup();
            afterClose?.Invoke();
        });
    }

    private static void SetText(GameObject root, string path, string value)
    {
        Transform target = root.transform.Find(path);
        if (target != null && target.TryGetComponent(out TMP_Text text))
            text.text = value;
    }

    private static int Average(OperationSaveData state, Func<DistrictStateData, int> selector)
    {
        if (state == null || state.districts == null || state.districts.Length == 0)
            return 0;

        int total = 0;
        int count = 0;
        foreach (DistrictStateData district in state.districts)
        {
            if (district == null)
                continue;

            total += selector(district);
            count++;
        }

        return count == 0 ? 0 : Mathf.RoundToInt((float)total / count);
    }

    private static string RiskLabel(int value)
    {
        if (value >= 82)
            return "CRITICAL";
        if (value >= 65)
            return "HIGH";
        if (value >= 45)
            return "ELEVATED";

        return "LOW";
    }

    private static void DestroyPopup(GameObject popup)
    {
        if (Application.isPlaying)
            Destroy(popup);
        else
            DestroyImmediate(popup);
    }
}
