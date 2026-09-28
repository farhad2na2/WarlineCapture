# CH04-M01 Air Corridor — readiness evidence

## Scope

Story source: `Design/Campaign_Mission_High_Level_Design_Catalog.md`, CH04-M01.
Urban relief-airfield defense: two supplied automatic G2A launchers, a protected mobile radar,
two escort soldiers, and two authored Vanguard waves (six jets/drones total).
The existing Campaign, briefing, tactical controls, Show Me, ARIA Play/Stop, field guide,
result and return UI are reused. No mission-specific gameplay button was added.

Existing `FutureMissionComics/CH04M01_AirCorridor` briefing, comms and debrief artwork
is reused without changing image pixels, faces or city setting. Each dialogue has an
authored crop for 16:9 and 20:9. Six English and six Persian approved local voice files
are included; there is no runtime network TTS.

## Automated checks

- Clean final combined checkpoint and English journey:
  `/private/tmp/warline-air-corridor-input-en-final-02.log`, wrapper exit 0.
  `[AirCorridorCheckpoint] result=Passed voices=12 comics=6 locales=en,fa-IR controls=existing-only`.
  Radar binding/damage/destruction, 240-second deadline, four-step public guidance,
  destroyed-reference missile-trail lifecycle regression, missile launcher checks and
  Campaign comic coverage passed. No C# or Burst compilation errors were found.
- Comic coverage: 34 sequences, 80 dialogue states, both 16:9 and 20:9; all six new
  mission dialogue states have distinct authored crops in both aspects.

## Normal-input mission journeys

- English: `/private/tmp/warline-air-corridor-input-en-final-02.log`.
  `[AirCorridorInput] result=Passed locale=en voices=6 missiles=live radar=linked
  ARIA-actions=7 victory=result=settlement=return`.
  Actual launcher movement, live missiles, both waves, blocking comms playback,
  six complete voice playbacks, victory, reward settlement and Campaign return were
  observed. Mission elapsed time: 70.610 seconds; three stars, zero friendly losses,
  undamaged radar. No position, health or outcome injection.
  The run used an isolated progress store and temporary Editor input routing equivalent
  to a foreground Game view; settings were restored before exit.
- Final Persian: `/private/tmp/warline-air-corridor-input-fa-final.log`, wrapper exit 0.
  `[AirCorridorInput] result=Passed locale=fa-IR voices=6 missiles=live radar=linked ARIA-actions=7 victory=result=settlement=return`.
  Both live waves, all six Persian voice playbacks, mandatory comics, victory,
  settlement and return passed. Mission elapsed time: 70.666 seconds; three stars,
  zero friendly losses, undamaged radar. No C# or Burst errors or runtime exceptions
  were found. The earlier Persian full journey also passed in
  `/private/tmp/warline-air-corridor-input-fa-01.log`.

## Native visual review

English 1920×1080 and Persian 2400×1080 native Campaign, briefing, comics, HUD and
result captures were reviewed under `/private/tmp/warline-air-corridor-input/`.
The original art pixels and portraits are preserved. The briefing uses clean artwork
with one native Deploy button. Existing colored unit/command controls and ARIA
Play/Stop/Show Me are retained. Result summary and radar status now fit in English
and Persian. Persian enemy-intelligence copy was shortened after a wide-screen
capture exposed truncation; the final Persian briefing capture is readable without
ellipsis. Representative native captures are saved in `VisualReview/` beside this report.
The final Persian return capture confirms the settled Campaign screen, not merely
the loading overlay. These are implementation reviews, not user visual acceptance.

## Failed evidence retained

Build logs 01–07, input logs en-01–en-09 and final candidate logs are retained under
`/private/tmp/warline-air-corridor-*`.
Earlier runs exposed radar-role/objective publication mismatch, inherited guidance-step
counts, repeated focus requests, and background Editor input routing. The en-09 run
contained an armored-extraction Burst string-conversion error, corrected before the
clean final checkpoint. The first combined final run exposed a stale destroyed
missile-trail reference with domain reload disabled; its source was fixed and the
regression test and subsequent combined run passed. A deferred screenshot captured
after its locale/resolution had already changed; capture completion now precedes
those changes and validation shutdown. Shutdown logs also report 398 persistent
allocations; this remains recorded separately from the passing mission checks.

## Readiness and separate acceptance gates

CH04-M01 is ready to play in the Unity Editor: compilation/checkpoint, both language
journeys, ARIA, local voices, comics, native layouts, result, settlement and return passed.
Real player acceptance and physical device testing are separate and have not been performed.
No packaged-player build or device acceptance is claimed. Repository publication is
recorded in the Git history; it does not change these acceptance limits.

## Cleanup

Four unrelated untracked Unity recovery scenes and their metadata, plus one crash dump,
were removed from the checkout and kept recoverably in
`/private/tmp/warline-removed-recovery.YJrDIQ`. No campaign scene was removed.
