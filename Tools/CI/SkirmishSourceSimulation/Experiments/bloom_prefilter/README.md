# Source-only Bloom prefilter experiment

This experiment generates a byte-identical baseline copy and an opt-in Bloom-filter candidate from `Adapters/122_OwnedSpatialMultiMap.cs`. It does not edit or change the default adapter. The filter is used only for `int` keys; every other generic key type uses the original lookup. Filter state is shared with map aliases, updated on every insertion, and retained after `Dispose` clears rows, so stale bits can only cause extra lookups.

Generate reproducibly with `python3 generate.py --repo <checkout> --out <fresh-output-dir>`. The generator returns source and output SHA-256 pins. `prepare_manifest.py` creates an alternate manifest from the current default manifest; it does not add the experiment to that default manifest.

Run `./run.sh <fresh-output-dir>` to generate both variants, prepare the alternate manifest, and compile and execute the same focused C# controls and miss-heavy microbenchmark against each generated copy. The harness checks negative keys, duplicate insertion and row order, a missing key, generic non-int fallback, and a copied alias after disposal. The benchmark uses 12,000 inserted keys and 1.2 million absent-key probes per variant. Results are local microbenchmark evidence only; they do not establish full simulator performance.

The benchmark is currently exploratory: the Bloom candidate may be slower because checking two bits adds work before the dictionary lookup. Keep the implementation opt-in unless a representative end-to-end workload demonstrates a gain.
