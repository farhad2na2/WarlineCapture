# SCN-09 Build Drawer Popup - Iteration 01

Status: Satisfied for current pass.

## Slice 01

Rescaled the popup from a small right-weighted drawer into a large centered Target Lock modal:

- Enlarged and centered the popup frame.
- Changed the title text to `BUILD DRAWER`.
- Increased title, tab, detail, queue, and button typography.
- Rebalanced the left catalog and right detail/production columns.
- Added hover/focus/press lift and scale treatment to tabs, catalog cards, production actions, build action, and close button.

## Slice 02

Fixed the transparent popup body:

- Added dark frame fill behind the chrome-only drawer frame.
- Added card fill behind catalog chrome so transparent frame art does not expose UI Builder checkerboard.
- Added explicit scroll content sizing.

## Slice 03

Confirmed the catalog still collapsed because `ui:Instance` wrappers were not accepting the intended card geometry in UI Builder:

- Added explicit slot classes as an intermediate test.
- Revalidated in the shadow UI Builder preview.

## Slice 04

Replaced the collapsing catalog template instances with explicit runtime-bindable buttons:

- Preserved the runtime item names: `ItemView`, `ItemView_1`, `ItemView_2`, `ItemView_3`, `ItemView_4`, `ItemView_5`, and `ItemView_6`.
- Preserved child names used by the binder: `Thumb`, `Title`, `Role`, `CreditsTinyCost`, `SuppliesTinyCost`, `TimeTinyCost`, `Icon`, and `Value`.
- Added differentiated static thumbnails and readable card labels/costs.
- Kept runtime compatibility because `UiToolkitShellView.CacheBuildDrawerCatalogItem` accepts each named item directly as a `Button`.

## Slice 05

Polished the readable static pass:

- Increased catalog card height so the first catalog row reads as the primary surface.
- Kept a small secondary-row reveal as scroll depth rather than a collapsed text defect.
- Verified detail and queue panels remain readable and aligned.

Validation:

- Synced `SCN09_BuildDrawerPopup.uxml` and `.uss` to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Visible shadow UI Builder preview opened from `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Match Game View was enabled and Fit Viewport was clicked before captures.
- No PlayMode or runtime Game View validation was used for this pass.
- `git diff --check` passes.

Artifacts:

- Baseline fitted UI Builder capture: `shadow_ui_builder_scn09_baseline_fit_window.png`.
- Slice 01 capture: `shadow_ui_builder_scn09_slice01_window.png`.
- Slice 02 capture: `shadow_ui_builder_scn09_slice02_window.png`.
- Slice 03 capture: `shadow_ui_builder_scn09_slice03_window.png`.
- Slice 04 capture: `shadow_ui_builder_scn09_slice04_window.png`.
- Final current-pass capture: `shadow_ui_builder_scn09_slice05_window.png`.
