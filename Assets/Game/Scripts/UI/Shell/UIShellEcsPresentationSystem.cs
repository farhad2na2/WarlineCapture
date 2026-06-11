using System.Collections.Generic;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UIShellEcsPresentationSystem : MonoBehaviour
{
    private static readonly ProfilerMarker TryGetBoundaryMarker = new("UIShellEcsPresentation.TryGetBoundary");
    private static readonly ProfilerMarker FlushCompletionMarker = new("UIShellEcsPresentation.FlushCompletion");
    private static readonly ProfilerMarker ReadCommandsMarker = new("UIShellEcsPresentation.ReadCommands");

    [SerializeField] private UIShellView shellView;

    private readonly List<UiShellPresentationCommandComponent> commandScratch = new();
    private World cachedWorld;
    private EntityQuery boundaryQuery;
    private Entity cachedBoundaryEntity;
    private bool hasBoundaryQuery;
    private bool isExecuting;
    private int activeSequenceId = -1;
    private bool hasPendingCompletion;
    private UiShellTransitionCompleteComponent pendingCompletion;

    private void Awake()
    {
        if (shellView == null)
            shellView = GetComponent<UIShellView>();
    }

    private void Update()
    {
        EntityManager entityManager;
        Entity boundary;
        using (TryGetBoundaryMarker.Auto())
        {
            if (!TryGetBoundaryEntity(out entityManager, out boundary))
                return;
        }

        using (FlushCompletionMarker.Auto())
        {
            FlushPendingCompletion(entityManager, boundary);
        }

        if (isExecuting || shellView == null)
            return;

        DynamicBuffer<UiShellPresentationCommandComponent> commands;
        using (ReadCommandsMarker.Auto())
        {
            commands = entityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary);
            if (commands.Length == 0)
                return;
        }

        commandScratch.Clear();
        for (int i = 0; i < commands.Length; i++)
            commandScratch.Add(commands[i]);
        commands.Clear();

        UiShellPresentationCommandComponent finalCommand = commandScratch[commandScratch.Count - 1];
        activeSequenceId = finalCommand.SequenceId;
        isExecuting = true;

        shellView.ExecuteCommandSequence(commandScratch, activeSequenceId, completedSequenceId =>
        {
            if (completedSequenceId != activeSequenceId)
                return;

            pendingCompletion = new UiShellTransitionCompleteComponent
            {
                Kind = finalCommand.Kind,
                Region = finalCommand.Region,
                SequenceId = completedSequenceId
            };
            hasPendingCompletion = true;
            isExecuting = false;
        });
    }

    public void Configure(UIShellView view)
    {
        shellView = view;
    }

    private bool TryGetBoundaryEntity(out EntityManager entityManager, out Entity boundary)
    {
        entityManager = default;
        boundary = Entity.Null;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        if (cachedWorld != world || !hasBoundaryQuery)
        {
            cachedWorld = world;
            cachedBoundaryEntity = Entity.Null;
            boundaryQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellBoundaryComponent>(),
                ComponentType.ReadWrite<UiShellPresentationCommandComponent>(),
                ComponentType.ReadWrite<UiShellTransitionCompleteComponent>());
            hasBoundaryQuery = true;
        }

        entityManager = world.EntityManager;
        if (cachedBoundaryEntity != Entity.Null &&
            entityManager.Exists(cachedBoundaryEntity) &&
            entityManager.HasComponent<UiShellBoundaryComponent>(cachedBoundaryEntity) &&
            entityManager.HasBuffer<UiShellPresentationCommandComponent>(cachedBoundaryEntity) &&
            entityManager.HasBuffer<UiShellTransitionCompleteComponent>(cachedBoundaryEntity))
        {
            boundary = cachedBoundaryEntity;
            return true;
        }

        if (boundaryQuery.IsEmptyIgnoreFilter)
            return false;

        boundary = boundaryQuery.GetSingletonEntity();
        cachedBoundaryEntity = boundary;
        return true;
    }

    private void FlushPendingCompletion(EntityManager entityManager, Entity boundary)
    {
        if (!hasPendingCompletion)
            return;

        DynamicBuffer<UiShellTransitionCompleteComponent> completions =
            entityManager.GetBuffer<UiShellTransitionCompleteComponent>(boundary);
        completions.Add(pendingCompletion);
        hasPendingCompletion = false;
    }
}
