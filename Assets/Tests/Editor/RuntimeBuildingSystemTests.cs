#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;

public sealed class RuntimeBuildingSystemTests
{
    [Test]
    public void AllocateId_IsSequentialAndResetsOnClear()
    {
        var system = new RuntimeBuildingSystem<string>();

        Assert.AreEqual(1, system.AllocateId());
        Assert.AreEqual(2, system.AllocateId());

        system.AddBuilding(1, "Barracks");
        system.Clear();

        Assert.AreEqual(0, system.Count);
        Assert.AreEqual(1, system.AllocateId());
    }

    [Test]
    public void SelectionTracksOnlyExistingBuildings()
    {
        var system = new RuntimeBuildingSystem<string>();
        system.AddBuilding(10, "HQ");
        system.SelectBuilding(10);

        Assert.IsTrue(system.HasSelectedBuilding());
        Assert.AreEqual(10, system.CurrentActiveBuildingId);

        system.RemoveBuilding(10);

        Assert.IsFalse(system.HasSelectedBuilding());
        Assert.IsFalse(system.CurrentActiveBuildingId.HasValue);
    }

    [Test]
    public void RemovingOtherBuilding_PreservesCurrentSelection()
    {
        var system = new RuntimeBuildingSystem<string>();
        system.AddBuilding(1, "Barracks");
        system.AddBuilding(2, "HQ");
        system.SelectBuilding(2);

        system.RemoveBuilding(1);

        Assert.IsTrue(system.HasSelectedBuilding());
        Assert.AreEqual(2, system.CurrentActiveBuildingId);
    }
}
#endif
