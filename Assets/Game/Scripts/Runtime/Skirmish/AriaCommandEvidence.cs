using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    /// <summary>
    /// Optional acceptance recorder. Receipts follow accepted touch releases into
    /// queued requests; a later command needs that request's explicit scope.
    /// This observes execution and never grants permission or changes gameplay.
    /// </summary>
    public static class AriaCommandEvidence
    {
        private static readonly HashSet<uint> issued = new();
        private static readonly Dictionary<uint,SkirmishPublicArmyOrderGoal> publicGoals = new();
        private static SkirmishPublicArmyOrderGoal currentPublicGoal;
        public static SkirmishPublicArmyOrderGoal CurrentPublicGoal => currentPublicGoal;
        public const int MaximumLiveGoalReceipts=64;
        public static void RetirePublicGoals(string sessionId,ulong worldSequenceNumber)
        {
            var remove=new List<uint>();foreach(var pair in publicGoals)if(pair.Value.SessionId==sessionId&&pair.Value.WorldSequenceNumber==worldSequenceNumber)remove.Add(pair.Key);
            foreach(var id in remove){publicGoals.Remove(id);issued.Remove(id);if(pendingReceipt==id)pendingReceipt=0;}
            if(currentPublicGoal.SessionId==sessionId&&currentPublicGoal.WorldSequenceNumber==worldSequenceNumber)currentPublicGoal=default;
        }
        public static void RetainCurrentPublicSession(ulong worldSequenceNumber,string currentSession)
        {
            var remove=new List<uint>();foreach(var pair in publicGoals)if(pair.Value.WorldSequenceNumber==worldSequenceNumber&&pair.Value.SessionId!=currentSession)remove.Add(pair.Key);
            foreach(var id in remove){publicGoals.Remove(id);issued.Remove(id);if(pendingReceipt==id)pendingReceipt=0;}
            if(currentPublicGoal.WorldSequenceNumber==worldSequenceNumber&&currentPublicGoal.SessionId!=currentSession)currentPublicGoal=default;
        }
        static void BoundPublicGoals()
        {
            while(publicGoals.Count>MaximumLiveGoalReceipts){uint oldest=uint.MaxValue;foreach(var id in publicGoals.Keys)if(id<oldest)oldest=id;publicGoals.Remove(oldest);issued.Remove(oldest);if(pendingReceipt==oldest)pendingReceipt=0;}
        }
        private static uint nextReceipt, pendingReceipt, currentReceipt;
        private static int releaseFrame;
        private static Vector2 releasePosition;
        public static bool Active { get; private set; }
        public static int CompletedTouches { get; private set; }
        public static int AcceptedCommands { get; private set; }
        public static int Violations { get; private set; }
        public static event Action<string, uint, bool> Recorded;

        public static bool TryReadAudit(uint completedTouches, uint unexpectedSamples, out int violations)
        {
            violations = -1;
            // The recorder must span the same completed gesture stream as the
            // live driver. A stopped/missing observer or an empty command stream
            // cannot be promoted to a measured clean run.
            if (!Active || completedTouches == 0 || CompletedTouches != completedTouches || AcceptedCommands == 0 ||
                unexpectedSamples > int.MaxValue - Violations) return false;
            violations = Violations + (int)unexpectedSamples;
            return true;
        }

        public static void Begin()
        {
            Active = true;
            pendingReceipt = currentReceipt = 0;
            CompletedTouches = AcceptedCommands = Violations = 0;
            issued.Clear();
            publicGoals.Clear(); currentPublicGoal=default;
        }

        public static void End()
        {
            Active = false;
            pendingReceipt = currentReceipt = 0;
            issued.Clear();
            publicGoals.Clear(); currentPublicGoal=default;
        }

        public static void ObserveRelease(int frame, Vector2 position) => ObserveRelease(frame,position,default);
        public static void ObserveRelease(int frame, Vector2 position, in SkirmishPublicArmyOrderGoal goal)
        {
            if(pendingReceipt!=0)publicGoals.Remove(pendingReceipt);
            pendingReceipt=0;
            if (!Active && !goal.Known) return;
            releaseFrame = frame;
            releasePosition = position;
            pendingReceipt = checked(++nextReceipt);
            if(goal.Known){publicGoals[pendingReceipt]=goal;BoundPublicGoals();}
            if(Active)CompletedTouches++;
        }

        public static uint ClaimRelease(int frame, Vector2? position = null)
        {
            if(pendingReceipt!=0&&frame>=releaseFrame&&frame-releaseFrame>2){publicGoals.Remove(pendingReceipt);pendingReceipt=0;}
            if (pendingReceipt == 0 || frame < releaseFrame || frame - releaseFrame > 2 ||
                (position.HasValue && Vector2.SqrMagnitude(position.Value - releasePosition) > 4f)) return 0;
            uint receipt = pendingReceipt;
            pendingReceipt = 0;
            issued.Add(receipt);
            return receipt;
        }

        public static Scope Enter(uint receipt) => new(receipt);
        // Logical CLI dispatch has its own explicit request/receipt domain.
        public static LogicalGoalScope EnterLogicalGoal(string requestId,in SkirmishPublicArmyOrderGoal goal)=>new(requestId,goal);
        public readonly struct LogicalGoalScope:IDisposable
        {
            readonly SkirmishPublicArmyOrderGoal previous;
            internal LogicalGoalScope(string requestId,SkirmishPublicArmyOrderGoal goal){if(string.IsNullOrEmpty(requestId))throw new ArgumentException("Actual logical dispatch request required");previous=currentPublicGoal;currentPublicGoal=goal;}
            public void Dispose(){currentPublicGoal=previous;}
        }

        public static void Accepted(string operation, byte faction)
        {
            if (!Active || faction != 1) return;
            AcceptedCommands++;
            bool verified = currentReceipt != 0;
            if (!verified) Violations++;
            Recorded?.Invoke(operation, currentReceipt, verified);
        }

        public readonly struct Scope : IDisposable
        {
            private readonly uint previous;
            private readonly SkirmishPublicArmyOrderGoal previousGoal;
            internal Scope(uint receipt)
            {
                previous = currentReceipt;previousGoal=currentPublicGoal;
                currentReceipt = receipt != 0 && issued.Remove(receipt) ? receipt : 0;
                currentPublicGoal=default;
                if(currentReceipt!=0&&publicGoals.TryGetValue(currentReceipt,out var goal)){currentPublicGoal=goal;publicGoals.Remove(currentReceipt);}
                if(receipt!=0&&currentReceipt==0)publicGoals.Remove(receipt);
            }
            public void Dispose() { currentReceipt = previous;currentPublicGoal=previousGoal; }
        }
    }
}
