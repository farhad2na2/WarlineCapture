using Unity.Entities;
using UnityEngine;
using Game.Configs;

namespace Game.Runtime
{
    internal sealed partial class RoadBuildStartupSystem : SystemBase
    {
        protected override void OnCreate()
        {
            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }

        internal sealed class State
        {
            public RoadBuildSystemConfig Config;
            public Camera WorldCamera;
            public Transform RuntimeRoot;
            public RoadRuntimeRootSceneSystemHelper.Roots RuntimeRoots;
            public GameObject StraightPrefab;
            public GameObject TIntersectionPrefab;
            public GameObject IntersectionPrefab;
            public GameObject EndPrefab;
            public GameObject CornerPrefab;
            public GameObject AutobahnPrefab;
            public GameObject AutobahnConnectPrefab;
            public Vector3 GridOrigin = Vector3.zero;
            public float BuildPlaneY = 0f;
            public float RoadGridSize = 20f;
            public int ChunkSizeInCells = 8;
            public float PreviewAlpha = 0.65f;
            public GameObject SoldierBasePrefab;
            public Vector2Int SoldierBaseFootprintCells = new(20, 20);
            public float PlacementOutlineHeight = 0.15f;
            public float PlacementOutlineWidth = 0.35f;
            public Color PlacementValidColor = new(0.15f, 0.85f, 0.2f, 1f);
            public Color PlacementInvalidColor = new(0.9f, 0.2f, 0.2f, 1f);
        }

        public State Initialize(
            RoadBuildSystemConfig configAsset,
            Camera sceneWorldCamera,
            Transform runtimeRoot,
            RoadBuildConfigSystem configSystem,
            RoadRuntimeRootSceneSystemHelper runtimeRootSystem,
            RoadVisualVariantSystem visualVariantSystem)
        {
            var state = new State
            {
                Config = configAsset,
                WorldCamera = sceneWorldCamera,
                RuntimeRoot = runtimeRoot
            };

            ApplyConfigIfAvailable(state, configSystem);
            state.RuntimeRoots = runtimeRootSystem != null
                ? runtimeRootSystem.CreateRoots(runtimeRoot)
                : default;
            visualVariantSystem?.CacheVariants(CreateRoadPrefabSet(state));
            return state;
        }

        public void DisposeRuntimeRoots(State state, RoadRuntimeRootSceneSystemHelper runtimeRootSystem)
        {
            if (state == null)
                return;

            runtimeRootSystem?.DisposeRoots(state.RuntimeRoots);
            state.RuntimeRoots = default;
        }

        public RoadVisualVariantSystem.Prefabs CreateRoadPrefabSet(State state)
        {
            return new RoadVisualVariantSystem.Prefabs(
                state.EndPrefab,
                state.StraightPrefab,
                state.CornerPrefab,
                state.TIntersectionPrefab,
                state.IntersectionPrefab,
                state.AutobahnPrefab,
                state.AutobahnConnectPrefab);
        }

        private static void ApplyConfigIfAvailable(State state, RoadBuildConfigSystem configSystem)
        {
            if (!configSystem.TryCreateSnapshot(state.Config, out RoadBuildConfigSystem.Snapshot snapshot))
                return;

            ApplyConfigSnapshot(state, snapshot);
        }

        private static void ApplyConfigSnapshot(State state, RoadBuildConfigSystem.Snapshot snapshot)
        {
            if (snapshot.WorldCamera != null)
                state.WorldCamera = snapshot.WorldCamera;
            state.StraightPrefab = snapshot.StraightPrefab;
            state.TIntersectionPrefab = snapshot.TIntersectionPrefab;
            state.IntersectionPrefab = snapshot.IntersectionPrefab;
            state.EndPrefab = snapshot.EndPrefab;
            state.CornerPrefab = snapshot.CornerPrefab;
            state.AutobahnPrefab = snapshot.AutobahnPrefab;
            state.AutobahnConnectPrefab = snapshot.AutobahnConnectPrefab;
            state.GridOrigin = snapshot.GridOrigin;
            state.BuildPlaneY = snapshot.BuildPlaneY;
            state.RoadGridSize = snapshot.RoadGridSize;
            state.ChunkSizeInCells = snapshot.ChunkSizeInCells;
            state.PreviewAlpha = snapshot.PreviewAlpha;
            state.SoldierBasePrefab = snapshot.SoldierBasePrefab;
            state.SoldierBaseFootprintCells = snapshot.SoldierBaseFootprintCells;
            state.PlacementOutlineHeight = snapshot.PlacementOutlineHeight;
            state.PlacementOutlineWidth = snapshot.PlacementOutlineWidth;
            state.PlacementValidColor = snapshot.PlacementValidColor;
            state.PlacementInvalidColor = snapshot.PlacementInvalidColor;
        }
    }
}
