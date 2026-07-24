using Game.Components;
using Game.Composition;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

public sealed class OperationMapEntityPresentationReadinessUtilityTests
{
    private const string OperationMapId = "opmap.skirmish.desert_base_01";
    private static readonly FixedString128Bytes MigrationHash = new(new string('a', 64));
    private World world;
    private Entity sceneEntity;
    private Entity sectionEntity;
    private Entity contractEntity;

    [SetUp]
    public void SetUp()
    {
        world = new World("OperationMapEntityPresentationReadinessUtilityTests");
        EntityManager entityManager = world.EntityManager;
        sceneEntity = entityManager.CreateEntity();
        sectionEntity = entityManager.CreateEntity();
        entityManager.AddBuffer<ResolvedSectionEntity>(sceneEntity).Add(
            new ResolvedSectionEntity { SectionEntity = sectionEntity });

        contractEntity = CreateSectionEntity(
            typeof(OperationMapEntityPresentationReadinessContract));
        entityManager.SetComponentData(
            contractEntity,
            new OperationMapEntityPresentationReadinessContract
            {
                OperationMapId = new FixedString128Bytes(OperationMapId),
                MigrationRecordSetHash = MigrationHash,
                ExpectedPresentationRootCount = 3,
                ExpectedGameplayBuildingCount = 1,
                ExpectedGameplayVehicleCount = 1,
                ExpectedRenderOnlyCount = 1,
                ExpectedGeneratedIdentityCount = 0,
                RequiresStaticPresentationPreload = 0
            });

        CreateRoot(1);
        CreateRoot(2);
        CreateRoot(3);
        CreateIdentity(1, "building");
        CreateIdentity(2, "vehicle");
        CreateIdentity(3, "render-only");
        CreateSectionEntity(typeof(OperationMapBuildingPresentation));
        CreateSectionEntity(typeof(OperationMapAuthoredVehiclePresentation));
    }

    [TearDown]
    public void TearDown()
    {
        if (world != null && world.IsCreated)
            world.Dispose();
    }

    [Test]
    public void TryValidate_AcceptsCompleteEntitySceneWithoutStaticPreload()
    {
        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                out string error),
            Is.True,
            error);
    }

    [Test]
    public void TryValidate_RejectsIncompleteIdentitySet()
    {
        OperationMapEntityPresentationReadinessContract contract =
            world.EntityManager.GetComponentData<
                OperationMapEntityPresentationReadinessContract>(contractEntity);
        contract.ExpectedRenderOnlyCount = 2;
        world.EntityManager.SetComponentData(contractEntity, contract);

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("identity counts"));
    }

    [Test]
    public void TryValidate_RejectsStaticPresentationPreloadRequirement()
    {
        OperationMapEntityPresentationReadinessContract contract =
            world.EntityManager.GetComponentData<
                OperationMapEntityPresentationReadinessContract>(contractEntity);
        contract.RequiresStaticPresentationPreload = 1;
        world.EntityManager.SetComponentData(contractEntity, contract);

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("prohibited static preload"));
    }

    [Test]
    public void TryValidate_AcceptsExplicitGeneratedIdentityTotals()
    {
        OperationMapEntityPresentationReadinessContract contract =
            world.EntityManager.GetComponentData<
                OperationMapEntityPresentationReadinessContract>(contractEntity);
        contract.ExpectedGameplayBuildingCount = 2;
        contract.ExpectedRenderOnlyCount = 2;
        contract.ExpectedGeneratedIdentityCount = 2;
        world.EntityManager.SetComponentData(contractEntity, contract);
        CreateGeneratedIdentity(1, "dense-building");
        CreateGeneratedIdentity(3, "dense-render-only");
        CreateSectionEntity(typeof(OperationMapBuildingPresentation));

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                out string error),
            Is.True,
            error);
    }

    [Test]
    public void TryValidate_RejectsDuplicateGeneratedIdentity()
    {
        OperationMapEntityPresentationReadinessContract contract =
            world.EntityManager.GetComponentData<
                OperationMapEntityPresentationReadinessContract>(contractEntity);
        contract.ExpectedGeneratedIdentityCount = 2;
        world.EntityManager.SetComponentData(contractEntity, contract);
        CreateGeneratedIdentity(1, "duplicate");
        CreateGeneratedIdentity(3, "duplicate");

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("duplicate generated identity"));
    }

    private Entity CreateSectionEntity(params ComponentType[] componentTypes)
    {
        Entity entity = world.EntityManager.CreateEntity(componentTypes);
        world.EntityManager.AddSharedComponent(
            entity,
            new SceneTag { SceneEntity = sectionEntity });
        return entity;
    }

    private void CreateRoot(byte role)
    {
        Entity entity = CreateSectionEntity(typeof(OperationMapEntityPresentationRoot));
        world.EntityManager.SetComponentData(
            entity,
            new OperationMapEntityPresentationRoot
            {
                OperationMapId = new FixedString128Bytes(OperationMapId),
                Role = role,
                SchemaVersion = 1,
                MigrationRecordSetHash = MigrationHash
            });
    }

    private void CreateIdentity(byte role, string sourceId)
    {
        Entity entity = CreateSectionEntity(typeof(OperationMapEntityPresentationIdentity));
        world.EntityManager.SetComponentData(
            entity,
            new OperationMapEntityPresentationIdentity
            {
                OperationMapId = new FixedString128Bytes(OperationMapId),
                SourceGlobalObjectId = new FixedString128Bytes(sourceId),
                Role = role,
                PlacementIndex = role
            });
    }

    private void CreateGeneratedIdentity(byte role, string stableId)
    {
        Entity entity = CreateSectionEntity(typeof(DenseCityPresentationIdentity));
        world.EntityManager.SetComponentData(
            entity,
            new DenseCityPresentationIdentity
            {
                StableId = new FixedString128Bytes(stableId),
                Role = role
            });
    }
}
