using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed partial class MatchHudSquadTraySelectionSystem : SystemBase
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager em);

    public readonly struct Context
    {
        public readonly Camera WorldCamera;
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Action<EntityManager> EnsureSelectionDependencies;
        public readonly Action<EntityManager, string> ClearCurrentSelection;
        public readonly Action ClearSelectedBuilding;
        public readonly Action<EntityManager, Entity> ApplyHudSelection;
        public readonly Action<int> ApplyHudSquadSelection;
        public readonly Action<string> LogSelectionDiagnostic;
        public readonly SelectionStateSystem SelectionStateSystem;
        public readonly FocusedUnitLifecycleSystem FocusedUnitLifecycleSystem;

        public Context(
            Camera worldCamera,
            TryGetEntityManagerDelegate tryGetEntityManager,
            Action<EntityManager> ensureSelectionDependencies,
            Action<EntityManager, string> clearCurrentSelection,
            Action clearSelectedBuilding,
            Action<EntityManager, Entity> applyHudSelection,
            Action<int> applyHudSquadSelection,
            Action<string> logSelectionDiagnostic,
            SelectionStateSystem selectionStateSystem,
            FocusedUnitLifecycleSystem focusedUnitLifecycleSystem)
        {
            WorldCamera = worldCamera;
            TryGetEntityManager = tryGetEntityManager;
            EnsureSelectionDependencies = ensureSelectionDependencies;
            ClearCurrentSelection = clearCurrentSelection;
            ClearSelectedBuilding = clearSelectedBuilding;
            ApplyHudSelection = applyHudSelection;
            ApplyHudSquadSelection = applyHudSquadSelection;
            LogSelectionDiagnostic = logSelectionDiagnostic;
            SelectionStateSystem = selectionStateSystem;
            FocusedUnitLifecycleSystem = focusedUnitLifecycleSystem;
        }
    }

    private readonly List<Candidate> _candidates = new();
    private readonly List<Candidate> _ranked = new();
    private readonly List<Entity> _selected = new();
    private readonly List<Entity> _lastSelected = new();
    private World _queryWorld;
    private EntityQuery _unitQuery;
    private MatchHudSquadTraySlot _activeSlot = MatchHudSquadTraySlot.None;

    public MatchHudSquadTraySlot ActiveSlot => _activeSlot;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void ClearActiveSlot(IMatchHudSquadTrayView view)
    {
        _activeSlot = MatchHudSquadTraySlot.None;
        view?.ClearActiveSlot();
    }

    public void SelectSlot(Context context, IMatchHudSquadTrayView view, MatchHudSquadTraySlot slot)
    {
        if (slot == MatchHudSquadTraySlot.None || view == null)
            return;

        if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
        {
            view.FlashDisabled(slot);
            return;
        }

        context.EnsureSelectionDependencies?.Invoke(em);
        EnsureEntityQueries(em);

        bool cycling = _activeSlot == slot;
        if (!TryBuildSelection(context, em, slot, cycling, _selected))
        {
            if (cycling && TryBuildSelection(context, em, slot, false, _selected))
                cycling = false;
            else
            {
                view.FlashDisabled(slot);
                context.LogSelectionDiagnostic?.Invoke($"result=SquadTraySelectSkipped slot={slot} reason=NoCandidates");
                return;
            }
        }

        ApplySelection(context, em, _selected);
        _activeSlot = slot;
        _lastSelected.Clear();
        _lastSelected.AddRange(_selected);

        view.SetSelectedSlot(slot);
        context.LogSelectionDiagnostic?.Invoke($"result=SquadTraySelect slot={slot} selected={_selected.Count} cycling={cycling}");
    }

    private bool TryBuildSelection(
        Context context,
        EntityManager em,
        MatchHudSquadTraySlot slot,
        bool excludePrevious,
        List<Entity> selected)
    {
        selected.Clear();
        CollectCandidates(context, em, slot, excludePrevious);
        if (_candidates.Count == 0)
            return false;

        int targetCount = GetTargetCount(slot);
        if (slot == MatchHudSquadTraySlot.Soldiers)
            SelectSoldierCluster(targetCount, selected);
        else
            SelectRanked(targetCount, selected);

        return selected.Count > 0;
    }

    private void CollectCandidates(
        Context context,
        EntityManager em,
        MatchHudSquadTraySlot slot,
        bool excludePrevious)
    {
        _candidates.Clear();
        _ranked.Clear();

        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<LocalToWorld> localToWorldType = em.GetComponentTypeHandle<LocalToWorld>(true);
        using NativeArray<ArchetypeChunk> chunks = _unitQuery.ToArchetypeChunkArray(Allocator.Temp);
        Vector3 cameraCenter = ResolveCameraCenter(context.WorldCamera);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<LocalToWorld> transforms = chunk.GetNativeArray(ref localToWorldType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!IsSelectablePlayerUnit(em, entity))
                    continue;
                if (excludePrevious && _lastSelected.Contains(entity))
                    continue;
                if (!MatchesSlot(em, entity, slot))
                    continue;

                Vector3 world = transforms[i].Position;
                Vector3 viewport = context.WorldCamera != null
                    ? context.WorldCamera.WorldToViewportPoint(world)
                    : new Vector3(0.5f, 0.5f, 1f);
                bool inViewport = viewport.z > 0f &&
                                  viewport.x >= 0f && viewport.x <= 1f &&
                                  viewport.y >= 0f && viewport.y <= 1f;
                float screenDistance = inViewport
                    ? new Vector2(viewport.x - 0.5f, viewport.y - 0.5f).sqrMagnitude
                    : float.MaxValue;
                float worldDistance = XzDistanceSquared(world, cameraCenter);
                _candidates.Add(new Candidate(entity, world, inViewport, screenDistance, worldDistance));
            }
        }
    }

    private void SelectRanked(int targetCount, List<Entity> selected)
    {
        _ranked.Clear();
        _ranked.AddRange(_candidates);
        _ranked.Sort(CompareCandidates);
        for (int i = 0; i < _ranked.Count && selected.Count < targetCount; i++)
            selected.Add(_ranked[i].Entity);
    }

    private void SelectSoldierCluster(int targetCount, List<Entity> selected)
    {
        _ranked.Clear();
        for (int i = 0; i < _candidates.Count; i++)
        {
            if (_candidates[i].InViewport)
                _ranked.Add(_candidates[i]);
        }

        if (_ranked.Count > 0)
        {
            _ranked.Sort(CompareCandidates);
            Candidate anchor = _ranked[0];
            _ranked.Sort((a, b) => XzDistanceSquared(a.WorldPosition, anchor.WorldPosition)
                .CompareTo(XzDistanceSquared(b.WorldPosition, anchor.WorldPosition)));
        }
        else
        {
            _ranked.AddRange(_candidates);
            _ranked.Sort(CompareCandidates);
            Candidate anchor = _ranked[0];
            _ranked.Sort((a, b) => XzDistanceSquared(a.WorldPosition, anchor.WorldPosition)
                .CompareTo(XzDistanceSquared(b.WorldPosition, anchor.WorldPosition)));
        }

        for (int i = 0; i < _ranked.Count && selected.Count < targetCount; i++)
            selected.Add(_ranked[i].Entity);
    }

    private void ApplySelection(Context context, EntityManager em, List<Entity> selected)
    {
        context.ClearCurrentSelection?.Invoke(em, "MatchHudSquadTray");
        context.ClearSelectedBuilding?.Invoke();

        for (int i = 0; i < selected.Count; i++)
        {
            Entity entity = selected[i];
            if (em.Exists(entity) && !em.HasComponent<SelectedUnitTag>(entity))
                em.AddComponent<SelectedUnitTag>(entity);
        }

        context.SelectionStateSystem.CacheSelectedMoveEntities(em, selected);
        context.FocusedUnitLifecycleSystem.ApplySelectionFocus(
            em,
            context.SelectionStateSystem,
            selected,
            selected.Count,
            (entityManager, entity) => context.ApplyHudSelection?.Invoke(entityManager, entity),
            count => context.ApplyHudSquadSelection?.Invoke(count));
    }

    private void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _unitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<LocalToWorld>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
    }

    private static bool IsSelectablePlayerUnit(EntityManager em, Entity entity)
    {
        if (em.HasComponent<Prefab>(entity) ||
            em.HasComponent<Disabled>(entity) ||
            em.HasComponent<StaticGridBlocker>(entity) ||
            em.HasComponent<UnitTransportPassenger>(entity))
        {
            return false;
        }

        if (!FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id))
            return false;

        return !em.HasComponent<UnitHealth>(entity) || em.GetComponentData<UnitHealth>(entity).Current > 0;
    }

    private static bool MatchesSlot(EntityManager em, Entity entity, MatchHudSquadTraySlot slot)
    {
        UnitKind kind = ResolveKind(em, entity);
        return slot switch
        {
            MatchHudSquadTraySlot.Soldiers => kind.IsSoldier,
            MatchHudSquadTraySlot.CombatVehicles => kind.IsCombatVehicle,
            MatchHudSquadTraySlot.AttackHelicopter => kind.IsAttackHelicopter,
            MatchHudSquadTraySlot.Jet => kind.IsJet,
            MatchHudSquadTraySlot.Transport => kind.IsTransport,
            _ => false
        };
    }

    private static UnitKind ResolveKind(EntityManager em, Entity entity)
    {
        string source = ResolveSource(em, entity);
        string lower = source.ToLowerInvariant();
        bool isAir = em.HasComponent<UnitAirMovement>(entity);
        bool hasTransport = em.HasComponent<UnitTransportCapacity>(entity) &&
                            em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity > 0;
        bool usesVehicleMotion = isAir ||
                                 (em.HasComponent<UnitMovementBehavior>(entity) &&
                                  em.GetComponentData<UnitMovementBehavior>(entity).UsesVehicleMotion != 0);
        bool namedTransport = ContainsAny(lower, "transport", "apc", "truck", "tanker", "hauler", "canopy");
        bool isTransport = hasTransport || namedTransport && (usesVehicleMotion || isAir);
        bool isSoldier = !usesVehicleMotion &&
                         ContainsAny(lower, "chr_soldier", "_soldier_") &&
                         !ContainsAny(lower, "civilian", "contractor", "pilot");
        bool isHelicopter = isAir && ContainsAny(lower, "helicopter", "heli");
        bool isJet = isAir && ContainsAny(lower, "jet", "plane") && !isTransport;
        bool isAttackHelicopter = isHelicopter && !isTransport;
        bool isCombatVehicle = usesVehicleMotion &&
                               !isAir &&
                               !isTransport &&
                               !ContainsAny(lower, "truck", "tanker", "hauler") &&
                               ContainsAny(lower, "veh", "tank", "armored", "launcher", "radar");

        return new UnitKind(isSoldier, isCombatVehicle, isAttackHelicopter, isJet, isTransport);
    }

    private static string ResolveSource(EntityManager em, Entity entity)
    {
        if (em.HasComponent<UnitSourcePrefabKey>(entity))
        {
            string source = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!string.IsNullOrWhiteSpace(source))
                return source;
        }

        if (em.HasComponent<UnitDisplayInfo>(entity))
        {
            string displayName = em.GetComponentData<UnitDisplayInfo>(entity).Name.ToString();
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
        }

        return em.GetName(entity);
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < needles.Length; i++)
        {
            if (value.Contains(needles[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static int GetTargetCount(MatchHudSquadTraySlot slot)
    {
        return slot switch
        {
            MatchHudSquadTraySlot.Soldiers => 4,
            MatchHudSquadTraySlot.CombatVehicles => 2,
            MatchHudSquadTraySlot.AttackHelicopter => 1,
            MatchHudSquadTraySlot.Jet => 1,
            MatchHudSquadTraySlot.Transport => 1,
            _ => 0
        };
    }

    private static int CompareCandidates(Candidate a, Candidate b)
    {
        int visible = b.InViewport.CompareTo(a.InViewport);
        if (visible != 0)
            return visible;

        int screen = a.ScreenDistanceSquared.CompareTo(b.ScreenDistanceSquared);
        if (screen != 0)
            return screen;

        return a.WorldDistanceSquared.CompareTo(b.WorldDistanceSquared);
    }

    private static Vector3 ResolveCameraCenter(Camera camera)
    {
        if (camera == null)
            return Vector3.zero;

        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Plane ground = new(Vector3.up, Vector3.zero);
        return ground.Raycast(ray, out float distance)
            ? ray.GetPoint(distance)
            : camera.transform.position;
    }

    private static float XzDistanceSquared(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return x * x + z * z;
    }

    private readonly struct Candidate
    {
        public readonly Entity Entity;
        public readonly Vector3 WorldPosition;
        public readonly bool InViewport;
        public readonly float ScreenDistanceSquared;
        public readonly float WorldDistanceSquared;

        public Candidate(Entity entity, Vector3 worldPosition, bool inViewport, float screenDistanceSquared, float worldDistanceSquared)
        {
            Entity = entity;
            WorldPosition = worldPosition;
            InViewport = inViewport;
            ScreenDistanceSquared = screenDistanceSquared;
            WorldDistanceSquared = worldDistanceSquared;
        }
    }

    private readonly struct UnitKind
    {
        public readonly bool IsSoldier;
        public readonly bool IsCombatVehicle;
        public readonly bool IsAttackHelicopter;
        public readonly bool IsJet;
        public readonly bool IsTransport;

        public UnitKind(bool isSoldier, bool isCombatVehicle, bool isAttackHelicopter, bool isJet, bool isTransport)
        {
            IsSoldier = isSoldier;
            IsCombatVehicle = isCombatVehicle;
            IsAttackHelicopter = isAttackHelicopter;
            IsJet = isJet;
            IsTransport = isTransport;
        }
    }
}
