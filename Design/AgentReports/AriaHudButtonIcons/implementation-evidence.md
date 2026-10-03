# ARIA, skirmish base and selection Commands buttons

The user approved short labels with metallic icons and role-specific colors. Production icons were generated with the built-in ImageGen tool from the approved board; the prompt is saved in production-imagegen-prompt.txt. The transparent atlas is Assets/Game/Resources/HudButtonIcons/hud-action-icons-v01.png.

The shared HUD component uses the current localized fonts at a fixed 24-point button-label size, mirrors icon placement for Persian, and preserves the existing Buttons and input callbacks. Green represents Play/Continue, red Stop/enemy base, amber alerts, blue guidance/navigation, and slate Cancel/Skip. Longer ARIA consent and failure descriptions sit outside action labels. The top-left Commands opener has the same pointer icon and text treatment.

Visual direction: approved.

Automated checks: focused-validation-02.log passed on the first implementation, validating 16 icons, English/Persian sizing, transparent import, listener preservation, and disabled-state preservation. focused-validation-03.log passed on the final corrected candidate, including inherited-gradient suppression and preserved hit targets.

Failed evidence retained:

- focused-validation-01.log.dispatch.json: Pipeline dispatch interrupted by script reload; validation did not start.
- campaign-aria-input-01.log: invalid entry-point lookup; no playthrough started.
- campaign-aria-input-02.log: stopped intentionally after the native capture showed the inherited green gradient covering the new red Stop surface. This candidate is not readiness evidence.
- native-screen-validation-01.log: legacy skirmish map metadata failure before HUD.
- native-screen-validation-02.log: shipping setup queued S003 successfully, then map launch fell back to legacy compatibility identity and failed metadata binding before HUD. Both runs exited their own Play session and retained the Editor.

Native screen inspection: first campaign capture confirmed icons and matching label sizes on Field Guide, Stop ARIA, and Commands; it exposed the role-background layering issue above. Final English campaign native capture confirms the red Stop background, metallic icons and matching labels. Read-only runtime inspection confirmed COMMANDS, FIELD GUIDE and STOP ARIA all use size 24 and the expected icons. Screenshots: Native/campaign-final-running.png and Native/campaign-final-commands.png. Skirmish English/Persian desktop/mobile inspection remains pending because map startup fails before HUD.

Normal-input complete mission: campaign-aria-input-03.log used normal touch to start ARIA and reached the breach, military-clearance and audit objectives on the final candidate. Stopped the owned probe after the user questioned the relevance of a full mission regression for this button-only presentation change. No complete mission, result, settlement or return claim is made. The Editor was preserved.

Real player/device acceptance: pending; Editor checks do not establish device acceptance.
