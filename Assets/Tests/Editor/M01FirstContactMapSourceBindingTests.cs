using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Game.Components;
using Game.Composition;
using Game.Configs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class M01FirstContactMapSourceBindingTests
{
    private const string PassMarker = "[M01FirstContactMapSourceBindingValidation] result=Passed tests=13";
    private const string SourceDefinitionPath =
        "Assets/Game/Configs/OperationMaps/Candidates/" +
        "OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
    private const string SourceMapId = "opmap.skirmish.desert_base_01";
    private const string LogicalMapId = "opmap.ch01.district_edge_01";
    private const string SourceIdentityHash =
        "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900";
    private const string SourceContentHash =
        "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c";

    public static void RunFocusedValidation()
    {
        try
        {
            M01FirstContactMapSourceBindingTests tests = new();
            tests.AcceptedPhysicalSourceAssetsRetainFrozenGuidsAndHashes();
            tests.DenseCityPhysicalScenesHaveNoCampaignClone();
            tests.BlankBindingPreservesExistingSelfOwnedMap();
            tests.LogicalMapCanBindToExactAcceptedPhysicalSource();
            tests.LogicalMetadataCarriesExactPhysicalRenderBinding();
            tests.RuntimeReuseValidatesExactPhysicalSource();
            tests.RuntimeReuseRejectsStalePhysicalContent();
            tests.UnresolvedPhysicalSourceFailsClosed();
            tests.StaleSourceIdentityHashFailsClosed();
            tests.StaleSourceContentHashFailsClosed();
            tests.MismatchedSourceSceneReferenceFailsClosed();
            tests.SelfReferenceFailsClosed();
            tests.ChainedPhysicalSourceBindingFailsClosed();
            Debug.Log(PassMarker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M01FirstContactMapSourceBindingValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void AcceptedPhysicalSourceAssetsRetainFrozenGuidsAndHashes()
    {
        AssertAsset(SourceDefinitionPath, "cbfc2f62e3142413485019bd83197539",
            "f91b737280d8950d97264b54589b963f605a8d8911a0f4e17397bef667e4eba6");
        AssertAsset(
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/" +
            "opmap_skirmish_desert_base_01_dense_city_authoring_candidate.unity",
            "a0c9dc175951148c48a94a26bc036bf5",
            "5a15843d63868d639b88d2084ea41184af0cf5d6050b22b563a516ef13752b9c");
        AssertAsset(
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/" +
            "opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity",
            "c00140f2e94a04c3084c8dcb0c18cbd0",
            "c1bc203591b3f32ae3d8410eaa0988e694b1d9d449ba1e938d9f38058698b598");
        AssertAsset(
            "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/" +
            "opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity",
            "dad0bd13fb20943dfb2f881cbe225f05",
            "f58a73d0a8f3627d7ddf42f72b42a9db495d5139ba5da6768492ac36e671ade9");
        AssertAsset("Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset",
            "12f517deb32ab49698acbfdaf7c3eac7",
            "1402d769704008e254563ff7ecda835294db83afc2cee6d5bb456987f0392b4d");
        AssertAsset(
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/" +
            "MinimapRaster.png",
            "3b1a8dbd670bf4e94863c00acbc3a3a7",
            "420d9f07fec2418fad279d5d104a2c3caa248ee5481c3658fb9b2b65b9afcc3f");
    }

    [Test]
    public void DenseCityPhysicalScenesHaveNoCampaignClone()
    {
        string root = "Assets/Game/Scenes/OperationMaps";
        foreach (string path in Directory.GetFiles(root, "*dense_city*.unity", SearchOption.AllDirectories))
            StringAssert.Contains("/Skirmish/Candidates/", path.Replace('\\', '/'));
        Assert.AreEqual(2, Directory.GetFiles(root, "*dense_city*.unity", SearchOption.AllDirectories).Length);
    }

    [Test]
    public void BlankBindingPreservesExistingSelfOwnedMap()
    {
        OperationMapDefinition source = Source();
        Assert.IsFalse(source.SourceBinding.IsConfigured);
        Assert.IsTrue(source.TryValidateMetadata(out string metadataError), metadataError);
        Assert.IsTrue(OperationMapContractValidation.TryValidate(
            new[] { source }, Array.Empty<ScenarioSetupConfig>(), new[] { Evidence(source) }, out string error), error);
    }

    [Test]
    public void LogicalMapCanBindToExactAcceptedPhysicalSource()
    {
        OperationMapDefinition source = Source();
        OperationMapDefinition logical = Logical(source, SourceIdentityHash, SourceContentHash);
        try
        {
            Assert.IsTrue(OperationMapContractValidation.TryValidate(
                new[] { source, logical }, Array.Empty<ScenarioSetupConfig>(),
                new[] { Evidence(source), Evidence(logical) }, out string error), error);
            Assert.AreEqual(source.SourceSceneReference.AssetGUID, logical.SourceSceneReference.AssetGUID);
        }
        finally { UnityEngine.Object.DestroyImmediate(logical); }
    }

    [Test]
    public void LogicalMetadataCarriesExactPhysicalRenderBinding()
    {
        OperationMapDefinition logical = Logical(
            Source(), SourceIdentityHash, SourceContentHash);
        BlobAssetReference<OperationMapBlob> blob = default;
        try
        {
            Assert.IsTrue(logical.TryCreatePersistentMetadataBlob(
                out blob, out string error), error);
            Assert.AreEqual(LogicalMapId, blob.Value.OperationMapId.ToString());
            Assert.AreEqual(
                SourceMapId, blob.Value.SourceOperationMapId.ToString());
            Assert.AreEqual(
                SourceIdentityHash, blob.Value.SourceIdentityHash.ToString());
            Assert.AreEqual(
                SourceContentHash, blob.Value.SourceContentHash.ToString());
        }
        finally
        {
            if (blob.IsCreated)
                blob.Dispose();
            UnityEngine.Object.DestroyImmediate(logical);
        }
    }

    [Test]
    public void RuntimeReuseValidatesExactPhysicalSource()
    {
        AssertRuntimeReuse(SourceContentHash, true);
    }

    [Test]
    public void RuntimeReuseRejectsStalePhysicalContent()
    {
        AssertRuntimeReuse(new string('b', 64), false);
    }

    [Test]
    public void UnresolvedPhysicalSourceFailsClosed()
    {
        OperationMapDefinition logical = Logical(Source(), SourceIdentityHash, SourceContentHash);
        try
        {
            Assert.IsFalse(OperationMapContractValidation.TryValidate(
                new[] { logical }, Array.Empty<ScenarioSetupConfig>(),
                new[] { Evidence(logical) }, out string error));
            StringAssert.Contains("unresolved physical source", error);
        }
        finally { UnityEngine.Object.DestroyImmediate(logical); }
    }

    [Test]
    public void StaleSourceIdentityHashFailsClosed() => AssertBindingMismatch(
        new string('a', 64), SourceContentHash);

    [Test]
    public void StaleSourceContentHashFailsClosed() => AssertBindingMismatch(
        SourceIdentityHash, new string('b', 64));

    [Test]
    public void MismatchedSourceSceneReferenceFailsClosed()
    {
        OperationMapDefinition source = Source();
        OperationMapDefinition logical = Logical(source, SourceIdentityHash, SourceContentHash);
        try
        {
            SetField(logical, "sourceSceneReference",
                new AssetReference("12f517deb32ab49698acbfdaf7c3eac7"));
            Assert.IsFalse(Validate(source, logical, out string error));
            StringAssert.Contains("scene reference", error);
        }
        finally { UnityEngine.Object.DestroyImmediate(logical); }
    }

    [Test]
    public void SelfReferenceFailsClosed()
    {
        OperationMapDefinition logical = Logical(Source(), SourceIdentityHash, SourceContentHash);
        try
        {
            SetField(logical, "sourceBinding", new OperationMapSourceBindingConfig(
                LogicalMapId, SourceIdentityHash, SourceContentHash));
            Assert.IsFalse(logical.TryValidateMetadata(out string error));
            StringAssert.Contains("self-referential", error);
        }
        finally { UnityEngine.Object.DestroyImmediate(logical); }
    }

    [Test]
    public void ChainedPhysicalSourceBindingFailsClosed()
    {
        OperationMapDefinition physical = Source();
        OperationMapDefinition alias = Logical(physical, SourceIdentityHash, SourceContentHash);
        OperationMapDefinition logical = UnityEngine.Object.Instantiate(alias);
        try
        {
            SetField(alias, "operationMapId", "opmap.ch01.source_alias");
            SetField(logical, "operationMapId", LogicalMapId);
            SetField(logical, "sourceBinding", new OperationMapSourceBindingConfig(
                "opmap.ch01.source_alias", alias.SourceIdentityHash, alias.ContentHash));
            Assert.IsFalse(OperationMapContractValidation.TryValidate(
                new[] { physical, alias, logical }, Array.Empty<ScenarioSetupConfig>(),
                new[] { Evidence(physical), Evidence(alias), Evidence(logical) }, out string error));
            StringAssert.Contains("stale or mismatched", error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(alias);
            UnityEngine.Object.DestroyImmediate(logical);
        }
    }

    private static void AssertBindingMismatch(string identityHash, string contentHash)
    {
        OperationMapDefinition source = Source();
        OperationMapDefinition logical = Logical(source, identityHash, contentHash);
        try
        {
            Assert.IsFalse(Validate(source, logical, out string error));
            StringAssert.Contains("stale or mismatched", error);
        }
        finally { UnityEngine.Object.DestroyImmediate(logical); }
    }

    private static void AssertRuntimeReuse(
        string sourceContentHash,
        bool expectedAccepted)
    {
        OperationMapDefinition physical = Source();
        OperationMapDefinition logical = Logical(
            physical, SourceIdentityHash, sourceContentHash);
        BlobAssetReference<OperationMapBlob> blob = default;
        using World world = new("m01-physical-source-reuse");
        try
        {
            Assert.IsTrue(logical.TryCreatePersistentMetadataBlob(
                out blob, out string error), error);
            Entity mapRoot = world.EntityManager.CreateEntity(
                typeof(OperationMapRootComponent),
                typeof(ActiveOperationMapComponent),
                typeof(OperationMapMetadataComponent));
            world.EntityManager.SetComponentData(mapRoot,
                new ActiveOperationMapComponent
                {
                    OperationMapId = new FixedString64Bytes(LogicalMapId),
                    ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
                    MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
                    SchemaVersion = 1,
                    Generation = 7
                });
            world.EntityManager.SetComponentData(mapRoot,
                new OperationMapMetadataComponent
                {
                    Blob = blob,
                    Generation = 7,
                    PhysicalSourceValidated = 1
                });
            Entity missionRoot = world.EntityManager.CreateEntity(
                typeof(CampaignMissionRootComponent));
            DynamicBuffer<CampaignMissionLaunchRequestElement> requests =
                world.EntityManager.AddBuffer<CampaignMissionLaunchRequestElement>(missionRoot);
            requests.Add(new CampaignMissionLaunchRequestElement
            {
                OperationMapId = new FixedString64Bytes(LogicalMapId),
                ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
                MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact")
            });

            bool accepted = CampaignMissionOperationMapReuseUtility.TryReuse(
                world.EntityManager, physical, out Entity resolved, out error);
            Assert.AreEqual(expectedAccepted, accepted, error);
            Assert.AreEqual(expectedAccepted ? mapRoot : Entity.Null, resolved);
            Assert.AreEqual(expectedAccepted ? 1 : 0,
                world.EntityManager.GetComponentData<OperationMapMetadataComponent>(
                    mapRoot).PhysicalSourceValidated);
        }
        finally
        {
            if (blob.IsCreated)
                blob.Dispose();
            UnityEngine.Object.DestroyImmediate(logical);
        }
    }

    private static bool Validate(
        OperationMapDefinition source, OperationMapDefinition logical, out string error) =>
        OperationMapContractValidation.TryValidate(
            new[] { source, logical }, Array.Empty<ScenarioSetupConfig>(),
            new[] { Evidence(source), Evidence(logical) }, out error);

    private static OperationMapDefinition Source()
    {
        OperationMapDefinition source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(SourceDefinitionPath);
        Assert.IsNotNull(source);
        return source;
    }

    private static OperationMapDefinition Logical(
        OperationMapDefinition source, string sourceIdentityHash, string sourceContentHash)
    {
        OperationMapDefinition logical = UnityEngine.Object.Instantiate(source);
        logical.name = "M01LogicalSourceBindingFixture";
        SetField(logical, "operationMapId", LogicalMapId);
        SetField(logical, "sourceBinding", new OperationMapSourceBindingConfig(
            SourceMapId, sourceIdentityHash, sourceContentHash));
        return logical;
    }

    private static OperationMapContractEvidence Evidence(OperationMapDefinition definition) => new(
        definition.OperationMapId,
        definition.SchemaVersion,
        definition.ContentVersion,
        definition.SourceIdentityHash,
        definition.ContentHash,
        definition.GeneratedMetadataHash);

    private static void AssertAsset(string path, string expectedGuid, string expectedSha256)
    {
        Assert.AreEqual(expectedGuid, AssetDatabase.AssetPathToGUID(path));
        using SHA256 sha = SHA256.Create();
        string actual = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))
            .Replace("-", string.Empty).ToLowerInvariant();
        Assert.AreEqual(expectedSha256, actual, path);
    }

    private static void SetField<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field '{name}'.");
        field.SetValue(target, value);
    }
}
