using Unity.Entities;

// The S073 source host retains owner EntityManager semantics but has no
// SimulationSystemGroup scheduler. Supply only the SystemState members needed
// to invoke and register original OnCreate/OnUpdate in this focused fixture.
namespace Unity.Entities
{
    public partial struct SystemState
    {
        public World World;
    }
}
