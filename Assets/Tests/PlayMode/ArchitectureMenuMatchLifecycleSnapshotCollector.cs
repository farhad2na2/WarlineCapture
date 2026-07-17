using System;
using System.Collections.Generic;
using Game.Components;
using Game.Composition;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

internal enum ArchitectureLifecycleCheckpointPhase : byte
{
    Menu = 0,
    Match = 1
}

internal readonly struct ArchitectureMenuMatchLifecycleSnapshot
{
    public ArchitectureMenuMatchLifecycleSnapshot(
        int cycle,
        ArchitectureLifecycleCheckpointPhase phase,
        ulong worldSequence,
        int loadedSceneCount,
        int sceneRootCount,
        int totalEntityCount,
        int managedSystemCount,
        SystemHandle shellFlowSystemHandle,
        SystemHandle actionRequestSystemHandle,
        int lifecycleRootCount,
        int operationMapRootCount,
        int menuViewCount,
        int matchViewCount,
        int matchHudCount,
        int enabledAudioListenerCount,
        int missileTrailRootCount)
    {
        Cycle = cycle;
        Phase = phase;
        WorldSequence = worldSequence;
        LoadedSceneCount = loadedSceneCount;
        SceneRootCount = sceneRootCount;
        TotalEntityCount = totalEntityCount;
        ManagedSystemCount = managedSystemCount;
        ShellFlowSystemHandle = shellFlowSystemHandle;
        ActionRequestSystemHandle = actionRequestSystemHandle;
        LifecycleRootCount = lifecycleRootCount;
        OperationMapRootCount = operationMapRootCount;
        MenuViewCount = menuViewCount;
        MatchViewCount = matchViewCount;
        MatchHudCount = matchHudCount;
        EnabledAudioListenerCount = enabledAudioListenerCount;
        MissileTrailRootCount = missileTrailRootCount;
    }

    public int Cycle { get; }
    public ArchitectureLifecycleCheckpointPhase Phase { get; }
    public ulong WorldSequence { get; }
    public int LoadedSceneCount { get; }
    public int SceneRootCount { get; }
    public int TotalEntityCount { get; }
    public int ManagedSystemCount { get; }
    public SystemHandle ShellFlowSystemHandle { get; }
    public SystemHandle ActionRequestSystemHandle { get; }
    public int LifecycleRootCount { get; }
    public int OperationMapRootCount { get; }
    public int MenuViewCount { get; }
    public int MatchViewCount { get; }
    public int MatchHudCount { get; }
    public int EnabledAudioListenerCount { get; }
    public int MissileTrailRootCount { get; }

    public string ToCompactString()
    {
        return $"cycle={Cycle} phase={Phase} world={WorldSequence} scenes={LoadedSceneCount} " +
               $"roots={SceneRootCount} entities={TotalEntityCount} systems={ManagedSystemCount} " +
               $"shellFlow={ShellFlowSystemHandle} actions={ActionRequestSystemHandle} " +
               $"lifecycle={LifecycleRootCount} map={OperationMapRootCount} menu={MenuViewCount} " +
               $"match={MatchViewCount} hud={MatchHudCount} listeners={EnabledAudioListenerCount} " +
               $"missileVfx={MissileTrailRootCount}";
    }
}

internal sealed class ArchitectureMenuMatchLifecycleSnapshotCollector : IDisposable
{
    private const string MissileTrailRootName = "MissileTrailVfxView";

    private readonly World world;
    private readonly UIShellContentView shellContent;
    private readonly EntityQuery lifecycleRootQuery;
    private readonly EntityQuery operationMapRootQuery;
    private readonly List<GameObject> sceneRoots = new(64);
    private readonly List<MenuBootstrapView> menuViews = new(2);
    private readonly List<MatchSceneView> matchViews = new(2);
    private readonly List<MatchHudFooterContentView> matchHudViews = new(2);
    private readonly List<AudioListener> audioListeners = new(2);
    private bool disposed;

    public ArchitectureMenuMatchLifecycleSnapshotCollector(World world, UIShellContentView shellContent)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.shellContent = shellContent != null
            ? shellContent
            : throw new ArgumentNullException(nameof(shellContent));
        if (!world.IsCreated)
            throw new ArgumentException("Lifecycle snapshot collection requires a created World.", nameof(world));

        lifecycleRootQuery = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<SceneLifecycleRootComponent>());
        operationMapRootQuery = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapRootComponent>());
    }

    public ArchitectureMenuMatchLifecycleSnapshot Capture(
        int cycle,
        ArchitectureLifecycleCheckpointPhase phase)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(ArchitectureMenuMatchLifecycleSnapshotCollector));
        if (!world.IsCreated)
            throw new InvalidOperationException("The lifecycle World was destroyed during snapshot collection.");

        int loadedSceneCount = 0;
        int sceneRootCount = 0;
        int menuViewCount = 0;
        int matchViewCount = 0;
        int matchHudCount = 0;
        int enabledAudioListenerCount = 0;
        int missileTrailRootCount = 0;

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            loadedSceneCount++;
            sceneRoots.Clear();
            scene.GetRootGameObjects(sceneRoots);
            sceneRootCount += sceneRoots.Count;
            for (int rootIndex = 0; rootIndex < sceneRoots.Count; rootIndex++)
            {
                GameObject root = sceneRoots[rootIndex];
                if (root.name == MissileTrailRootName)
                    missileTrailRootCount++;

                menuViewCount += CountComponents(root, menuViews);
                matchViewCount += CountComponents(root, matchViews);
                audioListeners.Clear();
                root.GetComponentsInChildren(true, audioListeners);
                for (int listenerIndex = 0; listenerIndex < audioListeners.Count; listenerIndex++)
                {
                    AudioListener listener = audioListeners[listenerIndex];
                    if (listener != null && listener.enabled && listener.gameObject.activeInHierarchy)
                        enabledAudioListenerCount++;
                }
            }
        }

        matchHudCount = CountComponents(shellContent.gameObject, matchHudViews);

        return new ArchitectureMenuMatchLifecycleSnapshot(
            cycle,
            phase,
            world.SequenceNumber,
            loadedSceneCount,
            sceneRootCount,
            world.EntityManager.UniversalQuery.CalculateEntityCount(),
            world.Systems.Count,
            world.GetExistingSystem<UiShellFlowSystem>(),
            world.GetExistingSystem<UiActionRequestSystem>(),
            lifecycleRootQuery.CalculateEntityCount(),
            operationMapRootQuery.CalculateEntityCount(),
            menuViewCount,
            matchViewCount,
            matchHudCount,
            enabledAudioListenerCount,
            missileTrailRootCount);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        lifecycleRootQuery.Dispose();
        operationMapRootQuery.Dispose();
    }

    private static int CountComponents<T>(GameObject root, List<T> buffer) where T : Component
    {
        buffer.Clear();
        root.GetComponentsInChildren(true, buffer);
        return buffer.Count;
    }
}
