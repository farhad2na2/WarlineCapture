using System;
using System.Collections.Generic;
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
            tests.AudioCategoryUsesImportProfileFolder();
            tests.DecodedAudioSizeUsesFloatPcmSamples();
            tests.CatalogAudioFieldsSerializeWithMeasurements();
            tests.CatalogAudioMarkdownExcludesUnreferencedAudioAssets();
            tests.PendingMarkdownDoesNotClaimMeasurements();
            tests.TextureSummaryMatchesUniqueTexture2DRowsWithoutDiscardingCubemaps();
            Debug.Log("[ContentResidencyInventoryValidation] result=Passed tests=9");
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
    public void AudioCategoryUsesImportProfileFolder()
    {
        Assert.AreEqual(
            "Voice",
            ContentResidencyInventoryGenerator.GetAudioCategory(
                "Assets/Game/Audio/Voice/ARIA/aria_message.wav"));
        Assert.AreEqual(
            "Music",
            ContentResidencyInventoryGenerator.GetAudioCategory(
                "Assets\\Game\\Audio\\Music\\music_menu_loop.wav"));
        Assert.IsNull(ContentResidencyInventoryGenerator.GetAudioCategory(
            "Assets/LegacyAudio/unused.wav"));
        Assert.IsNull(ContentResidencyInventoryGenerator.GetAudioCategory(
            "Assets/Game/Audio/orphan.wav"));
    }

    [Test]
    public void DecodedAudioSizeUsesFloatPcmSamples()
    {
        Assert.AreEqual(
            352800L,
            ContentResidencyInventoryGenerator.EstimateDecodedAudioSizeBytes(44100, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ContentResidencyInventoryGenerator.EstimateDecodedAudioSizeBytes(44100, 0));
    }

    [Test]
    public void CatalogAudioFieldsSerializeWithMeasurements()
    {
        ContentResidencyReport report = CreatePendingReport();
        CatalogAudioResidencyRecord clip = CreateCatalogClip();
        report.AudioCatalogAssetPaths.Add("Assets/Game/Audio/Events/AudioEventCatalogConfig.asset");
        report.CatalogAudioClips.Add(clip);

        string json = ContentResidencyInventoryGenerator.SerializeReport(report);

        StringAssert.Contains("\"audioResidencyTaskId\": \"APH-400\"", json);
        StringAssert.Contains("\"audioCatalogAssetPaths\"", json);
        StringAssert.Contains("\"catalogAudioClips\"", json);
        StringAssert.Contains("\"eventIds\"", json);
        StringAssert.Contains("\"busIds\"", json);
        StringAssert.Contains("\"category\": \"UI\"", json);
        StringAssert.Contains("\"durationSeconds\": 0.5", json);
        StringAssert.Contains("\"channels\": 2", json);
        StringAssert.Contains("\"frequencyHz\": 44100", json);
        StringAssert.Contains("\"importLoadType\": \"DecompressOnLoad\"", json);
        StringAssert.Contains("\"compressedSizeBytes\": 12000", json);
        StringAssert.Contains("\"estimatedDecodedSizeBytes\": 176400", json);
    }

    [Test]
    public void CatalogAudioMarkdownExcludesUnreferencedAudioAssets()
    {
        ContentResidencyReport report = CreatePendingReport();
        report.Assets.Add(new ContentResidencyAssetRecord
        {
            AssetPath = "Assets/Game/Audio/Legacy/unused.wav",
            AssetType = "AudioClip",
            AudioLoadType = "DecompressOnLoad"
        });
        report.AudioCatalogAssetPaths.Add("Assets/Game/Audio/Events/AudioEventCatalogConfig.asset");
        report.CatalogAudioClips.Add(CreateCatalogClip());

        string markdown = ContentResidencyInventoryGenerator.BuildMarkdown(report);

        StringAssert.Contains("## Catalog-Referenced Audio Residency", markdown);
        StringAssert.Contains("### Bus and Category Totals", markdown);
        StringAssert.Contains("### Catalog Clip Detail", markdown);
        StringAssert.Contains("Assets/Game/Audio/UI/ui_button.wav", markdown);
        StringAssert.Contains("UI.Button.Click", markdown);
        StringAssert.Contains("0.500 s", markdown);
        StringAssert.Contains("44,100 Hz", markdown);
        StringAssert.Contains("DecompressOnLoad", markdown);
        StringAssert.DoesNotContain("Assets/Game/Audio/Legacy/unused.wav", markdown);
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

    [Test]
    public void TextureSummaryMatchesUniqueTexture2DRowsWithoutDiscardingCubemaps()
    {
        List<ContentResidencyAssetRecord> assets = new()
        {
            new ContentResidencyAssetRecord
            {
                AssetPath = "Assets/Game/Textures/World.png",
                AssetType = nameof(Texture2D),
                TextureWidth = 1024,
                TextureHeight = 1024,
                TextureStreamingEnabled = true
            },
            new ContentResidencyAssetRecord
            {
                AssetPath = "Assets/Game/Textures/World.png",
                AssetType = nameof(Texture2D),
                TextureWidth = 1024,
                TextureHeight = 1024,
                TextureStreamingEnabled = true
            },
            new ContentResidencyAssetRecord
            {
                AssetPath = "Assets/Game/Scenes/Match/ReflectionProbe-0.exr",
                AssetType = nameof(Cubemap),
                TextureWidth = 512,
                TextureHeight = 512,
                TextureStreamingEnabled = false,
                ImportedSizeBytes = 1048576
            }
        };

        IReadOnlyList<ContentResidencyAssetRecord> textureRows =
            ContentResidencyInventoryGenerator.BuildDeterministicTexture2DRows(assets);
        ContentResidencySummary summary = ContentResidencyInventoryGenerator.BuildSummary(
            Array.Empty<DependencyRootRecord>(),
            assets,
            Array.Empty<CatalogAudioResidencyRecord>());
        ContentResidencyReport report = CreatePendingReport();
        report.Assets.AddRange(assets);
        report.Summary = summary;

        Assert.AreEqual(1, textureRows.Count);
        Assert.AreEqual("Assets/Game/Textures/World.png", textureRows[0].AssetPath);
        Assert.AreEqual(1, summary.TextureAssetCount);
        Assert.AreEqual(1, summary.TextureStreamingEnabledCount);
        Assert.AreEqual(1048576, summary.ImportedSizeBytes);

        string json = ContentResidencyInventoryGenerator.SerializeReport(report);
        StringAssert.Contains("\"textureAssetCount\": 1", json);
        StringAssert.Contains("Assets/Game/Scenes/Match/ReflectionProbe-0.exr", json);
        StringAssert.Contains("\"assetType\": \"Cubemap\"", json);
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

    private static CatalogAudioResidencyRecord CreateCatalogClip()
    {
        CatalogAudioResidencyRecord clip = new()
        {
            AssetPath = "Assets/Game/Audio/UI/ui_button.wav",
            Category = "UI",
            DurationSeconds = 0.5,
            SampleFrames = 22050,
            Channels = 2,
            FrequencyHz = 44100,
            ImportLoadType = "DecompressOnLoad",
            ImportLoadTypeSource = "default importer settings",
            CompressedSizeBytes = 12000,
            CompressedSizeMeasurement = "UnityEditor.AudioUtil.GetSoundSize",
            EstimatedDecodedSizeBytes = 176400
        };
        clip.EventIds.Add("UI.Button.Click");
        clip.BusIds.Add("UI");
        return clip;
    }
}
