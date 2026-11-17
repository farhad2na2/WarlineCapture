using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// HUD to ARIA contract for the base-under-attack warning (orchestrator decision 2026-11-17,
    /// S002 lone tank: legibility first). The HUD component that presents the warning implements
    /// this interface; ARIA finds it with GetComponent on the Skirmish match view. Every member is
    /// something the player is shown at that moment: the warning itself, the attacker it points at
    /// (only while fog or base vision lets the player see it) and the squad card it highlights as
    /// the nearest defender. ARIA reacts to these exactly as a player would and reads nothing else.
    /// Until a HUD component implements it, ARIA keeps its own public trigger (the HUD base-health
    /// readout falling, the same signal that raises the damage warning).
    /// </summary>
    public interface ISkirmishBaseAlertPresentation
    {
        /// <summary>The warning (banner/row, voice, minimap ping) is on screen now.</summary>
        bool BaseAlertPresented { get; }
        /// <summary>Why it was raised: 1 a hostile entered the base perimeter, 2 the base took damage; 0 none.</summary>
        byte BaseAlertReason { get; }
        /// <summary>Increases by one at each new warning onset (the frame the player first sees it).</summary>
        int BaseAlertSequence { get; }
        /// <summary>The attacker the warning points at is visible to the player (fog or base vision).</summary>
        bool BaseAlertAttackerShown { get; }
        /// <summary>World position of that attacker; meaningful only while <see cref="BaseAlertAttackerShown"/>.</summary>
        Vector3 BaseAlertAttackerWorld { get; }
        /// <summary>The HUD highlights a nearest-defender squad card (on any roster page).</summary>
        bool BaseAlertDefenderKnown { get; }
        /// <summary>Slot (0-4) of the highlighted card on the visible roster page; -1 when it is on another page.</summary>
        int BaseAlertDefenderSlot { get; }
        /// <summary>The HUD control that pages the roster to the highlighted card (navigation only, never selects).</summary>
        Button BaseAlertDefenderButton { get; }
    }
}
