using System;using Game.UI.Runtime;using Game.UI.Contracts;namespace Game.Runtime{
// Bound to SourceProductionDeliveryOwner.TryQueue, which executes original TryReserve once.
internal delegate bool SourceCampQueueRequest(int producerId,int productionIndex,float now,out SourcePendingProduction pending);
internal sealed class SourceCampPrimaryResult{internal readonly bool Accepted;internal readonly string SourceReason;internal readonly SourcePendingProduction Pending;internal readonly BuildingUiCommandFailure? UiFailure;internal SourceCampPrimaryResult(bool accepted,string reason,SourcePendingProduction pending=null,BuildingUiCommandFailure? failure=null){Accepted=accepted;SourceReason=reason;Pending=pending;UiFailure=failure;}}
internal sealed partial class SourceCampAvailability{
 internal SourceCampPrimaryResult InvokePrimary(BuildDrawerCatalogItem selected,bool sourceHasSelection,CatalogPublicFacts facts,bool commandBindingKnown,SourceCampQueueRequest actualOriginalQueue,float currentSourceTime){
 // Exact original OnPrimaryActionClicked guards: invalid selection, unbound adapter,
 // then request callback. Semantic UI owns category/item navigation before this call.
 if(!sourceHasSelection||selected.Prefab==null)return new SourceCampPrimaryResult(false,"InvalidSelection",failure:BuildingUiCommandFailure.InvalidSelection);
 if(!commandBindingKnown)throw new NotSupportedException("Source primary command binding unknown");
 if(actualOriginalQueue==null)return new SourceCampPrimaryResult(false,"StillConnecting");
 if(float.IsNaN(currentSourceTime)||float.IsInfinity(currentSourceTime)||currentSourceTime<0)throw new NotSupportedException("Current source production time required");
 if(selected.Category==BuildDrawerCategory.Buildings)throw new NotSupportedException("Building primary transaction belongs to shared construction owner");
 var available=Read(selected,facts,true,true);if(available.Failure!=BuildingUiCommandSystemHelper.CampRequestFailure.None)return new SourceCampPrimaryResult(false,available.Failure.ToString(),failure:available.UiFailure);
 using var scope=new SourceCampReadScope(em,store,facts,map);var prefab=map.Command(selected.Prefab);
 if(!kernel.TrySelectSourcePrimaryProducer(context,prefab,out int producerId,out int productionIndex,out _))return new SourceCampPrimaryResult(false,"OriginalProducerSelectionRejected");
 bool accepted=actualOriginalQueue(producerId,productionIndex,currentSourceTime,out var pending);
 if(!accepted)return new SourceCampPrimaryResult(false,"OriginalQueueReturnedFalse");
 if(pending==null||pending.ReceiptId==0||pending.ReceiptOwner!=session)throw new InvalidOperationException("Accepted source queue omitted its actual paid pending receipt");
 return new SourceCampPrimaryResult(true,"OriginalQueueAccepted",pending);
 }
}
// One semantic contact consumes one invocation; retry is not a second source click.
internal sealed class SourceCampPrimaryContact{readonly SourceCampAvailability owner;readonly BuildDrawerCatalogItem item;readonly bool selection;bool consumed;internal SourceCampPrimaryContact(SourceCampAvailability owner,BuildDrawerCatalogItem item,bool hasSelection){this.owner=owner;this.item=item;selection=hasSelection;}internal SourceCampPrimaryResult Complete(CatalogPublicFacts facts,bool bindingKnown,SourceCampQueueRequest actualQueue,float now){if(consumed)throw new NotSupportedException("Primary semantic contact already consumed");consumed=true;return owner.InvokePrimary(item,selection,facts,bindingKnown,actualQueue,now);}}
}