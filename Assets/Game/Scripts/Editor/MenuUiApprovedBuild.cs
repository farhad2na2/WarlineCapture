using System;
using System.Threading.Tasks;
using UnityEngine;
namespace Game.Editor
{
    public static class MenuUiApprovedBuild
    {
        public static async Task<int> BuildAndCapture()
        {
            try
            {
                MenuUiApprovedAuthoring.Localize();
                MainMenuV3PrefabBuilder.Build();
                CampaignOperationsV3PrefabBuilder.Build();
                SkirmishSetupPrefabBuilder.Build();
                OperationsDashboardV3PrefabBuilder.Build();
                CommanderProfileV3PrefabBuilder.Build();
                SplashLoadingV3PrefabBuilder.Build();
                Debug.Log("[MenuUiApprovedBuild] result=Passed nativeScreens=5 loading=rebuilt");
                return await MenuUiAuditCapture.RunAfterChecked();
            }
            catch(Exception error) { Debug.LogException(error); Debug.LogError("[MenuUiApprovedBuild] result=Failed"); MissionEditorValidationExit.Complete(false); return 1; }
        }
    }
}
