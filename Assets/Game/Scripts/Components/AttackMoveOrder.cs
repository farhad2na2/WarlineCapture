using Unity.Entities;
using Unity.Mathematics;
namespace Game.Components
{
    // A player destination retained while ordinary combat temporarily interrupts travel.
    public struct AttackMoveOrder : IComponentData
    {
        public int2 Destination;
        public float3 Position;
        public double RetryAt;
    }
}
