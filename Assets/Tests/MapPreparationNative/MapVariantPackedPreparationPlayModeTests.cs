using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Rendering;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Profiling;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.Profiling;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Hash128 = Unity.Entities.Hash128;
using Object = UnityEngine.Object;

public sealed class MapVariantPackedPreparationPlayModeTests
{
    [UnityTest, Timeout(2400000)]
    public IEnumerator Frontier_PackedFullMapLoadReloadDamageAndActualUnitRoutes()
    {
        Assert.That(Application.isEditor, Is.False, "Frontier acceptance requires the StandaloneOSX test player.");
        yield return Run("Frontier");
        Debug.Log("[MapVariantFrontierRuntime] result=Passed fullPlayable=2048x1024 scope=DesktopPackedRuntime");
    }

    [UnityTest, Timeout(850000)]
    public IEnumerator MediumCandidates_PackedLoadReloadDamageAndActualUnitRoutes()
    {
        Assert.That(Application.isEditor,Is.False,"Packed acceptance requires the StandaloneOSX test player; Editor SceneSystem resolves Editor artifacts.");
        foreach (string map in new[] { "RefineryDistrict", "CityEdgeAirfield", "AshLinePort" }) yield return Run(map);
        Debug.Log("[MapVariantPackedRuntime] result=Passed maps=3 reload=Passed independentDamage=Passed actualUnits=Passed scope=DesktopPackedRuntime");
    }
    [UnityTest, Timeout(600000)]
    public IEnumerator PreparedVariants_SwitchWithExistingDenseCity()
        => SwitchWithExistingDenseCity(
            new[]{"RefineryDistrict","ExistingDenseCity","CityEdgeAirfield","ExistingDenseCity","AshLinePort","ExistingDenseCity","RefineryDistrict"},
            "existing-map-switch-result.json", "MapVariantSwitch");

    [UnityTest, Timeout(600000)]
    public IEnumerator Frontier_SwitchWithExistingDenseCity()
        => SwitchWithExistingDenseCity(
            new[]{"Frontier","ExistingDenseCity","Frontier"},
            "frontier-switch-result.json", "MapVariantFrontierSwitch");

    private static IEnumerator SwitchWithExistingDenseCity(string[] maps, string resultFile, string marker)
    {
        Assert.That(Application.isEditor,Is.False,"Packed switching requires the StandaloneOSX test player.");
        var world = World.DefaultGameObjectInjectionWorld;
        Assert.That(world,Is.Not.Null);
        var em = world.EntityManager;
        foreach (string map in maps)
        {
            string path = "Design/MapVariants/Preparation/" + map + (map == "ExistingDenseCity" ? "/runtime-content.json" : "/Candidate/runtime-content.json");
            var packed = JsonUtility.FromJson<Packed>(File.ReadAllText(Resolve(path)));
            RuntimeContentManager.Cleanup(out _); RuntimeContentManager.Initialize();
            var initialization=Addressables.InitializeAsync(false); yield return initialization;
            Assert.That(initialization.Status,Is.EqualTo(AsyncOperationStatus.Succeeded));
            Addressables.Release(initialization); Addressables.ClearResourceLocators();
            foreach(var provider in Addressables.ResourceManager.ResourceProviders.OfType<ContentCatalogProvider>()) provider.DisableCatalogUpdateOnStart=false;
            Assert.That(RuntimeContentManager.LoadLocalCatalogData(Resolve(packed.entityCatalogPath), RuntimeContentManager.DefaultContentFileNameFunc,
                file => Path.Combine(Resolve(packed.entityContentPath),RuntimeContentManager.DefaultArchivePathFunc(file))),Is.True);
            var catalog = Addressables.LoadContentCatalogAsync(Resolve(packed.addressablesCatalogPath),false,"preparation-"+packed.addressablesCatalogHash);
            yield return catalog; Assert.That(catalog.Status,Is.EqualTo(AsyncOperationStatus.Succeeded));
            var definition = Addressables.LoadAssetAsync<OperationMapDefinition>(packed.definitionAddress);
            yield return definition;
            GameObject switchCamera = null; RenderTexture switchTarget = null;
            int baselineLights = ActiveLightCount(), baselineGeometry = ActiveGameObjectGeometryCount();
            Scene previousActiveScene=SceneManager.GetActiveScene();
            try
            {
                Assert.That(definition.Status,Is.EqualTo(AsyncOperationStatus.Succeeded));
                Assert.That(definition.Result.ContentHash,Is.EqualTo(packed.semanticHash));
                switchCamera = new GameObject("SwitchRuntimeDiagnosticCamera",typeof(Camera)); switchCamera.tag="MainCamera";
                var camera=switchCamera.GetComponent<Camera>();
                var view=definition.Result.Cameras.ToArray().Single(c=>c.CameraId==definition.Result.BattleCameraId);
                camera.transform.SetPositionAndRotation(view.Position,Quaternion.Euler(view.EulerAngles)); camera.fieldOfView=view.FieldOfView; camera.farClipPlane=4000;
                switchTarget=new RenderTexture(1920,1080,24);camera.targetTexture=switchTarget;
                using var loader = new OperationMapSceneLoadingSceneSystemHelper();
                Assert.That(loader.TryStart(definition.Result,out string error),Is.True,error);
                float deadline = Time.realtimeSinceStartup + 120;
                while (!loader.IsReady && !loader.HasFailed && Time.realtimeSinceStartup < deadline) { loader.Update(); yield return null; }
                Assert.That(loader.IsReady,Is.True,loader.Failure ?? "Switch load timeout");
                Entity loadedSource=SceneSystem.GetSceneEntity(world.Unmanaged,new Hash128(packed.entitySceneGuid));
                while(!SceneSystem.IsSceneLoaded(world.Unmanaged,loadedSource) && Time.realtimeSinceStartup<deadline) { yield return null;loadedSource=SceneSystem.GetSceneEntity(world.Unmanaged,new Hash128(packed.entitySceneGuid)); }
                Assert.That(SceneSystem.IsSceneLoaded(world.Unmanaged,loadedSource),Is.True,"Switch packed entity scene did not load");
                Assert.That(OperationMapEntityPresentationReadinessUtility.TryValidate(em,loadedSource,definition.Result.OperationMapId,definition.Result.RenderResidencyMode,out error),Is.True,error);
                Assert.That(SceneManager.SetActiveScene(loader.SceneView.gameObject.scene),Is.True);
                using var buildings = em.CreateEntityQuery(typeof(OperationMapBuildingIdentity));
                Assert.That(buildings.CalculateEntityCount(),Is.GreaterThan(0));
                using var grids = em.CreateEntityQuery(typeof(GridConfig)); Assert.That(grids.CalculateEntityCount(),Is.EqualTo(1));
                Entity source = SceneSystem.GetSceneEntity(world.Unmanaged,new Hash128(packed.entitySceneGuid));
                Assert.That(SceneManager.SetActiveScene(previousActiveScene),Is.True);
                Assert.That(loader.TryBeginUnload(out error),Is.True,error);
                deadline = Time.realtimeSinceStartup + 120;
                while (!loader.UnloadComplete && !loader.HasFailed && Time.realtimeSinceStartup < deadline) { loader.Update(); yield return null; }
                Assert.That(loader.UnloadComplete,Is.True,loader.Failure ?? "Switch unload timeout");
                Assert.That(em.Exists(source),Is.False); Assert.That(buildings.CalculateEntityCount(),Is.Zero); Assert.That(grids.CalculateEntityCount(),Is.Zero);
                for(int frame=0;frame<10;frame++) yield return null;
                Assert.That(ActiveLightCount(),Is.EqualTo(baselineLights),"Switch lighting leaked");
                Assert.That(ActiveGameObjectGeometryCount(),Is.EqualTo(baselineGeometry),"Switch source geometry leaked");
                Debug.Log("[MapVariantSwitch] map=" + map + " load=Passed unload=Passed grids=Single");
            }
            finally
            {
                if(previousActiveScene.IsValid() && previousActiveScene.isLoaded) SceneManager.SetActiveScene(previousActiveScene);
                if(switchCamera!=null) Object.Destroy(switchCamera);
                if(switchTarget!=null) { switchTarget.Release();Object.Destroy(switchTarget); }
                if(definition.IsValid()) Addressables.Release(definition);
                if(catalog.IsValid()) { if(catalog.Status==AsyncOperationStatus.Succeeded) Addressables.RemoveResourceLocator(catalog.Result); Addressables.Release(catalog); }
                RuntimeContentManager.Cleanup(out _);
            }
        }
        File.WriteAllText(Resolve("Design/MapVariants/Preparation/Evidence/"+resultFile),
            "{\"result\":\"Passed\",\"scope\":\"StandaloneOSX native packed switching\",\"loads\":"+maps.Length+"}");
        Debug.Log("["+marker+"] result=Passed variantToExisting=Passed existingToVariant=Passed loads="+maps.Length);
    }

    private static IEnumerator Run(string map)
    {
        string reportRoot = Resolve("Design/MapVariants/Preparation/" + map + "/Candidate");
        var packed = JsonUtility.FromJson<Packed>(File.ReadAllText(reportRoot + "/runtime-content.json"));
        AssertPackagedEntitySceneFiles(packed);
        Assert.That(FileHash(Resolve(packed.entityCatalogPath)),Is.EqualTo(packed.entityCatalogHash));
        Assert.That(FileHash(Resolve(packed.addressablesCatalogPath)),Is.EqualTo(packed.addressablesCatalogHash));
        var candidate = JsonUtility.FromJson<Candidate>(File.ReadAllText(reportRoot + "/output-manifest.json"));
        Assert.That(packed.semanticHash, Is.EqualTo(candidate.semanticHash));
        var result = new RuntimeEvidence { map = map, contentHash = candidate.semanticHash, target = packed.target, device = SystemInfo.deviceModel, scope = "StandaloneOSX native packed route fixture; fuel disabled; moving and 120 idle frame diagnostics; no player/Android device acceptance", result = "Failed gate", graphicsAPI = SystemInfo.graphicsDeviceType.ToString(), gpu = SystemInfo.graphicsDeviceName, operatingSystem = SystemInfo.operatingSystem, unityVersion = Application.unityVersion, qualityLevel = QualitySettings.names[QualitySettings.GetQualityLevel()], captureResolution = "1920x1080", entityCatalogHash=packed.entityCatalogHash, addressablesCatalogHash=packed.addressablesCatalogHash, bindingHash=packed.bindingHash, scriptingBackend="Mono" };
        AsyncOperationHandle<IResourceLocator> catalog = default;
        AsyncOperationHandle<OperationMapDefinition> definition = default;
        Entity fixtureScene = Entity.Null;
        GameObject diagnosticCamera = null; RenderTexture diagnosticTarget = null;
        World world = World.DefaultGameObjectInjectionWorld;
        Assert.That(world, Is.Not.Null);
        EntityManager em = world.EntityManager;
        float originalScale = Time.timeScale;
        Scene previousActiveScene=SceneManager.GetActiveScene();
        using var gameplayQuery = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
        Entity gameplay = gameplayQuery.IsEmptyIgnoreFilter ? em.CreateEntity(typeof(RuntimeGameplayStateComponent)) : gameplayQuery.GetSingletonEntity();
        var originalGameplay = em.GetComponentData<RuntimeGameplayStateComponent>(gameplay);
        try
        {
            RuntimeContentManager.Cleanup(out _); RuntimeContentManager.Initialize();
            var initialization=Addressables.InitializeAsync(false); yield return initialization;
            Assert.That(initialization.Status,Is.EqualTo(AsyncOperationStatus.Succeeded));
            Addressables.Release(initialization); Addressables.ClearResourceLocators();
            foreach(var provider in Addressables.ResourceManager.ResourceProviders.OfType<ContentCatalogProvider>()) provider.DisableCatalogUpdateOnStart=false;
            Assert.That(RuntimeContentManager.LoadLocalCatalogData(Resolve(packed.entityCatalogPath), RuntimeContentManager.DefaultContentFileNameFunc,
                file => Path.Combine(Resolve(packed.entityContentPath), RuntimeContentManager.DefaultArchivePathFunc(file))), Is.True);
            catalog = Addressables.LoadContentCatalogAsync(Resolve(packed.addressablesCatalogPath), false,"preparation-"+packed.addressablesCatalogHash);
            yield return catalog; Assert.That(catalog.Status, Is.EqualTo(AsyncOperationStatus.Succeeded), catalog.OperationException?.Message);
            definition = Addressables.LoadAssetAsync<OperationMapDefinition>(packed.definitionAddress);
            yield return definition; Assert.That(definition.Status, Is.EqualTo(AsyncOperationStatus.Succeeded), definition.OperationException?.Message);
            diagnosticCamera = new GameObject("PreparedRuntimeDiagnosticCamera", typeof(Camera));
            diagnosticCamera.tag = "MainCamera";
            var camera = diagnosticCamera.GetComponent<Camera>();
            var view = definition.Result.Cameras.ToArray().Single(c => c.CameraId == definition.Result.BattleCameraId);
            camera.transform.SetPositionAndRotation(view.Position,Quaternion.Euler(view.EulerAngles));
            camera.fieldOfView=view.FieldOfView; camera.farClipPlane=4000;
            diagnosticTarget = new RenderTexture(1920,1080,24); camera.targetTexture=diagnosticTarget;
            int baselineLights = ActiveLightCount(); int baselineGeometry = ActiveGameObjectGeometryCount();
            for (int cycle = 0; cycle < 2; cycle++)
            {
                using var loader = new OperationMapSceneLoadingSceneSystemHelper();
                float started = Time.realtimeSinceStartup;
                Assert.That(loader.TryStart(definition.Result, out string error), Is.True, error);
                float deadline = started + 120;
                while (!loader.IsReady && !loader.HasFailed && Time.realtimeSinceStartup < deadline) { loader.Update(); yield return null; }
                Assert.That(loader.IsReady, Is.True, loader.Failure ?? "Packed load timed out");
                Assert.That(SceneManager.SetActiveScene(loader.SceneView.gameObject.scene),Is.True);
                result.loadSeconds.Add(Time.realtimeSinceStartup - started);
                Assert.That(ActiveLightCount(),Is.EqualTo(baselineLights+1),"Map must have one active lighting owner");
                Assert.That(ActiveGameObjectGeometryCount(),Is.EqualTo(baselineGeometry),"Source GameObject geometry coexists with entity presentation");
                using var ownersQuery = em.CreateEntityQuery(typeof(OperationMapBuildingIdentity), typeof(OperationMapBuildingPresentation));
                using (var owners = ownersQuery.ToEntityArray(Allocator.Temp))
                {
                    Assert.That(owners.Length, Is.EqualTo(candidate.owners.Count));
                    foreach (var owner in owners)
                    {
                        var identity = em.GetComponentData<OperationMapBuildingIdentity>(owner);
                        var expected = candidate.owners.Single(o => o.stableId == identity.StableId.ToString());
                        Assert.That(math.distance(em.GetComponentData<LocalTransform>(owner).Position, (float3)expected.position), Is.LessThan(.01f));
                        Assert.That(em.GetComponentData<OperationMapBuildingPresentation>(owner).State, Is.Zero, "Reload retained destruction.");
                    }
                }
                for (int frame = 0; frame < 10; frame++) yield return null;
                using var graphics = em.CreateEntityQuery(typeof(MaterialMeshInfo));
                Assert.That(graphics.CalculateEntityCount(), Is.GreaterThan(candidate.owners.Count));
                result.renderEntityCount = graphics.CalculateEntityCount();
                if (cycle == 0)
                {
                    Capture(candidate, reportRoot + "/battle-native-runtime.png");
                    Capture(candidate, reportRoot + "/topdown-native-runtime.png", topDown:true);
                    if(map=="AshLinePort") Capture(candidate,reportRoot+"/bridge-native-runtime.png",new float3(680,.08f,505));
                    Entity damaged;
                    using (var ownerList = ownersQuery.ToEntityArray(Allocator.Temp))
                    {
                        damaged = ownerList.First(e => candidate.owners.Single(o => o.stableId == em.GetComponentData<OperationMapBuildingIdentity>(e).StableId.ToString()).sourceGuid == "c4196f14c6e6e4b739aaf457686e2523");
                    }
                    Capture(candidate, reportRoot + "/intact-native-runtime.png", em.GetComponentData<LocalTransform>(damaged).Position);
                    var health = em.GetComponentData<UnitHealth>(damaged); health.Current = 0; em.SetComponentData(damaged, health);
                    for (int frame = 0; frame < 5; frame++) yield return null;
                    using (var owners = ownersQuery.ToEntityArray(Allocator.Temp))
                    {
                        foreach (var owner in owners) Assert.That(em.GetComponentData<OperationMapBuildingPresentation>(owner).State, Is.EqualTo(owner == damaged ? 1 : 0));
                    }
                    Assert.That(em.HasComponent<StaticGridBlocker>(damaged), Is.True);
                    Capture(candidate, reportRoot + "/damage-native-runtime.png", em.GetComponentData<LocalTransform>(damaged).Position);
                    fixtureScene = SceneSystem.LoadSceneAsync(world.Unmanaged, new Hash128(packed.unitFixtureSceneGuid));
                    deadline = Time.realtimeSinceStartup + 120;
                    while (!SceneSystem.IsSceneLoaded(world.Unmanaged, fixtureScene) && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.That(SceneSystem.IsSceneLoaded(world.Unmanaged, fixtureScene), Is.True);
                    Time.timeScale = 4;
                    var playing = originalGameplay; playing.PlayRequested = 1; playing.SimulationActive = 1; em.SetComponentData(gameplay, playing);
                    using var fixtureQuery = em.CreateEntityQuery(typeof(MapPreparationUnitFixtureComponent));
                    var units = fixtureQuery.GetSingleton<MapPreparationUnitFixtureComponent>();
                    foreach (var route in Routes(map))
                    {
                        foreach (var kind in new[] { ("Infantry", units.InfantryPrefab), ("Hauler", units.HaulerPrefab), ("Armor", units.ArmorPrefab) })
                        {
                            yield return MoveActualUnit(em, candidate, kind.Item1, kind.Item2, route.Item1, route.Item2, route.Item3, result, reportRoot);
                            // Dynamic occupancy is rebuilt on subsequent simulation frames.
                            // Do not path the next unit through the previous unit's stale footprint.
                            yield return null; yield return null;
                        }
                    }
                    yield return RejectInvalidDestination(em, candidate, units.InfantryPrefab, "OutsidePlayable", map == "Frontier" ? new int2(-50,-50) : new int2(0,0), result);
                    if(map == "AshLinePort") yield return RejectInvalidDestination(em,candidate,units.InfantryPrefab,"CanalWithoutCrossing",new int2(680,445),result);
                    Time.timeScale = originalScale; em.SetComponentData(gameplay, originalGameplay);
                    SceneSystem.UnloadScene(world.Unmanaged, fixtureScene, SceneSystem.UnloadParameters.DestroyMetaEntities);
                    deadline = Time.realtimeSinceStartup + 30;
                    while (em.Exists(fixtureScene) && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.That(em.Exists(fixtureScene), Is.False); fixtureScene = Entity.Null;
                    using (var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread"))
                    using (var render = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread"))
                    using (var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
                    for (int frame = 0; frame < 120; frame++)
                    {
                        result.frameMilliseconds.Add(Time.unscaledDeltaTime * 1000);
                        if (main.Valid && main.LastValue > 0) result.mainThreadMilliseconds.Add(main.LastValue / 1000000f);
                        if (render.Valid && render.LastValue > 0) result.renderThreadMilliseconds.Add(render.LastValue / 1000000f);
                        if (draws.Valid && draws.LastValue > 0) result.drawCalls.Add(draws.LastValue);
                        yield return null;
                    }
                    result.memoryBytes = Profiler.GetTotalAllocatedMemoryLong();
                }
                Entity sceneEntity = SceneSystem.GetSceneEntity(world.Unmanaged, new Hash128(packed.entitySceneGuid));
                Assert.That(SceneManager.SetActiveScene(previousActiveScene),Is.True);
                Assert.That(loader.TryBeginUnload(out error), Is.True, error);
                deadline = Time.realtimeSinceStartup + 120;
                while (!loader.UnloadComplete && !loader.HasFailed && Time.realtimeSinceStartup < deadline) { loader.Update(); yield return null; }
                Assert.That(loader.UnloadComplete, Is.True, loader.Failure ?? "Unload timed out");
                Assert.That(em.Exists(sceneEntity), Is.False);
                Assert.That(ownersQuery.CalculateEntityCount(), Is.Zero, "Building entities leaked after unload");
                for (int frame = 0; frame < 10; frame++) yield return null;
                result.unloadedMaterialCounts.Add(Resources.FindObjectsOfTypeAll<Material>().Length);
                Assert.That(ActiveLightCount(),Is.EqualTo(baselineLights),"Lighting leaked after unload");
                Assert.That(ActiveGameObjectGeometryCount(),Is.EqualTo(baselineGeometry),"Source GameObject geometry leaked after unload");
                Assert.That(loader.SceneView == null || !loader.SceneView.gameObject.scene.isLoaded, Is.True, "Binding scene/lighting ownership leaked");
            }
            Assert.That(result.unloadedMaterialCounts[1], Is.LessThanOrEqualTo(result.unloadedMaterialCounts[0]), "Material count grew across identical reload cycles");
            result.result = "Passed";
            Debug.Log($"[MapVariantPackedRuntime] result=Passed map={map} owners={candidate.owners.Count} reload=Passed actualUnitRoutes={result.routes.Count} contentHash={candidate.semanticHash}");
        }
        finally
        {
            if(previousActiveScene.IsValid() && previousActiveScene.isLoaded) SceneManager.SetActiveScene(previousActiveScene);
            if(diagnosticCamera!=null) Object.Destroy(diagnosticCamera);
            if(diagnosticTarget!=null) { diagnosticTarget.Release();Object.Destroy(diagnosticTarget); }
            Time.timeScale = originalScale;
            if (em.Exists(gameplay)) em.SetComponentData(gameplay, originalGameplay);
            if (fixtureScene != Entity.Null && em.Exists(fixtureScene)) SceneSystem.UnloadScene(world.Unmanaged, fixtureScene, SceneSystem.UnloadParameters.DestroyMetaEntities);
            if (definition.IsValid()) Addressables.Release(definition);
            if (catalog.IsValid()) { if (catalog.Status == AsyncOperationStatus.Succeeded) Addressables.RemoveResourceLocator(catalog.Result); Addressables.Release(catalog); }
            RuntimeContentManager.Cleanup(out _);
            File.WriteAllText(reportRoot + "/packed-runtime-evidence.json", JsonUtility.ToJson(result, true));
        }
    }
    private static IEnumerable<(string, int2, int2)> Routes(string map)
    {
        if (map == "RefineryDistrict")
        {
            yield return ("RefineryGate", new int2(555,505), new int2(716,516));
            // The depot authored footprint ends at Z=494; verify exit to the
            // road beyond it instead of pathing into the separate substation.
            yield return ("DepotExit", new int2(941,446), new int2(941,540));
            // The authored SplitFront_Ridge zone is centered at 760,550. The
            // launcher compound at 955,679 is a separate later mission anchor.
            yield return ("RidgeAccess", new int2(716,516), new int2(760,550));
        }
        else if (map == "CityEdgeAirfield")
        {
            yield return ("CityGate", new int2(705,680), new int2(705,600));
            yield return ("HelipadApproach", new int2(730,600), new int2(780,648));
            yield return ("HospitalAccess", new int2(780,648), new int2(875,650));
        }
        else if(map == "Frontier")
        {
            yield return ("CanalBridge",new int2(324,529),new int2(404,529));
            yield return ("RefineryAccess",new int2(874,529),new int2(1124,529));
            yield return ("AirfieldAccess",new int2(1594,529),new int2(1834,464));
            yield return ("FullWidthHighway",new int2(144,529),new int2(1974,529));
        }
        else
        {
            yield return ("MainBridge", new int2(640,505), new int2(720,505));
            yield return ("SouthBridge", new int2(640,385), new int2(720,385));
            yield return ("EastCheckpoint", new int2(720,505), new int2(980,505));
        }
    }
    private static IEnumerator MoveActualUnit(EntityManager em, Candidate candidate, string kind, Entity prefab, string route,
        int2 requestedStart, int2 requestedGoal, RuntimeEvidence result, string reportRoot)
    {
        using var gridQuery = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable));
        using var surfaceQuery = em.CreateEntityQuery(typeof(MapSurfaceComponent));
        Entity gridEntity = gridQuery.GetSingletonEntity(); var grid = em.GetComponentData<GridConfig>(gridEntity);
        var surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
        var size = em.GetComponentData<UnitFootprint>(prefab).Size;
        var movementMask = kind == "Infantry" ? MapSurfaceMovementMask.Infantry : kind == "Armor" ? MapSurfaceMovementMask.TrackedVehicle : MapSurfaceMovementMask.WheeledVehicle;
        int2 start = ClearApproach(em, gridEntity, surface, requestedStart, size, candidate, movementMask);
        int2 goal = ClearApproach(em, gridEntity, surface, requestedGoal, size, candidate, movementMask);
        var evidence = new RouteEvidence { name = route, unit = kind, requestedStart = requestedStart, requestedGoal = requestedGoal, start = start, goal = goal, footprint = size, result = "Failed" };
        result.routes.Add(evidence);
        Entity unit = em.Instantiate(prefab);
        try
        {
            var transform = em.GetComponentData<LocalTransform>(unit);
            int index = start.x + start.y * 2048;
            Assert.That(MapSurfaceBlobAccess.TryGetSurfaceByIndex(ref surface.SurfaceBlob.Value, index, out var sample), Is.True);
            transform.Position = new float3(start.x + .5f, sample.Height + .1f, start.y + .5f); em.SetComponentData(unit, transform);
            em.SetComponentData(unit, new UnitGrid { Cell = start });
            var behavior = em.GetComponentData<UnitMovementBehavior>(unit); behavior.AllowIdleWander = 0; em.SetComponentData(unit, behavior);
            if (em.HasComponent<UnitFuelConsumption>(unit)) { var fuel = em.GetComponentData<UnitFuelConsumption>(unit); fuel.Enabled = 0; em.SetComponentData(unit, fuel); }
            if (em.HasComponent<Faction>(unit)) em.SetComponentData(unit, new Faction { Id = 1 });
            Assert.That(UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, unit, goal), Is.True);
            using var movingMain = ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread");
            using var movingRender = ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Render Thread");
            using var movingDraws = ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");
            float started = Time.realtimeSinceStartup, deadline = started + (candidate.playableSize.x > 600 ? 600 : 90);
            float distance = 0; float3 previous = transform.Position; bool captured = false;
            while (Time.realtimeSinceStartup < deadline && em.Exists(unit))
            {
                yield return null;
                result.movingFrameMilliseconds.Add(Time.unscaledDeltaTime*1000);
                if(movingMain.Valid && movingMain.LastValue>0)result.movingMainThreadMilliseconds.Add(movingMain.LastValue/1000000f);
                if(movingRender.Valid && movingRender.LastValue>0)result.movingRenderThreadMilliseconds.Add(movingRender.LastValue/1000000f);
                if(movingDraws.Valid && movingDraws.LastValue>0)result.movingDrawCalls.Add(movingDraws.LastValue);
                result.peakNativeMemoryBytes=Math.Max(result.peakNativeMemoryBytes,Profiler.GetTotalAllocatedMemoryLong());
                float3 current = em.GetComponentData<LocalTransform>(unit).Position;
                distance += math.distance(current, previous); previous = current;
                Assert.That(math.all(math.isfinite(current)), Is.True);
                if (!captured && distance > 10) { Capture(candidate, reportRoot + "/moving-" + route + "-" + kind + ".png", current); captured = true; }
                if (math.distance(current.xz, new float2(goal.x + .5f, goal.y + .5f)) < 2.5f) { evidence.result = "Passed"; break; }
            }
            evidence.seconds = Time.realtimeSinceStartup - started; evidence.distance = distance;
            Assert.That(evidence.result, Is.EqualTo("Passed"), route + " " + kind + " did not reach destination; traveled=" + distance + " final=" + previous + " goal=" + goal);
            Debug.Log($"[MapPreparationRoute] result=Passed map={candidate.semanticHash} route={route} unit={kind} seconds={evidence.seconds:F2} distance={distance:F1}");
        }
        finally { if (em.Exists(unit)) em.DestroyEntity(unit); }
    }
        private static IEnumerator RejectInvalidDestination(EntityManager em, Candidate candidate, Entity prefab, string name, int2 goal, RuntimeEvidence result)
    {
        using var gridQuery=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable));
        using var surfaceQuery=em.CreateEntityQuery(typeof(MapSurfaceComponent));
        Entity grid=gridQuery.GetSingletonEntity(); var surface=surfaceQuery.GetSingleton<MapSurfaceComponent>();
        bool goalInGrid=goal.x>=0 && goal.y>=0 && goal.x<2048 && goal.y<1024;
        // Outside-playable exclusion is represented by a baked static blocker;
        // water is excluded by the surface mask. GridWalkable alone is not the
        // full movement contract for either case.
        if(name=="OutsidePlayable" && goalInGrid)
        {
            using var outside=em.CreateEntityQuery(typeof(StaticGridBlocker),typeof(UnitGrid),typeof(GridBlockerSize));
            using var cells=outside.ToComponentDataArray<UnitGrid>(Allocator.Temp);
            using var sizes=outside.ToComponentDataArray<GridBlockerSize>(Allocator.Temp);
            Assert.That(Enumerable.Range(0,cells.Length).Any(i=>goal.x>=cells[i].Cell.x && goal.x<cells[i].Cell.x+sizes[i].Size.x && goal.y>=cells[i].Cell.y && goal.y<cells[i].Cell.y+sizes[i].Size.y),Is.True,"Outside destination needs a baked blocker");
        }
        if(goalInGrid)
        {
            Assert.That(MapSurfaceBlobAccess.TryGetSurfaceByIndex(ref surface.SurfaceBlob.Value,goal.y*2048+goal.x,out var invalid),Is.True);
            Assert.That((invalid.MovementMask & MapSurfaceMovementMask.Infantry)==0,Is.True);
        }
        else Assert.That(name,Is.EqualTo("OutsidePlayable"));
        int2 requestedStart=new((int)candidate.runtimePlayableMin.x+40,(int)candidate.runtimePlayableMin.y+45);
        var footprint=em.GetComponentData<UnitFootprint>(prefab).Size;
        int2 start=ClearApproach(em,grid,surface,requestedStart,footprint,candidate,MapSurfaceMovementMask.Infantry);
        Entity unit=em.Instantiate(prefab);
        try
        {
            MapSurfaceBlobAccess.TryGetSurfaceByIndex(ref surface.SurfaceBlob.Value,start.y*2048+start.x,out var sample);
            var transform=em.GetComponentData<LocalTransform>(unit);transform.Position=new float3(start.x+.5f,sample.Height+.1f,start.y+.5f);em.SetComponentData(unit,transform);
            em.SetComponentData(unit,new UnitGrid{Cell=start});
            var behavior=em.GetComponentData<UnitMovementBehavior>(unit);behavior.AllowIdleWander=0;em.SetComponentData(unit,behavior);
            if(em.HasComponent<Faction>(unit))em.SetComponentData(unit,new Faction{Id=1});
            // Prefabs can carry movement orders from authoring. Start this
            // invalid-goal check with an idle unit, as a player-issued order would.
            Assert.That(UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em,unit),Is.True);
            // Let the freshly instantiated prefab settle onto its grid cell
            // before measuring the effect of the invalid order itself.
            yield return null; yield return null;
            transform=em.GetComponentData<LocalTransform>(unit);
            int2 settledCell=em.GetComponentData<UnitGrid>(unit).Cell;
            Assert.That(settledCell,Is.EqualTo(start),"Invalid-order fixture moved before the order; prefab contains active movement state");
            UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em,unit,goal);
            float maxDisplacement=0;
            int maxPathLength=0;
            int2 finalCell=settledCell;
            for(int frame=0;frame<60;frame++)
            {
                yield return null;
                finalCell=em.GetComponentData<UnitGrid>(unit).Cell;
                maxDisplacement=math.max(maxDisplacement,math.distance(em.GetComponentData<LocalTransform>(unit).Position.xz,transform.Position.xz));
                if(em.HasComponent<UnitPathRange>(unit))maxPathLength=math.max(maxPathLength,em.GetComponentData<UnitPathRange>(unit).Length);
            }
            Debug.Log("[MapPreparationInvalidDestinationTrace] case="+name+" start="+settledCell+" goal="+goal+" final="+finalCell+" maxDisplacement="+maxDisplacement+" maxPathLength="+maxPathLength);
            Assert.That(finalCell,Is.EqualTo(settledCell),"Invalid destination moved unit; goal="+goal+" maxDisplacement="+maxDisplacement+" maxPathLength="+maxPathLength);
            Assert.That(maxDisplacement,Is.LessThan(1f),"Invalid destination moved beyond starting cell");
            Assert.That(maxPathLength,Is.LessThanOrEqualTo(1),"Invalid destination produced a usable path");
            result.rejectedDestinations.Add(name+": normal order acknowledged; invalid destination retains start cell and yields no usable path");
            Debug.Log("[MapPreparationInvalidDestination] result=Passed case="+name+" path=Rejected");
        }
        finally{if(em.Exists(unit))em.DestroyEntity(unit);}
    }

    private static int2 ClearApproach(EntityManager em, Entity grid, MapSurfaceComponent surface, int2 requested, int2 size, Candidate candidate, MapSurfaceMovementMask movementMask)
    {
        var walkable = em.GetBuffer<GridWalkable>(grid);
        using var blockers = em.CreateEntityQuery(typeof(StaticGridBlocker), typeof(UnitGrid), typeof(GridBlockerSize));
        using var origins = blockers.ToComponentDataArray<UnitGrid>(Allocator.Temp);
        using var sizes = blockers.ToComponentDataArray<GridBlockerSize>(Allocator.Temp);
        for (int radius = 0; radius <= 12; radius++) for (int dz = -radius; dz <= radius; dz++) for (int dx = -radius; dx <= radius; dx++)
        {
            if (math.max(math.abs(dx),math.abs(dz)) != radius) continue;
            int2 cell = requested + new int2(dx,dz); bool clear = true;
            int2 min = UnitFootprintUtility.GetMinCell(cell,size);
            for (int z = min.y - 1; z < min.y + size.y + 1 && clear; z++) for (int x = min.x - 1; x < min.x + size.x + 1; x++)
            {
                if (x < candidate.runtimePlayableMin.x || x >= candidate.runtimePlayableMin.x + candidate.playableSize.x || z < candidate.runtimePlayableMin.y || z >= candidate.runtimePlayableMin.y + candidate.playableSize.y || walkable[z * 2048 + x].Value == 0) { clear = false; break; }
                if (!MapSurfaceBlobAccess.TryGetSurfaceByIndex(ref surface.SurfaceBlob.Value, z * 2048 + x, out var sample) || (sample.MovementMask & movementMask) == 0) { clear = false; break; }
                for (int b = 0; b < origins.Length; b++) if (x >= origins[b].Cell.x && x < origins[b].Cell.x + sizes[b].Size.x && z >= origins[b].Cell.y && z < origins[b].Cell.y + sizes[b].Size.y) { clear = false; break; }
            }
            if (clear) return cell;
        }
        throw new AssertionException("No footprint-safe approach within declared 12m of " + requested + " size=" + size);
    }
    private static void AssertPackagedEntitySceneFiles(Packed packed)
    {
        string source=Path.Combine(Resolve(packed.entityContentPath),"EntityScenes");
        string packaged=Path.Combine(Application.streamingAssetsPath,"EntityScenes");
        foreach(string file in Directory.GetFiles(source,"*",SearchOption.TopDirectoryOnly))
        {
            string name=Path.GetFileName(file);
            if(!name.EndsWith(".entityheader",StringComparison.Ordinal) && !name.EndsWith(".entities",StringComparison.Ordinal)) continue;
            string destination=Path.Combine(packaged,name);
            Assert.That(File.Exists(destination),Is.True,"Native player is missing prepared entity scene file "+name);
            Assert.That(FileHash(destination),Is.EqualTo(FileHash(file)),"Native entity scene bytes differ from published content: "+name);
        }
        Assert.That(File.Exists(Path.Combine(packaged,packed.entitySceneGuid+".entityheader")),Is.True);
    }
    private static string FileHash(string path) { using var sha=System.Security.Cryptography.SHA256.Create(); return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(b=>b.ToString("x2"))); }
    private static string Resolve(string path)
    {
        if(Path.IsPathRooted(path)) return path;
        foreach(string start in new[]{Directory.GetCurrentDirectory(),Application.dataPath})
        {
            var directory=new DirectoryInfo(start);
            while(directory!=null)
            {
                if(File.Exists(Path.Combine(directory.FullName,"Design/MapVariants/HANDOFF_Map_Preparation.md"))) return Path.Combine(directory.FullName,path);
                directory=directory.Parent;
            }
        }
        throw new DirectoryNotFoundException("Native preparation tests must be built beneath the project (Build/MapVariantPreparedPlayer) so external candidate content and evidence can be resolved.");
    }
    private static int ActiveLightCount() => Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude).Count(l => l.enabled);
    private static int ActiveGameObjectGeometryCount() => Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude).Count(r => r.enabled && ((r is MeshRenderer && r.GetComponent<MeshFilter>()?.sharedMesh != null) || (r is SkinnedMeshRenderer skin && skin.sharedMesh != null)));
    private static void Capture(Candidate c, string path, float3? focus = null, bool topDown=false)
    {
        float3 target = focus ?? new float3(c.runtimePlayableMin.x + c.playableSize.x * .5f, 0, c.runtimePlayableMin.y + c.playableSize.y * .5f);
        float height = topDown ? 600 : focus.HasValue ? 64 : 180;
        var go = new GameObject("PreparedRuntimeEvidenceCamera", typeof(Camera)); var cam = go.GetComponent<Camera>();
        cam.transform.SetPositionAndRotation(new Vector3(target.x, target.y + height, topDown ? target.z : target.z - height / Mathf.Tan(51.6f * Mathf.Deg2Rad)), Quaternion.Euler(topDown ? 90 : 51.6f,0,0));
        cam.fieldOfView = 55; cam.farClipPlane = 4000;
        if(topDown)
        {
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(c.playableSize.y * .5f, c.playableSize.x / (2f * (1920f / 1080f))) * 1.05f;
        }
        var rt = new RenderTexture(1920,1080,24); var pixels = new Texture2D(1920,1080,TextureFormat.RGB24,false); var old = RenderTexture.active;
        try { cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply(); File.WriteAllBytes(path,pixels.EncodeToPNG()); }
        finally { RenderTexture.active = old; Object.Destroy(go); Object.Destroy(rt); Object.Destroy(pixels); }
    }
    [Serializable] private sealed class Packed
    {
        public string entityCatalogHash, addressablesCatalogHash, bindingHash;
        public string semanticHash, target, entityCatalogPath, entityContentPath, addressablesCatalogPath, definitionAddress, entitySceneGuid, unitFixtureSceneGuid;
    }
    [Serializable] private sealed class Candidate
    {
        public string semanticHash; public Vector2 runtimePlayableMin, playableSize; public List<Owner> owners;
    }
    [Serializable] private sealed class Owner { public string stableId, sourceGuid; public Vector3 position; }
    [Serializable] private sealed class RuntimeEvidence
    {
        public string map, contentHash, target, device, scope, result, graphicsAPI, gpu, operatingSystem, unityVersion, qualityLevel, captureResolution, entityCatalogHash, addressablesCatalogHash, bindingHash, scriptingBackend;
        public List<float> loadSeconds = new(), frameMilliseconds = new(), mainThreadMilliseconds = new(), renderThreadMilliseconds = new();
        public List<long> drawCalls = new(), movingDrawCalls = new();
        public List<float> movingFrameMilliseconds = new(), movingMainThreadMilliseconds = new(), movingRenderThreadMilliseconds = new();
        public List<string> rejectedDestinations = new();
        public long peakNativeMemoryBytes;
        public List<int> unloadedMaterialCounts = new();
        public List<RouteEvidence> routes = new(); public int renderEntityCount; public long memoryBytes;
    }
    [Serializable] private sealed class RouteEvidence
    {
        public string name, unit, result; public int2 requestedStart, requestedGoal, start, goal, footprint; public float seconds, distance;
    }
}
