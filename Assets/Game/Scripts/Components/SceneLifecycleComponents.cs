using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public enum SceneLifecycleSceneId : byte
    {
        None = 0,
        Menu = 1,
        Match = 2
    }

    public enum SceneLifecycleRequestKind : byte
    {
        LoadAdditive = 1,
        Unload = 2
    }

    public enum SceneLifecycleStatusKind : byte
    {
        None = 0,
        Queued = 1,
        Loading = 2,
        Loaded = 3,
        Unloading = 4,
        Unloaded = 5,
        Failed = 6
    }

    public enum SceneLifecycleResultCode : byte
    {
        None = 0,
        Accepted = 1,
        IgnoredDuplicate = 2,
        InvalidRequest = 3,
        SceneOperationFailed = 4
    }

    public struct SceneLifecycleRootComponent : IComponentData
    {
    }

    public struct SceneLifecycleQueueComponent : IComponentData
    {
        public int LastRequestId;
    }

    public struct SceneLifecycleStateComponent : IComponentData
    {
        public SceneLifecycleSceneId ActiveScene;
        public SceneLifecycleStatusKind Status;
        public int ActiveRequestId;
        public float Progress01;
        public byte IsBusy;
        public byte IsMatchLoaded;
    }

    public struct SceneLifecycleRequestElement : IBufferElementData
    {
        public SceneLifecycleRequestKind Kind;
        public SceneLifecycleSceneId Scene;
        public int RequestId;
        public byte ActivateOnLoad;
    }

    public struct SceneLifecycleResultElement : IBufferElementData
    {
        public SceneLifecycleRequestKind Kind;
        public SceneLifecycleSceneId Scene;
        public SceneLifecycleStatusKind Status;
        public SceneLifecycleResultCode ResultCode;
        public int RequestId;
        public FixedString128Bytes Message;
    }
}
