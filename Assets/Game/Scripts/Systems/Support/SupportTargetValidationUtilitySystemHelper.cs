using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Runtime
{
    public static class SupportTargetValidationUtilitySystemHelper
    {
        public static byte Mask(SupportAbilityKind kind) => kind>=SupportAbilityKind.Smoke && kind<=SupportAbilityKind.Supply ? (byte)(1 << ((int)kind-1)) : (byte)0;
        public static bool TryDefinition(in SupportCatalogComponent catalog, SupportAbilityKind kind, out SupportAbilityDefinition definition)
        {
            definition=default;
            if(!catalog.Blob.IsCreated) return false;
            ref var entries=ref catalog.Blob.Value.Abilities;
            for(int i=0;i<entries.Length;i++) if(entries[i].Kind==kind) { definition=entries[i]; return true; }
            return false;
        }
        public static SupportRejectionReason Validate(EntityManager em, Entity root, in SupportRequestElement request, bool checkConsent=true)
        {
            var session=em.GetComponentData<SupportSessionComponent>(root);
            if(request.Source!=SupportRequestSource.Player && request.Source!=SupportRequestSource.Aria) return SupportRejectionReason.InvalidTargetType;
            if(!request.SessionToken.Equals(session.SessionToken) || request.AttemptOrdinal!=session.AttemptOrdinal) return SupportRejectionReason.WrongAttempt;
            if(session.Active==0) return SupportRejectionReason.NotActive;
            var policy=em.GetComponentData<SupportMissionPolicyComponent>(root);
            byte mask=Mask(request.Kind);
            if(mask==0 || (policy.AllowedMask & mask)==0) return SupportRejectionReason.MissionRestricted;
            if(((policy.OwnedMask | policy.TestGrantMask) & mask)==0) return SupportRejectionReason.NotUnlocked;
            var catalog=em.GetComponentData<SupportCatalogComponent>(root);
            if(catalog.Revision!=session.CatalogRevision || !TryDefinition(catalog,request.Kind,out var definition)) return SupportRejectionReason.NotReady;
            if(definition.ProductionReady==0 && !(session.TestEncounter!=0 && (policy.TestGrantMask & mask)!=0)) return SupportRejectionReason.NotReady;
            var abilities=em.GetBuffer<SupportAbilityStateElement>(root);
            SupportAbilityStateElement ability=default; bool found=false;
            for(int i=0;i<abilities.Length;i++) if(abilities[i].Kind==request.Kind) { ability=abilities[i]; found=true; break; }
            if(!found || ability.Enabled==0) return SupportRejectionReason.NotReady;
            if(ability.StateVersion!=request.ExpectedAbilityVersion || request.PreviewId==0) return SupportRejectionReason.StalePreview;
            if(ability.ChargesRemaining<=0) return SupportRejectionReason.NoCharges;
            if(session.SimulationSeconds<ability.CooldownUntil) return SupportRejectionReason.Cooldown;
            var groundReason=ValidateGround(em,root,request.Position);if(groundReason!=SupportRejectionReason.None)return groundReason;
            if(request.Kind==SupportAbilityKind.Smoke)
            {if(request.TargetKind!=SupportTargetKind.Ground || request.Target!=Entity.Null)return SupportRejectionReason.InvalidTargetType;}
            else if(request.Kind==SupportAbilityKind.Strike)
            {
                var targetReason=SupportAirValidationUtilitySystemHelper.ValidateStrikeTarget(em,session,request);
                if(targetReason!=SupportRejectionReason.None)return targetReason;
                if(!SupportAirValidationUtilitySystemHelper.SafeRoute(em,root,session,out _))return SupportRejectionReason.NoSafeAirRoute;
                if(SupportAirValidationUtilitySystemHelper.ResolveAircraftPrefab(em,definition.AircraftSourceKey)==Entity.Null)return SupportRejectionReason.NotReady;
            }
            else if(request.Kind==SupportAbilityKind.Paratroopers)
            {
                if(!SupportAirValidationUtilitySystemHelper.SafeRoute(em,root,session,out _,true,request.Position))return SupportRejectionReason.NoSafeAirRoute;
                if(SupportAirValidationUtilitySystemHelper.ResolveAircraftPrefab(em,definition.AircraftSourceKey)==Entity.Null)return SupportRejectionReason.NotReady;
                using var cells=new Unity.Collections.NativeList<int2>(Unity.Collections.Allocator.Temp);
                using var positions=new Unity.Collections.NativeList<float3>(Unity.Collections.Allocator.Temp);
                var landing=SupportLandingUtilitySystemHelper.PlanParatroopers(em,root,request,cells,positions);if(landing!=SupportRejectionReason.None)return landing;
            }
            else if(request.Kind==SupportAbilityKind.Supply)
            {
                if(definition.Materials!=40)return SupportRejectionReason.NotReady;
                if(!SupportAirValidationUtilitySystemHelper.SafeRoute(em,root,session,out _,true,request.Position))return SupportRejectionReason.NoSafeAirRoute;
                if(SupportAirValidationUtilitySystemHelper.ResolveAircraftPrefab(em,definition.AircraftSourceKey)==Entity.Null)return SupportRejectionReason.NotReady;
                var landing=SupportSupplyAdapterSystemHelper.Plan(em,root,request,out _,out _);if(landing!=SupportRejectionReason.None)return landing;
            }
            else return SupportRejectionReason.NotReady;
            if(SupportFuelTransactionSystem.Available(em,session.FactionId)<definition.FuelCost) return SupportRejectionReason.InsufficientFuel;
            if(checkConsent && request.Source==SupportRequestSource.Aria)
            {
                if(!em.HasComponent<SupportProposalComponent>(root)) return SupportRejectionReason.ConsentRequired;
                var proposal=em.GetComponentData<SupportProposalComponent>(root);
                if(request.ProposalId==0 || request.ConsentVersion==0 || proposal.Consumed!=0 || proposal.Declined!=0 || proposal.Approved==0 || proposal.ProposalId!=request.ProposalId ||
                   proposal.ConsentVersion!=request.ConsentVersion) return SupportRejectionReason.ConsentRequired;
                if(session.SimulationSeconds>=proposal.ExpiresAt) return SupportRejectionReason.ConsentExpired;
                if(proposal.CatalogRevision!=catalog.Revision || proposal.FuelCost!=definition.FuelCost || !SameAction(proposal.Request,request)) return SupportRejectionReason.StalePreview;
            }
            return SupportRejectionReason.None;
        }
        public static SupportRejectionReason ValidateGround(EntityManager em,Entity root,float3 position)
        {
            var policy=em.GetComponentData<SupportMissionPolicyComponent>(root);
            if(!math.all(math.isfinite(position)) || math.any(position.xz<policy.GroundMin) || math.any(position.xz>policy.GroundMax))return SupportRejectionReason.InvalidGround;
            bool visible=false;
            foreach(var region in em.GetBuffer<SupportGroundRegionElement>(root,true))
            {
                if(math.any(position.xz<region.Min)||math.any(position.xz>region.Max))continue;
                if(region.Protected!=0)return SupportRejectionReason.ProtectedTarget;
                visible|=region.Visible!=0;
            }
            return visible?SupportRejectionReason.None:SupportRejectionReason.NotVisible;
        }
        public static bool SameAction(in SupportRequestElement a, in SupportRequestElement b) =>
            a.SessionToken.Equals(b.SessionToken) && a.AttemptOrdinal==b.AttemptOrdinal && a.Kind==b.Kind && a.Target==b.Target &&
            a.TargetKind==b.TargetKind && math.all(a.Position==b.Position) && math.all(a.Cell==b.Cell) &&
            a.ExpectedAbilityVersion==b.ExpectedAbilityVersion && a.PreviewId==b.PreviewId && a.TargetKnowledgeVersion==b.TargetKnowledgeVersion;
    }
}
