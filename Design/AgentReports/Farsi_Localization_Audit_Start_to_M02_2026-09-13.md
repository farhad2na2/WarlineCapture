# Farsi localization audit — first launch through M2

Date: 2026-09-13
Source revision: `f48014e1a08ffbf8ee34d9b5a7111c0228d9dba4` (`codex/m03-radar-warning`)
Locale: `fa-IR`
Scope: text audit only; no game code, translations, prefabs, assets, or saves changed in the source project.

The reported English text is reproducible. The main gaps are first-launch setup fields, runtime loading statuses, campaign mission copy, Build/placement metadata, ARIA ownership states, and mission-result copy. The shared catalog contains **1,891 English keys and 1,891 Farsi keys**, with no English-only key in that table. Equal key counts therefore do **not** establish complete localization: some runtime phrases have no catalog key, some bindings are bypassed, and some translated sentences retain English substitutions.

This register contains **86 findings/field families**: **35 Live**, **18 Preview**, **30 Source**, and **3 Review**. Repeated locations and variants are enumerated within the relevant row; this is not a count of unique words or unique catalog keys. The [CSV register](Farsi_Localization_Audit_Start_to_M02_2026-09-13.csv) contains the same entries for tracking.

## Evidence and coverage

- **Live:** recorded active runtime text in the isolated Unity Editor walkthrough. Representative Build, campaign, briefing and result captures were visually inspected. Not every short-lived status received its own screenshot.
- **Preview:** rendered the real first-launch prefab in the Editor with its serialized localization bindings and `NarrativeSequenceView.ApplyLanguage` using the configured Farsi locale. These are component-render observations, not a fresh-save end-to-end onboarding run.
- **Source:** inspected the active code branch and localization lookup. The conditional state was not separately forced in gameplay. These are documented gaps to verify on the later fix pass, not claimed live reproductions.
- **Review:** Latin names/acronyms observed in the relevant live or preview screen; recorded for consistency, rather than automatically classified as untranslated gameplay instructions.

| Screen / state | Audit performed | Findings / outcome |
|---|---|---|
| First-launch language choice | Dedicated view logic and prefab text reviewed | Farsi title, explanation and Continue supplied by the view. The English language card and English sample intentionally remain English. |
| First-launch comic / dialogue | Sequence keys checked against the dedicated Farsi locale and shared catalog | All **17 distinct dialogue keys** have Farsi text. This is text-data coverage, not a full voice or every-panel playback certification. |
| First-launch skip confirmation | Dedicated localized-target list and locale reviewed | Title, body, keep-watching and skip-intro controls have Farsi mappings. |
| Commander identity setup | Localized component render and source bindings | FI entries below. |
| ARIA guidance setup | Localized component render and source bindings | FG entries below. |
| Splash/loading and match/menu transitions | Live M1/M2 loading plus all emitted startup/unload statuses inspected | LD entries below. |
| Main menu | Live default screen and authored bindings | No additional missing-English UI copy observed in the sampled default state; brand excluded. Not every commander/profile variant was selected. |
| Commander profile | Live | MN-01. Player-entered names are not translation defects. |
| Campaign chapter selection | Live | CH review entries. |
| Campaign M1 selection | Live node selection | MS1 entries. |
| Campaign M2 selection / unlock display | Live node selection with isolated prerequisite progress | MS2 entries. This audit did not retest progression persistence. |
| M1 mission briefing | Live Campaign → M1 → Start Briefing | MB1 entries. |
| M2 mission briefing | Live Campaign → M2 → Start Briefing | MB2 entry. |
| Loadout / squad prep | Live and prefab binding scan | No new untranslated copy recorded; `APC` is an acronym. |
| M1 tutorial and match HUD | Live tutorial/command walkthrough through victory | AH entries; sampled tutorial instructions and main command labels appeared in Farsi. |
| M2 briefing/comms/debrief dialogue | Locale data coverage; walkthrough uses skip controls | All **9 distinct M2 dialogue keys** have Farsi text. Entire narrated sequences were not watched/listened to during this audit. |
| M2 tutorial and match HUD | Live build/recruit walkthrough | AH entries; sampled tutorial instructions appeared in Farsi. |
| M2 Build popup, empty-selection and recruitment | Live plus conditional error branches inspected | BU entries. |
| M2 placement and selected building | Live valid placement; invalid-status source inspected | PL / SE entries. No changes to placement rules. |
| M1 victory | Live | RE1 entry. |
| M2 first-clear victory | Live | RE2-01 through RE2-03. |
| M2 replay victory; M1/M2 defeat | Source and catalog inspection | RE2-04 and RF entries; not separate completed playthroughs. |
| Settings and pause | Live default states plus authored bindings/view logic | No additional untranslated default-state copy recorded. Every dropdown option/tab interaction was not separately exercised. `FPS`, `EN`, `FA`, `PRO`, `DEU`, `TRI` treated separately below. |
| Full map, expanded ARIA, resource exchange, reward-unlock popups | Authored prefab/binding inventory | No additional absent authored translation identified; not every dynamic state was opened in M1/M2. No claim of complete live coverage for these optional surfaces. |

## Screen-by-screen findings

English strings below use their readable source spelling. TMP's shaped Farsi output can store an embedded Latin word in reversed order; that storage detail is not presented as a second translation finding. Source aliases are resolved after the register.
### First launch — commander identity

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| FI-01 | Instruction | Your commander leads every operation. Choose your identity. | Preview | N1 / N2 | Outside the 18 narrative-localized targets; still English after applying the configured Farsi locale. Role list covers all six authored portrait cards. |
| FI-02 | Portrait role labels and selected-profile role | RECON SPECIALIST; ASSAULT LEADER; INTEL OFFICER; FIELD COMMANDER; OPERATIONS LEAD; SUPPORT COMMANDER | Preview | N1 / N2 | Outside the 18 narrative-localized targets; still English after applying the configured Farsi locale. Role list covers all six authored portrait cards. |
| FI-03 | Callsign rules | 3 - 12 characters, letters, numbers, hyphens only | Preview | N1 / N2 | Outside the 18 narrative-localized targets; still English after applying the configured Farsi locale. Role list covers all six authored portrait cards. |
| FI-04 | Selected-profile heading | SELECTED PROFILE | Preview | N1 / N2 | Outside the 18 narrative-localized targets; still English after applying the configured Farsi locale. Role list covers all six authored portrait cards. |
| FI-05 | Selected-profile description | Balanced leader with strong operational control. | Preview | N1 / N2 | Outside the 18 narrative-localized targets; still English after applying the configured Farsi locale. Role list covers all six authored portrait cards. |
| FI-06 | Previous button | ‹   PREV | Preview | N1 / N2 | Outside the 18 narrative-localized targets; still English after applying the configured Farsi locale. Role list covers all six authored portrait cards. |
| FI-07 | Portrait names and selected-profile name | MIRA ALAVI; JALIL OKAFOR; SAMIRA KHALID; DALIA RAHIM; KENJI SATO; IRINA PETROVA | Review | N1 / N2 | Visible Latin names in the Farsi component preview. Record for consistent name transliteration; this audit does not propose changing character identities. DALIA RAHIM repeats in the selected-profile panel. |

### First launch — ARIA guidance choice

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| FG-01 | Eyebrow | FIRST-LAUNCH SETUP | Preview | N1 / N2 | Remains English in the localized component render. The main title, choice names and Continue button do translate; these surrounding fields do not. ON is the authored displayed state; OFF was not exercised. |
| FG-02 | Full guidance description | Complete support for<br>new commanders. | Preview | N1 / N2 | Remains English in the localized component render. The main title, choice names and Continue button do translate; these surrounding fields do not. ON is the authored displayed state; OFF was not exercised. |
| FG-03 | Tactical hints description | Helpful tips in key<br>situations. | Preview | N1 / N2 | Remains English in the localized component render. The main title, choice names and Continue button do translate; these surrounding fields do not. ON is the authored displayed state; OFF was not exercised. |
| FG-04 | Minimal guidance description | Only essential alerts.<br>Maximum challenge. | Preview | N1 / N2 | Remains English in the localized component render. The main title, choice names and Continue button do translate; these surrounding fields do not. ON is the authored displayed state; OFF was not exercised. |
| FG-05 | Three support-level headings | SUPPORT LEVEL | Preview | N1 / N2 | Remains English in the localized component render. The main title, choice names and Continue button do translate; these surrounding fields do not. ON is the authored displayed state; OFF was not exercised. |
| FG-06 | Support-level values | HIGH; MEDIUM; LOW | Preview | N1 / N2 | Remains English in the localized component render. The main title, choice names and Continue button do translate; these surrounding fields do not. ON is the authored displayed state; OFF was not exercised. |
| FG-07 | Subtitle setting label | SUBTITLES | Preview | N1 / N2 | Remains English in the localized component render. The main title, choice names and Continue button do translate; these surrounding fields do not. ON is the authored displayed state; OFF was not exercised. |
| FG-08 | Subtitle setting explanation | Show dialogue subtitles. | Preview | N1 / N2 | Remains English in the localized component render. The main title, choice names and Continue button do translate; these surrounding fields do not. ON is the authored displayed state; OFF was not exercised. |
| FG-09 | Reduced-motion setting label | REDUCED MOTION | Preview | N1 / N2 | Remains English in the localized component render. The main title, choice names and Continue button do translate; these surrounding fields do not. ON is the authored displayed state; OFF was not exercised. |
| FG-10 | Reduced-motion explanation | Minimize camera movement. | Preview | N1 / N2 | Remains English in the localized component render. The main title, choice names and Continue button do translate; these surrounding fields do not. ON is the authored displayed state; OFF was not exercised. |
| FG-11 | Both setting toggles | ON | Preview | N1 / N2 | Remains English in the localized component render. The main title, choice names and Continue button do translate; these surrounding fields do not. ON is the authored displayed state; OFF was not exercised. |
| FG-12 | Previous button | ‹       PREV | Preview | N1 / N2 | Remains English in the localized component render. The main title, choice names and Continue button do translate; these surrounding fields do not. ON is the authored displayed state; OFF was not exercised. |

### Splash / loading / return transition

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| LD-01 | Progress status | Preparing command interface | Source | L1 | Initial fallback before progress arrives. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-02 | Progress status | Loading command shell | Source | L2 | Startup/menu loading. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-03 | Progress status | Command shell ready | Source | L2 / L3 | End of startup or return to menu. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-04 | Progress status | Preparing match load | Source | L3 | Match route accepted before scene-load progress is available. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-05 | Progress status | Loading operation interface | Live | L4 | Observed during M2 entry. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-06 | Progress status | Loading match | Live | L3 | Observed during M1 and M2 entry. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-07 | Progress status | Loading map presentation | Live | L3 | Observed during M1 and M2 entry. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-08 | Progress status | Preparing match | Live | L5 | Observed during M1 and M2 entry. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-09 | Progress status | Preparing gameplay runtime | Source | L5 | Startup step; may be shorter than the sampler interval. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-10 | Progress status | Resetting match state | Live | L5 | Observed during M1 and M2 entry. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-11 | Progress status | Preparing map data | Live | L5 | Observed during M1 entry. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-12 | Progress status | Preparing unit prefabs | Live | L5 | Observed during M2 entry. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-13 | Progress status | Preparing AI factions | Source | L5 | Startup step. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-14 | Progress status | Preparing resource exchange | Source | L5 | Startup step. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-15 | Progress status | Binding match HUD | Source | L5 | Startup step. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-16 | Progress status | Starting gameplay systems | Source | L5 | Startup step. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-17 | Progress status | Validating scenario recovery | Source | L5 | Startup step. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-18 | Progress status | Waiting for scenario catalog | Source | L5 | Conditional startup wait. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-19 | Progress status | Focusing camera | Source | L5 | Startup step. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-20 | Progress status | Spawning world | Source | L5 | Startup step. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-21 | Progress status | Starting match | Source | L3 | Fallback while startup is incomplete. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-22 | Progress status | Match ready | Live | L3 | Observed at the end of M1/M2 loading. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-23 | Progress status | Unloading map presentation | Source | L3 | Return from a match; presentation drain. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-24 | Progress status | Unloading operation map | Source | L3 | Return from a match; operation-map unload. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-25 | Progress status | Unloading match | Source | L3 | Return from a match; scene unload. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-26 | Progress status | Map unload failed: {detail} | Source | L3 | Conditional failure. English prefix plus raw failure detail; not induced during this audit. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |
| LD-27 | Progress status | Preparing opening briefing | Source | L4 | First-launch EnterMission startup disposition; not sampled in the replay-based live runs. No matching Farsi translation for this emitted status was found. Decorative loading labels and the authored tip are separate and have translations. |

### Commander profile

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| MN-01 | Commander subtitle | VICTORY IS PLANNED | Live | M1 | Runtime profile binding writes an untranslated motto. Default main-menu navigation itself did not show another English copy gap in the sampled state. |

### Campaign — chapter selection

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| CH-01 | Character labels | SAMIRA; DALIA | Review | C1 / LC | Observed in the current-mission character strip. The Farsi catalog deliberately repeats the Latin source values; consistency with Farsi narrative names needs a content decision. |
| CH-02 | ARIA protocol heading | ARIA | Review | C1 / LC | The surrounding heading is Farsi, but ARIA remains Latin. Other screens use آریا. Keep separate from missing instruction translations. |

### Campaign — M1 selected

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| MS1-01 | Summary | Secure the Old Market corridor and protect the civilian route. | Live | C2 / C3 | mission.m01.summary falls back to English. |
| MS1-02 | Reward strip | 260 XP  &#124;  1,200 CREDITS | Live | C2 / C3 | mission.m01.reward.card is absent from the shared locale table. |
| MS1-03 | Objective card | PROTECT CIVILIANS | Live | C2 / C3 | Exact runtime source phrase is not translated. |
| MS1-04 | Objective card | KEEP SQUAD SAFE | Live | C2 / C3 | Exact runtime source phrase is not translated. |

### Campaign — M2 selected

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| MS2-01 | Summary | Reopen an abandoned forward post before a second hostile cell reaches it. | Live | C2 / C3 | Runtime wording differs from the translated authored prefab summary. |
| MS2-02 | Reward strip | 320 XP  &#124;  1,500 CREDITS  &#124;  BARRACKS UNLOCK | Live | C2 / C3 | mission.m02.reward.card is absent from the shared locale table. |

### M1 mission briefing

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| MB1-01 | Mission summary | Secure the Old Market corridor and protect the civilian route. | Live | B1 | Observed through Campaign → M1 → Start Briefing; shares missing runtime mission copy with the mission-select screen. |
| MB1-02 | Protect-squad objective | KEEP THE COMMAND SQUAD ALIVE | Live | B1 | The full text is English; the rendered narrow row truncates it to KEEP THE COMMAND SQUAD…. |

### M2 mission briefing

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| MB2-01 | Mission summary | Reopen the abandoned JRC forward post before the Ash Line reaches it. Establish a foothold and prepare for incoming threats. | Live | B1 | Authored source text has a Farsi catalog counterpart, but the runtime briefing still displays the English paragraph. This is a presentation/binding issue, not simply an absent authored translation. |

### M1 / M2 match HUD — ARIA

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| AH-01 | Ownership / preview state | PLAYER CONTROL | Live | H1 / H2 | Displayed when the tutorial card yields to ownership/preview state. PLAYER CONTROL has a catalog translation but is still written directly to this surface; the other exact phrases have no source translation. |
| AH-02 | Ownership / preview state | ARIA CONTROL | Live | H1 / H2 | Displayed when the tutorial card yields to ownership/preview state. PLAYER CONTROL has a catalog translation but is still written directly to this surface; the other exact phrases have no source translation. |
| AH-03 | Ownership / preview state | PREVIEW | Live | H1 / H2 | Displayed when the tutorial card yields to ownership/preview state. PLAYER CONTROL has a catalog translation but is still written directly to this surface; the other exact phrases have no source translation. |

### M2 Build popup

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| BU-01 | Building list title and selected-item title | Barracks | Live | U1 / U2 / U3 | Title-case catalog display name remains English. An uppercase BARRACKS catalog translation does not cover the emitted title-case value. |
| BU-02 | Building role subtitle | STRUCTURE | Live | U1 / U2 / U3 | Runtime metadata role has no exact source translation. |
| BU-03 | Build instruction / placement-request message | Barracks | Live | U1 / U2 / U3 | English item name is inserted into otherwise Farsi instructions, including the Place/choose-location prompt. |
| BU-04 | Empty selection requirements | No requestable items. | Live | U1 / U2 / U3 | Observed before selecting an item and when switching category; differs from the translated longer empty-category strings. |
| BU-05 | Soldier primary action button | RECRUIT | Live | U1 / U2 / U3 | Uppercase action label remains English; the normal Recruit verb translation does not cover it. |
| BU-06 | Soldier instruction and recruitment confirmation | Rifleman Male IV | Live | U1 / U2 / U3 | English metadata name remains inside otherwise Farsi messages; the card title and detail name themselves display تفنگدار مرد ۴. |
| BU-07 | Not-enough-materials instruction | insufficient materials. | Live | U1 / U2 / U3 | Observed after placing the Barracks and reopening Build. The sentence is partly Farsi, with an English reason and Barracks name. |

### M2 Build popup — conditional errors

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| BU-08 | Request/requirements failure | Insufficient materials. | Source | U2 | Short requirement error has no Farsi key build.drawer.failure.short.insufficient_materials. Branch inspected; not all shortages were forced in the live audit. |
| BU-09 | Request/requirements failure | Insufficient credits and materials. | Source | U2 | Short requirement error has no Farsi key build.drawer.failure.short.insufficient_credits_and_materials. Branch inspected; not all shortages were forced in the live audit. |
| BU-10 | Request/requirements failure | Cannot {verb} {item}: insufficient credits. | Source | U2 | Missing build.drawer.failure.insufficient_credits. A broader translated template can translate the sentence wrapper while preserving the English reason. Branch inspected; not all shortages were forced in the live audit. |
| BU-11 | Request/requirements failure | Cannot {verb} {item}: insufficient credits and materials. | Source | U2 | Missing build.drawer.failure.insufficient_credits_and_materials; the reason remains English inside the broader translated wrapper. Branch inspected; not all shortages were forced in the live audit. |

### M2 building placement

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| PL-01 | Building title | BARRACKS | Live | P1 | Appears within the Farsi BUILD title; captured output still contains an English building name. |
| PL-02 | Valid-location status | VALID PLACEMENT (1006,330) 40X20 | Live | P1 / P2 | Coordinates and footprint vary. The English status phrase survives the runtime status formatter. |
| PL-03 | Invalid-location status | BLOCKED BY ROAD OR BLOCKER ({x},{y}) {width}X{height} | Source | P1 / P2 | Same status path as PL-02. Invalid placement is English at source and no matching translated status template was found; this audit did not perform a new invalid-confirm attempt. |

### M2 selected-building panel

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| SE-01 | Selection title | Barracks (1006,330) | Live | P2 | The building name is concatenated with coordinates; this composed title remains English. |

### M1 victory result

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| RE1-01 | Outcome sentence | Hostile patrol neutralized. The Old Market corridor is secure. | Live | R1 | Observed after the M1 tutorial/command walkthrough reached victory. |

### M2 victory result

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| RE2-01 | Mission identity heading | M02 ESTABLISH THE BASE<br>FORWARD POST | Live | R1 / R2 | Both title and location remain English. M02 itself is an intentional mission code. |
| RE2-02 | First-clear outcome paragraph | Forward post operational. Dalia Rahim accepts field-lead duty. The clinic-route warning sector has gone dark. | Live | R1 | Observed after the M2 build/recruit walkthrough reached its result. |
| RE2-03 | Unlock reward | BARRACKS UNLOCK | Live | R1 / R2 | Displayed alongside +1. XP and credit reward labels in this result already translate. |

### M2 replay victory result

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| RE2-04 | Replay outcome | Forward post defended. The clinic route remains under coalition control. | Source | R1 / R2 | Replay-specific branch; not separately played to victory. Exact English source has no matching Farsi translation; defeat was not deliberately triggered. |

### M1 / M2 defeat result

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| RF-01 | Mission status | COMMAND SQUAD LOST | Source | R1 / R2 | Shared loss presentation. Exact English source has no matching Farsi translation; defeat was not deliberately triggered. |
| RF-02 | Objective status labels | FAILED | Source | R1 / R2 | Written to both patrol and squad result rows. Exact English source has no matching Farsi translation; defeat was not deliberately triggered. |
| RF-03 | Civilian status | AT RISK | Source | R1 / R2 | Shared loss presentation. Exact English source has no matching Farsi translation; defeat was not deliberately triggered. |

### M1 defeat result

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| RF-04 | Outcome sentence | The command squad was lost. Regroup and redeploy. | Source | R1 / R2 | M1 loss summary. Exact English source has no matching Farsi translation; defeat was not deliberately triggered. |

### M2 defeat result

| ID | Element | English text / variants | Evidence | Source | Notes |
|---|---|---|---|---|---|
| RF-05 | Outcome sentence | The forward post fell before the defense was secured. Rebuild and redeploy. | Source | R1 / R2 | M2 loss summary. Exact English source has no matching Farsi translation; defeat was not deliberately triggered. |

## Intentional Latin text and exclusions

- `WARLINE CAPTURE` is the shared brand/logo. It is not counted as a missing translation.
- `M01`, `M02`, `M03`, chapter markers, numbers, coordinates, percentages, durations and footprint dimensions are identifiers/numbers. English descriptive words surrounding them **are** included above.
- `APC`, `JRC`, `XP`, `FPS`, `EN`/`FA`, and the `PRO`/`DEU`/`TRI` color-vision abbreviations need a consistent product policy, but are not counted as missing sentence translations. The campaign reward findings concern the English reward labels/whole unlocalized strip, not the letters XP alone.
- Authored first-launch `ECHO-7` and player-entered commander names/callsigns are identity values. The English rules and surrounding labels are counted; user-entered text should not be translated.
- Development reviewer controls (`PREV`, `GAME`, `DEBRIEF`, `CAPTURE`, `SAFE AREA`, etc.) are excluded from shipping-screen findings. The user-facing Previous buttons on setup screens are included.
- The nine English-only `narrative.m01.*` records in `M01_FirstContact_Narrative.asset` were checked but **not** classified as current visible gaps: `CampaignMissionNarrativePolicy.UsesMissionSequences` excludes M1. Its playable tutorial uses a different presentation path. Retain those records as an audit lead if that policy changes.
- Legacy/non-V3 briefing fallback labels such as `STARTING RESOURCES`, `MISSION ACCESS`, `BUILDING / PRODUCTION`, and `TRANSPORT / AIR OFF` were found in source. The active M1/M2 target layout returns through the V3 condition block, so these were not added as confirmed screen findings.
- Generic fallback metadata such as `No description configured.`, `{name} has no configured production description.`, or `Building {id}` was not counted without an observed/verified M1/M2 content path. `PRODUCE` is also an untranslated action source for vehicles/aircraft, but those production tabs are mission-restricted here; the live M2 soldier case is `RECRUIT`.
- M3/M4 gameplay, optional store/inbox/events/ranking/armory flows, Android/device behavior, full audio-language validation, and embedded environmental/illustration signage are outside this start-to-M2 audit. No evidence images were added to Design or Git.

## Why the existing translation count misses these cases

1. **Runtime wording differs from authored wording.** Campaign summaries and reward lines replace localized prefab text with phrases not represented by the same key/source string.
2. **Substitutions remain English.** A Farsi message template inserts the catalog's English `Barracks` or `Rifleman Male IV` value. Translating the surrounding sentence does not translate those arguments.
3. **Case and exact wording matter.** `Barracks` vs `BARRACKS`, `Recruit` vs `RECRUIT`, and `No requestable items.` vs the longer empty-category copy are distinct sources.
4. **Specialized narrative ownership skips the generic runtime binder.** First-launch narrative content relies on its explicit localized-target list, which covers 18 targets and leaves much of the expanded setup UI outside it.
5. **Some translated sources still escape onto the screen.** M2 briefing and `PLAYER CONTROL` have corresponding catalog text but were observed in English. A later fix must verify the runtime binding/ownership path, not just add duplicate entries.

## Source references

Paths and line numbers refer to the audited revision. The linked line is the relevant method/region; row notes distinguish exact missing keys from metadata or runtime presentation issues.

- **LC:** [Assets/Game/Resources/Localization/V3UiLocalizationCatalog.asset](/Users/farhad/Projects/WarlineCapture/Assets/Game/Resources/Localization/V3UiLocalizationCatalog.asset)
- **N1:** [Assets/Game/Prefabs/UI/Narrative/FirstLaunch/FirstLaunchNarrativeSequence.prefab](/Users/farhad/Projects/WarlineCapture/Assets/Game/Prefabs/UI/Narrative/FirstLaunch/FirstLaunchNarrativeSequence.prefab)
- **N2:** [Assets/Game/Scripts/UI/Narrative/NarrativeSequenceView.cs:114](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Narrative/NarrativeSequenceView.cs:114)
- **L1:** [Assets/Game/Scripts/UI/Shell/UIShellLoadingProgressView.cs:11](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellLoadingProgressView.cs:11)
- **L2:** [Assets/Game/Scripts/Composition/MenuBootstrapLoadingUtilities.cs:23](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MenuBootstrapLoadingUtilities.cs:23)
- **L3:** [Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs:314](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs:314)
- **L4:** [Assets/Game/Scripts/UI/Shell/Ecs/UiShellStartupFlow.cs:26](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/Ecs/UiShellStartupFlow.cs:26)
- **L5:** [Assets/Game/Scripts/Composition/MatchGameplayStartupCompositionSystemHelper.cs:155](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchGameplayStartupCompositionSystemHelper.cs:155)
- **M1:** [Assets/Game/Scripts/UI/Screens/CommanderProfileContentView.cs:25](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Screens/CommanderProfileContentView.cs:25)
- **C1:** [Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab](/Users/farhad/Projects/WarlineCapture/Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab)
- **C2:** [Assets/Game/Scripts/UI/Screens/CampaignOperationsScreenView.cs:131](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Screens/CampaignOperationsScreenView.cs:131)
- **C3:** [Assets/Game/Scripts/UI/Screens/CampaignOperationsScreenView.Goals.cs:28](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Screens/CampaignOperationsScreenView.Goals.cs:28)
- **B1:** [Assets/Game/Scripts/UI/Screens/MissionBriefingScreenView.cs:90](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Screens/MissionBriefingScreenView.cs:90)
- **H1:** [Assets/Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.Presentation.cs:429](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.Presentation.cs:429)
- **H2:** [Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.AssistantSettings.cs:263](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.AssistantSettings.cs:263)
- **U1:** [Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogQueryUiSystemHelper.cs:68](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogQueryUiSystemHelper.cs:68)
- **U2:** [Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogPresentationSystemHelper.cs:135](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogPresentationSystemHelper.cs:135)
- **U3:** [Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.Instructions.cs:13](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.Instructions.cs:13)
- **P1:** [Assets/Game/Scripts/UI/Screens/BuildPlacementConfirmationBarView.cs:229](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Screens/BuildPlacementConfirmationBarView.cs:229)
- **P2:** [Assets/Game/Scripts/Systems/BuildingPlacementQueryUiSystemHelper.cs:107](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementQueryUiSystemHelper.cs:107)
- **R1:** [Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Core.cs:173](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Core.cs:173)
- **R2:** [Assets/Game/Scripts/UI/Screens/MissionResultPopupView.cs:194](/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Screens/MissionResultPopupView.cs:194)

Additional inspected ownership/data paths:

- `Assets/Game/Scripts/UI/Localization/V3LocalizationRuntimeBinderView.cs`
- `Assets/Game/Scripts/UI/Localization/V3LocalizedTextBindingView.cs`
- `Assets/Game/Scripts/Configs/Localization/GameLocalization.cs`
- `Assets/Game/Scripts/Editor/Narrative/FirstLaunchNarrativeV3PrefabBuilder.cs` — `AssignLocalizedBindings`
- `Assets/Game/Configs/Narrative/FirstLaunch/FirstLaunchPersianLocale.asset`
- `Assets/Game/Configs/Narrative/FirstLaunch/FirstLaunchSequence.asset`
- `Assets/Game/Configs/Narrative/Chapter01/M02_EstablishBase_Narrative.asset`
- `Assets/Game/Scripts/Runtime/Missions/CampaignMissionNarrativePolicy.cs`

## Reproduction record and limits

Unity 6000.5.2f1 Editor, desktop Game view (live captures 2400×1080); localized onboarding component captures 1920×1080. Work ran in `/private/tmp/warline-campaign-return-qa` through `Tools/CI/invoke_unity_macos.sh`, with copied current scripts/configs/resources/UI prefabs, temporary audit instrumentation, and isolated campaign progress stores. The user's source Editor and source save were not reset. No Android validation was performed.

Successful audit runs:

- `/private/tmp/warline-fa-audit-m2.log` — real M2 four-click recruitment walkthrough; pass marker from the existing probe.
- `/private/tmp/warline-fa-audit-menus2.log` — menu/chapter/mission selection, M2 briefing, loadout, M1 play and result inspection.
- `/private/tmp/warline-fa-audit-m1-briefing.log` — M1 briefing through its visible Start Briefing control.
- `/private/tmp/warline-fa-audit-narrative-result.log` — M2 first-clear result reached; pass marker.
- `/private/tmp/warline-fa-audit-narrative-preview.log` — first-launch localized component renders; pass marker.

Text observations and temporary captures are in `/private/tmp/warline-fa-audit/`; Build captures from the existing probe are in `/private/tmp/warline-m02-placement/`. Temporary evidence is not a durable repository artifact; exact findings and sources are preserved in this Markdown file and its CSV register. An initial menu probe did not reach every requested mission route; only the later verified observations are used for briefing/play coverage.

This is a screen-by-screen inventory of the observed and source-verified gaps, **not a claim that every possible error, profile, replay outcome, or popup state has been rendered**. The coverage table explicitly identifies those limits. No fixes have been made.
