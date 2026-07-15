# Operation Map And Scenario Identity Contract

Date: 2026-07-16
Status: Accepted shared-foundation contract
Owner tracker: `operation_map_scene_split_and_generator_tracker.md`

## Purpose

Define stable operation-map and scenario identities before map extraction. These ids remain unchanged whether content is editor-authored, runtime-generated, bundled locally, or delivered later through another content mechanism.

## Common Grammar

All ids:

- use lowercase ASCII only;
- contain dot-separated nonempty segments;
- use `a-z`, `0-9`, and `_` inside a segment;
- start each segment with `a-z` or `0-9`;
- do not contain whitespace, hyphens, slashes, file extensions, GUIDs, versions, locale, platform, quality tier, or delivery state;
- are at most `60` ASCII bytes so they fit safely in `FixedString64Bytes` contracts;
- are compared with ordinal case-sensitive equality; and
- are immutable after first accepted content publication.

Normative segment expression:

```text
[a-z0-9][a-z0-9_]*
```

## Operation Map Id

Canonical form:

```text
opmap.<mode-or-chapter>.<slug>
```

Accepted examples:

```text
opmap.skirmish.desert_base_01
opmap.ch01.district_edge_01
```

Rules:

1. `opmap` is the required namespace.
2. Campaign content uses the stable chapter id, such as `ch01`.
3. Non-campaign content uses a stable mode id, such as `skirmish`.
4. The slug identifies the physical/logical operation map, not a mission, scenario variant, art revision, bundle, or generated seed.
5. Scenario variants that reuse one map must retain the same operation-map id.

## Scenario Id

Campaign form:

```text
scenario.<chapter>.<mission>.<slug>
```

Skirmish form:

```text
scenario.skirmish.<slug>
```

Accepted examples:

```text
scenario.ch01.m01.first_contact
scenario.skirmish.desert_base_standard
```

Rules:

1. `scenario` is the required namespace.
2. Campaign chapter and mission segments use stable ids such as `ch01` and `m01`.
3. The slug identifies gameplay setup: objectives, starting state, feature gates, rewards, restrictions, and ARIA hooks.
4. A scenario references exactly one operation-map id after validation.
5. Multiple scenarios may reference the same operation map.

## Identity Is Not Location

Neither id may be derived at runtime from:

- Unity scene name/path/GUID;
- hierarchy name or `GlobalObjectId`;
- Addressables address/group/label;
- bundle/file/CDN path;
- localized display name;
- static-presentation output path/hash;
- generator input, seed, or schema version.

Those values are versioned metadata associated with the stable id. Changing delivery or regenerating accepted content must not change identity.

## Validation Contract

Editor/config validation must reject:

- malformed or overlength ids;
- duplicate operation-map ids or duplicate scenario ids;
- an id used in both namespaces;
- missing scenario-to-map references;
- case-only aliases;
- an operation-map id containing scenario/mission variant policy; and
- released ids reused for semantically different content.

Runtime ECS stores validated ids in bounded fixed strings and never parses display text or scene paths for control flow. Catalog lookup occurs once during transition/composition, not in recurring gameplay updates.
