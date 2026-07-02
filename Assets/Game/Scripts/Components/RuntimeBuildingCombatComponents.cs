using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public struct RuntimeBuildingCombatTag : IComponentData { }

    public struct RuntimeBuildingCombatInfo : IComponentData
    {
        public int RuntimeBuildingId;
        public byte OwnerFactionId;
        public int2 OriginCell;
        public int2 FootprintCells;
        public byte IsWall;
        public byte IsGate;
    }
}
