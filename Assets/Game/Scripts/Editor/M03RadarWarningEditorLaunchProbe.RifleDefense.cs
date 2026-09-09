using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string RifleKey="Warline.M03.Probe.RifleDefense";
        private static bool riflePositioned, pingRequested, pingVerified;
        private static int nextRifleCommand;
        public static void RunRifleDefense()
        {
            M03RadarWarningConfigBuilder.Build();
            M03RadarWarningLocalizationBuilder.Import();
            SessionState.SetBool(RifleKey,true);
            riflePositioned=pingRequested=pingVerified=false; nextRifleCommand=0;
            RunConvoy();
        }
        private static void PlayRifleDefense(EntityManager em, Entity root, in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            if(!SessionState.GetBool(RifleKey,false) || runtime.Phase!=MissionPhaseKind.Engage ||
                runtime.Outcome!=MissionOutcomeKind.None || facts.ElapsedMilliseconds<5000) return;
            AdvanceRecoveryFault(em,root,in facts);
            using var members=em.GetBuffer<CampaignMissionDefenseMember>(root,true).ToNativeArray(Allocator.Temp);
            if(!riflePositioned)
            {
                int ordinal=0;
                foreach(var member in members)
                {
                    Entity e=member.Entity;
                    if(member.FactionId!=1 || member.IsSensor!=0 || !em.HasComponent<UnitAttack>(e)) continue;
                    var combat=em.GetComponentData<UnitCombat>(e);
                    if(!PerformanceActive) Debug.Log($"[M03RifleProbe] unit={e} combat={JsonUtility.ToJson(combat)}");
                    var position=RecoveryActive && !recoveryRepositioned ? new int2(960+(ordinal%4)*3,450+(ordinal/4)*4) : RoadBarrierActive ? new int2(1003+(ordinal%2)*2,394+(ordinal/2)*4) : SessionState.GetBool(ConstructionKey,false) && ordinal>=4
                        ? new int2(930+(ordinal%4)*3,400) : new int2(840+(ordinal%4)*3,432+(ordinal/4)*4);
                    UnitMoveOrderRequestSystem.EnqueueImmediateMoveOrder(em,e,position);
                    ordinal++;
                }
                if(ordinal!=8) throw new InvalidOperationException("Expected eight commanded rifles.");
                riflePositioned=true;
            }
            if(!RecoveryActive && !pingRequested && facts.ElapsedMilliseconds>=55000)
            {
                if(!UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.RadarPing))
                    throw new InvalidOperationException("Player Ping gateway rejected an available sensor.");
                pingRequested=true;
            }
            if(pingRequested && !pingVerified && em.HasComponent<RadarPingState>(root))
            {
                var ping=em.GetComponentData<RadarPingState>(root);
                if(ping.Result==RadarPingResultKind.Accepted)
                {
                    if(ping.Charges!=1 || ping.LastContactCount<0 || ping.ReadyAtMilliseconds<=facts.ElapsedMilliseconds)
                        throw new InvalidOperationException("Ping did not atomically scan and consume one use.");
                    if(UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.RadarPing))
                        throw new InvalidOperationException("Cooldown allowed a duplicate Ping.");
                    pingVerified=true;
                    if(!PerformanceActive) Debug.Log($"[M03RifleProbe] Ping accepted contacts={ping.LastContactCount} charges={ping.Charges}; duplicate blocked by cooldown");
                }
            }
            if(facts.ElapsedMilliseconds<nextRifleCommand) return;
            nextRifleCommand=facts.ElapsedMilliseconds+1000;
            foreach(var friendly in members)
            {
                Entity rifle=friendly.Entity;
                if(friendly.FactionId!=1 || friendly.IsSensor!=0 || !Alive(em,rifle) || !em.HasComponent<UnitAttack>(rifle)) continue;
                if(em.HasComponent<EngageTarget>(rifle) && Alive(em,em.GetComponentData<EngageTarget>(rifle).Target)) continue;
                float3 from=em.GetComponentData<LocalTransform>(rifle).Position;
                float range=em.GetComponentData<UnitAttack>(rifle).Range;
                Entity target=Entity.Null;
                // Only issue a player attack once a living, active target enters actual weapon contact range.
                // Armed vehicles get priority; health/position/outcome are never edited by this probe.
                foreach(var hostile in members)
                {
                    Entity candidate=hostile.Entity;
                    if(hostile.ElementIndex<0 || !Alive(em,candidate) || em.HasComponent<CampaignMissionCombatSuppressedTag>(candidate) ||
                        math.distancesq(from,em.GetComponentData<LocalTransform>(candidate).Position)>range*range) continue;
                    if(target==Entity.Null || em.HasComponent<UnitMovementBehavior>(candidate) &&
                        em.GetComponentData<UnitMovementBehavior>(candidate).UsesVehicleMotion!=0 && em.HasComponent<UnitAttack>(candidate)) target=candidate;
                }
                if(target==Entity.Null) continue;
                UnitAttackOrderRequestSystem.EnqueueSourceAttackTarget(em,rifle,target);
                if(!PerformanceActive) Debug.Log($"[M03RifleProbe] player attack source={rifle} confirmedTarget={target}");
            }
        }
        private static bool Alive(EntityManager em,Entity e) => e!=Entity.Null && em.Exists(e) &&
            em.HasComponent<UnitHealth>(e) && em.GetComponentData<UnitHealth>(e).Current>0;
    }
}
