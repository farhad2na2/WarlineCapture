# Placement camera handoff — 2026-09-16

The Build drawer could close with the preview outside the camera. Placement startup queued a smooth focus, but the input guard that keeps placement dragging from moving the camera also clears smooth focus on pointer release. The release from the drawer action could cancel the startup focus before it reached the preview. A second conflict occurred when closing the drawer cleared BuildModeActive while placement was still pending. The normal-mode camera update then started another height, pitch, yaw and FOV transition, moving the preview away even after initial centering. A temporary call-stack trace identified RtsSelectionRuntimeCameraSystemHelper.UpdateZoom as the writer. The regression reproduced this with unchanged footprint coordinates but camera pitch changing from 60.04° to 48.71° and FOV from 50.03° to 42.10°.

Placement entry now completes the camera handoff immediately, clears stale focus/zoom targets, and centers the actual snapped/rotated footprint in the playable viewport. The viewport excludes the placement footer and right-side ARIA/minimap rail, with room for the resource header. The fix applies to shared building placement, not only M3. Both normal-mode and build-mode automatic camera updates yield while a preview is pending; this ownership follows the actual placement, independently of the drawer state. Dragging still owns the pointer and keeps the camera stationary.

The previous QA replay checked the preview after dragging. A new assertion checks it immediately after the drawer action and again when placement is visible, before any drag. The replay starts from a deliberately distant camera with a stale smooth target.

Validation passed through the repository macOS wrapper in the isolated QA project. The Farsi M3 replay used real Build/card/Place/Confirm button handlers and Input System pointer drag events.

- Six distant perspective/orthographic camera poses: footprint centered within 0.001 normalized viewport units.
- Initial drawer close and next placement frame: preview remained at viewport (0.38, 0.57), with unchanged camera pose despite a stale focus target.
- Pointer drag, 1.5-second hold and release: camera stationary, green placement confirmable.
- Road gate rotation and placement: real confirmation advanced to squad movement.
- Captures inspected: `placement-initial-centered.png` and `tutorial-build-confirm.png`.
- Wrapper exited 0; concise pass markers saved in `validation.txt`.

This validates the placement handoff and confirmation path, not a new full-mission completion run.
