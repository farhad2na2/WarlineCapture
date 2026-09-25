using System;
using UnityEngine;

namespace Game.Configs
{
    /// <summary>A match-specific recipe applied to runtime definitions; authored prefabs remain immutable.</summary>
    [Serializable]
    public sealed class BuildingProductionRecipe
    {
        public GameObject ProducerPrefab;
        public GameObject UnitPrefab;
        public int Quantity = 1;
        public int MaterialsCost;
        public int CreditsCost;
        public bool UseProducerGroundExit;
    }
}
