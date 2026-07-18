using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Contracts
{
    public enum BuildingUiCommandFailure
    {
        None = 0,
        NotEnoughMoney = 1,
        MissingProducerBuilding = 2,
        InvalidSelection = 3,
        ProductionQueueFull = 4,
        GlobalProductionQueueFull = 5,
        InsufficientCredits = 6,
        InsufficientMaterials = 7,
        InsufficientCreditsAndMaterials = 8
    }

    public readonly struct BuildingPendingProductionUiEntry
    {
        public readonly int BuildingId;
        public readonly int PendingProductionIndex;
        public readonly GameObject Prefab;
        public readonly float RemainingSeconds;
        public readonly float DurationSeconds;
        public readonly float Progress01;
        public readonly float StartedAt;
        public readonly float ReadyAt;
        public readonly string ProducerDisplayName;

        public BuildingPendingProductionUiEntry(
            int buildingId,
            GameObject prefab,
            float remainingSeconds,
            float durationSeconds,
            float progress01,
            float startedAt,
            float readyAt,
            string producerDisplayName = "")
            : this(buildingId, -1, prefab, remainingSeconds, durationSeconds, progress01, startedAt, readyAt, producerDisplayName)
        {
        }

        public BuildingPendingProductionUiEntry(
            int buildingId,
            int pendingProductionIndex,
            GameObject prefab,
            float remainingSeconds,
            float durationSeconds,
            float progress01,
            float startedAt,
            float readyAt,
            string producerDisplayName)
        {
            BuildingId = buildingId;
            PendingProductionIndex = pendingProductionIndex;
            Prefab = prefab;
            RemainingSeconds = remainingSeconds;
            DurationSeconds = durationSeconds;
            Progress01 = progress01;
            StartedAt = startedAt;
            ReadyAt = readyAt;
            ProducerDisplayName = producerDisplayName ?? string.Empty;
        }
    }

    public interface IBuildingUiCommand
    {
        int CurrentDollars { get; }
        bool HasPendingBuildingPlacement { get; }
        bool CanConfirmBuildingPlacement { get; }
        string PlacementStatusText { get; }
        int ActivePlacementCost { get; }
        float ActivePlacementDurationSeconds { get; }
        int MaxQueuedUnitProductions { get; }

        BuildingUiCommandFailure GetCampRequestFailure(GameObject prefab, int materialsCost, out string requiredBuildingDisplayName);
        BuildingUiCommandFailure TryRequestCampItem(GameObject prefab, int materialsCost, out string requiredBuildingDisplayName, bool focusProducerOnSuccess);
        bool CancelProduction(int buildingId, int pendingProductionIndex);
        bool ConfirmBuildingPlacement();
        void CancelBuildingPlacement();
        bool RotateBuildingPlacement();
    }

    public interface IBuildingUiQuery
    {
        void GetFriendlyPendingProductionUiEntries(List<BuildingPendingProductionUiEntry> entries);
    }

    public interface IMatchRuntimeState
    {
        bool PlayRequested { get; set; }
        bool SimulationActive { get; set; }
        bool SelectionModeActive { get; set; }
        bool BuildModeActive { get; set; }
        bool ZoomInHeld { get; set; }
        bool ZoomOutHeld { get; set; }
        bool SuppressNextWorldClick { get; set; }
    }

    public readonly struct SelectionRectangleStateModel
    {
        public readonly bool CanDraw;
        public readonly Rect ScreenRect;

        public SelectionRectangleStateModel(bool canDraw, Rect screenRect)
        {
            CanDraw = canDraw;
            ScreenRect = screenRect;
        }
    }

    public interface ISelectionRectangleState
    {
        bool TryRead(out SelectionRectangleStateModel state);
    }

    public interface IMatchHudCameraControl
    {
        Camera WorldCamera { get; }
        bool IsCameraDragging { get; }
        void MoveCameraGroundCenterTo(Vector3 worldPosition);
        void UpdateZoomTransition();
        MatchHudZoomControlState ReadZoomControlState();
        bool RequestZoomInLevel();
        bool RequestZoomOutLevel();
    }

    public readonly struct MatchHudZoomControlState
    {
        public readonly bool ZoomInEnabled;
        public readonly bool ZoomOutEnabled;

        public MatchHudZoomControlState(bool zoomInEnabled, bool zoomOutEnabled)
        {
            ZoomInEnabled = zoomInEnabled;
            ZoomOutEnabled = zoomOutEnabled;
        }

        public static MatchHudZoomControlState Disabled => new(false, false);
        public static MatchHudZoomControlState Default => new(true, true);
    }

    public readonly struct MatchHudMinimapGridModel
    {
        public readonly Vector3 Origin;
        public readonly int Width;
        public readonly int Height;
        public readonly float CellSize;
        private readonly Vector2 projectionSize;

        public MatchHudMinimapGridModel(Vector3 origin, int width, int height, float cellSize)
            : this(origin, width, height, cellSize, default)
        {
        }

        public MatchHudMinimapGridModel(
            Vector3 origin,
            int width,
            int height,
            float cellSize,
            Vector2 projectionSize)
        {
            Origin = origin;
            Width = width;
            Height = height;
            CellSize = cellSize;
            this.projectionSize = projectionSize;
        }

        public float WorldWidth => Mathf.Max(0.001f, projectionSize.x > 0f ? projectionSize.x : Width * CellSize);
        public float WorldHeight => Mathf.Max(0.001f, projectionSize.y > 0f ? projectionSize.y : Height * CellSize);
        public bool IsValid => Width > 0 && Height > 0 && CellSize > 0f;
    }

    public readonly struct MatchHudMinimapAreaModel
    {
        public readonly Vector3 Origin;
        public readonly float Width;
        public readonly float Height;

        public MatchHudMinimapAreaModel(Vector3 origin, float width, float height)
        {
            Origin = origin;
            Width = Mathf.Max(0.001f, width);
            Height = Mathf.Max(0.001f, height);
        }

        public bool ContainsXZ(Vector3 position, float padding = 0f)
        {
            return position.x >= Origin.x - padding &&
                   position.x <= Origin.x + Width + padding &&
                   position.z >= Origin.z - padding &&
                   position.z <= Origin.z + Height + padding;
        }
    }

    public enum MatchHudMinimapMarkerAllegiance : byte
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2
    }

    public readonly struct MatchHudMinimapMarkerModel
    {
        public readonly Vector3 Position;
        public readonly MatchHudMinimapMarkerAllegiance Allegiance;

        public MatchHudMinimapMarkerModel(Vector3 position, MatchHudMinimapMarkerAllegiance allegiance)
        {
            Position = position;
            Allegiance = allegiance;
        }
    }

    public enum MatchHudMinimapRoadKind : byte
    {
        Road = 0,
        Sidewalk = 1,
        DirtRoad = 2
    }

    public readonly struct MatchHudMinimapRoadCellModel
    {
        public readonly Vector3 WorldPosition;
        public readonly float CellSize;
        public readonly MatchHudMinimapRoadKind Kind;

        public MatchHudMinimapRoadCellModel(Vector3 worldPosition, float cellSize, MatchHudMinimapRoadKind kind)
        {
            WorldPosition = worldPosition;
            CellSize = cellSize;
            Kind = kind;
        }
    }

    public enum MatchHudMinimapSurfaceFeatureKind : byte
    {
        Road = 0,
        DirtRoad = 1,
        Highway = 2,
        Bridge = 3,
        Ramp = 4,
        Plaza = 5,
        Blocked = 6
    }

    public readonly struct MatchHudMinimapSurfaceFeatureModel
    {
        public readonly Vector3 Center;
        public readonly Vector2 HalfExtents;
        public readonly float CellSize;
        public readonly MatchHudMinimapSurfaceFeatureKind Kind;
        public readonly bool FillArea;

        public MatchHudMinimapSurfaceFeatureModel(
            Vector3 center,
            Vector2 halfExtents,
            float cellSize,
            MatchHudMinimapSurfaceFeatureKind kind,
            bool fillArea)
        {
            Center = center;
            HalfExtents = halfExtents;
            CellSize = cellSize;
            Kind = kind;
            FillArea = fillArea;
        }
    }

    public interface IMatchHudMinimapDataSource
    {
        bool TryGetGrid(out MatchHudMinimapGridModel grid);
        void GetMarkers(MatchHudMinimapAreaModel area, List<MatchHudMinimapMarkerModel> markers);
        void GetRoadCells(MatchHudMinimapAreaModel area, List<MatchHudMinimapRoadCellModel> roadCells);
        void GetSurfaceFeatures(MatchHudMinimapAreaModel area, List<MatchHudMinimapSurfaceFeatureModel> features);
    }

    public enum UiBoardCommandModeDirection : byte
    {
        None = 0,
        PassengerToTransport = 1,
        TransportToPassenger = 2
    }

    public enum UiQuickGameEnemyType : byte
    {
        Balanced = 0,
        Military = 1,
        Defensive = 2,
        Air = 3,
        Swarm = 4,
        Random = 5
    }

    public enum UiQuickGameWinCondition : byte
    {
        DestroyAllEnemies = 0,
        SurviveDuration = 1,
        Sandbox = 2
    }

    public enum UiQuickGameStartingResources : byte
    {
        Standard = 0,
        Low = 1,
        High = 2
    }

    public enum UiAiDifficultySetting : byte
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Brutal = 3
    }

    public enum UiAiStartingMoneySetting : byte
    {
        Low = 0,
        Normal = 1,
        High = 2
    }

    public enum UiAiSpeedSetting : byte
    {
        Slow = 0,
        Normal = 1,
        Fast = 2
    }

    public enum UiAiAttackGroupSizeSetting : byte
    {
        Small = 0,
        Normal = 1,
        Large = 2
    }

    public enum UiAiAttackFrequencySetting : byte
    {
        Rare = 0,
        Normal = 1,
        Frequent = 2
    }

    public enum UiAiAggressionSetting : byte
    {
        Defensive = 0,
        Balanced = 1,
        Aggressive = 2
    }

    public enum UiAiExpansionSetting : byte
    {
        Off = 0,
        Slow = 1,
        Normal = 2,
        Fast = 3
    }

    public enum UiAiTargetPriority : byte
    {
        Balanced = 0,
        Units = 1,
        Economy = 2,
        Production = 3
    }

    public struct UiQuickCustomGameConfig
    {
        public UiQuickGameEnemyType EnemyType;
        public int EnemyCount;
        public UiAiDifficultySetting Difficulty;
        public UiAiStartingMoneySetting StartingMoney;
        public float IncomeMultiplier;
        public UiAiSpeedSetting BuildSpeed;
        public UiAiSpeedSetting UnitProductionSpeed;
        public UiAiAttackGroupSizeSetting AttackGroupSize;
        public UiAiAttackFrequencySetting AttackFrequency;
        public UiAiAggressionSetting Aggression;
        public UiAiExpansionSetting Expansion;
        public UiAiTargetPriority TargetPriority;
        public bool PlayerAutoAIEnabled;
        public UiQuickGameWinCondition WinCondition;
        public bool FogOfWar;
        public bool IntelReveal;
        public UiQuickGameStartingResources StartingResources;
        public int MapSeed;

        public static UiQuickCustomGameConfig Defaults => new()
        {
            EnemyType = UiQuickGameEnemyType.Balanced,
            EnemyCount = 1,
            Difficulty = UiAiDifficultySetting.Normal,
            StartingMoney = UiAiStartingMoneySetting.Normal,
            IncomeMultiplier = 1f,
            BuildSpeed = UiAiSpeedSetting.Normal,
            UnitProductionSpeed = UiAiSpeedSetting.Normal,
            AttackGroupSize = UiAiAttackGroupSizeSetting.Normal,
            AttackFrequency = UiAiAttackFrequencySetting.Normal,
            Aggression = UiAiAggressionSetting.Balanced,
            Expansion = UiAiExpansionSetting.Normal,
            TargetPriority = UiAiTargetPriority.Balanced,
            PlayerAutoAIEnabled = false,
            WinCondition = UiQuickGameWinCondition.DestroyAllEnemies,
            FogOfWar = false,
            IntelReveal = true,
            StartingResources = UiQuickGameStartingResources.Standard,
            MapSeed = 104729
        };
    }

    public interface IQuickCustomGameConfigStore
    {
        UiQuickCustomGameConfig Current { get; }
        UiQuickCustomGameConfig Defaults { get; }
        void Apply(UiQuickCustomGameConfig config);
    }

    public interface IMatchLaunchCommand
    {
        void LaunchMatch(Component source);
    }

    public interface ISelectionDiagnosticsSink
    {
        void LogMoveCommandTrace(string message);
    }
}
