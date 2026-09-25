using System;
using System.Collections.Generic;
using UnityEngine;

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
        }

        public static void End()
        {
            Active = false;
            pendingReceipt = currentReceipt = 0;
            issued.Clear();
        }

        public static void ObserveRelease(int frame, Vector2 position)
        {
            if (!Active) return;
            releaseFrame = frame;
            releasePosition = position;
            pendingReceipt = ++nextReceipt;
            CompletedTouches++;
        }

        public static uint ClaimRelease(int frame, Vector2? position = null)
        {
            if (!Active || pendingReceipt == 0 || frame < releaseFrame || frame - releaseFrame > 2 ||
                (position.HasValue && Vector2.SqrMagnitude(position.Value - releasePosition) > 4f)) return 0;
            uint receipt = pendingReceipt;
            pendingReceipt = 0;
            issued.Add(receipt);
            return receipt;
        }

        public static Scope Enter(uint receipt) => new(receipt);

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
            internal Scope(uint receipt)
            {
                previous = currentReceipt;
                currentReceipt = Active && receipt != 0 && issued.Remove(receipt) ? receipt : 0;
            }
            public void Dispose() { currentReceipt = previous; }
        }
    }
}
