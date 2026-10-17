using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishS002AriaHarnessTests
    {

        private static string ActualRequestRelative(string root,string path,HashSet<string> directories,Action<string> observed=null)
        {
            var method=typeof(SkirmishProductionCanaryRequest).GetMethod("Relative",
                System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            try { return (string)method.Invoke(null,new object[]{root,path,directories,observed}); }
            catch(System.Reflection.TargetInvocationException error) when(error.InnerException!=null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
        private static void ActualRequestPostTopology(string root, Newtonsoft.Json.Linq.JArray manifest)
        {
            var method = typeof(SkirmishProductionCanaryRequest).GetMethod("ValidateSourceManifestTopology",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            try { method.Invoke(null, new object[] { root, manifest }); }
            catch (System.Reflection.TargetInvocationException error) when (error.InnerException != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }

        [TestCase("countedSharedParents")]
        [TestCase("replacementBetweenPasses")]
        [TestCase("freshLeafReparse")]
        [TestCase("caseDistinctAncestor")]
        [TestCase("midPassAncestorReplacement")]
        [TestCase("foreignAbsolute")]
        [TestCase("colon")]
        [TestCase("traversal")]
        [TestCase("backslash")]
        [TestCase("duplicateManifest")]
        [TestCase("missingLeaf")]
        public void RequestPathValidation_ActualFilesystem(string kind)
        {
            string root=FreshProfileFixtureRoot();
            bool cleanupSafe=true;
            string link=null,socketPath=null;
            OwnedProfileSocket socket=null;
            string sentinelPath=null;
            try
            {
                string shared=Path.Combine(root,"Assets","shared");
                Directory.CreateDirectory(shared);
                File.WriteAllText(Path.Combine(shared,"a.json"),"a");
                File.WriteAllText(Path.Combine(shared,"b.json"),"b");
                var checkedDirectories=new HashSet<string>(StringComparer.Ordinal);
                if(kind=="countedSharedParents")
                {
                    var reads=new List<string>();
                    Action<string> counted=path=>reads.Add(path);
                    ActualRequestRelative(root,"Assets/shared/a.json",checkedDirectories,counted);
                    ActualRequestRelative(root,"Assets/shared/b.json",checkedDirectories,counted);
                    Assert.AreEqual(4,reads.Count,"Two real leaf reads plus two unique ancestors.");
                    Assert.AreEqual(1,reads.FindAll(p=>p==Path.Combine(shared,"a.json")).Count);
                    Assert.AreEqual(1,reads.FindAll(p=>p==Path.Combine(shared,"b.json")).Count);
                    Assert.AreEqual(1,reads.FindAll(p=>p==shared).Count);
                    Assert.AreEqual(1,reads.FindAll(p=>p==Path.Combine(root,"Assets")).Count);
                    var second=new List<string>();
                    ActualRequestRelative(root,"Assets/shared/a.json",new HashSet<string>(StringComparer.Ordinal),p=>second.Add(p));
                    Assert.AreEqual(3,second.Count,"A separate pass checks the full path afresh.");
                }
                else if(kind=="replacementBetweenPasses")
                {
                    Assert.AreEqual(RuntimePlatform.WindowsEditor,Application.platform);
                    ActualRequestRelative(root,"Assets/shared/a.json",checkedDirectories);
                    string target=Path.Combine(root,"target");Directory.CreateDirectory(target);
                    sentinelPath=Path.Combine(target,"sentinel");File.WriteAllText(sentinelPath,"unchanged");
                    File.WriteAllText(Path.Combine(target,"a.json"),"a");
                    File.Delete(Path.Combine(shared,"a.json"));File.Delete(Path.Combine(shared,"b.json"));
                    Directory.Delete(shared,false);link=shared;
                    CreateOwnedProfileJunction(link,target,ref cleanupSafe);
                    Assert.Throws<InvalidDataException>(()=>ActualRequestRelative(root,"Assets/shared/a.json",
                        new HashSet<string>(StringComparer.Ordinal)));
                    Assert.AreEqual("unchanged",File.ReadAllText(sentinelPath));
                }
                else if(kind=="caseDistinctAncestor")
                {
                    ActualRequestRelative(root,"Assets/shared/a.json",checkedDirectories);
                    string upper=Path.Combine(root,"Assets","SHARED");
                    Assert.IsFalse(checkedDirectories.Contains(upper),"Ordinal validation must not conflate case-distinct paths.");
                    Directory.CreateDirectory(upper);
                    string probe=Path.Combine(upper,"namespace-probe");File.WriteAllText(probe,"probe");
                    bool sameDirectory=File.Exists(Path.Combine(shared,"namespace-probe"));
                    File.Delete(probe);
                    if(sameDirectory)
                        Debug.Log("[RequestPathCaseDistinct] status=FilesystemCaseInsensitive distinctReparseNegative=Pending ordinalPolicyChecked=1");
                    else
                    {
                        Assert.AreEqual(RuntimePlatform.WindowsEditor,Application.platform);
                        Directory.Delete(upper,false);
                        string target=Path.Combine(root,"target");Directory.CreateDirectory(target);
                        sentinelPath=Path.Combine(target,"sentinel");File.WriteAllText(sentinelPath,"unchanged");
                        File.WriteAllText(Path.Combine(target,"a.json"),"a");
                        link=upper;CreateOwnedProfileJunction(link,target,ref cleanupSafe);
                        Assert.Throws<InvalidDataException>(()=>ActualRequestRelative(root,"Assets/SHARED/a.json",checkedDirectories));
                        Debug.Log("[RequestPathCaseDistinct] status=Passed distinctReparseNegative=Proved");
                    }
                }
                else if(kind=="midPassAncestorReplacement")
                {
                    Assert.AreEqual(RuntimePlatform.WindowsEditor,Application.platform);
                    var manifest=new Newtonsoft.Json.Linq.JArray(
                        new Newtonsoft.Json.Linq.JObject{["path"]="Assets/shared/a.json",["sha256"]=SkirmishProductionCanaryRequest.FileHash(Path.Combine(shared,"a.json"))},
                        new Newtonsoft.Json.Linq.JObject{["path"]="Assets/shared/b.json",["sha256"]=SkirmishProductionCanaryRequest.FileHash(Path.Combine(shared,"b.json"))});
                    foreach(var row in manifest)
                        ActualRequestRelative(root,(string)row["path"],checkedDirectories);
                    ActualRequestPostTopology(root,manifest); // Actual positive before the replacement.
                    string target=Path.Combine(root,"target");Directory.CreateDirectory(target);
                    sentinelPath=Path.Combine(target,"sentinel");File.WriteAllText(sentinelPath,"unchanged");
                    File.WriteAllText(Path.Combine(target,"a.json"),"a");
                    File.WriteAllText(Path.Combine(target,"b.json"),"b");
                    File.Delete(Path.Combine(shared,"a.json"));File.Delete(Path.Combine(shared,"b.json"));
                    Directory.Delete(shared,false);link=shared;
                    CreateOwnedProfileJunction(link,target,ref cleanupSafe);
                    foreach(var row in manifest)
                        Assert.AreEqual((string)row["sha256"],SkirmishProductionCanaryRequest.FileHash(
                            Path.Combine(root,((string)row["path"]).Replace('/',Path.DirectorySeparatorChar))),
                            "Same bytes do not establish physical path topology.");
                    Assert.Throws<InvalidDataException>(()=>ActualRequestPostTopology(root,manifest));
                    Assert.AreEqual("unchanged",File.ReadAllText(sentinelPath));
                }
                else if(kind=="freshLeafReparse")
                {
                    Assert.AreEqual(RuntimePlatform.WindowsEditor,Application.platform);
                    ActualRequestRelative(root,"Assets/shared/a.json",checkedDirectories);
                    socketPath=Path.Combine(root,"Assets","shared","s");
                    socket=new OwnedProfileSocket(socketPath);
                    Assert.Throws<InvalidDataException>(()=>ActualRequestRelative(root,"Assets/shared/s",checkedDirectories));
                }
                else if(kind=="duplicateManifest")
                {
                    var job=new Newtonsoft.Json.Linq.JObject{
                        ["schema_version"]=1,["job_id"]="path-control",["catalog_id"]="S005",
                        ["definition_id"]="skirmish.s005",["definition_version"]=1,
                        ["candidate_sha256"]=new string('a',64),["build_sha256"]=new string('b',64),
                        ["setup_sha256"]=new string('c',64),["content_sha256"]=new string('d',64),
                        ["source_manifest"]=new Newtonsoft.Json.Linq.JArray(
                            new Newtonsoft.Json.Linq.JObject{["path"]="Assets/shared/a.json",["sha256"]=new string('a',64)},
                            new Newtonsoft.Json.Linq.JObject{["path"]="Assets/shared/a.json",["sha256"]=new string('a',64)}),
                        ["packet_path"]="Design/Roadmap/Skirmish_Expansion/Scenarios/DB_BA.md",
                        ["packet_sha256"]=new string('e',64),["seed"]=155926,["locale"]="FA",
                        ["size"]="Standard",["difficulty"]="Regular",["control_mode"]="aria",
                        ["required_stages"]=new Newtonsoft.Json.Linq.JArray("compile","live_api","source_fit","normal_input","terminal","return"),
                        ["timeout_seconds"]=900};
                    var claim=new Newtonsoft.Json.Linq.JObject{
                        ["attempt_id"]="attempt-"+new string('a',32),["token"]=new string('b',32),
                        ["worker"]="path-control",["started_utc"]=DateTime.UtcNow.ToString("O"),
                        ["job_sha256"]=new string('c',64),["artifact_directory"]=Path.Combine(root,"attempt")};
                    string jp=Path.Combine(root,"job.json"),cp=Path.Combine(root,"claim.json");
                    File.WriteAllText(jp,job.ToString());File.WriteAllText(cp,claim.ToString());
                    var error=Assert.Throws<InvalidDataException>(()=>SkirmishProductionCanaryRequest.Read(root,jp,cp,false));
                    StringAssert.Contains("Duplicate source path",error.Message);
                }
                else
                {
                    string path=kind=="foreignAbsolute"?Path.Combine(Path.GetTempPath(),"foreign.json"):
                        kind=="colon"?"C:foreign.json":kind=="traversal"?"Assets/../foreign.json":
                        kind=="backslash"?"Assets\\shared\\a.json":"Assets/shared/missing.json";
                    if(kind=="missingLeaf")
                    {
                        var error=Assert.Throws<InvalidDataException>(()=>ActualRequestRelative(root,path,checkedDirectories));
                        StringAssert.Contains("Reparse/symlink source rejected.",error.Message);
                    }
                    else Assert.Throws<InvalidDataException>(()=>ActualRequestRelative(root,path,checkedDirectories));
                }
            }
            finally
            {
                try { socket?.Dispose(); }
                finally
                {
                    if(socketPath!=null&&File.Exists(socketPath))File.Delete(socketPath);
                    if(cleanupSafe && link!=null&&Directory.Exists(link))
                    {
                        Assert.AreNotEqual(0,(int)(File.GetAttributes(link)&FileAttributes.ReparsePoint));
                        Directory.Delete(link,false);
                    }
                    if(cleanupSafe && sentinelPath!=null)Assert.AreEqual("unchanged",File.ReadAllText(sentinelPath));
                    if(cleanupSafe)DeleteProfileFixtureRoot(root);
                }
            }
        }
        public static void RunRequestPathValidationControls()
        {
            var tests=new SkirmishS002AriaHarnessTests();
            foreach(string kind in new[]{"countedSharedParents","replacementBetweenPasses","freshLeafReparse",
                "foreignAbsolute","colon","traversal","backslash","duplicateManifest","missingLeaf","caseDistinctAncestor","midPassAncestorReplacement"})
                tests.RequestPathValidation_ActualFilesystem(kind);
            Debug.Log("[RequestPathValidationControls] result=Passed cases=11 caseDistinctReparse=CapabilityGated postPassTopology=Fresh freshPerRead=1 persistentCache=0 nativeMissionAcceptance=Pending");
        }

        private static SkirmishScenarioDefinitionConfig ActualBatchDefinition(string id)
        {
            var catalog = SkirmishExpansionCatalogConfig.Load();
            Assert.IsNotNull(catalog);
            Assert.IsTrue(catalog.TryGetDefinition(id, out var definition), id);
            Assert.IsNotNull(definition, id);
            return definition;
        }

        [TestCase("S002", SkirmishPresetConfig.DesertBaseEstablishedScenarioIndex)]
        [TestCase("S003", SkirmishPresetConfig.DesertBaseAirMobileFieldScenarioIndex)]
        public void BatchWarmup_ActualCodedNullSourcePreservesNormalBinding(string id, int index)
        {
            var definition = ActualBatchDefinition(id);
            Assert.IsNull(definition.CampaignMap, "Fixture covers the actual intentionally omitted coded source.");
            var normalPreset = SkirmishLegacyProductionIdentity.RequireBinding("S001");
            Assert.AreSame(normalPreset, SkirmishPresetConfig.Load(index));
            var ordinary = SkirmishLegacyProductionIdentity.ResolveSource("S001", out _);
            var actual = SkirmishS002AriaRunHarness.ResolveProductionBatchWarmupSource(definition);
            Assert.AreSame(ordinary, actual);
            Assert.AreEqual(definition.OperationMapId, actual.OperationMapId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(actual.NavigationMetadata.AuthoredSubSceneGuid));
            Assert.IsNull(definition.CampaignMap, "Warmup must not assign the persistent definition.");
        }

        [Test]
        public void BatchWarmup_ActualS004UsesRuntimeEnvironmentSource()
        {
            var definition = ActualBatchDefinition("S004");
            Assert.IsNull(definition.CampaignMap);
            var source = Resources.Load<OperationMapDefinition>("SkirmishS004OperationMap");
            Assert.IsNotNull(source);
            var defaultWorld = World.DefaultGameObjectInjectionWorld;
            using (var world = new World("BatchWarmupS004SourceControl"))
            {
                var em = world.EntityManager;
                var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
                em.SetComponentData(session, new SkirmishExpandedSessionComponent { IsLegacy = 0 });
                em.AddComponentObject(session, new SkirmishResolvedSetupRecord {
                    Setup = new SkirmishResolvedSetup { CatalogId = "S004" }
                });
                Assert.IsTrue(SkirmishS004Environment.IsActive(em));
                Assert.AreSame(source, SkirmishS004Environment.LoadMapForSession(em));
                Assert.AreSame(source, SkirmishS002AriaRunHarness.ResolveProductionBatchWarmupSource(definition));
                Assert.AreEqual(definition.OperationMapId, source.OperationMapId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(source.NavigationMetadata.AuthoredSubSceneGuid));
            }
            Assert.AreSame(defaultWorld, World.DefaultGameObjectInjectionWorld);
            Assert.IsNull(definition.CampaignMap);
        }

        [Test]
        public void BatchWarmup_ActualS004MustNotUseS001Source()
        {
            var source = SkirmishS002AriaRunHarness.ResolveProductionBatchWarmupSource(ActualBatchDefinition("S004"));
            var s001 = SkirmishLegacyProductionIdentity.ResolveSource("S001", out _);
            Assert.AreNotSame(s001, source, "Same coded preset does not identify the S004 physical source.");
            Assert.AreNotEqual(s001.SourceIdentityHash, source.SourceIdentityHash);
            Assert.AreNotEqual(s001.ContentHash, source.ContentHash);
            Assert.AreSame(Resources.Load<OperationMapDefinition>("SkirmishS004OperationMap"), source);
        }

        [Test]
        public void BatchWarmup_ExplicitSourcePrecedesS004Resource()
        {
            WithBatchDefinitionClone("S004", definition => {
                var explicitSource = ActualBatchDefinition("S005").CampaignMap;
                Assert.IsNotNull(explicitSource);
                definition.BindCampaignMap(explicitSource);
                Assert.AreSame(explicitSource, SkirmishS002AriaRunHarness.ResolveProductionBatchWarmupSource(definition));
            });
        }

        [Test]
        public void BatchWarmup_ActualExplicitS005SourceUnchanged()
        {
            var definition = ActualBatchDefinition("S005");
            Assert.IsNotNull(definition.CampaignMap);
            Assert.AreSame(definition.CampaignMap, SkirmishS002AriaRunHarness.ResolveProductionBatchWarmupSource(definition));
        }

        private static void WithBatchDefinitionClone(string id, Action<SkirmishScenarioDefinitionConfig> action)
        {
            var clone = UnityEngine.Object.Instantiate(ActualBatchDefinition(id));
            try { action(clone); }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
        }

        [Test]
        public void BatchWarmup_GenericMissingExplicitSourceRefused()
        {
            WithBatchDefinitionClone("S002", definition => {
                var serialized = new UnityEditor.SerializedObject(definition);
                serialized.FindProperty("catalogId").stringValue = "S049";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidDataException>(() => SkirmishS002AriaRunHarness.ResolveProductionBatchWarmupSource(definition));
            });
        }

        [Test]
        public void BatchWarmup_S005MissingExplicitSourceRefused()
        {
            WithBatchDefinitionClone("S005", definition => {
                definition.BindCampaignMap(null);
                Assert.Throws<InvalidDataException>(() => SkirmishS002AriaRunHarness.ResolveProductionBatchWarmupSource(definition));
            });
        }

        [Test]
        public void BatchWarmup_CodedForeignMapRefused()
        {
            WithBatchDefinitionClone("S002", definition => {
                var serialized = new UnityEditor.SerializedObject(definition);
                serialized.FindProperty("operationMapId").stringValue = "opmap.foreign";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidDataException>(() => SkirmishS002AriaRunHarness.ResolveProductionBatchWarmupSource(definition));
            });
        }

        [Test]
        public void BatchWarmup_NullDefinitionRefused()
        {
            Assert.Throws<InvalidDataException>(() => SkirmishS002AriaRunHarness.ResolveProductionBatchWarmupSource(null));
        }

        [Test]
        public void BatchWarmup_SourceResolutionLeavesWorldAndAssetBytesUnchanged()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var definition = ActualBatchDefinition("S002");
            var preset = SkirmishLegacyProductionIdentity.RequireBinding("S001");
            string definitionPath = UnityEditor.AssetDatabase.GetAssetPath(definition);
            string presetPath = UnityEditor.AssetDatabase.GetAssetPath(preset);
            var definitionBytes = File.ReadAllBytes(definitionPath);
            var presetBytes = File.ReadAllBytes(presetPath);
            SkirmishS002AriaRunHarness.ResolveProductionBatchWarmupSource(definition);
            CollectionAssert.AreEqual(definitionBytes, File.ReadAllBytes(definitionPath));
            CollectionAssert.AreEqual(presetBytes, File.ReadAllBytes(presetPath));
            Assert.AreSame(world, World.DefaultGameObjectInjectionWorld);
        }

        public static void RunBatchWarmupSourceValidation()
        {
            var tests = new SkirmishS002AriaHarnessTests();
            tests.BatchWarmup_ActualCodedNullSourcePreservesNormalBinding("S002", SkirmishPresetConfig.DesertBaseEstablishedScenarioIndex);
            tests.BatchWarmup_ActualCodedNullSourcePreservesNormalBinding("S003", SkirmishPresetConfig.DesertBaseAirMobileFieldScenarioIndex);
            tests.BatchWarmup_ActualS004UsesRuntimeEnvironmentSource();
            tests.BatchWarmup_ActualS004MustNotUseS001Source();
            tests.BatchWarmup_ExplicitSourcePrecedesS004Resource();
            tests.BatchWarmup_ActualExplicitS005SourceUnchanged();
            tests.BatchWarmup_GenericMissingExplicitSourceRefused();
            tests.BatchWarmup_S005MissingExplicitSourceRefused();
            tests.BatchWarmup_CodedForeignMapRefused();
            tests.BatchWarmup_NullDefinitionRefused();
            tests.BatchWarmup_SourceResolutionLeavesWorldAndAssetBytesUnchanged();
            Debug.Log("[ProductionBatchWarmupSourceControls] result=Passed cases=11 actualCodedBindings=3 actualS004Environment=1 explicitS005=1 streaming=0 sourceFitCertified=0 missionReady=0");
        }





        // Synchronous observer characterization. This does not certify live legacy launch/HUD publication.
        private static Newtonsoft.Json.Linq.JObject ObserverWorldState(EntityManager em)
        {
            return (Newtonsoft.Json.Linq.JObject)ObserverMethod("CaptureProductionObserverState").Invoke(null,new object[]{em});
        }
        private static string ObserverHash(Newtonsoft.Json.Linq.JToken value)
        {
            return SkirmishProductionCanaryRequest.Hash(System.Text.Encoding.UTF8.GetBytes(
                value.ToString(Newtonsoft.Json.Formatting.None)));
        }
        private static System.Reflection.MethodInfo ObserverMethod(string name)
        {
            var method=typeof(SkirmishS002AriaRunHarness).GetMethod(name,
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(method,name); return method;
        }
        private static Newtonsoft.Json.Linq.JObject ObserverCase(string name, EntityManager em, Action read)
        {
            var before=ObserverWorldState(em);
            read();
            var after=ObserverWorldState(em);
            Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(before,after),name+" changed ECS/public state");
            return new Newtonsoft.Json.Linq.JObject { ["name"]=name, ["before"]=before,
                ["after"]=after, ["beforeSha256"]=ObserverHash(before), ["afterSha256"]=ObserverHash(after),
                ["assertionsPassed"]=true };
        }
        public static void RunReadOnlyObserverCharacterization()
        {
            Assert.IsFalse(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode,"Observer fixture requires the idle focused boundary.");
            foreach(string name in new[]{"productionCanary","productionBatch"})
            {
                var active=typeof(SkirmishS002AriaRunHarness).GetField(name,
                    System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
                Assert.IsNotNull(active);Assert.IsNull(active.GetValue(null),"Never run the fixture inside an owned normal job.");
            }
            string output=Environment.GetEnvironmentVariable("WARLINE_OBSERVER_PROOF_OUTPUT");
            Assert.IsTrue(!string.IsNullOrEmpty(output)&&Path.IsPathRooted(output));
            Assert.IsFalse(File.Exists(output)||Directory.Exists(output),"Never overwrite observer evidence.");
            Assert.IsTrue(Directory.Exists(Path.GetDirectoryName(output)),"Owned host must prepare output parent.");
            var cases=new Newtonsoft.Json.Linq.JArray();
            using (var world=new World("ReadOnlyObserverCharacterization"))
            {
                var em=world.EntityManager;
                CompileAndSpawn(em,out var session);
                var actor=FirstFactionUnit(em,1,SkirmishRoleKind.Tank);
                var enemy=DesignatedBase(em,2);
                Assert.AreNotEqual(Entity.Null,actor);
                var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                // Explicit nonempty command-buffer sentinels and public boundary are outside read windows.
                var boundary=em.CreateEntity(typeof(AriaPlaySessionComponent),typeof(AriaPlayObservationComponent),
                    typeof(AriaSkirmishObservationOwner),typeof(AriaSkirmishObservationComponent),
                    typeof(AriaSkirmishPlanComponent),typeof(UiShellStateComponent),typeof(UnitAttackOrderQueueComponent),typeof(UiShellRootComponent));
                em.SetComponentData(boundary,new UiShellStateComponent{ActiveRoute=UIRoute.Match});
                em.SetComponentData(boundary,new AriaSkirmishObservationOwner{Boundary=boundary,TransitionSequence=0});
                em.SetComponentData(boundary,new AriaSkirmishObservationComponent{Value=new AriaSkirmishObservation{Active=true}});
                var planSentinel=new AriaSkirmishPlanComponent();
                planSentinel.PrecisionAcceptedGroups.Add(17);
                em.SetComponentData(boundary,planSentinel);
                var groups=(Newtonsoft.Json.Linq.JArray)ObserverMethod("ObserverValue").Invoke(null,new object[]{planSentinel.PrecisionAcceptedGroups});
                Assert.AreEqual(1,groups.Count);Assert.AreEqual(17,(uint)groups[0]);
                var actorPolicy=new CombatTargetPolicy{AllowedTargets=CombatTargetDomain.Structure,Domain=CombatTargetDomain.Ground,Visible=1};
                var targetPolicy=new CombatTargetPolicy{AllowedTargets=CombatTargetDomain.None,Domain=CombatTargetDomain.Structure,Visible=0};
                if(em.HasComponent<CombatTargetPolicy>(actor))em.SetComponentData(actor,actorPolicy);
                else em.AddComponentData(actor,actorPolicy);
                if(em.HasComponent<CombatTargetPolicy>(enemy))em.SetComponentData(enemy,targetPolicy);
                else em.AddComponentData(enemy,targetPolicy);
                em.AddBuffer<UnitAttackOrderRequestElement>(boundary).Add(default);
                em.AddBuffer<UnitAttackOrderResultElement>(boundary).Add(default);
                var targetReader=ObserverMethod("ReadCombatTargetDiagnostic");
                Func<Entity,Newtonsoft.Json.Linq.JObject> target=t=>Newtonsoft.Json.Linq.JObject.Parse(JsonUtility.ToJson(
                    targetReader.Invoke(null,new object[]{em,actor,owner,t})));
                SkirmishFogService.Reveal(em,enemy);
                cases.Add(ObserverCase("expanded_visible",em,()=>{
                    var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                    var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                    var match=new SkirmishMatchState{SessionId=state.SessionId,Phase=SkirmishPhase.Playing};
                    var facts=(Newtonsoft.Json.Linq.JObject)ObserverMethod("CaptureProductionExpandedSnapshot").Invoke(
                        null,new object[]{em,session,match,true,0f,state,setup});
                    Assert.AreEqual(setup.CatalogId,(string)facts["catalog_id"]);
                    Assert.AreEqual(state.SessionId.ToString(),(string)facts["native_session_id"]);
                    Assert.IsTrue((bool)target(enemy)["fogVisible"]);
                    Assert.IsTrue((bool)target(enemy)["policyAllows"]);
                    var observedType=typeof(SkirmishS002AriaRunHarness).GetNestedType("ActorDiagnostic",System.Reflection.BindingFlags.NonPublic);
                    Assert.IsNotNull(observedType);
                    var observed=Activator.CreateInstance(observedType,true);
                    var actorFacts=Newtonsoft.Json.Linq.JObject.Parse(JsonUtility.ToJson(ObserverMethod("ReadActorCombatDiagnostic").Invoke(
                        null,new object[]{em,actor,owner,observed,enemy})));
                    Assert.AreEqual(owner.SessionId.ToString(),(string)actorFacts["sessionId"]);
                    Assert.AreEqual(enemy.Index,(int)actorFacts["designatedEnemyBase"]["index"]);
                }));
                SkirmishFogService.Hide(em,enemy);
                cases.Add(ObserverCase("expanded_hidden",em,()=>{
                    var facts=target(enemy);
                    Assert.IsFalse((bool)facts["fogVisible"]); Assert.IsFalse((bool)facts["policyAllows"]);
                    Assert.IsFalse(SkirmishFogService.IsVisibleToFaction(em,enemy,1));
                }));
                var enemyOwner=em.GetComponentData<SkirmishAttemptOwnedComponent>(enemy);
                var foreign=enemyOwner;foreign.SessionId=new FixedString64Bytes("foreign-observer-session");
                em.SetComponentData(enemy,foreign);
                cases.Add(ObserverCase("foreign_session",em,()=>{
                    var facts=target(enemy);Assert.IsFalse((bool)facts["sessionMatches"]);
                    Assert.IsFalse((bool)facts["policyAllows"]);
                }));
                em.SetComponentData(enemy,enemyOwner);
                cases.Add(ObserverCase("foreign_faction",em,()=>{
                    Assert.IsTrue(SkirmishFogService.IsVisibleToFaction(em,enemy,enemyOwner.FactionId));
                    Assert.IsFalse(SkirmishFogService.IsVisibleToFaction(em,enemy,1));
                }));
                cases.Add(ObserverCase("missing_target",em,()=>{
                    Assert.IsFalse((bool)target(Entity.Null)["exists"]);
                }));
                cases.Add(ObserverCase("legacy_wrong_world_refused",em,()=>{
                    var match=new SkirmishMatchState{SessionId=owner.SessionId,ScenarioIndex=0};
                    Assert.Throws<InvalidDataException>(()=>SkirmishLegacyProductionIdentity.CaptureLive(em,session,in match,"S001"));
                }));
                cases.Add(ObserverCase("public_storage_read",em,()=>{
                    Assert.IsTrue(AriaSkirmishObservationStorage.TryGet(em,boundary,out var storage));
                    Assert.AreEqual(boundary,storage);
                    Assert.IsFalse(em.GetComponentData<AriaSkirmishObservationComponent>(storage).Value.EnemyBase.Available);
                }));

                string writerRoot=FreshProfileFixtureRoot();
                try
                {
                    Assert.IsTrue(Game.UI.Runtime.SkirmishMatchView.NativeCombatEvidenceEnabled,
                        "Set WARLINE_S005_COMBAT_EVIDENCE=1 before Editor startup; never change the readonly flag.");
                    string trace=Path.Combine(writerRoot,"actual-writer");
                    cases.Add(ObserverCase("actual_actor_writer",em,()=>{
                        ObserverMethod("AppendActorDiagnosticToPath").Invoke(null,new object[]{em,0f,enemy,trace});
                    }));
                    string[] actorRows=File.ReadAllLines(trace+".actors.jsonl");
                    using(var actors=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                        typeof(SkirmishUnitRoleComponent),typeof(LocalTransform)))
                        Assert.AreEqual(actors.CalculateEntityCount(),actorRows.Length);
                    int eligible=0;
                    using(var actors=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                        typeof(SkirmishUnitRoleComponent),typeof(LocalTransform)))
                    using(var actorsArray=actors.ToEntityArray(Allocator.Temp))
                    foreach(var unit in actorsArray)
                    {
                        var unitOwner=em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                        if(unitOwner.FactionId!=1||unitOwner.IsStructure!=0||
                            !em.HasComponent<SkirmishRoleOverlayComponent>(unit))continue;
                        var overlay=em.GetComponentData<SkirmishRoleOverlayComponent>(unit);
                        if(overlay.Damage>0&&(overlay.TargetDomains&SkirmishTargetDomain.Structure)!=0)eligible++;
                    }
                    var combatRows=File.Exists(trace+".combat.jsonl")?File.ReadAllLines(trace+".combat.jsonl"):Array.Empty<string>();
                    Assert.AreEqual(Math.Min(eligible,16),combatRows.Length);
                    if(combatRows.Length>0)
                        Assert.AreEqual(eligible>16,(bool)Newtonsoft.Json.Linq.JObject.Parse(combatRows[combatRows.Length-1])["capReached"]);
                    var actorCase=(Newtonsoft.Json.Linq.JObject)cases[cases.Count-1];
                    actorCase["coverage"]=new Newtonsoft.Json.Linq.JObject{
                        ["eligible"]=eligible,["recorded"]=combatRows.Length,["capped"]=eligible>16,
                        ["actorRows"]=actorRows.Length,["actorFileSha256"]=SkirmishProductionCanaryRequest.FileHash(trace+".actors.jsonl")};
                    if(combatRows.Length>0)actorCase["combatFileSha256"]=SkirmishProductionCanaryRequest.FileHash(trace+".combat.jsonl");

                    var previousMirror=Game.UI.Runtime.SkirmishMatchView.NativeEnemyBaseTouchSnapshot;
                    var helperType=typeof(Game.UI.Runtime.SkirmishMatchView).Assembly.GetType("Game.UI.Runtime.MatchHudAssistantUiSystemHelper");
                    var mirror=helperType.GetMethod("RecordEnemyBaseDiagnostic",
                        System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
                    Assert.IsNotNull(mirror);
                    Game.UI.Runtime.SkirmishMatchView hudView=null;
                    var fontField=typeof(Game.UI.Runtime.SkirmishMatchView).GetField("interfaceFont",
                        System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
                    object previousFont=fontField.GetValue(null);
                    try
                    {
                        hudView=Game.UI.Runtime.SkirmishMatchView.Create();
                        var hudField=typeof(Game.UI.Runtime.SkirmishMatchView).GetField("hud",
                            System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                        if(hudField.GetValue(hudView)==null)
                            typeof(Game.UI.Runtime.SkirmishMatchView).GetMethod("Awake",
                                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(hudView,null);
                        Assert.Greater(Screen.width,2);Assert.Greater(Screen.height,2);
                        var publicView=em.GetComponentData<AriaSkirmishObservationComponent>(boundary).Value;
                        publicView.Frame=Time.frameCount;
                        em.SetComponentData(boundary,new AriaSkirmishObservationComponent{Value=publicView});
                        var point=new Vector3(Screen.width*.5f,Screen.height*.5f,1);
                        hudView.PresentEnemyBaseMarker(point,true,new Vector3(100,0,100),Vector3.zero,false);
                        Assert.IsTrue(hudView.EnemyBaseMarkerVisible);Assert.IsFalse(hudView.EnemyBaseTargetVisible);
                        cases.Add(ObserverCase("actual_hidden_hud_diagnostic_mirror",em,()=>{
                            mirror.Invoke(null,new object[]{publicView,hudView});
                            var recorded=Game.UI.Runtime.SkirmishMatchView.NativeEnemyBaseTouchSnapshot;
                            Assert.IsTrue(recorded.collected&&recorded.viewPresent&&recorded.markerVisible);
                            Assert.IsFalse(recorded.targetVisible||recorded.reachabilityEvaluated||recorded.reachable);
                            Assert.IsFalse(em.GetComponentData<AriaSkirmishObservationComponent>(boundary).Value.EnemyBase.Available);
                        }));
                        hudView.PresentEnemyBaseMarker(point,true,new Vector3(100,0,100),Vector3.zero,true);
                        publicView.EnemyBase=new AriaTouchTarget{Id=-20001,Position=hudView.VisibleEnemyBasePoint,Available=false};
                        em.SetComponentData(boundary,new AriaSkirmishObservationComponent{Value=publicView});
                        cases.Add(ObserverCase("actual_visible_hud_diagnostic_mirror",em,()=>{
                            mirror.Invoke(null,new object[]{publicView,hudView});
                            var recorded=Game.UI.Runtime.SkirmishMatchView.NativeEnemyBaseTouchSnapshot;
                            Assert.IsTrue(recorded.markerVisible&&recorded.targetVisible&&recorded.reachabilityEvaluated);
                            Assert.AreEqual(publicView.EnemyBase.Available,recorded.reachable);
                        }));
                        cases.Add(ObserverCase("actual_input_writer",em,()=>{
                            ObserverMethod("AppendInputDiagnosticToPath").Invoke(null,new object[]{em,0f,trace+".input.jsonl"});
                        }));
                        var rows=File.ReadAllLines(trace+".input.jsonl");
                        Assert.AreEqual(1,rows.Length);
                        var inputRow=Newtonsoft.Json.Linq.JObject.Parse(rows[0]);
                        Assert.AreEqual(publicView.Frame,(int)inputRow["frame"]);
                        Assert.IsTrue((bool)inputRow["enemyBaseEvidenceFrameMatches"]);
                        Assert.IsTrue((bool)inputRow["enemyBaseEvidenceTargetMatches"]);
                        Assert.IsFalse((bool)inputRow["enemyBase"]["Available"]);
                        ((Newtonsoft.Json.Linq.JObject)cases[cases.Count-1])["fileSha256"]=SkirmishProductionCanaryRequest.FileHash(trace+".input.jsonl");
                    }
                    finally
                    {
                        if(hudView!=null)UnityEngine.Object.DestroyImmediate(hudView.gameObject);
                        fontField.SetValue(null,previousFont);
                        Game.UI.Runtime.SkirmishMatchView.RecordNativeEnemyBaseTouchEvidence(previousMirror);
                    }
                    var duplicate=em.CreateEntity(typeof(AriaSkirmishObservationComponent),
                        typeof(AriaSkirmishObservationOwner),typeof(AriaSkirmishPlanComponent));
                    try
                    {
                        cases.Add(ObserverCase("ambiguous_input_storage_refused",em,()=>{
                            ObserverMethod("AppendInputDiagnosticToPath").Invoke(null,new object[]{em,0f,trace+".duplicate.input.jsonl"});
                            Assert.IsFalse(File.Exists(trace+".duplicate.input.jsonl"));
                        }));
                    }
                    finally { em.DestroyEntity(duplicate); }
                    using(var empty=new World("ObserverZeroEligible"))
                    {
                        cases.Add(ObserverCase("zero_eligible_actual_writer",empty.EntityManager,()=>{
                            ObserverMethod("AppendActorDiagnosticToPath").Invoke(null,new object[]{empty.EntityManager,0f,Entity.Null,trace+".zero"});
                            Assert.AreEqual(0,new FileInfo(trace+".zero.actors.jsonl").Length);
                            Assert.IsFalse(File.Exists(trace+".zero.combat.jsonl"));
                            ObserverMethod("AppendInputDiagnosticToPath").Invoke(null,new object[]{empty.EntityManager,0f,trace+".zero.input.jsonl"});
                            Assert.IsFalse(File.Exists(trace+".zero.input.jsonl"));
                        }));
                        ((Newtonsoft.Json.Linq.JObject)cases[cases.Count-1])["coverage"]=new Newtonsoft.Json.Linq.JObject{
                            ["eligible"]=0,["recorded"]=0,["capped"]=false,["status"]="NoEligibleActors"};
                    }
                }
                finally { DeleteProfileFixtureRoot(writerRoot); }
            }
            string profile=FreshProfileFixtureRoot(), previous=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",profile);
                var match=new SkirmishMatchState{SessionId=new FixedString64Bytes("observer-legacy-result"),
                    Phase=SkirmishPhase.Finished,ResultSaved=1,Outcome=SkirmishOutcome.Defeat,
                    Reason=SkirmishEndReason.MainBaseDestroyed,Seed=104731,ScenarioIndex=0};
                var data=new QuickGameSaveData{lastResult=new SkirmishResultSaveData{
                    sessionId=match.SessionId.ToString(),seed=match.Seed,scenarioIndex=match.ScenarioIndex,
                    outcome=match.Outcome.ToString(),reason=match.Reason.ToString()}};
                string path=Path.Combine(profile,SaveService.QuickGameFileName);
                File.WriteAllText(path,JsonUtility.ToJson(data));
                byte[] before=File.ReadAllBytes(path);long version=JsonSaveRepository.ChangeVersion;
                string[] entries=Directory.GetFileSystemEntries(profile);
                Assert.IsTrue(SkirmishLegacyProductionIdentity.ReadSavedResultMatches(in match,out var facts));
                CollectionAssert.AreEqual(before,File.ReadAllBytes(path));
                CollectionAssert.AreEqual(entries,Directory.GetFileSystemEntries(profile));
                Assert.AreEqual(version,JsonSaveRepository.ChangeVersion);
                cases.Add(new Newtonsoft.Json.Linq.JObject{["name"]="legacy_saved_result",
                    ["fileBeforeSha256"]=SkirmishProductionCanaryRequest.Hash(before),
                    ["fileAfterSha256"]=SkirmishProductionCanaryRequest.FileHash(path),
                    ["changeVersionBefore"]=version,["changeVersionAfter"]=JsonSaveRepository.ChangeVersion,
                    ["matches"]=(bool)facts["matches"],["assertionsPassed"]=true});
            }
            finally
            {
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",previous);
                DeleteProfileFixtureRoot(profile);
            }
            var assemblies=new Newtonsoft.Json.Linq.JObject();
            foreach(var type in new[]{typeof(SkirmishS002AriaHarnessTests),typeof(SkirmishS002AriaRunHarness),
                typeof(SkirmishFogService),typeof(AriaSkirmishObservationStorage),typeof(Game.UI.Runtime.SkirmishMatchView),
                typeof(MatchSceneView),typeof(AriaSkirmishObservation)})
            {
                string location=type.Assembly.Location;
                Assert.IsTrue(File.Exists(location),"Actual loaded observer assembly file required.");
                assemblies[type.Assembly.GetName().Name]=new Newtonsoft.Json.Linq.JObject{
                    ["path"]=location,["sha256"]=SkirmishProductionCanaryRequest.FileHash(location)};
            }
            var receipt=new Newtonsoft.Json.Linq.JObject{
                ["schema"]="ObserverPurityCharacterization-v1",["status"]="PassedExpandedObserverFixture_LegacyLivePending",
                ["execution"]="UnityCheckedExecuteMethod",["method"]="Game.Tests.Editor.SkirmishS002AriaHarnessTests.RunReadOnlyObserverCharacterization",
                ["createdUtc"]=DateTime.UtcNow.ToString("O"),["loadedAssemblies"]=assemblies,["cases"]=cases,
                ["missingForAll120Admission"]=new Newtonsoft.Json.Linq.JArray("legacy_live_positive","per_attempt_candidate_and_scope_join"),
                ["expandedSharedFixturePassed"]=true,
                ["worldUpdateBetweenSnapshots"]=false,["driverOrCommandDispatchBetweenSnapshots"]=false,
                ["missionReady"]=false,["read_only_observers"]=false};
            File.WriteAllText(output,receipt.ToString(Newtonsoft.Json.Formatting.Indented));
            Debug.Log("[ReadOnlyObserverCharacterization] result=Collected cases=14 expandedSharedFixture=Passed legacyLive=Pending all120Admission=Pending missionReady=0");
        }

        private static System.Type ProfileContextType => typeof(Game.Editor.SkirmishS002AriaRunHarness)
            .GetNestedType("ProductionCanaryContext",System.Reflection.BindingFlags.NonPublic);
        private static System.Reflection.MethodInfo ProfileObservationMethod => ProfileContextType.GetMethod(
            "ObserveProfileJournal",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        private static Newtonsoft.Json.Linq.JObject ActualProfileSnapshot(string root)
        {
            var type=ProfileContextType.GetNestedType("ProfileByteJournal",System.Reflection.BindingFlags.NonPublic);
            var method=type.GetMethod("Snapshot",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (Newtonsoft.Json.Linq.JObject)method.Invoke(null,new object[]{root,"S005",new byte[65536]});
        }
        private static string FreshProfileFixtureRoot()
        {
            string path=Path.Combine(Path.GetTempPath(),"WPJ-"+Guid.NewGuid().ToString("N"));
            Assert.IsFalse(File.Exists(path)||Directory.Exists(path));Directory.CreateDirectory(path);return path;
        }
        private static void DeleteProfileFixtureRoot(string root)
        {
            string full=Path.GetFullPath(root),temporary=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            Assert.IsTrue(full.StartsWith(temporary,StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(Path.GetFileName(full).StartsWith("WPJ-",StringComparison.Ordinal));
            Assert.AreEqual(0,(int)(File.GetAttributes(full)&FileAttributes.ReparsePoint));
            Directory.Delete(full,true);
        }
        // Fixture-only real NTFS points; no symbolic-link privilege or fabricated attributes.
        private sealed class OwnedProfileSocket : IDisposable
        {
            private UIntPtr handle;
            private bool started, opened;
            [System.Runtime.InteropServices.DllImport("Ws2_32.dll", ExactSpelling=true)]
            private static extern int WSAStartup(ushort version, IntPtr data);
            [System.Runtime.InteropServices.DllImport("Ws2_32.dll", ExactSpelling=true)]
            private static extern int WSACleanup();
            [System.Runtime.InteropServices.DllImport("Ws2_32.dll", ExactSpelling=true)]
            private static extern UIntPtr socket(int family,int type,int protocol);
            [System.Runtime.InteropServices.DllImport("Ws2_32.dll", ExactSpelling=true)]
            private static extern int bind(UIntPtr socket,byte[] address,int length);
            [System.Runtime.InteropServices.DllImport("Ws2_32.dll", ExactSpelling=true)]
            private static extern int closesocket(UIntPtr socket);
            [System.Runtime.InteropServices.DllImport("Ws2_32.dll", ExactSpelling=true)]
            private static extern int WSAGetLastError();
            public OwnedProfileSocket(string path)
            {
                byte[] name=System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(path));
                Assert.Less(name.Length,108,"AF_UNIX pathname including terminator must fit108 bytes");
                IntPtr data=System.Runtime.InteropServices.Marshal.AllocHGlobal(512);
                try
                {
                    int status=WSAStartup(0x0202,data);
                    if(status!=0)throw new IOException("WSAStartup="+status);
                    started=true;
                    Assert.AreEqual(0x0202,(int)(ushort)System.Runtime.InteropServices.Marshal.ReadInt16(data));
                    handle=socket(1,1,0);
                    if(handle.ToUInt64()==(IntPtr.Size==8?ulong.MaxValue:uint.MaxValue))
                        throw new IOException("AF_UNIX socket="+WSAGetLastError());
                    opened=true;
                    byte[] address=new byte[110];address[0]=1;
                    Buffer.BlockCopy(name,0,address,2,name.Length);
                    if(bind(handle,address,address.Length)!=0)
                        throw new IOException("AF_UNIX bind="+WSAGetLastError());
                    Assert.AreNotEqual(0,(int)(File.GetAttributes(path)&FileAttributes.ReparsePoint));
                    Assert.AreEqual(0,(int)(File.GetAttributes(path)&FileAttributes.Directory));
                }
                catch{Dispose();throw;}
                finally{System.Runtime.InteropServices.Marshal.FreeHGlobal(data);}
            }
            public void Dispose()
            {
                int close=0,cleanup=0;
                if(opened){opened=false;close=closesocket(handle);}
                if(started){started=false;cleanup=WSACleanup();}
                if(close!=0||cleanup!=0)throw new IOException("Winsock cleanup close="+close+" cleanup="+cleanup);
            }
        }
        private static void CreateOwnedProfileJunction(string link,string target,ref bool cleanupSafe)
        {
            var start=new System.Diagnostics.ProcessStartInfo
            {
                FileName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                    @"WindowsPowerShell\v1.0\powershell.exe"),
                Arguments="-NoProfile -NonInteractive -Command \"$ErrorActionPreference='Stop'; New-Item -ItemType Junction -Path $env:WARLINE_PROFILE_FIXTURE_LINK -Target $env:WARLINE_PROFILE_FIXTURE_TARGET | Out-Null\"",
                UseShellExecute=false,CreateNoWindow=true
            };
            start.EnvironmentVariables["WARLINE_PROFILE_FIXTURE_LINK"]=link;
            start.EnvironmentVariables["WARLINE_PROFILE_FIXTURE_TARGET"]=target;
            using(var process=System.Diagnostics.Process.Start(start))
            {
                Assert.IsNotNull(process);
                if(!process.WaitForExit(20000))
                {
                    // Never race a still-running child with recursive cleanup. Preserve owned evidence instead.
                    cleanupSafe=false;
                    throw new IOException("Owned junction helper timeout; preserve fixture root: "+Path.GetDirectoryName(link));
                }
                Assert.AreEqual(0,process.ExitCode,"Owned junction helper failed; no native reparse coverage");
            }
            Assert.AreNotEqual(0,(int)(File.GetAttributes(link)&FileAttributes.ReparsePoint));
            Assert.AreNotEqual(0,(int)(File.GetAttributes(link)&FileAttributes.Directory));
        }

        public static void RunProfileJournalNativeFilesystemValidation()
        {
            var tests=new SkirmishS002AriaHarnessTests();int cases=0;
            foreach(string kind in new[]{"absent","empty","changed","unknownFile","unknownDirectory","knownDirectory",
                "rootFile","rootReparse","ancestorReparse","fileReparse"})
            {tests.ProfileJournal_ActualSnapshotFilesystem(kind);cases++;}
            tests.ProfileJournal_LockedNativeLogCannotBlockCleanup();cases++;
            tests.ProfileJournal_OutOfMemoryIsNotSwallowed();cases++;
            tests.ProfileJournal_ActualEnvironmentCleanupUsesFinally();cases++;
            UnityEngine.Debug.Log($"[ProfileJournalNativeFilesystemControls] result=Passed cases={cases} profileAdmissionProved=0 missionReady=0");
        }
        [TestCase("absent")]
        [TestCase("empty")]
        [TestCase("changed")]
        [TestCase("unknownFile")]
        [TestCase("unknownDirectory")]
        [TestCase("knownDirectory")]
        [TestCase("rootFile")]
        [TestCase("rootReparse")]
        [TestCase("ancestorReparse")]
        [TestCase("fileReparse")]
        public void ProfileJournal_ActualSnapshotFilesystem(string kind)
        {
            string owned=FreshProfileFixtureRoot(),link=null,sentinel=null;
            bool linkDirectory=false,cleanupSafe=true;OwnedProfileSocket socket=null;
            try
            {
                string root=Path.Combine(owned,"profile");
                if(kind!="absent"&&kind!="rootFile")Directory.CreateDirectory(root);
                if(kind=="rootFile")File.WriteAllText(root,"root-is-a-file");
                if(kind=="unknownFile")File.WriteAllText(Path.Combine(root,"unknown.txt"),"unknown");
                if(kind=="unknownDirectory")Directory.CreateDirectory(Path.Combine(root,"unknown"));
                if(kind=="knownDirectory")Directory.CreateDirectory(Path.Combine(root,"profile.json"));
                if(kind.EndsWith("Reparse",StringComparison.Ordinal))
                {
                    Assert.AreEqual(UnityEngine.RuntimePlatform.WindowsEditor,UnityEngine.Application.platform,
                        "Native reparse controls require Windows; do not turn unavailable coverage into a pass");
                    string target=Path.Combine(owned,"real");Directory.CreateDirectory(target);
                    Directory.CreateDirectory(Path.Combine(target,"nested"));
                    sentinel=Path.Combine(target,"sentinel.txt");File.WriteAllText(sentinel,"owned-target-unchanged");
                    link=kind=="fileReparse"?Path.Combine(root,"profile.json"):Path.Combine(owned,"alias");
                    linkDirectory=kind!="fileReparse";
                    if(linkDirectory)CreateOwnedProfileJunction(link,target,ref cleanupSafe);
                    else socket=new OwnedProfileSocket(link); // bind only; never listen/connect.
                    Assert.AreNotEqual(0,(int)(File.GetAttributes(link)&FileAttributes.ReparsePoint),
                        "Actual native/.NET attribute is required before testing the tag-agnostic rejection");
                    if(kind=="rootReparse")root=link;
                    if(kind=="ancestorReparse")root=Path.Combine(link,"nested");
                }
                if(kind=="absent"||kind=="empty")
                {
                    var value=ActualProfileSnapshot(root);
                    Assert.AreEqual(kind=="empty",(bool)value["directory_exists"]);
                    Assert.AreEqual(0,((Newtonsoft.Json.Linq.JArray)value["directory_entries"]).Count);
                    Assert.AreEqual(16,((Newtonsoft.Json.Linq.JArray)value["files"]).Count);
                    Assert.AreEqual(0L,(long)value["observed_bytes"]);
                    foreach(var row in value["files"])Assert.IsFalse((bool)row["exists"]);
                }
                else if(kind=="changed")
                {
                    byte[] bytes=new byte[131073];for(int i=0;i<bytes.Length;i++)bytes[i]=17;
                    string path=Path.Combine(root,"profile.json");File.WriteAllBytes(path,bytes);
                    var first=ActualProfileSnapshot(root);
                    string expected;
                    using(var hash=System.Security.Cryptography.SHA256.Create())
                        expected=BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
                    Assert.AreEqual(expected,(string)first["files"][0]["sha256"]);
                    bytes[bytes.Length-1]=18;File.WriteAllBytes(path,bytes);
                    var second=ActualProfileSnapshot(root);
                    Assert.AreEqual(131073L,(long)second["observed_bytes"]);
                    Assert.AreNotEqual((string)first["files"][0]["sha256"],(string)second["files"][0]["sha256"],
                        "actual producer reads beyond64KiB and detects last-byte change");
                }
                else
                {
                    var error=Assert.Throws<System.Reflection.TargetInvocationException>(()=>ActualProfileSnapshot(root));
                    Assert.IsInstanceOf<InvalidDataException>(error.InnerException);
                }
            }
            finally
            {
                if(cleanupSafe)
                {
                    try{socket?.Dispose();}
                    finally
                    {
                        if(link!=null)
                        {
                            FileAttributes attributes=0;bool exists=true;
                            try{attributes=File.GetAttributes(link);}
                            catch(FileNotFoundException){exists=false;}
                            catch(DirectoryNotFoundException){exists=false;}
                            if(exists)
                            {
                                Assert.AreNotEqual(0,(int)(attributes&FileAttributes.ReparsePoint),
                                    "Refuse cleanup of an unexpected ordinary object at the exact link path");
                                if(linkDirectory)Directory.Delete(link,false);else File.Delete(link);
                            }
                        }
                        if(sentinel!=null)Assert.AreEqual("owned-target-unchanged",File.ReadAllText(sentinel));
                        DeleteProfileFixtureRoot(owned);
                    }
                }
            }
        }
        [Test]
        public void ProfileJournal_LockedNativeLogCannotBlockCleanup()
        {
            string owned=FreshProfileFixtureRoot();bool observed=false,failure=false,cleanup=false;
            try
            {
                string path=Path.Combine(owned,"native-events.jsonl");File.WriteAllText(path,"preserve");
                using(var locked=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                {
                    try
                    {
                        Assert.DoesNotThrow(()=>ProfileObservationMethod.Invoke(null,new object[]{
                            (Action)(()=>{observed=true;File.AppendAllText(path,"primary");}),
                            (Action<Exception>)(error=>{failure=true;File.AppendAllText(path,"failure");})}));
                    }
                    finally{cleanup=true;}
                }
                Assert.IsTrue(observed&&failure&&cleanup);
                Assert.AreEqual("preserve",File.ReadAllText(path),"neither failed write changes native evidence bytes");
            }
            finally{DeleteProfileFixtureRoot(owned);}
        }
        [Test]
        public void ProfileJournal_OutOfMemoryIsNotSwallowed()
        {
            bool failure=false;
            var error=Assert.Throws<System.Reflection.TargetInvocationException>(()=>ProfileObservationMethod.Invoke(null,new object[]{
                (Action)(()=>throw new OutOfMemoryException("small fixture exception; no memory pressure")),
                (Action<Exception>)(_=>failure=true)}));
            Assert.IsInstanceOf<OutOfMemoryException>(error.InnerException);Assert.IsFalse(failure);
        }
        [Test]
        public void ProfileJournal_ActualEnvironmentCleanupUsesFinally()
        {
            string owned=FreshProfileFixtureRoot();
            string save=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            string background=Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION");
            string sentinel=Guid.NewGuid().ToString("N");
            const string saveKey="Warline.AriaPlayValidation";
            var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
            var harness=typeof(Game.Editor.SkirmishS002AriaRunHarness);
            string backgroundKey=(string)harness.GetField("BackgroundPreviousKey",flags).GetValue(null);
            string previousSave=UnityEditor.SessionState.GetString(saveKey,sentinel);
            string previousBackground=UnityEditor.SessionState.GetString(backgroundKey,sentinel);
            try
            {
                Assert.IsNull(harness.GetField("productionCanary",flags).GetValue(null),"never interfere with owned native context");
                string original=Path.Combine(owned,"original"),isolated=Path.Combine(owned,"isolated");
                Directory.CreateDirectory(original);Directory.CreateDirectory(isolated);
                UnityEditor.SessionState.SetString(saveKey,original);
                UnityEditor.SessionState.SetString(backgroundKey,"original-background");
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",isolated);
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","owned-background");
                var reset=harness.GetMethod("RecordProductionProfileBeforeRestoreAndResetEnvironment",flags);
                Assert.IsNotNull(reset);reset.Invoke(null,null);
                Assert.AreEqual(original,Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"));
                Assert.AreEqual("original-background",Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",save);
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",background);
                if(previousSave==sentinel)UnityEditor.SessionState.EraseString(saveKey);else UnityEditor.SessionState.SetString(saveKey,previousSave);
                if(previousBackground==sentinel)UnityEditor.SessionState.EraseString(backgroundKey);else UnityEditor.SessionState.SetString(backgroundKey,previousBackground);
                DeleteProfileFixtureRoot(owned);
            }
        }

        // Fixture-only service exercise. Never used for normal-input acceptance.
        private static bool FixtureAttackEnemyBase(EntityManager em, Entity session)
        {
            if (!SkirmishExpandedSessionControlService.IsExpanded(em, session) ||
                SkirmishArmySelectionService.SelectedLivingCount(em, session) == 0)
                return false;
            Entity target = FindVisibleEnemyBase(em, session);
            if (target == Entity.Null)
                return false;
            return SkirmishArmyCommandService.TryAttack(em, session, target, out _);
        }

        private static Entity FindVisibleEnemyBase(EntityManager em, Entity session)
        {
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return Entity.Null;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(
                typeof(SkirmishObjectiveRoleComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (!owned.SessionId.Equals(sessionId) || owned.FactionId == 1)
                    continue;
                if (em.GetComponentData<SkirmishObjectiveRoleComponent>(entity).Role !=
                    SkirmishObjectiveRoleKind.EnemyBase)
                    continue;
                if (!SkirmishFogService.IsVisible(em, entity) || !SkirmishArmyGroupSystem.IsAlive(em, entity))
                    continue;
                return entity;
            }

            return Entity.Null;
        }

        [Test]
        public void CandidateHashChangesForSourceEditsRenamesAndDeletesButNotEvidence()
        {
            string root = Path.Combine(Path.GetTempPath(), "warline-candidate-hash-" + Guid.NewGuid().ToString("N"));
            try
            {
                foreach (string directory in new[] { "Assets", "Packages", "ProjectSettings", "Evidence" })
                    Directory.CreateDirectory(Path.Combine(root, directory));
                string source = Path.Combine(root, "Assets", "Rules.cs");
                File.WriteAllText(source, "class Rules { const int Damage = 10; }");
                File.WriteAllText(Path.Combine(root, "Packages", "manifest.json"), "{}");
                string original = SkirmishCandidateSourceHash.Compute(root);
                File.WriteAllText(Path.Combine(root, "Evidence", "run.json"), "new evidence");
                Assert.AreEqual(original, SkirmishCandidateSourceHash.Compute(root));
                File.WriteAllText(source, "class Rules { const int Damage = 20; }");
                string edited = SkirmishCandidateSourceHash.Compute(root);
                Assert.AreNotEqual(original, edited);
                string renamed = Path.Combine(root, "Assets", "ChangedRules.cs");
                File.Move(source, renamed);
                string moved = SkirmishCandidateSourceHash.Compute(root);
                Assert.AreNotEqual(edited, moved);
                File.Delete(renamed);
                Assert.AreNotEqual(moved, SkirmishCandidateSourceHash.Compute(root));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test]
        public void ExpandedMissionSuppressesSelectedLegacyRoster()
        {
            using var world = new World(nameof(ExpandedMissionSuppressesSelectedLegacyRoster));
            EntityManager em = world.EntityManager;
            Entity session = em.CreateEntity(typeof(SkirmishMatchState), typeof(SkirmishExpandedSessionComponent));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent { IsLegacy = 0 });
            Entity legacyRoster = em.CreateEntity(typeof(InitialUnitsSpawnConfig),
                typeof(CustomGameStartupStateComponent), typeof(InitialUsableFuelStorageSeedPending));

            SkirmishWorldSetup.SuppressUnselectedStartupConfigs(em);

            Assert.IsFalse(em.HasComponent<InitialUnitsSpawnConfig>(legacyRoster));
            Assert.IsFalse(em.HasComponent<InitialUsableFuelStorageSeedPending>(legacyRoster));
            Assert.IsTrue(em.HasComponent<CustomGameStartupStateComponent>(legacyRoster));
        }

        [Test]
        public void SpawnedForceMemberKeepsBakedPrefabComponents()
        {
            using var world = new World(nameof(SpawnedForceMemberKeepsBakedPrefabComponents));
            EntityManager em = world.EntityManager;
            Entity prefab = em.CreateEntity(typeof(Prefab), typeof(UnitSourcePrefabKey),
                typeof(UnitMove), typeof(UnitFootprint));
            em.SetComponentData(prefab, new UnitSourcePrefabKey
            {
                Value = new FixedString64Bytes("Unit_Chr_Soldier_Male_02_Alt_04")
            });
            em.SetComponentData(prefab, new UnitMove { Speed = 4f });
            Entity registry = em.CreateEntity(typeof(UnitPrefabRegistryTag));
            em.AddBuffer<UnitPrefabRegistryEntry>(registry).Add(new UnitPrefabRegistryEntry { Prefab = prefab });
            var force = new SkirmishResolvedForceEntry
            {
                RoleId = "rifle",
                RoleKind = SkirmishRoleKind.Rifle,
                FactionId = 1,
                SpawnWorldX = 949f,
                SpawnWorldZ = 345f
            };
            Entity member = SkirmishScenarioSpawnSystem.CreateForceMember(em,
                new FixedString64Bytes("s002-test"), force, 0, 0, 1,
                "Unit_Chr_Soldier_Male_02_Alt_04");
            Assert.AreNotEqual(prefab, member);
            Assert.IsTrue(em.HasComponent<UnitMove>(member));
            Assert.IsTrue(em.HasComponent<UnitFootprint>(member));
            Assert.AreEqual(1, em.GetComponentData<SkirmishVisualSpawnedComponent>(member).FromRegistry);
            Assert.AreEqual(949f, em.GetComponentData<LocalTransform>(member).Position.x, 0.01f);
        }

        [Test]
        public void SpawnedVehiclesAvoidBlockedAndOccupiedCells()
        {
            using var world = new World(nameof(SpawnedVehiclesAvoidBlockedAndOccupiedCells));
            EntityManager em = world.EntityManager;
            const int width = 64;
            const int count = width * width;
            var blocked = new NativeBitArray(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            var occupied = new NativeBitArray(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            try
            {
                blocked.Set(22 + 22 * width, true);
                Entity gridEntity = em.CreateEntity(typeof(GridConfig),
                    typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
                em.SetComponentData(gridEntity, new GridConfig
                {
                    Width = width, Height = width, CellSize = 1f, Origin = float3.zero
                });
                em.SetComponentData(gridEntity, new DynamicBlockerComponent
                {
                    GridSize = count, Blocked = blocked
                });
                em.SetComponentData(gridEntity, new DynamicOccupancyComponent
                {
                    GridSize = count, Occupied = occupied
                });
                DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
                walkable.ResizeUninitialized(count);
                for (int i = 0; i < count; i++) walkable[i] = new GridWalkable { Value = 1 };

                Entity prefab = em.CreateEntity(typeof(Prefab), typeof(UnitSourcePrefabKey),
                    typeof(UnitMove), typeof(UnitFootprint));
                em.SetComponentData(prefab, new UnitSourcePrefabKey { Value = new FixedString64Bytes("test.tank") });
                em.SetComponentData(prefab, new UnitMove { Speed = 8f });
                em.SetComponentData(prefab, new UnitFootprint { Size = new int2(3, 3) });
                Entity registry = em.CreateEntity(typeof(UnitPrefabRegistryTag));
                em.AddBuffer<UnitPrefabRegistryEntry>(registry).Add(new UnitPrefabRegistryEntry { Prefab = prefab });
                var force = new SkirmishResolvedForceEntry
                {
                    RoleId = "tank", RoleKind = SkirmishRoleKind.Tank,
                    FactionId = 1, SpawnWorldX = 22f, SpawnWorldZ = 22f
                };
                Entity first = SkirmishScenarioSpawnSystem.CreateForceMember(em,
                    new FixedString64Bytes("s002-test"), force, 0, 0, 1, "test.tank");
                Entity second = SkirmishScenarioSpawnSystem.CreateForceMember(em,
                    new FixedString64Bytes("s002-test"), force, 1, 0, 1, "test.tank");
                int2 a = em.GetComponentData<UnitGrid>(first).Cell;
                int2 b = em.GetComponentData<UnitGrid>(second).Cell;
                Assert.IsFalse(blocked.IsSet(a.x + a.y * width));
                Assert.IsFalse(blocked.IsSet(b.x + b.y * width));
                Assert.IsTrue(math.abs(a.x - b.x) >= 3 || math.abs(a.y - b.y) >= 3);
            }
            finally
            {
                blocked.Dispose();
                occupied.Dispose();
            }
        }

        [Test]
        public void CheckedInRunsCsvHasExpectedHeader()
        {
            string path = Path.Combine(ProjectRoot(), SkirmishAcceptanceScaffold.RelativeRunsPath);
            string[] lines = File.ReadAllLines(path);
            Assert.GreaterOrEqual(lines.Length, 1);
            Assert.AreEqual(SkirmishAcceptanceScaffold.RequiredHeader, lines[0]);
        }

        [Test]
        public void ForcedVictoryDoesNotAppend()
        {
            string path = TempCsv();
            try
            {
                File.WriteAllText(path, SkirmishAcceptanceScaffold.RequiredHeader + "\n");
                var facts = Finished(SkirmishS002AriaRunLog.ResultVictory);
                facts.ForcedVictory = true;
                Assert.IsFalse(SkirmishS002AriaRunLog.TryAppendTerminalRow(path, in facts, out string row, out string error));
                Assert.IsNull(row);
                Assert.IsTrue(error.IndexOf("Forced Victory", StringComparison.Ordinal) >= 0);
                Assert.AreEqual(1, File.ReadAllLines(path).Length);
                Assert.IsFalse(SkirmishS002AriaRunLog.IsCountedAriaWin(in facts, SkirmishS002AriaRunLog.ResultVictory));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void UnfinishedOutcomeIsRefusedUntilTheMatchEnds()
        {
            string path = TempCsv();
            try
            {
                var unfinished = Finished(SkirmishS002AriaRunLog.ResultVictory);
                unfinished.MatchFinished = false;
                unfinished.Aborted = false;
                Assert.IsFalse(SkirmishS002AriaRunLog.TryAcceptTerminal(in unfinished, out _, out string error));
                Assert.IsTrue(error.IndexOf("has not finished", StringComparison.Ordinal) >= 0);
                Assert.IsFalse(SkirmishS002AriaRunLog.TryAppendTerminalRow(path, in unfinished, out _, out _));
                Assert.IsFalse(File.Exists(path));

                var aborted = unfinished;
                aborted.Aborted = true;
                aborted.RunId = "S002-aria-rs-104731-en-1";
                Assert.IsTrue(SkirmishS002AriaRunLog.TryAppendTerminalRow(path, in aborted, out string row, out _));
                Assert.AreEqual(SkirmishS002AriaRunLog.ResultAbort, Field(row, 13));
                Assert.IsFalse(row.IndexOf("," + SkirmishS002AriaRunLog.ResultVictory + ",", StringComparison.Ordinal) >= 0);
                Assert.IsFalse(SkirmishS002AriaRunLog.IsCountedAriaWin(in aborted, SkirmishS002AriaRunLog.ResultAbort));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void ExistingAntiAirPreservesEstablishedAirBudget()
        {
            var view = new AriaSkirmishObservation { ExpandedSession = true, AirProfile = true,
                OwnDefenseTowers = 2, Infantry = 20, Time = 10, AntiAirCommitted = true,
                VisibleHostileAir = 1, CanAffordAntiAir = true, PadPresent = true, ReadinessEligible = true,
                AirQueueOffered = true,
                RecruitAntiAir = new AriaTouchTarget { Id = 1, Available = true },
                RecruitAir = new AriaTouchTarget { Id = 2, Available = true } };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(2, output.TargetId, "A living or queued AA unit covers the known air contact; invest in the offered offensive aircraft.");
        }

        [Test]
        public void EstablishedDefensesDoNotOpenAnotherConstructionSequence()
        {
            var view = new AriaSkirmishObservation { ExpandedSession = true, AirProfile = true,
                OwnDefenseTowers = 2, Infantry = 20, Time = 10,
                DefenseBuild = new AriaTouchTarget { Id = 1, Available = true },
                FocusPlayer = new AriaTouchTarget { Id = 2, Available = true } };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(4, plan.DefenseStage);
            Assert.AreNotEqual(AriaSkirmishIntent.BuildDefense, plan.Intent);
        }

        [Test]
        public void S004RunIdsUseTheirOwnSeedSequenceAndRetainAttempts()
        {
            string csv = SkirmishAcceptanceScaffold.RequiredHeader + "\n";
            foreach (int seed in new[] { 104733, 130367, 155925 })
            {
                Assert.IsTrue(SkirmishS002AriaRunLog.TryNextAriaRunId(csv, "S004", seed, "en", out string id, out _));
                Assert.AreEqual("S004-aria-rs-" + seed + "-en-1", id);
                Assert.IsTrue(SkirmishS002AriaRunLog.TryNextAriaRunId(csv + id + ",rest\n", "S004", seed, "en", out string next, out _));
                Assert.AreEqual("S004-aria-rs-" + seed + "-en-2", next);
            }
            Assert.IsFalse(SkirmishS002AriaRunLog.TryNextAriaRunId(csv, "S004", 104731, "en", out _, out _));
        }

        [Test]
        public void FinishedOutcomesAppendAndOnlyUnguidedVictoryCounts()
        {
            string path = TempCsv();
            try
            {
                Assert.IsTrue(SkirmishS002AriaRunLog.TryNextAriaRunId(
                    SkirmishAcceptanceScaffold.RequiredHeader + "\n",
                    104731,
                    GameLocalization.EnglishLocaleCode,
                    out string first,
                    out _));
                Assert.AreEqual("S002-aria-rs-104731-en-1", first);

                AppendFinished(path, "S002-aria-rs-104731-en-1", SkirmishS002AriaRunLog.ResultDefeat, true, 0, 0, false);
                AppendFinished(path, "S002-aria-rs-104731-en-2", SkirmishS002AriaRunLog.ResultDraw, true, 0, 0, false);
                AppendFinished(path, "S002-row-slow", SkirmishS002AriaRunLog.ResultVictory, false, 0, 0, false);
                AppendFinished(path, "S002-row-violation", SkirmishS002AriaRunLog.ResultVictory, true, 1, 0, false);
                AppendFinished(path, "S002-row-intervention", SkirmishS002AriaRunLog.ResultVictory, true, 0, 2, false);
                AppendFinished(path, "S002-aria-rs-130365-fa-1", SkirmishS002AriaRunLog.ResultVictory, true, 0, 0, true);

                string[] lines = File.ReadAllLines(path);
                Assert.AreEqual(7, lines.Length);
                Assert.AreEqual(SkirmishS002AriaRunLog.ResultDefeat, Field(lines[1], 13));
                Assert.AreEqual(SkirmishS002AriaRunLog.ResultDraw, Field(lines[2], 13));
                Assert.AreEqual("0", Field(lines[3], 11));
                Assert.AreEqual(SkirmishS002AriaRunLog.ResultVictory, Field(lines[6], 13));
                Assert.AreEqual("1", Field(lines[6], 11));
                Assert.AreEqual("0", Field(lines[6], 16));
                Assert.AreEqual("0", Field(lines[6], 17));

                string used = File.ReadAllText(path);
                Assert.IsTrue(SkirmishS002AriaRunLog.TryNextAriaRunId(
                    used, 104731, GameLocalization.EnglishLocaleCode, out string next, out _));
                Assert.AreEqual("S002-aria-rs-104731-en-3", next);

                string filled = used + "S002-aria-rs-104731-en-3,rest\n";
                Assert.IsFalse(SkirmishS002AriaRunLog.TryNextAriaRunId(
                    filled, 104731, GameLocalization.EnglishLocaleCode, out _, out string full));
                Assert.IsTrue(full.IndexOf("three", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void AttackOrdersDamageOnlyLegalVisibleTargets()
        {
            using var world = new World(nameof(AttackOrdersDamageOnlyLegalVisibleTargets));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session);
            int materials = SkirmishMaterialsService.Read(em, session, 1);
            Entity barracks = DesignatedBase(em, 2);
            Entity rifle = FirstGroupMember(em, FirstFactionGroup(em, session, 1, SkirmishRoleKind.Rifle).GroupId);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Entity enemyTank = FirstFactionUnit(em, 2, SkirmishRoleKind.Tank);
            Place(em, barracks, new float3(0f, 0f, 0f));
            Place(em, rifle, new float3(10f, 0f, 0f));
            RevealAll(em);
            Assert.IsTrue(SkirmishArmyCommandService.TryIssueGroupOrder(
                em, session, GroupOf(em, rifle), 1, SkirmishGroupOrderKind.Attack, barracks, out _));
            for (int i = 0; i < 3; i++)
                SkirmishExpandedEngagementService.Step(em, session, 1f, false);
            Assert.AreEqual(800, em.GetComponentData<UnitHealth>(barracks).Current);

            Place(em, tank, new float3(10f, 0f, 0f));
            Assert.IsTrue(SkirmishArmyCommandService.TryIssueGroupOrder(
                em, session, GroupOf(em, tank), 1, SkirmishGroupOrderKind.Attack, barracks, out _));
            Assert.Greater(SkirmishExpandedEngagementService.Step(em, session, 1f, false), 0);
            Assert.AreEqual(772, em.GetComponentData<UnitHealth>(barracks).Current);

            var reset = em.GetComponentData<UnitHealth>(enemyTank);
            int before = reset.Current;
            Place(em, tank, new float3(0f, 0f, 0f));
            Place(em, enemyTank, new float3(8f, 0f, 0f));
            SkirmishFogService.Reveal(em, enemyTank);
            Assert.IsTrue(SkirmishArmyCommandService.TryIssueGroupOrder(
                em, session, GroupOf(em, tank), 1, SkirmishGroupOrderKind.Attack, enemyTank, out _));
            SkirmishFogService.Hide(em, enemyTank);
            SkirmishExpandedEngagementService.Step(em, session, 1f, false);
            Assert.AreEqual(before, em.GetComponentData<UnitHealth>(enemyTank).Current);
            Assert.AreEqual(materials, SkirmishMaterialsService.Read(em, session, 1));
        }

        [Test]
        public void AssaultColumnKillsTheEnemyTankAndLeavesThePlayerBarracks()
        {
            using var world = new World(nameof(AssaultColumnKillsTheEnemyTankAndLeavesThePlayerBarracks));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session);
            int materials = SkirmishMaterialsService.Read(em, session, 1);
            Entity playerBase = DesignatedBase(em, 1);
            Entity enemyBase = DesignatedBase(em, 2);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Entity apc = FirstApc(em, 1);
            Entity car = FirstFactionUnit(em, 1, SkirmishRoleKind.Car);
            Entity enemyTank = FirstFactionUnit(em, 2, SkirmishRoleKind.Tank);
            Place(em, playerBase, new float3(-228f, 0f, 0f));
            Place(em, enemyBase, new float3(228f, 0f, 0f));
            Place(em, tank, new float3(-168f, 0f, 0f));
            Place(em, apc, new float3(-168f, 0f, 4f));
            Place(em, car, new float3(-168f, 0f, -4f));
            Place(em, enemyTank, new float3(168f, 0f, 0f));
            RevealAll(em);
            Assert.IsFalse(SkirmishFogService.IsVisibleToFaction(em, playerBase, 2),
                "Player-only Reveal must not grant enemy precision targeting.");
            Assert.IsFalse(SkirmishArmyCommandService.TryIssueGroupOrder(
                em, session, GroupOf(em, enemyTank), 2,
                SkirmishGroupOrderKind.Attack, playerBase, out var unknownAttack));
            Assert.AreEqual(SkirmishReasonCode.HiddenContact, unknownAttack.Reason);
            Assert.AreEqual(materials, SkirmishMaterialsService.Read(em, session, 1));
            ObserveEnemyFixtureThreats(world, session);
            RevealAll(em);

            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, tank), false, out _));
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, apc), true, out _));
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, car), true, out _));
            Assert.IsTrue(SkirmishArmyCommandService.TryAttack(em, session, enemyBase, out _));
            Assert.IsTrue(SkirmishArmyCommandService.TryIssueGroupOrder(
                em,
                session,
                GroupOf(em, enemyTank),
                2,
                SkirmishGroupOrderKind.Attack,
                playerBase,
                out _));

            for (int step = 0; step < 180; step++)
            {
                SkirmishExpandedEngagementService.Step(em, session, 1f, false);
                SkirmishWorldMovementService.Step(em, session, 1f, false);
            }

            Assert.AreEqual(0, em.GetComponentData<UnitHealth>(enemyTank).Current);
            Assert.AreEqual(800, em.GetComponentData<UnitHealth>(playerBase).Current);
            Assert.Less(em.GetComponentData<UnitHealth>(enemyBase).Current, 800);
            Assert.Greater(em.GetComponentData<UnitHealth>(tank).Current, 0);
            Assert.AreEqual(materials, SkirmishMaterialsService.Read(em, session, 1));
        }

        [Test]
        public void ExpandedAssaultPlanUsesVisibleCardsWithoutMutatingStocks()
        {
            using var world = new World(nameof(ExpandedAssaultPlanUsesVisibleCardsWithoutMutatingStocks));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session);
            int materials = SkirmishMaterialsService.Read(em, session, 1);
            var view = new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                Infantry = 20,
                ExpandedCanNextPage = true,
                ExpandedPageIndex = 0,
                Squad0 = new AriaTouchTarget { Id = 10, Available = true },
                Squad3 = new AriaTouchTarget { Id = 13, Available = true },
                Squad4 = new AriaTouchTarget { Id = 104, Available = true },
                NextSquadPage = new AriaTouchTarget { Id = 200, Available = true },
                Attack = new AriaTouchTarget { Id = 42, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
            Assert.AreEqual(200, output.TargetId);
            Assert.AreEqual(1, plan.PagedToAssault);
            Assert.AreEqual(0, plan.AssaultIssued);

            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0, plan.AssaultIssued);
            Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
            Assert.AreEqual(AriaPlayObservationKind.Control, output.Kind);
            Assert.AreEqual(200, output.TargetId, "page action remains pending until PageIndex changes");

            view.ExpandedPageIndex = 1;
            view.NextSquadPage = default;
            view.ExpandedAssaultMask = 1 << 3;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
            Assert.AreEqual(13, output.TargetId);
            Assert.AreEqual(1, plan.AssaultSelecting);

            view.ExpandedSelectedMask = 1 << 3;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Attack, plan.Intent);
            Assert.AreEqual(42, output.TargetId);
            // A tap is not the confirmed order; ARIA waits for the public
            // card state and then continues through the remaining pages.
            Assert.AreEqual(0, plan.AssaultIssued);
            Assert.AreEqual(materials, SkirmishMaterialsService.Read(em, session, 1));
        }

        [Test]
        public void ExpandedAssaultDoesNotAttackWithRiflesWhileNextPageExists()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                Infantry = 20,
                SelectionVisible = true,
                ExpandedCanNextPage = true,
                ExpandedPageIndex = 0,
                ExpandedAssaultMask = 0,
                Squad0 = new AriaTouchTarget { Id = 10, Available = true },
                Squad4 = new AriaTouchTarget { Id = 104, Available = true },
                NextSquadPage = new AriaTouchTarget { Id = 200, Available = false },
                Attack = new AriaTouchTarget { Id = 42, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0, plan.AssaultIssued);
            Assert.AreEqual(AriaSkirmishIntent.Inspect, plan.Intent);
            Assert.AreEqual(AriaPlayObservationKind.Waiting, output.Kind);
            Assert.AreEqual(0, output.TargetId);
        }

        [Test]
        public void ExpandedAssaultStartsImmediatelyAndDoesNotBlockOnFlatHealth()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                Infantry = 20,
                Time = 0f,
                EnemyHealth = 800f,
                PlayerHealth = 800f,
                ForceHealth = 20f,
                ExpandedCanNextPage = true,
                ExpandedPageIndex = 0,
                Squad4 = new AriaTouchTarget { Id = 104, Available = true },
                NextSquadPage = new AriaTouchTarget { Id = 14, Available = true },
                Attack = new AriaTouchTarget { Id = 42, Available = true },
                Squad0 = new AriaTouchTarget { Id = 10, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0f, plan.OpeningUntil);
            Assert.AreEqual(0, plan.AssaultStarted);
            Assert.AreEqual(AriaPlayPhase.Observing, touch.Phase);
            Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
            Assert.AreEqual(14, output.TargetId);

            view.Time = 200f;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreNotEqual(AriaPlayPhase.Blocked, touch.Phase);
            Assert.AreEqual(0f, plan.OpeningUntil);
            Assert.AreEqual(0, plan.AssaultStarted);
            Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
            Assert.AreEqual(14, output.TargetId);

            view.ExpandedPageIndex = 1;
            view.ExpandedStructureMask = 1 << 3;
            view.ExpandedAssaultMask = (1 << 0) | (1 << 3);
            view.ExpandedSelectedMask = 1 << 0;
            view.SelectionVisible = true;
            view.Squad3 = new AriaTouchTarget { Id = 13, Available = false };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreNotEqual(AriaSkirmishIntent.Attack, plan.Intent);
            Assert.AreNotEqual(42, output.TargetId);

            view.ExpandedSelectedMask = (1 << 0) | (1 << 3);
            view.Squad3 = new AriaTouchTarget { Id = 13, Available = true };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Attack, plan.Intent);
            Assert.AreEqual(42, output.TargetId);
            Assert.AreEqual(0, plan.AssaultIssued);
        }

        [Test]
        public void BlockedExpandedPlanRequiresExplicitRetry()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                EnemyDesignatedAlive = true,
                PlayerDesignatedAlive = true,
                Time = 400f,
                ExpandedCanNextPage = true,
                Squad4 = new AriaTouchTarget { Id = 104, Available = true },
                NextSquadPage = new AriaTouchTarget { Id = 14, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent
            {
                Phase = AriaPlayPhase.Blocked,
                Attempts = 3
            };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaPlayPhase.Blocked, touch.Phase);
            Assert.AreEqual(3, touch.Attempts);
            Assert.AreEqual(0, touch.GestureRequested);
            Assert.AreEqual(0, output.TargetId);
            Assert.AreEqual(0f, touch.LastProgressAt);
            Assert.AreEqual(0f, touch.LastObjectiveProgressAt);
        }

        [Test]
        public void GridlessFixtureClearsFollowerBeforeLocalStep()
        {
            using var world = new World(nameof(GridlessFixtureClearsFollowerBeforeLocalStep));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Place(em, tank, new float3(0f, 0f, 0f));
            em.AddComponentData(tank, new UnitMove { Speed = 8f, ArriveDistance = 0.35f });
            em.AddComponentData(tank, new UnitPathRequest { Goal = new int2(4, 0) });
            SkirmishWorldMovementService.AssignIntent(em, tank, new float3(80f, 0f, 0f), SkirmishGroupOrderKind.Move);
            Assert.IsFalse(em.HasComponent<UnitPathFollow>(tank));
            float before = em.GetComponentData<LocalTransform>(tank).Position.x;
            Assert.Greater(SkirmishWorldMovementService.Step(em, session, 1f, false), 0);
            Assert.Greater(em.GetComponentData<LocalTransform>(tank).Position.x, before + 1f);

            em.AddComponentData(tank, new UnitPathFollow { PathIndex = 0 });
            float held = em.GetComponentData<LocalTransform>(tank).Position.x;
            Assert.Greater(SkirmishWorldMovementService.Step(em, session, 1f, false), 0);
            Assert.Greater(em.GetComponentData<LocalTransform>(tank).Position.x, held + 1f);
            Assert.IsFalse(em.HasComponent<UnitPathFollow>(tank), "A gridless fixture must not keep a second movement owner.");
        }

        [Test]
        public void StructureInRangeIsDamagedWhileACombatantIsAlsoInRange()
        {
            using var world = new World(nameof(StructureInRangeIsDamagedWhileACombatantIsAlsoInRange));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session);
            Entity barracks = DesignatedBase(em, 2);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Entity enemyCar = FirstFactionUnit(em, 2, SkirmishRoleKind.Car);
            Place(em, barracks, new float3(10f, 0f, 0f));
            Place(em, tank, new float3(0f, 0f, 0f));
            Place(em, enemyCar, new float3(8f, 0f, 0f));
            RevealAll(em);
            int carHealth = em.GetComponentData<UnitHealth>(enemyCar).Current;
            Assert.IsTrue(SkirmishArmyCommandService.TryIssueGroupOrder(
                em, session, GroupOf(em, tank), 1, SkirmishGroupOrderKind.Attack, barracks, out _));
            Assert.Greater(SkirmishExpandedEngagementService.Step(em, session, 1f, false), 0);
            Assert.AreEqual(772, em.GetComponentData<UnitHealth>(barracks).Current);
            Assert.AreEqual(carHealth, em.GetComponentData<UnitHealth>(enemyCar).Current);
            Assert.AreEqual(0, em.GetComponentData<SkirmishMoveIntentComponent>(tank).Engaged);
        }

        [Test]
        public void DeadlineDrawPublishesFinishedMatchWithoutStampingVictory()
        {
            using var world = new World(nameof(DeadlineDrawPublishesFinishedMatchWithoutStampingVictory));
            BootPlayingSession(world, out Entity session);
            EntityManager em = world.EntityManager;
            Assert.Greater(em.GetComponentData<SkirmishResolvedSetupComponent>(session).DeadlineSeconds, 0);
            var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            clock.DeadlineSeconds = 0;
            clock.ElapsedSeconds = 1080f;
            clock.Playing = 1;
            clock.Paused = 0;
            em.SetComponentData(session, clock);
            Assert.IsFalse(em.HasComponent<SkirmishResultComponent>(session));
            PublishProjectedMatch(world, session);

            Assert.AreEqual(1, em.GetComponentData<SkirmishResultComponent>(session).Frozen);
            clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            Assert.AreEqual(1080, clock.DeadlineSeconds);
            SkirmishObjectiveStateComponent objective = em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            Assert.AreEqual(1, objective.Terminal);
            Assert.AreEqual(SkirmishOutcomeKind.Draw, objective.Outcome);
            Assert.AreEqual(SkirmishEndReasonKind.TimeLimit, objective.Reason);
            Assert.AreEqual(SkirmishSessionPhase.Finished, em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase);
            SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
            Assert.AreEqual(SkirmishPhase.Finished, match.Phase);
            Assert.AreEqual(SkirmishOutcome.Draw, match.Outcome);
            Assert.AreEqual(SkirmishEndReason.TimeLimit, match.Reason);
            CheckedInRunsCsvHasExpectedHeader();
        }

        [Test]
        public void DeadEnemyBarracksFinishesVictoryWithoutRulesSystem()
        {
            using var world = new World(nameof(DeadEnemyBarracksFinishesVictoryWithoutRulesSystem));
            BootPlayingSession(world, out Entity session);
            EntityManager em = world.EntityManager;
            Entity enemyBase = DesignatedBase(em, 2);
            Entity playerBase = DesignatedBase(em, 1);
            var health = em.GetComponentData<UnitHealth>(enemyBase);
            health.Current = 0;
            em.SetComponentData(enemyBase, health);
            Assert.IsFalse(em.HasComponent<SkirmishResultComponent>(session));
            Assert.AreEqual(SkirmishPhase.Playing, em.GetComponentData<SkirmishMatchState>(session).Phase);

            PublishProjectedMatch(world, session);

            Assert.Greater(em.GetComponentData<UnitHealth>(playerBase).Current, 0);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, em.GetComponentData<SkirmishObjectiveStateComponent>(session).Outcome);
            Assert.AreEqual(SkirmishEndReasonKind.MainBaseDestroyed, em.GetComponentData<SkirmishObjectiveStateComponent>(session).Reason);
            SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
            Assert.AreEqual(SkirmishPhase.Finished, match.Phase);
            Assert.AreEqual(SkirmishOutcome.Victory, match.Outcome);
            Assert.AreEqual(SkirmishEndReason.MainBaseDestroyed, match.Reason);
            CheckedInRunsCsvHasExpectedHeader();
        }

        [Test]
        public void AssaultColumnDestroysEnemyBarracksAndPublishesVictory()
        {
            using var world = new World(nameof(AssaultColumnDestroysEnemyBarracksAndPublishesVictory));
            BootPlayingSession(world, out Entity session);
            EntityManager em = world.EntityManager;
            PlaceOnAuthoredPads(em, session);
            int materials = SkirmishMaterialsService.Read(em, session, 1);
            int enemyMaterials = SkirmishMaterialsService.Read(em, session, 2);
            Entity playerBase = DesignatedBase(em, 1);
            Entity enemyBase = DesignatedBase(em, 2);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Entity rocketeer = FirstFactionUnit(em, 1, SkirmishRoleKind.Rocketeer);
            Entity car = FirstFactionUnit(em, 1, SkirmishRoleKind.Car);
            Entity apc = FirstApc(em, 1);
            Assert.Greater(
                em.GetComponentData<LocalTransform>(enemyBase).Position.x -
                em.GetComponentData<LocalTransform>(tank).Position.x,
                100f);
            ObserveEnemyFixtureThreats(world, session);
            RevealAll(em);

            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, tank), false, out _));
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, apc), true, out _));
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, car), true, out _));
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, GroupOf(em, rocketeer), true, out _));
            Assert.IsTrue(SkirmishArmyCommandService.TryAttack(em, session, enemyBase, out _));

            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            for (int step = 0; step < 180; step++)
            {
                SkirmishEnemyStrategySystem.Evaluate(em, session, owned, authored.ArmyGround);
                SkirmishExpandedEngagementService.Step(em, session, 1f, false);
                SkirmishWorldMovementService.Step(em, session, 1f, false);
            }

            Assert.AreEqual(0, em.GetComponentData<UnitHealth>(enemyBase).Current);
            Assert.Greater(em.GetComponentData<UnitHealth>(playerBase).Current, 0);
            Assert.AreEqual(materials, SkirmishMaterialsService.Read(em, session, 1));
            Assert.AreEqual(enemyMaterials - 120, SkirmishMaterialsService.Read(em, session, 2));
            Assert.AreNotEqual(
                SkirmishGroupOrderKind.Attack,
                FirstFactionGroup(em, session, 2, SkirmishRoleKind.Rifle).LastOrder);

            PublishProjectedMatch(world, session);
            SkirmishObjectiveStateComponent objective = em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, objective.Outcome);
            Assert.AreEqual(SkirmishEndReasonKind.MainBaseDestroyed, objective.Reason);
            SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
            Assert.AreEqual(SkirmishPhase.Finished, match.Phase);
            Assert.AreEqual(SkirmishOutcome.Victory, match.Outcome);
            Assert.AreEqual(SkirmishEndReason.MainBaseDestroyed, match.Reason);
            CheckedInRunsCsvHasExpectedHeader();
        }

        [Test]
        public void StructureColumnWaitsForAttackOrderBeforeLatchingAssault()
        {
            var view = new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                EnemyDesignatedAlive = true,
                ExpandedCanNextPage = true,
                ExpandedPageCount = 2,
                ExpandedPageIndex = 0,
                Squad4 = new AriaTouchTarget { Id = 104, Available = true },
                NextSquadPage = new AriaTouchTarget { Id = 200, Available = true },
                Attack = new AriaTouchTarget { Id = 42, Available = true }
            };
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
            Assert.AreEqual(200, output.TargetId);
            Assert.AreEqual(0, plan.AssaultIssued);

            view.ExpandedPageIndex = 1;
            view.ExpandedStructureMask = (1 << 0) | (1 << 3);
            view.ExpandedAssaultMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3) | (1 << 4);
            view.Squad0 = new AriaTouchTarget { Id = 100, Available = true };
            view.Squad1 = new AriaTouchTarget { Id = 101, Available = true };
            view.Squad2 = new AriaTouchTarget { Id = 102, Available = true };
            view.Squad3 = new AriaTouchTarget { Id = 103, Available = true };
            view.Squad4 = new AriaTouchTarget { Id = 104, Available = true };
            for (int slot = 0; slot < 5; slot++)
            {
                AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
                Assert.AreEqual(0, plan.AssaultIssued);
                Assert.AreEqual(AriaSkirmishIntent.SelectSquad, plan.Intent);
                Assert.AreEqual(100 + slot, output.TargetId);
                view.ExpandedSelectedMask |= 1 << slot;
            }

            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(AriaSkirmishIntent.Attack, plan.Intent);
            Assert.AreEqual(42, output.TargetId);
            Assert.AreEqual(0, plan.AssaultIssued, "Attack is requested only; the latch waits for the order.");

            view.Attack = new AriaTouchTarget { Id = 42, Available = false };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0, plan.AssaultIssued);
            Assert.AreEqual(AriaSkirmishIntent.Inspect, plan.Intent);
            Assert.AreEqual(AriaPlayObservationKind.Waiting, output.Kind);

            view.Attack = new AriaTouchTarget { Id = 42, Available = true };
            view.ExpandedAttackOrderMask = view.ExpandedAssaultMask;
            view.NextSquadPage = default;
            view.PreviousSquadPage = new AriaTouchTarget { Id = 201, Available = true };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(0, plan.AssaultIssued);
            Assert.AreEqual(1, plan.StructureOrdered);
            Assert.AreEqual(201, output.TargetId);

            view.ExpandedPageIndex = 0;
            view.ExpandedAssaultMask = 0;
            view.ExpandedStructureMask = 0;
            view.ExpandedSelectedMask = 0;
            view.ExpandedAttackOrderMask = 0;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(1, plan.AssaultIssued);
            Assert.AreEqual(1, plan.StructureOrdered);
        }

        [Test]
        public void PresentedColumnDestroysEnemyBaseBeforeDeadlineOnLiveSeeds()
        {
            PresentedColumnDestroysEnemyBaseBeforeDeadline(130365);
            PresentedColumnDestroysEnemyBaseBeforeDeadline(155923);
            CheckedInRunsCsvHasExpectedHeader();
        }

        [Test]
        public void SingleTimeScaleBlipKeepsNormalSpeedUntilFinish()
        {
            SkirmishS002NormalSpeedLatch latch = SkirmishS002NormalSpeedLatch.Start();
            latch = SkirmishS002AriaRunLog.ObserveTimeScale(latch, 0f);
            Assert.IsTrue(latch.Normal);
            Assert.AreEqual(1, latch.ConsecutiveOffSpeed);
            latch = SkirmishS002AriaRunLog.ObserveTimeScale(latch, 1f);
            Assert.IsTrue(latch.Normal);
            Assert.AreEqual(0, latch.ConsecutiveOffSpeed);

            latch = SkirmishS002NormalSpeedLatch.Start();
            for (int i = 0; i < SkirmishS002AriaRunLog.SustainedOffSpeedSamples - 1; i++)
                latch = SkirmishS002AriaRunLog.ObserveTimeScale(latch, 2f);
            Assert.IsTrue(latch.Normal);
            latch = SkirmishS002AriaRunLog.ObserveTimeScale(latch, 2f);
            Assert.IsFalse(latch.Normal);
            Assert.AreEqual(SkirmishS002AriaRunLog.SustainedOffSpeedSamples, latch.ConsecutiveOffSpeed);

            Assert.IsTrue(SkirmishS002AriaRunLog.FinishNormalSpeed(latch, 1f, 1080f, 1080d));
            Assert.IsFalse(SkirmishS002AriaRunLog.FinishNormalSpeed(latch, 1f, 1080f, 400d));
            Assert.IsFalse(SkirmishS002AriaRunLog.FinishNormalSpeed(latch, 0f, 1080f, 1080d));
            Assert.IsTrue(SkirmishS002AriaRunLog.FinishNormalSpeed(
                SkirmishS002NormalSpeedLatch.Start(), 0f, 1080f, 1080d, finishedResultFrozen: true));
            Assert.IsFalse(SkirmishS002AriaRunLog.FinishNormalSpeed(latch, 0f, 1080f, 1080d, finishedResultFrozen: true));
            Assert.IsFalse(SkirmishS002AriaRunLog.FinishNormalSpeed(latch, 0f, 1080f, 400d, finishedResultFrozen: true));
            Assert.IsFalse(SkirmishS002AriaRunLog.FinishNormalSpeed(latch, 1f, 20f, 40d));
            var stillNormal = SkirmishS002NormalSpeedLatch.Start();
            stillNormal = SkirmishS002AriaRunLog.ObserveTimeScale(stillNormal, 0f);
            Assert.IsTrue(SkirmishS002AriaRunLog.FinishNormalSpeed(stillNormal, 1f, 20f, 40d));
        }

        private static void PresentedColumnDestroysEnemyBaseBeforeDeadline(int seed)
        {
            using var world = new World("PresentedColumn-" + seed);
            BootPlayingSession(world, out Entity session, seed);
            EntityManager em = world.EntityManager;
            PlaceOnAuthoredPads(em, session);
            ObserveEnemyFixtureThreats(world, session);
            RevealAll(em);
            int materials = SkirmishMaterialsService.Read(em, session, 1);
            Entity playerBase = DesignatedBase(em, 1);
            Entity enemyBase = DesignatedBase(em, 2);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Assert.AreNotEqual(Entity.Null, tank, "seed " + seed);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            int steps = 0;
            while (steps < 1080 && em.GetComponentData<UnitHealth>(enemyBase).Current > 0)
            {
                var output = new AriaPlayObservationComponent();
                AriaSkirmishObservation view = PresentedObservation(em, session, playerBase, enemyBase);
                AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
                ApplyPresentedControl(em, session, output);
                SkirmishFogService.Project(em, session);
                SkirmishEnemyStrategySystem.Evaluate(em, session, owned, authored.ArmyGround);
                SkirmishExpandedEngagementService.Step(em, session, 1f, false);
                SkirmishWorldMovementService.Step(em, session, 1f, false);
                steps++;
            }

            int enemyHp = em.GetComponentData<UnitHealth>(enemyBase).Current;
            int playerHp = em.GetComponentData<UnitHealth>(playerBase).Current;
            Assert.Less(steps, 1080, "seed " + seed + " enemyHp=" + enemyHp + " playerHp=" + playerHp);
            Assert.AreEqual(0, enemyHp, "seed " + seed);
            Assert.Greater(playerHp, 0, "seed " + seed);
            Assert.AreEqual(materials, SkirmishMaterialsService.Read(em, session, 1), "seed " + seed);
            Assert.AreEqual(1, plan.AssaultIssued, "seed " + seed);
            Assert.AreEqual(
                SkirmishGroupOrderKind.Attack,
                FirstFactionGroup(em, session, 1, SkirmishRoleKind.Tank).LastOrder,
                "seed " + seed);
            if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
            {
                SkirmishObjectiveClockComponent clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
                clock.ElapsedSeconds = steps;
                clock.Playing = 1;
                clock.Paused = 0;
                em.SetComponentData(session, clock);
            }

            PublishProjectedMatch(world, session);
            SkirmishObjectiveStateComponent objective = em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, objective.Outcome, "seed " + seed);
            Assert.AreEqual(SkirmishEndReasonKind.MainBaseDestroyed, objective.Reason, "seed " + seed);
            SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
            Assert.AreEqual(SkirmishPhase.Finished, match.Phase, "seed " + seed);
            Assert.AreEqual(SkirmishOutcome.Victory, match.Outcome, "seed " + seed);
            Assert.AreEqual(SkirmishEndReason.MainBaseDestroyed, match.Reason, "seed " + seed);
            Assert.AreNotEqual(SkirmishOutcome.Draw, match.Outcome, "seed " + seed);
        }

        private static AriaSkirmishObservation PresentedObservation(
            EntityManager em,
            Entity session,
            Entity playerBase,
            Entity enemyBase)
        {
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryReadPage(
                em,
                session,
                out int pageIndex,
                out bool previousPage,
                out bool nextPage,
                out int pageCount, out int totalGroups, out int selectedGroups, out int selectedOffPage, out uint rosterRevision,
                out SkirmishPresentedSlot slot0,
                out SkirmishPresentedSlot slot1,
                out SkirmishPresentedSlot slot2,
                out SkirmishPresentedSlot slot3,
                out SkirmishPresentedSlot slot4));
            var slots = new[] { slot0, slot1, slot2, slot3, slot4 };
            int assault = 0;
            int selected = 0;
            int structure = 0;
            int ordered = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].Occupied)
                    continue;
                if (slots[i].Assault)
                    assault |= 1 << i;
                if (slots[i].Selected)
                    selected |= 1 << i;
                if (slots[i].Structure)
                    structure |= 1 << i;
                if (slots[i].AttackOrdered)
                    ordered |= 1 << i;
            }

            var publicRoster = new List<SkirmishArmyDrawerSlot>();
            SkirmishArmyDrawerProjection.Project(em, session, publicRoster);
            int selectedOrdinal = publicRoster.FindIndex(card => card.Selected != 0);
            AriaTouchTarget selectedCard = default;
            if (selectedOrdinal >= 0)
            {
                int targetPage = selectedOrdinal / 5;
                selectedCard = targetPage == pageIndex
                    ? new AriaTouchTarget { Id = 100 + selectedOrdinal % 5, Available = true }
                    : new AriaTouchTarget { Id = targetPage > pageIndex ? 200 : 201,
                        Available = true, Drag = true, ScrollTargetId = (int)publicRoster[selectedOrdinal].GroupId,
                        ScrollDistanceViewports = Math.Abs(targetPage - pageIndex) };
            }
            return new AriaSkirmishObservation
            {
                Active = true,
                ExpandedSession = true,
                EnemyDesignatedAlive = em.GetComponentData<UnitHealth>(enemyBase).Current > 0,
                PlayerDesignatedAlive = em.GetComponentData<UnitHealth>(playerBase).Current > 0,
                Infantry = 20,
                ExpandedPageIndex = pageIndex,
                ExpandedPageCount = pageCount,
                ExpandedTotalGroups = totalGroups,
                ExpandedSelectedGroups = selectedGroups,
                ExpandedSelectedOffPage = selectedOffPage,
                ExpandedRosterRevision = rosterRevision,
                ExpandedCanNextPage = nextPage,
                PreviousSquadPage = new AriaTouchTarget { Id = 201, Available = previousPage },
                NextSquadPage = new AriaTouchTarget { Id = 200, Available = nextPage },
                SelectedSquadToToggle = selectedCard,
                ExpandedAssaultMask = assault,
                ExpandedSelectedMask = selected,
                ExpandedStructureMask = structure,
                ExpandedAttackOrderMask = ordered,
                Squad0 = PresentedCard(slot0, 100),
                Squad1 = PresentedCard(slot1, 101),
                Squad2 = PresentedCard(slot2, 102),
                Squad3 = PresentedCard(slot3, 103),
                Squad4 = PresentedCard(slot4, 104),
                Attack = new AriaTouchTarget { Id = 42, Available = true }
            };
        }

        private static AriaTouchTarget PresentedCard(SkirmishPresentedSlot slot, int id)
        {
            return new AriaTouchTarget { Id = id, Available = slot.Occupied };
        }

        private static void ApplyPresentedControl(EntityManager em, Entity session, AriaPlayObservationComponent output)
        {
            if (output.Kind != AriaPlayObservationKind.Control)
                return;
            if (output.TargetId == 200)
                SkirmishExpandedPresentedOrders.TryChangePage(em, session, 1);
            else if (output.TargetId == 201)
                SkirmishExpandedPresentedOrders.TryChangePage(em, session, -1);
            else if (output.TargetId >= 100 && output.TargetId <= 104 &&
                SkirmishExpandedPresentedOrders.TryReadPage(em, session, out _, out _, out _,
                    out _, out _, out _, out _, out _,
                    out var a, out var b, out var c, out var d, out var e))
            {
                var visibleSlots = new[] { a, b, c, d, e };
                SkirmishExpandedPresentedOrders.TryTogglePresentedGroup(em, session,
                    visibleSlots[output.TargetId - 100].GroupId);
            }
            else if (output.TargetId == 42)
                FixtureAttackEnemyBase(em, session);
        }

        [Test]
        public void PayloadLoggersDoNotStampVictory()
        {
            Assert.IsTrue(SkirmishAriaAcceptancePayload.TryCreateFirstVisitS002(
                104731,
                GameLocalization.EnglishLocaleCode,
                out SkirmishAriaAcceptancePayload payload,
                out string error),
                error);
            string play = AriaPlayEditorValidation.AcceptExpandedPayload(in payload);
            string watch = SkirmishExpandedAriaWatchValidation.AcceptExpandedPayload(
                SkirmishAcceptanceCensusCapture.DefinitionId,
                SkirmishSizeId.Standard,
                SkirmishDifficultyId.Regular,
                155923,
                GameLocalization.PersianLocaleCode);
            Assert.IsTrue(play.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(watch.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(play.IndexOf("seed=104731", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(watch.IndexOf("seed=155923", StringComparison.Ordinal) >= 0);
        }

        [Test]
        public void SimulationStallFailsFastAfterGrace()
        {
            Assert.IsFalse(SkirmishS002AriaRunLog.IsSimulationNotAdvancing(
                playing: true,
                simulationActive: false,
                matchElapsedSeconds: 0f,
                wallSecondsSincePlaying: 10d));
            Assert.IsTrue(SkirmishS002AriaRunLog.IsSimulationNotAdvancing(
                playing: true,
                simulationActive: false,
                matchElapsedSeconds: 0f,
                wallSecondsSincePlaying: SkirmishS002AriaRunLog.SimulationStallGraceSeconds));
            Assert.IsTrue(SkirmishS002AriaRunLog.IsSimulationNotAdvancing(
                playing: true,
                simulationActive: true,
                matchElapsedSeconds: 0f,
                wallSecondsSincePlaying: SkirmishS002AriaRunLog.SimulationStallGraceSeconds + 1d));
            Assert.IsFalse(SkirmishS002AriaRunLog.IsSimulationNotAdvancing(
                playing: true,
                simulationActive: true,
                matchElapsedSeconds: 0.5f,
                wallSecondsSincePlaying: 120d));
            Assert.AreEqual(
                SkirmishS002AriaRunLog.AbortReasonSimulationNotAdvancing,
                "simulationNotAdvancing");
        }

        [Test]
        public void ExpandedObjectiveClockProjectsOntoMatchElapsed()
        {
            using var world = new World(nameof(ExpandedObjectiveClockProjectsOntoMatchElapsed));
            EntityManager em = world.EntityManager;
            Entity session = em.CreateEntity();
            em.AddComponentData(session, new SkirmishExpandedSessionComponent
            {
                SessionId = "elapsed-s002",
                CatalogId = "S002",
                IsLegacy = 0,
                Phase = SkirmishSessionPhase.Playing
            });
            em.AddComponentData(session, new SkirmishMatchState
            {
                SessionId = "elapsed-s002",
                Phase = SkirmishPhase.Preparing,
                ElapsedSeconds = 0f
            });
            em.AddComponentData(session, new SkirmishObjectiveClockComponent
            {
                ElapsedSeconds = 37.5f,
                DeadlineSeconds = 1080,
                Paused = 0,
                Playing = 1
            });

            SkirmishExpandedSessionControlService.ProjectMatchPhase(em, session);

            var match = em.GetComponentData<SkirmishMatchState>(session);
            Assert.AreEqual(SkirmishPhase.Playing, match.Phase);
            Assert.AreEqual(37.5f, match.ElapsedSeconds, 0.001f);
            Assert.AreEqual(
                37.5f,
                SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match),
                0.001f);
        }

        [Test]
        public void RosterProjectionOnUpdateAppliesStructureDamageWithoutIterating()
        {
            using var world = new World(nameof(RosterProjectionOnUpdateAppliesStructureDamageWithoutIterating));
            BootPlayingSession(world, out Entity session);
            EntityManager em = world.EntityManager;
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Entity barracks = DesignatedBase(em, 2);
            Assert.AreNotEqual(Entity.Null, tank);
            em.RemoveComponent<SkirmishRoleOverlayComponent>(tank);
            em.RemoveComponent<UnitHealth>(tank);
            em.RemoveComponent<SkirmishRoleOverlayComponent>(barracks);
            em.RemoveComponent<UnitHealth>(barracks);

            world.GetOrCreateSystem<SkirmishRosterProjectionSystem>().Update(world.Unmanaged);

            SkirmishRoleOverlayComponent overlay = em.GetComponentData<SkirmishRoleOverlayComponent>(tank);
            Assert.AreEqual(1, overlay.Applied);
            Assert.Greater(overlay.Damage, 0);
            Assert.Greater(overlay.RangeWorld, 0f);
            Assert.IsTrue((overlay.TargetDomains & SkirmishTargetDomain.Structure) != 0);
            Assert.Greater(em.GetComponentData<UnitHealth>(tank).Max, 0);
            Assert.Greater(em.GetComponentData<UnitHealth>(barracks).Max, 0);
            Assert.AreEqual(SkirmishPhase.Playing, em.GetComponentData<SkirmishMatchState>(session).Phase);
        }

        [Test]
        public void FinishedMatchRetainsActorsAndSetupUntilExplicitCleanup()
        {
            using var world = new World(nameof(FinishedMatchRetainsActorsAndSetupUntilExplicitCleanup));
            BootPlayingSession(world, out Entity session);
            EntityManager em = world.EntityManager;
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            SkirmishExpandedSessionComponent expanded = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            expanded.Phase = SkirmishSessionPhase.Finished;
            em.SetComponentData(session, expanded);

            world.GetOrCreateSystem<SkirmishSessionCleanupSystem>().Update(world.Unmanaged);
            Assert.IsTrue(em.Exists(tank), "Result review must preserve the frozen battlefield.");
            Assert.IsTrue(em.HasComponent<SkirmishResolvedSetupRecord>(session), "Replay still needs the compiled setup.");
            Assert.AreEqual(SkirmishSessionPhase.Finished, em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase);
            em.AddComponent<SkirmishExpandedCleanupRequest>(session);
            world.GetOrCreateSystem<SkirmishSessionCleanupSystem>().Update(world.Unmanaged);

            Assert.IsFalse(em.Exists(tank));
            Assert.AreEqual(
                SkirmishSessionPhase.Cleaning,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase);
            CheckedInRunsCsvHasExpectedHeader();
        }

        [Test]
        public void PresentedSelectionOrdersStructureColumnAndDestroysEnemyBase()
        {
            using var world = new World(nameof(PresentedSelectionOrdersStructureColumnAndDestroysEnemyBase));
            BootPlayingSession(world, out Entity session, 130365);
            EntityManager em = world.EntityManager;
            PlaceOnAuthoredPads(em, session);
            ObserveEnemyFixtureThreats(world, session);
            RevealAll(em);
            int materials = SkirmishMaterialsService.Read(em, session, 1);
            Entity playerBase = DesignatedBase(em, 1);
            Entity enemyBase = DesignatedBase(em, 2);
            Entity shell = em.CreateEntity();
            em.AddComponentData(shell, new AriaPlaySessionComponent { Phase = AriaPlayPhase.Manual });
            Assert.IsFalse(FixtureAttackEnemyBase(em, session));
            Assert.AreNotEqual(
                SkirmishGroupOrderKind.Attack,
                FirstFactionGroup(em, session, 1, SkirmishRoleKind.Tank).LastOrder);

            em.SetComponentData(shell, new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing });
            SelectPresentedStructureColumn(em, session);
            Assert.AreEqual(
                SkirmishGroupOrderKind.Attack,
                FirstFactionGroup(em, session, 1, SkirmishRoleKind.Tank).LastOrder);
            Assert.AreEqual(
                SkirmishGroupOrderKind.Attack,
                FirstFactionGroup(em, session, 1, SkirmishRoleKind.Rocketeer).LastOrder);
            Assert.AreEqual(materials, SkirmishMaterialsService.Read(em, session, 1));

            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            int steps = 0;
            while (steps < 1080 && em.GetComponentData<UnitHealth>(enemyBase).Current > 0)
            {
                SkirmishEnemyStrategySystem.Evaluate(em, session, owned, authored.ArmyGround);
                SkirmishExpandedEngagementService.Step(em, session, 1f, false);
                SkirmishWorldMovementService.Step(em, session, 1f, false);
                steps++;
            }

            Assert.Less(steps, 1080, "enemyHp=" + em.GetComponentData<UnitHealth>(enemyBase).Current);
            Assert.AreEqual(0, em.GetComponentData<UnitHealth>(enemyBase).Current);
            Assert.Greater(em.GetComponentData<UnitHealth>(playerBase).Current, 0);
            Assert.AreEqual(materials, SkirmishMaterialsService.Read(em, session, 1));
            PublishProjectedMatch(world, session);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, em.GetComponentData<SkirmishObjectiveStateComponent>(session).Outcome);
            Assert.AreEqual(SkirmishEndReasonKind.MainBaseDestroyed, em.GetComponentData<SkirmishObjectiveStateComponent>(session).Reason);
            SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
            Assert.AreEqual(SkirmishPhase.Finished, match.Phase);
            Assert.AreEqual(SkirmishOutcome.Victory, match.Outcome);
            Assert.AreEqual(SkirmishEndReason.MainBaseDestroyed, match.Reason);
            CheckedInRunsCsvHasExpectedHeader();
        }

        [Test]
        public void MissingBarracksPrefabStillAnchorsTheMeasuredBaseForPresentedOrders()
        {
            using var world = new World(nameof(MissingBarracksPrefabStillAnchorsTheMeasuredBaseForPresentedOrders));
            BootPlayingSession(world, out Entity session, 130365);
            EntityManager em = world.EntityManager;
            Entity playerBase = DesignatedBase(em, 1);
            Entity enemyBase = DesignatedBase(em, 2);
            Entity tank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Entity rocketeer = FirstFactionUnit(em, 1, SkirmishRoleKind.Rocketeer);
            Assert.IsFalse(em.HasComponent<LocalTransform>(enemyBase));
            Assert.IsFalse(em.HasComponent<LocalTransform>(playerBase));
            Assert.IsFalse(em.HasComponent<LocalTransform>(rocketeer));

            GameObject tankPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tankPrefab.name = "Unit_Veh_Tank_USA";
            tankPrefab.hideFlags = HideFlags.HideAndDontSave;
            tankPrefab.SetActive(false);
            var catalog = new SkirmishVisualPrefabCatalog();
            catalog.Bind("Unit_Veh_Tank_USA", tankPrefab, true);
            try
            {
                SkirmishVisualSpawnService.BindCatalog(em, session, catalog);
                Assert.IsFalse(catalog.Contains("Building_Barrack"));
                world.GetOrCreateSystem<SkirmishVisualSpawnSystem>().Update(world.Unmanaged);

                Assert.IsTrue(em.HasComponent<LocalTransform>(enemyBase));
                Assert.IsTrue(em.HasComponent<LocalTransform>(playerBase));
                Assert.IsTrue(em.HasComponent<LocalTransform>(rocketeer));
                Assert.IsFalse(VisuallySpawned(em, enemyBase));
                Assert.IsFalse(VisuallySpawned(em, rocketeer));
                Assert.IsTrue(VisuallySpawned(em, tank));
                SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                Assert.IsTrue(TryStructureSpawn(setup, 2, true, out float enemyX, out float enemyZ));
                float3 enemyPos = em.GetComponentData<LocalTransform>(enemyBase).Position;
                Assert.AreEqual(enemyX, enemyPos.x, 0.05f);
                Assert.AreEqual(enemyZ, enemyPos.z, 0.05f);
                Assert.IsTrue(TryStructureSpawn(setup, 1, true, out float playerX, out float playerZ));
                float3 playerPos = em.GetComponentData<LocalTransform>(playerBase).Position;
                Assert.AreEqual(playerX, playerPos.x, 0.05f);
                Assert.AreEqual(playerZ, playerPos.z, 0.05f);
                float3 standIn = SkirmishWorldMovementService.DefaultAdvance(em, session);
                // The advance default is the enemy staging pad, distinct from the
                // designated base; map-bound spacing is ~61 m along the base axis.
                Assert.Greater(math.distance(enemyPos, standIn), 40f);
                Assert.Greater(math.distance(em.GetComponentData<LocalTransform>(tank).Position, float3.zero), 50f);

                int materials = SkirmishMaterialsService.Read(em, session, 1);
                Assert.IsFalse(SkirmishFogService.IsVisible(em, enemyBase));
                Assert.IsFalse(SkirmishArmyCommandService.TryIssueGroupOrder(
                    em, session, GroupOf(em, tank), 1,
                    SkirmishGroupOrderKind.Attack, enemyBase, out var unknownAttack));
                Assert.AreEqual(SkirmishReasonCode.HiddenContact, unknownAttack.Reason);
                ObserveMeasuredBaseForPlayerFixture(world, session, enemyBase);
                Assert.IsFalse(VisuallySpawned(em, enemyBase),
                    "Observation must not supply the missing Barracks prefab.");
                Assert.AreEqual(enemyPos, em.GetComponentData<LocalTransform>(enemyBase).Position,
                    "Observation must retain the native measured base anchor.");
                Assert.AreEqual(materials, SkirmishMaterialsService.Read(em, session, 1));
                Entity shell = em.CreateEntity();
                em.AddComponentData(shell, new AriaPlaySessionComponent { Phase = AriaPlayPhase.Manual });
                Assert.IsFalse(FixtureAttackEnemyBase(em, session));
                Assert.AreNotEqual(
                    SkirmishGroupOrderKind.Attack,
                    FirstFactionGroup(em, session, 1, SkirmishRoleKind.Tank).LastOrder);

                em.SetComponentData(shell, new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing });
                SelectPresentedStructureColumn(em, session);
                Assert.AreEqual(
                    SkirmishGroupOrderKind.Attack,
                    FirstFactionGroup(em, session, 1, SkirmishRoleKind.Tank).LastOrder);
                Assert.AreEqual(
                    SkirmishGroupOrderKind.Attack,
                    FirstFactionGroup(em, session, 1, SkirmishRoleKind.Rocketeer).LastOrder);
                SkirmishMoveIntentComponent ordered = em.GetComponentData<SkirmishMoveIntentComponent>(tank);
                Assert.AreEqual(enemyPos.x, ordered.DestinationX, 0.05f);
                Assert.AreEqual(enemyPos.z, ordered.DestinationZ, 0.05f);
                Assert.AreEqual(enemyBase, ordered.AttackTarget);
                Assert.AreEqual(materials, SkirmishMaterialsService.Read(em, session, 1));

                ordered.DestinationX = standIn.x;
                ordered.DestinationZ = standIn.z;
                ordered.Active = 0;
                em.SetComponentData(tank, ordered);
                Assert.Greater(SkirmishWorldMovementService.Step(em, session, 1f, false), 0);
                ordered = em.GetComponentData<SkirmishMoveIntentComponent>(tank);
                Assert.AreEqual(enemyPos.x, ordered.DestinationX, 0.05f);
                Assert.AreEqual(enemyPos.z, ordered.DestinationZ, 0.05f);
                Assert.AreEqual(1, ordered.Active);

                SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
                using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
                int steps = 0;
                while (steps < 1080 && em.GetComponentData<UnitHealth>(enemyBase).Current > 0)
                {
                    SkirmishEnemyStrategySystem.Evaluate(em, session, owned, authored.ArmyGround);
                    SkirmishVisualSpawnService.EnsureMissingCombatTransforms(em, session);
                    SkirmishExpandedEngagementService.Step(em, session, 1f, false);
                    SkirmishWorldMovementService.Step(em, session, 1f, false);
                    steps++;
                }

                Assert.Less(steps, 1080, "enemyHp=" + em.GetComponentData<UnitHealth>(enemyBase).Current);
                Assert.AreEqual(0, em.GetComponentData<UnitHealth>(enemyBase).Current);
                Assert.Greater(em.GetComponentData<UnitHealth>(playerBase).Current, 0);
                Assert.AreEqual(materials, SkirmishMaterialsService.Read(em, session, 1));
                PublishProjectedMatch(world, session);
                Assert.AreEqual(SkirmishOutcomeKind.Victory, em.GetComponentData<SkirmishObjectiveStateComponent>(session).Outcome);
                Assert.AreEqual(SkirmishEndReasonKind.MainBaseDestroyed, em.GetComponentData<SkirmishObjectiveStateComponent>(session).Reason);
                SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
                Assert.AreEqual(SkirmishPhase.Finished, match.Phase);
                Assert.AreEqual(SkirmishOutcome.Victory, match.Outcome);
                Assert.AreEqual(SkirmishEndReason.MainBaseDestroyed, match.Reason);
                CheckedInRunsCsvHasExpectedHeader();
            }
            finally
            {
                if (em.Exists(session))
                {
                    SkirmishScenarioSpawnSystem.DestroyAttemptOwned(
                        em, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
                }

                catalog.Dispose();
            }
        }

        [Test]
        public void LaunchResetsLiveTraceSoVictoryEvidenceOmitsPriorDraws()
        {
            string directory = Path.Combine(Path.GetTempPath(), "s002-aria-trace-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string live = Path.Combine(directory, "s002-aria-live-130365-en.jsonl");
                File.WriteAllText(
                    live,
                    "{\"elapsed\":1080.03,\"phase\":\"Finished\",\"outcome\":\"Draw\",\"reason\":\"TimeLimit\",\"playerBaseHp\":800,\"enemyBaseHp\":800}\n" +
                    "{\"elapsed\":12,\"phase\":\"Finished\",\"outcome\":\"Abort\",\"reason\":\"timeout\"}\n");

                SkirmishS002AriaRunHarness.ResetLiveTrace(live);
                Assert.AreEqual(string.Empty, File.ReadAllText(live));

                const string playing =
                    "{\"elapsed\":2,\"phase\":\"Playing\",\"outcome\":\"None\",\"reason\":\"None\",\"playerBaseHp\":800,\"enemyBaseHp\":760}\n";
                const string victory =
                    "{\"elapsed\":90,\"phase\":\"Finished\",\"outcome\":\"Victory\",\"reason\":\"MainBaseDestroyed\",\"playerBaseHp\":800,\"enemyBaseHp\":0}\n";
                File.AppendAllText(live, playing);
                File.AppendAllText(live, victory);

                string evidence = Path.Combine(directory, "s002-aria-rs-130365-en-1.jsonl");
                SkirmishS002AriaRunHarness.CopyLiveTraceToEvidence(live, evidence);
                string copied = File.ReadAllText(evidence);
                Assert.AreEqual(playing + victory, copied);
                Assert.AreEqual(2, File.ReadAllLines(evidence).Length);
                Assert.IsTrue(copied.IndexOf("\"phase\":\"Playing\"", StringComparison.Ordinal) >= 0);
                Assert.IsTrue(copied.IndexOf("\"outcome\":\"Victory\"", StringComparison.Ordinal) >= 0);
                Assert.IsTrue(copied.IndexOf("\"reason\":\"MainBaseDestroyed\"", StringComparison.Ordinal) >= 0);
                Assert.IsTrue(copied.IndexOf("\"outcome\":\"Draw\"", StringComparison.Ordinal) < 0);
                Assert.IsTrue(copied.IndexOf("\"outcome\":\"Abort\"", StringComparison.Ordinal) < 0);
                Assert.IsTrue(copied.IndexOf("TimeLimit", StringComparison.Ordinal) < 0);
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        // Fixture-level control dispatch; this is not normal-input acceptance evidence.
        private static void SelectPresentedStructureColumn(EntityManager em, Entity session)
        {
            int initialPage = -1;
            for (int pages = 0; pages < 32; pages++)
            {
                Assert.IsTrue(SkirmishExpandedPresentedOrders.TryReadPage(em, session,
                    out int page, out bool previous, out bool next, out int pageCount, out _, out _, out _, out _, out var a, out var b, out var c, out var d, out var e));
                if (initialPage < 0) initialPage = page;
                if (page >= pageCount) break;
                var slots = new[] { a, b, c, d, e };
                bool selected = false;
                for (int i = 0; i < slots.Length; i++)
                    if (slots[i].Occupied && slots[i].Structure)
                    {
                        Assert.IsTrue(SkirmishExpandedPresentedOrders.TryPresentedSlot(em, session, i));
                        selected = true;
                    }
                if (selected) Assert.IsTrue(FixtureAttackEnemyBase(em, session));
                if (next) SkirmishExpandedPresentedOrders.TryChangePage(em, session, 1);
                else if (page > 0 && previous) SkirmishExpandedPresentedOrders.TryChangePage(em, session, -1);
                else break;
            }
        }

        [Test]
        public void UnknownInputEvidenceCannotCountAsAriaWin()
        {
            var facts = Finished("Victory");
            facts.InputViolations = -1;
            facts.HumanInterventions = -1;
            Assert.IsFalse(SkirmishS002AriaRunLog.IsCountedAriaWin(in facts, "Victory"));
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishS002AriaHarnessTests();
                suite.CandidateHashChangesForSourceEditsRenamesAndDeletesButNotEvidence();
                suite.ExpandedMissionSuppressesSelectedLegacyRoster();
                suite.SpawnedForceMemberKeepsBakedPrefabComponents();
                suite.SpawnedVehiclesAvoidBlockedAndOccupiedCells();
                suite.CheckedInRunsCsvHasExpectedHeader();
                suite.ForcedVictoryDoesNotAppend();
                suite.UnknownInputEvidenceCannotCountAsAriaWin();
                suite.UnfinishedOutcomeIsRefusedUntilTheMatchEnds();
                suite.FinishedOutcomesAppendAndOnlyUnguidedVictoryCounts();
                suite.S004RunIdsUseTheirOwnSeedSequenceAndRetainAttempts();
                suite.EstablishedDefensesDoNotOpenAnotherConstructionSequence();
                suite.ExistingAntiAirPreservesEstablishedAirBudget();
                suite.AttackOrdersDamageOnlyLegalVisibleTargets();
                suite.AssaultColumnKillsTheEnemyTankAndLeavesThePlayerBarracks();
                suite.ExpandedAssaultPlanUsesVisibleCardsWithoutMutatingStocks();
                suite.ExpandedAssaultDoesNotAttackWithRiflesWhileNextPageExists();
                suite.ExpandedAssaultStartsImmediatelyAndDoesNotBlockOnFlatHealth();
                suite.BlockedExpandedPlanRequiresExplicitRetry();
                suite.GridlessFixtureClearsFollowerBeforeLocalStep();
                suite.StructureInRangeIsDamagedWhileACombatantIsAlsoInRange();
                suite.DeadlineDrawPublishesFinishedMatchWithoutStampingVictory();
                suite.DeadEnemyBarracksFinishesVictoryWithoutRulesSystem();
                suite.AssaultColumnDestroysEnemyBarracksAndPublishesVictory();
                suite.StructureColumnWaitsForAttackOrderBeforeLatchingAssault();
                suite.PresentedColumnDestroysEnemyBaseBeforeDeadlineOnLiveSeeds();
                suite.SingleTimeScaleBlipKeepsNormalSpeedUntilFinish();
                suite.PayloadLoggersDoNotStampVictory();
                suite.SimulationStallFailsFastAfterGrace();
                suite.ExpandedObjectiveClockProjectsOntoMatchElapsed();
                suite.RosterProjectionOnUpdateAppliesStructureDamageWithoutIterating();
                suite.FinishedMatchRetainsActorsAndSetupUntilExplicitCleanup();
                suite.PresentedSelectionOrdersStructureColumnAndDestroysEnemyBase();
                suite.MissingBarracksPrefabStillAnchorsTheMeasuredBaseForPresentedOrders();
                suite.LaunchResetsLiveTraceSoVictoryEvidenceOmitsPriorDraws();
                Debug.Log("[SkirmishS002AriaHarnessTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishS002AriaHarnessTests] result=Failed\n" + exception);
                throw;
            }
        }

        private static void AppendFinished(
            string path,
            string runId,
            string outcome,
            bool normalSpeed,
            int violations,
            int interventions,
            bool expectCounted)
        {
            var facts = Finished(outcome);
            facts.RunId = runId;
            facts.NormalSpeed = normalSpeed;
            facts.InputViolations = violations;
            facts.HumanInterventions = interventions;
            Assert.IsTrue(SkirmishS002AriaRunLog.TryAppendTerminalRow(path, in facts, out string row, out string error), error);
            Assert.AreEqual(outcome, Field(row, 13));
            Assert.AreEqual(expectCounted, SkirmishS002AriaRunLog.IsCountedAriaWin(in facts, outcome));
        }

        private static SkirmishS002AriaTerminalFacts Finished(string outcome)
        {
            return new SkirmishS002AriaTerminalFacts
            {
                RunId = "S002-aria-rs-104731-en-1",
                DefinitionVersion = 1,
                CodeHash = "abc",
                ConfigHash = "def",
                Seed = 104731,
                Locale = GameLocalization.EnglishLocaleCode,
                Device = "Editor",
                NormalSpeed = true,
                StartedAtUtc = "2026-09-22T00:00:00Z",
                MatchFinished = true,
                MatchOutcome = outcome,
                EndReason = "MainBaseDestroyed",
                DurationSeconds = 90f,
                TracePath = "Design/AgentReports/SkirmishExpansion/S002/_Evidence/sample.jsonl",
                LogPath = "Design/AgentReports/SkirmishExpansion/S002/_Evidence/sample-log.txt",
                ForcedVictory = false
            };
        }

        private static string Field(string row, int index)
        {
            string[] fields = row.Split(',');
            Assert.Greater(fields.Length, index);
            return fields[index];
        }

        private static string TempCsv()
        {
            return Path.Combine(Path.GetTempPath(), "s002-aria-harness-" + Guid.NewGuid().ToString("N") + ".csv");
        }

        private static string ProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static void BootPlayingSession(World world, out Entity session, int seed = 104731)
        {
            EntityManager em = world.EntityManager;
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(ProjectRoot(), out var matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = CreateLedgerFixture();
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS002.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                em,
                "S002",
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                seed,
                authored,
                matrix,
                manifest,
                out _,
                out _,
                out var reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            world.GetOrCreateSystem<SkirmishSessionInitializationSystem>().Update(world.Unmanaged);
            world.GetOrCreateSystem<SkirmishScenarioSpawnSystem>().Update(world.Unmanaged);
            Assert.AreEqual(SkirmishSessionPhase.Playing, em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            SkirmishRosterProjectionSystem.Apply(em, owned, sessionId, setup);
            SkirmishArmyGroupSystem.RefreshAlive(em, session, owned, sessionId);
            SkirmishExpandedSessionControlService.ProjectMatchPhase(em, session);
            Assert.AreEqual(SkirmishPhase.Playing, em.GetComponentData<SkirmishMatchState>(session).Phase);
        }

        private static void PublishProjectedMatch(World world, Entity session)
        {
            world.GetOrCreateSystem<SkirmishObjectiveFactProjectionSystem>().Update(world.Unmanaged);
            world.GetOrCreateSystem<SkirmishBaseAssaultObjectiveSystem>().Update(world.Unmanaged);
            world.GetOrCreateSystem<SkirmishOutcomeSystem>().Update(world.Unmanaged);
            world.GetOrCreateSystem<SkirmishExpandedSessionControlSystem>().Update(world.Unmanaged);
        }

        private static void PlaceOnAuthoredPads(EntityManager em, Entity session)
        {
            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            using var units = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using NativeArray<Entity> entities = units.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                byte faction = em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId;
                SkirmishRoleKind role = em.GetComponentData<SkirmishUnitRoleComponent>(entities[i]).Role;
                Assert.IsTrue(TryForceSpawn(setup, faction, role, out float x, out float z), role + " faction " + faction);
                Place(em, entities[i], new float3(x, 0f, z));
            }

            using var structures = em.CreateEntityQuery(
                typeof(SkirmishStructureIdentityComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> pads = structures.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < pads.Length; i++)
            {
                byte faction = em.GetComponentData<SkirmishAttemptOwnedComponent>(pads[i]).FactionId;
                bool designated = em.GetComponentData<SkirmishStructureIdentityComponent>(pads[i]).DesignatedBase != 0;
                Assert.IsTrue(TryStructureSpawn(setup, faction, designated, out float x, out float z));
                Place(em, pads[i], new float3(x, 0f, z));
            }
        }

        private static bool TryForceSpawn(SkirmishResolvedSetup setup, byte faction, SkirmishRoleKind role, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            if (setup?.Forces == null)
                return false;
            for (int i = 0; i < setup.Forces.Length; i++)
            {
                if (setup.Forces[i].FactionId != faction || setup.Forces[i].RoleKind != role)
                    continue;
                x = setup.Forces[i].SpawnWorldX;
                z = setup.Forces[i].SpawnWorldZ;
                return true;
            }

            return false;
        }

        private static bool TryStructureSpawn(SkirmishResolvedSetup setup, byte faction, bool designated, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            if (setup?.Structures == null)
                return false;
            for (int i = 0; i < setup.Structures.Length; i++)
            {
                if (setup.Structures[i].FactionId != faction || setup.Structures[i].DesignatedBase != designated)
                    continue;
                x = setup.Structures[i].SpawnWorldX;
                z = setup.Structures[i].SpawnWorldZ;
                return true;
            }

            return false;
        }

        private static SkirmishExpansionAuthoredSet CreateLedgerFixture()
        {
            // Preserve the original prefab-free ledger profiles; only their reviewed
            // packaged map/source references replace the stale generated layout.
            var catalog=SkirmishExpansionCatalogConfig.Load();
            Assert.IsNotNull(catalog,"Packaged skirmish expansion catalog is required.");
            var fixture=SkirmishExpansionCatalogFactory.CreateInMemory();
            foreach(var definition in new[]{fixture.DefinitionS002,fixture.DefinitionS003,fixture.DefinitionS004})
            {
                Assert.IsTrue(catalog.TryGetDefinition(definition.CatalogId,out var packaged));
                definition.Configure(definition.CatalogId,definition.ObjectiveConfig,definition.ArmyProfileConfig,
                    definition.StartPackageConfig,definition.EconomyConfig,definition.UpgradeCatalog,
                    definition.IntelConfig,definition.RoleCatalog,packaged.MapLayout,
                    definition.RecommendedSize,definition.RequiredFeatureIds);
                definition.BindCampaignMap(packaged.CampaignMap);
                Assert.AreNotSame(packaged.RoleCatalog,definition.RoleCatalog);
            }
            fixture.LayoutDbBa=fixture.DefinitionS002.MapLayout;
            return fixture;
        }

        private static void CompileAndSpawn(EntityManager em, out Entity session)
        {
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(ProjectRoot(), out var matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = CreateLedgerFixture();
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS002.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                em,
                "S002",
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                104731,
                authored,
                matrix,
                manifest,
                out SkirmishResolvedSetup setup,
                out _,
                out var reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(
                em, session, setup, out SkirmishReasonCode reason, out byte visualPending), reason.ToString());
            Assert.AreEqual(0, visualPending);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            SkirmishRosterProjectionSystem.Apply(em, owned, sessionId, setup);
            SkirmishArmyGroupSystem.RefreshAlive(em, session, owned, sessionId);
        }

        private static Entity DesignatedBase(EntityManager em, byte faction)
        {
            using var query = em.CreateEntityQuery(
                typeof(SkirmishStructureIdentityComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishStructureIdentityComponent>(entities[i]).DesignatedBase == 0)
                    continue;
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId == faction)
                    return entities[i];
            }

            Assert.Fail("Missing designated base for faction " + faction);
            return Entity.Null;
        }

        private static Entity FirstApc(EntityManager em, byte faction)
        {
            Entity fast = FirstFactionUnit(em, faction, SkirmishRoleKind.ApcFast);
            return fast != Entity.Null ? fast : FirstFactionUnit(em, faction, SkirmishRoleKind.ApcArmored);
        }

        private static Entity FirstFactionUnit(EntityManager em, byte faction, SkirmishRoleKind role)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId == faction &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(entities[i]).Role == role)
                    return entities[i];
            }

            return Entity.Null;
        }

        private static SkirmishArmyGroupRecord FirstFactionGroup(
            EntityManager em,
            Entity session,
            byte faction,
            SkirmishRoleKind role)
        {
            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].FactionId == faction && buffer[i].Role == role)
                    return buffer[i];
            }

            return default;
        }

        private static uint GroupOf(EntityManager em, Entity unit)
        {
            Assert.AreNotEqual(Entity.Null, unit);
            return em.GetComponentData<SkirmishArmyGroupMembershipComponent>(unit).GroupId;
        }

        private static Entity FirstGroupMember(EntityManager em, uint groupId)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishArmyGroupMembershipComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishArmyGroupMembershipComponent>(entities[i]).GroupId == groupId)
                    return entities[i];
            }

            return Entity.Null;
        }

        private static void Place(EntityManager em, Entity unit, float3 position)
        {
            Assert.AreNotEqual(Entity.Null, unit);
            var transform = LocalTransform.FromPosition(position);
            if (em.HasComponent<LocalTransform>(unit))
                em.SetComponentData(unit, transform);
            else
                em.AddComponentData(unit, transform);
        }

        private static bool VisuallySpawned(EntityManager em, Entity entity)
        {
            return em.HasComponent<SkirmishVisualSpawnedComponent>(entity) &&
                   em.GetComponentData<SkirmishVisualSpawnedComponent>(entity).Spawned != 0;
        }

        // Sparse ledger fixtures have no baked enemy acquisition components. Supply
        // two fixture-owned native observers at the positioned player base/Tank and
        // run the real positive-time fog system. No sight/knowledge/debug override is
        // written. Observers have no roster role, group, attack or capacity identity.
        private static void ObserveEnemyFixtureThreats(World world, Entity session)
        {
            EntityManager em = world.EntityManager;
            Entity playerBase = DesignatedBase(em, 1);
            Entity playerTank = FirstFactionUnit(em, 1, SkirmishRoleKind.Tank);
            Entity baseObserver = CreateFixtureFogObserver(em, session, playerBase, 2);
            Entity tankObserver = CreateFixtureFogObserver(em, session, playerTank, 2);
            var expanded = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var originalPhase = expanded.Phase;
            var fog = em.GetComponentData<SkirmishFogStateComponent>(session);
            Assert.AreEqual(1, fog.SharedFog);
            Assert.AreEqual(0, fog.DevelopmentFullVision);
            Entity temporaryGrid = Entity.Null;
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (grids.IsEmptyIgnoreFilter)
            {
                temporaryGrid = em.CreateEntity(typeof(GridConfig));
                em.SetComponentData(temporaryGrid,
                    new GridConfig { Width = 4096, Height = 4096, CellSize = 1 });
            }
            try
            {
                Assert.AreEqual(1, grids.CalculateEntityCount());
                expanded.Phase = SkirmishSessionPhase.Playing;
                em.SetComponentData(session, expanded);
                world.SetTime(new TimeData(world.Unmanaged.Time.ElapsedTime + 1, .1f));
                world.GetOrCreateSystem<SkirmishSharedFogUpdateSystem>().Update(world.Unmanaged);
                em.CompleteAllTrackedJobs();
                AssertFixtureDirectObservation(em, session, playerBase, baseObserver, tankObserver, 2);
                AssertFixtureDirectObservation(em, session, playerTank, baseObserver, tankObserver, 2);
            }
            finally
            {
                expanded.Phase = originalPhase;
                em.SetComponentData(session, expanded);
                // Preserve the pre-existing gridless local movement test contract.
                if (temporaryGrid != Entity.Null && em.Exists(temporaryGrid))
                    em.DestroyEntity(temporaryGrid);
            }
        }

        // This missing-prefab test measures the native base anchor and presented
        // order destination, not exploration. Establish its required known enemy base
        // through a real player acquisition observer without moving the measured base.
        private static void ObserveMeasuredBaseForPlayerFixture(World world, Entity session, Entity enemyBase)
        {
            EntityManager em = world.EntityManager;
            Assert.AreEqual(SkirmishSessionPhase.Playing,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase);
            var fog = em.GetComponentData<SkirmishFogStateComponent>(session);
            Assert.AreEqual(1, fog.SharedFog);
            Assert.AreEqual(0, fog.DevelopmentFullVision);
            Entity observer = CreateFixtureFogObserver(em, session, enemyBase, 1);
            Entity temporaryGrid = Entity.Null;
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (grids.IsEmptyIgnoreFilter)
            {
                temporaryGrid = em.CreateEntity(typeof(GridConfig));
                em.SetComponentData(temporaryGrid,
                    new GridConfig { Width = 4096, Height = 4096, CellSize = 1 });
            }
            try
            {
                Assert.AreEqual(1, grids.CalculateEntityCount());
                world.SetTime(new TimeData(world.Unmanaged.Time.ElapsedTime + 1, .1f));
                world.GetOrCreateSystem<SkirmishSharedFogUpdateSystem>().Update(world.Unmanaged);
                em.CompleteAllTrackedJobs();
                AssertFixtureDirectObservation(em, session, enemyBase, observer, observer, 1);
            }
            finally
            {
                if (temporaryGrid != Entity.Null && em.Exists(temporaryGrid))
                    em.DestroyEntity(temporaryGrid);
            }
        }

        private static Entity CreateFixtureFogObserver(EntityManager em, Entity session, Entity target, byte faction)
        {
            Assert.IsTrue(em.HasComponent<LocalTransform>(target));
            Entity observer = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),
                typeof(Faction), typeof(UnitHealth), typeof(UnitCombat), typeof(LocalTransform));
            em.SetComponentData(observer, new SkirmishAttemptOwnedComponent
            {
                SessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                StableObjectId = new FixedString64Bytes("s002.fixture.observer." + observer.Index),
                FactionId = faction
            });
            em.SetComponentData(observer, new Faction { Id = faction });
            em.SetComponentData(observer, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(observer, new UnitCombat { AggroRangeCells = 20 });
            em.SetComponentData(observer,
                LocalTransform.FromPosition(em.GetComponentData<LocalTransform>(target).Position));
            return observer;
        }

        private static void AssertFixtureDirectObservation(EntityManager em, Entity session,
            Entity target, Entity baseObserver, Entity tankObserver, byte faction)
        {
            Assert.IsTrue(SkirmishFogService.IsVisibleToFaction(em, target, faction));
            Assert.IsTrue(SkirmishFogService.TryGetKnowledge(em, target, faction, out var knowledge));
            Assert.AreEqual(SkirmishContactSight.Visible, knowledge.Sight);
            Assert.AreEqual(1, knowledge.ObservationKind);
            // Overlapping acquisition circles can legitimately select either source.
            // Require the exact native recorded source to be one of these real fixtures,
            // then independently verify its current native observation eligibility.
            Entity source = knowledge.ObservationSource;
            Assert.IsTrue(source == baseObserver || source == tankObserver);
            Assert.IsTrue(em.Exists(source));
            Assert.Greater(em.GetComponentData<UnitHealth>(source).Current, 0);
            Assert.IsFalse(em.HasComponent<Disabled>(source));
            Assert.AreEqual(faction, em.GetComponentData<Faction>(source).Id);
            var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(source);
            Assert.AreEqual(faction, owned.FactionId);
            Assert.AreEqual(0, owned.CapacityReleased);
            Assert.AreEqual(em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                owned.SessionId);
            Assert.AreEqual(owned.SessionId,
                em.GetComponentData<SkirmishAttemptOwnedComponent>(target).SessionId);
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            Assert.AreEqual(1, grids.CalculateEntityCount());
            float cellSize = grids.GetSingleton<GridConfig>().CellSize;
            float radius = math.max(0, em.GetComponentData<UnitCombat>(source).AggroRangeCells) * cellSize;
            if (cellSize > 0 && em.HasComponent<UnitAttack>(source))
                radius = math.max(radius,
                    math.ceil(math.max(0, em.GetComponentData<UnitAttack>(source).Range) / cellSize) * cellSize);
            Assert.Greater(radius, 0);
            float3 sourcePosition = em.GetComponentData<LocalTransform>(source).Position;
            float3 targetPosition = em.GetComponentData<LocalTransform>(target).Position;
            Assert.LessOrEqual(math.distancesq(sourcePosition.xz, targetPosition.xz), radius * radius);
        }

        private static void RevealAll(EntityManager em)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
                SkirmishFogService.Reveal(em, entities[i]);
        }
    }
}
