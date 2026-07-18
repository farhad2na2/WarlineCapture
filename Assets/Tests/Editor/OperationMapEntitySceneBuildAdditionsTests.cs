using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public sealed class OperationMapEntitySceneBuildAdditionsTests
{
    [Test]
    public void RegisteredEntitySceneMatchesOperationMapSourceSubScene()
    {
        string expectedGuid = AssetDatabase.AssetPathToGUID(
            OperationMapAddressablesLayoutBuilder.SourceSubScenePath);
        var additions = new OperationMapEntitySceneBuildAdditions();

        var registered = additions.RegisterAdditionalEntityScenesToBuild();

        Assert.That(expectedGuid, Is.Not.Empty);
        Assert.That(registered, Has.Count.EqualTo(1));
        Assert.That(registered, Does.Contain(new Unity.Entities.Hash128(expectedGuid)));
    }

    [Test]
    public void SourceSubSceneIsNotManuallyAddressable()
    {
        AddressableAssetSettings settings =
            AddressableAssetSettingsDefaultObject.GetSettings(false);
        Assert.That(settings, Is.Not.Null);

        string guid = AssetDatabase.AssetPathToGUID(
            OperationMapAddressablesLayoutBuilder.SourceSubScenePath);

        Assert.That(settings.FindAssetEntry(guid), Is.Null);
    }
}
