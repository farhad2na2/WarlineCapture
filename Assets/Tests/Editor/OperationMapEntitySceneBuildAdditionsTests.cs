using System;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public sealed class OperationMapEntitySceneBuildAdditionsTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new OperationMapEntitySceneBuildAdditionsTests();
            tests.RegisteredEntitySceneMatchesProductionDefinition();
            tests.SourceSubSceneIsNotManuallyAddressable();
            Debug.Log("[OperationMapEntitySceneBuildAdditionsValidation] result=Passed tests=2");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[OperationMapEntitySceneBuildAdditionsValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void RegisteredEntitySceneMatchesProductionDefinition()
    {
        OperationMapDefinition definition =
            AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                OperationMapAddressablesLayoutBuilder.DefinitionPath);
        Assert.That(definition, Is.Not.Null);
        Assert.That(definition.PresentationKind, Is.EqualTo(OperationMapPresentationKind.EntityScene));
        string expectedGuid = definition.NavigationMetadata.AuthoredSubSceneGuid;
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
