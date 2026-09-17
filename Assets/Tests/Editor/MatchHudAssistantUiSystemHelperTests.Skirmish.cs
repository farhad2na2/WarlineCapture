using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed partial class MatchHudAssistantUiSystemHelperTests
{
    [TestCase("en")]
    [TestCase("fa-IR")]
    public void SkirmishBriefingKeepsItsLayoutAcrossAllHudUpdates(string locale)
    {
        GameLocalization.SetLocale(locale, false);
        Assert.AreEqual(locale, GameLocalization.CurrentLocaleCode);
        CreateHudHarness(true, out var overlay, out var header, out _);
        var model = new UiAssistantPanelModel(1, "", "", "", false, false, true,
            GameText.Get("ui.skirmish.base_assault", "BASE ASSAULT"),
            GameText.Get("ui.skirmish.match_help", "Destroy the enemy Main Base."),
            "", "", false, false, false, false, "", "");
        var gateway = new FakeAssistantPanelGateway(model, UiAssistantHighlightModel.Empty) { Skirmish = true };
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        try
        {
            ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());
            var helper = GetPrivateField<MatchHudAssistantUiSystemHelper>(ui, "_matchHudAssistantUiSystem");
            var view = header.Find("AriaAssistantButton").GetComponent<AriaTutorialBriefingView>();
            helper.ApplyReadModel(model);
            view.ApplyAccessibility(false, false);
            view.RefreshContentLayout();
            var expected = ((RectTransform)view.transform).rect;
            for (int frame = 0; frame < 20; frame++)
            {
                helper.ApplyReadModel(model);
                Check();
                helper.TickHighlight(frame);
                Check();
                view.ApplyAccessibility(false, false);
                view.RefreshContentLayout();
                Check();
            }
            void Check()
            {
                Assert.IsTrue(view.IsPresentationVisible);
                Assert.IsFalse(view.ShowMeButton.gameObject.activeSelf, "No campaign action may reappear between updates.");
                Assert.IsFalse(view.ContinueButton.gameObject.activeSelf);
                Assert.AreEqual(expected, ((RectTransform)view.transform).rect, "The briefing must not alternate height within a frame.");
                Assert.AreEqual(model.RecommendationBody, view.CurrentInstructionBody);
            }
        }
        finally { ui.Dispose(); }
    }

    private sealed partial class FakeAssistantPanelGateway : IUiSkirmishGateway
    {
        public bool Skirmish;
        public bool TryReadSkirmish(out UiSkirmishModel model) { model = default; return Skirmish; }
        public bool TryRequestSkirmish(UiSkirmishAction action) => false;
    }
}
