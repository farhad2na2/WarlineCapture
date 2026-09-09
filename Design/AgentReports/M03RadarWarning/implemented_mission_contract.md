# M3 implemented mission contract

Checkpoint: 2026-09-09. This records implemented decisions; formal QA status is in [the evidence index](editor_qa_index.md). Earlier production-plan proposals remain historical planning inputs.

| Concern | Implemented contract | Source |
|---|---|---|
| Identity | `saga.ch01.m03.radar_warning` / `scenario.ch01.m03.radar_warning` / `opmap.ch01.convoy_approach_01` | `M03RadarWarningConfigBuilder`, `M03RadarWarningMapBuilder` |
| Player force | Two four-member rifle squads and one loaned, unarmed Ground Radar Tank | `M03RadarWarningConfigBuilder.Roster.cs` |
| Other participants | Four civilians; finite hostile force of two armed cars, one unarmed APC and four infantry | Same roster builder; defense member buffer is the outcome authority |
| Shared physical map | Original placement/identity/content assets preserved; 22 authored vehicles and authored map defenses are dormant only within M3 | `CampaignMissionSpawnSystem.MapDefenses.cs` and `M03MapIsolationTests` |
| Sensor | Ground kind, radius 240 cells; existing Air dish remains Air-only | Canonical prefab config, `RadarPingRequestSystem` |
| Economy | Free completed Barracks, 50,000 Credits / 100 Materials; loaned vehicles do not require an unavailable Fuel feature | Resource initialization and mission vehicle self-supply authoring |
| Real purchase options | Tower 22,000/50; Barrier 6,000/15; four-rifle reinforcement order 10,000/20. All three leave 12,000/15. | Canonical build and production transactions; the prefab's raw material field alone is not the effective production quote |
| Time | Vanguard warning/activation/contact at 0/45/65 seconds; main element at 100/140/165 seconds | Existing delayed-wave owner and attempt clock |
| Camera | Begin at actual RTS pose, pan and zoom together to important areas, then return to captured RTS pose; no mission time elapses during the opening | Existing patrol/camera request owners; camera CSV and captures |
| Defense | Real movement, target acquisition and weapons. Hold permits in-range defense; Stop disables auto-engage. Convoy path orders use the existing queue. | Existing combat and command owners; acquisition regression and real Full Guidance journey |
| Victory and loss | Defeat all seven required hostiles with post/core safe. Core breach, post loss or roster integrity fault defeats the mission. Missing entities are not counted as kills. | Rule/fact/runtime tests |
| Stars | Victory, civilian safety, and no damage to the protected post; independent conditions | Mission result projection and rule tests |
| Ping | Two charges with 60 simulated seconds cooldown; actual eligible sensor required. An accepted empty scan consumes one use, invalid requests consume none. | `RadarPingRequestSystem`, actual detector and four focused Ping tests |
| Guidance | Twelve typed steps. Movement already satisfied, alternative completed actions, and contact preemption skip unnecessary optional steps. Purchases and Ping are optional. | Guidance projection, real Full Guidance Hold/Stop journey |
| Guide / all classes | Twelve topics; exactly 57 reference identities, 51 canonical configs and six unavailable future naval identities. No promise of 57 playable M3 choices. | Guide builder, complete language/aspect navigation checks |
| Languages | English and Persian, central keys, live locale switching, RTL shaping/native page digits; requested Large/Extra Large caption sizes retained | Localization builders and Editor captures |
| Story | Three opening panels, nonblocking C01 report/archive, three outcome-aware debrief panels before Victory | Seven final text-free illustrations, fourteen aspect crops, existing narrative owners |
| Rewards | First clear: 400 XP, 2,000 Credits, Tower and Radar Ping. Replay: 300 Credits. M04 becomes available but remains undeployable without its definition. | Settlement/progress store; failure and duplicate/retry tests |
| Save failure | Visible localized Retry Save; outcome presentation waits for a durable write, duplicate retry cannot grant twice | Existing settlement owner and actual atomic-write failure probe |
| Exit/retry | Remove attempt-owned runtime buildings and produced units; clear M3 roster, convoy, warning, Ping, interaction and camera records; preserve authored map content | Existing attempt/building owners; cleanup regression and live lifecycle probes |

Current observed choice/consequence: untouched idle play loses after stopping three of seven hostiles; explicit Hold through Full Guidance wins with all seven stopped and all resources/Ping retained. The current-source paid road Barrier and rearward rifle strategy wins at 229.728 seconds, with actual detours by all three vehicles. Normal-speed forward rifle defense also wins at 195.935 seconds. These are reproducible automated play observations, not player-cohort fun or learning scores.

Voice generation and listening are pending the existing explicit ElevenLabs payload approval. No scripts have been sent. Android validation is excluded by the user.
