using Game.Editor;
using NUnit.Framework;

public sealed class OperationMapEntityScenePackageGateTests
{
    private const string Guid = "d50925a18e9164ce782536576cb833d8";

    [Test]
    public void AcceptsExpectedEntityScenePayload()
    {
        string[] entries =
        {
            $"assets/EntityScenes/{Guid}.entityheader",
            $"assets/EntityScenes/{Guid}.0.entities",
            "assets/EntityScenes/scene_info.bin"
        };

        Assert.That(OperationMapEntityScenePackageGate.GetValidationError(entries, Guid), Is.Null);
    }

    [TestCase("header")]
    [TestCase("section")]
    [TestCase("scene-info")]
    public void RejectsMissingRequiredPayload(string missing)
    {
        string[] entries =
        {
            missing == "header" ? "unused" : $"base/assets/EntityScenes/{Guid}.entityheader",
            missing == "section" ? "unused" : $"base/assets/EntityScenes/{Guid}.0.entities",
            missing == "scene-info" ? "unused" : "base/assets/EntityScenes/scene_info.bin"
        };

        Assert.That(
            OperationMapEntityScenePackageGate.GetValidationError(entries, Guid),
            Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void RejectsDifferentEntitySceneGuid()
    {
        const string otherGuid = "b57de3fe43d8a4dcb9eefc6ef149ee66";
        string[] entries =
        {
            $"assets/EntityScenes/{otherGuid}.entityheader",
            $"assets/EntityScenes/{otherGuid}.0.entities",
            "assets/EntityScenes/scene_info.bin"
        };

        Assert.That(
            OperationMapEntityScenePackageGate.GetValidationError(entries, Guid),
            Does.Contain(Guid));
    }
}
