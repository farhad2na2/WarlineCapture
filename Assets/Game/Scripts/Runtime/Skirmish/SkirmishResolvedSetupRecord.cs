using Game.Configs;
using Unity.Entities;

namespace Game.Runtime
{
    public sealed class SkirmishResolvedSetupRecord : IComponentData
    {
        public SkirmishResolvedSetup Setup;
    }
}
