"""Versioned native census identity serialization; serialization is not evidence.

Collectors must read all values from the native resolved setup/current locale.
Qualifiers must bind that raw census to the owned attempt plus complete source/DLL
pins. Never manufacture a census from job fields or treat these SHA256s as wins.
"""
import hashlib
import json
import re

SETUP_KIND = "SkirmishNativeSetupIdentity"
CONTENT_KIND = "SkirmishNativeContentIdentity"
CONFIG_KEYS = ("catalog_id", "definition_id", "definition_version", "difficulty", "size", "seed", "locale")
SETUP_KEYS = {"schema_version", "identity_kind", *CONFIG_KEYS, "native_setup_hash_uint32", "native_content_identity"}


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=True, allow_nan=False).encode("utf-8")


def sha256(value):
    return hashlib.sha256(canonical(value)).hexdigest()


def normalize_native_locale(raw):
    # Raw native GameLocalization codes, not the requested locale/environment.
    if raw not in ("en", "fa-IR"):
        raise ValueError("Unknown actual native locale")
    return "EN" if raw == "en" else "FA"


def validate_setup_identity(value):
    if not isinstance(value, dict) or set(value) != SETUP_KEYS:
        raise ValueError("Native setup identity fields differ from v1")
    if type(value["schema_version"]) is not int or value["schema_version"] != 1 or value["identity_kind"] != SETUP_KIND:
        raise ValueError("Native setup identity schema/kind differs")
    catalog = value["catalog_id"]
    if not isinstance(catalog, str) or re.fullmatch(r"S(?:00[1-9]|0[1-9][0-9]|1[01][0-9]|120)", catalog) is None or value["definition_id"] != "skirmish." + catalog.lower():
        raise ValueError("Native catalog/definition identity differs")
    for field in ("definition_version", "seed"):
        if type(value[field]) is not int or not 1 <= value[field] <= 2147483647:
            raise ValueError("Native positive int32 required: " + field)
    if type(value["native_setup_hash_uint32"]) is not int or not 0 <= value["native_setup_hash_uint32"] <= 4294967295:
        raise ValueError("Actual native SetupHash uint32 required")
    if value["locale"] not in ("EN", "FA") or value["difficulty"] not in ("Recruit", "Regular", "Veteran", "Commander") or value["size"] not in ("Standard", "War", "LargeWar"):
        raise ValueError("Unknown native configuration")
    if not isinstance(value["native_content_identity"], str) or not value["native_content_identity"]:
        raise ValueError("Actual native content identity required")
    return value


def from_native_census(*, catalog_id, definition_id, definition_version, difficulty, size, seed,
                       native_locale, native_setup_hash_uint32, native_content_identity):
    return validate_setup_identity(dict(schema_version=1, identity_kind=SETUP_KIND, catalog_id=catalog_id,
        definition_id=definition_id, definition_version=definition_version, difficulty=difficulty,
        size=size, seed=seed, locale=normalize_native_locale(native_locale),
        native_setup_hash_uint32=native_setup_hash_uint32, native_content_identity=native_content_identity))


def content_identity(setup_identity):
    value = validate_setup_identity(setup_identity)
    return dict(schema_version=1, identity_kind=CONTENT_KIND,
                **{key: value[key] for key in ("catalog_id", "definition_id", "definition_version", "native_content_identity")})


def verify_job(job, setup_identity):
    """Bind a separately collected actual census to all job setup/content fields."""
    value = validate_setup_identity(setup_identity)
    if any(job.get(key) != value[key] or type(job.get(key)) is not type(value[key]) for key in CONFIG_KEYS):
        raise ValueError("Actual native setup configuration differs from job")
    if job.get("setup_sha256") != sha256(value):
        raise ValueError("Actual native setup identity SHA256 differs from job")
    if job.get("content_sha256") != sha256(content_identity(value)):
        raise ValueError("Actual native content identity SHA256 differs from job")
    return value
