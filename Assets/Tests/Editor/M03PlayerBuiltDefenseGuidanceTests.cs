using Game.Components;
using Game.Runtime;
using NUnit.Framework;
public sealed class M03PlayerBuiltDefenseGuidanceTests
{
    [Test]
    public void ExistingMapDefensesDoNotCompleteLessonButPlayerPlacementDoes()
    {
        var state=new CampaignMissionDefenseStateComponent();
        Assert.That(CampaignMissionGuidanceProjectionSystem.HasNewPlayerDefense(ref state,2),Is.False);
        Assert.That(CampaignMissionGuidanceProjectionSystem.HasNewPlayerDefense(ref state,2),Is.False);
        Assert.That(CampaignMissionGuidanceProjectionSystem.HasNewPlayerDefense(ref state,3),Is.True,
            "A manual placement has an authoritative ownership receipt without a scripted spawn request.");
        var retry=new CampaignMissionDefenseStateComponent();
        Assert.That(CampaignMissionGuidanceProjectionSystem.HasNewPlayerDefense(ref retry,3),Is.False,
            "Retry must establish its own baseline; prior buildings cannot finish the lesson.");
    }
}
