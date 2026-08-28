using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public sealed class BuildingProductionConfigEntry
    {
        [SerializeField] private GameObject spawnUnitPrefab;
        [SerializeField, Min(1)] private int quantity = 1;

        public GameObject SpawnUnitPrefab
        {
            get => spawnUnitPrefab;
            set => spawnUnitPrefab = value;
        }

        public int Quantity
        {
            get => Mathf.Max(1, quantity);
            set => quantity = Mathf.Max(1, value);
        }
    }

    public enum BuildingRole : byte
    {
        None = 0,
        House = 1,
        Shop = 2,
        CityHall = 3,
        TentRefugee = 4,
        MilitaryCamp = 5
    }

    public enum GeneratedCityBuildingRole : byte
    {
        None = 0,
        House = 1,
        Shop = 2,
        Civic = 3,
        Other = 4
    }

    public enum UnitAnimationKind : byte
    {
        Idle = 0,
        Aim = 1,
        Shoot = 2,
        Grenade = 3,
        Walk = 4,
        WalkAim = 5,
        WalkShoot = 6,
        Run = 7,
        RunAim = 8,
        RunShoot = 9,
        Reload = 10,
        Death01 = 11,
        Death02 = 12,
        Death03 = 13
    }

    [Serializable]
    public sealed class GameStringConfigEntry
    {
        [SerializeField] private string key;
        [TextArea, SerializeField] private string value;
        [SerializeField] private string audioEventId;

        public string Key => key;
        public string Value => value;
        public string AudioEventId => audioEventId;
    }

    public enum AIControllerRole : byte
    {
        Enemy = 0,
        PlayerAuto = 1
    }

    public enum AIControllerDifficulty : byte
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }
}
