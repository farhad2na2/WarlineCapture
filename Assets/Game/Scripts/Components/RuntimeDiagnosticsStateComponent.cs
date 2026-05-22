using Unity.Entities;

public struct RuntimeDiagnosticsStateComponent : IComponentData
{
    public byte VerboseAILogs;
    public byte TransportBoardingDiagnostics;
}
