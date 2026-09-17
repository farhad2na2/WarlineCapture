# Mobile command bar simplification

Implemented September 16, 2026.

- Removed Stop from the permanent command bar. Seven commands remain: Select, Move, Attack, Hold, Scan, Support, Build.
- Transferred 128 reference pixels to the five squad cards: tray width 579 → 707; each card is approximately 23% wider. Portraits and health bars stretch with the cards.
- Kept the four selected-unit radial actions. Added a 300 × 72 Cease Fire button below the wheel, following its responsive positioning. It invokes the existing Stop order, closes the menu, cancels orders and disables automatic engagement. Hold restores defense and automatic engagement.
- Cease Fire is available for an eligible group selection, not just a focused individual. Enemy, civilian, dead and embarked units are excluded by existing command capability checks.
- Updated English/Farsi central localization, field-guide text and the retired M3 tutorial slot. The saved slot ID and acknowledgement mask stay stable. No active mission requires toolbar Stop. Generic Stop guidance now points to Commands, then Cease Fire.
- Existing internal Stop enums and the hidden serialized toolbar reference remain for compatibility.

## Validation

Required macOS wrapper, isolated QA project, exit 0. Log: `validation.log`.

- Generated prefab: seven visible main commands; Cease Fire binding present; wider squad tray.
- Captured and visually reviewed English/Farsi at 1920 × 1080 and 2400 × 1080. Capture fixtures are synthetic UI states, not full mission replays.
- Automated geometry checks: command/tray screen bounds, no overlap between squad tray and commands, no overlap among main buttons, Cease Fire above the command bar, button dimensions at least 44 rendered pixels.
- `HoldStopScanCommandPlayModeTests`: Hold clears previous orders and anchors the unit; Stop clears a mixed vehicle/air selection. Updated these older fixtures to include the player faction required by current capability checks.
- `M03ActiveClassCommandTests`: grouped Hold, repeated Hold, Cease Fire, then Hold again. Automatic engagement returns after Cease Fire; invalid actors retain their orders.
- Applied exactly the three validated generated assets to the main project: HUD prefab, central localization catalog and M3 scenario.

This validation covers the changed layout and command behavior; it is not a full mission or physical-device QA pass.
