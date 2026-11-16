# S026 alias layout handoff

The Standard/Regular Rally and waypoint drafts are unqualified source-only
hypotheses. They are not compiler route rows or a physical-source capture.
Their inputs and limits are recorded in the respective receipts under
`Logs/SkirmishProduction/s026-standard-rally-pad-draft-20261116-r1/` and
`Logs/SkirmishProduction/s026-r41-owner-waypoint-draft-20261116-r1/`.

The existing S026 source-authoring flow checks the frozen base layout before
applying its separately pinned declared candidate. Preserve that baseline and
its fingerprint. The current allowlisted candidate and its compiler context
refer to a different derived layout; neither establishes admission for the new
alias draft. `SourceDeclaredMissionConfiguration.ApplyS026DerivedDiagnosticCandidate`
and `RequireLayoutSelection` require an exact candidate, selected tuple, source
identity and derived semantic fingerprint. The compiler and structure-instance
mapping checks remain in force.

For the new candidate, first finish the typed layout and its route inputs and
obtain a fresh compiler context for that exact derived fingerprint. Then add a
narrow exact-hash/provenance branch to the declared diagnostic path as needed;
do not replace the frozen base fingerprint or reuse the old context target.
The resulting source diagnostic remains unqualified until its separate physical
source, live placement and mission acceptance checks pass. The user requested
that the Unity editor remain available, so defer any required capture until it
can be performed without taking over that editor.
