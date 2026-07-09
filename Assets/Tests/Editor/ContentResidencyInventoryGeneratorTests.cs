using System;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class ContentResidencyInventoryGeneratorTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            ContentResidencyInventoryGeneratorTests tests = new();
            tests.UnavailableImportedSizesSerializeAsNull();
            tests.GeneratedAnimationTextureConventionIsNarrow();
            tests.PlayerContentFilterExcludesEditorAndScriptSources();
            tests.PendingMarkdownDoesNotClaimMeasurements();
            Debug.Log("[ContentResidencyInventoryValidation] result=Passed tests=4");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[ContentResidencyInventoryValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void UnavailableImportedSizesSerializeAsNull()
    {
        ContentResidencyReport report = CreatePendingReport();
        report.Assets.Add(new ContentResidencyAssetRecord
        {
            AssetPath = "Assets/Game/Textures/Example.png",
            AssetType = "Texture2D",
            SourceSizeBytes = 1024,
            ImportedSizeBytes = null,
            AnimationTexturePayloadBytes = null
        });

        string json = ContentResidencyInventoryGenerator.SerializeReport(report);

        StringAssert.Contains("\"importedSizeBytes\": null", json);
        StringAssert.Contains("\"animationTexturePayloadBytes\": null", json);
        StringAssert.DoesNotContain("\"importedSizeBytes\": -1", json);
    }

    [Test]
    public void GeneratedAnimationTextureConventionIsNarrow()
    {
        Assert.IsTrue(ContentResidencyInventoryGenerator.IsGeneratedAnimationTexturePath(
            "Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture0.asset"));
        Assert.IsFalse(ContentResidencyInventoryGenerator.IsGeneratedAnimationTexturePath(
            "Assets/Game/Textures/Generated/AnimationTexture0.png"));
        Assert.IsFalse(ContentResidencyInventoryGenerator.IsGeneratedAnimationTexturePath(
            "Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/BaseTexture.asset"));
    }

    [Test]
    public void PlayerContentFilterExcludesEditorAndScriptSources()
    {
        Assert.IsTrue(ContentResidencyInventoryGenerator.IsPotentialPlayerContentPath(
            "Assets/Game/Textures/World.png"));
        Assert.IsTrue(ContentResidencyInventoryGenerator.IsPotentialPlayerContentPath(
            "Packages/com.example.runtime/Runtime/Shaders/World.shader"));
        Assert.IsFalse(ContentResidencyInventoryGenerator.IsPotentialPlayerContentPath(
            "Assets/Game/Scripts/Editor/Generator.cs"));
        Assert.IsFalse(ContentResidencyInventoryGenerator.IsPotentialPlayerContentPath(
            "Assets/Game/Scripts/RuntimeSystem.cs"));
        Assert.IsFalse(ContentResidencyInventoryGenerator.IsPotentialPlayerContentPath(
            "ProjectSettings/GraphicsSettings.asset"));
    }

    [Test]
    public void PendingMarkdownDoesNotClaimMeasurements()
    {
        ContentResidencyReport report = CreatePendingReport();

        string markdown = ContentResidencyInventoryGenerator.BuildMarkdown(report);

        StringAssert.Contains("Status: `pending-unity-generation`", markdown);
        StringAssert.Contains("No asset row or imported-size measurement", markdown);
        StringAssert.Contains("Unavailable until Unity generation", markdown);
    }

    private static ContentResidencyReport CreatePendingReport()
    {
        ContentResidencyReport report = new()
        {
            Status = "pending-unity-generation",
            Scope = "Focused test scope"
        };
        report.Limitations.Add("Unity generation is required.");
        return report;
    }
}
