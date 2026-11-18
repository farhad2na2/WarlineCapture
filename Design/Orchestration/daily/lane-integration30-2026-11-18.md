# Lane 30, 2026-11-18

- Waited for the owner's Codex Editors (never stopped); then merged missions/heli-player-capture (--no-ff). Compile clean, python 149 OK.
- Passed: AircraftFollowFraming 5, CameraFraming 10, EditMode AircraftProductionFocus 14 and RigidAircraftGeometry 20.
- Failed (not retried, Codex 040): SkirmishMeasuredRotorAircraftFramingTests, "Actual source dependency changed" (SkirmishCanonicalAircraftGeometryAuthoring.cs:89). Likely installed aircraft metadata hash drift, not caused by the merge (unverified).
- Failed twice: S026 helicopter capture, reason=aircraftFollowNotApplied after 10 s, helicopter never airborne (lane30/heli.log:3159, heli2.log:3168). Follow-camera fix is therefore NOT verified by capture. Frame review, flight.jsonl checks and old-frame deletion not done.
- Possible cause to look at: the capture's flight-start touch input did not produce takeoff (airborne=0, moveTarget=0); not diagnosed.
