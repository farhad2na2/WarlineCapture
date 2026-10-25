using System;using Unity.Entities;using Game.Skirmish.Contracts;
namespace Game.Runtime {
 internal readonly struct SourceAriaRecruitmentSurface {
  internal readonly bool Present, CanRequest; internal readonly string Reason;
  internal SourceAriaRecruitmentSurface(bool present, bool canRequest, string reason) { Present=present;CanRequest=canRequest;Reason=reason; }
 }
 internal readonly struct SourceAriaRecruitmentReceipt {
  internal readonly bool Accepted; internal readonly string SourceReason; internal readonly uint? ReceiptId;
  internal SourceAriaRecruitmentReceipt(bool accepted, string reason, uint? receiptId) { Accepted=accepted;SourceReason=reason;ReceiptId=receiptId; }
 }
 // Data-only transport boundary. The concrete source adapter binds the actual paid owner;
 // absent context is not an offered control and never reports enqueue or delivery success.
 internal abstract class SourceAriaRecruitmentBoundary {
  internal readonly object WorldIdentity;internal readonly Entity SessionIdentity;
  protected SourceAriaRecruitmentBoundary(object world,Entity session){WorldIdentity=world??throw new ArgumentNullException(nameof(world));SessionIdentity=session;}

  internal abstract SourceAriaRecruitmentSurface Read(SkirmishRoleKind role);
  internal abstract SourceAriaRecruitmentReceipt Request(SkirmishRoleKind role, float actualProductionSourceTime);
 }
}
