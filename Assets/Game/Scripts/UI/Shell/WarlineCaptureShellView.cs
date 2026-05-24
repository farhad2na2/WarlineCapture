using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WarlineCaptureShellView : MonoBehaviour
{
    [SerializeField] private WarlineCaptureUiMotionHostView motionHost;
    [SerializeField] private WarlineCaptureShellRegionView[] regions;

    private readonly Dictionary<WarlineCaptureShellRegionId, WarlineCaptureShellRegionView> regionById = new();

    public WarlineCaptureUiMotionHostView MotionHost => motionHost;
    public IReadOnlyList<WarlineCaptureShellRegionView> Regions => regions;

    private void Awake()
    {
        RebuildRegionLookup();
    }

    public void Configure(WarlineCaptureUiMotionHostView host, WarlineCaptureShellRegionView[] shellRegions)
    {
        motionHost = host;
        regions = shellRegions;
        RebuildRegionLookup();
    }

    public bool TryGetRegion(WarlineCaptureShellRegionId id, out WarlineCaptureShellRegionView region)
    {
        RebuildRegionLookup();
        return regionById.TryGetValue(id, out region) && region != null;
    }

    public void ExecuteCommandSequence(
        IReadOnlyList<UiShellPresentationCommandComponent> commands,
        int sequenceId,
        Action<int> completed)
    {
        if (motionHost == null || commands == null || commands.Count == 0)
        {
            completed?.Invoke(sequenceId);
            return;
        }

        int transitionId = motionHost.BeginTransition();
        List<WarlineCaptureUiMotionStep> steps = new();
        for (int i = 0; i < commands.Count; i++)
            AddStepsForCommand(commands[i], transitionId, steps);

        motionHost.PlaySequence(
            transitionId,
            () => completed?.Invoke(sequenceId),
            steps.ToArray());
    }

    private void AddStepsForCommand(
        UiShellPresentationCommandComponent command,
        int transitionId,
        List<WarlineCaptureUiMotionStep> steps)
    {
        switch (command.Kind)
        {
            case UiShellCommandKind.ShowLoading:
                AddShowLoadingSteps(transitionId, steps);
                break;
            case UiShellCommandKind.ExitLoading:
                AddExitLoadingSteps(transitionId, steps);
                break;
            case UiShellCommandKind.EnterMenu:
                AddEnterMenuSteps(transitionId, steps);
                break;
            case UiShellCommandKind.ExitMenu:
                AddExitMenuSteps(transitionId, steps);
                break;
            case UiShellCommandKind.SwapMenuMiddle:
                AddMiddleSwapSteps(transitionId, steps);
                break;
            case UiShellCommandKind.EnterMatchHud:
                AddEnterMatchHudSteps(transitionId, steps);
                break;
            case UiShellCommandKind.ExitMatchHud:
                AddExitMatchHudSteps(transitionId, steps);
                break;
            case UiShellCommandKind.ShowPopup:
                AddShowPopupSteps(transitionId, steps);
                break;
            case UiShellCommandKind.HidePopup:
                AddHidePopupSteps(transitionId, steps);
                break;
        }
    }

    private void AddShowLoadingSteps(int transitionId, List<WarlineCaptureUiMotionStep> steps)
    {
        if (!TryGetRegion(WarlineCaptureShellRegionId.LoadingLayer, out WarlineCaptureShellRegionView loading))
            return;

        loading.ResetVisualState();
        loading.CanvasGroup.alpha = 0f;
        steps.Add(WarlineCaptureUiMotionStep.Single(
            motionHost.AlphaStep(loading.CanvasGroup, 1f, motionHost.DefaultDurationSeconds, motionHost.DefaultEnterEase, transitionId)));
    }

    private void AddExitLoadingSteps(int transitionId, List<WarlineCaptureUiMotionStep> steps)
    {
        if (!TryGetRegion(WarlineCaptureShellRegionId.LoadingLayer, out WarlineCaptureShellRegionView loading))
            return;

        steps.Add(WarlineCaptureUiMotionStep.Parallel(
            motionHost.AlphaStep(loading.CanvasGroup, 0f, motionHost.DefaultDurationSeconds, motionHost.DefaultExitEase, transitionId),
            motionHost.ScaleStep(loading.RegionRoot, Vector3.zero, motionHost.DefaultDurationSeconds, motionHost.DefaultExitEase, transitionId)));
    }

    private void AddEnterMenuSteps(int transitionId, List<WarlineCaptureUiMotionStep> steps)
    {
        TryGetRegion(WarlineCaptureShellRegionId.HeaderRegion, out WarlineCaptureShellRegionView header);
        TryGetRegion(WarlineCaptureShellRegionId.LeftRegion, out WarlineCaptureShellRegionView left);
        TryGetRegion(WarlineCaptureShellRegionId.RightRegion, out WarlineCaptureShellRegionView right);
        TryGetRegion(WarlineCaptureShellRegionId.MiddleRegion, out WarlineCaptureShellRegionView middle);

        PrimeOffscreen(header);
        PrimeOffscreen(left);
        PrimeOffscreen(right);
        if (middle != null)
            middle.RegionRoot.localScale = Vector3.zero;

        AddRegionEnterStep(steps, header, transitionId);
        steps.Add(WarlineCaptureUiMotionStep.Parallel(
            RegionEnterFactory(left, transitionId),
            RegionEnterFactory(right, transitionId)));
        AddScaleStep(steps, middle, Vector3.one, motionHost.DefaultEnterEase, transitionId);
    }

    private void AddExitMenuSteps(int transitionId, List<WarlineCaptureUiMotionStep> steps)
    {
        TryGetRegion(WarlineCaptureShellRegionId.HeaderRegion, out WarlineCaptureShellRegionView header);
        TryGetRegion(WarlineCaptureShellRegionId.LeftRegion, out WarlineCaptureShellRegionView left);
        TryGetRegion(WarlineCaptureShellRegionId.RightRegion, out WarlineCaptureShellRegionView right);
        TryGetRegion(WarlineCaptureShellRegionId.MiddleRegion, out WarlineCaptureShellRegionView middle);

        steps.Add(WarlineCaptureUiMotionStep.Parallel(
            RegionExitFactory(header, transitionId),
            RegionExitFactory(left, transitionId),
            RegionExitFactory(right, transitionId),
            RegionScaleFactory(middle, Vector3.zero, motionHost.DefaultExitEase, transitionId)));
    }

    private void AddMiddleSwapSteps(int transitionId, List<WarlineCaptureUiMotionStep> steps)
    {
        if (!TryGetRegion(WarlineCaptureShellRegionId.MiddleRegion, out WarlineCaptureShellRegionView middle))
            return;

        steps.Add(WarlineCaptureUiMotionStep.Single(
            motionHost.ScaleStep(middle.RegionRoot, Vector3.zero, motionHost.DefaultDurationSeconds, motionHost.DefaultSwapEase, transitionId)));
        steps.Add(WarlineCaptureUiMotionStep.Single(
            motionHost.ScaleStep(middle.RegionRoot, Vector3.one, motionHost.DefaultDurationSeconds, motionHost.DefaultSwapEase, transitionId)));
    }

    private void AddEnterMatchHudSteps(int transitionId, List<WarlineCaptureUiMotionStep> steps)
    {
        TryGetRegion(WarlineCaptureShellRegionId.HeaderRegion, out WarlineCaptureShellRegionView header);
        TryGetRegion(WarlineCaptureShellRegionId.LeftRegion, out WarlineCaptureShellRegionView left);
        TryGetRegion(WarlineCaptureShellRegionId.RightRegion, out WarlineCaptureShellRegionView right);
        TryGetRegion(WarlineCaptureShellRegionId.FooterRegion, out WarlineCaptureShellRegionView footer);

        PrimeOffscreen(header);
        PrimeOffscreen(left);
        PrimeOffscreen(right);
        PrimeOffscreen(footer);

        steps.Add(WarlineCaptureUiMotionStep.Parallel(
            RegionEnterFactory(header, transitionId),
            RegionEnterFactory(left, transitionId),
            RegionEnterFactory(right, transitionId),
            RegionEnterFactory(footer, transitionId)));
    }

    private void AddExitMatchHudSteps(int transitionId, List<WarlineCaptureUiMotionStep> steps)
    {
        TryGetRegion(WarlineCaptureShellRegionId.HeaderRegion, out WarlineCaptureShellRegionView header);
        TryGetRegion(WarlineCaptureShellRegionId.LeftRegion, out WarlineCaptureShellRegionView left);
        TryGetRegion(WarlineCaptureShellRegionId.RightRegion, out WarlineCaptureShellRegionView right);
        TryGetRegion(WarlineCaptureShellRegionId.FooterRegion, out WarlineCaptureShellRegionView footer);

        steps.Add(WarlineCaptureUiMotionStep.Parallel(
            RegionExitFactory(header, transitionId),
            RegionExitFactory(left, transitionId),
            RegionExitFactory(right, transitionId),
            RegionExitFactory(footer, transitionId)));
    }

    private void AddShowPopupSteps(int transitionId, List<WarlineCaptureUiMotionStep> steps)
    {
        if (!TryGetRegion(WarlineCaptureShellRegionId.PopupLayer, out WarlineCaptureShellRegionView popup))
            return;

        popup.ResetVisualState();
        popup.RegionRoot.localScale = Vector3.zero;
        popup.CanvasGroup.alpha = 0f;
        steps.Add(WarlineCaptureUiMotionStep.Parallel(
            motionHost.ScaleStep(popup.RegionRoot, Vector3.one, motionHost.DefaultDurationSeconds, WarlineCaptureUiEase.EaseOutBackSubtle, transitionId),
            motionHost.AlphaStep(popup.CanvasGroup, 1f, motionHost.DefaultDurationSeconds, motionHost.DefaultEnterEase, transitionId)));
    }

    private void AddHidePopupSteps(int transitionId, List<WarlineCaptureUiMotionStep> steps)
    {
        if (!TryGetRegion(WarlineCaptureShellRegionId.PopupLayer, out WarlineCaptureShellRegionView popup))
            return;

        steps.Add(WarlineCaptureUiMotionStep.Parallel(
            motionHost.ScaleStep(popup.RegionRoot, Vector3.zero, motionHost.DefaultDurationSeconds, motionHost.DefaultExitEase, transitionId),
            motionHost.AlphaStep(popup.CanvasGroup, 0f, motionHost.DefaultDurationSeconds, motionHost.DefaultExitEase, transitionId)));
    }

    private void AddRegionEnterStep(List<WarlineCaptureUiMotionStep> steps, WarlineCaptureShellRegionView region, int transitionId)
    {
        if (region == null)
            return;

        steps.Add(WarlineCaptureUiMotionStep.Single(RegionEnterFactory(region, transitionId)));
    }

    private void AddScaleStep(
        List<WarlineCaptureUiMotionStep> steps,
        WarlineCaptureShellRegionView region,
        Vector3 scale,
        WarlineCaptureUiEase ease,
        int transitionId)
    {
        if (region == null)
            return;

        steps.Add(WarlineCaptureUiMotionStep.Single(RegionScaleFactory(region, scale, ease, transitionId)));
    }

    private Func<System.Collections.IEnumerator> RegionEnterFactory(WarlineCaptureShellRegionView region, int transitionId)
    {
        if (region == null)
            return EmptyStep;

        region.CanvasGroup.alpha = 1f;
        return motionHost.AnchoredPositionStep(region.RegionRoot, region.OnScreenAnchoredPosition, motionHost.DefaultDurationSeconds, motionHost.DefaultEnterEase, transitionId);
    }

    private Func<System.Collections.IEnumerator> RegionExitFactory(WarlineCaptureShellRegionView region, int transitionId)
    {
        if (region == null)
            return EmptyStep;

        return motionHost.AnchoredPositionStep(region.RegionRoot, OffscreenPosition(region), motionHost.DefaultDurationSeconds, motionHost.DefaultExitEase, transitionId);
    }

    private Func<System.Collections.IEnumerator> RegionScaleFactory(
        WarlineCaptureShellRegionView region,
        Vector3 scale,
        WarlineCaptureUiEase ease,
        int transitionId)
    {
        if (region == null)
            return EmptyStep;

        return motionHost.ScaleStep(region.RegionRoot, scale, motionHost.DefaultDurationSeconds, ease, transitionId);
    }

    private void PrimeOffscreen(WarlineCaptureShellRegionView region)
    {
        if (region == null)
            return;

        region.ResetVisualState();
        region.RegionRoot.anchoredPosition = OffscreenPosition(region);
    }

    private Vector2 OffscreenPosition(WarlineCaptureShellRegionView region)
    {
        Rect rect = region.RegionRoot.rect;
        Vector2 direction = region.OffScreenDirection;
        Vector2 offset = new(
            direction.x * (Mathf.Max(rect.width, 1f) + 96f),
            direction.y * (Mathf.Max(rect.height, 1f) + 96f));
        return region.OnScreenAnchoredPosition + offset;
    }

    private void RebuildRegionLookup()
    {
        regionById.Clear();
        if (regions == null)
            return;

        for (int i = 0; i < regions.Length; i++)
        {
            WarlineCaptureShellRegionView region = regions[i];
            if (region != null)
                regionById[region.RegionId] = region;
        }
    }

    private static System.Collections.IEnumerator EmptyStep()
    {
        yield break;
    }
}
