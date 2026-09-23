using System;
using UnityEngine;

namespace Game.Configs
{
    public enum SkirmishLegalPadKind : byte
    {
        None = 0,
        BaseBarracks = 1,
        GroundStaging = 2,
        InfantrySpawn = 3,
        VehicleSpawn = 4,
        Rally = 5,
        AirReturn = 6,
        Supply = 7,
        Service = 8,
        SupplyExpansion = 9
    }

    public enum SkirmishMeasuredRouteKind : byte
    {
        None = 0,
        MainHighway = 1,
        FlankNorthRuins = 2,
        FlankSouthSweep = 3
    }

    [Serializable]
    public struct SkirmishLayoutAnchorConfig
    {
        public string AnchorId;
        public string RoleId;
        public float NormalizedU;
        public float NormalizedV;
        public float WorldX;
        public float WorldZ;
    }

    [Serializable]
    public struct SkirmishLayoutRouteConfig
    {
        public string RouteId;
        public string[] WaypointAnchorIds;
        public SkirmishMeasuredRouteKind Kind;
        public float LengthMetres;
        public float InfantryTransitSeconds;
        public float GroundTransitSeconds;
        public float AirTransitSeconds;
        public float InfantryFirstContactSeconds;
        public float GroundFirstContactSeconds;
        public bool ProvisionalTimes;
        public float WidthMetres;
        public bool AllowsVehicles;
        public bool AllowsAircraft;
    }

    [Serializable]
    public struct SkirmishLegalPadConfig
    {
        public string PadId;
        public string AnchorId;
        public string RoleId;
        public SkirmishLegalPadKind Kind;
        public float CenterX;
        public float CenterZ;
        public float WidthMetres;
        public float DepthMetres;
        public byte FactionId;
    }

    /// <summary>
    /// Pins the layout's normalized authoring frame onto the loaded operation map.
    /// The map owner derives Origin/Forward/Across and usable metres from the map
    /// definition's authored deployment anchors and saves them here; runtime never
    /// reapplies the raw envelope coordinates to the world.
    /// </summary>
    [Serializable]
    public struct SkirmishLayoutWorldBindingConfig
    {
        public float OriginX;
        public float OriginZ;
        public float ForwardX;
        public float ForwardZ;
        public float AcrossX;
        public float AcrossZ;
        public float ForwardMetres;
        public float AcrossMetres;
        public string PlayerAnchorId;
        public string EnemyAnchorId;
        public string SourceHash;

        public bool IsValid =>
            ForwardMetres > 0f && AcrossMetres > 0f &&
            (ForwardX != 0f || ForwardZ != 0f) &&
            (AcrossX != 0f || AcrossZ != 0f) &&
            !string.IsNullOrEmpty(PlayerAnchorId) &&
            !string.IsNullOrEmpty(EnemyAnchorId);
    }

    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Map Layout")]
    public sealed class SkirmishMapLayoutConfig : ScriptableObject
    {
        public const float DesertBaseWidthMetres = 600f;
        public const float DesertBaseDepthMetres = 420f;
        public const float DesertBaseOriginX = -300f;
        public const float DesertBaseOriginZ = -210f;
        public const float DesertBaseCellSize = 2f;

        [SerializeField] private string layoutId = "layout.skirmish.db.ba";
        [SerializeField] private string operationMapId = "opmap.skirmish.desert_base_01";
        [SerializeField] private int contentVersion = 1;
        [SerializeField] private string contentHash = "measured.db.ba.v1";
        [SerializeField] private float worldWidthMetres = DesertBaseWidthMetres;
        [SerializeField] private float worldDepthMetres = DesertBaseDepthMetres;
        [SerializeField] private float originX = DesertBaseOriginX;
        [SerializeField] private float originZ = DesertBaseOriginZ;
        [SerializeField] private float cellSize = DesertBaseCellSize;
        [SerializeField] private SkirmishLayoutAnchorConfig[] anchors = Array.Empty<SkirmishLayoutAnchorConfig>();
        [SerializeField] private SkirmishLayoutRouteConfig[] routes = Array.Empty<SkirmishLayoutRouteConfig>();
        [SerializeField] private SkirmishLegalPadConfig[] pads = Array.Empty<SkirmishLegalPadConfig>();
        [SerializeField] private SkirmishLayoutWorldBindingConfig worldBinding;

        public string LayoutId => layoutId;
        public string OperationMapId => operationMapId;
        public int ContentVersion => contentVersion;
        public string ContentHash => contentHash;
        public float WorldWidthMetres => worldWidthMetres;
        public float WorldDepthMetres => worldDepthMetres;
        public float OriginX => originX;
        public float OriginZ => originZ;
        public float CellSize => cellSize;
        public SkirmishLayoutAnchorConfig[] Anchors => anchors;
        public SkirmishLayoutRouteConfig[] Routes => routes;
        public SkirmishLegalPadConfig[] Pads => pads;
        public SkirmishLayoutWorldBindingConfig WorldBinding => worldBinding;
        public bool HasWorldBinding => worldBinding.IsValid;

        internal void ApplyWorldBinding(in SkirmishLayoutWorldBindingConfig binding)
        {
            worldBinding = binding;
        }

        /// <summary>
        /// Projects a normalized authoring position into the loaded map's world frame.
        /// u runs player rear to enemy rear along Forward; v runs across, with v=0.5 on
        /// the base-to-base axis. Returns false when no map binding is pinned.
        /// </summary>
        public bool TryProjectToMap(float u, float v, out float worldX, out float worldZ)
        {
            worldX = 0f;
            worldZ = 0f;
            if (!HasWorldBinding)
                return false;

            worldX = worldBinding.OriginX +
                     u * worldBinding.ForwardMetres * worldBinding.ForwardX +
                     (v - 0.5f) * worldBinding.AcrossMetres * worldBinding.AcrossX;
            worldZ = worldBinding.OriginZ +
                     u * worldBinding.ForwardMetres * worldBinding.ForwardZ +
                     (v - 0.5f) * worldBinding.AcrossMetres * worldBinding.AcrossZ;
            return true;
        }

        /// <summary>
        /// Converts a stored envelope-frame position (anchors/pads authored against
        /// Origin/WorldWidth/WorldDepth) into the loaded map's world frame.
        /// </summary>
        public bool TryProjectLocalToMap(float localX, float localZ, out float mapX, out float mapZ)
        {
            mapX = 0f;
            mapZ = 0f;
            if (!HasWorldBinding || worldWidthMetres <= 0f || worldDepthMetres <= 0f)
                return false;

            float u = (localX - originX) / worldWidthMetres;
            float v = (localZ - originZ) / worldDepthMetres;
            return TryProjectToMap(u, v, out mapX, out mapZ);
        }

        public void ConfigureDesertBaseAssault()
        {
            SkirmishMapLayoutBuilder.ApplyDesertBaseAssault(this);
        }

        internal void ApplyMeasured(
            string configuredLayoutId,
            string configuredMapId,
            int version,
            string hash,
            float width,
            float depth,
            float worldOriginX,
            float worldOriginZ,
            float configuredCellSize,
            SkirmishLayoutAnchorConfig[] configuredAnchors,
            SkirmishLayoutRouteConfig[] configuredRoutes,
            SkirmishLegalPadConfig[] configuredPads)
        {
            layoutId = configuredLayoutId;
            operationMapId = configuredMapId;
            contentVersion = version;
            contentHash = hash;
            worldWidthMetres = width;
            worldDepthMetres = depth;
            originX = worldOriginX;
            originZ = worldOriginZ;
            cellSize = configuredCellSize;
            anchors = configuredAnchors ?? Array.Empty<SkirmishLayoutAnchorConfig>();
            routes = configuredRoutes ?? Array.Empty<SkirmishLayoutRouteConfig>();
            pads = configuredPads ?? Array.Empty<SkirmishLegalPadConfig>();
        }

        public bool TryGetAnchor(string roleId, out SkirmishLayoutAnchorConfig anchor)
        {
            for (int i = 0; i < anchors.Length; i++)
            {
                if (anchors[i].RoleId == roleId)
                {
                    anchor = anchors[i];
                    return true;
                }
            }

            anchor = default;
            return false;
        }

        public bool TryGetAnchorById(string anchorId, out SkirmishLayoutAnchorConfig anchor)
        {
            for (int i = 0; i < anchors.Length; i++)
            {
                if (anchors[i].AnchorId == anchorId)
                {
                    anchor = anchors[i];
                    return true;
                }
            }

            anchor = default;
            return false;
        }

        public bool TryGetPad(string roleId, out SkirmishLegalPadConfig pad)
        {
            for (int i = 0; i < pads.Length; i++)
            {
                if (pads[i].RoleId == roleId)
                {
                    pad = pads[i];
                    return true;
                }
            }

            pad = default;
            return false;
        }

        public bool TryGetPad(byte factionId, SkirmishLegalPadKind kind, out SkirmishLegalPadConfig pad)
        {
            for (int i = 0; i < pads.Length; i++)
            {
                if (pads[i].FactionId == factionId && pads[i].Kind == kind)
                {
                    pad = pads[i];
                    return true;
                }
            }

            pad = default;
            return false;
        }

        public bool TryGetRoute(string routeId, out SkirmishLayoutRouteConfig route)
        {
            for (int i = 0; i < routes.Length; i++)
            {
                if (routes[i].RouteId == routeId)
                {
                    route = routes[i];
                    return true;
                }
            }

            route = default;
            return false;
        }

        public bool TryGetRoute(SkirmishMeasuredRouteKind kind, out SkirmishLayoutRouteConfig route)
        {
            for (int i = 0; i < routes.Length; i++)
            {
                if (routes[i].Kind == kind)
                {
                    route = routes[i];
                    return true;
                }
            }

            route = default;
            return false;
        }

        public static void ProjectNormalized(
            float u,
            float v,
            float worldOriginX,
            float worldOriginZ,
            float width,
            float depth,
            out float worldX,
            out float worldZ)
        {
            worldX = worldOriginX + u * width;
            worldZ = worldOriginZ + v * depth;
        }
    }
}
