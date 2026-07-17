namespace Game.Runtime
{
    public sealed partial class SelectionUiCameraSystemHelper
    {
        public void Dispose()
        {
            _tacticalFollowCameraStateQueryCache.Dispose();
        }
    }
}
