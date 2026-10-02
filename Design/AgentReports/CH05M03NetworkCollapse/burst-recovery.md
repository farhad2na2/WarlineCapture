# Burst assembly-hash recovery

The same Game.Runtime metadata tokens resolve to different methods and members in Editor, player and preprocessed assemblies. The unresolved type hash is compiler metadata, not evidence of an AISquad gameplay defect.

Clean script compilation and Scriptable Build Pipeline cache purge did not clear the separate macOS AOT hash cache. packed-content-05.log still failed qualification. Native compiler subprocess output uses ordinary log messages, so the packaging observer now catches the Burst error text regardless of log severity and listens on all logging threads.

Recovered the compiler client using Unity’s public CompilationPipeline.codeOptimization Debug/Release cycle, waiting for compilation/reload and checking scriptCompilationFailed. Burst stayed enabled. Preserved only Library/BurstCache/macOS-Arm/Hashes (210 rebuildable .bhc files, 71,714,404 bytes) at /private/tmp/warline-burst-macos-arm-hashes-20261002 after the packaging wrapper finished, allowing the next macOS AOT build to regenerate its hashes. Editor JIT caches, project assets and other platform caches remain intact.

No Editor or Hub was terminated. No voices were sent. Final fresh native content qualification passed in packed-content-06.log: wrapper exit 0, both native content and bridge pass markers, zero Burst internal compiler errors, 105,825,524 entity-content bytes. Original Release mode restored and C# compilation succeeded. Complete normal-input mission runs are separately recorded in review-readiness.md; real player/device acceptance remains pending.
