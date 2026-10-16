# Native setup/content identity protocol v1

Native `SkirmishResolvedSetup.SetupHash` is an unsigned 32-bit value. Native
`ContentHash` is an identity string, not necessarily a SHA256. Neither may be padded
or relabelled as the job's 64-character SHA256. The previously unjoined job fields
now mean the explicit manifests below; old arbitrary pins fail closed.

Canonical bytes use JSON with ordinal sorted object keys, compact comma/colon
separators, ASCII escaping (`ensure_ascii=True` / Newtonsoft `EscapeNonAscii`),
no nonfinite numbers, UTF8 without BOM. A trailing newline is not hashed. Values
are strict JSON strings/integers; booleans, numeric strings and hexadecimal strings
are rejected where integers are required. A manifest's schema/kind and exact field
set are mandatory. `Tools/CI/skirmish_native_identity.py` is the Python reference;
the pure adapter parser implements the independently compared C# equivalent.

`setup_sha256` hashes this complete manifest:

```json
{
  "schema_version": 1,
  "identity_kind": "SkirmishNativeSetupIdentity",
  "catalog_id": "S005",
  "definition_id": "skirmish.s005",
  "definition_version": 2,
  "difficulty": "Regular",
  "size": "Standard",
  "seed": 155926,
  "locale": "FA",
  "native_setup_hash_uint32": 0,
  "native_content_identity": "EXAMPLE ONLY; replace from actual native census"
}
```

The example's uint32 is not a measured setup. Read the real value from the actual
resolved setup. Read actual locale from `GameLocalization.CurrentLocaleCode`:
only exact `en` maps to `EN` and `fa-IR` maps to `FA`. Do not substitute environment,
requested job locale or a permissive locale fallback. Manifest configuration must
match all seven corresponding job fields before either digest is accepted.

`content_sha256` hashes a separate manifest containing only `schema_version:1`,
`identity_kind:"SkirmishNativeContentIdentity"`, `catalog_id`, `definition_id`,
`definition_version` and the actual `native_content_identity`. Seed, locale,
difficulty and size belong to the complete setup manifest. Full source and compiled
DLL fingerprints remain separate mandatory pins; neither digest replaces them.

A native preflight exporter may read the real packaged compiler/census to prepare
these pins, with its checked wrapper receipt, full source/build snapshot and raw
output retained. Suggested envelope: `schema_version:1`,
`collector_kind:"SkirmishNativeCompiledSetupCensus"`, `native_setup_identity`,
`actual_native_locale`, `native_config_sha256`. Such a preflight proves compiled
identity only. It cannot prove spawned entity census, source fit, normal input,
ARIA, terminal/result/return or device readiness. Qualification must independently
reconstruct the same manifests from owned actual runtime rows and compare both
job digests and configuration, not merely trust an exporter/manifest supplied by
the caller. Each manifest must remain bound to the actual owned session/attempt.

The generic adapter draft writes actual native raw values/current locale and the
manifest at resolved snapshot points, then verifies the job. The S005 north
qualifier maps its actual binding fields through the shared Python reference.
Neither path enables generic queue success. Synthetic tests only establish
serialization/type/mutation rejection, not actual game outcomes.
