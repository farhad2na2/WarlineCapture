using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    // Projects the authored clue through the existing speech arbiter; it owns no combat or reward facts.
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(AssistantMessagePrioritySystem))]
    public partial struct M03RadioReportProjectionSystem : ISystem
    {
        private const int MessageId=830003;
        private FixedString64Bytes session;
        private int attempt;
        private uint sourceVersion;
        private bool issued;
        private FixedString32Bytes locale;
        private EntityQuery boundaryQuery;
        public void OnCreate(ref SystemState state)
        {
            boundaryQuery=state.GetEntityQuery(typeof(UiShellStateComponent),typeof(AssistantMessageElement));
            state.RequireForUpdate(boundaryQuery);
        }
        public void OnUpdate(ref SystemState state)
        {
            if(boundaryQuery.CalculateEntityCount()!=1) return;
            Entity boundary=boundaryQuery.GetSingletonEntity();
            var messages=state.EntityManager.GetBuffer<AssistantMessageElement>(boundary);
            bool playing=SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) && gameplay.PlayRequested!=0;
            bool valid=SystemAPI.TryGetSingleton(out CampaignMissionRuntimeComponent runtime) &&
                runtime.MissionId.Equals("saga.ch01.m03.radar_warning") && runtime.Phase==MissionPhaseKind.Engage &&
                runtime.Outcome==MissionOutcomeKind.None && playing &&
                state.EntityManager.GetComponentData<UiShellStateComponent>(boundary).ActiveRoute==Game.UI.Contracts.UIRoute.Match;
            bool same=session.Equals(runtime.SessionToken) && attempt==runtime.AttemptOrdinal && sourceVersion==runtime.SourceVersion;
            if(!valid || !same)
            {
                for(int i=messages.Length-1;i>=0;i--) if(messages[i].MessageId==MessageId) messages.RemoveAt(i);
                issued=false; locale=default; session=runtime.SessionToken; attempt=runtime.AttemptOrdinal; sourceVersion=runtime.SourceVersion;
            }
            if(!valid || !SystemAPI.TryGetSingleton(out CampaignMissionAttemptFactsComponent facts) || facts.ElapsedMilliseconds<45000 ||
                gameplay.SimulationActive==0) return;
            string currentLocale=GameLocalization.CurrentLocaleCode;
            if(issued && locale.Equals(currentLocale)) return;
            var line=M03RadarWarningCopyCatalog.Comms[0];
            if(issued)
            {
                for(int i=0;i<messages.Length;i++) if(messages[i].MessageId==MessageId)
                {
                    var message=messages[i]; message.Text=new FixedString512Bytes(GameText.Get(line.Key,line.English));
                    message.AudioEventId=new FixedString64Bytes("vo.aria.m03.comms.01."+(currentLocale=="fa-IR" ? "fa" : "en")); messages[i]=message;
                }
                locale=new FixedString32Bytes(currentLocale); return;
            }
            float now=(float)SystemAPI.Time.ElapsedTime;
            messages.Add(new AssistantMessageElement
            {
                MessageId=MessageId,SourceVersion=System.Math.Max(1,unchecked((int)runtime.SourceVersion)),Priority=AssistantMessagePriority.Normal,
                RelatedKind=AssistantRecommendationKind.Explain,SuppressionKey="mission.m03.outage_report",
                Text=new FixedString512Bytes(GameText.Get(line.Key,line.English)),
                AudioEventId=new FixedString64Bytes("vo.aria.m03.comms.01."+(currentLocale=="fa-IR" ? "fa" : "en")),
                CreatedAt=now,ExpiresAt=now+30,RequiresNarration=1
            });
            issued=true; locale=new FixedString32Bytes(currentLocale);
        }
    }
}
