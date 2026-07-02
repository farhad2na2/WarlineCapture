using Game.UI.Contracts;

namespace Game.Runtime
{
    public sealed class NullMatchIntroStateQuery : IMatchIntroStateQuery
    {
        public static readonly NullMatchIntroStateQuery Instance = new();

        private NullMatchIntroStateQuery()
        {
        }

        public bool IsGameplayInputLocked()
        {
            return false;
        }

        public bool IsIntroComplete()
        {
            return true;
        }
    }
}
