# S005 Windows north-flank trial adapter draft

This adapter prepares one exact S005 FA/Standard/Regular/manual seed155926 trial.
It does not implement paid-air qualification or publish mission readiness. No live
trial has certified this adapter. Independent review and a connected native canary
remain required even when every offline control passes.

Files:

- `prepare_s005_windows_successor.py` reconstructs a uniquely named external observer.
- `InvokeS005WindowsNorthTrial.ps1` defaults to a plan; `-Run` is explicit dispatch.
- `s005_windows_source_snapshot.py` checks the complete source set and exact Windows
  `SkirmishCandidateSourceHash.Compute` BinaryWriter/ordinal digest.
- `s005_windows_north_qualification.py` consumes actual raw evidence and existing
  `audit_skirmish_native_contacts.audit`; it requires a genuine scoped Victory.
- `s005_windows_north_qualification_test.py` contains synthetic contract controls
  and negative controls against the preserved native1628 actual failure.

The old Mac external assembly cannot be called by a newly launched Windows Editor.
After static observer review, install its exact bytes as a unique Editor-only
`.cs` under Assets, run compile/load validation, and then freeze source/build/job.
The adapter never installs this file for the owner. Keep its ScriptAssemblies DLL
build stable between preflight and completion. `build_sha256` is SHA256 of the
canonical sorted JSON list of `{path,sha256}` for `Library/ScriptAssemblies/*.dll`;
this is a concrete assembly-byte pin and still needs the native load canary. It
must not be confused with the candidate digest or hash of a player executable.

Create a v1 job and claim with `skirmish_production_queue.py`. Its source_manifest
must include every eligible Assets/Packages/ProjectSettings source file and the
installed observer. Store the actual claim receipt; the runner rechecks it against
the canonical queue `claim.json` and `job.json`, not a caller-supplied assertion.
Use the queue-allocated artifact_directory as RunDirectory. A short shared metadata
lock verifies ownership and atomically reserves the one-shot start sentinel;
neither a retry nor concurrent invocation may overwrite that sentinel.

Example after compile/load, independent review and source freeze:

```powershell
rtk proxy powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeS005WindowsNorthTrial.ps1 `
  -UnityExe '<resolved Editor executable>' -ProjectPath 'D:\Projects\WarlineCapture' `
  -JobFile '<pinned job.json>' -ClaimFile '<actual claim.json>' `
  -PreparedDirectory 'Design/AgentReports/SkirmishExpansion/S005/windows-north1632-draft' `
  -InstalledObserver '<reviewed unique Assets/.../Editor/...cs>' `
  -RunDirectory '<queue allocated attempt directory>' -GuiLicensing
```

This command prints a plan. Dispatch additionally requires `-Run` and
`-ReviewedObserverSha256 '<exact independently reviewed candidate hash>'`.
Hub must remain open/signed in and no Editor may own the project. The checked
`InvokeUnity.ps1` wrapper owns timeout and its Unity child; no active Editor is
terminated. The asynchronous harness owns exit, so the launcher omits `-quit`.
Every attempt writes a unique full log, wrapper receipt and before/after source
snapshots. A nonzero wrapper, missing marker, raw failure, changed source/build,
incorrect actor/receipt/target, input violation, missing rendered result or missing
MainMenu return fails qualification. Preserve all failures.

The queue still rejects `finish --status qualified`: this adapter has not passed
the required native canary. Its scoped qualifier always emits
`native_qualification=pending_independent_native_review`, `playerReady=false`.
Device, unfamiliar-player acceptance and owner screenshot review remain separate.

Validation performed offline: Python compilation, PowerShell AST parsing, four
control tests, independent .NET BinaryWriter/ordinal oracle. Synthetic positives
demonstrate checker reachability only. Actual native1628 failed paid receipt1004
assumption is rejected with and without grafted synthetic success markers.

Independent qualification-pipeline agent re-review passed the bounded offline
gate after correcting identity-splice, source-ordering and queue-ownership gaps.
It independently reran all four tests and actor-swap, stage-only fresh completion,
route-only acceleration and missing-attempt mutations; each negative rejected.
This review establishes no native capture-schema compatibility or gameplay win.

Windows PowerShell portability follow-up: `Get-FileHash` is unavailable in the
host's actual `powershell.exe` environment. The runner uses local .NET
FileStream/SHA256/BitConverter hashing instead. JSON reads explicitly use UTF8;
preparation and claim files may contain a UTF8 BOM and Unicode paths. The shared
queue currently requires job JSON in UTF8 **without BOM** and rejects BOM jobs;
the adapter preserves that contract. All other PowerShell cmdlets used by this
runner were resolved under the actual executable.

`rtk proxy python Tools/CI/s005_windows_runner_portability_test.py` passed four
real plan-mode tests: known SHA256 (`abc`) through Unicode/spaced paths and BOM
preparation/claim; binary-byte SHA256 plus candidate tamper rejection; source
mutation rejection; and explicit BOM-job rejection. Dummy Unity and observer
paths were never launched or installed, no Assets directory was created, and the
native attempt artifact directory remained absent. These are platform/plan
checks only; native dispatch remains pending.
