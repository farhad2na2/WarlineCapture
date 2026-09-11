using System;
using System.IO;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string ConvoyKey = "Warline.M03.LaunchProbe.Convoy";
        private const string InspectKey = "Warline.M03.LaunchProbe.Inspect";
        private static bool ConvoyProbeActive => SessionState.GetBool(ConvoyKey, false);
        private static double ProbeTimeoutSeconds => EveryEntryActive ? 600 : SessionState.GetBool(HudRoadKey,false) ? 420 : CrossMissionActive ? 800 : SessionState.GetBool(LifecycleKey,false) ? 1500 : ConvoyProbeActive ? 420 : 180;

        public static void RunConvoy()
        {
            SessionState.SetBool(ConvoyKey, true);
            Run();
        }

        public static void BuildAndRunConvoy()
        {
            M03RadarWarningConfigBuilder.Build();
            M03RadarWarningLocalizationBuilder.Import();
            RunConvoy();
        }

        public static void RunConvoyInspection()
        {
            SessionState.SetBool(InspectKey, true);
            RunConvoy();
        }

        private static void ValidateStartingBudget(EntityManager em)
        {
            using EntityQuery resources = em.CreateEntityQuery(typeof(FactionEconomy), typeof(FactionTacticalMaterialsComponent));
            using NativeArray<Entity> entities = resources.ToEntityArray(Allocator.Temp);
            int owners = 0;
            foreach (Entity entity in entities)
            {
                FactionEconomy economy = em.GetComponentData<FactionEconomy>(entity);
                if (!FactionIdentity.IsPlayerControlled(economy.FactionId)) continue;
                owners++;
                FactionTacticalMaterialsComponent materials = em.GetComponentData<FactionTacticalMaterialsComponent>(entity);
                Debug.Log($"[M03LaunchProbe] actual budget credits={economy.Money} materials={materials.Current}/{materials.Capacity}");
                if (economy.Money != 50000 || materials.Current != 100 || materials.Capacity != 100)
                    throw new InvalidOperationException("The real M3 starting budget was overwritten or charged for its initial producer.");
            }
            if (owners != 1) throw new InvalidOperationException("Expected one player resource owner.");
        }

        private static bool AdvanceConvoyProbe(EntityManager em, Entity root,
            in CampaignMissionRuntimeComponent runtime, in CampaignMissionAttemptFactsComponent facts)
        {
            if (!ConvoyProbeActive) return false;
            if (facts.HostileRosterIntegrityFault != 0) throw new InvalidOperationException("Convoy roster integrity fault.");
            PlayRifleDefense(em,root,in runtime,in facts);
            if (runtime.Phase == MissionPhaseKind.Engage && facts.ElapsedMilliseconds >= 5000 && !capturedHud)
            {
                ValidateStartingBudget(em);
                if(!PerformanceActive) {M03RadarWarningRuntimeGridProbe.Capture(em, Output); ScreenCapture.CaptureScreenshot(Output + "/m03_ready_hud.png");}
                capturedHud = true;
                // Accelerates simulation only. This lane makes no real-time performance claim.
                Time.timeScale = PerformanceActive || MissionMotionEditorAudit.Active || SessionState.GetBool(GuidanceJourneyKey,false) ? 1f : 3f;
            }
            PlayConstructionDefense(em,root,in runtime,in facts);
            if (SessionState.GetBool(InspectKey, false) && facts.ElapsedMilliseconds >= 60000)
            { Time.timeScale = 0f; return true; }
            if (runtime.Outcome == MissionOutcomeKind.None && facts.ElapsedMilliseconds < 260000) return true;
            LogDefenseMembers(em, root);
            ScreenCapture.CaptureScreenshot(Output + "/m03_idle_convoy.png");
            SessionState.SetBool(ConvoyKey, false);
            bool passed = SessionState.GetBool(RifleKey,false)
                ? runtime.Outcome==MissionOutcomeKind.Victory && pingVerified && (!RoadBarrierActive || roadBarrierVerified) && (!SessionState.GetBool(ConstructionKey,false) || constructionVerified) : runtime.Outcome!=MissionOutcomeKind.None;
            Complete(passed,
                $"strategy={(SessionState.GetBool(RifleKey,false) ? "rifle defense with player commands" : "idle")} outcome={runtime.Outcome} elapsed={facts.ElapsedMilliseconds} defeated={facts.HostileDefeatedCount}/7 core={facts.CoreBreached} postDestroyed={facts.ForwardPostDestroyed}; simulation speed=3");
            return true;
        }

        private static void LogDefenseMembers(EntityManager em, Entity root)
        {
            if (!ConvoyProbeActive || !em.HasBuffer<CampaignMissionDefenseMember>(root)) return;
            DynamicBuffer<CampaignMissionDefenseMember> members = em.GetBuffer<CampaignMissionDefenseMember>(root, true);
            for (int i = 0; i < members.Length; i++)
            {
                CampaignMissionDefenseMember member = members[i];
                if (member.ElementIndex < 0 || !em.Exists(member.Entity) ||
                    !em.HasComponent<CampaignMissionUnitRoleComponent>(member.Entity) ||
                    !em.HasComponent<UnitHealth>(member.Entity) || !em.HasComponent<LocalTransform>(member.Entity)) continue;
                var role = em.GetComponentData<CampaignMissionUnitRoleComponent>(member.Entity);
                var health = em.GetComponentData<UnitHealth>(member.Entity);
                var position = em.GetComponentData<LocalTransform>(member.Entity).Position;
                string line = $"element={member.ElementIndex} entity={member.Entity.Index} position={position} health={health.Current}/{health.Max} waypoint={role.RouteIndex} order={role.PatrolOrderVersion} suppressed={em.HasComponent<CampaignMissionCombatSuppressedTag>(member.Entity)}";
                File.AppendAllText(Output + "/state.txt", line + "\n");
                Debug.Log("[M03ConvoyProbe] " + line);
                if (health.Current > 0)
                    LogMovement(em, member.Entity);
            }
        }
    }
}
