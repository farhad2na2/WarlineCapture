# Operation Map Manifest Source Isolation

## Scope

Static-presentation freshness now hashes only the canonical map-owned presentation sources under the authored map root. Shell, HUD, and other Match-scene dependencies no longer invalidate the operation-map manifest.

## Validation

- Map-owned source-set hash EditMode test: `1 / 1` passed.
- Static-map Android resolver and build-scene guard EditMode suite: `26 / 26` passed after rebake.
- One-button current-map bake: `10 / 10` stages passed.
- Local Addressables output: `137,585,693` bytes, `96` partitions, `1,265` stable addresses.
- `git diff --check`: passed for the owned change set.

## Result

The manifest freshness contract remains fail-closed for map renderer/material changes while unrelated Match shell or HUD edits no longer require a map rebake. No runtime loading, unloading, scene ownership, or gameplay behavior changed in this slice.
