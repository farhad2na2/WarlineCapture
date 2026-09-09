# APC movement review — 9 September 2026

User report: APC looks visually fragmented during movement.

The production M4 Fast APC was exercised using its actual movement and boarding owners: westbound pickup, turn, boarding four specialists, and eastbound travel to the helicopter. No vehicle transform, geometry, health or mission fact was changed for this review. The camera owner provided close and wider views; the final acceptance run used requested camera heights of 14, 55 and 140 above the vehicle. Map bounds prevent perfect centering at the widest distance.

## Observed result

The close captures show a connected hull, wheels and windows before and after selecting/loading the APC. The hierarchy snapshots show the body and glass meshes under the same moving vehicle tree. Their world transforms agree; the mid/low meshes are hidden in these observed frames, and the selected outline follows the same parent. No detached geometry was reproduced. This is bounded evidence for M4's `Unit_Veh_APC_Fast`, not a claim that every APC variant or every frame is correct.

The cyan selected outline emphasizes individual edges, but there is not enough evidence to attribute the user's report to that treatment. No speculative mesh, shader, or LOD replacement was applied.

Evidence: `VehicleQA/close-before` contains the original close captures and hierarchy records. `EditorProbe/vehicle-stage-*` contains the latest requested zoom levels. Stages 1 and 3 are the moving APC, stage 6 is the loaded helicopter waiting for clearance. `Validation/warline-m04-vehicles-01-markers.txt` and `Validation/warline-m04-final-05-markers.txt` record successful real rescue runs.

The visual report remains unconfirmed. A clip of the original artifact, or the specific APC variant and mission if different from M4's Fast APC, would let a later reproduction target the remaining gap. Android testing is excluded by the user's instruction.
