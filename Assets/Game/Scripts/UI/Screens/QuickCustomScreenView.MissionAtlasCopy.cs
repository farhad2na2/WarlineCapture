using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        // Shared with the Skirmish result screen (SkirmishLibraryMissionNames).
        private string AtlasMissionName(string objective,int slot)=>SkirmishLibraryMissionNames.Name(objective,slot);
        private static string AtlasObjectivePurpose(string objective)
        {
            switch(objective)
            {
                case "FC":return AtlasWords.Get("purpose.fc","Control the front line. Hold your ground against the enemy.","خط مقدم را کنترل کن و در برابر دشمن نگه دار.");
                case "BT":return AtlasWords.Get("purpose.bt","Break through the enemy defenses.","از دفاع دشمن عبور کن.");
                case "CE":return AtlasWords.Get("purpose.ce","Protect the convoy along its route.","در مسیر از کاروان محافظت کن.");
                default:return AtlasWords.Get("purpose.ba","Destroy the enemy HQ. Keep your own HQ standing.","ستاد دشمن را نابود کن و از ستاد خودت محافظت کن.");
            }
        }
    }
}
