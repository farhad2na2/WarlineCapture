# Skirmish building selection — 2026-09-18

## Report and causes

Friendly buildings sometimes ignored taps, with no outline or selection panel.

- The outer building-click handler discarded taps while any unit pathfinding job was pending. A live mouse-release trace reproduced this during normal Skirmish simulation.
- In explicit Select mode, the later unit-input phase did not honor the consumed-click flag set by building selection. It could replace the just-selected building with a nearby cargo truck.

## Changes

- Building selection no longer rejects taps based on unrelated pathfinding activity.
- Its grid query reads GridConfig only; it does not acquire the mutable road buffer used by pathfinding.
- Select-mode tap handling honors building ownership of the release. Rectangle selection and explicit Move/Attack/Scan/Board target handling retain their existing paths.

## Validation

- 23 building/marker regression tests passed, including enemy ownership restrictions, destroyed-building marker removal, the pending-path selection regression, and reading the selection grid while a job owns the road buffer.
- 11 input cases passed, including both normal and explicit Select-mode consumed releases, UI-release recovery, command targeting and camera dragging.
- Live Editor Skirmish uses an isolated save and real Input System mouse press/release events through the existing gameplay probe. No direct building-selection calls are used for the final replay.
- Final live replay: 15 consecutive Select-mode taps selected the correct friendly building across all five types, with the expected outline and panel. Five additional normal taps verified switching from a selected soldier squad and between buildings.
- Physical Android testing is not part of this check.

See the test logs and live tap results in this directory. The test Editor is restored to its original Edit-mode scene after validation.
