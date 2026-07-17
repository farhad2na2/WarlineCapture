namespace Game.Runtime
{
    public sealed partial class RtsSelectionRuntimeCameraSystemHelper
    {
        public void Dispose()
        {
            _tacticalFollowStateQueries.Dispose();
        }
    }
}
