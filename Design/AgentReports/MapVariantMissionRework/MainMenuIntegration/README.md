# Latest main Campaign menu integration

Updated 2026-09-30 on `codex/airlift-airfield-review`.

Merged `origin/main` at `2c36f349f` in commit `c8665ab14`. Includes District Atlas Campaign UI, comic header, live Main Menu comic backdrop binding and Farsi navigation fixes. The Campaign prefab conflict was resolved with the complete latest-main prefab; the staged prefab was byte-identical to main. Mission preview GUID bindings were checked against the prior branch and preserved.

Native verification used `Tools/CI/invoke_unity_macos.sh` with GUI licensing and an explicit 600-second timeout. Wrapper exit 0; `[CampaignOperationsScreenValidation] result=Passed tests=4` verified in the full retained log. Checks cover Menu scene Campaign binding, shared header preservation, Campaign entry/actions, Back navigation and prefab hierarchy/art binding.

This is automated integration evidence. A fresh visual review of the latest menu and complete current-map mission journeys remain separate gates. Prior mission screenshots show the older menu and are not visual evidence for this merge. In-progress Air Corridor and Route Reopened work was stashed before the merge and restored afterward; it was not included in the merge commit.
