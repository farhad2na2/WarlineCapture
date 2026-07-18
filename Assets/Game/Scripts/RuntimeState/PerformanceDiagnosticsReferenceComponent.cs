using Unity.Entities;

namespace Game.Runtime
{
    public sealed class PerformanceDiagnosticsReferenceComponent : IComponentData
    {
        public PerformanceDiagnosticsSystemHelper Diagnostics;
    }
}
