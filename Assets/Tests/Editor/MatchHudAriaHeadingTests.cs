using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;

// W42: the ARIA panel heading in a Skirmish match names the mission's own objective family.
// Drives the shipped read model (UiShellEcsGateway.TryReadMatchHudAssistantPanel) with an isolated
// session world; presentation only, no ARIA decision, objective or gameplay state is touched.
public sealed class MatchHudAriaHeadingTests
{
    private World previousWorld;
    private string previousLocale;

    [SetUp]
    public void SetUp()
    {
        previousWorld = World.DefaultGameObjectInjectionWorld;
        previousLocale = GameLocalization.CurrentLocaleCode;
    }

    [TearDown]
    public void TearDown()
    {
        World.DefaultGameObjectInjectionWorld = previousWorld;
        GameLocalization.SetLocale(previousLocale, false);
    }

    // Expected copy: Base Assault is the localized catalog title (ui.skirmish.base_assault); the other
    // families reuse the Battle Library family names, as the W40 objective card does.
    [TestCase(SkirmishObjectiveKind.BaseAssault, "en", "BASE ASSAULT")]
    [TestCase(SkirmishObjectiveKind.FrontlineControl, "en", "FRONTLINE CONTROL")]
    [TestCase(SkirmishObjectiveKind.Breakthrough, "en", "BREAKTHROUGH")]
    [TestCase(SkirmishObjectiveKind.ConvoyEscort, "en", "CONVOY ESCORT")]
    [TestCase(SkirmishObjectiveKind.BaseAssault, "fa-IR", "حمله به پایگاه")]
    [TestCase(SkirmishObjectiveKind.FrontlineControl, "fa-IR", "کنترل خط")]
    [TestCase(SkirmishObjectiveKind.Breakthrough, "fa-IR", "شکستن خط")]
    [TestCase(SkirmishObjectiveKind.ConvoyEscort, "fa-IR", "اسکورت کاروان")]
    public void AriaHeadingNamesTheMissionObjectiveFamily(SkirmishObjectiveKind kind, string locale, string expected)
    {
        GameLocalization.SetLocale(locale, false);
        Assert.AreEqual(locale, GameLocalization.CurrentLocaleCode);
        var panel = Read(kind, resolvedSetup: true, expandedSession: true);
        Assert.AreEqual(expected, panel.RecommendationTitle, $"ARIA heading for {kind} in {locale}");
        Assert.IsTrue(panel.HasRecommendation);
        if (kind == SkirmishObjectiveKind.BaseAssault)
            Assert.AreEqual(GameText.Get("ui.skirmish.base_assault", "BASE ASSAULT"), panel.RecommendationTitle,
                "Base Assault keeps the catalog title it showed before W42.");
    }

    [TestCase("en")]
    [TestCase("fa-IR")]
    public void AriaHeadingFollowsTheSessionWhenNoResolvedSetupIsRecorded(string locale)
    {
        GameLocalization.SetLocale(locale, false);
        bool fa = locale == "fa-IR";
        Assert.AreEqual(fa ? "اسکورت کاروان" : "CONVOY ESCORT",
            Read(SkirmishObjectiveKind.ConvoyEscort, resolvedSetup: false, expandedSession: true).RecommendationTitle);
        Assert.AreEqual(GameText.Get("ui.skirmish.base_assault", "BASE ASSAULT"),
            Read(SkirmishObjectiveKind.None, resolvedSetup: false, expandedSession: false).RecommendationTitle,
            "A legacy session without a family keeps the Base Assault title.");
    }

    [Test]
    public void AriaHeadingDoesNotChangeTheBriefingBodyOrActions()
    {
        GameLocalization.SetLocale("en", false);
        var baseAssault = Read(SkirmishObjectiveKind.BaseAssault, true, true);
        foreach (var kind in new[] { SkirmishObjectiveKind.FrontlineControl, SkirmishObjectiveKind.Breakthrough, SkirmishObjectiveKind.ConvoyEscort })
        {
            var panel = Read(kind, true, true);
            Assert.AreEqual(baseAssault.RecommendationBody, panel.RecommendationBody, "Heading only: the body is unchanged.");
            Assert.AreEqual(baseAssault.CanShow, panel.CanShow);
            Assert.AreEqual(baseAssault.CanExecute, panel.CanExecute);
            Assert.AreEqual(baseAssault.CanStop, panel.CanStop);
            Assert.AreEqual(baseAssault.CanTakeControl, panel.CanTakeControl);
            Assert.AreEqual(baseAssault.RecommendationActionLabel, panel.RecommendationActionLabel);
        }
    }

    private static UiAssistantPanelModel Read(SkirmishObjectiveKind kind, bool resolvedSetup, bool expandedSession)
    {
        using var world = new World("W42AriaHeading");
        var em = world.EntityManager;
        var session = em.CreateEntity(typeof(SkirmishMatchState));
        em.SetComponentData(session, new SkirmishMatchState { Phase = SkirmishPhase.Playing });
        if (expandedSession)
            em.AddComponentData(session, new SkirmishExpandedSessionComponent { ObjectiveKind = kind });
        if (resolvedSetup)
            em.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = new SkirmishResolvedSetup { ObjectiveKind = kind } });
        var previous = World.DefaultGameObjectInjectionWorld;
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudAssistantPanel(out var panel), "A playing Skirmish session publishes the ARIA panel.");
            return panel;
        }
        finally { World.DefaultGameObjectInjectionWorld = previous; }
    }
}
