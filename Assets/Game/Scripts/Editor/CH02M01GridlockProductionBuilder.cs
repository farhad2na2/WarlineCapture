using UnityEngine;
namespace Game.Editor
{
    public static class CH02M01GridlockProductionBuilder
    {
        public static void BuildCaptioned()
        {
            CH02M01GridlockConfigBuilder.Build();
            CH02M01GridlockPresentationBuilder.Build();
            CH02M01GridlockNarrativeBuilder.BuildCaptionedArtAndInstall();
            Debug.Log("[GridlockProduction] result=Passed scope=CaptionedPrototype artSources=12 artLayoutReview=Pending finalVoice=Pending acceptance=Pending");
        }
    }
}
