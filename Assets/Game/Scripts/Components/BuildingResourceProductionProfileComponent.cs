using Unity.Entities;

namespace Game.Components
{
    /// <summary>Explicit runtime production period and batch conversion on one physical facility.</summary>
    public struct BuildingResourceProductionProfileComponent : IComponentData
    {
        public float PeriodSeconds;
        public float OilPerPeriod;
        public float FuelPerPeriod;
        public float OilPerFuelBatch;
        public float FuelPerBatch;
        public float FuelBatchSeconds;
        public float FuelBatchProgressSeconds;
    }
}
