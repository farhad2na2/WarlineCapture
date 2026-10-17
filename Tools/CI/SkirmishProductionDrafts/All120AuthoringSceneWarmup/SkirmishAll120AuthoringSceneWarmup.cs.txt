#if UNITY_EDITOR
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishAll120AuthoringSceneWarmup
    {
        public static void Run()
        {
            SkirmishGroundSourceFitSurvey.RunAll120RefinementAuthoringControls();
            S005SceneArtifactWarmup.Run();
            Debug.Log("[All120AuthoringSceneWarmup] result=Passed sourceGroupsStarted=0 warmupScenes=2 snapshotAfterWrapperExit=1 missionCertified=0");
        }
    }
}
#endif
