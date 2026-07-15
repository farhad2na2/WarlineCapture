# Operation-Map Source And Content Hash Contract

Date: 2026-07-16

## Implemented

- Added `sourceIdentityHash` to identify the canonical authored or generated source payload without encoding a scene path, asset GUID, loader address, or generator policy.
- Retained `schemaVersion`, `contentVersion`, and `contentHash` as operation-map definition metadata.
- Added `generatedMetadataHash` for the accepted small generated metadata payload.
- Added allocation-free validation requiring all three hashes to be exactly 64 lowercase hexadecimal characters (SHA-256 text form).
- Integrated hash validation into `OperationMapDefinition.TryValidateMetadata` while keeping gameplay identity validation separate from source/content evidence.

## Architecture

- The fields are bounded metadata strings on the existing sealed config asset.
- The contract supports either authored or runtime-produced source data.
- No source path, scene reference, Unity object, Addressables handle, loader, generator implementation, or rendering dependency was added.
- Hashes are evidence/version metadata and never replace the stable operation-map id.

## Validation

- Unity EditMode `OperationMapSpatialConfigTests`: passed `24/24` (`/private/tmp/opmap-hashes-focused.xml`).
- Unity compilation: zero compiler-error markers (`/private/tmp/opmap-hashes-unity.log`).
- `Game.Configs` and `Game.Tests.Editor` compilation: passed with `0` errors (`/private/tmp/opmap-hashes-*-build.log`).
- Unity production source-growth guardrail: passed `15/15` (`/private/tmp/opmap-hashes-growth.xml`).
- `git diff --check`: passed.
