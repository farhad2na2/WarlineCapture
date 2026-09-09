using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionGuidanceProjectionSystem
    {
        private static readonly FixedString64Bytes RadarMissionId="saga.ch01.m03.radar_warning";
        private static readonly FixedString64Bytes RadarFork="anchor.ch01.m03.fork";
        private static readonly FixedString64Bytes RadarContinue="mission.m03.action.continue";
        private static readonly FixedString64Bytes RadarAct="mission.m03.action.do_it";
        private static readonly FixedString128Bytes RadarTower="Building_GuardTower";
        private static readonly FixedString128Bytes RadarBarrier="Building_Road_Barrier";
        private static readonly FixedString128Bytes RadarRifle="Unit_Chr_Soldier_Male_02_Alt_04";

        private bool TryUpdateDefenseGuidance(ref SystemState state, Entity root, in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts, in AssistantSettingsComponent settings,
            in CampaignMissionGuidanceProjectionComponent current)
        {
            if(!runtime.MissionId.Equals(RadarMissionId)) return false;
            EntityManager em=state.EntityManager;
            if(!em.HasComponent<CampaignMissionDefenseStateComponent>(root) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog,in runtime,out int index))
            { ClearDefenseGuidance(em,root,in current); return true; }
            ref var definition=ref catalog.Blob.Value.Missions[index];
            var defense=em.GetComponentData<CampaignMissionDefenseStateComponent>(root);
            if(!defense.SessionToken.Equals(runtime.SessionToken) || defense.AttemptOrdinal!=runtime.AttemptOrdinal || defense.SourceVersion!=runtime.SourceVersion || runtime.Phase!=MissionPhaseKind.Engage ||
                runtime.Outcome!=MissionOutcomeKind.None || runtime.Guidance==NarrativeGuidanceMode.Minimal ||
                runtime.RunKind!=MissionRunKind.FirstClear && runtime.ReplayTutorialEnabled==0)
            { ClearDefenseGuidance(em,root,in current); return true; }
            if(!SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) || gameplay.SimulationActive==0) return true;
            bool positioned=false,holding=false,built=false,main=false,selectedFriendly=false;
            Entity friendly=Entity.Null;
            float3 fork=defense.InnerCoreCenter;
            if(SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) && metadata.Blob.IsCreated &&
                CampaignMissionSpawnSystem.TryFindAnchor(ref metadata.Blob.Value,RadarFork,out var anchor)) fork=anchor.Position;
            int positionedCount=0;
            foreach(var (role,faction,health,attack,pose,e) in SystemAPI.Query<RefRO<CampaignMissionUnitRoleComponent>,RefRO<Faction>,
                        RefRO<UnitHealth>,RefRO<UnitAttack>,RefRO<LocalTransform>>().WithEntityAccess())
            {
                if(!role.ValueRO.SessionToken.Equals(runtime.SessionToken) || faction.ValueRO.Id!=1 || health.ValueRO.Current<=0) continue;
                if(!em.HasComponent<UnitCombat>(e) || em.GetComponentData<UnitCombat>(e).CanAttack==0) continue;
                if(friendly==Entity.Null) friendly=e;
                if(em.HasComponent<SelectedUnitTag>(e)) {selectedFriendly=true; friendly=e;}
                float range=math.max(10,attack.ValueRO.Range-10);
                if(!em.HasComponent<UnitPathRequest>(e) && !em.HasComponent<UnitPathFollow>(e) &&
                    math.distancesq(pose.ValueRO.Position.xz,fork.xz)<=range*range) positionedCount++;
                holding|=em.HasComponent<HoldPositionOrderTag>(e);
            }
            int producedCount=0;
            if(em.HasComponent<CampaignMissionAttemptFactProjectionStateComponent>(root))
            {
                int baseline=em.GetComponentData<CampaignMissionAttemptFactProjectionStateComponent>(root).ProducedUnitReadModelBaselineCount;
                foreach(var produced in SystemAPI.Query<DynamicBuffer<BuildingProducedUnitReadModel>>())
                {
                    producedCount=math.max(producedCount,CampaignMissionAttemptFactProjectionSystem.CountMatchingProducedUnits(em,produced,baseline,RadarRifle,4));
                    for(int i=math.max(0,baseline);i<produced.Length;i++)
                    {
                        var unit=produced[i]; Entity e=unit.Unit;
                        if(unit.HasOwnerFaction==0 || unit.OwnerFactionId!=1 || !em.Exists(e) || em.HasComponent<CampaignMissionUnitRoleComponent>(e) ||
                            !em.HasComponent<UnitHealth>(e) || em.GetComponentData<UnitHealth>(e).Current<=0 || !em.HasComponent<UnitAttack>(e) || !em.HasComponent<LocalTransform>(e)) continue;
                        if(em.HasComponent<SelectedUnitTag>(e)) {selectedFriendly=true; friendly=e;}
                        float range=math.max(10,em.GetComponentData<UnitAttack>(e).Range-10);
                        if(!em.HasComponent<UnitPathRequest>(e) && !em.HasComponent<UnitPathFollow>(e) && math.distancesq(em.GetComponentData<LocalTransform>(e).Position.xz,fork.xz)<=range*range) positionedCount++;
                        holding|=em.HasComponent<HoldPositionOrderTag>(e);
                    }
                }
            }
            defense.ReinforcedRifleCount=math.max(defense.ReinforcedRifleCount,producedCount);
            positioned=positionedCount>=math.min(4,math.max(1,8-facts.SquadLossCount+producedCount));
            foreach(var requests in SystemAPI.Query<DynamicBuffer<BuildingRuntimeSpawnRequest>>())
                for(int i=0;i<requests.Length;i++)
                {
                    var r=requests[i];
                    built|=r.RequestId>defense.InitialProducerRequestId && r.Status==BuildingRuntimeSpawnRequest.Succeeded &&
                        r.HasOwnerFaction!=0 && r.FactionId==1 && (r.BuildingId.Equals(RadarTower) || r.BuildingId.Equals(RadarBarrier));
                }
            ObserveDefenseCommandResults(ref state,ref defense);
            var elements=em.GetBuffer<CampaignMissionConvoyElementState>(root,true);
            main=elements.Length>1 && elements[1].Activated!=0;
            var ledger=em.GetComponentData<ThreatWarningLedgerState>(root);
            bool imminent=false,mainConfirmed=false;
            var warnings=em.GetBuffer<ThreatWarningRecord>(root,true);
            for(int i=0;i<warnings.Length;i++)
            {imminent|=warnings[i].Resolved==0 && warnings[i].Critical!=0; mainConfirmed|=warnings[i].ElementIndex==1 && warnings[i].Source!=ThreatWarningSourceKind.ScoutReport;}
            uint oldMask=defense.AcknowledgedGuidanceMask;
            for(int i=0;i<definition.Defense.GuidanceSteps.Length;i++)
            {
                ref var step=ref definition.Defense.GuidanceSteps[i];
                bool done=step.Completion switch
                {
                    MissionGuidanceCompletionKind.WarningRead=>(defense.AcknowledgedGuidanceMask&1)!=0,
                    MissionGuidanceCompletionKind.RouteInspected=>defense.PlayerRequestedFocus!=0,
                    MissionGuidanceCompletionKind.DefenseBuilt=>built,
                    MissionGuidanceCompletionKind.SquadPositioned=>positioned,
                    MissionGuidanceCompletionKind.Holding=>holding,
                    MissionGuidanceCompletionKind.StopAccepted=>defense.StopAccepted!=0,
                    MissionGuidanceCompletionKind.PingAccepted=>defense.PingUsed!=0,
                    MissionGuidanceCompletionKind.ReinforcementProduced=>defense.ReinforcedRifleCount>=4,
                    MissionGuidanceCompletionKind.HostileDefeated=>facts.HostileDefeatedCount>0,
                    MissionGuidanceCompletionKind.MainElementActivated=>main && mainConfirmed && positioned,
                    MissionGuidanceCompletionKind.ResultSettled=>runtime.Outcome!=MissionOutcomeKind.None,
                    _=>false
                };
                if(done || imminent && (step.Optional!=0 || i==2)) defense.AcknowledgedGuidanceMask|=1u<<i;
            }
            int selected=-1;
            for(int i=0;i<definition.Defense.GuidanceSteps.Length-1;i++)
                if((defense.AcknowledgedGuidanceMask&(1u<<i))==0 && !(runtime.Guidance==NarrativeGuidanceMode.Contextual && i is 2 or 3 or 6 or 8)) { selected=i; break; }
            if(oldMask!=defense.AcknowledgedGuidanceMask && current.Active!=0 && current.Prompt>=CampaignMissionGuidancePromptKind.RadarReadWarning &&
                (defense.AcknowledgedGuidanceMask&(1u<<((int)current.Prompt-13)))!=0)
                defense.NextGuidanceAtMilliseconds=facts.ElapsedMilliseconds+3000;
            em.SetComponentData(root,defense);
            if(selected<0 || ledger.Active==0 || facts.ElapsedMilliseconds<defense.NextGuidanceAtMilliseconds)
            { ClearDefenseGuidance(em,root,in current); return true; }
            ref var chosen=ref definition.Defense.GuidanceSteps[selected];
            bool canExecute=chosen.Action is not (MissionGuidanceActionKind.Move or MissionGuidanceActionKind.Hold or MissionGuidanceActionKind.Stop) || selectedFriendly;
            if(chosen.Action==MissionGuidanceActionKind.RadarPing)
            {var ping=em.GetComponentData<RadarPingState>(root); canExecute=ping.Charges>0 && ping.PendingRequestId==0 && ping.ReadyAtMilliseconds<=facts.ElapsedMilliseconds && RadarPingRequestSystem.FindSensor(em,root)!=Entity.Null;}
            var next=new CampaignMissionGuidanceProjectionComponent
            {
                GuidanceId=45001+selected,Version=Next(current.Version),MissionSourceVersion=runtime.Version,
                Prompt=(CampaignMissionGuidancePromptKind)(13+selected),GuidanceMode=runtime.Guidance,Active=1,
                RecommendationKind=AssistantRecommendationKind.Explain,TargetKind=AssistantTargetKind.UiSurface,
                TargetId=chosen.StepId,Title=chosen.TitleKey,Body=chosen.BodyKey,
                ActionLabel=chosen.Action==MissionGuidanceActionKind.Explain || chosen.Action==MissionGuidanceActionKind.InspectContact ? RadarContinue : RadarAct,
                Priority=imminent ? AssistantMessagePriority.Critical : AssistantMessagePriority.High,
                SourceEntity=friendly,CanShow=1,CanExecute=canExecute ? (byte)1 : (byte)0,WorldPosition=fork,HasWorldPosition=1,
                SubtitlesEnabled=settings.SubtitlesEnabled,LargeTextEnabled=settings.LargeTextEnabled,HighContrastEnabled=settings.HighContrastEnabled
            };
            if(!ProjectionEquals(in current,in next) || current.Active==0 || current.Priority!=next.Priority ||
                !current.Title.Equals(next.Title) || !current.Body.Equals(next.Body) || current.MissionSourceVersion!=next.MissionSourceVersion)
                em.SetComponentData(root,next);
            return true;
        }

        private static void ClearDefenseGuidance(EntityManager em,Entity root,in CampaignMissionGuidanceProjectionComponent current)
        { if(current.Active!=0) em.SetComponentData(root,new CampaignMissionGuidanceProjectionComponent {Version=Next(current.Version)}); }

        private void ObserveDefenseCommandResults(ref SystemState state,ref CampaignMissionDefenseStateComponent defense)
        {
            foreach(var results in SystemAPI.Query<DynamicBuffer<RtsSelectionCommandResultElement>>())
                for(int i=0;i<results.Length;i++)
                {
                    var result=results[i];
                    if(result.RequestId<=defense.LastSelectionCommandRequestId) continue;
                    defense.LastSelectionCommandRequestId=result.RequestId;
                    if(defense.SelectionCommandBaselineSet!=0 && result.Accepted!=0 && result.HasCommandResult!=0 &&
                        result.Kind==RtsSelectionCommandIntentKind.Stop) defense.StopAccepted=1;
                }
            defense.SelectionCommandBaselineSet=1;
        }
    }
}
