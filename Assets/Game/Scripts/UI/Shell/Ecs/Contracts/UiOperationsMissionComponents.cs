using Game.UI.Contracts;
using Unity.Entities;

namespace Game.UI.Shell.Contracts.Ecs
{
    public sealed class UiOperationsMissionReadModel : IComponentData
    {
        public UiOperationsMissionModel Value;
    }

    [InternalBufferCapacity(4)]
    public struct UiOperationsMissionRequest : IBufferElementData
    {
        public UiOperationsMissionAction Action;
        public int SiteIndex;
    }
}
