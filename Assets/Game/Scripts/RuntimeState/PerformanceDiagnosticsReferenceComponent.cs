using Unity.Entities;

public sealed class PerformanceDiagnosticsReferenceComponent : IComponentData
{
    public PerformanceDiagnosticsSystem Diagnostics;
}
