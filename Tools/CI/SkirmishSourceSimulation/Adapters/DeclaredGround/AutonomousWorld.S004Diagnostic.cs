using System;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
namespace Game.Runtime
{
    internal sealed partial class AutonomousWorld
    {
        private bool s004GroundLeaseActive;
        private bool s004SourceOwnersInitialized;
#if WARLINE_S004_EXPERIMENT_TICK
        private S004SupplementalTickOwners s004SupplementalTickOwners;
#if WARLINE_S004_MISSILE_TICK
        private S004MissileTickOwners s004MissileTickOwners;
#endif
#if WARLINE_S004_SCAN_ROPE_TICK
        private S004ScanRopeTickOwners s004ScanRopeTickOwners;
#endif
#if WARLINE_S004_TRANSFORM_SCHEDULE
        private SourceMissionTransformBridge s004ScheduledTransforms;
        private int s004TransformSteps, s004TransformEntitiesWritten;
        private int s004TransformExpectedSteps;
        private readonly JArray s004TransformPhaseReadbacks = new JArray();
        private void RunS004TransformScheduledPhase()
        {
            s004TransformEntitiesWritten += s004ScheduledTransforms.Step(Tick); s004TransformSteps++;
            if (Environment.GetEnvironmentVariable("WARLINE_S004_TRANSFORM_PHASE_VERIFY") == "1" &&
                (s004TransformSteps == 1 || s004TransformSteps == s004TransformExpectedSteps))
            {
                var readback = S004TransformProjectionReadback.Read(Entities);
                readback["tick"] = Tick;
                s004TransformPhaseReadbacks.Add(readback);
            }
        }
#endif
        internal JObject RunS004ExperimentalSourceTick(System.Collections.Generic.IEnumerable<string> sourcePrefabKeys)
        {
            if (disposed || !s004SourceOwnersInitialized || !s004GroundLeaseActive || s004SupplementalTickOwners != null)
                throw new InvalidOperationException("One initialized S004 source diagnostic tick required");
            double elapsed = Entities.WorldUnmanaged.Time.ElapsedTime;
            if (Math.Abs(elapsed - 10.0 / 60.0) > 0.000001)
                throw new InvalidOperationException("Observed source diagnostic clock must be at frame 10");
            FixedDeltaTime = 1f / 60f; Tick = 10; SimulationSeconds = elapsed;
            string requestedText = Environment.GetEnvironmentVariable("WARLINE_S004_EXPERIMENT_TICK_COUNT") ?? "1";
            if (!int.TryParse(requestedText, out int requestedTicks) || requestedTicks < 1 || requestedTicks > 120)
                throw new InvalidOperationException("S004 source diagnostic accepts between 1 and 120 fixed ticks");
            var sessionId = Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session).SessionId;
            using (var query = Entities.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent)))
            using (var rows = query.ToEntityArray(Allocator.Temp))
                foreach (var entity in rows)
                {
                    var owner = Entities.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                    if (owner.SessionId.Equals(sessionId) && owner.IsStructure == 0) OpeningActors.Add(entity);
                }
            if (OpeningActors.Count != Setup.Forces.Sum(force => force.Quantity) || OpeningActors.Distinct().Count() != OpeningActors.Count)
                throw new InvalidOperationException("Actual session-owned opening actors differ from the compiled S004 forces");
            InitializeActualRosterOwner();
            InitializeActualContinuationOwners();
            storageState=FreshState(); storageState.Begin(); storageOwner.OnCreate(ref storageState); storageState.End();
            dedupState=FreshState(); dedupState.Begin(); dedup.OnCreate(ref dedupState); dedupState.End();
            staticBlockerState=FreshState(); staticBlockerState.Begin(); staticBlockers.OnCreate(ref staticBlockerState); staticBlockerState.End();
            s004SupplementalTickOwners = new S004SupplementalTickOwners(Entities);
#if WARLINE_S004_MISSILE_TICK
            s004MissileTickOwners = new S004MissileTickOwners(Entities);
#endif
#if WARLINE_S004_SCAN_ROPE_TICK
            s004ScanRopeTickOwners = new S004ScanRopeTickOwners(Entities);
#endif
#if WARLINE_S004_TRANSFORM_SCHEDULE
            if (sourcePrefabKeys == null) throw new InvalidOperationException("Captured S004 source prefab keys required");
            var transformBeforeTicks = S004TransformProjectionReadback.Read(Entities);
            s004ScheduledTransforms = new SourceMissionTransformBridge(Entities, Session, sourcePrefabKeys, SourceTransformConsumedReadset.VerifyForCurrentImage());
            s004TransformExpectedSteps = requestedTicks;
#endif
            var initialCells = OpeningActors.Where(entity => Entities.Exists(entity) && Entities.HasComponent<UnitGrid>(entity))
                .ToDictionary(entity => entity, entity => Entities.GetComponentData<UnitGrid>(entity).Cell);
            JObject result = null; var steps = new JArray(); var clock = System.Diagnostics.Stopwatch.StartNew();
            SkirmishExpandedSessionComponent sessionAfter = default; SkirmishObjectiveStateComponent objectiveAfter = default;
            for (int i = 0; i < requestedTicks; i++) {
                try { result = AdvanceConstructedSourceOwners(); }
                catch (Exception error) { Console.Error.WriteLine("[S004ExperimentalTickFailure] attempted_tick=" + Tick + " completed=" + steps.Count + " error=" + error); throw; }
                if (!Entities.Exists(Session) || !Entities.HasComponent<SkirmishExpandedSessionComponent>(Session) ||
                    !Entities.HasComponent<SkirmishObjectiveStateComponent>(Session))
                    throw new InvalidOperationException("Original source session and objective must remain available for tick readback");
                sessionAfter = Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
                objectiveAfter = Entities.GetComponentData<SkirmishObjectiveStateComponent>(Session);
                int live = 0, moved = 0, pathRequests = 0, targetComponents = 0, engageComponents = 0;
                foreach (var entity in OpeningActors) {
                    if (!Entities.Exists(entity)) continue;
                    live++;
                    if (initialCells.TryGetValue(entity, out var initialCell) && Entities.HasComponent<UnitGrid>(entity) &&
                        !Entities.GetComponentData<UnitGrid>(entity).Cell.Equals(initialCell)) moved++;
                    if (Entities.HasComponent<UnitPathRequest>(entity)) pathRequests++;
                    if (Entities.HasComponent<UnitTarget>(entity)) targetComponents++;
                    if (Entities.HasComponent<EngageTarget>(entity)) engageComponents++;
                }
                steps.Add(new JObject { ["tick"] = Tick, ["phase"] = sessionAfter.Phase.ToString(), ["outcome"] = objectiveAfter.Outcome.ToString(),
                    ["terminal"] = objectiveAfter.Terminal != 0, ["existing_opening_actors"] = live, ["actors_changed_grid_cell"] = moved,
                    ["actors_with_path_request"] = pathRequests, ["actors_with_unit_target"] = targetComponents, ["actors_with_engage_target"] = engageComponents });
                if (objectiveAfter.Terminal != 0 || sessionAfter.Phase == SkirmishSessionPhase.Finished || sessionAfter.Phase == SkirmishSessionPhase.Failed) break;
            }
            clock.Stop();
            result["scope"] = "ExperimentalSourceOwnerSubset_NotFullScheduler";
            result["requested_ticks"] = requestedTicks; result["completed_ticks"] = steps.Count;
            result["source_steps"] = steps; result["advance_wall_seconds"] = clock.Elapsed.TotalSeconds;
            result["session_phase_after"] = sessionAfter.Phase.ToString();
            result["objective_outcome_after"] = objectiveAfter.Outcome.ToString();
            result["observed_opening_actor_count"] = OpeningActors.Count;
            result["source_owner_capabilities"] = new JObject {
                ["scope"] = "SameWorldOwnerBindings_NotMissionReadiness",
                ["production_catalog_record_present"] = Entities.HasComponent<SkirmishProductionCatalogRecord>(Session),
                ["building_definitions_bound"] = SourceDefinitions != null,
                ["unit_production_metadata_bound"] = ActualUnitMetadata != null,
                ["production_queue_owner_bound"] = ActualProduction != null,
                ["production_delivery_owner_bound"] = ActualDeliveryOwner != null,
                ["helicopter_delivery_bound"] = ActualDeliveryOwner?.Helicopters != null,
                ["source_aria_controller_bound"] = sourceS002Aria != null,
                ["numeric_camera_bound"] = SourceNumericCamera != null,
                ["mission_ready"] = false
            };
            if (Environment.GetEnvironmentVariable("WARLINE_S004_TRANSFORM_CENSUS") == "1")
            {
                int transforms = 0, localToWorld = 0, parents = 0, missingParents = 0, linkedRoots = 0, linkedMembers = 0;
                using (var query = Entities.CreateEntityQuery(typeof(Unity.Transforms.LocalTransform)))
                using (var rows = query.ToEntityArray(Allocator.Temp))
                foreach (var entity in rows)
                {
                    transforms++;
                    if (Entities.HasComponent<Unity.Transforms.LocalToWorld>(entity)) localToWorld++;
                    if (Entities.HasComponent<Unity.Transforms.Parent>(entity))
                    {
                        parents++;
                        if (!Entities.Exists(Entities.GetComponentData<Unity.Transforms.Parent>(entity).Value)) missingParents++;
                    }
                    if (Entities.HasBuffer<LinkedEntityGroup>(entity))
                    {
                        linkedRoots++;
                        linkedMembers += Entities.GetBuffer<LinkedEntityGroup>(entity, true).Length;
                    }
                }
                result["transform_census"] = new JObject { ["scope"] = "SameWorldReadOnly_LocalTransformQuery_NotTransformParity", ["local_transform_entities"] = transforms,
                    ["with_local_to_world"] = localToWorld, ["with_parent"] = parents, ["missing_parent_entities"] = missingParents,
                    ["linked_roots"] = linkedRoots, ["linked_members"] = linkedMembers, ["transform_owner_executed"] = false, ["native_parity"] = false };
                var actualKeys = new System.Collections.Generic.SortedSet<string>(StringComparer.Ordinal);
                var ownedNodes = new System.Collections.Generic.HashSet<Entity>();
                var nodeOwners = new System.Collections.Generic.Dictionary<Entity, Entity>();
                int ownRoots = 0, missingKeys = 0, duplicateNodes = 0, missingNodes = 0, foreignSessionRoots = 0;
                using (var query = Entities.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent)))
                using (var rows = query.ToEntityArray(Allocator.Temp))
                foreach (var root in rows)
                {
                    var ownership = Entities.GetComponentData<SkirmishAttemptOwnedComponent>(root);
                    if (!ownership.SessionId.Equals(sessionId)) { foreignSessionRoots++; continue; }
                    ownRoots++;
                    if (ownership.IsStructure == 0)
                    {
                        if (Entities.HasComponent<UnitSourcePrefabKey>(root)) actualKeys.Add(Entities.GetComponentData<UnitSourcePrefabKey>(root).Value.ToString());
                        else missingKeys++;
                    }
                    if (Entities.HasBuffer<LinkedEntityGroup>(root))
                    {
                        foreach (var member in Entities.GetBuffer<LinkedEntityGroup>(root, true))
                        {
                            if (!Entities.Exists(member.Value)) missingNodes++;
                            else if (!ownedNodes.Add(member.Value)) duplicateNodes++;
                            else nodeOwners.Add(member.Value, root);
                        }
                    }
                    else if (!ownedNodes.Add(root)) duplicateNodes++;
                    else nodeOwners.Add(root, root);
                }
                int ownedWithoutTransform = 0, crossRootParents = 0, parentsOutsideDomain = 0, cyclicOrDeep = 0, maximumDepth = 0;
                foreach (var entry in nodeOwners)
                {
                    if (!Entities.HasComponent<Unity.Transforms.LocalTransform>(entry.Key)) ownedWithoutTransform++;
                    if (!Entities.HasComponent<Unity.Transforms.Parent>(entry.Key)) continue;
                    var parent = Entities.GetComponentData<Unity.Transforms.Parent>(entry.Key).Value;
                    if (!nodeOwners.TryGetValue(parent, out var parentRoot)) parentsOutsideDomain++;
                    else if (parentRoot != entry.Value) crossRootParents++;
                    var visited = new System.Collections.Generic.HashSet<Entity>(); var node = entry.Key; int depth = 0;
                    while (Entities.Exists(node) && Entities.HasComponent<Unity.Transforms.Parent>(node))
                    {
                        if (!visited.Add(node) || ++depth > 64) { cyclicOrDeep++; break; }
                        node = Entities.GetComponentData<Unity.Transforms.Parent>(node).Value;
                    }
                    maximumDepth = Math.Max(maximumDepth, depth);
                }
                ((JObject)result["transform_census"])["same_session_owned_roots"] = ownRoots;
                ((JObject)result["transform_census"])["foreign_session_roots"] = foreignSessionRoots;
                ((JObject)result["transform_census"])["unique_owned_linked_nodes"] = ownedNodes.Count;
                ((JObject)result["transform_census"])["duplicate_owned_linked_nodes"] = duplicateNodes;
                ((JObject)result["transform_census"])["missing_owned_linked_nodes"] = missingNodes;
                ((JObject)result["transform_census"])["owned_nodes_without_local_transform"] = ownedWithoutTransform;
                ((JObject)result["transform_census"])["cross_root_parents"] = crossRootParents;
                ((JObject)result["transform_census"])["parents_outside_owned_domain"] = parentsOutsideDomain;
                ((JObject)result["transform_census"])["cyclic_or_deep_parent_chains"] = cyclicOrDeep;
                ((JObject)result["transform_census"])["maximum_parent_depth"] = maximumDepth;
                ((JObject)result["transform_census"])["actor_roots_missing_prefab_key"] = missingKeys;
                ((JObject)result["transform_census"])["observed_actor_prefab_keys"] = JArray.FromObject(actualKeys);
#if WARLINE_S004_TRANSFORM_PROJECTION
                result["transform_projection"] = S004TransformProjectionReadback.Read(Entities);
#endif
#if WARLINE_S004_TRANSFORM_BRIDGE
#if WARLINE_S004_TRANSFORM_SCHEDULE
                var afterProjection = S004TransformProjectionReadback.Read(Entities);
                result["source_transform_bridge_probe"] = new JObject { ["scope"] = "S004SourceOwnerSubset_ScheduledTransformBridge_NotFullScheduler",
                    ["bridge_step_executed"] = s004TransformSteps > 0, ["bridge_steps"] = s004TransformSteps,
                    ["entities_written_sum"] = s004TransformEntitiesWritten, ["before"] = transformBeforeTicks, ["after"] = afterProjection,
                    ["immediate_phase_readbacks"] = s004TransformPhaseReadbacks,
                    ["native_parity"] = false, ["full_scheduler"] = false, ["mission_ready"] = false };
#else
                if (requestedTicks != 1 || sourcePrefabKeys == null) throw new InvalidOperationException("One actual S004 bridge probe and captured prefab keys required");
                var beforeProjection = S004TransformProjectionReadback.Read(Entities);
                var bridge = new SourceMissionTransformBridge(Entities, Session, sourcePrefabKeys, SourceTransformConsumedReadset.VerifyForCurrentImage());
                int transformed = bridge.Step(Tick);
                var afterProjection = S004TransformProjectionReadback.Read(Entities);
                result["source_transform_bridge_probe"] = new JObject { ["scope"] = "OnePostTickS004SourceBridgeProbe_NotSchedulerAdmission",
                    ["bridge_step_executed"] = true, ["entities_written"] = transformed, ["before"] = beforeProjection, ["after"] = afterProjection,
                    ["native_parity"] = false, ["full_scheduler"] = false, ["mission_ready"] = false };
#endif
#endif
            }
#if WARLINE_S004_MISSILE_TICK
            result["source_missile_owners"] = s004MissileTickOwners.Observe();
#endif
#if WARLINE_S004_SCAN_ROPE_TICK
            result["source_scan_rope_owners"] = s004ScanRopeTickOwners.Observe();
#endif
            result["native_input_claimed"] = false; result["mission_ready"] = false;
            return result;
        }
#endif
        private sealed class S004GroundOwnerLease : IDisposable
        {
            private AutonomousWorld owner;
            internal S004GroundOwnerLease(AutonomousWorld world) { owner=world; }
            public void Dispose() { if(owner==null)return;
#if WARLINE_S004_EXPERIMENT_TICK
                owner.s004SupplementalTickOwners?.Dispose(); owner.s004SupplementalTickOwners=null;
#endif
                if(owner.s004SourceOwnersInitialized){owner.materialState.Begin();owner.material.OnDestroy(ref owner.materialState);owner.materialState.End();owner.combat?.Dispose();owner.resources.Dispose();owner.s004SourceOwnersInitialized=false;} owner.ground?.Dispose(); owner.ground=null; owner.s004GroundLeaseActive=false; owner=null; }
        }
        internal IDisposable LeaseS004GroundOwner()
        {
            if (initialRoads==null || ground!=null || disposed || s004GroundLeaseActive) throw new InvalidOperationException("Prepared S004 road authority required before leasing ground owner");
            s004GroundLeaseActive=true;
            return new S004GroundOwnerLease(this);
        }
        internal bool S004GroundOwnerRetained => ground!=null && initialRoads!=null;
        internal JObject BindS004FreshSourceStorage(string bundlePath,string bundleHash)
        {
            if(disposed || !s004GroundLeaseActive || ground==null || initialRoads==null ||
                Setup==null || Setup.CatalogId!="S004" || !Entities.Exists(Session) || Entities.BankBound || StorageBank!=null || freshStorageAuthority!=null)
                throw new InvalidOperationException("Unbound retained S004 source world required");
            initialRoads.RequireBeforePath();RequireExistingActiveSourceGameplay(Entities);
            var authority=FreshSourceStorageLifetimeAuthority.FromS004SourceDiagnostic(bundlePath,bundleHash,Setup,out var definition);
            using var query=Entities.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
            using var entities=query.ToEntityArray(Allocator.Temp);var ids=entities.ToArray();
            var values=ids.Select(Entities.GetComponentData<BuildingResourceStorageComponent>).ToArray();
            var present=ids.Select(Entities.HasComponent<BuildingResourceProductionProfileComponent>).ToArray();
            var profiles=ids.Select((e,i)=>present[i]?Entities.GetComponentData<BuildingResourceProductionProfileComponent>(e):default).ToArray();
            if(ids.Length==0)throw new InvalidOperationException("Observed S004 storage entities required");
            var bank=CanonicalStorageBank.CreateFreshSourceConfig(authority,ids,values,profiles,present);
            Entities.BindStorageBank(bank);
            if(!Entities.BankBound || !bank.IsFreshSourceOwnedBy(Entities))throw new InvalidOperationException("Same-world source bank binding failed");
            for(int i=0;i<ids.Length;i++){
                if(!CanonicalStorageBank.Same(values[i],Entities.GetComponentData<BuildingResourceStorageComponent>(ids[i])) ||
                    Entities.HasComponent<BuildingResourceProductionProfileComponent>(ids[i])!=present[i] ||
                    (present[i] && !CanonicalStorageBank.Same(profiles[i],Entities.GetComponentData<BuildingResourceProductionProfileComponent>(ids[i]))))
                    throw new InvalidOperationException("Canonical bank changed observed storage or profiles");
            }
            authority.RequireClaimedActual(definition,Setup,bundleHash);
            StorageBank=bank;freshStorageAuthority=authority;ActualDefinition=definition;
            var session=Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
            return new JObject { ["scope"]="S004SourceOnly_FreshCompiledAuthoredSetup",["storage_rows"]=ids.Length,
                ["profile_rows"]=present.Count(value=>value),["same_world_bank_bound"]=true,["before_after_rows_equal"]=true,
                ["original_setup_compiler_rechecked"]=true,["bundle_sha256"]=bundleHash,
                ["session_objective_kind"]=session.ObjectiveKind.ToString(),["setup_objective_kind"]=Setup.ObjectiveKind.ToString(),
                ["resolved_setup_component_present"]=Entities.HasComponent<SkirmishResolvedSetupComponent>(Session),
                ["objective_state_present"]=Entities.HasComponent<SkirmishObjectiveStateComponent>(Session),
                ["objective_clock_present"]=Entities.HasComponent<SkirmishObjectiveClockComponent>(Session),
                ["fog_state_present"]=Entities.HasComponent<SkirmishFogStateComponent>(Session),
                ["native_source_or_readiness_claimed"]=false,["mission_ready"]=false };
        }
        internal void InitializeS004ActiveSourceOwners()
        {
            if(disposed || !s004GroundLeaseActive || ground==null || initialRoads==null ||
                Setup==null || Setup.CatalogId!="S004" || !Entities.Exists(Session))
                throw new InvalidOperationException("Retained S004 source owner context required");
            initialRoads.RequireBeforePath();
            RequireExistingActiveSourceGameplay(Entities);
            if(!Entities.BankBound)throw new InvalidOperationException("Canonical source storage bank required before owner initialization");
            if(StorageBank==null || !StorageBank.IsFreshSourceOwnedBy(Entities))throw new InvalidOperationException("Same-world fresh source storage authority required");
            var session=Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
            if(session.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault ||
                !Entities.HasComponent<SkirmishResolvedSetupComponent>(Session) ||
                !Entities.HasComponent<SkirmishObjectiveStateComponent>(Session) ||
                Entities.GetComponentData<SkirmishObjectiveStateComponent>(Session).Kind!=SkirmishObjectiveKind.BaseAssault ||
                !Entities.HasComponent<SkirmishObjectiveClockComponent>(Session) || !Entities.HasComponent<SkirmishFogStateComponent>(Session))
                throw new InvalidOperationException("Original BaseAssault launch and objective state required before owner initialization");
            float day=ResolveS004SourcePresetDay(Entities,Session,Setup,System.IO.Path.GetFullPath("Assets/Game/Resources/SkirmishBaseAssault.asset"),"ff020b7c082ecbede2e0383731f0ca11443b715e8f625336174ebddf89c412fe");
            InitializeGeneratedSourceOwnersForExistingGameplay(day);
            s004SourceOwnersInitialized=true;
        }
        internal bool S004SourceOwnersInitialized => s004SourceOwnersInitialized;
        internal float S004SourceOwnerResourceDaySeconds => s004SourceOwnersInitialized ? resourceSecondsPerDay : throw new InvalidOperationException("S004 source owners are not initialized");
        internal static float ResolveS004SourcePresetDay(EntityManager em,Entity sessionEntity,SkirmishResolvedSetup setup,string presetPath,string expectedHash)
        {
            if(setup==null || setup.CatalogId!="S004" || !em.Exists(sessionEntity) || !em.HasComponent<SkirmishExpandedSessionComponent>(sessionEntity))
                throw new InvalidOperationException("Actual S004 source session and setup required");
            using var matches=em.CreateEntityQuery(typeof(SkirmishMatchState));
            if(matches.CalculateEntityCount()!=1 || matches.GetSingletonEntity()!=sessionEntity)
                throw new InvalidOperationException("Unique same-session Skirmish match required");
            var match=matches.GetSingleton<SkirmishMatchState>();var session=em.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity);
            if(!SkirmishMissionDispatch.TryGetLegacyIndex(setup.CatalogId,out int index) || match.ScenarioIndex!=index ||
                match.Seed!=setup.Seed || !match.SessionId.Equals(session.SessionId) ||
                SkirmishPresetConfig.SourcePresetResourceName(index)!="SkirmishBaseAssault")
                throw new InvalidOperationException("Original selected S004 preset identity differs");
            string exact=System.IO.Path.GetFullPath("Assets/Game/Resources/SkirmishBaseAssault.asset");
            if(presetPath!=exact || expectedHash!="ff020b7c082ecbede2e0383731f0ca11443b715e8f625336174ebddf89c412fe")
                throw new InvalidOperationException("Exact source preset identity required");
            var info=new System.IO.FileInfo(presetPath);
            if(!info.Exists || info.Length<=0 || info.Length>4*1024*1024 || (info.Attributes&System.IO.FileAttributes.ReparsePoint)!=0)
                throw new InvalidOperationException("Bounded regular source preset required");
            byte[] bytes=System.IO.File.ReadAllBytes(presetPath);using var hash=System.Security.Cryptography.SHA256.Create();
            string digest=BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
            if(digest!=expectedHash)throw new InvalidOperationException("Selected source preset hash differs");
            string text=System.Text.Encoding.UTF8.GetString(bytes);
            var fields=System.Text.RegularExpressions.Regex.Matches(text,@"(?m)^  supplyDaySeconds: (?<value>[^\r\n]+)\r?$");
            if(fields.Count!=1 || !float.TryParse(fields[0].Groups["value"].Value,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float day) || day<1f || float.IsNaN(day) || float.IsInfinity(day))
                throw new InvalidOperationException("Exact finite selected preset day required");
            return day; // The original Mathf.Max(1f, value) is identity for this admitted range.
        }
        internal JObject RunS004ActiveEnemyOwnerDiagnostic()
        {
            if (disposed || !s004GroundLeaseActive || ground==null || initialRoads==null ||
                !Entities.Exists(Session) || !Entities.HasComponent<SkirmishExpandedSessionComponent>(Session))
                throw new InvalidOperationException("Retained same-world S004 owners required");
            initialRoads.RequireBeforePath();
            RequireExistingActiveSourceGameplay(Entities);
            var session=Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
            if(session.Phase!=SkirmishSessionPhase.Playing || session.SpawnComplete!=1)
                throw new InvalidOperationException("Actual playing S004 source session required");
            var before=Entities.GetComponentData<SkirmishEnemyStrategyComponent>(Session);
            const int frame=9; const float dt=1f/60f;
            Entities.WorldUnmanaged.Time=new TimeData {ElapsedTime=frame/60.0,DeltaTime=dt};
            UnityEngine.Time.frameCount=frame;UnityEngine.Time.time=frame/60f;UnityEngine.Time.unscaledTime=frame/60f;
            UnityEngine.Time.deltaTime=dt;UnityEngine.Time.unscaledDeltaTime=dt;UnityEngine.Time.timeScale=1;
            var owner=new SkirmishEnemyStrategySystem();var state=new SystemState {EntityManager=Entities,Enabled=true};
            state.Begin();owner.OnCreate(ref state);state.End();
            state.Begin();if(!state.Enabled||!state.RequiredQueriesPresent)throw new InvalidOperationException("Actual enemy owner prerequisites unavailable");owner.OnUpdate(ref state);state.End();
            ground.EndSimulationPhase();
            var after=Entities.GetComponentData<SkirmishEnemyStrategyComponent>(Session);
            using var gameplayQuery=Entities.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            var gameplay=gameplayQuery.GetSingleton<RuntimeGameplayStateComponent>();
            var afterSession=Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
            return new JObject { ["scope"]="SingleOriginalEnemyOwnerUpdate_NotFullScheduler",["frame"]=frame,["delta_seconds"]=dt,
                ["owner_update_invoked"]=true,["priority_before"]=before.Priority.ToString(),["priority_after"]=after.Priority.ToString(),
                ["last_group_before"]=before.LastGroupId,["last_group_after"]=after.LastGroupId,
                ["simulation_active"]=(int)gameplay.SimulationActive,["session_phase"]=afterSession.Phase.ToString(),
                ["native_input_claimed"]=false,["scheduler_admitted"]=false,["mission_ready"]=false };
        }
        internal JObject RunS004OriginalAirOwnerDiagnostic()
        {
            if(disposed || !s004SourceOwnersInitialized || !s004GroundLeaseActive || ground==null || initialRoads==null)
                throw new InvalidOperationException("Initialized same-world S004 source owners required");
            initialRoads.RequireBeforePath();RequireExistingActiveSourceGameplay(Entities);
            var type=typeof(UnitAirMovementSystem);
            var create=type.GetMethod("OnCreate",new[]{typeof(SystemState).MakeByRefType()});
            var update=type.GetMethod("OnUpdate",new[]{typeof(SystemState).MakeByRefType()});
            if(create==null || update==null)throw new InvalidOperationException("Original air movement owner is not compiled");
            using var query=Entities.CreateEntityQuery(typeof(UnitAirMovement),typeof(UnitAirComponent),typeof(UnitMove),typeof(UnitGrid));
            int before=query.CalculateEntityCount();
            const int frame=10;const float dt=1f/60f;
            Entities.WorldUnmanaged.Time=new TimeData{ElapsedTime=frame/60.0,DeltaTime=dt};
            UnityEngine.Time.frameCount=frame;UnityEngine.Time.time=frame/60f;UnityEngine.Time.unscaledTime=frame/60f;
            UnityEngine.Time.deltaTime=dt;UnityEngine.Time.unscaledDeltaTime=dt;UnityEngine.Time.timeScale=1;
            object owner=Activator.CreateInstance(type);var state=new SystemState{EntityManager=Entities,Enabled=true};
            state.Begin();var call=new object[]{state};try{create.Invoke(owner,call);}finally{state=(SystemState)call[0];state.End();}
            bool ran=false;state.Begin();call[0]=state;
            try{if(state.Enabled&&state.RequiredQueriesPresent){update.Invoke(owner,call);ran=true;}}
            finally{state=(SystemState)call[0];state.End();}
            ground.EndSimulationPhase();int after=query.CalculateEntityCount();
            if(!Entities.Exists(Session) || !Entities.HasComponent<SkirmishExpandedSessionComponent>(Session))
                throw new InvalidOperationException("Retained source session required for transport readback");
            var session=Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
            var capacityType=System.Reflection.Assembly.GetExecutingAssembly().GetType("Game.Runtime.UnitTransportCapacitySystem");
            var resolveCapacity=capacityType?.GetMethod("ResolveTransportCargoCapacity",new[]{typeof(EntityManager),typeof(Entity)});
            if(resolveCapacity==null)throw new InvalidOperationException("Original transport capacity owner is not compiled");
            object capacityOwner=Activator.CreateInstance(capacityType);var capacityRows=new JArray();
            using(var transports=Entities.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(UnitTransportCapacity)))
            using(var rows=transports.ToEntityArray(Allocator.Temp))foreach(var entity in rows){
                var owned=Entities.GetComponentData<SkirmishAttemptOwnedComponent>(entity);if(!owned.SessionId.Equals(session.SessionId))continue;
                var capacity=(UnitTransportCargoCapacity)resolveCapacity.Invoke(capacityOwner,new object[]{Entities,entity});
                capacityRows.Add(new JObject{["entity_index"]=entity.Index,["faction"]=(int)owned.FactionId,
                    ["soldier_capacity"]=capacity.SoldierCapacity,["vehicle_capacity"]=capacity.VehicleCapacity,["cargo_weight_capacity"]=capacity.CargoWeightCapacity});
            }
            return new JObject{["scope"]="SingleOriginalAirMovementOwnerUpdate_NotFullScheduler",["frame"]=frame,["delta_seconds"]=dt,
                ["matching_air_entities_before"]=before,["matching_air_entities_after"]=after,["owner_update_invoked"]=ran,
                ["original_transport_capacity_readback"]=capacityRows,["transport_capacity_mutated"]=false,
                ["native_input_claimed"]=false,["scheduler_admitted"]=false,["mission_ready"]=false};
        }
        // Source-only experiment: original road-network sync before actors.
        // Base Campaign surface roads remain a separate immutable input.
        internal JObject PrepareS004InitialRoadDiagnostic(Entity grid, JObject run)
        {
            if (grid == Entity.Null || !Entities.Exists(grid) || Buildings.Count != 0 ||
                initializationAttempted || Session != Entity.Null || initialRoads != null)
                throw new InvalidOperationException("Fresh same-world S004 road projection required");
            PrepareFreshInitialRoads(run);
            initialRoads.Sync(Entities, grid);
            initialRoads.RequireBeforePath();
            return initialRoads.Receipt();
        }
        // This host has no managed DayNightSystem. Derive the fallback literal
        // from the pinned original production owner and reject any match state,
        // whose preset would override that fallback in the original owner.
        internal static float ResolveS004SourceOnlyResourceDay(EntityManager em, string ownerSourcePath, string expectedHash)
        {
            using (var query=em.CreateEntityQuery(typeof(SkirmishMatchState)))
                if (query.CalculateEntityCount()!=0) throw new InvalidOperationException("Skirmish match requires its original resource preset");
            if (String.IsNullOrWhiteSpace(ownerSourcePath) || !System.IO.Path.IsPathRooted(ownerSourcePath) ||
                String.IsNullOrWhiteSpace(expectedHash)) throw new InvalidOperationException("Pinned production source required");
            byte[] bytes=System.IO.File.ReadAllBytes(ownerSourcePath);
            using var hash=System.Security.Cryptography.SHA256.Create();
            string actual=BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
            if (!String.Equals(actual,expectedHash,StringComparison.Ordinal)) throw new InvalidOperationException("Production source hash differs");
            string source=System.Text.Encoding.UTF8.GetString(bytes);
            var matches=System.Text.RegularExpressions.Regex.Matches(source,@"float\s+secondsPerDay\s*=\s*context\.DayNightSystem\s*!=\s*null\s*\?\s*Mathf\.Max\(1f,\s*context\.DayNightSystem\.FullDayDurationMinutes\s*\*\s*60f\)\s*:\s*(?<fallback>[0-9]+(?:\.[0-9]+)?)f\s*;");
            if(matches.Count!=1 || !float.TryParse(matches[0].Groups["fallback"].Value,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float seconds) || !(seconds>0) || float.IsInfinity(seconds))
                throw new InvalidOperationException("Exact original resource fallback expression required");
            return seconds;
        }
        // Retains only the already constructed source diagnostic; owner scheduling
        // and managed/native loading admission remain separate requirements.
        internal void AttachS004DiagnosticStartup(Entity grid, Entity surface, Entity session,
            Entity boundary, SkirmishResolvedSetup setup, SkirmishArmyProfileConfig army,
            GeneratedStartingBuildingPipeline pipeline, GeneratedBuildingDefinitions definitions, Entity mapRoot, string setupPath, string setupHash)
        {
            if (disposed || initializationAttempted || Initialized || GridEntity != Entity.Null ||
                setup == null || army == null || pipeline == null || definitions == null || Buildings.Count != 20 ||
                !Entities.Exists(grid) || !Entities.Exists(surface) || !Entities.Exists(session) ||
                !Entities.Exists(boundary) || !Entities.Exists(mapRoot) ||
                !Entities.HasComponent<OperationMapMetadataComponent>(mapRoot) ||
                Entities.GetComponentData<OperationMapMetadataComponent>(mapRoot).PhysicalSourceValidated != 0 ||
                Entities.GetComponentData<SkirmishExpandedSessionComponent>(session).SpawnComplete != 1)
                throw new InvalidOperationException("Completed, unvalidated S004 source startup required");
            SourceS004CompiledSetupInput.RequireExact(setup, System.IO.Path.GetFullPath(setupPath), setupHash);
            GridEntity = grid; SurfaceEntity = surface; Session = session; Boundary = boundary;
            Setup = setup; Army = army; StartingPipeline = pipeline; SourceDefinitions = definitions;
            initializationAttempted = true;
            InitializationStage = "S004DiagnosticStartupRetained_SchedulerNotAdmitted";
        }
        private bool s004InactiveOwnersRun;
        internal JObject RunS004InactiveOwnerSubset(int ticks)
        {
            if (disposed || Initialized || !initializationAttempted || s004InactiveOwnersRun ||
                InitializationStage != "S004DiagnosticStartupRetained_SchedulerNotAdmitted" || ticks != 8)
                throw new InvalidOperationException("One bounded inactive S004 owner diagnostic required");
            s004InactiveOwnersRun = true;
            var em=Entities; var gridEntity=GridEntity;
            var grid=em.GetComponentData<GridConfig>(gridEntity);
            int count=checked(grid.Width*grid.Height);
            var sessionId=em.GetComponentData<SkirmishExpandedSessionComponent>(Session).SessionId;
            void Need(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
            int gameplayStates; using(var states=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent))) gameplayStates=states.CalculateEntityCount();
            Need(gameplayStates==0,"Fixture unexpectedly contains gameplay state");
            RuntimeGameplayStartTransition.ResetForGameplayStart(em);
            var gameplay=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)).GetSingleton<RuntimeGameplayStateComponent>();
            Need(gameplay.PlayRequested==1 && gameplay.SimulationActive==0,"Source start must remain inactive");
            if (!em.HasComponent<GridRoadSidewalk>(gridEntity) && !em.HasComponent<GridRoadDirt>(gridEntity)) {
                var sidewalks=em.AddBuffer<GridRoadSidewalk>(gridEntity); var dirt=em.AddBuffer<GridRoadDirt>(gridEntity);
                for(int i=0;i<count;i++){sidewalks.Add(default);dirt.Add(default);}
            } else {
                Need(em.HasComponent<GridRoadSidewalk>(gridEntity) && em.HasComponent<GridRoadDirt>(gridEntity) &&
                    em.GetBuffer<GridRoadSidewalk>(gridEntity).Length==count && em.GetBuffer<GridRoadDirt>(gridEntity).Length==count,
                    "Existing source road buffers must be complete");
            }
            em.AddComponentData(gridEntity,new PathPoolComponent {Cells=new NativeList<int2>(64,Allocator.Persistent)});
            var deferredOwner=new OwnedEndSimulationBuffers(); var deferredEntity=em.CreateEntity(); em.AddComponentData(deferredEntity,new EndSimulationEntityCommandBufferSystem.Singleton {Owner=deferredOwner});
            if(initialRoads!=null) deferred=deferredOwner;
            var movementOwner=new SkirmishWorldMovementSystem(); var movementState=new SystemState {EntityManager=em,Enabled=true};
            movementState.Begin(); movementOwner.OnCreate(ref movementState); movementState.End();
            var enemyOwner=new SkirmishEnemyStrategySystem(); var enemyState=new SystemState {EntityManager=em,Enabled=true};
            enemyState.Begin(); enemyOwner.OnCreate(ref enemyState); enemyState.End();
            var materialOwner=new MaterialFabricationSystem(); var materialState=new SystemState {EntityManager=em,Enabled=true};
            materialState.Begin(); materialOwner.OnCreate(ref materialState); materialState.End();
            var fuelOwner=new VehicleFuelConsumptionSystem(); var fuelState=new SystemState {EntityManager=em,Enabled=true};
            fuelState.Begin(); fuelOwner.OnCreate(ref fuelState); fuelState.End();
            JObject ResourceSnapshot() {
                var materials=new JArray(); var storage=new JArray(); int fuelUnits=0, fuelInitialized=0;
                using(var q=em.CreateEntityQuery(typeof(FactionTacticalMaterialsComponent))) using(var entities=q.ToEntityArray(Allocator.Temp)) foreach(var entity in entities) {
                    var value=em.GetComponentData<FactionTacticalMaterialsComponent>(entity);
                    materials.Add(new JObject { ["entity_index"]=entity.Index,["faction"]=(int)value.FactionId,["current"]=value.Current,["spent"]=value.LifetimeSpent,["fabricated"]=value.LifetimeFabricated });
                }
                using(var q=em.CreateEntityQuery(typeof(BuildingResourceStorageComponent))) using(var entities=q.ToEntityArray(Allocator.Temp)) foreach(var entity in entities) {
                    var value=em.GetComponentData<BuildingResourceStorageComponent>(entity);
                    storage.Add(new JObject { ["entity_index"]=entity.Index,["faction"]=(int)value.OwnerFactionId,["oil"]=value.StoredOilBarrels,["fuel"]=value.StoredFuelBarrels });
                }
                using(var q=em.CreateEntityQuery(typeof(UnitFuelConsumption),typeof(UnitFuelConsumptionState))) using(var entities=q.ToEntityArray(Allocator.Temp)) foreach(var entity in entities) {
                    fuelUnits++; if(em.GetComponentData<UnitFuelConsumptionState>(entity).Initialized!=0) fuelInitialized++;
                }
                int legacyMatches; using(var q=em.CreateEntityQuery(typeof(SkirmishMatchState))) legacyMatches=q.CalculateEntityCount();
                return new JObject { ["materials"]=materials,["building_storage"]=storage,["fuel_state_count"]=fuelUnits,["fuel_state_initialized"]=fuelInitialized,["legacy_match_state_count"]=legacyMatches };
            }
            var resourcesBefore=ResourceSnapshot();
            int CountEnemyPaths() {int total=0; using(var q=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(UnitPathRequest))) using(var es=q.ToEntityArray(Allocator.Temp)) foreach(var actor in es) {var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor); if(owned.SessionId.Equals(sessionId)&&owned.FactionId==2) total++;} return total;}
            int CountApplied() {int total=0; using(var q=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(UnitPathFollow))) using(var es=q.ToEntityArray(Allocator.Temp)) foreach(var actor in es) {var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor); if(owned.SessionId.Equals(sessionId)&&owned.FactionId==2) total++;} return total;}
            var timeline=new JArray(); var timer=System.Diagnostics.Stopwatch.StartNew();
            try {
            using(var combat=new OriginalCombatOwners(em)) {
            bool retainGround=initialRoads!=null;
            if(retainGround){initialRoads.RequireBeforePath();if(ground!=null||!s004GroundLeaseActive)throw new InvalidOperationException("One leased ground owner required");}
            var localGround=new OriginalGroundOwners(em,generatedFresh:true);
            if(retainGround)ground=localGround;
            try { for(int tick=1;tick<=8;tick++) {
                float now=tick/60f; em.WorldUnmanaged.Time=new TimeData {ElapsedTime=tick/60.0,DeltaTime=1f/60f}; UnityEngine.Time.frameCount=tick; UnityEngine.Time.time=now; UnityEngine.Time.unscaledTime=now; UnityEngine.Time.deltaTime=1f/60f; UnityEngine.Time.unscaledDeltaTime=1f/60f; UnityEngine.Time.timeScale=1;
                combat.EnsureStatePhase();
                materialState.Begin(); bool materialRan=materialState.Enabled&&materialState.RequiredQueriesPresent; if(materialRan)materialOwner.OnUpdate(ref materialState); materialState.End();
                bool validated=combat.ValidatePhase(); bool occupancyRan=localGround.OccupancyPhase();
                bool acquisitionRan=combat.AcquisitionPhase(); bool synchronized=combat.SynchronizePhase();
                int before=CountEnemyPaths(); bool ran=localGround.PathPhase(); int afterPath=CountEnemyPaths(); int applied=CountApplied();
                bool chaseRan=combat.ChasePhase(); bool gridMovementRan=localGround.MovementPhase();
                fuelState.Begin(); bool fuelRan=fuelState.Enabled&&fuelState.RequiredQueriesPresent; if(fuelRan)fuelOwner.OnUpdate(ref fuelState); fuelState.End();
                combat.AttackPhase(); combat.DefensePhase(); combat.DeathPhase();
                movementState.Begin(); Need(movementState.Enabled&&movementState.RequiredQueriesPresent,"Movement owner prerequisites missing"); movementOwner.OnUpdate(ref movementState); movementState.End();
                enemyState.Begin(); Need(enemyState.Enabled&&enemyState.RequiredQueriesPresent,"Enemy owner prerequisites missing"); enemyOwner.OnUpdate(ref enemyState); enemyState.End(); localGround.EndSimulationPhase();
                timeline.Add(new JObject {["tick"]=tick,["requests_before_path"]=before,["combat_validation_owner_ran"]=validated,["occupancy_owner_ran"]=occupancyRan,["material_owner_ran"]=materialRan,["fuel_owner_ran"]=fuelRan,["acquisition_owner_ran"]=acquisitionRan,["synchronization_owner_ran"]=synchronized,["chase_owner_ran"]=chaseRan,["grid_movement_owner_ran"]=gridMovementRan,["path_owner_ran"]=ran,["requests_after_path"]=afterPath,["applied_paths"]=applied,["requests_after_enemy"]=CountEnemyPaths(),["pending_path_job"]=localGround.HasPendingPath});
            }} finally {if(!retainGround)localGround.Dispose();}
            }
            } finally { materialState.Begin(); materialOwner.OnDestroy(ref materialState); materialState.End(); }
            timer.Stop();
            var resourcesAfter=ResourceSnapshot();
            Need(!(Environment.GetEnvironmentVariable("WARLINE_S004_DIRECT_RESOURCE_OWNER_DIAGNOSTIC")=="1" &&
                   Environment.GetEnvironmentVariable("WARLINE_S004_RESOURCE_CADENCE_DIAGNOSTIC")=="1"),
                "Choose one resource owner diagnostic before mutating the source world");
            JObject directResourceDiagnostic=null;
            if(Environment.GetEnvironmentVariable("WARLINE_S004_DIRECT_RESOURCE_OWNER_DIAGNOSTIC")=="1") {
                Need((int)resourcesAfter["legacy_match_state_count"]==0,"Legacy preset cadence requires its original owner");
                // This isolated source host has no managed DayNight owner. The original
                // composition's null-owner branch uses 300 seconds; this is not a
                // claim about the loaded native scene, which may initialize that owner.
                float sourceDaySeconds=ResolveS004SourceOnlyResourceDay(em,System.IO.Path.GetFullPath("Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickCompositionSystemHelper.cs"),"03ef47a92864080a4126f43c0e515e2d30f912959cb1a4f2abcfc6a8ec60f6cf");
                var owner=new FactionResourceCompositionSystemHelper();
                try {
                    var result=owner.UpdateResourceProduction(em,Buildings,sourceDaySeconds,1f/60f,2f);
                    directResourceDiagnostic=new JObject {
                        ["scope"]="DirectSourceOwnerDiagnostic_OutsideInactiveScheduler",
                        ["day_night_owner"]="AbsentFromSourceHost",
                        ["seconds_per_day"]=sourceDaySeconds,["delta_seconds"]=1f/60f,["oil_per_fuel"]=2f,
                        ["oil_extracted"]=result.OilExtractedBarrels,["fuel_produced"]=result.FuelProducedBarrels,
                        ["before"]=resourcesAfter,["after"]=ResourceSnapshot(),["native_cadence_qualified"]=false
                    };
                } finally { owner.Dispose(); }
            }
            JObject cadenceDiagnostic=null;
            if(Environment.GetEnvironmentVariable("WARLINE_S004_RESOURCE_CADENCE_DIAGNOSTIC")=="1") {
                Need(directResourceDiagnostic==null && (int)resourcesAfter["legacy_match_state_count"]==0,
                    "Unmodified source resources and nonlegacy cadence required");
                float sourceDaySeconds=ResolveS004SourceOnlyResourceDay(em,System.IO.Path.GetFullPath("Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickCompositionSystemHelper.cs"),"03ef47a92864080a4126f43c0e515e2d30f912959cb1a4f2abcfc6a8ec60f6cf");
                var owner=new FactionResourceCompositionSystemHelper();
                var calls=new JArray();float accumulated=0f;double next=0d;
                try {
                    for(int tick=1;tick<=120;tick++) {
                        const float dt=1f/60f;
                        float clamped=Game.Simulation.BuildingResourceCadenceKernel.ClampDelta(dt);
                        Game.Simulation.BuildingResourceCadenceKernel.AccumulateDelta(clamped,ref accumulated);
                        double now=tick/60.0;
                        if(Game.Simulation.BuildingResourceCadenceKernel.BeginProduction(now,false,ref next)) {
                            float elapsed=Game.Simulation.BuildingResourceCadenceKernel.ConsumeProductionDelta(clamped,ref accumulated);
                            var result=owner.UpdateResourceProduction(em,Buildings,sourceDaySeconds,elapsed,2f);
                            calls.Add(new JObject { ["tick"]=tick,["elapsed_seconds"]=elapsed,
                                ["oil_extracted"]=result.OilExtractedBarrels,["fuel_produced"]=result.FuelProducedBarrels });
                        }
                    }
                    cadenceDiagnostic=new JObject {
                        ["scope"]="DirectSourceCadenceAndOwner_OutsideInactiveScheduler",
                        ["clock"]="ExplicitFixedStepSourceDiagnostic",["ticks"]=120,["fixed_delta_seconds"]=1f/60f,
                        ["day_night_owner"]="AbsentFromSourceHost",["seconds_per_day"]=sourceDaySeconds,["oil_per_fuel"]=2f,
                        ["before"]=resourcesAfter,["after"]=ResourceSnapshot(),["calls"]=calls,
                        ["native_cadence_qualified"]=false
                    };
                } finally { owner.Dispose(); }
            }
            return new JObject { ["timeline"]=timeline,
                ["resources_before"]=resourcesBefore,["resources_after"]=resourcesAfter,
                ["direct_resource_owner_diagnostic"]=directResourceDiagnostic,
                ["resource_cadence_diagnostic"]=cadenceDiagnostic,
                ["runtime_gameplay_state_count_before_start"]=gameplayStates,
                ["advance_wall_seconds"]=timer.Elapsed.TotalSeconds,
                ["scheduler_admitted"]=false };
        }
    }
}
