using Unity.Entities;
using ProductionDeliveryLogicalBasis = Game.Simulation.SharedProductionDeliveryKernel.ProductionDeliveryLogicalBasis;

namespace Game.Runtime
{
    // Stored on the owning Skirmish session. The value is captured once from
    // the admitted placement grid frame and remains independent of presentation.
    public struct SkirmishProductionDeliveryBasisComponent : IComponentData
    {
        public ProductionDeliveryLogicalBasis Basis;
        public uint SetupHash;
        public Entity PlacementGrid;
    }
}
