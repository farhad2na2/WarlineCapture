using Game.Editor;
using NUnit.Framework;
namespace Game.Tests.Editor
{
    public sealed class S005ObservedSquadPageReceiptTests
    {
        public static void RunFocusedValidation()
        {var t=new S005ObservedSquadPageReceiptTests();for(int i=0;i<9;i++)t.ActualCursorAndContactAreRequired(i);UnityEngine.Debug.Log("[S005ObservedSquadPageReceipt] result=Passed controls=9 nativeMissionPending=1");}
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)]
        public void ActualCursorAndContactAreRequired(int mutation)
        {
            var r=new S005ObservedSquadPageReceipt();r.Dispatch(0,17,10,0);
            var state=mutation switch{
                0=>r.Observe(0,0,17,false,10,1),1=>r.Observe(1,1,17,true,11,1),
                2=>r.Observe(0,0,17,false,11,1),3=>r.Observe(1,1,17,false,11,1),
                4=>r.Observe(1,0,17,false,11,1),5=>r.Observe(1,1,18,false,11,1),
                6=>r.Observe(2,2,17,false,11,1),7=>r.Observe(0,0,17,false,11,1),
                _=>r.Observe(0,0,17,false,10,13)};
            if(mutation==7){r.Dispatch(0,17,11,2);r.Observe(0,0,17,false,12,3);r.Dispatch(0,17,12,4);state=r.Observe(0,0,17,false,13,5);}
            var expected=mutation==3?S005SquadPageReceiptState.Advanced:mutation==2?S005SquadPageReceiptState.Ready:
                mutation>=5?S005SquadPageReceiptState.Failed:S005SquadPageReceiptState.Waiting;
            Assert.That(state,Is.EqualTo(expected));
            if(mutation==3)Assert.That(r.IsPending,Is.False);
        }
    }
}
