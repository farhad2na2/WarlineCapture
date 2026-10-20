# Remaining ground BA pilot input specs

S050 (MP / BA / G / E), S097 (AP / BA / G / F), and S098 (AP / BA / G / E) are disabled binding specifications for the existing generic Request/BuildBatch API. S026 remains Windows-owned. No mission-specific builder is proposed.

All nine canonical 62-field size dictionaries, full manifest rows and seed families are preserved at dd4a5bca3ed3c8988efc51e4a37ecdfe49890b5d. Their values equal the original Full120 report rows; no gameplay budget was changed. The prior conformance audit is immutable evidence at 5f389ada216c748aa546b7893429f31a7208ce44; its canonical transport successor f8ee7e9b75b2715d9526a4628326f62a97d360e3 preserves the original CRLF manifest failure. No prior acceptance transfers.

Historical candidates are CH04M05ArmorBreak for S050 and CityEdgeAirfield for S097/S098. Source metadata frames are reference evidence only. Active CampaignSource and every ExpectedSource pin remain null until a fresh explicit source choice; MeasuredLayout and missing mission measurements remain null. Existing S049/S074 measured-layout aliases are not reusable substitutes for the API-required MP/AP logical identities. Empty historical missing-footprint lists do not prove fit.

The AP definition is LFS-backed: source-pins binds committed pointer bytes; source-evidence preserves its small hydrated UTF-8 payload verified against the pointer SHA-256 and size. Grid/surface hashes in the historical report are attributed references, not newly verified physical dependencies. No scene/grid/surface tree was read or hashed.

The specs describe path/GUID resolutions into persistent Unity objects, not executable JSON or an implemented loader. API admission, successful compilation, physical source fit, normal-input play, ARIA, result/return, paid-resource and queue controls, APK, and device/player acceptance remain separate pending gates. No Unity, compiler, native run, process control, Library modification, catalog publication, or geometry authoring occurred.

Verification is static evidence integrity only. Run verify.py with a committed revision to check raw Git payload hashes, exact base pins, canonical row preservation, null missing measurements, exclusions and LFS payload identity. The manifest excludes itself; its external hash belongs in the handoff receipt.
