# Command Node Burst recovery

Packed content attempts 01 and 02 failed closed on an unresolved compiler type hash while hashing the newly added Command Node state read in TryUpdateCommandNodeGuidance. Both compiled Editor/player component assemblies contain that type. Native metadata tokens differ between those assemblies; the AISquad entrypoint list does not identify an AISquad defect.

Unity SBP BuildCache.PurgeCache(false) did not resolve the in-memory assembly store. Used public CompilationPipeline.codeOptimization Debug/Release reload cycle, preserving Burst enabled and the original Release setting. The macOS AOT Hashes directory was already absent, so no additional cache deletion or movement was performed. No Editor, Hub or licensing process was terminated.

Earlier fresh native packed qualification passed in packed-content-03.log, wrapper exit0, 98,549,213 Entities content bytes, zero Burst internal compiler errors and zero C# errors; original Release mode restored, Burst enabled. Complete normal-input validation and real device acceptance are separate gates.

## Final cover check compiler correction

Packed-content-04 failed closed with BC1016 on FixedString64Bytes.ToString in the newly added armored-cover helper. This was a real unsupported managed conversion, separate from the earlier stale metadata hash. Both cover helpers now compare fixed-string ASCII bytes directly, preserving the case-insensitive canonical tank match and all live/armed/stopped checks. Existing native EN and FA full journeys remain recorded before this equivalent key-comparison correction; focused-validation-05 passed the corrected rules and packed-content-05 passed with wrapper exit0, 98,546,587 Entities bytes and zero Burst/C# errors. Burst remained enabled.
