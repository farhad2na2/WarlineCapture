extern alias ActualCore;using GameObject=ActualCore::UnityEngine.GameObject;using Unity.Entities;using Unity.Mathematics;using Game.Components;using Unity.Collections;namespace Game.Runtime{internal static class CombatGameObjectVfxRequests
    {
        /*SOURCE:enqueue*/
    }
internal struct SkirmishNativeTransientOwner : IComponentData
    {
        public FixedString64Bytes SessionId;
    }
public static partial class CampaignSplitFrontLauncherSafety{public static void RecordLaunch(EntityManager em,Entity launcher){if(!em.HasComponent<SplitFrontLauncherCommandState>(launcher))return;throw new System.NotSupportedException("Active Campaign RecordLaunch continuation not admitted by ground-Skirmish source integration");}}}
