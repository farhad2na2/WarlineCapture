using Unity.Entities;
using Unity.Mathematics;

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
    public float3 World;
}

public struct RuntimeGameplayLegacyMirrorComponent : IComponentData
{
    public byte HasGameplayState;
    public byte HasCameraInput;
    public byte HasCameraFocusRequest;
    public RuntimeGameplayStateComponent GameplayState;
    public RuntimeCameraInputComponent CameraInput;
    public RuntimeCameraFocusRequestComponent CameraFocusRequest;
}
