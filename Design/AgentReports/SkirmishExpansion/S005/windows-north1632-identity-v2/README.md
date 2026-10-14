# North1632 identity-aware successor — outside Assets, dispatch disabled

This is a new source revision for future replacement of the already imported
observer. It keeps the same14 global1632 types and `Run`/`RunValidation` methods;
do not import it beside the old file. Replace the existing imported observer
only after the root releases the active native freeze, then recompile/load and
freeze fresh source/build pins. The original `windows-north1632-draft/` remains
preserved with its old SHA and historical evidence scope.

New observer SHA256:
`2237d7d0a6310e13cdbe0bbf4e331563c42f0945bf4225aba2716ea8b83e6276`.
The only additional native facts are actual `setup.ContentVersion`,
`setup.SetupHash` as uint32, and `setup.ContentHash`, captured directly into the
single `ActualResolvedBinding` row. Locale already comes from actual native
localization in `Append`; no requested job value supplies those facts.

`Tools/CI/skirmish_native_identity.py` owns canonical schema1. Setup identity
includes seven configuration fields, actual native uint32 SetupHash and native
content identity. Content identity has catalog/definition/version/native content.
Their canonical UTF8 SHA256 digests are distinct from the uint32 itself. Do not
pad the native integer into64 digits or invent all-zero identities.

The north qualifier now joins both job digests to these actual gameplay binding
facts and all exact native configuration fields. Missing old-observer fields,
unknown actual locale, wrong integer type/range, mismatched version/content or
different job hashes reject. The Windows runner refuses metadata that does not
declare identity-aware native capture. The old source remains valid only for
its documented historical/load-only scope, not a new counted gameplay trial.

## Fresh job packet

The qualification worker owns the shared no-Play compiled-setup census exporter.
Use its genuine checked native export of packaged S005 Standard/Regular155926
and actual FA context; locale restoration/no-prefs behavior must retain its
separate scope. The required envelope is exactly schema1,
`collector_kind=SkirmishNativeCompiledSetupCensus`, `native_setup_identity`,
`actual_native_locale`, and `native_config_sha256`. This pre-run compiler census
is not runtime gameplay evidence; the gameplay observer must independently
match it.

After grouped imports and metadata stabilize, capture a complete fresh source
and DLL snapshot. Do this only when the native lane owner permits byte hashing:

```powershell
rtk proxy python Tools/CI/s005_windows_source_snapshot.py --capture-only --project 'D:\Projects\WarlineCapture' --output '<fresh source-build.json>'
```

Then construct an unqualified job packet from actual inputs:

```powershell
rtk proxy python Tools/CI/prepare_s005_north_job_packet.py `
  --census '<genuine native census.json>' --source-snapshot '<fresh source-build.json>' `
  --governing-packet '<existing governing mission packet.md>' `
  --preparation 'Design/AgentReports/SkirmishExpansion/S005/windows-north1632-identity-v2/PREPARATION.json' `
  --installed-observer 'Assets/Game/Scripts/Editor/Skirmish/S005NormalNorthFlank1632Observer.cs' `
  --collector-source '<actual imported census collector.cs>' --census-log '<checked census log>' `
  --census-pass-marker '<exact reviewed native exporter marker>' `
  --job-id '<fresh unique job ID>' --output '<fresh packet directory>'
```

The builder checks comprehensive source/DLL sets and bytes, reviewed installed
observer identity, pinned collector source, actual native configuration, current
governing packet and checked wrapper/pass markers. It derives both hashes only
through the shared protocol and runs queue preflight. It does not enqueue,
claim, launch Unity or qualify a mission. `INPUT_PROVENANCE.json` preserves all
input/artifact hashes. A caller-created census or old passed log is not
authenticated native provenance; retain the actual wrapper/exporter evidence
and source/build capture. Runtime identity matching remains mandatory.

## Camera and acceptance

Every mission capture must show readable infantry rather than dots/impostors,
while aircraft fit comfortably on screen. Neither an excessively near nor far
camera is acceptable. The current native image at camera height65 needs review;
its height alone is not a pass. The qualification worker owns camera/LOD audit.
Capture opening, selected infantry, aircraft, ARIA controls, result and return
as the applicable journey expands; send screenshots asynchronously. Owner
review, normal-input technical proofs and real device/player acceptance stay
separate. No camera/LOD acceptance is claimed by this source revision.

Offline checks: five north qualifier tests, four packetbuilder tests and four
real plan-mode portability tests passed. These are not new native compilation
or gameplay passes for this observer revision.
