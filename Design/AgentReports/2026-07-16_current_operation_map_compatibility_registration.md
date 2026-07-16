# Current Operation Map Compatibility Registration

Date: 2026-07-16
Status: Accepted loader-neutral current-map registration
Source revision: `762e927d016ca43b82db585d4bef3164e86dd22a`

## Registration Identity

- Operation-map id: `opmap.skirmish.desert_base_01`
- This record binds the id to current compatibility content only.
- No scene, asset, manifest, generated chunk, build setting, or runtime binding
  changed during capture.

## Source Scene And Subscene

| Role | Path | GUID | SHA-256 / structural SHA-256 |
|---|---|---|---|
| Canonical source scene | `Assets/Game/Scenes/Match.unity` | `cc4f48a57793d4597b4ffac2906c515e` | file `dca7c83b765ce40099ce4fd62a53cbee5bc306107f8a026abcb941a59bf53a46`; hierarchy `2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900` |
| Optional ECS subscene | `Assets/Game/Scenes/Match/MatchSubScene.unity` | `8d5e3c3f2ef84b61a4d61472c40c9a11` | file `bcc255f3fb140a0d91687b45b679b47fb60f01f5cfa8690bac3032ec642dadd8`; hierarchy `1eece17878927ce653d2b828b044f0b8b9ba8a0be1b45a6041bad89034b064b9` |

The existing `MatchSubScene[1]` reference remains `AutoLoad=true`. This is
compatibility evidence, not a decision about the eventual scene loader.

## Static Presentation Product

- Manifest: `Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset`
- Manifest GUID: `2d7b3d165106141ba81b98138bb8fa7f`
- Schema: `1`
- Canonical dependency hash: `db252d7b61b87458dafbd30acb8a5559`
- Content hash: `9eebc7c8aa774d5f505cb684099d133a`
- Manifest SHA-256: `3940dcac3d42c703f47cf11f134b183c4554f9944629925f7b38957e08d93746`
- Chunk size: `32`; chunks: `514`; sources: `16,542`
- Scene aggregate SHA-256: `8bb8a22383f72361724602e3e888371867602a49c45ef203f0a2c77801d6fb4c`
- Scene-meta aggregate SHA-256: `5e9e4f2f826f4c9655ff1f4ed41651eef6ff18aeedfa5b5a30add4fcd00a713d`
- Combined generated-output SHA-256: `574afec991fbc1a684531c9f727c20eb296271260e7a4e1c4a8c300a2b642e79`
- Integrity ledger and disk/manifest file sets have exact `514 / 514` parity.

Schema-v1 remains the accepted compatibility product. Map-specific schema
migration and delivery-provider binding remain later tasks.

## Grid And Surface Content

| Role | Path | GUID | Current identity |
|---|---|---|---|
| Grid | `Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset` | `b201000000000000000000000000000b` | `2048 x 1024`, cell size `1`, origin `(0,0,0)`, SHA-256 `8ef1b3f17074774040111a48ea82901b3355da8b8b86c8dc5c6e2a0bcccc2cfb` |
| Surface/height | `Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset` | `12f517deb32ab49698acbfdaf7c3eac7` | `2,097,152` surfaces, payload v3/encoding 1, runtime hash `8f661e49fcdbb96314ff03d48bbb3993`, SHA-256 `aa08cb9115e8727bfdbc671a4a2cfd9334ef48134c00d58d7d29e350c45b752c` |

## Placement Content

Building and vehicle registration is authoritative in
`2026-07-16_current_operation_map_placement_registration.md`: 451 building and
29 vehicle placements with exact asset GUIDs and identity aggregate hashes.

## Validation Evidence

- External report: `/private/tmp/opmap-current-baseline.json`
- Report SHA-256: `a18761d6ee1faceb1543d0752c5c6e7c82e0279a4c4aaa0f77185a1eae80fda3`
- Unity log: `/private/tmp/opmap-current-baseline.log`
- Probe marker: `result=Passed chunks=514 sources=16542`
- Unity exit: `0`; compiler errors: `0`
- Project worktree after probe: clean

This registration provides exact current identities without choosing
Addressables, runtime scene generation, remote content, or physical map rollout.
