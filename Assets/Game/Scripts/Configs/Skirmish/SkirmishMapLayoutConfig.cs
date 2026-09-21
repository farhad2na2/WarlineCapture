using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct SkirmishLayoutAnchorConfig
    {
        public string AnchorId;
        public string RoleId;
        public float NormalizedU;
        public float NormalizedV;
    }

    [Serializable]
    public struct SkirmishLayoutRouteConfig
    {
        public string RouteId;
        public string[] WaypointAnchorIds;
    }

    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Map Layout")]
    public sealed class SkirmishMapLayoutConfig : ScriptableObject
    {
        [SerializeField] private string layoutId = "layout.skirmish.db.ba";
        [SerializeField] private string operationMapId = "opmap.skirmish.desert_base_01";
        [SerializeField] private int contentVersion = 1;
        [SerializeField] private string contentHash = "planned.db.ba.v1";
        [SerializeField] private SkirmishLayoutAnchorConfig[] anchors = Array.Empty<SkirmishLayoutAnchorConfig>();
        [SerializeField] private SkirmishLayoutRouteConfig[] routes = Array.Empty<SkirmishLayoutRouteConfig>();

        public string LayoutId => layoutId;
        public string OperationMapId => operationMapId;
        public int ContentVersion => contentVersion;
        public string ContentHash => contentHash;
        public SkirmishLayoutAnchorConfig[] Anchors => anchors;
        public SkirmishLayoutRouteConfig[] Routes => routes;

        public void ConfigureDesertBaseAssault()
        {
            layoutId = "layout.skirmish.db.ba";
            operationMapId = "opmap.skirmish.desert_base_01";
            contentVersion = 1;
            contentHash = "planned.db.ba.v1";
            anchors = new[]
            {
                Anchor("anchor.skirmish.db.base_player", "base.player", 0.12f, 0.50f),
                Anchor("anchor.skirmish.db.base_enemy", "base.enemy", 0.88f, 0.50f),
                Anchor("anchor.skirmish.db.staging_player", "staging.player", 0.18f, 0.50f),
                Anchor("anchor.skirmish.db.staging_enemy", "staging.enemy", 0.82f, 0.50f),
                Anchor("anchor.skirmish.db.economy_player", "economy.player", 0.10f, 0.24f),
                Anchor("anchor.skirmish.db.economy_enemy", "economy.enemy", 0.90f, 0.76f),
                Anchor("anchor.skirmish.db.service_player", "service.player", 0.12f, 0.68f),
                Anchor("anchor.skirmish.db.service_enemy", "service.enemy", 0.88f, 0.32f)
            };
            routes = new[]
            {
                new SkirmishLayoutRouteConfig
                {
                    RouteId = "route.skirmish.db.main",
                    WaypointAnchorIds = new[] { "anchor.skirmish.db.staging_player", "anchor.skirmish.db.staging_enemy" }
                },
                new SkirmishLayoutRouteConfig
                {
                    RouteId = "route.skirmish.db.flank_a",
                    WaypointAnchorIds = new[] { "anchor.skirmish.db.staging_player", "anchor.skirmish.db.economy_enemy" }
                },
                new SkirmishLayoutRouteConfig
                {
                    RouteId = "route.skirmish.db.flank_b",
                    WaypointAnchorIds = new[] { "anchor.skirmish.db.staging_player", "anchor.skirmish.db.service_enemy" }
                }
            };
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

        private static SkirmishLayoutAnchorConfig Anchor(string id, string role, float u, float v) =>
            new SkirmishLayoutAnchorConfig { AnchorId = id, RoleId = role, NormalizedU = u, NormalizedV = v };
    }
}
