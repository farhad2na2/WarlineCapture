using System;
using System.Collections.Generic;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime {
 internal readonly struct SourceAriaRecruitmentOffer {
  internal readonly bool Present,CanRequest;internal readonly string Reason;internal readonly SourceUnitPrefab Prefab;internal readonly int MaterialsFallback;
  internal SourceAriaRecruitmentOffer(bool present,bool can,string reason,SourceUnitPrefab prefab,int materialsFallback){Present=present;CanRequest=can;Reason=reason;Prefab=prefab;MaterialsFallback=materialsFallback;}
 }
 // Resolves a role from current gameplay identities and delegates legality/payment to the original owners.
 internal sealed class SourceAriaPaidRecruitment : SourceAriaRecruitmentBoundary {
  readonly EntityManager em;readonly Entity session;readonly SourceCampAvailability availability;readonly SourceProductionDeliveryOwner delivery;
  internal SourceAriaPaidRecruitment(EntityManager em,Entity session,SourceCampAvailability availability,SourceProductionDeliveryOwner delivery):base(em,session){
   this.em=em??throw new ArgumentNullException(nameof(em));this.session=session;this.availability=availability??throw new ArgumentNullException(nameof(availability));this.delivery=delivery??throw new ArgumentNullException(nameof(delivery));
   if(!ReferenceEquals(delivery.World.Em,em)||!availability.AriaSameWorld(em,session,delivery.World.Buildings))throw new InvalidOperationException("Same source EM/session/live producer registry required");
  }
  SourceAriaRecruitmentOffer ReadOffer(SkirmishRoleKind role,bool evaluateAvailability=true){
   if(!availability.TryResolveRoleUnit(role,out var prefab,out int materials))return new SourceAriaRecruitmentOffer(false,false,"ActualSourceCatalogRoleAbsent",null,0);
   if(!evaluateAvailability)return new SourceAriaRecruitmentOffer(true,false,"Availability evaluated by shared request owner",prefab,materials);
   var result=availability.ReadUnitSemantic(prefab,materials);return new SourceAriaRecruitmentOffer(true,result.PrimaryInteractable,result.Failure.ToString(),prefab,materials);
  }
  internal override SourceAriaRecruitmentSurface Read(SkirmishRoleKind role){var offer=ReadOffer(role);return new SourceAriaRecruitmentSurface(offer.Present,offer.CanRequest,offer.Reason);}
  internal override SourceAriaRecruitmentReceipt Request(SkirmishRoleKind role,float actualProductionSourceTime){
   // Resolve again at dispatch; the actual source catalog/recipe identity can change after a read.
   var offer=ReadOffer(role,false);if(!offer.Present)return new SourceAriaRecruitmentReceipt(false,offer.Reason,null);
   var result=availability.RequestUnitSemantic(offer.Prefab,offer.MaterialsFallback,delivery.TryQueue,actualProductionSourceTime);return new SourceAriaRecruitmentReceipt(result.Accepted,result.SourceReason,result.Pending?.ReceiptId);
  }
 }
 internal sealed partial class SourceCampAvailability {
  internal bool AriaSameWorld(EntityManager candidate,Entity actualSession,IReadOnlyDictionary<int,RuntimeBuildingEntity> actualRegistry)=>ReferenceEquals(em,candidate)&&session==actualSession&&ReferenceEquals(buildings,actualRegistry);
 }
}
