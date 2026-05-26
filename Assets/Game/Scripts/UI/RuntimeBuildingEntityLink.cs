using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class RuntimeBuildingEntityLink : MonoBehaviour
{
    private static readonly HashSet<RuntimeBuildingEntityLink> ActiveLinks = new();

    private BuildingPlacementInteractionSystem _buildingPlacementInteractionSystem;
    private BuildingPlacementInteractionSystem.Context _buildingPlacementInteractionContext;
    private int _buildingId;
    private Entity _entity;
    private Entity _blockerEntity;
    private Vector3 _positionOffset;
    private bool _configured;

    public Entity Entity => _entity;
    public int BuildingId => _buildingId;

    public static IEnumerable<RuntimeBuildingEntityLink> GetActiveLinks()
    {
        return ActiveLinks;
    }

    private void OnEnable()
    {
        ActiveLinks.Add(this);
    }

    private void OnDisable()
    {
        ActiveLinks.Remove(this);
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
        CacheOffsetFromEntity();
        _configured = true;
    }

    private void Update()
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
        transform.localScale = Vector3.one * transformData.Scale;
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
