using System;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct SkirmishRoleBindingConfig
    {
        public string RoleId;
        public SkirmishRoleKind Kind;
        public string SourceConfigPath;
        public string RuntimePrefabKey;
        public SkirmishProducerKind Producer;
        public SkirmishReadinessStage RequiredReadiness;
        public SkirmishRosterDisposition Disposition;
        public bool CapabilityCertified;
    }

    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Role Catalog")]
    public sealed class SkirmishRoleCatalogConfig : ScriptableObject
    {
        [SerializeField] private string catalogId = "skirmish.roles.v1";
        [SerializeField] private SkirmishRoleBindingConfig[] roles = Array.Empty<SkirmishRoleBindingConfig>();
        [SerializeField] private int contentVersion = 1;

        public string CatalogId => catalogId;
        public SkirmishRoleBindingConfig[] Roles => roles;
        public int ContentVersion => contentVersion;

        public void ConfigureCanonicalGroundSlice()
        {
            catalogId = "skirmish.roles.v1";
            contentVersion = 1;
            roles = new[]
            {
                Bind(SkirmishRoleIds.Rifle, SkirmishRoleKind.Rifle,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset",
                    "Unit_Chr_Soldier_Male_02_Alt_04", SkirmishProducerKind.Barracks, SkirmishReadinessStage.Field),
                Bind(SkirmishRoleIds.Gunner, SkirmishRoleKind.Gunner,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_01_Config.asset",
                    "Unit_Chr_Soldier_Male_01", SkirmishProducerKind.Barracks, SkirmishReadinessStage.Field),
                Bind(SkirmishRoleIds.Marksman, SkirmishRoleKind.Marksman,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Female_01_Config.asset",
                    "Unit_Chr_Soldier_Female_01", SkirmishProducerKind.Barracks, SkirmishReadinessStage.Field),
                Bind(SkirmishRoleIds.Breacher, SkirmishRoleKind.Breacher,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_02_Config.asset",
                    "Unit_Chr_Soldier_Female_02_Alt_02", SkirmishProducerKind.Barracks, SkirmishReadinessStage.Field),
                Bind(SkirmishRoleIds.Rocketeer, SkirmishRoleKind.Rocketeer,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Ghillie_Male_01_Config.asset",
                    "Unit_Chr_Ghillie_Male_01", SkirmishProducerKind.Barracks, SkirmishReadinessStage.Field),
                Bind(SkirmishRoleIds.Car, SkirmishRoleKind.Car,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Light_Armored_Car_Config.asset",
                    "Unit_Veh_Light_Armored_Car", SkirmishProducerKind.GroundStaging, SkirmishReadinessStage.Field),
                Bind(SkirmishRoleIds.ApcFast, SkirmishRoleKind.ApcFast,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_APC_Fast_Config.asset",
                    "Unit_Veh_APC_Fast", SkirmishProducerKind.GroundStaging, SkirmishReadinessStage.Field),
                Bind(SkirmishRoleIds.ApcArmored, SkirmishRoleKind.ApcArmored,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_APC_Slow_Config.asset",
                    "Unit_Veh_APC_Slow", SkirmishProducerKind.GroundStaging, SkirmishReadinessStage.Field),
                Bind(SkirmishRoleIds.ApcHeavy, SkirmishRoleKind.ApcHeavy,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_APC_Heavy_Config.asset",
                    "Unit_Veh_APC_Heavy", SkirmishProducerKind.GroundStaging, SkirmishReadinessStage.Established),
                Bind(SkirmishRoleIds.Tank, SkirmishRoleKind.Tank,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Tank_USA_Config.asset",
                    "Unit_Veh_Tank_USA", SkirmishProducerKind.GroundStaging, SkirmishReadinessStage.Established),
                Bind(SkirmishRoleIds.Radar, SkirmishRoleKind.Radar,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Radar_Tank.asset",
                    "Unit_Veh_Radar_Tank", SkirmishProducerKind.GroundStaging, SkirmishReadinessStage.Established),
                Bind(SkirmishRoleIds.Siege, SkirmishRoleKind.Siege,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Missle_Launcher_Ground_Config.asset",
                    "Unit_Veh_Missle_Launcher_Ground", SkirmishProducerKind.GroundStaging, SkirmishReadinessStage.FullArsenal),
                Bind(SkirmishRoleIds.TransportHeli, SkirmishRoleKind.TransportHeli,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Helicopter_Transport_Config.asset",
                    "Unit_Veh_Helicopter_Transport", SkirmishProducerKind.Helipad, SkirmishReadinessStage.Established),
                Bind(SkirmishRoleIds.Drone, SkirmishRoleKind.Drone,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Drone_Config.asset",
                    "Unit_Veh_Drone", SkirmishProducerKind.IntelStation, SkirmishReadinessStage.Established),
                Bind(SkirmishRoleIds.LogisticsTruck, SkirmishRoleKind.LogisticsTruck,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Truck_Tray.asset",
                    "Unit_Veh_Truck_Tray", SkirmishProducerKind.GroundStaging, SkirmishReadinessStage.Field),
                Bind(SkirmishRoleIds.Tanker, SkirmishRoleKind.Tanker,
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Truck_Tanker.asset",
                    "Unit_Veh_Truck_Tanker", SkirmishProducerKind.GroundStaging, SkirmishReadinessStage.Field)
            };
        }

        public bool TryGet(string roleId, out SkirmishRoleBindingConfig binding)
        {
            for (int i = 0; i < roles.Length; i++)
            {
                if (roles[i].RoleId == roleId)
                {
                    binding = roles[i];
                    return true;
                }
            }

            binding = default;
            return false;
        }

        private static SkirmishRoleBindingConfig Bind(
            string roleId,
            SkirmishRoleKind kind,
            string sourcePath,
            string prefabKey,
            SkirmishProducerKind producer,
            SkirmishReadinessStage readiness)
        {
            return new SkirmishRoleBindingConfig
            {
                RoleId = roleId,
                Kind = kind,
                SourceConfigPath = sourcePath,
                RuntimePrefabKey = prefabKey,
                Producer = producer,
                RequiredReadiness = readiness,
                Disposition = SkirmishRosterDisposition.CanonicalRoleCandidate,
                CapabilityCertified = false
            };
        }
    }
}
