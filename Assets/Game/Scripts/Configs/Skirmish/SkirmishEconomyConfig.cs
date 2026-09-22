using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Economy")]
    public sealed class SkirmishEconomyConfig : ScriptableObject
    {
        [SerializeField] private string economyId = "skirmish.economy.v1";
        [SerializeField] private int fieldMaterials = 450;
        [SerializeField] private int fieldOil = 120;
        [SerializeField] private int fieldFuel = 350;
        [SerializeField] private int fieldMaterialsCapacity = 800;
        [SerializeField] private int fieldOilCapacity = 500;
        [SerializeField] private int fieldFuelCapacity = 800;
        [SerializeField] private int establishedMaterials = 900;
        [SerializeField] private int establishedOil = 240;
        [SerializeField] private int establishedFuel = 700;
        [SerializeField] private int establishedMaterialsCapacity = 1800;
        [SerializeField] private int establishedOilCapacity = 1000;
        [SerializeField] private int establishedFuelCapacity = 1600;
        [SerializeField] private int contentVersion = 1;

        public string EconomyId => economyId;
        public int ContentVersion => contentVersion;

        public void ConfigureDefault()
        {
            economyId = "skirmish.economy.v1";
            fieldMaterials = 450;
            fieldOil = 120;
            fieldFuel = 350;
            fieldMaterialsCapacity = 800;
            fieldOilCapacity = 500;
            fieldFuelCapacity = 800;
            establishedMaterials = 900;
            establishedOil = 240;
            establishedFuel = 700;
            establishedMaterialsCapacity = 1800;
            establishedOilCapacity = 1000;
            establishedFuelCapacity = 1600;
            contentVersion = 1;
        }

        public void Stocks(
            SkirmishStartPackageId start,
            SkirmishSizeId size,
            out int materials,
            out int oil,
            out int fuel,
            out int materialsCapacity,
            out int oilCapacity,
            out int fuelCapacity)
        {
            bool established = start == SkirmishStartPackageId.EstablishedBase;
            materials = established ? establishedMaterials : fieldMaterials;
            oil = established ? establishedOil : fieldOil;
            fuel = established ? establishedFuel : fieldFuel;
            materialsCapacity = established ? establishedMaterialsCapacity : fieldMaterialsCapacity;
            oilCapacity = established ? establishedOilCapacity : fieldOilCapacity;
            fuelCapacity = established ? establishedFuelCapacity : fieldFuelCapacity;
            int numerator = 2;
            int denominator = 2;
            if (size == SkirmishSizeId.War)
            {
                numerator = 3;
                denominator = 2;
            }
            else if (size == SkirmishSizeId.LargeWar)
            {
                numerator = 2;
                denominator = 1;
            }

            materials = Scale(materials, numerator, denominator);
            oil = Scale(oil, numerator, denominator);
            fuel = Scale(fuel, numerator, denominator);
            materialsCapacity = Scale(materialsCapacity, numerator, denominator);
            oilCapacity = Scale(oilCapacity, numerator, denominator);
            fuelCapacity = Scale(fuelCapacity, numerator, denominator);
        }

        private static int Scale(int value, int numerator, int denominator) =>
            (value * numerator) / denominator;
    }
}
