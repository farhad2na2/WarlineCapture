using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WarlineCaptureShellEcsBridgeView : MonoBehaviour
{
    [SerializeField] private WarlineCaptureShellView shellView;

    private readonly List<UiShellPresentationCommandComponent> commandScratch = new();
    private World cachedWorld;
    private EntityQuery boundaryQuery;
    private bool hasBoundaryQuery;
    private bool isExecuting;
    private int activeSequenceId = -1;
    private bool hasPendingCompletion;
    private UiShellTransitionCompleteComponent pendingCompletion;

    private void Awake()
    {
        if (shellView == null)
            shellView = GetComponent<WarlineCaptureShellView>();
    }

    private void Update()
    {
        if (!TryGetBoundaryEntity(out EntityManager entityManager, out Entity boundary))
            return;

        FlushPendingCompletion(entityManager, boundary);

        if (isExecuting || shellView == null)
            return;

        DynamicBuffer<UiShellPresentationCommandComponent> commands =
            entityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary);
        if (commands.Length == 0)
            return;

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

    public void Configure(WarlineCaptureShellView view)
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
            boundaryQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellBoundaryComponent>(),
                ComponentType.ReadWrite<UiShellPresentationCommandComponent>(),
                ComponentType.ReadWrite<UiShellTransitionCompleteComponent>());
            hasBoundaryQuery = true;
        }

        if (boundaryQuery.IsEmptyIgnoreFilter)
            return false;

        entityManager = world.EntityManager;
        boundary = boundaryQuery.GetSingletonEntity();
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
