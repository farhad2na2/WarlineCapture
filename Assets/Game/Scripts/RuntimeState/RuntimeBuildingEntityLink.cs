using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RuntimeBuildingEntityLink : MonoBehaviour
{
    private static readonly List<RuntimeBuildingEntityLink> RegisteredLinks = new();

    private BuildingPlacementInteractionSystem _buildingPlacementInteractionSystem;
    private BuildingPlacementInteractionSystem.Context _buildingPlacementInteractionContext;
    private int _buildingId;
    private Entity _entity;
    private Entity _blockerEntity;
    private Vector3 _positionOffset;
    private Vector3 _authoredLocalScale = Vector3.one;
    private bool _preserveAuthoredTransform;
    private bool _configured;

    public Entity Entity => _entity;
    public int BuildingId => _buildingId;

    public static void SyncRegisteredLinks()
    {
        for (int i = RegisteredLinks.Count - 1; i >= 0; i--)
        {
            RuntimeBuildingEntityLink link = RegisteredLinks[i];
            if (link == null)
            {
                RegisteredLinks.RemoveAt(i);
                continue;
            }

            link.SyncNow();
        }
    }

    public void Configure(
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        int buildingId,
        Entity entity,
        Entity blockerEntity)
    {
        _buildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
        _buildingPlacementInteractionContext = buildingPlacementInteractionContext;
        _buildingId = buildingId;
        _entity = entity != Entity.Null ? entity : blockerEntity;
        _blockerEntity = blockerEntity;
        MapAuthoredBuildingVisualComponent authoredVisual = GetComponent<MapAuthoredBuildingVisualComponent>();
        _preserveAuthoredTransform = authoredVisual != null && authoredVisual.PreserveAuthoredTransform;
        _authoredLocalScale = transform.localScale;
        CacheOffsetFromEntity();
        _configured = true;
    }

    public void SyncNow()
    {
        if (!_configured)
            return;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        var em = world.EntityManager;
        if (!em.Exists(_entity))
        {
            _buildingPlacementInteractionSystem?.HandleRuntimeBuildingEntityDestroyed(
                _buildingPlacementInteractionContext,
                _buildingId,
                _blockerEntity,
                gameObject);
            _configured = false;
            return;
        }

        if (!em.HasComponent<LocalTransform>(_entity))
            return;

        LocalTransform transformData = em.GetComponentData<LocalTransform>(_entity);
        Vector3 worldPosition = (Vector3)transformData.Position + _positionOffset;
        transform.SetPositionAndRotation(worldPosition, transformData.Rotation);
        transform.localScale = _preserveAuthoredTransform
            ? _authoredLocalScale
            : Vector3.one * transformData.Scale;
    }

    private void OnEnable()
    {
        if (RegisteredLinks.Contains(this))
            return;

        RegisteredLinks.Add(this);
    }

    private void OnDisable()
    {
        RegisteredLinks.Remove(this);
    }

    private void CacheOffsetFromEntity()
    {
        _positionOffset = Vector3.zero;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        var em = world.EntityManager;
        if (!em.Exists(_entity) || !em.HasComponent<LocalTransform>(_entity))
            return;

        LocalTransform transformData = em.GetComponentData<LocalTransform>(_entity);
        _positionOffset = transform.position - (Vector3)transformData.Position;
    }
}
