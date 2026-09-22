using System;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Army Profile")]
    public sealed class SkirmishArmyProfileConfig : ScriptableObject
    {
        [SerializeField] private string profileId = "G";
        [SerializeField] private SkirmishArmyProfileId kind = SkirmishArmyProfileId.GroundManeuver;
        [SerializeField] private string[] allowedRoleIds = new string[0];
        [SerializeField] private string[] excludedRoleIds = new string[0];
        [SerializeField] private int contentVersion = 1;

        public string ProfileId => profileId;
        public SkirmishArmyProfileId Kind => kind;
        public string[] AllowedRoleIds => allowedRoleIds;
        public string[] ExcludedRoleIds => excludedRoleIds;
        public int ContentVersion => contentVersion;

        public bool AllowsOffensiveAir =>
            Allows(SkirmishRoleIds.AttackHeliLight) ||
            Allows(SkirmishRoleIds.AttackHeli) ||
            Allows(SkirmishRoleIds.Fighter) ||
            Allows(SkirmishRoleIds.Strike);

        public bool AllowsAdvancedAir =>
            Allows(SkirmishRoleIds.Fighter) ||
            Allows(SkirmishRoleIds.Strike) ||
            Allows(SkirmishRoleIds.TransportPlane);

        private static SkirmishArmyProfileConfig cachedGround;
        private static SkirmishArmyProfileConfig cachedAir;

        public static SkirmishArmyProfileConfig ResolveCached(SkirmishArmyProfileId id)
        {
            if (id == SkirmishArmyProfileId.GroundManeuver)
                return Cache(ref cachedGround, static profile => profile.ConfigureGroundManeuver());
            if (id == SkirmishArmyProfileId.AirMobile)
                return Cache(ref cachedAir, static profile => profile.ConfigureAirMobile());
            return null;
        }

        private static SkirmishArmyProfileConfig Cache(
            ref SkirmishArmyProfileConfig slot,
            Action<SkirmishArmyProfileConfig> configure)
        {
            if (slot == null)
            {
                slot = CreateInstance<SkirmishArmyProfileConfig>();
                configure(slot);
                slot.hideFlags = HideFlags.HideAndDontSave;
            }

            return slot;
        }

        public void ConfigureGroundManeuver()
        {
            profileId = "G";
            kind = SkirmishArmyProfileId.GroundManeuver;
            allowedRoleIds = new[]
            {
                SkirmishRoleIds.Rifle,
                SkirmishRoleIds.Gunner,
                SkirmishRoleIds.Marksman,
                SkirmishRoleIds.Breacher,
                SkirmishRoleIds.Rocketeer,
                SkirmishRoleIds.Car,
                SkirmishRoleIds.ApcFast,
                SkirmishRoleIds.ApcArmored,
                SkirmishRoleIds.ApcHeavy,
                SkirmishRoleIds.Tank,
                SkirmishRoleIds.Radar,
                SkirmishRoleIds.Siege,
                SkirmishRoleIds.TransportHeli,
                SkirmishRoleIds.Drone,
                SkirmishRoleIds.LogisticsTruck,
                SkirmishRoleIds.Tanker
            };
            excludedRoleIds = new[]
            {
                SkirmishRoleIds.AttackHeliLight,
                SkirmishRoleIds.AttackHeli,
                SkirmishRoleIds.Fighter,
                SkirmishRoleIds.Strike,
                SkirmishRoleIds.TransportPlane,
                SkirmishRoleIds.AntiAir
            };
            contentVersion = 1;
        }

        public void ConfigureAirMobile()
        {
            profileId = "A";
            kind = SkirmishArmyProfileId.AirMobile;
            allowedRoleIds = new[]
            {
                SkirmishRoleIds.Rifle,
                SkirmishRoleIds.Gunner,
                SkirmishRoleIds.Marksman,
                SkirmishRoleIds.Breacher,
                SkirmishRoleIds.Rocketeer,
                SkirmishRoleIds.Car,
                SkirmishRoleIds.ApcFast,
                SkirmishRoleIds.ApcArmored,
                SkirmishRoleIds.Radar,
                SkirmishRoleIds.AntiAir,
                SkirmishRoleIds.TransportHeli,
                SkirmishRoleIds.AttackHeliLight,
                SkirmishRoleIds.AttackHeli,
                SkirmishRoleIds.Drone,
                SkirmishRoleIds.Fighter,
                SkirmishRoleIds.Strike,
                SkirmishRoleIds.TransportPlane,
                SkirmishRoleIds.LogisticsTruck,
                SkirmishRoleIds.Tanker
            };
            excludedRoleIds = new[]
            {
                SkirmishRoleIds.Tank,
                SkirmishRoleIds.ApcHeavy,
                SkirmishRoleIds.Siege
            };
            contentVersion = 1;
        }

        public bool Allows(string roleId)
        {
            if (string.IsNullOrEmpty(roleId))
                return false;
            for (int i = 0; i < excludedRoleIds.Length; i++)
            {
                if (excludedRoleIds[i] == roleId)
                    return false;
            }

            if (allowedRoleIds.Length == 0)
                return true;
            for (int i = 0; i < allowedRoleIds.Length; i++)
            {
                if (allowedRoleIds[i] == roleId)
                    return true;
            }

            return false;
        }
    }
}
