using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Runtime;
using UnityEngine;

namespace Game.Composition
{
    /// <summary>
    /// Binds the existing content-access authority into the battle-library seam.
    /// This does not award medals and does not implement the WP2 projection.
    /// </summary>
    static class SkirmishBattleLibraryAccessHost
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SkirmishBattleLibraryServices.Access = Evaluate;
        }

        static SkirmishLibraryCardAccess Evaluate(string catalogId, bool catalogPlayable)
        {
            if (!catalogPlayable || string.IsNullOrEmpty(catalogId))
                return SkirmishLibraryCardAccess.Planned;

            string canonical = "skirmish." + catalogId.ToLowerInvariant();
            switch (ContentAccessRuntime.Evaluate(canonical))
            {
                case ContentAccessState.Allowed:
                    return SkirmishLibraryCardAccess.OwnedPlayable;
                case ContentAccessState.NotOwned:
                    return SkirmishLibraryCardAccess.NotOwned;
                default:
                    return SkirmishLibraryCardAccess.Unavailable;
            }
        }
    }
}
