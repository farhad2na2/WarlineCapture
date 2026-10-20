# Single Game.Editor edit: incremental compile evidence proposal

Review only, based on `ad3a252e98c33391b4b58a64fdb4b05bc3753513`. No adapter or validator implementation, project changes, compiler/native execution, SDK/RSP/output captures or blanket prior-build acceptance. Windows remains integrator and native owner. The selected source pins and read-only Mac ownership snapshot accompany this report.

## Recommended smallest defensible scope

Use the normal incremental Unity compilation path for a future source-frozen single Game.Editor harness edit, conditional on a trusted complete prior baseline and measured Windows dependency graph. Freshly compile the changed owner and affected consumers required by the applicable evidence contract. Reuse upstream and unrelated assemblies only when their complete current sources, raw RSPs, references, SDK/analyzer/configuration inputs and native output lineage match a genuine successful origin. Changed reference bytes demand fresh consumer evidence.

The 40 committed Assets asmdefs give the provisional reverse closure `Game.Editor -> Game.Tests.Editor`. This is a planning hint, not the actual Windows native192 graph: package/default/generated owners, platform/defines and actual RSP references may add consumers. Neither two fresh compiles nor 190 retained assemblies has been measured for a future edit. Use the actual graph, not the provisional count.

Windows subsequently reported a complete frozen historical192 graph from map SHA256 `39955f869788e74c576fb85eb465fe195f9727f539638a4018d2f0799bbfeb1a`: ProductionBatch.cs has sole owner Game.Editor, and the measured historical reverse closure is Game.Editor plus Game.Tests.Editor, with Tests referencing Editor. This is coordinator-attributed historical evidence, not a Mac capture or future admission. The new epoch must recompute it.

The historical map uses lowercase Windows canonical input keys while owner names preserve case. Windows preserved its first projection unaccepted after it missed that join, then used an explicit case-normalized DLL/refDLL basename join in separate r2. Keep this distinction in any proposed graph calculation. Native admission reads raw RSP declarations; no equivalent parser defect is asserted without exact raw-path proof.

The Mac census observed Fig/Fig Editor PID51962 (parent1, start Tue Oct20 12:41:34 2026) and Hub-related processes; no native job belongs to this chat. This is ownership context only, not capacity or launch authority. No process action was performed.

## Existing contracts must remain intact

`native_compile_closure.py:74-95` enforces required owners <= numeric Csc owners <= allowed owners, exact owner/output union and strict whole-window equality of ALL declared inputs. Allowed output changes do not waive reference drift. Capture records raw RSP/source/reference/analyzer/additional/config inputs and both checked SDK tools. Unknown RSP options refuse. Actual native output artifact paths must match exactly; same hash elsewhere is insufficient. The native result must seal the same-window descriptor; no later backfill (`:97-114`).

`skirmish_native_compile_admission.py` requires direct changed-source owners fresh and derives allowed transitive consumers from actual RSP references (`:59,78-89`). Required-fresh and allowed-affected sets differ: do not claim every allowed owner compiled. Preserve actual overlay/source/build identities, operational grant/watch/shutdown/settings checks, complete owner maps and terminal/census gates.

`composite_compile_evidence.py:35-36` and its final source-delta check require fresh changed-source reverse closure and forbid retaining a dependent of any fresh owner. Its fixed 24-owner offline contract is not a native192 reuse schema. Prior successful origin chains, exact current/origin source membership, references, SDK and raw RSP pins remain necessary. Do not generalize or weaken this contract by calling it an optimization.

If a Game.Editor implementation edit leaves its consumed refDLL bytes unchanged, Unity may skip a consumer. Record the actual skip; the current composite reverse-closure gate still needs fresh evidence. A separate reviewed per-consumer reuse contract could be proposed later, but is not implemented or accepted here. If consumed references change during the global window, the current native checker must stay Pending even when both producer and consumer compiled. A clean rebuild alone does not solve that observation-window problem.

A genuine future per-consumer design would bind immutable producer completion/output-reference hashes before each consumer invocation, capture that invocation's exact inputs/RSP/SDK, then verify outputs and stability with ordered receipts. Event callbacks alone do not prove compiler file consumption. Do not retrospectively invent snapshots or normalize away raw RSP differences.

## Supported Unity API boundary

Unity 6000.5 documents `RequestScriptCompilation()` and its options overload, without an assembly selector. The default incrementally compiles changed scripts/settings; Unity chooses the scheduled assemblies. Successful compilation reloads assemblies. This is an API proposal, not an executed command or per-assembly force mechanism. [Unity 6000.5 API](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityeditor/compilation/compilationpipeline/requestscriptcompilation).

`RequestScriptCompilationOptions.None` retains incremental behavior; `CleanBuildCache` requests a full script rebuild. A routine one-file edit does not inherently require cleaning. [Unity 6000.5 options](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityeditor/compilation/requestscriptcompilationoptions).

`CompilationPipeline.GetAssemblies` can help obtain assembly build metadata, including sources, outputs, references, defines and compiler options. This is not the loaded runtime assembly set or evidence that compilation succeeded; reconcile with actual RSPs and logs. [Unity 6000.5 metadata API](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityeditor/compilation/compilationpipeline/getassemblies). Documentation is versioned 6000.5, not a measurement of the installed 6000.5.8f1 binary.

The inspected Pipeline package's `RecompileCommand.cs` uses `AssetDatabase.Refresh` and compilation callbacks; it supplies no per-owner force selector. Its stored `completed`/`up_to_date` status has no run identity, full owner/RSP/input evidence, and completion can precede domain reload. It cannot replace numeric Csc/consumption evidence or prove post-reload reconnect. The Mac package pin needs independent Windows byte comparison.

## When full clean compilation still matters

A trusted initial baseline with complete origin evidence may require a full clean compile. Also use it when independently reviewed policy demands all owners fresh, invalidation is suspect, or complete lineage cannot be established. Unity specifically documents clean compilation for untracked RSP changes or suspected dependency detection problems in the API linked above. SDK/package/settings/define/generator changes require a newly reviewed scope and identities; expand to a full clean baseline when the affected set or retained provenance cannot be established. Cleaning does not waive exact inputs, compiler-consumption evidence, output paths, errors, terminal or operational gates.

Do not force freshness through unrelated source edits, timestamp changes, Library deletion/copying, or an external compiler labeled native. Any future execution must retain checked Windows wrappers, explicit logs/timeouts and current ownership/grant constraints.

## Acceptance and handoff

`windows-measurements.json` lists the exact remaining measurements. This review earns source-review credit only. Historical Windows192 / 15014-input / 384-output compiler observations remain attributed and immutable; the reported post-reload Pipeline bind/readiness failure remains failed. Compiler evidence, Pipeline health, normal-input mission/ARIA/result/return, source-fit, paid-resource/queue, APK and device/player acceptance stay separate. Original Mac r5/r6 failures and settings additions remain preserved; this proposal grants no native retry or restoration authority.
