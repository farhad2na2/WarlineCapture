#if UNITY_EDITOR
using System;
using Game.Components;
using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    // Borrowed input views, invoked only when a fresh opt-in Editor observer is
    // attached. Observers must not retain buffers or change the world.
    internal static partial class S002PhysicalStartupObservation
    {
        // Material views are synchronous borrowed inputs. Never retain their buffers or mutate gameplay.
        internal static Func<EntityManager, bool, bool> ContinuousMaterialAdmission;
        internal static Action<MaterialFacts> ContinuousMaterial;
        internal sealed class MaterialSourceScope
        {
            internal EntityManager Manager; internal SystemHandle System; internal int Id;
            internal Entity Entity; internal byte Requester; internal bool Enabled, Ingress;
        }
        internal struct MaterialFacts
        {
            internal MaterialSourceScope Scope; internal string Stage; internal Entity Entity, Selected;
            internal int Index, Count, Health; internal byte Simulation, OwnerCount;
            internal bool After, Normal, LookupReached, Disabled, Death, Operational, HasMaterials, Applied;
            internal bool? Success; internal int RequestId; internal float Elapsed, Delta;
            internal MaterialFabricationComponent Fabrication; internal BuildingResourceStorageComponent Storage;
            internal FactionTacticalMaterialsComponent Materials; internal MaterialFabricationSystem.TickResult Tick;
            internal DynamicBuffer<MaterialFabricationRequestComponent> Requests;
            internal DynamicBuffer<MaterialFabricationResultComponent> Results;
        }
        internal static MaterialSourceScope MaterialBegin(EntityManager manager, SystemHandle system, float elapsed)
        {
            if (ContinuousMaterial == null || ContinuousMaterialAdmission == null) return null;
            try { if (!ContinuousMaterialAdmission(manager, false)) return null;
                var scope = new MaterialSourceScope { Manager=manager, System=system };
                ContinuousSend(ContinuousMaterial, new MaterialFacts { Scope=scope, Stage="update-before", Elapsed=elapsed });return scope;
            } catch (Exception ex) { ContinuousError(ex.ToString());return null; }
        }
        internal static bool MaterialRead(MaterialSourceScope scope, int cost) => scope != null && ContinuousMaterial != null && ContinuousRead(Math.Max(cost, 768));
        internal static void MaterialEmit(MaterialFacts facts) { ContinuousSend(ContinuousMaterial, facts); }
        internal static MaterialSourceScope MaterialIngressBegin(EntityManager manager, Entity entity, byte requester, bool enabled)
        {
            if (ContinuousMaterial == null || ContinuousMaterialAdmission == null) return null;
            try { if (!ContinuousMaterialAdmission(manager, true)) return null;
                var scope = new MaterialSourceScope { Manager=manager, Entity=entity, Requester=requester, Enabled=enabled, Ingress=true };
                ContinuousSend(ContinuousMaterial, new MaterialFacts { Scope=scope, Stage="ingress-before", Entity=entity });return scope;
            } catch (Exception ex) { ContinuousError(ex.ToString());return null; }
        }
        internal static void MaterialTickAfter(ref MaterialFacts facts, in MaterialFabricationComponent fabrication,
            in BuildingResourceStorageComponent storage, bool applied, in MaterialFabricationSystem.TickResult result)
        {
            if (!MaterialRead(facts.Scope, 96)) return;
            try { facts.Stage="tick-after";facts.Fabrication=fabrication;facts.Storage=storage;facts.Applied=applied;facts.Tick=result;
                if (facts.HasMaterials) facts.Materials=facts.Scope.Manager.GetComponentData<FactionTacticalMaterialsComponent>(facts.Selected);
                MaterialEmit(facts);
            } catch (Exception ex) { ContinuousError(ex.ToString()); }
        }
        internal static bool MaterialIngressResult(ref bool? observed, bool result) { observed=result;return result; }
        internal static void MaterialIngressEnd(MaterialSourceScope scope, bool? success, int requestId)
        {
            if (!MaterialRead(scope, 4)) return;
            MaterialEmit(new MaterialFacts { Scope=scope, Stage="ingress-after", Entity=scope.Entity, Success=success, RequestId=requestId });
        }
        internal static Action<EntityManager, BuildingRuntimeSpawnRequest, BuildingDefinition,
            BuildingRuntimeSpawnCompositionSystemHelper.Context, BuildingPlacementAuthoredBuildingCache,
            BuildingPlacementAuthoredRoadCache> BeforePlacement;
        internal static Action<EntityManager, BuildingRuntimeSpawnRequest> AfterPlacement;
        internal static Action<EntityManager, Entity, BuildingDefinition, int, Vector2Int, byte,
            Quaternion, RuntimeActorBirthContext> BeforeFactory;
        internal static Action<EntityManager, Entity, RuntimeActorBirthContext> AfterFactory;
        internal static Action<EntityManager, Entity, string, Entity[]> SupplyStage;
        // Editor observer owns the allocation until its read dependency is complete.
        // Jobs only write distinct EntityIndexInQuery slots; they never invoke managed code.
        internal static Func<EntityManager, float, ForwardGridFacts, ForwardTraceBuffers> BeginGridInterval;
        internal static Action<EntityManager, string, bool, int, bool> ForwardFuelBoundary;
        internal static Action<EntityManager, Entity> ForwardFuelUnit;
        internal static Action<EntityManager, Entity, int, float> ForwardFuelStorageVisit;
        internal static Action<NativeArray<float>> ForwardFuelRequested;
        internal static Action<EntityManager, EntityQuery, string, float, float, float> ForwardProductionBoundary;
        internal sealed class ForwardGridFacts
        {
            internal GridConfig Grid;
            internal NativeArray<int2> Pool;
            internal NativeArray<GridWalkable> Walkable;
            internal NativeBitArray DynamicBlocked, Occupied;
            internal NativeArray<byte> FriendlyPass;
            internal NativeArray<GridRoad> Roads;
            internal NativeArray<GridRoadSidewalk> Sidewalks;
            internal NativeArray<GridRoadDirt> DirtRoads;
            internal NativeArray<Entity> LiveEntities;
            internal NativeArray<UnitGrid> LiveGrids;
            internal NativeArray<UnitFootprint> LiveFootprints;
        }
        internal struct ForwardTraceBuffers
        {
            internal NativeArray<ForwardVehicleStep> Movement;
            internal NativeArray<ForwardCleanupStep> Cleanup;
        }
        internal struct ForwardCleanupStep
        {
            internal byte Seen;
            internal Entity Entity;
            internal int QueryIndex;
        }
        internal struct ForwardVehicleStep
        {
            internal byte Seen, Called, Moved;
            internal Entity Entity;
            internal int QueryIndex;
            internal LocalTransform Before, After;
            internal UnitVehicleKinematics KinematicsBefore, KinematicsAfter;
            internal UnitVehicleMovement Movement;
            internal float3 Direction;
            internal float Threshold, Speed, DeltaTime, MaxDistance;
        }
        // Editor-only synchronous main-thread facts; collector copies values before return.
        internal static Action<InitialHaulerProviderFacts> InitialHaulerProvider;
        static bool firstHaulerSelectionTaken, firstHaulerSelectionActive;
        static Entity firstHaulerSelectionUnit;
        static int firstHaulerProviderEvents;
        internal sealed class InitialHaulerProviderFacts
        {
            internal EntityManager Manager; internal Entity Unit;
            internal string Kind, Callsite; internal RuntimeBuildingEntity Building;
            internal Vector3 Position; internal byte Faction;
            internal bool ResolverPresent, HasInput; internal bool? Result;
            internal BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput Input;
        }
        internal static void BeginInitialHaulerSelection(EntityManager em, Entity unit)
        {
            ContinuousSelect(em, unit, "entry", null);
            if (InitialHaulerProvider == null || firstHaulerSelectionTaken) return;
            firstHaulerSelectionTaken = true; firstHaulerSelectionActive = true; firstHaulerSelectionUnit = unit;
            SendInitialHaulerProvider(em, "selection-entry", "TryAssignAutomaticHaulerOrder", null, default, 0, false, false, default, null);
        }
        internal static bool InitialHaulerSelectionResult(ref bool? result, bool value) { result = value; return value; }
        internal static void EndInitialHaulerSelection(EntityManager em, Entity unit, bool? result)
        {
            ContinuousSelect(em, unit, "exit", result);
            if (!firstHaulerSelectionActive || unit != firstHaulerSelectionUnit) return;
            try { SendInitialHaulerProvider(em, "selection-exit", "TryAssignAutomaticHaulerOrder", null, default, 0, false, false, default, result); }
            finally { firstHaulerSelectionActive = false; InitialHaulerProvider = null; }
        }
        internal static void ObserveInitialHaulerFocus(EntityManager em, RuntimeBuildingEntity building, Vector3 position, string callsite)
            { if (ContinuousProvider != null && ContinuousRead(20)) ContinuousSend(ContinuousProvider, new InitialHaulerProviderFacts { Manager = em, Kind = "focus-return", Callsite = callsite, Building = building, Position = position });
                SendInitialHaulerProvider(em, "focus-return", callsite, building, position, 0, false, false, default, null); }
        internal static void ObserveInitialHaulerAI(EntityManager em, byte faction, bool resolverPresent, bool hasInput,
            BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput input)
            { if (ContinuousProvider != null && ContinuousRead(20)) ContinuousSend(ContinuousProvider, new InitialHaulerProviderFacts { Manager = em, Kind = "ai-resolver-return", Callsite = "TryResolveCachedInput.actual-resolver-expression", Faction = faction, ResolverPresent = resolverPresent, HasInput = hasInput, Input = input });
                SendInitialHaulerProvider(em, "ai-resolver-return", "TryResolveCachedInput.actual-resolver-expression", null, default, faction, resolverPresent, hasInput, input, null); }
        static void SendInitialHaulerProvider(EntityManager em, string kind, string callsite, RuntimeBuildingEntity building,
            Vector3 position, byte faction, bool resolverPresent, bool hasInput,
            BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput input, bool? result)
        {
            if (InitialHaulerProvider == null || !firstHaulerSelectionActive) return;
            if (firstHaulerProviderEvents >= 256) { StorageWriterFailure = "Initial selection provider256event bound exceeded"; InitialHaulerProvider = null; return; }
            firstHaulerProviderEvents++;
            try { InitialHaulerProvider(new InitialHaulerProviderFacts { Manager = em, Unit = firstHaulerSelectionUnit,
                Kind = kind, Callsite = callsite, Building = building, Position = position, Faction = faction,
                ResolverPresent = resolverPresent, HasInput = hasInput, Input = input, Result = result }); }
            catch (Exception ex) { StorageWriterFailure = "Initial provider observer failed: " + ex; InitialHaulerProvider = null; }
        }
        internal static Action<InitialHaulerDispatchFacts> InitialHaulerDispatch;
        internal sealed class InitialHaulerDispatchFacts
        {
            internal EntityManager Manager;
            internal BuildingResourceHaulerBridgeCompositionSystemHelper.Context Context;
            internal System.Collections.Generic.List<Entity> Haulers;
            internal GridConfig Grid;
            internal int ActualQueryCount;
            internal bool HasPendingPathJob, HasSignature, ApproachQueryCreated, AIPolicyPresent, ContinuousAfter;
            internal uint LastSignature, NextReservationId;
            internal float NextRefreshAt, Now;
        }
        internal static void ObserveInitialHaulerDispatch(InitialHaulerDispatchFacts facts)
        {
            try { InitialHaulerDispatch?.Invoke(facts); }
            catch (Exception ex) { StorageWriterFailure = "Initial dispatch observer failed: " + ex; InitialHaulerDispatch = null; }
        }
        internal static Action<StorageWriterFacts> StorageWriter;
        internal static string StorageWriterFailure;
        internal static Entity StorageCallerUnit;
        internal static string StorageCallerSite;
        static long storageSequence;
        static int storageFactAllocations;
        internal static int StorageFactAllocations => storageFactAllocations;
        internal static int StorageReadonlySyncSkipped, StorageUnparentedWrites;
        static void UnsupportedStorageWrite(string stage)
        {
            ContinuousError("Unsupported unparented storage write: " + stage);
            StorageUnparentedWrites++;
            StorageWriterFailure = "Unsupported unparented actual storage write stage=" + stage +
                " facts=" + storageFactAllocations + " readonly_sync_skipped=" + StorageReadonlySyncSkipped;
            StorageWriter = null;
        }
        static bool StorageAllocation()
        {
            if (StorageWriter == null && ContinuousStorage == null) return false;
            if (storageFactAllocations >= 512) { StorageWriterFailure = "Storage writer fact allocation limit512 exceeded"; StorageWriter = null; return false; }
            storageFactAllocations++; return true;
        }
        static StorageWriterFacts activeStorage;
        internal sealed class StorageWriterFacts
        {
            internal long Invocation, Parent;
            internal string Stage, Operation, Callsite;
            internal EntityManager Manager;
            internal Entity Unit;
            internal RuntimeBuildingEntity Source, Destination, Building;
            internal byte Kind;
            internal float Amount;
            internal bool Deferred, UtilityPresent;
            internal uint Counter;
            internal bool? Result;
            internal UnitResourceHaulReservation Reservation;
            internal BuildingResourceStorageComponent Storage;
            internal bool HasStorage;
            internal bool HasHauler;
            internal UnitResourceHauler Hauler;
            internal StorageWriterFacts Previous;
        }
        internal static StorageWriterFacts StorageBegin(EntityManager em, string operation,
            RuntimeBuildingEntity source, RuntimeBuildingEntity destination, byte kind, float amount,
            bool deferred = false, uint counter = 0, bool utilityPresent = false,
            bool hasHauler = false, UnitResourceHauler hauler = default)
        {
            // Consume caller context even when detached/bounded out; never leak it to a later call.
            var callerUnit = StorageCallerUnit; var callerSite = StorageCallerSite;
            StorageCallerUnit = Entity.Null; StorageCallerSite = null;
            if (!StorageAllocation()) return null;
            try
            {
                var previous = activeStorage;
                var facts = new StorageWriterFacts { Invocation = ++storageSequence,
                    Parent = previous == null ? 0 : previous.Invocation, Previous = previous,
                    Stage = "entry", Operation = operation, Manager = em,
                    Unit = previous == null ? callerUnit : previous.Unit,
                    Callsite = previous == null ? callerSite : previous.Callsite,
                    Source = source, Destination = destination, Kind = kind, Amount = amount,
                    Deferred = deferred, Counter = counter, UtilityPresent = utilityPresent, HasHauler = hasHauler, Hauler = hauler };
                StorageCallerUnit = Entity.Null; StorageCallerSite = null;
                activeStorage = facts; StorageSend(facts); return facts;
            }
            catch (Exception ex) { StorageWriterFailure = ex.ToString(); StorageWriter = null; return null; }
        }
        internal static bool StorageResult(ref bool? result, bool value) { result = value; return value; }
        internal static void StorageEnd(StorageWriterFacts facts, bool? result,
            uint counter = 0, UnitResourceHaulReservation reservation = default,
            bool hasHauler = false, UnitResourceHauler hauler = default)
        {
            if (facts == null) return;
            try { facts.Stage = "exit"; facts.Result = result; facts.Counter = counter;
                facts.Reservation = reservation; facts.HasHauler = hasHauler; facts.Hauler = hauler; StorageSend(facts); }
            finally { activeStorage = facts.Previous; }
        }
        internal static void StoragePoint(EntityManager em, RuntimeBuildingEntity building, string stage,
            bool hasStorage, in BuildingResourceStorageComponent storage)
        {
            if (StorageWriter == null && ContinuousStorage == null) return;
            // Source-only readonly accesses are frequent queries, not writer calls.
            // Exclude only these two unparented stages BEFORE allocating facts.
            if (activeStorage == null)
            {
                if (stage == "raw-ecs-before-sync" || stage == "synchronized-local")
                { StorageReadonlySyncSkipped++; return; }
                UnsupportedStorageWrite(stage); return;
            }
            if (!StorageAllocation()) return;
            try { var parent = activeStorage; StorageSend(new StorageWriterFacts {
                Stage = stage, Operation = parent == null ? "unparented-access" : parent.Operation,
                Invocation = parent == null ? 0 : parent.Invocation, Parent = parent == null ? 0 : parent.Parent,
                Unit = parent == null ? Entity.Null : parent.Unit, Manager = em, Building = building,
                Callsite = parent == null ? null : parent.Callsite,
                Kind = parent == null ? (byte)0 : parent.Kind, Amount = parent == null ? 0f : parent.Amount,
                HasStorage = hasStorage, Storage = storage }); }
            catch (Exception ex) { StorageWriterFailure = ex.ToString(); StorageWriter = null; }
        }
        internal static void StorageMirror(FactionResourceCompositionSystemHelper.IResourceBuilding building,
            string stage, in BuildingResourceStorageComponent storage)
        {
            if (StorageWriter == null && ContinuousStorage == null) return;
            if (activeStorage == null) { UnsupportedStorageWrite(stage); return; }
            StoragePoint(activeStorage.Manager, building as RuntimeBuildingEntity, stage, true, storage);
        }
        static void StorageSend(StorageWriterFacts facts)
        {
            try { ContinuousSend(ContinuousStorage, facts); StorageWriter?.Invoke(facts); }
            catch (Exception ex) { StorageWriterFailure = ex.ToString(); StorageWriter = null; }
        }
        internal static Action<OriginalPlacementFacts> OriginalPlacement;
        internal static Action<Vector2Int, Vector2Int, GridConfig, float> FootprintCenter;
        internal sealed class OriginalPlacementFacts
        {
            internal BuildingDefinition Definition;
            internal Vector2Int Origin, Footprint;
            internal RectInt EffectiveRect;
            internal bool Rotated, HasInvalidPrefix;
            internal GridConfig Grid;
            internal int[] InvalidPrefix;
            internal int PrefixWidth, PrefixHeight;
            internal BuildingPlacementAuthoredBuildingCache Buildings;
            internal BuildingPlacementAuthoredRoadCache Roads;
            internal BuildingPlacementStartupSystemHelper Startup;
            internal BuildingGameplayDependencyCompositionSystemHelper Dependencies;
        }
    }
}
#endif
