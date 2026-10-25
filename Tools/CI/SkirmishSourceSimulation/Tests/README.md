Finite admission regression controls

Run Python profile execution-epoch regressions with:
`python3 -B -m unittest discover -s Tools/CI/tests -p test_skirmish_source_profiles.py`

MetadataBindingControls.cs is a noWorld executable fixture. Compile it with the current isolated source build compile.rsp, replacing only -main/-out and adding this file, then run with arguments `<pinned-initialization-json> <source-repository> <new-result-json>`. Keep the current build placement DLL/Newtonsoft/engine references in MONO_PATH. It uses the original compiler and all three production reader mechanisms and rejects wrong-target bindings, altered dependencies/metadata/provenance. It requires the exact already-qualified native metadata descriptors, never arbitrary test-world state. A fresh result filename gives the script-GUID negative a unique isolated directory.

Passing these controls does not establish a mission outcome or native player readiness.
