namespace Game.Runtime
{
    public sealed partial class FactionResourceCompositionSystemHelper
    {
        internal void Dispose()
        {
            _storageQueryCache.Dispose();
        }
    }
}
