using System;
using System.Collections.Generic;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;
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
        int missileTrailRootCount,
        int audioRuntimeViewCount,
        int audioPoolSize,
        int activeAudioSourceCount,
        int pathPoolOwnerCount,
        int pathPoolLength,
        int pathPoolCapacity,
        int missileTrailCreatedCount,
        int missileTrailActiveCount,
        int impactVfxCreatedCount,
        int impactVfxActiveCount,
        long totalAllocatedMemoryBytes,
        long totalReservedMemoryBytes,
        long monoUsedMemoryBytes,
        long monoHeapMemoryBytes)
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
        AudioRuntimeViewCount = audioRuntimeViewCount;
        AudioPoolSize = audioPoolSize;
        ActiveAudioSourceCount = activeAudioSourceCount;
        PathPoolOwnerCount = pathPoolOwnerCount;
        PathPoolLength = pathPoolLength;
        PathPoolCapacity = pathPoolCapacity;
        MissileTrailCreatedCount = missileTrailCreatedCount;
        MissileTrailActiveCount = missileTrailActiveCount;
        ImpactVfxCreatedCount = impactVfxCreatedCount;
        ImpactVfxActiveCount = impactVfxActiveCount;
        TotalAllocatedMemoryBytes = totalAllocatedMemoryBytes;
        TotalReservedMemoryBytes = totalReservedMemoryBytes;
        MonoUsedMemoryBytes = monoUsedMemoryBytes;
        MonoHeapMemoryBytes = monoHeapMemoryBytes;
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
    public int AudioRuntimeViewCount { get; }
    public int AudioPoolSize { get; }
    public int ActiveAudioSourceCount { get; }
    public int PathPoolOwnerCount { get; }
    public int PathPoolLength { get; }
    public int PathPoolCapacity { get; }
    public int MissileTrailCreatedCount { get; }
    public int MissileTrailActiveCount { get; }
    public int ImpactVfxCreatedCount { get; }
    public int ImpactVfxActiveCount { get; }
    public long TotalAllocatedMemoryBytes { get; }
    public long TotalReservedMemoryBytes { get; }
    public long MonoUsedMemoryBytes { get; }
    public long MonoHeapMemoryBytes { get; }

    public string ToCompactString()
    {
        return $"cycle={Cycle} phase={Phase} world={WorldSequence} scenes={LoadedSceneCount} " +
               $"roots={SceneRootCount} entities={TotalEntityCount} systems={ManagedSystemCount} " +
               $"shellFlow={ShellFlowSystemHandle} actions={ActionRequestSystemHandle} " +
               $"lifecycle={LifecycleRootCount} map={OperationMapRootCount} menu={MenuViewCount} " +
               $"match={MatchViewCount} hud={MatchHudCount} listeners={EnabledAudioListenerCount} " +
               $"missileVfx={MissileTrailRootCount} audioViews={AudioRuntimeViewCount} " +
               $"audioPool={AudioPoolSize} audioActive={ActiveAudioSourceCount} pathPool={PathPoolLength}/{PathPoolCapacity} " +
               $"trails={MissileTrailActiveCount}/{MissileTrailCreatedCount} impacts={ImpactVfxActiveCount}/{ImpactVfxCreatedCount} " +
               $"allocated={TotalAllocatedMemoryBytes} " +
               $"reserved={TotalReservedMemoryBytes} monoUsed={MonoUsedMemoryBytes} monoHeap={MonoHeapMemoryBytes}";
    }
}

internal sealed class ArchitectureMenuMatchLifecycleSnapshotCollector : IDisposable
{
    private const string MissileTrailRootName = "MissileTrailVfxView";

    private readonly World world;
    private readonly UIShellContentView shellContent;
    private readonly EntityQuery lifecycleRootQuery;
    private readonly EntityQuery operationMapRootQuery;
    private readonly EntityQuery pathPoolQuery;
    private readonly List<GameObject> sceneRoots = new(64);
    private readonly List<MenuBootstrapView> menuViews = new(2);
    private readonly List<MatchSceneView> matchViews = new(2);
    private readonly List<MatchHudFooterContentView> matchHudViews = new(2);
    private readonly List<AudioListener> audioListeners = new(2);
    private readonly List<AudioPlaybackPresentationRuntimeView> audioRuntimeViews = new(2);
    private readonly List<MissileTrailVfxView> missileTrailViews = new(2);
    private readonly List<UnitAttackImpactVfxView> impactVfxViews = new(2);
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
        pathPoolQuery = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PathPoolComponent>());
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
        int audioRuntimeViewCount = 0;
        int audioPoolSize = 0;
        int activeAudioSourceCount = 0;
        int missileTrailCreatedCount = 0;
        int missileTrailActiveCount = 0;
        int impactVfxCreatedCount = 0;
        int impactVfxActiveCount = 0;

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

                audioRuntimeViews.Clear();
                root.GetComponentsInChildren(true, audioRuntimeViews);
                audioRuntimeViewCount += audioRuntimeViews.Count;
                for (int audioIndex = 0; audioIndex < audioRuntimeViews.Count; audioIndex++)
                {
                    AudioPlaybackPresentationRuntimeView audioRuntime = audioRuntimeViews[audioIndex];
                    if (audioRuntime == null)
                        continue;

                    audioPoolSize += audioRuntime.PoolSize;
                    activeAudioSourceCount += audioRuntime.ActiveSourceCount;
                }

                CountPoolChildren(root, missileTrailViews, ref missileTrailCreatedCount, ref missileTrailActiveCount);
                CountPoolChildren(root, impactVfxViews, ref impactVfxCreatedCount, ref impactVfxActiveCount);
            }
        }

        matchHudCount = CountComponents(shellContent.gameObject, matchHudViews);
        int pathPoolOwnerCount = pathPoolQuery.CalculateEntityCount();
        int pathPoolLength = 0;
        int pathPoolCapacity = 0;
        if (pathPoolOwnerCount == 1)
        {
            PathPoolComponent pathPool = pathPoolQuery.GetSingleton<PathPoolComponent>();
            if (pathPool.Cells.IsCreated)
            {
                pathPoolLength = pathPool.Cells.Length;
                pathPoolCapacity = pathPool.Cells.Capacity;
            }
        }

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
            missileTrailRootCount,
            audioRuntimeViewCount,
            audioPoolSize,
            activeAudioSourceCount,
            pathPoolOwnerCount,
            pathPoolLength,
            pathPoolCapacity,
            missileTrailCreatedCount,
            missileTrailActiveCount,
            impactVfxCreatedCount,
            impactVfxActiveCount,
            Profiler.GetTotalAllocatedMemoryLong(),
            Profiler.GetTotalReservedMemoryLong(),
            Profiler.GetMonoUsedSizeLong(),
            Profiler.GetMonoHeapSizeLong());
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        lifecycleRootQuery.Dispose();
        operationMapRootQuery.Dispose();
        pathPoolQuery.Dispose();
    }

    private static int CountComponents<T>(GameObject root, List<T> buffer) where T : Component
    {
        buffer.Clear();
        root.GetComponentsInChildren(true, buffer);
        return buffer.Count;
    }

    private static void CountPoolChildren<T>(
        GameObject root,
        List<T> buffer,
        ref int createdCount,
        ref int activeCount) where T : Component
    {
        buffer.Clear();
        root.GetComponentsInChildren(true, buffer);
        for (int viewIndex = 0; viewIndex < buffer.Count; viewIndex++)
        {
            Transform poolRoot = buffer[viewIndex].transform;
            createdCount += poolRoot.childCount;
            for (int childIndex = 0; childIndex < poolRoot.childCount; childIndex++)
            {
                if (poolRoot.GetChild(childIndex).gameObject.activeSelf)
                    activeCount++;
            }
        }
    }
}
