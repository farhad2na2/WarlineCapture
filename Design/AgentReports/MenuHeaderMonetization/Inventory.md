# Menu/header implementation inventory

Baseline: d3d4c5824, inspected 2026-09-28. Only unrelated O002 ImageGen output was untracked initially. Concurrent Operations/checkpoint/access validation and Support evidence appeared during this task; those files are owned by their existing agents and are preserved.

## Owners and consumer fields

| Owner | Existing authority | Menu consumption |
|---|---|---|
| Persistence / mission reward settlement | SaveService, JsonSaveRepository.ChangeVersion, PlayerProfileSaveData | Read credits and saved commander identity; no UI arithmetic or save writes. |
| Campaign projection | UiCampaignMissionProjectionSystem, catalog and CampaignMissionProgressStore | Selected mission, briefing name/summary, available/completed masks. At headquarters, choose first unfinished available catalog mission; completion retains a replay selection. |
| Content access | ContentAccessRuntime / ContentAccessAuthority | Project access state through the existing gateway; ownership enforcement remains disabled under its release gate. |
| Operations remediation | Operations scope/session/checkpoint model | Preserve strategic behavior; do not compute intro or city completion. |
| Mission resource remediation | MatchHudResourceHeaderPresentation / mission restrictions | Preserve integrated Materials/Oil/Fuel and tactical constraints; no menu account balance in combat. |
| Commerce | No implemented billing/restore adapter found | Purchases remain disabled. No fake transaction, price, restore result or local ownership grant. |

## Surface map

Home: MainMenuV3PrefabBuilder → SCN02_MainMenuContent sections → CommanderProfileRouteLifecyclePresentation. Old background used one baked commander scene; old header had visible Credits/Command plus hidden compatibility resources. Campaign card scanned backwards for any available art. Footer dominated the full width.

Header defaults: MenuBootstrapCompositionSystemHelper, UiShellStateSystem, gateway DefaultState seeded 12,450 and 78/100. New read-only account projection publishes profile values with a neutral unavailable state; home binding observes the gateway.

ARIA asset: Assets/Game/Art/UI/V3Shared/Portraits/ARIA_MainMenu_V3.png. Commander selection sheet: Assets/Game/Art/Narrative/FirstLaunch/Commander/commander_portrait_choices.png, sorted commander sprites matching onboarding indices.

Store: StoreCommandExchangeV3View uses a static six-category consumables catalog and disabled checkout. Substantial store redesign requires separate actual-reference ImageGen review. No finished product/release/billing claim is permitted.

Baseline EN prefab captures are in Before/. They are native Editor fixture renders, not player-flow acceptance; baseline FA was not yet captured. Additional evidence will be recorded in the final handoff.
