using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Skirmish;
using UnityEngine;

namespace Game.Composition
{
    /// <summary>
    /// Binds content access into the stub view model. Programmer 1's view model
    /// reads the same authority itself. This does not award medals.
    /// </summary>
    static class SkirmishBattleLibraryAccessHost
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SkirmishLibraryStubViewModel.Access = Evaluate;
        }

        static SkirmishCardAccess Evaluate(string catalogId, bool catalogPlayable)
        {
            if (!catalogPlayable || string.IsNullOrEmpty(catalogId))
                return SkirmishCardAccess.Planned;

            string canonical = "skirmish." + catalogId.ToLowerInvariant();
            switch (ContentAccessRuntime.Evaluate(canonical))
            {
                case ContentAccessState.Allowed:
                    return SkirmishCardAccess.Playable;
                case ContentAccessState.NotOwned:
                    return SkirmishCardAccess.NotOwned;
                default:
                    return SkirmishCardAccess.Unavailable;
            }
        }
    }
}
