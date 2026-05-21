# SCN-02 Main Menu Target-Slice Implementation Rejected

Date: 2026-05-17
Owner: PM / direct implementation
Status: Rejected

## Rejection

The previous direct implementation used target-reference panel slices as runtime UI surfaces and then reported a low image-diff score. That was not a professional UI canvas implementation.

This approach is rejected because:

- It used baked mockup/reference slices instead of building the screen from reusable layered UI assets.
- It did not exercise the intended 9-sliced frame assets as production components.
- It relied on transparent hitboxes over baked panels instead of proper visible button/panel hierarchy.
- The visual metric measured a shortcut, not the quality of the actual UI implementation.

## Corrected Direction

Runtime SCN-02 must be built panel by panel using:

- `screen_shell_frame`
- `top_resource_strip_frame`
- `resource_counter_frame`
- `profile_block_frame`
- `side_route_button_frame`
- `mode_card_frame`
- `footer_status_frame`
- icon/content art layers
- live TMP labels/counters/body copy
- real button hierarchy and states

Target-reference images may be used only as visual comparison references, not as runtime UI surfaces.
