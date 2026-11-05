# Campaign A — chapter and mission comic thumbnails

Owner accepted Campaign A and requested comic thumbnails for every chapter and mission, reusing the existing main-page Campaign comic work. This is the refined visual mockup.

![Campaign A with chapter and mission comic thumbnails](campaign-A-chapter-and-mission-comics.png)

- Five chapter choices on the left each have a distinct comic thumbnail.
- Five missions for the selected chapter each have a comic thumbnail, exact mission name and progression state.
- The selected mission has a larger matching comic preview and visible purpose.
- Operations-style connected chapter zones, shared Back button, typography, panel backgrounds, borders, colors and command sizes remain the accepted design language.

Native artwork must reuse the exact corresponding mission comic source already bound on the main page (see `MainMenuV3PrefabBuilder.BindCampaignPlates` and `MainMenuCampaignCardView.MissionPlate`). For chapter art use its own intro/bookend comic or a representative comic from that same chapter. Never borrow another mission's story to fill missing media. The generated concept uses actual comic references; native asset identity/coverage must be checked separately.

Skirmish selection: the owner liked the [scrolling objective-region mission screen](../selected-A-iteration-02-two-step/04-missions-scrolling-regions.png), following a separate map-selection screen. Each region presents six large mission choices; four regions cover24 per map, five maps cover120 catalog slots. Keep actual ready states and ledger medals.

[Exact prompts, source references and PNG hash](prompts-and-manifest.json). This request produced mockups; no runtime UI source was edited. PR70 remains draft and unmerged.
