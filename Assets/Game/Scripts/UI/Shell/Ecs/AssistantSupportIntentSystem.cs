using Game.Components;
using Game.Runtime;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(AssistantCommandIntentSystem))]
    public partial struct AssistantSupportIntentSystem : ISystem
    {
        public void OnCreate(ref SystemState state)=>state.RequireForUpdate<SupportSessionComponent>();
        public void OnUpdate(ref SystemState state)
        {
            if(!SystemAPI.TryGetSingletonEntity<SupportSessionComponent>(out var root))return;
            var em=state.EntityManager;
            foreach(var (requests,results) in SystemAPI.Query<DynamicBuffer<AssistantCommandIntentRequestElement>,DynamicBuffer<AssistantCommandIntentResultElement>>())
            {
                for(int i=requests.Length-1;i>=0;i--)
                {
                    var request=requests[i];
                    if(request.Kind==AssistantCommandIntentKind.StopAssistantControl)
                    {var p=em.GetComponentData<SupportProposalComponent>(root);p.Declined=p.Consumed=1;em.SetComponentData(root,p);continue;}
                    if(request.Kind<AssistantCommandIntentKind.ProposeSupport || request.Kind>AssistantCommandIntentKind.ShowSupportTarget)continue;
                    var reason=Process(em,root,request);requests.RemoveAt(i);
                    if(results.Length>=16)results.RemoveAt(0);
                    results.Add(new AssistantCommandIntentResultElement {RequestId=request.RequestId,Kind=request.Kind,
                        Status=reason==0?AssistantCommandIntentStatus.Accepted:AssistantCommandIntentStatus.Rejected,ReasonCode=(int)reason,
                        TargetEntity=request.TargetEntity,WorldPosition=request.WorldPosition});
                }
            }
        }
        public static SupportRejectionReason Process(EntityManager em,Entity root,in AssistantCommandIntentRequestElement intent)
        {
            var session=em.GetComponentData<SupportSessionComponent>(root);
            if(!intent.SupportSessionToken.Equals(session.SessionToken) || intent.SupportAttemptOrdinal!=session.AttemptOrdinal)return SupportRejectionReason.WrongAttempt;
            var preview=em.GetComponentData<SupportPreviewComponent>(root);
            var p=em.GetComponentData<SupportProposalComponent>(root);
            if(intent.Kind==AssistantCommandIntentKind.DeclineSupport)
            {
                if(p.ProposalId!=intent.SupportProposalId || p.ConsentVersion!=intent.SupportConsentVersion)return SupportRejectionReason.StalePreview;
                p.Declined=p.Consumed=1;em.SetComponentData(root,p);return SupportRejectionReason.None;
            }
            if(intent.Kind==AssistantCommandIntentKind.ShowSupportTarget)
            {
                if(preview.PreviewId!=intent.SupportPreviewId || preview.Request.Target!=intent.TargetEntity || !math.all(preview.Request.Position==intent.WorldPosition))return SupportRejectionReason.StalePreview;
                using var camera=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
                if(camera.CalculateEntityCount()!=1)return SupportRejectionReason.NotReady;
                em.SetComponentData(camera.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent {Requested=1,Smooth=1,World=preview.Request.Position,SmoothTimeSeconds=.25f});return SupportRejectionReason.None;
            }
            if(intent.Kind==AssistantCommandIntentKind.ProposeSupport)
            {
                if(intent.SupportProposalId==0 || intent.SupportConsentVersion==0 || preview.Valid==0 || intent.SupportKind!=preview.Request.Kind || intent.SupportPreviewId!=preview.PreviewId ||
                    intent.SupportAbilityVersion!=preview.Request.ExpectedAbilityVersion || intent.SupportCatalogRevision!=session.CatalogRevision ||
                    intent.SupportFuelCost!=preview.FuelCost || intent.TargetEntity!=preview.Request.Target || !math.all(intent.WorldPosition==preview.Request.Position))return SupportRejectionReason.StalePreview;
                if(p.Declined!=0 && SupportTargetValidationUtilitySystemHelper.SameAction(p.Request,preview.Request))return SupportRejectionReason.ConsentRequired;
                em.SetComponentData(root,new SupportProposalComponent {ProposalId=intent.SupportProposalId,ConsentVersion=intent.SupportConsentVersion,
                    Request=preview.Request,CatalogRevision=session.CatalogRevision,FuelCost=preview.FuelCost,ExpiresAt=session.SimulationSeconds+15});return SupportRejectionReason.None;
            }
            if(intent.Kind!=AssistantCommandIntentKind.ApproveSupport)return SupportRejectionReason.ConsentRequired;
            if(em.GetComponentData<SupportInputStateComponent>(root).Phase==4)return SupportRejectionReason.AlreadyProcessed;
            if(p.ProposalId==0 || p.ProposalId!=intent.SupportProposalId || p.ConsentVersion!=intent.SupportConsentVersion || p.Consumed!=0 || p.Declined!=0)return SupportRejectionReason.ConsentRequired;
            if(session.SimulationSeconds>=p.ExpiresAt)return SupportRejectionReason.ConsentExpired;
            var request=p.Request;request.Source=SupportRequestSource.Aria;request.ProposalId=p.ProposalId;request.ConsentVersion=p.ConsentVersion;p.Approved=1;
            em.SetComponentData(root,p);
            var reason=SupportTargetValidationUtilitySystemHelper.Validate(em,root,request);
            if(reason!=0){p.Approved=0;em.SetComponentData(root,p);return reason;}
            request.RequestId=session.NextRequestId++;em.SetComponentData(root,session);em.GetBuffer<SupportRequestElement>(root).Add(request);
            // Mark queued, preventing double approval before the next simulation tick.
            var input=em.GetComponentData<SupportInputStateComponent>(root);input.Phase=4;input.Version++;em.SetComponentData(root,input);
            return SupportRejectionReason.None;
        }
    }
}
