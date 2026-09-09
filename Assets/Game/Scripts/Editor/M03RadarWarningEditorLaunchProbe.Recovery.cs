using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string RecoveryKey="Warline.M03.Probe.Recovery";
        private static bool recoverySensorFault,recoveryRepositioned;
        private static bool RecoveryActive=>SessionState.GetBool(RecoveryKey,false);
        public static void RunRecoveryValidation()=>RunChecked(()=>
        {
            recoverySensorFault=recoveryRepositioned=false;
            SessionState.SetBool(RecoveryKey,true); RunRifleDefense();
        });
        private static void AdvanceRecoveryFault(EntityManager em,Entity root,in CampaignMissionAttemptFactsComponent facts)
        {
            if(!RecoveryActive) return;
            if(!recoverySensorFault)
            {
                Entity sensor=RadarPingRequestSystem.FindSensor(em,root);
                if(sensor==Entity.Null) throw new InvalidOperationException("Recovery fault requires the real initial Ground Radar Tank.");
                var health=em.GetComponentData<UnitHealth>(sensor); health.Current=0; em.SetComponentData(sensor,health);
                recoverySensorFault=true;
                Debug.Log("[M03RecoveryProbe] explicit fault injection: sensor health set to zero; all subsequent defense uses normal movement/attack commands; no Ping requested");
            }
            if(!pingVerified && facts.ElapsedMilliseconds>=6000)
            {
                var ping=em.GetComponentData<RadarPingState>(root);
                if(RadarPingRequestSystem.FindSensor(em,root)!=Entity.Null || !UiShellRuntimeGateway.TryReadMissionDefense(out var defense) ||
                    defense.CanPing || !defense.HasWarning || ping.Charges!=2 || ping.PendingRequestId!=0 ||
                    UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.RadarPing))
                    throw new InvalidOperationException("Sensor loss hid the scout warning or accepted/charged an unavailable Ping.");
                pingVerified=true;
                Debug.Log("[M03RecoveryProbe] scout warning remains; dead sensor disables Ping; both charges retained");
            }
            if(!recoveryRepositioned && facts.ElapsedMilliseconds>=35000)
            {
                recoveryRepositioned=true; riflePositioned=false;
                Debug.Log("[M03RecoveryProbe] correct the initial rearward deployment at 35 simulated seconds");
            }
        }
    }
}
