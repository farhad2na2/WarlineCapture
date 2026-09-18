# Operations child-page navigation — 2026-09-18

Visible cyan-bordered, localized Back controls now occupy the top-left header of District Details, Command Feed, Armory, Raid confirmation, and End-of-Day Report. Armory retains its existing footer Back as well.

Page navigation pops shell history, preserving the actual parent when reached from Operations or another menu. Raid Back cancels without confirming or queueing a raid. Report Back uses the return-to-Operations callback, not Save & Continue. Builder source and generated prefabs are updated together; `ui.navigation.back` is in the localization configuration and generated catalog.

## Validation

- Five existing focused prefab, route, and popup tests passed (`tests.txt`).
- Actual pointer hit tests, clicks, and shell-route assertions passed for all five surfaces in English and Farsi (`live.txt`).
- Nested navigation checked: Inbox → Command Feed → Operations; Commander Profile → Armory → Operations; Raid confirmation → District Details → Operations. Existing Inbox and Commander Profile Back controls preserve history.
- Reviewed Farsi screenshots for all five changed surfaces; labels render correctly and Back controls are visible.
- QA used an isolated save in the authorized normal Unity Editor. No battle or Android-device testing was needed for this navigation change.

The first nested Farsi probe selected an existing unbound scene placeholder with zero action buttons instead of the visible prefab instance. The retry scopes selection to the bound page; `deep.txt` preserves that probe failure and `deep-fa.txt` records the retry result.

## Screenshots

- [District Details](fa-IR-district.png)
- [Command Feed](fa-IR-feed.png)
- [Armory](fa-IR-armory.png)
- [Raid confirmation](fa-IR-raid.png)
- [End-of-Day Report](fa-IR-report.png)
