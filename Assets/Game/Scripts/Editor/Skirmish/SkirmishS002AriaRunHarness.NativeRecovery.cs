#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        public static void RunS005NativeRecoveryDiagnosticAndExit()
        {
            string path = Path.GetFullPath("Design/AgentReports/SkirmishExpansion/S005/native-093-checkpoint-wire.json");
            if (!File.Exists(path)) throw new InvalidOperationException("Native recovery diagnostic save missing.");
            Environment.SetEnvironmentVariable("WARLINE_NATIVE_RECOVERY_DIAGNOSTIC", path);
            RunS005MapReviewAndExit();
        }

        private static void QueueNativeRecoveryDiagnostic(EntityManager em, Entity session, SkirmishResolvedSetup setup)
        {
            string path = Environment.GetEnvironmentVariable("WARLINE_NATIVE_RECOVERY_DIAGNOSTIC");
            if (string.IsNullOrEmpty(path)) return;
            if (!SkirmishNativeCheckpointCodec.TryRead(path, setup, out var snapshot))
                throw new InvalidOperationException("Native recovery diagnostic save incompatible.");
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            state.SessionId = new Unity.Collections.FixedString64Bytes(snapshot.SessionId);
            em.SetComponentData(session, state);
            var match = em.GetComponentData<SkirmishMatchState>(session);
            match.SessionId = state.SessionId;
            em.SetComponentData(session, match);
            if (!SkirmishNativeCheckpointRecoveryRunner.TryQueue(em, session, snapshot, setup))
                throw new InvalidOperationException("Native recovery diagnostic queue rejected.");
            Debug.Log("[S005NativeRecoveryDiagnostic] phase=Queued savedActors=" + snapshot.Actors.Length +
                " savedSources=" + snapshot.MapReferences.Length + " banks=" + snapshot.Banks.Length + " playerReady=0");
        }
    }
}
#endif
