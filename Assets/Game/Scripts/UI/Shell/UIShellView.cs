using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class UIShellView : MonoBehaviour
{
    [SerializeField] private UIMotionHostView motionHost;
    [SerializeField] private UIShellRegionView[] regions;
    [FormerlySerializedAs("contentPresenter")]
    [SerializeField] private UIShellContentView contentSystem;
    [SerializeField] private MatchIntroCurtainView matchIntroCurtain;

    private readonly Dictionary<UIShellRegionId, UIShellRegionView> regionById = new();

    public UIMotionHostView MotionHost => motionHost;
    public IReadOnlyList<UIShellRegionView> Regions => regions;
    public UIShellContentView ContentSystem => contentSystem;
    public MatchIntroCurtainView MatchIntroCurtain => matchIntroCurtain;

    private void Awake()
    {
        if (contentSystem == null)
            contentSystem = GetComponent<UIShellContentView>();
        RebuildRegionLookup();
    }

    public void Configure(UIMotionHostView host, UIShellRegionView[] shellRegions)
    {
        motionHost = host;
        regions = shellRegions;
        RebuildRegionLookup();
    }

    public void Configure(
        UIMotionHostView host,
        UIShellRegionView[] shellRegions,
        UIShellContentView shellContentSystem)
    {
        Configure(host, shellRegions);
        contentSystem = shellContentSystem;
    }

    public void SetContentSystem(UIShellContentView shellContentSystem)
    {
        contentSystem = shellContentSystem;
    }

    public void SetMatchIntroCurtain(MatchIntroCurtainView curtain)
    {
        matchIntroCurtain = curtain;
    }

    public bool TryGetRegion(UIShellRegionId id, out UIShellRegionView region)
    {
        RebuildRegionLookup();
        return regionById.TryGetValue(id, out region) && region != null;
    }

    public void ExecuteCommandSequence(
        IReadOnlyList<UiShellPresentationCommandModel> commands,
        int sequenceId,
        Action<int> completed)
    {
        if (motionHost == null || commands == null || commands.Count == 0)
        {
            completed?.Invoke(sequenceId);
            return;
        }

        contentSystem?.PrepareForCommandSequence(commands);

        int transitionId = motionHost.BeginTransition();
        List<UIMotionStep> steps = new();
        for (int i = 0; i < commands.Count; i++)
            AddStepsForCommand(commands[i], transitionId, steps);

        motionHost.PlaySequence(
            transitionId,
            () => completed?.Invoke(sequenceId),
            steps.ToArray());
    }

    private void AddStepsForCommand(
        UiShellPresentationCommandModel command,
        int transitionId,
        List<UIMotionStep> steps)
    {
        switch (command.Kind)
        {
            case UiShellCommandKind.ShowLoading:
                AddShowLoadingSteps(command.Route, transitionId, steps);
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
                AddMenuBodySwapSteps(command.Route, transitionId, steps);
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

    private void AddShowLoadingSteps(UIRoute route, int transitionId, List<UIMotionStep> steps)
    {
        if (!TryGetRegion(UIShellRegionId.LoadingLayer, out UIShellRegionView loading))
            return;

        if (route == UIRoute.Match)
            matchIntroCurtain?.ShowOpaque();
        else
            matchIntroCurtain?.SetVisible(false);

        loading.ResetVisualState();
        loading.CanvasGroup.alpha = 1f;
        steps.Add(UIMotionStep.Single(
            motionHost.AlphaStep(loading.CanvasGroup, 1f, 0f, motionHost.DefaultEnterEase, transitionId)));
    }

    private void AddExitLoadingSteps(int transitionId, List<UIMotionStep> steps)
    {
        if (!TryGetRegion(UIShellRegionId.LoadingLayer, out UIShellRegionView loading))
            return;

        steps.Add(UIMotionStep.Parallel(
            motionHost.AlphaStep(loading.CanvasGroup, 0f, motionHost.DefaultDurationSeconds, motionHost.DefaultExitEase, transitionId),
            motionHost.ScaleStep(loading.RegionRoot, Vector3.zero, motionHost.DefaultDurationSeconds, motionHost.DefaultExitEase, transitionId)));
    }

    private void AddEnterMenuSteps(int transitionId, List<UIMotionStep> steps)
    {
        TryGetRegion(UIShellRegionId.MenuBackgroundRegion, out UIShellRegionView background);
        TryGetRegion(UIShellRegionId.HeaderRegion, out UIShellRegionView header);
        TryGetRegion(UIShellRegionId.LeftRegion, out UIShellRegionView left);
        TryGetRegion(UIShellRegionId.RightRegion, out UIShellRegionView right);
        TryGetRegion(UIShellRegionId.MiddleRegion, out UIShellRegionView middle);
        TryGetRegion(UIShellRegionId.FooterRegion, out UIShellRegionView footer);

        if (background != null)
        {
            background.ResetVisualState();
            background.CanvasGroup.alpha = 0f;
        }

        PrimeOffscreen(header);
        PrimeOffscreen(left);
        PrimeOffscreen(right);
        PrimeOffscreen(footer);
        if (middle != null)
            middle.RegionRoot.localScale = Vector3.zero;

        AddAlphaStep(steps, background, 1f, motionHost.DefaultEnterEase, transitionId);
        AddRegionEnterStep(steps, header, transitionId);
        steps.Add(UIMotionStep.Parallel(
            RegionEnterFactory(left, transitionId),
            RegionEnterFactory(right, transitionId),
            RegionEnterFactory(footer, transitionId)));
        AddScaleStep(steps, middle, Vector3.one, motionHost.DefaultEnterEase, transitionId);
    }

    private void AddExitMenuSteps(int transitionId, List<UIMotionStep> steps)
    {
        TryGetRegion(UIShellRegionId.MenuBackgroundRegion, out UIShellRegionView background);
        TryGetRegion(UIShellRegionId.HeaderRegion, out UIShellRegionView header);
        TryGetRegion(UIShellRegionId.LeftRegion, out UIShellRegionView left);
        TryGetRegion(UIShellRegionId.RightRegion, out UIShellRegionView right);
        TryGetRegion(UIShellRegionId.MiddleRegion, out UIShellRegionView middle);
        TryGetRegion(UIShellRegionId.FooterRegion, out UIShellRegionView footer);

        steps.Add(UIMotionStep.Parallel(
            RegionAlphaFactory(background, 0f, motionHost.DefaultExitEase, transitionId),
            RegionExitFactory(header, transitionId),
            RegionExitFactory(left, transitionId),
            RegionExitFactory(right, transitionId),
            RegionExitFactory(footer, transitionId),
            RegionScaleFactory(middle, Vector3.zero, motionHost.DefaultExitEase, transitionId)));
    }

    private void AddMenuBodySwapSteps(UIRoute route, int transitionId, List<UIMotionStep> steps)
    {
        TryGetRegion(UIShellRegionId.LeftRegion, out UIShellRegionView left);
        TryGetRegion(UIShellRegionId.MiddleRegion, out UIShellRegionView middle);
        TryGetRegion(UIShellRegionId.RightRegion, out UIShellRegionView right);
        TryGetRegion(UIShellRegionId.FooterRegion, out UIShellRegionView footer);

        steps.Add(UIMotionStep.Parallel(
            RegionExitFactory(left, transitionId),
            RegionScaleFactory(middle, Vector3.zero, motionHost.DefaultSwapEase, transitionId),
            RegionExitFactory(right, transitionId),
            RegionExitFactory(footer, transitionId)));
        steps.Add(UIMotionStep.Single(() => SwapMenuRouteBodyRoutine(route, left, middle, right, footer)));
        steps.Add(UIMotionStep.Parallel(
            RegionEnterFactory(left, transitionId),
            RegionScaleFactory(middle, Vector3.one, motionHost.DefaultSwapEase, transitionId),
            RegionEnterFactory(right, transitionId),
            RegionEnterFactory(footer, transitionId)));
    }

    private void AddEnterMatchHudSteps(int transitionId, List<UIMotionStep> steps)
    {
        TryGetRegion(UIShellRegionId.MenuBackgroundRegion, out UIShellRegionView background);
        TryGetRegion(UIShellRegionId.HeaderRegion, out UIShellRegionView header);
        TryGetRegion(UIShellRegionId.LeftRegion, out UIShellRegionView left);
        TryGetRegion(UIShellRegionId.RightRegion, out UIShellRegionView right);
        TryGetRegion(UIShellRegionId.FooterRegion, out UIShellRegionView footer);

        if (background != null && background.CanvasGroup != null)
            background.CanvasGroup.alpha = 0f;

        matchIntroCurtain?.ShowOpaque();
        PrimeOffscreen(header);
        PrimeOffscreen(left);
        PrimeOffscreen(right);
        PrimeOffscreen(footer);

        steps.Add(UIMotionStep.Parallel(
            RegionEnterFactory(header, transitionId),
            RegionEnterFactory(left, transitionId),
            RegionEnterFactory(right, transitionId),
            RegionEnterFactory(footer, transitionId)));
        AddMatchIntroCurtainFadeOutStep(steps, transitionId);
    }

    private void AddExitMatchHudSteps(int transitionId, List<UIMotionStep> steps)
    {
        TryGetRegion(UIShellRegionId.HeaderRegion, out UIShellRegionView header);
        TryGetRegion(UIShellRegionId.LeftRegion, out UIShellRegionView left);
        TryGetRegion(UIShellRegionId.RightRegion, out UIShellRegionView right);
        TryGetRegion(UIShellRegionId.FooterRegion, out UIShellRegionView footer);

        steps.Add(UIMotionStep.Parallel(
            RegionExitFactory(header, transitionId),
            RegionExitFactory(left, transitionId),
            RegionExitFactory(right, transitionId),
            RegionExitFactory(footer, transitionId)));
    }

    private void AddShowPopupSteps(int transitionId, List<UIMotionStep> steps)
    {
        if (!TryGetRegion(UIShellRegionId.PopupLayer, out UIShellRegionView popup))
            return;

        popup.ResetVisualState();
        popup.RegionRoot.localScale = Vector3.zero;
        popup.CanvasGroup.alpha = 0f;
        steps.Add(UIMotionStep.Parallel(
            motionHost.ScaleStep(popup.RegionRoot, Vector3.one, motionHost.DefaultDurationSeconds, UIEase.EaseOutBackSubtle, transitionId),
            motionHost.AlphaStep(popup.CanvasGroup, 1f, motionHost.DefaultDurationSeconds, motionHost.DefaultEnterEase, transitionId)));
    }

    private void AddHidePopupSteps(int transitionId, List<UIMotionStep> steps)
    {
        if (!TryGetRegion(UIShellRegionId.PopupLayer, out UIShellRegionView popup))
            return;

        steps.Add(UIMotionStep.Parallel(
            motionHost.ScaleStep(popup.RegionRoot, Vector3.zero, motionHost.DefaultDurationSeconds, motionHost.DefaultExitEase, transitionId),
            motionHost.AlphaStep(popup.CanvasGroup, 0f, motionHost.DefaultDurationSeconds, motionHost.DefaultExitEase, transitionId)));
    }

    private void AddRegionEnterStep(List<UIMotionStep> steps, UIShellRegionView region, int transitionId)
    {
        if (region == null)
            return;

        steps.Add(UIMotionStep.Single(RegionEnterFactory(region, transitionId)));
    }

    private void AddScaleStep(
        List<UIMotionStep> steps,
        UIShellRegionView region,
        Vector3 scale,
        UIEase ease,
        int transitionId)
    {
        if (region == null)
            return;

        steps.Add(UIMotionStep.Single(RegionScaleFactory(region, scale, ease, transitionId)));
    }

    private void AddAlphaStep(
        List<UIMotionStep> steps,
        UIShellRegionView region,
        float alpha,
        UIEase ease,
        int transitionId)
    {
        if (region == null)
            return;

        steps.Add(UIMotionStep.Single(RegionAlphaFactory(region, alpha, ease, transitionId)));
    }

    private void AddMatchIntroCurtainFadeOutStep(List<UIMotionStep> steps, int transitionId)
    {
        if (matchIntroCurtain == null || matchIntroCurtain.CanvasGroup == null)
            return;

        steps.Add(UIMotionStep.Single(
            motionHost.AlphaStep(
                matchIntroCurtain.CanvasGroup,
                0f,
                motionHost.DefaultDurationSeconds,
                motionHost.DefaultExitEase,
                transitionId)));
        steps.Add(UIMotionStep.Single(() => HideMatchIntroCurtainRoutine()));
    }

    private Func<System.Collections.IEnumerator> RegionEnterFactory(UIShellRegionView region, int transitionId)
    {
        if (region == null)
            return EmptyStep;

        region.CanvasGroup.alpha = 1f;
        return motionHost.AnchoredPositionStep(region.RegionRoot, region.OnScreenAnchoredPosition, motionHost.DefaultDurationSeconds, motionHost.DefaultEnterEase, transitionId);
    }

    private Func<System.Collections.IEnumerator> RegionExitFactory(UIShellRegionView region, int transitionId)
    {
        if (region == null)
            return EmptyStep;

        return motionHost.AnchoredPositionStep(region.RegionRoot, OffscreenPosition(region), motionHost.DefaultDurationSeconds, motionHost.DefaultExitEase, transitionId);
    }

    private Func<System.Collections.IEnumerator> RegionScaleFactory(
        UIShellRegionView region,
        Vector3 scale,
        UIEase ease,
        int transitionId)
    {
        if (region == null)
            return EmptyStep;

        return motionHost.ScaleStep(region.RegionRoot, scale, motionHost.DefaultDurationSeconds, ease, transitionId);
    }

    private Func<System.Collections.IEnumerator> RegionAlphaFactory(
        UIShellRegionView region,
        float alpha,
        UIEase ease,
        int transitionId)
    {
        if (region == null)
            return EmptyStep;

        return motionHost.AlphaStep(region.CanvasGroup, alpha, motionHost.DefaultDurationSeconds, ease, transitionId);
    }

    private void PrimeOffscreen(UIShellRegionView region)
    {
        if (region == null)
            return;

        region.ResetVisualState();
        region.RegionRoot.anchoredPosition = OffscreenPosition(region);
    }

    private System.Collections.IEnumerator SwapMenuRouteBodyRoutine(
        UIRoute route,
        UIShellRegionView left,
        UIShellRegionView middle,
        UIShellRegionView right,
        UIShellRegionView footer)
    {
        contentSystem?.InstallMenuRouteBody(route);

        if (left != null)
        {
            left.CanvasGroup.alpha = 1f;
            left.RegionRoot.anchoredPosition = OffscreenPosition(left);
            left.RegionRoot.localScale = Vector3.one;
        }

        if (middle != null)
        {
            middle.CanvasGroup.alpha = 1f;
            middle.RegionRoot.anchoredPosition = middle.OnScreenAnchoredPosition;
            middle.RegionRoot.localScale = Vector3.zero;
        }

        if (right != null)
        {
            right.CanvasGroup.alpha = 1f;
            right.RegionRoot.anchoredPosition = OffscreenPosition(right);
            right.RegionRoot.localScale = Vector3.one;
        }

        if (footer != null)
        {
            footer.CanvasGroup.alpha = 1f;
            footer.RegionRoot.anchoredPosition = OffscreenPosition(footer);
            footer.RegionRoot.localScale = Vector3.one;
        }

        yield break;
    }

    private Vector2 OffscreenPosition(UIShellRegionView region)
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
            UIShellRegionView region = regions[i];
            if (region != null)
                regionById[region.RegionId] = region;
        }
    }

    private static System.Collections.IEnumerator EmptyStep()
    {
        yield break;
    }

    private System.Collections.IEnumerator HideMatchIntroCurtainRoutine()
    {
        matchIntroCurtain?.HideIfTransparent();
        yield break;
    }
}
