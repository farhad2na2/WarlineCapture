using System;
using System.Collections.Generic;
using Game.Runtime;
using UnityEngine;

namespace Game.Editor
{
    public enum S005GuardReceiptState { Waiting,Accepted,Retry }
    public sealed class S005GuardMoveReceipt
    {
        public string Attempt;
        public uint Group,CompletedBefore;
        public int DispatchFrame,LastRequest;
        public double Deadline;
        public Vector2 Screen;
        public Rect QaEnvelope;
        public AriaMoveExecutionEvidence.Member[] Members;
        public AriaMoveExecutionEvidence.Row Actual;
        public uint VerifiedReceipt;
        public string Failure;
        public S005GuardReceiptState Evaluate(uint completed,bool busy,double now)
        {
            if(now>Deadline)return Reject("No exact owned full-member normal Move receipt before the bounded deadline");
            if(Actual!=null)
            {
                var row=Actual;
                if(row.receipt!=0&&row.receipt==VerifiedReceipt&&row.requestId>LastRequest&&row.requestFrame>=DispatchFrame&&
                    row.frame>=row.requestFrame&&row.frame-row.requestFrame<=2&&Vector2.Distance(row.screen,Screen)<=2&&
                    row.attempt==Attempt&&row.group==Group&&!busy&&completed==CompletedBefore+1)
                {
                    if(!row.accepted||!row.completeGroup||!string.IsNullOrEmpty(row.error))return Reject("Native Move did not accept every selected current member");
                    if(!Inside(row.marker))return Reject("Native marker outside the declared QA defense envelope");
                    var ids=new Dictionary<string,AriaMoveExecutionEvidence.Member>(StringComparer.Ordinal);
                    foreach(var expected in Members)if(ids.ContainsKey(expected.stableId))return Reject("Duplicate original stable member");else ids.Add(expected.stableId,expected);
                    if(row.members.Length!=Members.Length)return Reject("Current full group member count changed");
                    foreach(var member in row.members)
                        if(member.entityIndex<=0||member.entityVersion<=0||!ids.TryGetValue(member.stableId,out var expected)||
                            member.entityIndex!=expected.entityIndex||member.entityVersion!=expected.entityVersion||!Inside(member.goalWorld)||!ids.Remove(member.stableId))
                            return Reject("Native member identity/goal outside the declared QA defense envelope");
                    if(ids.Count!=0)return Reject("Original selected member missing from accepted group");
                    return S005GuardReceiptState.Accepted;
                }
                // An unrelated native request is retained as raw evidence. It
                // cannot satisfy this exact owned contact or reset its timeout.
            }
            return S005GuardReceiptState.Waiting;
        }
        bool Inside(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)&&p.x>=QaEnvelope.xMin&&p.x<=QaEnvelope.xMax&&p.z>=QaEnvelope.yMin&&p.z<=QaEnvelope.yMax;
        S005GuardReceiptState Reject(string reason){Failure=reason;return S005GuardReceiptState.Retry;}
    }
}
