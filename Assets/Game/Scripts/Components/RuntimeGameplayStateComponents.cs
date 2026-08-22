using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public struct RuntimeGameplayStateComponent : IComponentData
    {
        public byte PlayRequested;
        public byte SimulationActive;
        public byte SelectionModeActive;
        public byte BuildModeActive;
        public byte FullscreenMapOpen;
        public byte FullscreenMapIsoMode;
        public byte SuppressNextWorldClick;
        public byte PlayerAutoModeEnabled;
    }

    public struct RuntimeCameraInputComponent : IComponentData
    {
        public byte ZoomInHeld;
        public byte ZoomOutHeld;
    }

    public struct RuntimeCameraFocusRequestComponent : IComponentData
    {
        public byte Requested;
        public byte Smooth;
        public byte UseTacticalRevealZoom;
        public byte UseExplicitYaw;
        public float SmoothTimeSeconds;
        public float YawDegrees;
        public float3 World;
    }

}
