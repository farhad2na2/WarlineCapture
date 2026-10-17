#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Configs;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Transforms;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        static StreamWriter ground30MemoryWriter;
        static FileStream ground30MemoryStream;
        static string ground30MemorySize;
        static string ground30MemorySystems;
        static int ground30MemorySequence;

        // Explicit owned diagnostic only. No full30 entry, no implicit default mission/stage.
        public static void RunGroundObjective30MemoryDiagnostic()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && ground30MemoryWriter == null, "Idle unowned diagnostic required");
            string id = Environment.GetEnvironmentVariable("WARLINE_GROUND30_DIAGNOSTIC_MISSION");
            string size = Environment.GetEnvironmentVariable("WARLINE_GROUND30_DIAGNOSTIC_SIZE");
            string stage = Environment.GetEnvironmentVariable("WARLINE_GROUND30_DIAGNOSTIC_STAGE");
            string selector = Environment.GetEnvironmentVariable("WARLINE_GROUND30_DIAGNOSTIC_SYSTEMS");
            string output = Environment.GetEnvironmentVariable("WARLINE_GROUND30_DIAGNOSTIC_OUTPUT");
            Require(Ground30Ids.Contains(id) && new[]{"Standard","War","LargeWar"}.Contains(size), "Exact one canonical mission and size required");
            Require(new[]{"WorldOnly","LoadedSource","OneSize"}.Contains(stage), "Explicit bounded diagnostic stage required");
            Require(new[]{"Original","StreamingIntersection"}.Contains(selector), "Explicit original or intersected streaming system experiment required");
            string allowed = Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction")) + Path.DirectorySeparatorChar;
            Require(!string.IsNullOrWhiteSpace(output) && Path.IsPathRooted(output) && Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase) && !File.Exists(output), "Exclusive persistent diagnostic output required");
            var input = ReadGround30Envelope();
            var mission = ((JArray)input["missions"]).OfType<JObject>().Single(m => (string)m["catalog_id"] == id);
            var pin = (JObject)mission["source_candidate"];
            foreach (string pair in new[]{"sourcePath|sourceAssetSha256","gridPath|gridAssetSha256","surfacePath|surfaceAssetSha256"})
            { var fields=pair.Split('|'); Require(Hash((string)pin[fields[0]])==(string)pin[fields[1]], "Pinned diagnostic source bytes changed: "+fields[0]); }
            var source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)pin["sourcePath"]);
            Require(source != null && !EditorUtility.IsDirty(source) && source.SourceSceneReference.AssetGUID==(string)pin["sourceSceneGuid"] && source.NavigationMetadata.AuthoredSubSceneGuid==(string)pin["authoredSubSceneGuid"], "Exact clean diagnostic scene binding required");
            var companionPrewarm = Ground30PrewarmCompanions();
            var sceneOwnershipBefore = Ground30SceneOwnership();
            var before = CaptureMaterializationWorldCensus();
            var defaultBefore = World.DefaultGameObjectInjectionWorld;
            int scenes = UnityEngine.SceneManagement.SceneManager.sceneCount;
            int previews = UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            bool passed = false;
            using (var stream = new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using (var writer = new StreamWriter(stream,new System.Text.UTF8Encoding(false)))
            {
                ground30MemoryWriter=writer; ground30MemoryStream=stream; ground30MemorySize=size; ground30MemorySystems=selector; ground30MemorySequence=0;
                try
                {
                    var systems = new JArray();
                    foreach (var system in Ground30DiagnosticSystems())
                        systems.Add(TypeManager.GetSystemName(system).ToString());
                    Ground30MemoryCheckpoint("DiagnosticStarted",new JObject{["mission"]=id,["size"]=size,["stage"]=stage,["systemSelector"]=selector,["companionPrewarm"]=companionPrewarm,["inputSha256"]=Hash(GroundObjective30Inputs),["sourcePins"]=pin.DeepClone(),["streamingSystems"]=systems});
                    if (stage=="OneSize")
                    {
                        var report = new JObject{["sourcePath"]=pin["sourcePath"],["missions"]=new JArray()};
                        CollectGround30Source(new[]{mission},report);
                        Ground30MemoryCheckpoint("BoundedCollectionReturned",report);
                        var row=((JArray)report["missions"]).OfType<JObject>().Single();
                        var measured=((JArray)row["sizes"]).OfType<JObject>().Single(s=>(string)s["size"]==size);
                        Require(row["error"]==null && measured["error"]==null && (string)measured["status"]!="NotCollected", "One-size collection failed; raw row retained");
                    }
                    else
                    {
                        Ground30MemoryCheckpoint("BeforeWorld",null);
                        using(var world=StreamingWorld())
                        {
                            Ground30MemoryCheckpoint("WorldCreated",new JObject{["worldSequence"]=world.SequenceNumber});
                            if(stage=="LoadedSource")
                            {
                                Load(world,source.NavigationMetadata.AuthoredSubSceneGuid);
                                Ground30MemoryCheckpoint("SourceLoaded",null);
                                Load(world,AssetDatabase.AssetPathToGUID(Support));
                                Ground30MemoryCheckpoint("SupportLoaded",null);
                                using var surfaceOwner=PublishMissingPinnedSurface(world,source,(string)pin["surfacePath"],new JObject());
                                Ground30MemoryCheckpoint("LoadedQueryFacts",Ground30LoadedQueryFacts(world));
                            }
                        }
                        Ground30MemoryCheckpoint("WorldDisposed",null);
                    }
                    Require(before.SequenceEqual(CaptureMaterializationWorldCensus()) && ReferenceEquals(defaultBefore,World.DefaultGameObjectInjectionWorld) && scenes==UnityEngine.SceneManagement.SceneManager.sceneCount && previews==UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount && JToken.DeepEquals(sceneOwnershipBefore,Ground30SceneOwnership()), "Diagnostic cleanup census changed; no unrelated previews closed");
                    passed=true;
                }
                catch(Exception error) { Ground30MemoryCheckpoint("DiagnosticFailed",new JObject{["error"]=error.ToString()}); throw; }
                finally
                {
                    try { Ground30MemoryCheckpoint("DiagnosticCompleted",new JObject{["collected"]=passed,["sourceFitCertified"]=false,["playerReady"]=false}); }
                    finally { ground30MemoryWriter=null; ground30MemoryStream=null; ground30MemorySize=null; ground30MemorySystems=null; }
                }
            }
            Debug.Log("[SkirmishGroundObjective30MemoryDiagnostic] result=Collected mission="+id+" size="+size+" stage="+stage+" sourceFitCertified=0 output="+output);
        }

        static Type Ground30CompanionOwnerType()
        {
            var types=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Entities.CompanionGameObjectUtility",false)).Where(t=>t!=null).ToArray();
            Require(types.Length==1,"Exact installed CompanionGameObjectUtility owner required");
            return types[0];
        }

        static JObject Ground30PrewarmCompanions()
        {
            var before=Ground30SceneOwnership();
            var type=Ground30CompanionOwnerType();
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
            var method=type.GetMethod("GetCompanionScene",flags,null,new[]{typeof(bool)},null);
            Require(method!=null && method.ReturnType==typeof(UnityEngine.SceneManagement.Scene),"Installed companion scene initialization API changed");
            int validBefore=0;
            foreach(string name in new[]{"_companionScene","_companionSceneLiveConversion"})
            {
                var field=type.GetField(name,flags);
                Require(field!=null && field.FieldType==typeof(UnityEngine.SceneManagement.Scene),"Installed companion scene owner field changed");
                var scene=(UnityEngine.SceneManagement.Scene)field.GetValue(null);
                Require(scene.IsValid() || scene==default,"Invalid nondefault companion cache must not be recreated or ignored");
                if(scene.IsValid())validBefore++;
            }
            Require(validBefore==0 || validBefore==2,"Partial companion cache initialization must not replace a valid existing handle");
            // Invoke only the actual package initializer. It owns BOTH static caches
            // and their beforeAssemblyReload disposal; never close them ourselves.
            var initialized=(UnityEngine.SceneManagement.Scene)method.Invoke(null,new object[]{false});
            Require(initialized.IsValid(),"Actual package companion prewarm failed");
            var settingsTypes=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Scenes.Editor.LiveConversionEditorSettings",false)).Where(t=>t!=null).ToArray();
            Require(settingsTypes.Length==1,"Exact installed live-conversion settings owner required");
            var modeProperty=settingsTypes[0].GetProperty("LiveConversionMode",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
            Require(modeProperty!=null && modeProperty.CanRead,"Actual package live-conversion mode getter required");
            var mode=modeProperty.GetValue(null);
            Require(mode!=null && mode.GetType().IsEnum,"Actual package live-conversion enum required");
            var culling=type.GetMethod("UpdateLiveConversionCulling",flags,null,new[]{mode.GetType()},null);
            Require(culling!=null && culling.ReturnType==typeof(void),"Exact package culling initializer required");
            culling.Invoke(null,new[]{mode});
            var after=Ground30SceneOwnership();
            var owners=(JArray)after["companionOwners"];
            Require(owners.Count==2 && owners.All(o=>(bool)o["valid"]),"Both actual package cache handles required before baseline");
            Require((int)after["actualPreviewCount"]-(int)before["actualPreviewCount"]==(validBefore==0?2:0),"Unexpected package prewarm scene count change");
            if(validBefore==2)Require(JToken.DeepEquals(before["companionOwners"],after["companionOwners"]),"Existing package cache handles changed during prewarm");
            foreach(JObject oldScene in (JArray)before["scenes"])
            {
                var same=((JArray)after["scenes"]).OfType<JObject>().SingleOrDefault(s=>(string)s["handle"]==(string)oldScene["handle"]);
                Require(same!=null,"Prewarm removed a preexisting scene");
                var oldIdentity=(JObject)oldScene.DeepClone();var sameIdentity=(JObject)same.DeepClone();
                bool packageOwned=owners.Any(o=>(string)o["handle"]==(string)oldScene["handle"]);
                if(packageOwned){oldIdentity.Remove("name");sameIdentity.Remove("name");oldIdentity.Remove("cullingMask");sameIdentity.Remove("cullingMask");}
                Require(JToken.DeepEquals(oldIdentity,sameIdentity),"Prewarm changed a preexisting scene/root identity");
            }
            if(validBefore==0)foreach(JObject owner in owners)
                Require(((JArray)after["scenes"]).OfType<JObject>().Single(s=>(string)s["handle"]==(string)owner["handle"])["rootCount"].Value<int>()==0,"New package cache must be empty before baseline");
            return new JObject{["scope"]="ExplicitPackageCompanionInitializationBeforeOwnedBaseline",["ownerType"]=type.FullName,
                ["ownerAssembly"]=type.Assembly.FullName,["initializer"]=method.Name,["modeGetterOwner"]=settingsTypes[0].FullName,["actualMode"]=mode.ToString(),["actualModeNumeric"]=Convert.ToInt64(mode),["cullingInitializer"]=culling.Name,["before"]=before,["after"]=after};
        }

        static JObject Ground30SceneOwnership()
        {
            var scenes=new System.Collections.Generic.Dictionary<string,UnityEngine.SceneManagement.Scene>(StringComparer.Ordinal);
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            {var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);if(scene.IsValid())scenes[Ground30SceneHandle(scene)]=scene;}
            foreach(var root in Resources.FindObjectsOfTypeAll<GameObject>())
                if(root!=null && root.scene.IsValid())scenes[Ground30SceneHandle(root.scene)]=root.scene;
            var owners=new JArray();
            var type=Ground30CompanionOwnerType();
            foreach(string name in new[]{"_companionScene","_companionSceneLiveConversion"})
            {
                var field=type.GetField(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
                Require(field!=null && field.FieldType==typeof(UnityEngine.SceneManagement.Scene),"Installed companion owner schema changed");
                var scene=(UnityEngine.SceneManagement.Scene)field.GetValue(null);
                owners.Add(new JObject{["field"]=name,["valid"]=scene.IsValid(),["handle"]=Ground30SceneHandle(scene)});
                if(scene.IsValid())scenes[Ground30SceneHandle(scene)]=scene;
            }
            var rows=new JArray();int measuredPreviews=0;
            foreach(var scene in scenes.Values.OrderBy(Ground30SceneHandle,StringComparer.Ordinal))
            {
                bool preview=UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(scene);
                if(preview)measuredPreviews++;
                var roots=new JArray();
                if(scene.isLoaded)foreach(var root in scene.GetRootGameObjects().OrderBy(r=>r.GetEntityId().ToString(),StringComparer.Ordinal))
                    roots.Add(new JObject{["nativeId"]=root.GetEntityId().ToString(),["name"]=root.name,["activeSelf"]=root.activeSelf,
                        ["hideFlags"]=(int)root.hideFlags,["descendantCount"]=root.GetComponentsInChildren<Transform>(true).Length});
                rows.Add(new JObject{["handle"]=Ground30SceneHandle(scene),["name"]=scene.name,["path"]=scene.path,["loaded"]=scene.isLoaded,
                    ["preview"]=preview,["cullingMask"]=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene).ToString(System.Globalization.CultureInfo.InvariantCulture),["rootCount"]=scene.rootCount,["roots"]=roots});
            }
            int actualPreviews=UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
            Require(measuredPreviews==actualPreviews,"Unknown preview owner; no count allowance or arbitrary preview closure");
            return new JObject{["actualPreviewCount"]=actualPreviews,["measuredPreviewCount"]=measuredPreviews,["companionOwners"]=owners,["scenes"]=rows};
        }

        static string Ground30SceneHandle(UnityEngine.SceneManagement.Scene scene) => scene.handle.GetRawData().ToString(System.Globalization.CultureInfo.InvariantCulture);

        static System.Collections.Generic.IReadOnlyList<Type> Ground30DiagnosticSystems()
        {
            if (ground30MemorySystems=="Original")
                return DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming,true);
            Require(ground30MemorySystems=="StreamingIntersection", "Unknown diagnostic system selector");
            // Installed TypeManager ORs required flags into optional flags. Intersect
            // two independently filtered lists to require BOTH flags without that union.
            var editor=DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Editor,false);
            var selected=DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming,false).Where(editor.Contains).ToArray();
            Require(selected.Length>0 && !selected.Any(t=>t.FullName=="Unity.Rendering.EntitiesGraphicsSystem"), "Strict streaming selector includes unexpected presentation owner");
            return selected;
        }

        // Actual loaded bytes, not asset-file hashes substituted for runtime data.
        static JObject Ground30LoadedQueryFacts(World world)
        {
            var em=world.EntityManager;
            using var grids=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(GridRoad));
            using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
            using var databases=em.CreateEntityQuery(typeof(OperationMapRenderDatabaseComponent));
            using var transforms=em.CreateEntityQuery(typeof(LocalToWorld));
            using var parents=em.CreateEntityQuery(typeof(Parent));
            var colliderTypes=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Physics.PhysicsCollider",false)).Where(t=>t!=null).ToArray();
            Require(colliderTypes.Length<=1,"Ambiguous loaded native PhysicsCollider type");
            Type colliderType=colliderTypes.Length==1?colliderTypes[0]:null;
            int? physicsCount=null;
            if(colliderType!=null)
            {
                Require(typeof(IComponentData).IsAssignableFrom(colliderType),"Loaded PhysicsCollider is not an ECS component");
                using var physics=em.CreateEntityQuery(ComponentType.ReadOnly(colliderType));
                physicsCount=physics.CalculateEntityCount();
            }
            using var registry=em.CreateEntityQuery(typeof(UnitPrefabRegistryEntry));
            Require(grids.CalculateEntityCount()==1 && surfaces.CalculateEntityCount()==1,"Actual unique grid/surface facts required");
            var entity=grids.GetSingletonEntity(); var grid=em.GetComponentData<GridConfig>(entity);
            var walkable=em.GetBuffer<GridWalkable>(entity,true).AsNativeArray();
            var road=em.GetBuffer<GridRoad>(entity,true).AsNativeArray();
            var surface=surfaces.GetSingleton<MapSurfaceComponent>();
            Require(surface.HasSurfaceData!=0 && surface.SurfaceBlob.IsCreated,"Native surface blob missing");
            string surfaceHash=Ground30SemanticHash(w=>
            {
                ref var value=ref surface.SurfaceBlob.Value;
                void V(Unity.Mathematics.float3 v) {w.Write(v.x);w.Write(v.y);w.Write(v.z);}
                V(value.GridOrigin);w.Write(value.CellSize);w.Write(value.Dimensions.x);w.Write(value.Dimensions.y);
                w.Write((int)value.RuntimeEncoding);w.Write(value.CompactMinHeight);w.Write(value.CompactHeightStep);
                w.Write(value.Cells.Length);for(int i=0;i<value.Cells.Length;i++){var v=value.Cells[i];w.Write(v.FirstSurfaceIndex);w.Write(v.SurfaceCount);w.Write(v.InlineSurfaceIndex);}
                w.Write(value.Samples.Length);for(int i=0;i<value.Samples.Length;i++){var v=value.Samples[i];w.Write(v.Cell.x);w.Write(v.Cell.y);w.Write(v.SurfaceId);w.Write(v.LayerId);w.Write(v.Height);V(v.Normal);w.Write(v.SlopeDegrees);w.Write((int)v.SurfaceType);w.Write((int)v.MovementMask);w.Write((int)v.Flags);w.Write(v.FirstConnectionIndex);w.Write(v.ConnectionCount);}
                w.Write(value.Connections.Length);for(int i=0;i<value.Connections.Length;i++){var v=value.Connections[i];w.Write(v.FromSurfaceId);w.Write(v.ToSurfaceId);w.Write(v.Direction.x);w.Write(v.Direction.y);w.Write((int)v.ConnectionType);w.Write((int)v.MovementMask);}
                w.Write(value.CompactSamples.Length);for(int i=0;i<value.CompactSamples.Length;i++){var v=value.CompactSamples[i];w.Write(v.PackedHeight);w.Write(v.LayerId);w.Write((int)v.MovementMask);w.Write((int)v.Flags);w.Write(v.NormalX);w.Write(v.NormalY);w.Write(v.NormalZ);w.Write((int)v.SurfaceType);}
            });
            var prefabs=new System.Collections.Generic.List<string>();
            using(var owners=registry.ToEntityArray(Allocator.Persistent))
                foreach(var owner in owners) foreach(var entry in em.GetBuffer<UnitPrefabRegistryEntry>(owner,true))
                {
                    Require(em.Exists(entry.Prefab) && em.HasComponent<UnitSourcePrefabKey>(entry.Prefab),"Registry prefab/source key absent");
                    string key=em.GetComponentData<UnitSourcePrefabKey>(entry.Prefab).Value.ToString();
                    bool hasFootprint=em.HasComponent<UnitFootprint>(entry.Prefab);
                    bool hasBehavior=em.HasComponent<UnitMovementBehavior>(entry.Prefab);
                    bool hasAir=em.HasComponent<UnitAirMovement>(entry.Prefab);
                    var footprint=hasFootprint?em.GetComponentData<UnitFootprint>(entry.Prefab):default;
                    var behavior=hasBehavior?em.GetComponentData<UnitMovementBehavior>(entry.Prefab):default;
                    var air=hasAir?em.GetComponentData<UnitAirMovement>(entry.Prefab):default;
                    Require(!float.IsNaN(air.CruiseHeight)&&!float.IsInfinity(air.CruiseHeight)&&!float.IsNaN(air.RunwayTaxiSpeed)&&!float.IsInfinity(air.RunwayTaxiSpeed),"Nonfinite native air movement census");
                    prefabs.Add(new JObject{["key"]=key,["hasFootprint"]=hasFootprint,["footprintX"]=footprint.Size.x,["footprintY"]=footprint.Size.y,
                        ["hasMovementBehavior"]=hasBehavior,["allowIdleWander"]=behavior.AllowIdleWander,["usesVehicleMotion"]=behavior.UsesVehicleMotion,
                        ["hasAirMovement"]=hasAir,["cruiseHeight"]=air.CruiseHeight,["runwayTaxiSpeed"]=air.RunwayTaxiSpeed}.ToString(Newtonsoft.Json.Formatting.None));
                }
            prefabs.Sort(StringComparer.Ordinal);
            var packed=new System.Collections.Generic.List<string>();
            using(var owners=databases.ToEntityArray(Allocator.Persistent)) foreach(var owner in owners)
            {
                var blob=em.GetComponentData<OperationMapRenderDatabaseComponent>(owner).Blob;
                Require(blob.IsCreated,"Packed geometry database blob absent");
                packed.Add("prototypes="+blob.Value.Prototypes.Length+"|placements="+blob.Value.Placements.Length);
            }
            packed.Sort(StringComparer.Ordinal);
            var geometry=new BuildingPlacementAuthoredBuildingCache(); geometry.Ensure(em,grid);
            Require(geometry.IsReady,"Actual source geometry mask unavailable");
            var mask=new bool[checked(grid.Width*grid.Height)]; geometry.AppendTo(mask);
            string maskHash=Ground30SemanticHash(w=>{w.Write(mask.Length);foreach(bool value in mask)w.Write(value);});
            string walkHash=Ground30SemanticHash(w=>{w.Write(walkable.Length);for(int i=0;i<walkable.Length;i++)w.Write(walkable[i].Value);});
            string roadHash=Ground30SemanticHash(w=>{w.Write(road.Length);for(int i=0;i<road.Length;i++)w.Write(road[i].Value);});
            return new JObject{["gridWidth"]=grid.Width,["gridHeight"]=grid.Height,["cellSize"]=grid.CellSize,
                ["origin"]=new JArray(grid.Origin.x,grid.Origin.y,grid.Origin.z),["walkableSha256"]=walkHash,["roadSha256"]=roadHash,
                ["surfaceSemanticSha256"]=surfaceHash,["geometryMaskSha256"]=maskHash,["packedDatabaseCounts"]=new JArray(packed),
                ["prefabKeyFootprints"]=new JArray(prefabs),["localToWorldCount"]=transforms.CalculateEntityCount(),
                ["parentCount"]=parents.CalculateEntityCount(),["physicsColliderTypeAvailable"]=colliderType!=null,
                ["physicsColliderAssemblyQualifiedName"]=colliderType?.AssemblyQualifiedName??"",
                ["physicsColliderCount"]=physicsCount.HasValue?(JToken)physicsCount.Value:JValue.CreateNull(),
                ["physicsCensusScope"]=colliderType==null?"DOTSPhysicsNotApplicable_AbsentLoadedComponent_NotConsumedByGround30Queries":"LoadedEcsTypeCountOnly_NoColliderValueEquivalence",
                ["comparisonScope"]="ConsumedGridSurfaceGeometryMaskAndRosterFacts_TransformPhysicsCountsOnly",
                ["equivalenceEstablished"]=false,["remainingEquivalenceGates"]="CompareOneSizeActualFacilityMetadataPackingAndRouteRows;TransformValuesNotCompared;OptionalDOTSPhysicsCensusIsNotAMissionGate"};
        }

        static string Ground30SemanticHash(Action<System.IO.BinaryWriter> write)
        {
            using var digest=System.Security.Cryptography.SHA256.Create();
            using var stream=new System.Security.Cryptography.CryptoStream(Stream.Null,digest,System.Security.Cryptography.CryptoStreamMode.Write);
            using var buffered=new BufferedStream(stream,128*1024);
            using var writer=new System.IO.BinaryWriter(buffered,System.Text.Encoding.UTF8,true);
            write(writer);writer.Flush();buffered.Flush();stream.FlushFinalBlock();
            return BitConverter.ToString(digest.Hash).Replace("-","").ToLowerInvariant();
        }

        static void Ground30MemoryCheckpoint(string phase,JObject facts)
        {
            if (ground30MemoryWriter==null) return;
            using var process=System.Diagnostics.Process.GetCurrentProcess();
            process.Refresh();
            var row=new JObject{["schema"]=1,["sequence"]=++ground30MemorySequence,["phase"]=phase,["utc"]=DateTime.UtcNow.ToString("O"),
                ["workingSetBytes"]=process.WorkingSet64,["privateBytes"]=process.PrivateMemorySize64,["managedBytes"]=GC.GetTotalMemory(false),
                ["unityAllocatedBytes"]=Profiler.GetTotalAllocatedMemoryLong(),["unityReservedBytes"]=Profiler.GetTotalReservedMemoryLong(),
                ["unityUnusedReservedBytes"]=Profiler.GetTotalUnusedReservedMemoryLong(),["monoUsedBytes"]=Profiler.GetMonoUsedSizeLong(),
                ["monoHeapBytes"]=Profiler.GetMonoHeapSizeLong(),["worlds"]=new JArray(CaptureMaterializationWorldCensus()),
                ["scenes"]=UnityEngine.SceneManagement.SceneManager.sceneCount,["previewScenes"]=UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount,
                ["sourceFitCertified"]=false,["sceneOwnership"]=Ground30SceneOwnership(),["facts"]=facts?.DeepClone()};
            ground30MemoryWriter.WriteLine(row.ToString(Newtonsoft.Json.Formatting.None));
            ground30MemoryWriter.Flush();
            ground30MemoryStream.Flush(true);
            Debug.Log("[SkirmishGroundObjective30MemoryPhase] sequence="+ground30MemorySequence+" phase="+phase+" privateBytes="+process.PrivateMemorySize64);
        }
    }
}
#endif
