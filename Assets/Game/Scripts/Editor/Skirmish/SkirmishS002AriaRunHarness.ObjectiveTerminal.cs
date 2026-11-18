#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Entities;
using UnityEngine;

namespace Game.Editor
{
    // FC/BT/CE (Frontline Control, Breakthrough, Convoy Escort) terminal evidence. The shared terminal probe
    // ([SkirmishTerminalFrozen], [SkirmishTerminalProbe] result=... reason=frozenResultAnd:MAIN MENU) is objective
    // agnostic: it reads the session's objective clock, the shared Victory/Defeat/Draw heading of the result card
    // and the normal Leave control. These additions only log, and only for FC/BT/CE sessions, so Base Assault
    // (S002/S026/...) output is unchanged:
    //   [SkirmishTerminalObjective] catalog type outcome reason elapsed deadline deadlinePct score...  at the terminal
    //   and, when ARIA stops before a result, the same line plus [SkirmishGateUnits] event=used ... unused=...
    public static partial class SkirmishS002AriaRunHarness
    {
        internal static bool ObjectiveTerminalSession(EntityManager em, Entity session, out SkirmishObjectiveKind kind)
        {
            kind = SkirmishObjectiveKind.None;
            if (session == Entity.Null || !em.Exists(session) || !em.HasComponent<SkirmishExpandedSessionComponent>(session)) return false;
            var expanded = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (expanded.IsLegacy != 0) return false;
            kind = expanded.ObjectiveKind;
            return SkirmishTerminalObjectiveEvidence.Applies(kind);
        }

        private static void EmitTerminalObjectiveEvidence(EntityManager em, Entity session, in SkirmishMatchState match, float elapsed)
        {
            try
            {
                if (!ObjectiveTerminalSession(em, session, out var kind)) return;
                var facts = new SkirmishTerminalObjectiveEvidence.Facts
                {
                    Catalog = catalogId, Kind = kind, Attempt = match.SessionId.ToString(),
                    Outcome = match.Outcome.ToString(), Reason = match.Reason.ToString(), Elapsed = elapsed, Deadline = -1f
                };
                // The player's public scoreboard (the goal HUD's snapshot), never the hidden reducer state.
                if (SkirmishNativePublicGoalPresentation.TryReadPlayer(em, session, out var goal, out _))
                    SkirmishTerminalObjectiveEvidence.ReadScore(goal, ref facts);
                Debug.Log(SkirmishTerminalObjectiveEvidence.Format(facts));
            }
            catch (Exception error)
            {
                Debug.LogWarning(SkirmishTerminalObjectiveEvidence.Marker + " catalog=" + catalogId + " result=Unavailable reason=" + error.Message);
            }
        }

        // ARIA stopped before a result on an FC/BT/CE session: keep the gate-3 used/unused list and the score
        // at the stop as evidence (Base Assault keeps its existing abort output).
        private static void EmitObjectiveAbortEvidence(EntityManager em, Entity session, in SkirmishMatchState match)
        {
            if (!ObjectiveTerminalSession(em, session, out _)) return;
            EmitGateUnitsUsed(em);
            EmitTerminalObjectiveEvidence(em, session, in match, SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match));
        }
    }
}
#endif
