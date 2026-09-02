using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;

public sealed class MatchHudCurrentOrderBannerTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";

    [Test]
    public void Model_FromCommand_UsesSharedCommandText()
    {
        MatchHudCurrentOrderBannerModel model = MatchHudCurrentOrderBannerUiSystemHelper.BuildCommandModeBanner(TacticalCommandMode.Move, null);

        Assert.That(model.Visible, Is.True);
        Assert.That(model.CommandMode, Is.EqualTo(TacticalCommandMode.Move));
        Assert.That(model.OrderText, Is.EqualTo("MOVE ORDER"));
        Assert.That(model.DescriptionText, Is.EqualTo("Select a destination."));
        Assert.That(model.ChevronsVisible, Is.True);
    }

    [Test]
    public void View_OnEnable_HidesBannerAndClearsStaleVisuals()
    {
        using TestBannerRig rig = new();
        Sprite sprite = CreateSprite();

        rig.Root.SetActive(true);
        rig.Chevrons.SetActive(true);
        rig.Icon.sprite = sprite;
        rig.Icon.enabled = true;
        rig.OrderText.text = "STALE";
        rig.DescriptionText.text = "STALE DESCRIPTION";

        typeof(MatchHudCurrentOrderBannerView)
            .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(rig.View, null);

        Assert.That(rig.Root.activeSelf, Is.False);
        Assert.That(rig.Chevrons.activeSelf, Is.False);
        Assert.That(rig.Icon.enabled, Is.False);
        Assert.That(rig.Icon.sprite, Is.Null);
        Assert.That(rig.OrderText.text, Is.Empty);
        Assert.That(rig.DescriptionText.text, Is.Empty);

        Object.DestroyImmediate(sprite.texture);
    }

    [Test]
    public void View_ApplyVisibleModel_ShowsCommandContent()
    {
        using TestBannerRig rig = new();
        Sprite sprite = CreateSprite();
        MatchHudCurrentOrderBannerModel model = new(
            true,
            TacticalCommandMode.Attack,
            "ATTACK ORDER",
            "Tap hostile target.",
            sprite);

        rig.View.Apply(model);

        Assert.That(rig.Root.activeSelf, Is.True);
        Assert.That(rig.Chevrons.activeSelf, Is.True);
        Assert.That(rig.Icon.enabled, Is.True);
        Assert.That(rig.Icon.sprite, Is.SameAs(sprite));
        Assert.That(rig.Icon.preserveAspect, Is.True);
        Assert.That(rig.OrderText.text, Is.EqualTo("ATTACK ORDER"));
        Assert.That(rig.DescriptionText.text, Is.EqualTo("Tap hostile target."));

        Object.DestroyImmediate(sprite.texture);
    }

    [Test]
    public void View_Hide_ClearsCommandContent()
    {
        using TestBannerRig rig = new();
        Sprite sprite = CreateSprite();
        rig.View.Apply(new MatchHudCurrentOrderBannerModel(
            true,
            TacticalCommandMode.Scan,
            "SCAN ORDER",
            "Tap scan area.",
            sprite));

        rig.View.Hide();

        Assert.That(rig.Root.activeSelf, Is.False);
        Assert.That(rig.Chevrons.activeSelf, Is.False);
        Assert.That(rig.Icon.enabled, Is.False);
        Assert.That(rig.Icon.sprite, Is.Null);
        Assert.That(rig.OrderText.text, Is.Empty);
        Assert.That(rig.DescriptionText.text, Is.Empty);

        Object.DestroyImmediate(sprite.texture);
    }

    [Test]
    public void Prefab_BindsCurrentOrderBannerAndCommandIconSource()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null, PrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        try
        {
            MatchHudCurrentOrderBannerView bannerView = instance.GetComponentInChildren<MatchHudCurrentOrderBannerView>(true);
            Transform header = bannerView != null ? bannerView.transform : null;
            Assert.That(header, Is.Not.Null, "HeaderContent is required for CurrentOrderBanner.");

            Transform banner = header.Find("CurrentOrderBanner");
            Assert.That(banner, Is.Not.Null, "HeaderContent/CurrentOrderBanner is required.");
            Assert.That(banner.gameObject.activeSelf, Is.False, "CurrentOrderBanner must start hidden.");

            Assert.That(bannerView, Is.Not.Null, "HeaderContent must own the serialized banner view binder.");
            Assert.That(bannerView.BannerRoot, Is.SameAs(banner.gameObject));
            Assert.That(bannerView.Chevrons, Is.Not.Null);
            Assert.That(bannerView.Chevrons.name, Is.EqualTo("Chevrons"));
            Assert.That(bannerView.Chevrons.activeSelf, Is.False);
            Image chevrons = bannerView.Chevrons.GetComponent<Image>();
            Assert.That(chevrons, Is.Not.Null);
            Assert.That(chevrons.sprite, Is.Not.Null);
            Assert.That(chevrons.preserveAspect, Is.True);
            Assert.That(bannerView.Icon, Is.Not.Null);
            Assert.That(bannerView.OrderText, Is.Not.Null);
            Assert.That(bannerView.DescriptionText, Is.Not.Null);

            BattleHudRuntimeFeedbackView runtimeFeedback = instance.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
            Assert.That(runtimeFeedback, Is.Not.Null);
            Assert.That(runtimeFeedback.CurrentOrderBanner, Is.SameAs(bannerView));
            Assert.That(runtimeFeedback.CommandIconSource, Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void Prefab_V3AttackModeShowsOnlyAttackSelectionFrame()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null, PrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        try
        {
            BattleHudRuntimeFeedbackView feedback = instance.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
            MatchOverlayCommandControlsView controls = instance.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(controls, Is.Not.Null);

            feedback.ApplyCommandModeTabs(TacticalCommandMode.Attack);

            Assert.That(controls.AttackButton.transform.Find("V3SelectedState")?.gameObject.activeSelf, Is.True);
            Assert.That(controls.MoveButton.transform.Find("V3SelectedState")?.gameObject.activeSelf, Is.False);
            Assert.That(controls.SelectButton.transform.Find("V3SelectedState")?.gameObject.activeSelf, Is.False);

            feedback.ClearCommandModeTabs();
            Assert.That(controls.AttackButton.transform.Find("V3SelectedState")?.gameObject.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void Prefab_CommandIconSourceMatchesActualButtonIcons()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        try
        {
            MatchOverlayCommandControlsView controls = instance.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            Assert.That(controls, Is.Not.Null);
            AssertIconSource(controls, TacticalCommandMode.Select, controls.SelectIcon);
            AssertIconSource(controls, TacticalCommandMode.Move, controls.MoveIcon);
            AssertIconSource(controls, TacticalCommandMode.Attack, controls.AttackIcon);
            AssertIconSource(controls, TacticalCommandMode.Hold, controls.HoldIcon);
            AssertIconSource(controls, TacticalCommandMode.Stop, controls.StopIcon);
            AssertIconSource(controls, TacticalCommandMode.Scan, controls.ScanIcon);
            AssertIconSource(controls, TacticalCommandMode.Board, controls.BoardIcon);
            AssertIconSource(controls, TacticalCommandMode.Build, controls.BuildIcon);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void Prefab_TacticalGroundMarkersUsePerspectiveV3RingsWithoutPlaceholder()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        try
        {
            V3EllipseRingGraphic[] rings =
                instance.GetComponentsInChildren<V3EllipseRingGraphic>(true);
            Assert.That(rings, Has.Length.EqualTo(4));
            Assert.That(rings, Has.Some.Matches<V3EllipseRingGraphic>(ring =>
                ring.transform.name == "FriendlySourceRing"));
            Assert.That(rings, Has.Some.Matches<V3EllipseRingGraphic>(ring =>
                ring.transform.name == "HostileTargetRing"));
            Assert.That(
                instance.GetComponentsInChildren<Image>(true),
                Has.None.Matches<Image>(image => image.name == "FriendlySourceMarker"),
                "The old leaf-like friendly ground placeholder must not return.");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void RuntimeFeedback_AppliesAndClearsStickyCommandModeBanner()
    {
        Sprite sprite = CreateSprite();
        FakeRuntimeFeedbackView view = new(TacticalCommandMode.Move, sprite);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(view, TacticalCommandMode.Move);

        Assert.That(view.LastBanner.Visible, Is.True);
        Assert.That(view.LastBanner.CommandMode, Is.EqualTo(TacticalCommandMode.Move));
        Assert.That(view.LastBanner.OrderText, Is.EqualTo("MOVE ORDER"));
        Assert.That(view.LastBanner.DescriptionText, Is.EqualTo("Select a destination."));
        Assert.That(view.LastBanner.IconSprite, Is.SameAs(sprite));

        BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(view);

        Assert.That(view.LastBanner.Visible, Is.False);

        Object.DestroyImmediate(sprite.texture);
    }

    [Test]
    public void RuntimeFeedback_DoesNotShowBannerForSelectMode()
    {
        FakeRuntimeFeedbackView view = new(TacticalCommandMode.Select, null);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(view, TacticalCommandMode.Select);

        Assert.That(view.LastBanner.Visible, Is.False);
    }

    [Test]
    public void RuntimeFeedback_AppliesBoardDirectionBanner()
    {
        Sprite sprite = CreateSprite();
        FakeRuntimeFeedbackView view = new(TacticalCommandMode.Board, sprite);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyBoardCommandMode(
            view,
            UiBoardCommandModeDirection.TransportToPassenger,
            boardAllInteractable: false);

        Assert.That(view.LastBanner.Visible, Is.True);
        Assert.That(view.LastBanner.CommandMode, Is.EqualTo(TacticalCommandMode.Board));
        Assert.That(view.LastBanner.OrderText, Is.EqualTo("BOARD ORDER"));
        Assert.That(view.LastBanner.DescriptionText, Is.EqualTo("Select units to board."));
        Assert.That(view.LastBanner.IconSprite, Is.SameAs(sprite));

        Object.DestroyImmediate(sprite.texture);
    }

    [Test]
    public void RuntimeFeedback_AppliesAcceptedImmediateCommandTransientBanner()
    {
        Sprite sprite = CreateSprite();
        FakeRuntimeFeedbackView view = new(TacticalCommandMode.Hold, sprite);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
            view,
            TacticalCommandResult.Success("Holding current position."));

        Assert.That(view.LastTransientBanner.Visible, Is.True);
        Assert.That(view.LastTransientBanner.CommandMode, Is.EqualTo(TacticalCommandMode.Hold));
        Assert.That(view.LastTransientBanner.OrderText, Is.EqualTo("HOLD POSITION"));
        Assert.That(view.LastTransientBanner.DescriptionText, Is.EqualTo("Selected units holding ground."));
        Assert.That(view.LastTransientBanner.IconSprite, Is.SameAs(sprite));

        Object.DestroyImmediate(sprite.texture);
    }

    [Test]
    public void RuntimeFeedback_RejectedResultDoesNotShowBanner()
    {
        FakeRuntimeFeedbackView view = new(TacticalCommandMode.Move, null);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
            view,
            TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));

        Assert.That(view.LastBanner.Visible, Is.False);
        Assert.That(view.LastTransientBanner.Visible, Is.False);
    }

    [Test]
    public void RuntimeFeedback_AppliesManualCommandModeCoverage()
    {
        AssertCommandModeBanner(TacticalCommandMode.Move, "MOVE ORDER", "Select a destination.");
        AssertCommandModeBanner(TacticalCommandMode.Attack, "ATTACK ORDER", "Select an enemy target.");
        AssertCommandModeBanner(TacticalCommandMode.Scan, "SCAN ORDER", "Select an area to scan.");
        AssertCommandModeBanner(TacticalCommandMode.Board, "BOARD ORDER", "Select a transport.");
        AssertCommandModeBanner(TacticalCommandMode.Build, "BUILD ORDER", "Place structure on valid terrain.");
    }

    [Test]
    public void RuntimeFeedback_AppliesManualAcceptedResultCoverage()
    {
        AssertAcceptedResultBanner(TacticalCommandMode.Move, "Move order accepted.", "MOVE ORDER", "Units moving to target.");
        AssertAcceptedResultBanner(TacticalCommandMode.Attack, "Attack order accepted.", "ATTACK ORDER", "Engaging target.");
        AssertAcceptedResultBanner(TacticalCommandMode.Hold, "Holding current position.", "HOLD POSITION", "Selected units holding ground.");
        AssertAcceptedResultBanner(TacticalCommandMode.Stop, "Stopped selected units.", "STOP ORDER", "Selected units clearing orders.");
        AssertAcceptedResultBanner(TacticalCommandMode.Scan, "Scan order accepted.", "SCAN ORDER", "Recon sweep in progress.");
        AssertAcceptedResultBanner(TacticalCommandMode.Board, "Boarding transport.", "BOARD ORDER", "Boarding transport.");
        AssertAcceptedResultBanner(TacticalCommandMode.Build, "Building placement accepted.", "BUILD ORDER", "Building order accepted.");
    }

    [Test]
    public void SelectionBoundary_CommandModeFlowsThroughRuntimeFeedbackSink()
    {
        Sprite sprite = CreateSprite();
        World world = new("CurrentOrderBannerCommandModeTestWorld");
        try
        {
            FakeRuntimeFeedbackView view = new(TacticalCommandMode.Move, sprite);
            SelectionHudFeedbackUiSystemHelper boundary = new();
            boundary.BindBattleHudRuntimeFeedback(new BattleHudRuntimeFeedbackSink(view));

            boundary.ApplyCommandMode(world.EntityManager, TacticalCommandMode.Move);

            Assert.That(view.LastBanner.Visible, Is.True);
            Assert.That(view.LastBanner.CommandMode, Is.EqualTo(TacticalCommandMode.Move));
            Assert.That(view.LastBanner.IconSprite, Is.SameAs(sprite));
        }
        finally
        {
            world.Dispose();
            Object.DestroyImmediate(sprite.texture);
        }
    }

    [Test]
    public void SelectionBoundary_BoardCommandModeFlowsThroughRuntimeFeedbackSink()
    {
        Sprite sprite = CreateSprite();
        World world = new("CurrentOrderBannerBoardModeTestWorld");
        try
        {
            FakeRuntimeFeedbackView view = new(TacticalCommandMode.Board, sprite);
            SelectionHudFeedbackUiSystemHelper boundary = new();
            boundary.BindBattleHudRuntimeFeedback(new BattleHudRuntimeFeedbackSink(view));
            SelectionHudFeedbackUiSystemHelper.Context context = new(default, TryGetEntityManager);

            boundary.ApplyBoardCommandMode(
                context,
                BoardCommandModeDirection.TransportToPassenger,
                boardAllInteractable: false);

            Assert.That(view.LastBanner.Visible, Is.True);
            Assert.That(view.LastBanner.CommandMode, Is.EqualTo(TacticalCommandMode.Board));
            Assert.That(view.LastBanner.DescriptionText, Is.EqualTo("Select units to board."));
            Assert.That(view.LastBanner.IconSprite, Is.SameAs(sprite));
        }
        finally
        {
            world.Dispose();
            Object.DestroyImmediate(sprite.texture);
        }

        bool TryGetEntityManager(out EntityManager em)
        {
            em = world.EntityManager;
            return true;
        }
    }

    [Test]
    public void SelectionBoundary_ClearCommandModeHidesRuntimeFeedbackBanner()
    {
        Sprite sprite = CreateSprite();
        World world = new("CurrentOrderBannerClearModeTestWorld");
        try
        {
            FakeRuntimeFeedbackView view = new(TacticalCommandMode.Attack, sprite);
            SelectionHudFeedbackUiSystemHelper boundary = new();
            boundary.BindBattleHudRuntimeFeedback(new BattleHudRuntimeFeedbackSink(view));

            boundary.ApplyCommandMode(world.EntityManager, TacticalCommandMode.Attack);
            boundary.ClearCommandMode(world.EntityManager);

            Assert.That(view.LastBanner.Visible, Is.False);
        }
        finally
        {
            world.Dispose();
            Object.DestroyImmediate(sprite.texture);
        }
    }

    public static void RunFocusedValidation()
    {
        MatchHudCurrentOrderBannerTests tests = new();

        tests.Model_FromCommand_UsesSharedCommandText();
        tests.View_OnEnable_HidesBannerAndClearsStaleVisuals();
        tests.View_ApplyVisibleModel_ShowsCommandContent();
        tests.View_Hide_ClearsCommandContent();
        tests.Prefab_BindsCurrentOrderBannerAndCommandIconSource();
        tests.Prefab_V3AttackModeShowsOnlyAttackSelectionFrame();
        tests.Prefab_CommandIconSourceMatchesActualButtonIcons();
        tests.Prefab_TacticalGroundMarkersUsePerspectiveV3RingsWithoutPlaceholder();
        tests.RuntimeFeedback_AppliesAndClearsStickyCommandModeBanner();
        tests.RuntimeFeedback_DoesNotShowBannerForSelectMode();
        tests.RuntimeFeedback_AppliesBoardDirectionBanner();
        tests.RuntimeFeedback_AppliesAcceptedImmediateCommandTransientBanner();
        tests.RuntimeFeedback_RejectedResultDoesNotShowBanner();
        tests.RuntimeFeedback_AppliesManualCommandModeCoverage();
        tests.RuntimeFeedback_AppliesManualAcceptedResultCoverage();
        tests.SelectionBoundary_CommandModeFlowsThroughRuntimeFeedbackSink();
        tests.SelectionBoundary_BoardCommandModeFlowsThroughRuntimeFeedbackSink();
        tests.SelectionBoundary_ClearCommandModeHidesRuntimeFeedbackBanner();

        Debug.Log("[MatchHudCurrentOrderBannerValidation] result=Passed tests=18");
    }

    private static Sprite CreateSprite()
    {
        Texture2D texture = new(4, 4, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
    }

    private static void AssertIconSource(MatchOverlayCommandControlsView controls, TacticalCommandMode mode, Image image)
    {
        Assert.That(image, Is.Not.Null, $"{mode} icon reference must be assigned.");
        Assert.That(image.sprite, Is.Not.Null, $"{mode} icon reference must have a sprite.");
        Assert.That(controls.ResolveCommandIconSprite(mode), Is.SameAs(image.sprite));
    }

    private static void AssertCommandModeBanner(
        TacticalCommandMode mode,
        string expectedOrderText,
        string expectedDescriptionText)
    {
        Sprite sprite = CreateSprite();
        try
        {
            FakeRuntimeFeedbackView view = new(mode, sprite);

            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(view, mode);

            Assert.That(view.LastBanner.Visible, Is.True, $"{mode} must show a current-order banner.");
            Assert.That(view.LastBanner.CommandMode, Is.EqualTo(mode));
            Assert.That(view.LastBanner.OrderText, Is.EqualTo(expectedOrderText));
            Assert.That(view.LastBanner.DescriptionText, Is.EqualTo(expectedDescriptionText));
            Assert.That(view.LastBanner.IconSprite, Is.SameAs(sprite));
        }
        finally
        {
            Object.DestroyImmediate(sprite.texture);
        }
    }

    private static void AssertAcceptedResultBanner(
        TacticalCommandMode mode,
        string resultMessage,
        string expectedOrderText,
        string expectedDescriptionText)
    {
        Sprite sprite = CreateSprite();
        try
        {
            FakeRuntimeFeedbackView view = new(mode, sprite)
            {
                CurrentCommandMode = mode
            };

            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
                view,
                TacticalCommandResult.Success(resultMessage));

            Assert.That(view.LastTransientBanner.Visible, Is.True, $"{mode} accepted result must show a transient banner.");
            Assert.That(view.LastTransientBanner.CommandMode, Is.EqualTo(mode));
            Assert.That(view.LastTransientBanner.OrderText, Is.EqualTo(expectedOrderText));
            Assert.That(view.LastTransientBanner.DescriptionText, Is.EqualTo(expectedDescriptionText));
            Assert.That(view.LastTransientBanner.IconSprite, Is.SameAs(sprite));
        }
        finally
        {
            Object.DestroyImmediate(sprite.texture);
        }
    }

    private sealed class TestBannerRig : System.IDisposable
    {
        public TestBannerRig()
        {
            Owner = new GameObject("CurrentOrderBannerViewOwner");
            Root = new GameObject("CurrentOrderBanner");
            Root.transform.SetParent(Owner.transform, false);
            Chevrons = new GameObject("Chevrons");
            Chevrons.transform.SetParent(Root.transform, false);

            GameObject iconObject = new("Icon");
            iconObject.transform.SetParent(Root.transform, false);
            Icon = iconObject.AddComponent<Image>();

            GameObject orderObject = new("OrderText");
            orderObject.transform.SetParent(Root.transform, false);
            OrderText = orderObject.AddComponent<TextMeshProUGUI>();

            GameObject descriptionObject = new("DescriptionText");
            descriptionObject.transform.SetParent(Root.transform, false);
            DescriptionText = descriptionObject.AddComponent<TextMeshProUGUI>();

            View = Owner.AddComponent<MatchHudCurrentOrderBannerView>();
            SetField(View, "bannerRoot", Root);
            SetField(View, "chevrons", Chevrons);
            SetField(View, "icon", Icon);
            SetField(View, "orderText", OrderText);
            SetField(View, "descriptionText", DescriptionText);
        }

        public GameObject Owner { get; }
        public GameObject Root { get; }
        public GameObject Chevrons { get; }
        public Image Icon { get; }
        public TMP_Text OrderText { get; }
        public TMP_Text DescriptionText { get; }
        public MatchHudCurrentOrderBannerView View { get; }

        public void Dispose()
        {
            Object.DestroyImmediate(Owner);
        }

        private static void SetField(MatchHudCurrentOrderBannerView view, string fieldName, object value)
        {
            FieldInfo field = typeof(MatchHudCurrentOrderBannerView).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(view, value);
        }
    }

    private sealed class FakeRuntimeFeedbackView : IBattleHudRuntimeFeedbackView
    {
        private readonly TacticalCommandMode _iconMode;
        private readonly Sprite _iconSprite;

        public FakeRuntimeFeedbackView(TacticalCommandMode iconMode, Sprite iconSprite)
        {
            _iconMode = iconMode;
            _iconSprite = iconSprite;
        }

        public TacticalCommandMode CurrentCommandMode { get; set; }
        public TacticalCommandMode StickyCommandMode { get; set; }
        public TacticalCommandResult LastCommandResult { get; set; }
        public bool HasLastCommandResult { get; set; }
        public BattleHudRuntimeFeedbackState RuntimeFeedbackState =>
            new(CurrentCommandMode, StickyCommandMode, LastCommandResult, HasLastCommandResult);
        public MatchHudCurrentOrderBannerModel LastBanner { get; private set; } = MatchHudCurrentOrderBannerModel.Hidden;
        public MatchHudCurrentOrderBannerModel LastTransientBanner { get; private set; } = MatchHudCurrentOrderBannerModel.Hidden;

        public Sprite ResolveCommandIconSprite(TacticalCommandMode mode)
        {
            return mode == _iconMode ? _iconSprite : null;
        }

        public void ApplyCurrentOrderBanner(MatchHudCurrentOrderBannerModel model)
        {
            LastBanner = model;
        }

        public void ApplyTransientCurrentOrderBanner(MatchHudCurrentOrderBannerModel model, float now, float durationSeconds)
        {
            LastTransientBanner = model;
        }

        public void ApplyCommandFeedbackActions(MatchHudCommandFeedbackActionsModel model)
        {
        }

        public void ApplyCommandModeTabs(TacticalCommandMode mode)
        {
        }

        public void ApplyPersistentCommandFeedback(MatchHudCommandFeedbackModel model, MatchHudCommandFeedbackActionsModel actionsModel)
        {
        }

        public void ApplyTransientCommandFeedback(MatchHudCommandFeedbackModel model, float now)
        {
        }

        public void BindFeedbackActionCallbacks(System.Action boardAllRequested, System.Action cancelRequested)
        {
        }

        public void ClearCommandModeTabs()
        {
        }

        public void ClearFeedbackActionCallbacks()
        {
        }

        public void ClearPersistentCommandFeedback()
        {
        }

        public void HideCommandMode()
        {
        }

        public void HideCurrentOrderBanner()
        {
            LastBanner = MatchHudCurrentOrderBannerModel.Hidden;
        }

        public void HideFeedbackMessage()
        {
        }

        public void HideInvalidCommand()
        {
        }

        public void HideSelectedEntity()
        {
        }

        public void SetWorldMarkersVisible(bool visible)
        {
        }

        public void ShowCommandMode(string mode)
        {
        }

        public void ShowFeedbackMessage(string message)
        {
        }

        public void ShowFeedbackMessage(string message, CommandFeedbackSeverity severity)
        {
        }

        public void ShowInvalidCommand(string reason)
        {
        }

        public void ShowSelectedEntity(string displayName, string status)
        {
        }

        public void TickFeedbackLifetime(float now)
        {
        }
    }
}
