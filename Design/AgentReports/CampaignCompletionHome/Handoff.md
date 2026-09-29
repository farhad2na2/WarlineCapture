# Mission-completion home package — handoff to main-menu agent

Date: 2026-09-29
Scope: artwork, copy and integration guidance only. The user explicitly assigned main-menu implementation to another agent.
Approval: the user approved the recommended completion direction and the previously shown full home concept. This does not establish native implementation or device acceptance.
No runtime, prefab, scene or localization catalog changes are delivered by this package.

## Start here

Use [completion-assets.json](completion-assets.json) as the asset/copy manifest. Three final, text-free PNGs are ready in [Assets](Assets). Each is 1672×941 and was generated with built-in ImageGen from the existing canonical epilogue comic as a style/character reference.

- [Rebuilding](Assets/home-aftermath-rebuilding-v01.png): quiet repair work and a recovering local market.
- [Supply](Assets/home-aftermath-supply-v01.png): relief supplies arriving at a protected depot.
- [Watch](Assets/home-aftermath-watch-v01.png): the team watching a secured district from its outpost.

The lower-left is deliberately calm/dark for native text and controls. Characters are toward the upper-right. Keep faces visible; preserve aspect ratio with top-aligned cropping inside the actual hero bounds. Add the existing native readability gradient as needed. Do not stretch the artwork or bake labels/buttons into it.

Copy the PNGs into an additive folder such as `Assets/Game/Art/UI/V3Shared/MainMenuPlates/CampaignCompletion` when integrating. Import through Unity authoring: Sprite (single), mipmaps off, Read/Write off, clamp wrap, maximum size 2048, the project's normal mobile texture/compression policy. Bind direct sprite references in the existing Campaign card. No new Resources folder or scene required solely for these images. Do not overwrite any existing comic or portrait.

The [approved home concept](Mockups/home-campaign-complete-v01.png) is a composition reference only; never use it as a shipping flattened background. Preserve exact original ARIA and Commander assets, actual earned Credits, existing Oxanium fonts and mobile hit areas.

## State rules — current content is not the entire story

Repository inspection on this date:
- `CampaignMissionSequence.RegisteredMissionCount` is 18; the last registered mission is `saga.ch04.m03.split_front`.
- The authored comic catalog reaches Chapter V Mission 05, `saga.ch05.m05.command_node`, followed by the canonical epilogue.
- `MainMenuCampaignCardView.Refresh` currently reads the selected Campaign mission and its plate, then changes the CTA for an already-cleared mission. It does not project an aggregate end-of-content hero.

Use saved authoritative completion/progression, not a null plate, an empty NextMissionId or the selected mission's FirstClearCompleted alone.

| State | Heading | Art and purpose | Main action |
| --- | --- | --- | --- |
| More current missions remain | Existing mission heading | Existing projected mission art/briefing | Continue Campaign |
| All required currently playable/released missions cleared; later authored story not yet playable | ALL AVAILABLE MISSIONS COMPLETED | One of the three generic aftermath scenes with its paired caption | Choose Mission |
| Entire authored, released campaign cleared including the final Chapter V mission | CAMPAIGN COMPLETE | Canonical epilogue as the default; authored aftermath rotation is also suitable | Choose Mission |
| Required current mission still uncleared, inaccessible or missing art/content | Existing honest mission/access or neutral fallback | Explain actual access/readiness; never infer completion | Existing appropriate action |

With today's 18 registered missions, all 18 first-clear bits equal `0x3FFFF` (262143). This is a useful current-state fixture, not a permanent shipped magic number. Derive the required set from the registered, released playable catalog/progression contract and update it as missions are added. AvailableMissionMask means progression availability in the current projection; clearing its unlocked subset alone does not mean the campaign is complete. Do not filter unowned or missing required missions into a falsely completed campaign. Pending resume must retain its Continue/mission destination.

Hide the chapter/mission-number row in either completion state. Keep Campaign label and completion heading visible. Operations, Skirmish, Commander and other existing routes retain their usual disclosure and behavior. Do not promise another chapter's release, show invented unlocks/rewards, or add a timer.

For full completion default copy: “The city is rebuilding. Your command made the difference.”
Use the safer paired district captions from the manifest at today's end of available content.

## Rotation and text

Select one complete scene/caption pair when home becomes active. Keep it unchanged during that visit, including LateUpdate, balance refreshes, locale changes and opening/closing an overlay. On the next home visit, choose from the other two pairs so no immediate repeat occurs. Session memory is sufficient; persistent profile selection is optional and should follow existing save practices if used. Never randomize art and caption independently or use unrelated mission brief/comms/debrief art.

Bind the English/Persian entries in the manifest through the existing localization catalog builder. Retain native Persian fonts/RTL handling; do not render translated text into images. The longer “ALL AVAILABLE MISSIONS COMPLETED” title needs measured autosizing or wrapping at mobile widths, with no collision with the primary button or faces.

## Actions

**Choose Mission:** open Campaign selection for replay/review. Re-read progression and access at activation. Do not deploy, skip narrative, start ARIA, award currency, or issue troop orders.

**Story Archive:** give access to completed story content only, with a working return/close path to home. Re-check the completion mask before playback.

Current limitation: `CampaignOperationsScreenView.OpenRadarGuideArchive` only handles Chapter II's Gridlock replay and M03's guide. Routing this new shortcut to that existing button does not implement a general archive for the completed campaign.

Useful integration points:
- `Assets/Game/Scripts/UI/Screens/MainMenuCampaignCardView.cs`
- `Assets/Game/Scripts/Editor/MainMenuV3PrefabBuilder.cs` (`BuildCampaignCard`, `BindCampaignPlates`)
- `Assets/Game/Scripts/Editor/V3UiLocalizationCatalogBuilder.cs` (`ImportHomeStrings`)
- `Assets/Game/Scripts/UI/Contracts/UiCampaignMissionModels.cs` (completion masks and pending resume)
- `Assets/Game/Scripts/UI/Shell/Ecs/UiCampaignMissionProjectionSystem.Catalog.cs` (authoritative projection)
- `Assets/Game/Scripts/UI/Screens/FutureMissionComicPreviewController.cs` (existing narrative-canvas playback)
- `Assets/Game/Scripts/UI/Screens/FutureMissionComicCatalog.cs` (authored later-mission and bookend catalogs)
- `MenuBootstrapView.CampaignMissionNarrativeConfigs` (existing earlier mission sequences)

Reuse the existing native narrative canvas and playback controls for story replay. A completed-story chooser should expose only cleared mission sequences/fully cleared chapter bookends, and gate Chapter V/epilogue until earned. The preview catalog contains future unplayed stories; do not expose the whole catalog as completed history.

## Validation to complete in the implementation lane

- Native EN/FA home at 1920×1080 and 2400×1080; long title, enlarged text, safe margins, unchanged portrait identity.
- All three artwork/caption pairs; faces survive actual hero crop; captions remain paired after locale changes.
- Fresh/partial progression and missing plate stay on normal mission/fallback; last required uncleared mission does not show completion.
- All 18 first clears show today's available-missions-completed state, including saved-profile reload; pending resume retains its appropriate mission action.
- A full authored-final-mission fixture shows Campaign Complete without treating today’s 18-mission endpoint as the full ending.
- Normal input for Choose Mission, replay review/back, Story Archive/close/back, Operations and Skirmish. No unintended deployment, ARIA activation or settlement.
- Returning home rotates once without immediate repetition; refreshes and overlays do not rotate.
- Any executeMethod/prefab/capture runs use RTK around `Tools/CI/invoke_unity_macos.sh`, with Hub open/signed in, explicit logs/timeouts and required pass markers. Use its supported `--reuse` mode for the existing Editor if appropriate. Preserve full logs; never bypass wrapper or pass batchmode.

Report visual review, automated checks, normal-input journeys and real player/device acceptance separately. Fixture captures alone do not establish campaign readiness.

## Evidence delivered by this package

- Direction review: original completion concept approved by the user.
- Asset QA: all three generated outputs visually inspected; text/UI/watermarks absent, consistent faceted comic style, readable lower-left overlay region, correct 1672×941 dimensions.
- Source changes: experimental integration edits were removed after the user clarified ownership; affected runtime source files have no diff from this task.
- Automated/native/normal-input validation: pending with the main-menu implementation lane. No Unity authoring or validation execution was performed for this package.
- Generation prompts: [generation-prompts.md](generation-prompts.md); previous full concept prompt is in [Mockups/home-campaign-complete-v01.md](Mockups/home-campaign-complete-v01.md).
