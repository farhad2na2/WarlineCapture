using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Role Overlay Catalog")]
    public sealed class SkirmishRoleOverlayCatalog : ScriptableObject
    {
        [SerializeField] private SkirmishRoleOverlay[] overlays = System.Array.Empty<SkirmishRoleOverlay>();
        [SerializeField] private int contentVersion = 1;

        public SkirmishRoleOverlay[] Overlays => overlays;
        public int ContentVersion => contentVersion;

        public void ConfigureS002GroundSlice()
        {
            contentVersion = 1;
            overlays = CreateS002GroundSlice();
        }

        public static SkirmishRoleOverlay[] CreateAirMobileSlice()
        {
            SkirmishRoleOverlay[] ground = CreateS002GroundSlice();
            var kept = new System.Collections.Generic.List<SkirmishRoleOverlay>(ground.Length + 8);
            for (int i = 0; i < ground.Length; i++)
            {
                if (ground[i].RoleKind == SkirmishRoleKind.Tank)
                    continue;
                kept.Add(ground[i]);
            }

            kept.Add(Overlay(SkirmishRoleIds.AntiAir, SkirmishRoleKind.AntiAir, 220, 20, 28f, 220,
                SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.Air, 1));
            kept.Add(Overlay(SkirmishRoleIds.TransportHeli, SkirmishRoleKind.TransportHeli, 260, 0, 0f, 240,
                SkirmishProducerKind.Helipad, SkirmishTargetDomain.None, 1));
            kept.Add(Overlay(SkirmishRoleIds.AttackHeliLight, SkirmishRoleKind.AttackHeliLight, 240, 18, 22f, 300,
                SkirmishProducerKind.Helipad, SkirmishTargetDomain.Ground | SkirmishTargetDomain.Structure, 1));
            kept.Add(Overlay(SkirmishRoleIds.AttackHeli, SkirmishRoleKind.AttackHeli, 320, 24, 26f, 420,
                SkirmishProducerKind.Helipad, SkirmishTargetDomain.Ground | SkirmishTargetDomain.Structure, 1));
            kept.Add(Overlay(SkirmishRoleIds.Drone, SkirmishRoleKind.Drone, 80, 0, 0f, 140,
                SkirmishProducerKind.IntelStation, SkirmishTargetDomain.None, 1));
            kept.Add(Overlay(SkirmishRoleIds.Fighter, SkirmishRoleKind.Fighter, 280, 22, 32f, 480,
                SkirmishProducerKind.Airport, SkirmishTargetDomain.Air, 1));
            kept.Add(Overlay(SkirmishRoleIds.Strike, SkirmishRoleKind.Strike, 300, 26, 28f, 520,
                SkirmishProducerKind.Airport, SkirmishTargetDomain.Ground | SkirmishTargetDomain.Structure, 1));
            kept.Add(Overlay(SkirmishRoleIds.TransportPlane, SkirmishRoleKind.TransportPlane, 340, 0, 0f, 440,
                SkirmishProducerKind.Airport, SkirmishTargetDomain.None, 1));
            return kept.ToArray();
        }

        public static SkirmishRoleOverlay[] CreateS002GroundSlice()
        {
            return new[]
            {
                Overlay(SkirmishRoleIds.Rifle, SkirmishRoleKind.Rifle, 100, 10, 18f, 80,
                    SkirmishProducerKind.Barracks, SkirmishTargetDomain.Infantry | SkirmishTargetDomain.Ground, 4),
                Overlay(SkirmishRoleIds.Gunner, SkirmishRoleKind.Gunner, 110, 14, 16f, 100,
                    SkirmishProducerKind.Barracks, SkirmishTargetDomain.Infantry | SkirmishTargetDomain.Ground, 4),
                Overlay(SkirmishRoleIds.Marksman, SkirmishRoleKind.Marksman, 90, 16, 26f, 120,
                    SkirmishProducerKind.Barracks, SkirmishTargetDomain.Infantry, 4),
                Overlay(SkirmishRoleIds.Breacher, SkirmishRoleKind.Breacher, 120, 18, 12f, 100,
                    SkirmishProducerKind.Barracks, SkirmishTargetDomain.Infantry | SkirmishTargetDomain.Structure, 4),
                Overlay(SkirmishRoleIds.Rocketeer, SkirmishRoleKind.Rocketeer, 90, 22, 24f, 120,
                    SkirmishProducerKind.Barracks, SkirmishTargetDomain.Ground | SkirmishTargetDomain.Structure, 4),
                Overlay(SkirmishRoleIds.Car, SkirmishRoleKind.Car, 180, 12, 20f, 160,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.Infantry | SkirmishTargetDomain.Ground, 1),
                Overlay(SkirmishRoleIds.ApcFast, SkirmishRoleKind.ApcFast, 240, 14, 20f, 160,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.Infantry | SkirmishTargetDomain.Ground, 1),
                Overlay(SkirmishRoleIds.ApcArmored, SkirmishRoleKind.ApcArmored, 280, 16, 22f, 200,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.Infantry | SkirmishTargetDomain.Ground, 1),
                Overlay(SkirmishRoleIds.Tank, SkirmishRoleKind.Tank, 420, 28, 26f, 360,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.Ground | SkirmishTargetDomain.Structure, 1),
                Overlay(SkirmishRoleIds.Radar, SkirmishRoleKind.Radar, 200, 0, 0f, 180,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.None, 1),
                Overlay(SkirmishRoleIds.LogisticsTruck, SkirmishRoleKind.LogisticsTruck, 160, 0, 0f, 100,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.None, 1),
                Overlay(SkirmishRoleIds.Tanker, SkirmishRoleKind.Tanker, 160, 0, 0f, 140,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.None, 1)
            };
        }

        // Ground defense shares the mission's combat scale. Rocketeers can outrange it;
        // infantry needs cover. Both factions receive the same overlay.
        public static SkirmishRoleOverlay WatchtowerStructure() =>
            new SkirmishRoleOverlay
            {
                RoleId = SkirmishStructureIds.Watchtower,
                MaxHealth = 700,
                Damage = 10,
                RangeWorld = 22f,
                TargetDomains = SkirmishTargetDomain.Infantry | SkirmishTargetDomain.Ground,
                SquadMembers = 1
            };

        public static SkirmishRoleOverlay BarracksStructure() =>
            new SkirmishRoleOverlay
            {
                RoleId = SkirmishStructureIds.Barracks,
                RoleKind = SkirmishRoleKind.None,
                MaxHealth = 800,
                Damage = 0,
                RangeWorld = 0f,
                SupplyCost = 0,
                Producer = SkirmishProducerKind.None,
                TargetDomains = SkirmishTargetDomain.None,
                SquadMembers = 1,
                CapabilityCertified = false
            };

        public static SkirmishRoleOverlay HelipadStructure() =>
            new SkirmishRoleOverlay
            {
                RoleId = SkirmishStructureIds.Helipad,
                RoleKind = SkirmishRoleKind.None,
                MaxHealth = 500,
                Damage = 0,
                RangeWorld = 0f,
                SupplyCost = 0,
                Producer = SkirmishProducerKind.Helipad,
                TargetDomains = SkirmishTargetDomain.None,
                SquadMembers = 1,
                CapabilityCertified = false
            };

        public static SkirmishRoleOverlay GroundStagingStructure() =>
            new SkirmishRoleOverlay
            {
                RoleId = SkirmishStructureIds.GroundStaging,
                RoleKind = SkirmishRoleKind.None,
                MaxHealth = 600,
                Damage = 0,
                RangeWorld = 0f,
                SupplyCost = 0,
                Producer = SkirmishProducerKind.GroundStaging,
                TargetDomains = SkirmishTargetDomain.None,
                SquadMembers = 1,
                CapabilityCertified = false
            };

        public static bool TryGet(SkirmishRoleOverlay[] overlays, SkirmishRoleKind kind, out SkirmishRoleOverlay overlay)
        {
            if (overlays != null)
            {
                for (int i = 0; i < overlays.Length; i++)
                {
                    if (overlays[i].RoleKind == kind)
                    {
                        overlay = overlays[i];
                        return true;
                    }
                }
            }

            overlay = default;
            return false;
        }

        public bool TryGet(SkirmishRoleKind kind, out SkirmishRoleOverlay overlay) =>
            TryGet(overlays, kind, out overlay);

        private static SkirmishRoleOverlay Overlay(
            string roleId,
            SkirmishRoleKind kind,
            int health,
            int damage,
            float range,
            int materialsCost,
            SkirmishProducerKind producer,
            SkirmishTargetDomain domains,
            int squadMembers)
        {
            return new SkirmishRoleOverlay
            {
                RoleId = roleId,
                RoleKind = kind,
                MaxHealth = health,
                Damage = damage,
                RangeWorld = range,
                SupplyCost = SkirmishRoleIds.SupplyCost(kind),
                MaterialsCost = materialsCost,
                FuelCost = 0,
                Producer = producer,
                TargetDomains = domains,
                SquadMembers = squadMembers,
                CapabilityCertified = false
            };
        }
    }
}
