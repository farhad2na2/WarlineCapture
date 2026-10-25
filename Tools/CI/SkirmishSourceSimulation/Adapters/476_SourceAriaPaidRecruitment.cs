using System;using System.Collections.Generic;using Game.Components;using Game.Skirmish.Contracts;using Game.UI.Contracts;using Game.UI.Runtime;using Unity.Entities;
namespace Game.Runtime {
 internal readonly struct SourceAriaRecruitmentOffer {
  internal readonly bool Present,CanRequest;internal readonly string Reason;internal readonly BuildDrawerCatalogItem Item;internal readonly CatalogPublicFacts Facts;
  internal SourceAriaRecruitmentOffer(bool present,bool can,string reason,BuildDrawerCatalogItem item,CatalogPublicFacts facts){Present=present;CanRequest=can;Reason=reason;Item=item;Facts=facts;}
 }
 // A typed source catalog/availability/paid owner binding, not an acceptance callback.
 internal sealed class SourceAriaPaidRecruitment : SourceAriaRecruitmentBoundary {
  readonly EntityManager em;readonly Entity session;readonly SourceCampAvailability availability;readonly SourceCatalogRows rows;readonly SourceProductionDeliveryOwner delivery;readonly Func<CatalogPublicFacts> sourceFacts;
  internal SourceAriaPaidRecruitment(EntityManager em,Entity session,SourceCampAvailability availability,SourceCatalogRows rows,SourceProductionDeliveryOwner delivery,Func<CatalogPublicFacts> sourceFacts):base(em,session){
   this.em=em??throw new ArgumentNullException(nameof(em));this.session=session;this.availability=availability??throw new ArgumentNullException(nameof(availability));this.rows=rows??throw new ArgumentNullException(nameof(rows));this.delivery=delivery??throw new ArgumentNullException(nameof(delivery));this.sourceFacts=sourceFacts??throw new ArgumentNullException(nameof(sourceFacts));
   if(!ReferenceEquals(delivery.World.Em,em)||!availability.AriaSameWorld(em,session,delivery.World.Buildings))throw new InvalidOperationException("Same source EM/session/live producer registry required");
  }
  SourceAriaRecruitmentOffer ReadOffer(SkirmishRoleKind role){
   var facts=sourceFacts();var catalog=em.GetComponentObject<SkirmishProductionCatalogRecord>(session).Catalog;BuildDrawerCatalogItem chosen=default;bool present=false;
   foreach(var category in new[]{BuildDrawerCategory.Vehicles,BuildDrawerCategory.Aircrafts,BuildDrawerCategory.Soldiers})foreach(var row in rows.Read(category,facts)){
    if(row.Prefab==null||!catalog.TryRole(row.Prefab.name,out var overlay)||overlay.RoleKind!=role)continue;
    if(present)throw new NotSupportedException("Ambiguous source role catalog identity");chosen=row;present=true;
   }
   if(!present)return new SourceAriaRecruitmentOffer(false,false,"ActualSourceCatalogRoleAbsent",default,facts);
   var result=availability.Read(chosen,facts,true,true);return new SourceAriaRecruitmentOffer(true,result.PrimaryInteractable,result.Failure.ToString(),chosen,facts);
  }
  internal override SourceAriaRecruitmentSurface Read(SkirmishRoleKind role){var offer=ReadOffer(role);return new SourceAriaRecruitmentSurface(offer.Present,offer.CanRequest,offer.Reason);}
  internal override SourceAriaRecruitmentReceipt Request(SkirmishRoleKind role,float actualProductionSourceTime){
   // Re-read actual rows/availability at dispatch, never reuse a stale proposal.
   var offer=ReadOffer(role);if(!offer.Present)return new SourceAriaRecruitmentReceipt(false,offer.Reason,null);
   var contact=new SourceCampPrimaryContact(availability,offer.Item,true);var result=contact.Complete(offer.Facts,true,delivery.TryQueue,actualProductionSourceTime);return new SourceAriaRecruitmentReceipt(result.Accepted,result.SourceReason,result.Pending?.ReceiptId);
  }
 }
 internal sealed partial class SourceCampAvailability {
  internal bool AriaSameWorld(EntityManager candidate,Entity actualSession,IReadOnlyDictionary<int,RuntimeBuildingEntity> actualRegistry)=>ReferenceEquals(em,candidate)&&session==actualSession&&ReferenceEquals(buildings,actualRegistry);
 }
}
