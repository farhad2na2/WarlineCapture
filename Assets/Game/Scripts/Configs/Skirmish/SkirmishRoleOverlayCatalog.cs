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

        public static SkirmishRoleOverlay[] CreateS002GroundSlice()
        {
            return new[]
            {
                Overlay(SkirmishRoleIds.Rifle, SkirmishRoleKind.Rifle, 100, 10, 18f,
                    SkirmishProducerKind.Barracks, SkirmishTargetDomain.Infantry | SkirmishTargetDomain.Ground, 4),
                Overlay(SkirmishRoleIds.Gunner, SkirmishRoleKind.Gunner, 110, 14, 16f,
                    SkirmishProducerKind.Barracks, SkirmishTargetDomain.Infantry | SkirmishTargetDomain.Ground, 4),
                Overlay(SkirmishRoleIds.Marksman, SkirmishRoleKind.Marksman, 90, 16, 26f,
                    SkirmishProducerKind.Barracks, SkirmishTargetDomain.Infantry, 4),
                Overlay(SkirmishRoleIds.Breacher, SkirmishRoleKind.Breacher, 120, 18, 12f,
                    SkirmishProducerKind.Barracks, SkirmishTargetDomain.Infantry | SkirmishTargetDomain.Structure, 4),
                Overlay(SkirmishRoleIds.Rocketeer, SkirmishRoleKind.Rocketeer, 90, 22, 24f,
                    SkirmishProducerKind.Barracks, SkirmishTargetDomain.Ground | SkirmishTargetDomain.Structure, 4),
                Overlay(SkirmishRoleIds.Car, SkirmishRoleKind.Car, 180, 12, 20f,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.Infantry | SkirmishTargetDomain.Ground, 1),
                Overlay(SkirmishRoleIds.ApcFast, SkirmishRoleKind.ApcFast, 240, 14, 20f,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.Infantry | SkirmishTargetDomain.Ground, 1),
                Overlay(SkirmishRoleIds.ApcArmored, SkirmishRoleKind.ApcArmored, 280, 16, 22f,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.Infantry | SkirmishTargetDomain.Ground, 1),
                Overlay(SkirmishRoleIds.Tank, SkirmishRoleKind.Tank, 420, 28, 26f,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.Ground | SkirmishTargetDomain.Structure, 1),
                Overlay(SkirmishRoleIds.Radar, SkirmishRoleKind.Radar, 200, 0, 0f,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.None, 1),
                Overlay(SkirmishRoleIds.LogisticsTruck, SkirmishRoleKind.LogisticsTruck, 160, 0, 0f,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.None, 1),
                Overlay(SkirmishRoleIds.Tanker, SkirmishRoleKind.Tanker, 160, 0, 0f,
                    SkirmishProducerKind.GroundStaging, SkirmishTargetDomain.None, 1)
            };
        }

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
                Producer = producer,
                TargetDomains = domains,
                SquadMembers = squadMembers,
                CapabilityCertified = false
            };
        }
    }
}
