#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Game.Components;
namespace Game.Runtime {
    internal static partial class S002PhysicalStartupObservation {
        internal static string ContinuousFailure;
        internal static Action<string> ContinuousFailed;
        internal static Func<int, bool> ContinuousBeforeRead;
        internal static Action<ContinuousOwnerFacts> ContinuousOwner;
        internal static Action<InitialHaulerDispatchFacts> ContinuousDispatch;
        internal static Action<EntityManager, Entity, string, bool?> ContinuousSelection;
        internal static Action<InitialHaulerProviderFacts> ContinuousProvider;
        internal static Action<ContinuousFuelFacts> ContinuousFuel;
        internal static Action<EntityManager, Entity, int2, byte> ContinuousFuelStatePost;
        internal static Action<EntityManager, string, bool, int, bool, int, GridConfig> ContinuousFuelBoundary;
        internal static Action<NativeArray<float>> ContinuousRequested;
        internal static Action<EntityManager, Entity, int, float> ContinuousDrainVisit;
        internal static Action<EntityManager, Entity, int, float, float, uint> ContinuousDrainPost;
        internal static Action<EntityManager, NativeArray<Entity>> ContinuousRoots;
        internal static Action<ContinuousSyncFacts> ContinuousSync;
        internal static Action<ContinuousStatusFacts> ContinuousStatus;
        internal struct ContinuousStatusFacts { internal EntityManager Manager; internal Entity Entity; internal UnitResourceHaulStatus Status; internal bool EnteredBlockedEpisode, After; internal byte ResourceKind; }
        internal static Action<StorageWriterFacts> ContinuousStorage;
        internal struct ContinuousOwnerFacts { internal object Owner; internal double Now, BeforeDue, AfterDue; internal bool After, Invoke, Disabled, CallbackPresent; }
        internal struct ContinuousFuelFacts {
            internal EntityManager Manager; internal Entity Entity;
            internal int2 Cell, LastCell; internal byte Faction, Motion, Enabled, Initialized;
            internal float GroundRate, AirRate; internal bool Air, HasTransform, HasGrid;
            internal float3 Position; internal GridConfig Grid;
        }
        internal struct ContinuousSyncFacts {
            internal EntityManager Manager; internal RuntimeBuildingEntity Building;
            internal bool After, HasProfile; internal BuildingResourceStorageComponent Before, Prepared;
            internal BuildingResourceProductionProfileComponent Profile;
        }
        internal static void ContinuousError(string reason) {
            if (ContinuousFailure != null || ContinuousBeforeRead == null) return;
            try { if (!ContinuousBeforeRead(0)) return; } catch { return; }
            ContinuousFailure = reason;
            try { ContinuousFailed?.Invoke(reason); } catch { /* Never throw into gameplay. */ }
        }
        internal static bool ContinuousRead(int work) {
            try { return ContinuousFailure == null && ContinuousBeforeRead != null && ContinuousBeforeRead(work); }
            catch (Exception ex) { ContinuousError(ex.ToString()); return false; }
        }
        internal static void ContinuousSend<T>(Action<T> callback, T facts) {
            if (callback == null || ContinuousFailure != null || !ContinuousRead(1)) return;
            try { callback(facts); } catch (Exception ex) { ContinuousError(ex.ToString()); }
        }
        internal static void ContinuousSelect(EntityManager em, Entity unit, string stage, bool? result) {
            if (ContinuousSelection == null || ContinuousFailure != null || !ContinuousRead(1)) return;
            try { ContinuousSelection(em, unit, stage, result); } catch (Exception ex) { ContinuousError(ex.ToString()); }
        }
        internal static void ContinuousRootSnapshot(EntityManager em, NativeArray<Entity> roots) {
            if (ContinuousRoots == null || ContinuousFailure != null || !ContinuousRead(1)) return;
            try { ContinuousRoots(em, roots); } catch (Exception ex) { ContinuousError(ex.ToString()); }
        }
        internal static void ContinuousFuelStart(EntityManager em, string boundary, bool steel, int expanded, bool legacy, int grids, GridConfig grid) {
            if (ContinuousFuelBoundary == null || ContinuousFailure != null || !ContinuousRead(1)) return;
            try { ContinuousFuelBoundary(em, boundary, steel, expanded, legacy, grids, grid); } catch (Exception ex) { ContinuousError(ex.ToString()); }
        }
        internal static void ContinuousStatePost(EntityManager em, Entity entity, int2 cell, byte initialized) {
            if (ContinuousFuelStatePost == null || ContinuousFailure != null || !ContinuousRead(1)) return;
            try { ContinuousFuelStatePost(em, entity, cell, initialized); } catch (Exception ex) { ContinuousError(ex.ToString()); }
        }
        internal static void ContinuousVisit(EntityManager em, Entity entity, int faction, float remaining) {
            if (ContinuousDrainVisit == null || ContinuousFailure != null || !ContinuousRead(1)) return;
            try { ContinuousDrainVisit(em, entity, faction, remaining); } catch (Exception ex) { ContinuousError(ex.ToString()); }
        }
        internal static void ContinuousPost(EntityManager em, Entity entity, int faction, float remaining, float balance, uint version) {
            if (ContinuousDrainPost == null || ContinuousFailure != null || !ContinuousRead(1)) return;
            try { ContinuousDrainPost(em, entity, faction, remaining, balance, version); } catch (Exception ex) { ContinuousError(ex.ToString()); }
        }
    }
}
#endif
