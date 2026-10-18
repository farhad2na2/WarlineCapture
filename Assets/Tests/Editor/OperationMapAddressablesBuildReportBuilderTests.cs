using System;
using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Build.Layout;

public sealed class OperationMapAddressablesBuildReportBuilderTests
{
    private sealed class PreparedFixture : IDisposable
    {
        internal readonly string Root;
        internal PreparedFixture(){Root=System.IO.Path.GetFullPath("Logs/SkirmishProduction/Offline/PreparedContentControlTemp/"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(Root);}
        internal string Write(string name,string value){string p=System.IO.Path.Combine(Root,name);System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(p));System.IO.File.WriteAllText(p,value);return p;}
        public void Dispose(){string parent=System.IO.Path.GetFullPath("Logs/SkirmishProduction/Offline/PreparedContentControlTemp")+System.IO.Path.DirectorySeparatorChar;
            if(!Root.StartsWith(parent,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Owned fixture path required");
            BuildScript.RequirePreparedNonReparse(System.IO.File.GetAttributes(Root));System.IO.Directory.Delete(Root,true);}
    }
    private static Newtonsoft.Json.Linq.JObject PreparedSources(Newtonsoft.Json.Linq.JArray assets)
        =>new Newtonsoft.Json.Linq.JObject{["Assets"]=assets,["Packages"]=new Newtonsoft.Json.Linq.JArray(),["ProjectSettings"]=new Newtonsoft.Json.Linq.JArray()};
    private static Newtonsoft.Json.Linq.JObject PreparedLink(bool present=true)
        =>new Newtonsoft.Json.Linq.JObject{["present"]=present,["assetPath"]="Assets/Config/link.xml",["metaPath"]="Assets/Config/link.xml.meta",
            ["outputPath"]="unused-by-pure-lifecycle-control",["xmlSha256"]=new string('a',64),["metaSha256"]=new string('b',64),["metaGuid"]=new string('c',32)};
    private static Newtonsoft.Json.Linq.JObject PreparedLinkSources()
        =>PreparedSources(new Newtonsoft.Json.Linq.JArray(
            new Newtonsoft.Json.Linq.JObject{["path"]="Config/link.xml",["bytes"]=3,["sha256"]=new string('a',64)},
            new Newtonsoft.Json.Linq.JObject{["path"]="Config/link.xml.meta",["bytes"]=4,["sha256"]=new string('b',64)},
            new Newtonsoft.Json.Linq.JObject{["path"]="image.png",["bytes"]=2,["sha256"]=new string('d',64)}));
    [Test] public void PreparedTreeIncludesBinaryInputs()
    {using var f=new PreparedFixture();f.Write("image.png","binary-a");f.Write("model.fbx","binary-b");f.Write("font.ttf","binary-c");Assert.AreEqual(3,BuildScript.PreparedTree(f.Root).Count);}
    [Test] public void PreparedActualOutputMutationAndExtraFileRefused()
    {using var f=new PreparedFixture();f.Write("catalog.bin","original");var expected=BuildScript.PreparedTree(f.Root);f.Write("catalog.bin","changed");
        Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedEqual(expected,BuildScript.PreparedTree(f.Root),"output"));
        f.Write("catalog.bin","original");f.Write("extra.bundle","extra");Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedEqual(expected,BuildScript.PreparedTree(f.Root),"output"));}
    [Test] public void PreparedMissingOutputRefused()
    {using var f=new PreparedFixture();string p=f.Write("catalog.bin","original");f.Write("settings.json","settings");var expected=BuildScript.PreparedTree(f.Root);System.IO.File.Delete(p);
        Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedEqual(expected,BuildScript.PreparedTree(f.Root),"output"));}
    [Test] public void PreparedCaseAndReparseRefusals()
    {using var f=new PreparedFixture();Assert.Throws<InvalidOperationException>(()=>BuildScript.RequireUniquePreparedPaths(new[]{System.IO.Path.Combine(f.Root,"A"),System.IO.Path.Combine(f.Root,"a")}));
        Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedNonReparse(System.IO.FileAttributes.Directory|System.IO.FileAttributes.ReparsePoint));BuildScript.RequirePreparedNonReparse(System.IO.FileAttributes.Normal);}
    [Test] public void PreparedExactLinkPairUnchanged()
    {var a=PreparedLinkSources();var result=BuildScript.ValidatePreparedSourceLifecycle(a,(Newtonsoft.Json.Linq.JObject)a.DeepClone(),PreparedLink());Assert.IsTrue((bool)result["wholeSourceManifestUnchanged"]);}
    [Test] public void PreparedExactLinkPairRemovedIsDistinctObservedManifest()
    {var a=PreparedLinkSources();var b=(Newtonsoft.Json.Linq.JObject)a.DeepClone();((Newtonsoft.Json.Linq.JArray)b["Assets"]).RemoveAt(0);((Newtonsoft.Json.Linq.JArray)b["Assets"]).RemoveAt(0);
        var result=BuildScript.ValidatePreparedSourceLifecycle(a,b,PreparedLink());Assert.IsFalse((bool)result["wholeSourceManifestUnchanged"]);Assert.AreEqual(2,((Newtonsoft.Json.Linq.JArray)result["removedPaths"]).Count);
        Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(b,result["sourceManifestObserved"]));Assert.AreEqual(3,((Newtonsoft.Json.Linq.JArray)a["Assets"]).Count);}
    [Test] public void PreparedPartialPairRefused()
    {foreach(int index in new[]{0,1}){var a=PreparedLinkSources();var b=(Newtonsoft.Json.Linq.JObject)a.DeepClone();((Newtonsoft.Json.Linq.JArray)b["Assets"]).RemoveAt(index);Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSourceLifecycle(a,b,PreparedLink()));}}
    [Test] public void PreparedMutatedPairRefused()
    {foreach(int index in new[]{0,1}){var a=PreparedLinkSources();var b=(Newtonsoft.Json.Linq.JObject)a.DeepClone();b["Assets"][index]["sha256"]=new string('e',64);Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSourceLifecycle(a,b,PreparedLink()));}}
    [Test] public void PreparedOtherSourceDriftRefused()
    {var a=PreparedLinkSources();var b=(Newtonsoft.Json.Linq.JObject)a.DeepClone();b["Assets"][2]["sha256"]=new string('e',64);Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSourceLifecycle(a,b,PreparedLink()));}
    [Test] public void PreparedAbsentOriginalGrantsNoWaiver()
    {var a=PreparedSources(new Newtonsoft.Json.Linq.JArray());Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSourceLifecycle(a,PreparedLinkSources(),PreparedLink(false)));}
    [Test] public void PreparedPinnedLinkHashAndGuidRequired()
    {var a=PreparedLinkSources();var link=PreparedLink();link["xmlSha256"]=new string('e',64);Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSourceLifecycle(a,(Newtonsoft.Json.Linq.JObject)a.DeepClone(),link));
        link=PreparedLink();link["metaGuid"]="bad";Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSourceLifecycle(a,(Newtonsoft.Json.Linq.JObject)a.DeepClone(),link));}
    [Test] public void PreparedSerializedSettingsOnlyScopedEnumMayDiffer()
    {var expected=new Newtonsoft.Json.Linq.JObject{["m_BuildAddressablesWithPlayerBuild"]=0,["other"]=1};var actual=(Newtonsoft.Json.Linq.JObject)expected.DeepClone();BuildScript.ValidatePreparedSettings(expected,actual,false);
        actual["m_BuildAddressablesWithPlayerBuild"]=2;BuildScript.ValidatePreparedSettings(expected,actual,true);Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSettings(expected,actual,false));
        actual["other"]=2;Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSettings(expected,actual,true));}
    private static void WithPreparedSettings(Action<UnityEditor.AddressableAssets.Settings.AddressableAssetSettings> action)
    {Assert.IsNull(AddressablesPlayerBuildProcessor.BuildAddressablesOverride);var settings=UnityEngine.ScriptableObject.CreateInstance<UnityEditor.AddressableAssets.Settings.AddressableAssetSettings>();
        try{action(settings);}finally{UnityEngine.Object.DestroyImmediate(settings);}}
    [Test] public void PreparedSentinelRefusesRebuildAndRestores()
    {WithPreparedSettings(settings=>{var prior=settings.BuildAddressablesWithPlayerBuild;BuildScript.WithPreparedNoRebuild(settings,()=>{
        Assert.AreEqual(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer,settings.BuildAddressablesWithPlayerBuild);
        Assert.Throws<InvalidOperationException>(()=>AddressablesPlayerBuildProcessor.BuildAddressablesOverride(settings));});Assert.AreEqual(prior,settings.BuildAddressablesWithPlayerBuild);Assert.IsNull(AddressablesPlayerBuildProcessor.BuildAddressablesOverride);});}
    [Test] public void PreparedBuildExceptionStillRestoresScope()
    {WithPreparedSettings(settings=>{var prior=settings.BuildAddressablesWithPlayerBuild;Assert.Throws<InvalidOperationException>(()=>BuildScript.WithPreparedNoRebuild(settings,()=>throw new InvalidOperationException("injected build failure")));
        Assert.AreEqual(prior,settings.BuildAddressablesWithPlayerBuild);Assert.IsNull(AddressablesPlayerBuildProcessor.BuildAddressablesOverride);});}
    [Test] public void PreparedForeignCallbackNeverReplaced()
    {WithPreparedSettings(settings=>{Func<UnityEditor.AddressableAssets.Settings.AddressableAssetSettings,UnityEditor.AddressableAssets.Build.AddressablesPlayerBuildResult> foreign=_=>null;
        AddressablesPlayerBuildProcessor.BuildAddressablesOverride=foreign;try{Assert.Throws<InvalidOperationException>(()=>BuildScript.WithPreparedNoRebuild(settings,()=>Assert.Fail("foreign callback must refuse before build")));Assert.AreSame(foreign,AddressablesPlayerBuildProcessor.BuildAddressablesOverride);}
        finally{if(AddressablesPlayerBuildProcessor.BuildAddressablesOverride==foreign)AddressablesPlayerBuildProcessor.BuildAddressablesOverride=null;}});}
    [Test] public void PreparedForeignReplacementPreservedAndScopeFails()
    {WithPreparedSettings(settings=>{var prior=settings.BuildAddressablesWithPlayerBuild;Func<UnityEditor.AddressableAssets.Settings.AddressableAssetSettings,UnityEditor.AddressableAssets.Build.AddressablesPlayerBuildResult> foreign=_=>null;
        try{Assert.Throws<InvalidOperationException>(()=>BuildScript.WithPreparedNoRebuild(settings,()=>AddressablesPlayerBuildProcessor.BuildAddressablesOverride=foreign));Assert.AreSame(foreign,AddressablesPlayerBuildProcessor.BuildAddressablesOverride);Assert.AreEqual(prior,settings.BuildAddressablesWithPlayerBuild);}
        finally{if(AddressablesPlayerBuildProcessor.BuildAddressablesOverride==foreign)AddressablesPlayerBuildProcessor.BuildAddressablesOverride=null;}});}
    [Test] public void PreparedReportHashMismatchRefused()
    {using var f=new PreparedFixture();string p=f.Write("report.json","original");var hash=new Newtonsoft.Json.Linq.JValue(BuildScript.PreparedFileHash(p));f.Write("report.json","other");Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedEqual(hash,new Newtonsoft.Json.Linq.JValue(BuildScript.PreparedFileHash(p)),"report"));}
    [Test] public void PreparedFreshReportGuardBeforeBuild()
    {using var f=new PreparedFixture();string key=OperationMapAddressablesBuildReportBuilder.OutputPathEnvironmentVariable,prior=Environment.GetEnvironmentVariable(key);string p=System.IO.Path.Combine(f.Root,"fresh.json");
        try{Environment.SetEnvironmentVariable(key,p);BuildScript.RequirePreparedFreshReport(p);f.Write("fresh.json","exists");Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedFreshReport(p));
            Environment.SetEnvironmentVariable(key,null);Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedFreshReport(System.IO.Path.Combine(f.Root,"other.json")));}
        finally{Environment.SetEnvironmentVariable(key,prior);}}
    private static Newtonsoft.Json.Linq.JObject PreparedRealMeta(PreparedFixture f,string file,string guid,string extra="userData: ")
    {
        string path=f.Write(file,"fileFormatVersion: 2\nguid: "+guid+"\nDefaultImporter:\n  "+extra+"\n");var link=PreparedLink();
        link["metaGuid"]=guid;link["metaSha256"]=BuildScript.PreparedFileHash(path);link["metaBase64"]=Convert.ToBase64String(System.IO.File.ReadAllBytes(path));return link;
    }
    [Test] public void PreparedPostPlayerOnlyGuidMetaChangeAllowed()
    {
        using var f=new PreparedFixture();var first=PreparedRealMeta(f,"a.meta",new string('a',32));var second=PreparedRealMeta(f,"b.meta",new string('b',32));
        var a=PreparedLinkSources();a["Assets"][1]["sha256"]=first["metaSha256"].DeepClone();a["Assets"][1]["bytes"]=Convert.FromBase64String((string)first["metaBase64"]).Length;
        var b=(Newtonsoft.Json.Linq.JObject)a.DeepClone();b["Assets"][1]["sha256"]=second["metaSha256"].DeepClone();
        var pre=(Newtonsoft.Json.Linq.JObject)a.DeepClone();((Newtonsoft.Json.Linq.JArray)pre["Assets"]).RemoveAt(0);((Newtonsoft.Json.Linq.JArray)pre["Assets"]).RemoveAt(0);
        var preProof=BuildScript.ValidatePreparedSourceLifecycle(a,pre,first);
        var result=BuildScript.ValidatePreparedPostPlayerSource(a,b,first,second,preProof);
        Assert.IsTrue((bool)result["metaGuidChanged"]);Assert.IsFalse((bool)result["matchesPreparedASourceManifest"]);Assert.IsFalse((bool)result["sourceManifestUnchangedFromBPrebuild"]);
        Assert.AreEqual(2,((Newtonsoft.Json.Linq.JArray)result["createdPaths"]).Count);Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(b,result["sourceManifestObserved"]));
    }
    [Test] public void PreparedPostPlayerRejectsNonGuidMetaAndForeignXml()
    {
        using var f=new PreparedFixture();var first=PreparedRealMeta(f,"a.meta",new string('a',32));var foreign=PreparedRealMeta(f,"b.meta",new string('b',32),"userData: foreign");
        Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedMetaTemplate(first,foreign));
        var second=PreparedRealMeta(f,"c.meta",new string('b',32));second["xmlSha256"]=new string('e',64);
        var a=PreparedLinkSources();Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedPostPlayerSource(a,(Newtonsoft.Json.Linq.JObject)a.DeepClone(),first,second,new Newtonsoft.Json.Linq.JObject()));
    }
    public static void RunPreparedContentFocusedValidation()
    {
        var t=new OperationMapAddressablesBuildReportBuilderTests();t.PreparedTreeIncludesBinaryInputs();t.PreparedActualOutputMutationAndExtraFileRefused();t.PreparedMissingOutputRefused();t.PreparedCaseAndReparseRefusals();
        t.PreparedExactLinkPairUnchanged();t.PreparedExactLinkPairRemovedIsDistinctObservedManifest();t.PreparedPartialPairRefused();t.PreparedMutatedPairRefused();t.PreparedOtherSourceDriftRefused();t.PreparedAbsentOriginalGrantsNoWaiver();t.PreparedPinnedLinkHashAndGuidRequired();
        t.PreparedSerializedSettingsOnlyScopedEnumMayDiffer();t.PreparedSentinelRefusesRebuildAndRestores();t.PreparedBuildExceptionStillRestoresScope();t.PreparedForeignCallbackNeverReplaced();t.PreparedForeignReplacementPreservedAndScopeFails();t.PreparedReportHashMismatchRefused();t.PreparedFreshReportGuardBeforeBuild();
        t.PreparedPostPlayerOnlyGuidMetaChangeAllowed();t.PreparedPostPlayerRejectsNonGuidMetaAndForeignXml();
        UnityEngine.Debug.Log("[PreparedContentSplitControls] result=Passed controls=20 actualContentBuild=0 actualPlayerBuild=0 acceptance=0");
    }

    [Test]
    public void RuntimeSettings_AcceptBuiltInCatalogWithUpdatesDisabled()
    {
        const string json =
            "{\"m_DisableCatalogUpdateOnStart\":true," +
            "\"m_CatalogLocations\":[{\"m_InternalId\":" +
            "\"{UnityEngine.AddressableAssets.Addressables.RuntimePath}/catalog.bin\"}]}";

        Assert.That(
            OperationMapAddressablesBuildReportBuilder.TryValidateRuntimeSettings(
                json,
                out string error),
            Is.True,
            error);
    }

    [TestCase(false, "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/catalog.bin")]
    [TestCase(true, "https://content.example/catalog.bin")]
    public void RuntimeSettings_RejectCatalogUpdateOrRemoteLocation(
        bool disableUpdates,
        string internalId)
    {
        string json =
            $"{{\"m_DisableCatalogUpdateOnStart\":{disableUpdates.ToString().ToLowerInvariant()}," +
            $"\"m_CatalogLocations\":[{{\"m_InternalId\":\"{internalId}\"}}]}}";

        Assert.That(
            OperationMapAddressablesBuildReportBuilder.TryValidateRuntimeSettings(
                json,
                out string error),
            Is.False);
        Assert.That(error, Is.Not.Empty);
    }

    [Test]
    public void Create_AttributesBundleClosurePartitionsEntitiesAndDuplicates()
    {
        BuildLayout layout = CreateLayout(reverseAssets: false);

        OperationMapAddressablesBuildReport report =
            OperationMapAddressablesBuildReportBuilder.Create(layout);

        Assert.AreEqual(1, report.Maps.Length);
        Assert.AreEqual(3, report.Maps[0].BundleCount);
        Assert.AreEqual(600ul, report.Maps[0].BundleBytes);
        Assert.AreEqual(600ul, report.AggregateBundleBytes);
        Assert.AreEqual(1, report.Partitions.Length);
        Assert.AreEqual(2, report.Partitions[0].EntryCount);
        Assert.AreEqual(2, report.Partitions[0].BundleCount);
        Assert.AreEqual(2, report.RequiredAddresses.Length);
        Assert.AreEqual(1, report.EntitiesArtifacts.Length);
        Assert.AreEqual(44ul, report.EntitiesArtifacts[0].Bytes);
        Assert.AreEqual(1, report.DuplicateDependencies.Length);
        Assert.AreEqual(2, report.DuplicateDependencies[0].BundleCount);
        Assert.AreEqual(12ul, report.DuplicateDependencies[0].DuplicateBytes);
    }

    [Test]
    public void Serialize_IsByteIdenticalWhenInputOrderChanges()
    {
        string first = OperationMapAddressablesBuildReportBuilder.Serialize(
            OperationMapAddressablesBuildReportBuilder.Create(CreateLayout(reverseAssets: false)));
        string second = OperationMapAddressablesBuildReportBuilder.Serialize(
            OperationMapAddressablesBuildReportBuilder.Create(CreateLayout(reverseAssets: true)));

        Assert.AreEqual(first, second);
    }

    [Test]
    public void Create_RejectsDuplicateStableAddresses()
    {
        BuildLayout layout = CreateLayout(reverseAssets: false);
        layout.Groups[0].Bundles[1].Files[0].Assets[0].AddressableName =
            layout.Groups[0].Bundles[0].Files[0].Assets[0].AddressableName;

        Assert.Throws<InvalidOperationException>(
            () => OperationMapAddressablesBuildReportBuilder.Create(layout));
    }

    [Test]
    public void DuplicateValidation_RejectsProjectOwnedDependency()
    {
        OperationMapAddressablesDuplicateDependencyReport[] duplicates =
        {
            new("project-guid", "Assets/Shared.asset", 2, 12)
        };

        Assert.IsFalse(
            OperationMapAddressablesBuildReportBuilder.TryValidateDuplicateDependencies(
                duplicates,
                out string error));
        StringAssert.Contains("project-guid", error);
        StringAssert.Contains("Assets/Shared.asset", error);
    }

    [Test]
    public void DuplicateValidation_RejectsRuntimePackageDependency()
    {
        OperationMapAddressablesDuplicateDependencyReport[] duplicates =
        {
            new("package-guid", "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader", 2, 12)
        };

        Assert.IsFalse(
            OperationMapAddressablesBuildReportBuilder.TryValidateDuplicateDependencies(
                duplicates,
                out string error),
            error);
        StringAssert.Contains("Lit.shader", error);
    }

    [Test]
    public void DuplicateValidation_AllowsEditorOnlyPackageDependency()
    {
        OperationMapAddressablesDuplicateDependencyReport[] duplicates =
        {
            new(
                "package-guid",
                "Packages/com.unity.shadergraph/Editor/Resources/Shaders/FallbackError.shader",
                2,
                12),
            new(
                "mixed-case-package-guid",
                "Packages/com.example.rendering/eDiToR/Shaders/FallbackError.shader",
                2,
                12)
        };

        Assert.IsTrue(
            OperationMapAddressablesBuildReportBuilder.TryValidateDuplicateDependencies(
                duplicates,
                out string error),
            error);
    }

    [Test]
    public void Publish_DoesNotRewriteIdenticalContent()
    {
        string path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "operation-map-addressables-build-report-test.json");
        try
        {
            Assert.IsTrue(OperationMapAddressablesBuildReportBuilder.Publish(path, "report\n"));
            DateTime firstWrite = System.IO.File.GetLastWriteTimeUtc(path);
            Assert.IsFalse(OperationMapAddressablesBuildReportBuilder.Publish(path, "report\n"));
            Assert.AreEqual(firstWrite, System.IO.File.GetLastWriteTimeUtc(path));
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    private static BuildLayout CreateLayout(bool reverseAssets)
    {
        BuildLayout layout = new()
        {
            BuildResultHash = "0123456789abcdef0123456789abcdef",
            BuildTarget = BuildTarget.Android
        };
        BuildLayout.Group group = new() { Name = "Operation Map" };
        layout.Groups.Add(group);

        BuildLayout.Bundle dependency = CreateBundle("shared", 300);
        BuildLayout.Bundle first = CreateBundle("first", 100);
        BuildLayout.Bundle second = CreateBundle("second", 200);
        first.Dependencies = new List<BuildLayout.Bundle> { dependency };
        second.Dependencies = new List<BuildLayout.Bundle> { dependency };
        group.Bundles.Add(first);
        group.Bundles.Add(second);
        group.Bundles.Add(dependency);

        const string partition = "operation-map-partition-skirmish-desert-base-01-region-p000-p000";
        BuildLayout.ExplicitAsset firstAsset = CreateAsset(
            first,
            "operation-map/opmap.skirmish.desert_base_01/a",
            partition);
        BuildLayout.ExplicitAsset secondAsset = CreateAsset(
            second,
            "operation-map/opmap.skirmish.desert_base_01/b",
            partition);
        if (reverseAssets)
        {
            first.Files[0].Assets.Add(secondAsset);
            secondAsset.File = first.Files[0];
            secondAsset.Bundle = first;
            second.Files[0].Assets.Add(firstAsset);
            firstAsset.File = second.Files[0];
            firstAsset.Bundle = second;
        }
        else
        {
            first.Files[0].Assets.Add(firstAsset);
            second.Files[0].Assets.Add(secondAsset);
        }

        first.Files[0].SubFiles.Add(new BuildLayout.SubFile
        {
            Name = "scene.entityheader",
            Size = 44
        });
        AddDuplicateImplicitAsset(first, "duplicate-guid", 12);
        AddDuplicateImplicitAsset(second, "duplicate-guid", 12);
        layout.DuplicatedAssets.Add(new BuildLayout.AssetDuplicationData
        {
            AssetGuid = "duplicate-guid",
            DuplicatedObjects = new List<BuildLayout.ObjectDuplicationData>
            {
                new()
                {
                    LocalIdentifierInFile = 1,
                    IncludedInBundleFiles = new List<BuildLayout.File>
                    {
                        first.Files[0],
                        second.Files[0]
                    }
                }
            }
        });
        return layout;
    }

    private static BuildLayout.Bundle CreateBundle(string name, ulong bytes)
    {
        BuildLayout.Bundle bundle = new() { Name = name, FileSize = bytes };
        BuildLayout.File file = new() { Name = name + ".file", Bundle = bundle };
        bundle.Files.Add(file);
        return bundle;
    }

    private static BuildLayout.ExplicitAsset CreateAsset(
        BuildLayout.Bundle bundle,
        string address,
        string partition)
    {
        return new BuildLayout.ExplicitAsset
        {
            AddressableName = address,
            AssetPath = "Assets/" + address.GetHashCode() + ".asset",
            Bundle = bundle,
            File = bundle.Files[0],
            Labels = new[]
            {
                OperationMapAddressablesLayoutBuilder.PackLabel,
                partition
            }
        };
    }

    private static void AddDuplicateImplicitAsset(
        BuildLayout.Bundle bundle,
        string guid,
        ulong bytes)
    {
        bundle.Files[0].OtherAssets.Add(new BuildLayout.DataFromOtherAsset
        {
            AssetGuid = guid,
            AssetPath = "Assets/Shared.asset",
            File = bundle.Files[0],
            SerializedSize = bytes
        });
    }
}
