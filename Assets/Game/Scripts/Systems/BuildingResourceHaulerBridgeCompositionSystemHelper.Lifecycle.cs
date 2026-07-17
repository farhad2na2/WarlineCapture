namespace Game.Runtime
{
    internal sealed partial class BuildingResourceHaulerBridgeCompositionSystemHelper
    {
        internal void Dispose()
        {
            _moveOrderQueueQueryCache.Dispose();
        }
    }
}
