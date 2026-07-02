using Unity.Entities;

namespace Game.Components
{
    public struct RuntimeDiagnosticsStateComponent : IComponentData
    {
        public byte VerboseAILogs;
        public byte TransportBoardingDiagnostics;
        public byte BuildingRuntimeSliceDiagnostics;
    }
}
