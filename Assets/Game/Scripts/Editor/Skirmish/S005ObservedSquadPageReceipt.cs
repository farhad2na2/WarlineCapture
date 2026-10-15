using System;
namespace Game.Editor
{
    public enum S005SquadPageReceiptState { Ready,Waiting,Advanced,Failed }
    public sealed class S005ObservedSquadPageReceipt
    {
        public string Failure {get;private set;}
        public bool IsPending=>pending;
        bool pending;int originalPage,attempts;uint roster,completedBefore;double issuedAt;
        public void Reset(){pending=false;attempts=0;Failure=null;}
        public void Dispatch(int page,uint revision,uint completed,double now)
        {if(pending)throw new InvalidOperationException("Squad swipe already pending");pending=true;originalPage=page;roster=revision;completedBefore=completed;issuedAt=now;attempts++;}
        public S005SquadPageReceiptState Observe(int actual,int rendered,uint revision,bool busy,uint completed,double now)
        {
            if(Failure!=null)return S005SquadPageReceiptState.Failed;
            if(!pending)return actual==rendered?S005SquadPageReceiptState.Ready:S005SquadPageReceiptState.Waiting;
            if(now-issuedAt>12){Failure="Normal swipe contact/page receipt timed out.";return S005SquadPageReceiptState.Failed;}
            if(busy||completed<=completedBefore||actual!=rendered)return S005SquadPageReceiptState.Waiting;
            // Roster churn cannot be credited as a successful navigation receipt.
            if(revision!=roster){Failure="Own roster changed during the page swipe.";return S005SquadPageReceiptState.Failed;}
            if(actual==originalPage+1){pending=false;attempts=0;return S005SquadPageReceiptState.Advanced;}
            if(actual!=originalPage){Failure="Normal swipe moved to an unexpected page.";return S005SquadPageReceiptState.Failed;}
            pending=false;
            if(attempts>=3){Failure="Three completed normal swipes did not advance the public page.";return S005SquadPageReceiptState.Failed;}
            return S005SquadPageReceiptState.Ready;
        }
    }
}
