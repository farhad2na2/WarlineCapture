using System;
using System.Linq;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal sealed partial class AutonomousWorld
    {
        internal JObject BindAuthoredSourceUnitMetadataFromInput(string descriptorPath, string descriptorSha256,
            string repositoryRoot, IDictionary<string, Entity> templates)
        {
            if (disposed || !initializationAttempted || Setup == null || ActualUnitMetadata != null)
                throw new InvalidOperationException("Fresh same-world source metadata context required");
            var input = SourceAuthoredUnitMetadataInput.Open(Setup, descriptorPath, descriptorSha256, repositoryRoot);
            return BindAuthoredSourceUnitMetadata(input.Authority, input.MetadataPath, input.MetadataSha256,
                input.IdentityPath, repositoryRoot, templates);
        }

        internal JObject BindAuthoredSourceUnitMetadata(SourceAuthoredUnitMetadataAuthority authority,
            string metadataPath,string metadataHash,string identityPath,string sourceRoot,IDictionary<string,Entity> templates)
        {
            if(disposed || !initializationAttempted || ActualUnitMetadata!=null || SourceDefinitions==null || Setup==null ||
               StorageBank==null || !Entities.BankBound || authority==null || templates==null || MetadataCompatibility!=null)
                throw new InvalidOperationException("Unqualified same-world source metadata context required once");
            var independent=authority.ReadIndependentIdentities(identityPath);
            var stagedAssets=new Dictionary<string,SourceUnitPrefab>(SourceDefinitions.Assets,StringComparer.Ordinal);
            var stagedIdentities=SourceDefinitions.AssetIdentities.ToDictionary(pair=>pair.Key,pair=>(JObject)pair.Value.DeepClone(),StringComparer.Ordinal);
            foreach(var pair in independent)
            {
                string key=pair.Key;var identity=pair.Value;
                if(!templates.TryGetValue(key,out var entity) || !Entities.Exists(entity) || !Entities.HasComponent<Prefab>(entity) ||
                    !Entities.HasComponent<UnitSourcePrefabKey>(entity) || Entities.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()!=key)
                    throw new InvalidOperationException("Actual same-world source template required: "+key);
                if(stagedIdentities.TryGetValue(key,out var priorIdentity) && !JToken.DeepEquals(priorIdentity,identity))
                    throw new InvalidOperationException("Source factory and independent prefab identity differ: "+key);
                stagedIdentities[key]=(JObject)identity.DeepClone();
                if(stagedAssets.TryGetValue(key,out var prior))
                {
                    if(prior.Template!=entity || prior.SourceAssetPath!=null &&
                        (prior.SourceAssetPath!=(string)identity["path"] || prior.SourceAssetGuid!=(string)identity["guid"] || prior.SourceAssetLocalId!=(long)identity["local_id"]))
                        throw new InvalidOperationException("Source factory and actual template disagree: "+key);
                }
                else stagedAssets.Add(key,new SourceUnitPrefab(key,entity,(string)identity["path"],(string)identity["guid"],(long)identity["local_id"]));
            }
            var loaded=GeneratedUnitProductionMetadata.Load(metadataPath,metadataHash,Setup.SetupHash,Setup.Seed,sourceRoot,
                stagedAssets,templates,stagedIdentities,sourceAuthority:authority);
            if(loaded.Ordered.Count!=authority.UnitCount || loaded.Ordered.Any(row=>!row.HasActualTemplate) || loaded.FactoryDomainQualified)
                throw new InvalidOperationException("Complete unqualified same-world authored metadata required");
            foreach(var pair in stagedAssets)if(!SourceDefinitions.Assets.ContainsKey(pair.Key))SourceDefinitions.Assets.Add(pair.Key,pair.Value);
            foreach(var pair in stagedIdentities)if(!SourceDefinitions.AssetIdentities.ContainsKey(pair.Key))SourceDefinitions.AssetIdentities.Add(pair.Key,(JObject)pair.Value.DeepClone());
            ActualUnitMetadata=loaded;
            return new JObject { ["configured_units"]=loaded.Ordered.Count,["actual_same_world_templates"]=loaded.Ordered.Count(row=>row.HasActualTemplate),
                ["independent_asset_identities"]=independent.Count,["source_factory_qualified"]=false,["native_readiness"]=false,["mission_ready"]=false };
        }
        internal JObject BindFreshExpandedSourceStorage(FreshSourceStorageLifetimeAuthority authority, string authorityHash)
        {
            if (disposed || !initializationAttempted || Initialized || initialRoads == null ||
                ground == null || Setup == null || ActualDefinition == null || authority == null ||
                Entities.BankBound || StorageBank != null || !Entities.Exists(Session))
                throw new InvalidOperationException("Unbound retained source world and storage authority required");
            initialRoads.RequireBeforePath();
            using var query = Entities.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
            using var rows = query.ToEntityArray(Allocator.Temp);
            var entities = rows.ToArray();
            if (entities.Length == 0) throw new InvalidOperationException("Actual source storage components required");
            var storage = entities.Select(Entities.GetComponentData<BuildingResourceStorageComponent>).ToArray();
            var present = entities.Select(Entities.HasComponent<BuildingResourceProductionProfileComponent>).ToArray();
            var profiles = entities.Select((e, i) => present[i] ? Entities.GetComponentData<BuildingResourceProductionProfileComponent>(e) : default).ToArray();
            var bank = CanonicalStorageBank.CreateFreshSourceConfig(authority, entities, storage, profiles, present);
            Entities.BindStorageBank(bank);
            if (!Entities.BankBound || !bank.IsFreshSourceOwnedBy(Entities))
                throw new InvalidOperationException("Same-world storage bank binding failed");
            for (int i = 0; i < entities.Length; i++)
                if (!CanonicalStorageBank.Same(storage[i], Entities.GetComponentData<BuildingResourceStorageComponent>(entities[i])) ||
                    Entities.HasComponent<BuildingResourceProductionProfileComponent>(entities[i]) != present[i] ||
                    (present[i] && !CanonicalStorageBank.Same(profiles[i], Entities.GetComponentData<BuildingResourceProductionProfileComponent>(entities[i]))))
                    throw new InvalidOperationException("Source bank changed actual storage or production profiles");
            authority.RequireClaimedActual(ActualDefinition, Setup, authorityHash);
            StorageBank = bank; freshStorageAuthority = authority;
            return new JObject { ["same_world_bank_bound"]=true,["storage_rows"]=entities.Length,
                ["production_profile_rows"]=present.Count(value=>value),["before_after_rows_equal"]=true,
                ["scope"]="Fresh source components, not a native or conservation qualification",["mission_ready"]=false };
        }
        private JObject CaptureExpandedSourceResources()
        {
            var materials = new JArray(); var storage = new JArray(); var reservations = new JArray();
            using (var query = Entities.CreateEntityQuery(typeof(FactionTacticalMaterialsComponent)))
            using (var entities = query.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities)
                {
                    var row = Entities.GetComponentData<FactionTacticalMaterialsComponent>(entity);
                    materials.Add(new JObject { ["entity"] = entity.Index + ":" + entity.Version, ["faction"] = row.FactionId,
                        ["current"] = row.Current, ["capacity"] = row.Capacity, ["fabricated"] = row.LifetimeFabricated,
                        ["imported"] = row.LifetimeImported, ["rewarded"] = row.LifetimeRewarded,
                        ["exported"] = row.LifetimeExported, ["spent"] = row.LifetimeSpent,
                        ["production_spent"] = row.LifetimeProductionSpent });
                }
            using (var query = Entities.CreateEntityQuery(typeof(BuildingResourceStorageComponent)))
            using (var entities = query.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities)
                {
                    var row = Entities.GetComponentData<BuildingResourceStorageComponent>(entity);
                    storage.Add(new JObject { ["entity"] = entity.Index + ":" + entity.Version,
                        ["building_id"] = row.RuntimeBuildingId, ["faction"] = row.OwnerFactionId,
                        ["oil"] = row.StoredOilBarrels, ["fuel"] = row.StoredFuelBarrels,
                        ["oil_inbound_reserved"] = row.ReservedOilInboundBarrels, ["oil_outbound_reserved"] = row.ReservedOilOutboundBarrels,
                        ["fuel_inbound_reserved"] = row.ReservedFuelInboundBarrels, ["fuel_outbound_reserved"] = row.ReservedFuelOutboundBarrels });
                }
            if (Entities.HasBuffer<SkirmishProductionReservation>(Session))
            {
                var rows = Entities.GetBuffer<SkirmishProductionReservation>(Session);
                for (int i = 0; i < rows.Length; i++)
                {
                    var row = rows[i]; reservations.Add(new JObject { ["id"] = row.ReservationId, ["faction"] = row.FactionId,
                        ["role"] = row.Role.ToString(), ["phase"] = row.Phase.ToString(), ["materials_paid"] = row.MaterialsPaid,
                        ["members"] = row.MemberCount, ["remaining"] = row.RemainingMembers, ["delivered"] = row.DeliveredMembers });
                }
            }
            JObject legacy = null;
            if (Entities.HasComponent<SkirmishEnemyStockComponent>(Session))
            {
                var row=Entities.GetComponentData<SkirmishEnemyStockComponent>(Session);
                legacy=new JObject { ["materials"]=row.Materials,["oil"]=row.Oil,["fuel"]=row.Fuel };
            }
            return new JObject { ["scope"]="Observed source-world components; not a conservation certification",
                ["materials"]=materials,["storage"]=storage,["production_reservations"]=reservations,
                ["enemy_stock_component"]=legacy,["storage_bank_bound"]=Entities.BankBound };
        }
        // Retains an already constructed, source-only world. This does not
        // authorize a scheduler, a loaded scene, or a native/player result.
        internal void AttachExpandedSourceStartup(Entity grid, Entity surface, Entity session, Entity boundary,
            Entity mapRoot, SkirmishScenarioDefinitionConfig definition, SkirmishResolvedSetup setup,
            GeneratedStartingBuildingPipeline pipeline, GeneratedBuildingDefinitions definitions)
        {
            if (disposed || initializationAttempted || Initialized || GridEntity != Entity.Null || initialRoads == null ||
                definition == null || setup == null || pipeline == null || definitions == null || definition.ArmyProfileConfig == null ||
                setup.Structures == null || Buildings.Count != setup.Structures.Length ||
                !Entities.Exists(grid) || !Entities.Exists(surface) || !Entities.Exists(session) ||
                !Entities.Exists(boundary) || !Entities.Exists(mapRoot) ||
                !Entities.HasComponent<GridConfig>(grid) || !Entities.HasComponent<MapSurfaceComponent>(surface) ||
                !Entities.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !Entities.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                !Entities.HasComponent<OperationMapMetadataComponent>(mapRoot) ||
                !Entities.HasComponent<ActiveOperationMapComponent>(mapRoot))
                throw new InvalidOperationException("Completed same-world expanded source startup required");
            initialRoads.RequireBeforePath();
            var actual = Entities.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var active = Entities.GetComponentData<ActiveOperationMapComponent>(mapRoot);
            var metadata = Entities.GetComponentData<OperationMapMetadataComponent>(mapRoot);
            if (!ReferenceEquals(Entities.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup, setup) ||
                actual.IsLegacy != 0 || actual.SpawnComplete != 1 || actual.Phase != SkirmishSessionPhase.Playing ||
                actual.SetupHash != setup.SetupHash || actual.Seed != setup.Seed ||
                actual.CatalogId.ToString() != setup.CatalogId || actual.DefinitionId.ToString() != setup.DefinitionId ||
                active.ScenarioId.ToString() != setup.CatalogId || active.MissionId.ToString() != setup.DefinitionId ||
                !metadata.Blob.IsCreated || metadata.PhysicalSourceValidated != 0 || definition.CampaignMap == null ||
                active.OperationMapId.ToString() != definition.CampaignMap.OperationMapId)
                throw new InvalidOperationException("Source session, setup and logical map identity must agree");
            GridEntity = grid; SurfaceEntity = surface; Session = session; Boundary = boundary;
            Setup = setup; Army = definition.ArmyProfileConfig; ActualDefinition = definition;
            StartingPipeline = pipeline; SourceDefinitions = definitions;
            FixedDeltaTime = 1f / 60f;
            initializationAttempted = true;
            InitializationStage = "ExpandedSourceStartupRetained_SchedulerNotAdmitted";
        }

        // An explicitly bounded original-owner subset, not the complete scheduler.
        // Retain the path and ECB owners in this world for subsequent integration.
        internal JObject InitializeAndRunExpandedGroundSubset()
        {
            if (disposed || !initializationAttempted || Initialized ||
                InitializationStage != "ExpandedSourceStartupRetained_SchedulerNotAdmitted" ||
                ground != null || deferred != null || initialRoads == null ||
                !Entities.Exists(GridEntity) || !Entities.Exists(Session))
                throw new InvalidOperationException("Fresh retained expanded source world required");
            initialRoads.RequireBeforePath();
            using var states = Entities.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (states.CalculateEntityCount() != 1 || states.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive != 1 ||
                Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session).Phase != SkirmishSessionPhase.Playing ||
                Entities.HasComponent<PathPoolComponent>(GridEntity))
                throw new InvalidOperationException("Active source gameplay and a fresh path pool are required");
            using var existing = Entities.CreateEntityQuery(typeof(EndSimulationEntityCommandBufferSystem.Singleton));
            if (existing.CalculateEntityCount() != 0)
                throw new InvalidOperationException("Fresh source deferred owner required");
            Entities.AddComponentData(GridEntity, new PathPoolComponent { Cells = new NativeList<int2>(64, Allocator.Persistent) });
            deferred = new OwnedEndSimulationBuffers();
            var deferredEntity = Entities.CreateEntity();
            Entities.AddComponentData(deferredEntity, new EndSimulationEntityCommandBufferSystem.Singleton { Owner = deferred });
            ground = new OriginalGroundOwners(Entities, generatedFresh: true);
            Entities.WorldUnmanaged.Time = new TimeData { ElapsedTime = 1.0 / 60.0, DeltaTime = 1f / 60f };
            UnityEngine.Time.frameCount = 1; UnityEngine.Time.time = 1f / 60f;
            UnityEngine.Time.unscaledTime = 1f / 60f; UnityEngine.Time.deltaTime = 1f / 60f;
            UnityEngine.Time.unscaledDeltaTime = 1f / 60f; UnityEngine.Time.timeScale = 1;
            bool occupancy = ground.OccupancyPhase();
            bool path = ground.PathPhase();
            bool movement = ground.MovementPhase();
            ground.EndSimulationPhase();
            InitializationStage = "ExpandedSourceGroundSubsetObserved_SchedulerNotAdmitted";
            return new JObject { ["fixed_delta_seconds"] = 1f / 60f, ["occupancy_owner_ran"] = occupancy,
                ["path_owner_ran"] = path, ["movement_owner_ran"] = movement,
                ["pending_path"] = ground.HasPendingPath, ["same_world"] = true,
                ["scope"] = "One original ground-owner subset; not a full scheduler tick",
                ["scheduler_admitted"] = false, ["mission_ready"] = false };
        }

        internal JObject RunExpandedRecurringOwnerDiagnostic(string presetPath, string presetHash, int steps)
        {
            if (disposed || Initialized || InitializationStage != "ExpandedSourceGroundSubsetObserved_SchedulerNotAdmitted" ||
                ground == null || deferred == null || initialRoads == null || Setup == null || ActualDefinition == null ||
                StorageBank == null || !StorageBank.IsFreshSourceOwnedBy(Entities) || !Entities.BankBound ||
                steps < 1 || steps > 600)
                throw new InvalidOperationException("Active retained source world, ground and source storage required");
            initialRoads.RequireBeforePath(); RequireExistingActiveSourceGameplay(Entities);
            if (!Entities.HasComponent<SkirmishMatchState>(Session)) throw new InvalidOperationException("Original match state required");
            var match=Entities.GetComponentData<SkirmishMatchState>(Session);
            var session=Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
            if (!SkirmishMissionDispatch.TryGetLegacyIndex(Setup.CatalogId,out int index) || match.ScenarioIndex!=index ||
                match.Seed!=Setup.Seed || !match.SessionId.Equals(session.SessionId) || session.Phase!=SkirmishSessionPhase.Playing)
                throw new InvalidOperationException("Actual source dispatch and session identity required");
            string resource=SkirmishPresetConfig.SourcePresetResourceName(index);
            string expected=System.IO.Path.GetFullPath("Assets/Game/Resources/"+resource+".asset");
            if (System.IO.Path.GetFullPath(presetPath)!=expected) throw new InvalidOperationException("Selected source preset path differs");
            string text=System.Text.Encoding.UTF8.GetString(PinnedFile(expected,presetHash,4*1024*1024));
            var values=System.Text.RegularExpressions.Regex.Matches(text,@"(?m)^  supplyDaySeconds: (?<value>[^\r\n]+)\r?$");
            if (values.Count!=1 || !float.TryParse(values[0].Groups["value"].Value,System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,out float day) || day<1f || float.IsNaN(day) || float.IsInfinity(day))
                throw new InvalidOperationException("Finite authored source resource cadence required");
            using (var query=Entities.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent)))
            using (var rows=query.ToEntityArray(Allocator.Temp))
                foreach(var entity in rows)
                {
                    var owner=Entities.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                    if(owner.SessionId.Equals(session.SessionId) && owner.IsStructure==0)OpeningActors.Add(entity);
                }
            if(OpeningActors.Count!=Setup.Forces.Sum(force=>force.Quantity) || OpeningActors.Distinct().Count()!=OpeningActors.Count)
                throw new InvalidOperationException("Actual opening roster must match the original compiled setup");
            SourceOwnerOrderAdmission.RequirePlan();
            Tick=1; SimulationSeconds=Entities.WorldUnmanaged.Time.ElapsedTime; FixedDeltaTime=1f/60f;
            InitializeGeneratedSourceOwnersForExistingGameplay(day);
            InitializeActualRosterOwner(); InitializeActualContinuationOwners();
            storageState=FreshState(); storageState.Begin(); storageOwner.OnCreate(ref storageState); storageState.End();
            dedupState=FreshState(); dedupState.Begin(); dedup.OnCreate(ref dedupState); dedupState.End();
            staticBlockerState=FreshState(); staticBlockerState.Begin(); staticBlockers.OnCreate(ref staticBlockerState); staticBlockerState.End();
            string output=Environment.GetEnvironmentVariable("WARLINE_S005_ROAD_RECEIPT_DIR");
            if(String.IsNullOrEmpty(output) || !System.IO.Path.IsPathRooted(output) || !System.IO.Directory.Exists(output))
                throw new InvalidOperationException("Owned source diagnostic output required");
            using var trace=new System.IO.StreamWriter(new System.IO.FileStream(System.IO.Path.Combine(output,"recurring-owner-trace.jsonl"),System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.Read));
            var before=CaptureExpandedSourceResources(); int completed=0; var timer=System.Diagnostics.Stopwatch.StartNew(); JObject last=null;
            for(int i=0;i<steps;i++)
            {
                try {
                    last=AdvanceConstructedSourceOwners();
                    var current=Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
                    var objective=Entities.GetComponentData<SkirmishObjectiveStateComponent>(Session);
                    var row=new JObject { ["tick"]=Tick,["simulation_seconds"]=SimulationSeconds,["phase"]=current.Phase.ToString(),
                        ["objective_outcome"]=objective.Outcome.ToString(),["objective_terminal"]=(int)objective.Terminal };
                    if(Tick%60==0 || objective.Terminal!=0)row["resources"]=CaptureExpandedSourceResources();
                    trace.WriteLine(row.ToString(Newtonsoft.Json.Formatting.None));trace.Flush();completed++;
                    if(objective.Terminal!=0 || current.Phase==SkirmishSessionPhase.Finished || current.Phase==SkirmishSessionPhase.Failed)break;
                } catch(Exception error) {
                    trace.WriteLine(new JObject { ["attempted_tick"]=Tick,["completed_steps"]=completed,["failure"]=error.ToString() }.ToString(Newtonsoft.Json.Formatting.None));trace.Flush();throw;
                }
            }
            timer.Stop();var finalSession=Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
            var finalObjective=Entities.GetComponentData<SkirmishObjectiveStateComponent>(Session);
            InitializationStage="ExpandedRecurringSourceOwnersObserved_NotMissionQualified";
            return new JObject { ["requested_steps"]=steps,["completed_steps"]=completed,["fixed_delta_seconds"]=FixedDeltaTime,
                ["advance_wall_seconds"]=timer.Elapsed.TotalSeconds,["preset_resource"]=resource,["supply_day_seconds"]=day,
                ["source_phase_receipt"]=last,["session_phase"]=finalSession.Phase.ToString(),["objective_outcome"]=finalObjective.Outcome.ToString(),
                ["objective_terminal"]=(int)finalObjective.Terminal,["resources_before"]=before,["resources_after"]=CaptureExpandedSourceResources(),
                ["production_catalog_bound"]=ActualCatalog!=null,["production_delivery_bound"]=ActualDeliveryOwner!=null,
                ["full_mission"]=false,["native_mission"]=false,["mission_ready"]=false };
        }

        internal JObject RunExpandedStrategyOwnerSubset(int steps = 1)
        {
            if (disposed || ground == null || deferred == null ||
                InitializationStage != "ExpandedSourceGroundSubsetObserved_SchedulerNotAdmitted" ||
                !Entities.Exists(Session) || steps < 1 || steps > 600)
                throw new InvalidOperationException("Retained source ground subset required");
            var before = Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
            if (before.Phase != SkirmishSessionPhase.Playing || before.ObjectiveKind != SkirmishObjectiveKind.BaseAssault ||
                !Entities.HasComponent<SkirmishObjectiveStateComponent>(Session))
                throw new InvalidOperationException("Actual active Base Assault session required");
            using var requests = Entities.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(UnitPathRequest));
            int beforeRequests = requests.CalculateEntityCount();
            var capacity = new SkirmishCapacityLifecycleSystem(); var capState = new SystemState { EntityManager = Entities, Enabled = true };
            capState.Begin(); capacity.OnCreate(ref capState); capState.End();
            var groups = new SkirmishArmyGroupSystem(); var groupsState = new SystemState { EntityManager = Entities, Enabled = true };
            groupsState.Begin(); groups.OnCreate(ref groupsState); groupsState.End();
            var facts = new SkirmishObjectiveFactProjectionSystem(Entities);
            var baseOwner = new SkirmishBaseAssaultObjectiveSystem(Entities);
            var enemy = new SkirmishEnemyStrategySystem(); var enemyState = new SystemState { EntityManager = Entities, Enabled = true };
            enemyState.Begin(); enemy.OnCreate(ref enemyState); enemyState.End();
            var outcome = new SkirmishOutcomeSystem(Entities);
            using var combatSubset = new OriginalCombatOwners(Entities);
            var materialSubset = new MaterialFabricationSystem(); var materialSubsetState = new SystemState { EntityManager = Entities, Enabled = true };
            materialSubsetState.Begin(); materialSubset.OnCreate(ref materialSubsetState); materialSubsetState.End();
            var fuelSubset = new VehicleFuelConsumptionSystem(); var fuelSubsetState = new SystemState { EntityManager = Entities, Enabled = true };
            fuelSubsetState.Begin(); fuelSubset.OnCreate(ref fuelSubsetState); fuelSubsetState.End();
            string output=Environment.GetEnvironmentVariable("WARLINE_S005_ROAD_RECEIPT_DIR");
            if (String.IsNullOrEmpty(output) || !System.IO.Path.IsPathRooted(output) || !System.IO.Directory.Exists(output))
                throw new InvalidOperationException("Owned source diagnostic output required");
            using var trace = new System.IO.StreamWriter(new System.IO.FileStream(System.IO.Path.Combine(output,"strategy-subset-trace.jsonl"),System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.Read));
            var resourcesBefore=CaptureExpandedSourceResources();
            bool occupancyRan=false,pathRan=false,movementRan=false,capRan=false,groupsRan=false,enemyRan=false;
            int completed=0; var timer=System.Diagnostics.Stopwatch.StartNew();
            try { for(int i=0;i<steps;i++)
            {
                int frame=i+2;
                Entities.WorldUnmanaged.Time = new TimeData { ElapsedTime = frame / 60.0, DeltaTime = 1f / 60f };
                UnityEngine.Time.frameCount=frame; UnityEngine.Time.time=frame / 60f; UnityEngine.Time.unscaledTime=frame / 60f;
                try {
                    combatSubset.EnsureStatePhase();
                    materialSubsetState.Begin(); bool materialRan=materialSubsetState.Enabled && materialSubsetState.RequiredQueriesPresent;
                    if(materialRan)materialSubset.OnUpdate(ref materialSubsetState); materialSubsetState.End();
                    bool validationRan=combatSubset.ValidatePhase(); occupancyRan=ground.OccupancyPhase();
                    bool acquisitionRan=combatSubset.AcquisitionPhase(); bool synchronizationRan=combatSubset.SynchronizePhase();
                    pathRan=ground.PathPhase(); bool chaseRan=combatSubset.ChasePhase(); movementRan=ground.MovementPhase();
                    fuelSubsetState.Begin(); bool fuelRan=fuelSubsetState.Enabled && fuelSubsetState.RequiredQueriesPresent;
                    if(fuelRan)fuelSubset.OnUpdate(ref fuelSubsetState); fuelSubsetState.End();
                    combatSubset.AttackPhase(); combatSubset.DefensePhase(); combatSubset.DeathPhase();
                    capState.Begin(); capRan=capState.Enabled && capState.RequiredQueriesPresent; if(capRan)capacity.OnUpdate(ref capState); capState.End();
                    groupsState.Begin(); groupsRan=groupsState.Enabled && groupsState.RequiredQueriesPresent; if(groupsRan)groups.OnUpdate(ref groupsState); groupsState.End();
                    facts.Step(Entities); baseOwner.Step(Entities);
                    int beforeEnemy=requests.CalculateEntityCount();
                    enemyState.Begin(); enemyRan=enemyState.Enabled && enemyState.RequiredQueriesPresent; if(enemyRan)enemy.OnUpdate(ref enemyState); enemyState.End();
                    outcome.Step(Entities); ground.EndSimulationPhase();
                    var current=Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
                    var currentObjective=Entities.GetComponentData<SkirmishObjectiveStateComponent>(Session);
                    var row=new JObject { ["frame"]=frame,["enemy_owner_ran"]=enemyRan,["movement_owner_ran"]=movementRan,
                        ["material_owner_ran"]=materialRan,["fuel_owner_ran"]=fuelRan,["target_validation_ran"]=validationRan,
                        ["target_acquisition_ran"]=acquisitionRan,["target_sync_ran"]=synchronizationRan,["chase_owner_ran"]=chaseRan,
                        ["path_requests_before_enemy"]=beforeEnemy,
                        ["path_requests"]=requests.CalculateEntityCount(),["phase"]=current.Phase.ToString(),
                        ["objective_outcome"]=currentObjective.Outcome.ToString(),["objective_terminal"]=(int)currentObjective.Terminal };
                    if(frame%60==0 || currentObjective.Terminal!=0)row["resources"]=CaptureExpandedSourceResources();
                    trace.WriteLine(row.ToString(Newtonsoft.Json.Formatting.None)); trace.Flush(); completed++;
                    if(currentObjective.Terminal!=0 || current.Phase==SkirmishSessionPhase.Finished || current.Phase==SkirmishSessionPhase.Failed)break;
                } catch(Exception error) {
                    trace.WriteLine(new JObject { ["frame"]=frame,["completed_steps"]=completed,["failure"]=error.ToString() }.ToString(Newtonsoft.Json.Formatting.None));
                    trace.Flush(); throw;
                }
            }} finally { materialSubsetState.Begin(); materialSubset.OnDestroy(ref materialSubsetState); materialSubsetState.End(); }
            timer.Stop();
            var after = Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
            var objective = Entities.GetComponentData<SkirmishObjectiveStateComponent>(Session);
            InitializationStage = "ExpandedSourceStrategySubsetObserved_SchedulerNotAdmitted";
            return new JObject { ["fixed_delta_seconds"] = 1f / 60f, ["requested_steps"]=steps,["completed_steps"]=completed,
                ["advance_wall_seconds"]=timer.Elapsed.TotalSeconds, ["occupancy_owner_ran"] = occupancyRan,
                ["path_owner_ran"] = pathRan, ["movement_owner_ran"] = movementRan,
                ["capacity_owner_ran"] = capRan, ["group_owner_ran"] = groupsRan,
                ["facts_owner_ran"] = true, ["base_objective_owner_ran"] = true,
                ["native_objective_owner_ran"] = false, ["enemy_owner_ran"] = enemyRan,
                ["outcome_owner_ran"] = true, ["path_requests_before"] = beforeRequests,
                ["path_requests_after"] = requests.CalculateEntityCount(), ["session_phase"] = after.Phase.ToString(),
                ["objective_outcome"] = objective.Outcome.ToString(), ["objective_terminal"] = (int)objective.Terminal,
                ["resources_before"]=resourcesBefore,["resources_after"]=CaptureExpandedSourceResources(),
                ["scope"] = "Original-owner subset; not the complete scheduler", ["scheduler_admitted"] = false,
                ["mission_ready"] = false };
        }
    }
}
