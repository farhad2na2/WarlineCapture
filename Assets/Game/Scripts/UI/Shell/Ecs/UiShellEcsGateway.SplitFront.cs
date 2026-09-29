using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiSplitFrontGateway
    {
        private bool TrySplitFront(out EntityManager em,out Entity root,out Entity launcher)
        {
            launcher=Entity.Null;
            if(!TryGetMissionRoot(out em,out root)||!em.HasComponent<CampaignMissionSplitFrontState>(root))return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if(runtime.MissionId.ToString()!=CampaignMissionSequence.SplitFront||runtime.Phase!=MissionPhaseKind.Engage||runtime.Outcome!=MissionOutcomeKind.None)return false;
            launcher=em.GetComponentData<CampaignMissionSplitFrontState>(root).Launcher;return true;
        }
        public bool TryReadSplitFrontLauncher(out UiSplitFrontLauncherModel model)
        {
            model=default;if(!TrySplitFront(out var em,out var root,out var launcher))return false;
            bool exists=em.Exists(launcher)&&em.HasComponent<SplitFrontLauncherCommandState>(launcher);
            var command=exists?em.GetComponentData<SplitFrontLauncherCommandState>(launcher):default;
            var configuration=exists?em.GetComponentData<GroundMissileLauncherComponent>(launcher):default;
            Entity target=command.CommandedTarget;
            bool impact=em.Exists(target)&&em.HasComponent<LocalTransform>(target);
            model=new UiSplitFrontLauncherModel(true,
                exists&&em.HasComponent<SelectedUnitTag>(launcher),impact,exists?em.GetComponentData<LocalTransform>(launcher).Position:default,
                impact?em.GetComponentData<LocalTransform>(target).Position:default,command.ProtectedCenter,configuration.MinRange,configuration.MaxRange,configuration.DamageRadius,command.ProtectedRadius);return true;
        }
    }
}
