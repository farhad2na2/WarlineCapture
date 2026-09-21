using System;

namespace Game.Skirmish.Contracts
{
    public enum SkirmishRoleKind : byte
    {
        None = 0,
        Rifle = 1,
        Gunner = 2,
        Marksman = 3,
        Breacher = 4,
        Rocketeer = 5,
        Car = 6,
        ApcFast = 7,
        ApcArmored = 8,
        ApcHeavy = 9,
        Tank = 10,
        AntiAir = 11,
        Radar = 12,
        Siege = 13,
        TransportHeli = 14,
        AttackHeliLight = 15,
        AttackHeli = 16,
        Drone = 17,
        Fighter = 18,
        Strike = 19,
        TransportPlane = 20,
        LogisticsTruck = 21,
        Tanker = 22,
        ConvoyLogic = 23
    }

    public enum SkirmishPopulationCategory : byte
    {
        None = 0,
        Infantry = 1,
        Ground = 2,
        Air = 3,
        LogisticsSupport = 4,
        ObjectiveSupport = 5,
        Structure = 6
    }

    public enum SkirmishProducerKind : byte
    {
        None = 0,
        Barracks = 1,
        GroundStaging = 2,
        Helipad = 3,
        Airport = 4,
        IntelStation = 5,
        AuthoredOnly = 6
    }

    public enum SkirmishReadinessStage : byte
    {
        None = 0,
        Field = 1,
        Established = 2,
        FullArsenal = 3
    }

    public enum SkirmishRosterDisposition : byte
    {
        None = 0,
        CanonicalRoleCandidate = 1,
        RoleAppearanceVariant = 2,
        AlternateFactionAppearance = 3,
        ScenarioSandboxOnly = 4,
        SupportedFacility = 5,
        VisualSourceOnly = 6
    }

    public static class SkirmishRoleIds
    {
        public const string Rifle = "role.rifle";
        public const string Gunner = "role.gunner";
        public const string Marksman = "role.marksman";
        public const string Breacher = "role.breacher";
        public const string Rocketeer = "role.rocketeer";
        public const string Car = "role.car";
        public const string ApcFast = "role.apc_fast";
        public const string ApcArmored = "role.apc_armored";
        public const string ApcHeavy = "role.apc_heavy";
        public const string Tank = "role.tank";
        public const string AntiAir = "role.aa";
        public const string Radar = "role.radar";
        public const string Siege = "role.siege";
        public const string TransportHeli = "role.transport_heli";
        public const string AttackHeliLight = "role.attack_heli_light";
        public const string AttackHeli = "role.attack_heli";
        public const string Drone = "role.drone";
        public const string Fighter = "role.fighter";
        public const string Strike = "role.strike";
        public const string TransportPlane = "role.transport_plane";
        public const string LogisticsTruck = "role.logistics_truck";
        public const string Tanker = "role.tanker";
        public const string ConvoyLogic = "role.convoy_logic";

        public static bool TryParse(string value, out SkirmishRoleKind kind)
        {
            switch (value)
            {
                case Rifle: kind = SkirmishRoleKind.Rifle; return true;
                case Gunner: kind = SkirmishRoleKind.Gunner; return true;
                case Marksman: kind = SkirmishRoleKind.Marksman; return true;
                case Breacher: kind = SkirmishRoleKind.Breacher; return true;
                case Rocketeer: kind = SkirmishRoleKind.Rocketeer; return true;
                case Car: kind = SkirmishRoleKind.Car; return true;
                case ApcFast: kind = SkirmishRoleKind.ApcFast; return true;
                case ApcArmored: kind = SkirmishRoleKind.ApcArmored; return true;
                case ApcHeavy: kind = SkirmishRoleKind.ApcHeavy; return true;
                case Tank: kind = SkirmishRoleKind.Tank; return true;
                case AntiAir: kind = SkirmishRoleKind.AntiAir; return true;
                case Radar: kind = SkirmishRoleKind.Radar; return true;
                case Siege: kind = SkirmishRoleKind.Siege; return true;
                case TransportHeli: kind = SkirmishRoleKind.TransportHeli; return true;
                case AttackHeliLight: kind = SkirmishRoleKind.AttackHeliLight; return true;
                case AttackHeli: kind = SkirmishRoleKind.AttackHeli; return true;
                case Drone: kind = SkirmishRoleKind.Drone; return true;
                case Fighter: kind = SkirmishRoleKind.Fighter; return true;
                case Strike: kind = SkirmishRoleKind.Strike; return true;
                case TransportPlane: kind = SkirmishRoleKind.TransportPlane; return true;
                case LogisticsTruck: kind = SkirmishRoleKind.LogisticsTruck; return true;
                case Tanker: kind = SkirmishRoleKind.Tanker; return true;
                case ConvoyLogic: kind = SkirmishRoleKind.ConvoyLogic; return true;
                default: kind = SkirmishRoleKind.None; return false;
            }
        }

        public static string ToId(SkirmishRoleKind kind)
        {
            switch (kind)
            {
                case SkirmishRoleKind.Rifle: return Rifle;
                case SkirmishRoleKind.Gunner: return Gunner;
                case SkirmishRoleKind.Marksman: return Marksman;
                case SkirmishRoleKind.Breacher: return Breacher;
                case SkirmishRoleKind.Rocketeer: return Rocketeer;
                case SkirmishRoleKind.Car: return Car;
                case SkirmishRoleKind.ApcFast: return ApcFast;
                case SkirmishRoleKind.ApcArmored: return ApcArmored;
                case SkirmishRoleKind.ApcHeavy: return ApcHeavy;
                case SkirmishRoleKind.Tank: return Tank;
                case SkirmishRoleKind.AntiAir: return AntiAir;
                case SkirmishRoleKind.Radar: return Radar;
                case SkirmishRoleKind.Siege: return Siege;
                case SkirmishRoleKind.TransportHeli: return TransportHeli;
                case SkirmishRoleKind.AttackHeliLight: return AttackHeliLight;
                case SkirmishRoleKind.AttackHeli: return AttackHeli;
                case SkirmishRoleKind.Drone: return Drone;
                case SkirmishRoleKind.Fighter: return Fighter;
                case SkirmishRoleKind.Strike: return Strike;
                case SkirmishRoleKind.TransportPlane: return TransportPlane;
                case SkirmishRoleKind.LogisticsTruck: return LogisticsTruck;
                case SkirmishRoleKind.Tanker: return Tanker;
                case SkirmishRoleKind.ConvoyLogic: return ConvoyLogic;
                default: return string.Empty;
            }
        }

        public static SkirmishPopulationCategory Category(SkirmishRoleKind kind)
        {
            switch (kind)
            {
                case SkirmishRoleKind.Rifle:
                case SkirmishRoleKind.Gunner:
                case SkirmishRoleKind.Marksman:
                case SkirmishRoleKind.Breacher:
                case SkirmishRoleKind.Rocketeer:
                    return SkirmishPopulationCategory.Infantry;
                case SkirmishRoleKind.Car:
                case SkirmishRoleKind.ApcFast:
                case SkirmishRoleKind.ApcArmored:
                case SkirmishRoleKind.ApcHeavy:
                case SkirmishRoleKind.Tank:
                case SkirmishRoleKind.AntiAir:
                case SkirmishRoleKind.Radar:
                case SkirmishRoleKind.Siege:
                    return SkirmishPopulationCategory.Ground;
                case SkirmishRoleKind.TransportHeli:
                case SkirmishRoleKind.AttackHeliLight:
                case SkirmishRoleKind.AttackHeli:
                case SkirmishRoleKind.Drone:
                case SkirmishRoleKind.Fighter:
                case SkirmishRoleKind.Strike:
                case SkirmishRoleKind.TransportPlane:
                    return SkirmishPopulationCategory.Air;
                case SkirmishRoleKind.LogisticsTruck:
                case SkirmishRoleKind.Tanker:
                    return SkirmishPopulationCategory.LogisticsSupport;
                case SkirmishRoleKind.ConvoyLogic:
                    return SkirmishPopulationCategory.ObjectiveSupport;
                default:
                    return SkirmishPopulationCategory.None;
            }
        }

        public static int SupplyCost(SkirmishRoleKind kind)
        {
            switch (kind)
            {
                case SkirmishRoleKind.Rifle:
                case SkirmishRoleKind.Gunner:
                case SkirmishRoleKind.Marksman:
                case SkirmishRoleKind.Breacher:
                case SkirmishRoleKind.Rocketeer:
                    return 1;
                case SkirmishRoleKind.Car:
                case SkirmishRoleKind.ApcFast:
                case SkirmishRoleKind.ApcArmored:
                case SkirmishRoleKind.Radar:
                    return 4;
                case SkirmishRoleKind.ApcHeavy:
                case SkirmishRoleKind.Tank:
                case SkirmishRoleKind.AntiAir:
                case SkirmishRoleKind.Siege:
                    return 6;
                case SkirmishRoleKind.TransportHeli:
                case SkirmishRoleKind.AttackHeliLight:
                case SkirmishRoleKind.AttackHeli:
                case SkirmishRoleKind.Drone:
                case SkirmishRoleKind.Fighter:
                case SkirmishRoleKind.Strike:
                case SkirmishRoleKind.TransportPlane:
                    return 8;
                default:
                    return 0;
            }
        }

        public static bool IsOffensiveAir(SkirmishRoleKind kind) =>
            kind == SkirmishRoleKind.AttackHeliLight ||
            kind == SkirmishRoleKind.AttackHeli ||
            kind == SkirmishRoleKind.Fighter ||
            kind == SkirmishRoleKind.Strike;

        public const int InfantrySquadMembers = 4;
    }

    [Flags]
    public enum SkirmishTargetDomain : byte
    {
        None = 0,
        Infantry = 1,
        Ground = 2,
        Structure = 4,
        Air = 8
    }

    [Serializable]
    public struct SkirmishRoleOverlay
    {
        public string RoleId;
        public SkirmishRoleKind RoleKind;
        public int MaxHealth;
        public int Damage;
        public float RangeWorld;
        public int SupplyCost;
        public SkirmishProducerKind Producer;
        public SkirmishTargetDomain TargetDomains;
        public int SquadMembers;
        public bool CapabilityCertified;
    }

    public static class SkirmishStructureIds
    {
        public const string Barracks = "building.barracks";
        public const string GroundStaging = "building.ground_staging";
        public const string BarracksReplacement = "building.barracks.replacement";
    }
}
