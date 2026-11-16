#!/usr/bin/env python3
"""Fail-closed source-input feasibility probe for the Alt04 nonvisual core slice.

This validates that the current raw config is bound to the current Baker expressions.
It deliberately does not synthesize Unity entities or resolve prefab/model references.
"""
import argparse
import hashlib
import json
import re
import shutil
import tempfile
from pathlib import Path

CONFIG = Path("Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset")
CONFIG_META = Path(str(CONFIG) + ".meta")
PREFAB = Path("Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab")
AUTHORING = Path("Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs")
COMPONENTS = {
    "Faction": Path("Assets/Game/Scripts/Components/FactionAIComponents.cs"),
    "UnitCombat": Path("Assets/Game/Scripts/Components/UnitCombatComponents.cs"),
    "UnitHealth": Path("Assets/Game/Scripts/Components/UnitCombatComponents.cs"),
    "UnitAttack": Path("Assets/Game/Scripts/Components/UnitCombatComponents.cs"),
    "UnitSurfaceComponent": Path("Assets/Game/Scripts/Components/MapSurfaceComponents.cs"),
}


def require(ok, message):
    if not ok:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def one(text, name, cast):
    found = re.findall(rf"(?m)^\s*{re.escape(name)}:\s*([^\r\n]+?)\s*$", text)
    require(len(found) == 1, f"expected one raw config field: {name}")
    value = found[0]
    if cast is int:
        require(re.fullmatch(r"-?\d+", value) is not None, f"invalid integer {name}")
        return int(value)
    if cast is float:
        require(re.fullmatch(r"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?", value) is not None,
                f"invalid float {name}")
        return float(value)
    require(value in ("0", "1"), f"invalid bool {name}")
    return value == "1"


def method(source, signature):
    start = source.find(signature)
    require(start >= 0, f"missing source method: {signature}")
    opening = source.find("{", start + len(signature))
    depth = 0
    for i in range(opening, len(source)):
        if source[i] == "{":
            depth += 1
        elif source[i] == "}":
            depth -= 1
            if depth == 0:
                return source[opening:i + 1]
    raise ValueError(f"unbalanced method: {signature}")


def getter_expression(source, name):
    matches = re.findall(rf"(?m)^\s*public\s+[^;\r\n]+?\s+{re.escape(name)}\s*=>\s*([^;\r\n]+);", source)
    require(len(matches) == 1, f"expected exactly one authoring getter: {name}; found {len(matches)}")
    return matches[0].strip()


def getter_matches(source, name, expected):
    try:
        return getter_expression(source, name) == expected
    except ValueError:
        return False


def run(repo, output):
    repo = repo.resolve(strict=True)
    output = output.resolve()
    require(output.is_relative_to(repo) and not output.exists(), "fresh output directory inside repository required")
    cfg_path, meta_path, prefab_path, authoring_path = [repo / p for p in (CONFIG, CONFIG_META, PREFAB, AUTHORING)]
    cfg, meta, prefab, authoring = [p.read_text(encoding="utf-8") for p in
                                    (cfg_path, meta_path, prefab_path, authoring_path)]
    guid = re.search(r"(?m)^guid: ([0-9a-f]{32})$", meta)
    require(guid and prefab.count("guid: " + guid.group(1)) == 1,
            "Alt04 prefab does not uniquely bind the current config meta GUID")
    bake = method(authoring, "public override void Bake(UnitGridAuthoring authoring)")
    exact = {
        "Faction": "AddComponent(entity, new Faction { Id = bakedFactionId });",
        "UnitCombat": "AddComponent(entity, new UnitCombat { AggroRangeCells = authoring.ConfiguredAggroRangeCells, ChaseBreakDistance = authoring.ConfiguredChaseBreakDistance, CanAttack = (byte)(authoring.ConfiguredCanAttack ? 1 : 0), AutoEngage = (byte)(authoring.ConfiguredAllowAutoEngage ? 1 : 0) });",
        "UnitAttack": "AddComponent(entity, new UnitAttack { Range = authoring.ConfiguredAttackRange, CooldownSeconds = authoring.ConfiguredAttackCooldownSeconds, Damage = authoring.ConfiguredAttackDamage,",
        "UnitHealth": "int maxHp = authoring.ConfiguredMaxHealth; AddComponent(entity, new UnitHealth { Current = maxHp, Max = maxHp });",
        "UnitSurfaceComponent": "AddComponent(entity, new UnitSurfaceComponent { SurfaceId = -1, LayerId = 0, LastSampledHeight = authoring.transform.position.y, LastSampledNormal = new float3(0f, 1f, 0f), HasSurface = 0, IsGrounded = 0 });",
    }
    compact_bake = re.sub(r"\s+", " ", bake)
    for name, snippet in exact.items():
        require(name in COMPONENTS and snippet in compact_bake,
                f"current Baker expression changed or not uniquely supported: {name}")
    getter_checks = {
        "ConfiguredCanAttack": "config != null ? config.CanAttack : canAttack",
        "ConfiguredAllowAutoEngage": "config != null ? config.AllowAutoEngage : allowAutoEngage",
        "ConfiguredAggroRangeCells": "Mathf.Max(0, config != null ? config.AggroRangeCells : aggroRangeCells)",
        "ConfiguredAttackRange": "Mathf.Max(0f, config != null ? config.AttackRange : attackRange)",
        "ConfiguredChaseBreakDistance": "Mathf.Max(0f, config != null ? config.ChaseBreakDistance : chaseBreakDistance)",
        "ConfiguredAttackCooldownSeconds": "Mathf.Max(0.01f, config != null ? config.AttackCooldownSeconds : attackCooldownSeconds)",
        "ConfiguredAttackDamage": "Mathf.Max(0, config != null ? config.AttackDamage : attackDamage)",
        "ConfiguredMaxHealth": "Mathf.Max(1, config != null ? config.MaxHealth : maxHealth)",
    }
    for name, expression in getter_checks.items():
        require(getter_expression(authoring, name) == expression, f"configured getter changed: {name}")
        # ApplyConfigIfAvailable writes the same live Config property into the
        # serialized backing field used by the null-config fallback.
        field_name = {
            "ConfiguredCanAttack": "canAttack", "ConfiguredAllowAutoEngage": "allowAutoEngage",
            "ConfiguredAggroRangeCells": "aggroRangeCells", "ConfiguredAttackRange": "attackRange",
            "ConfiguredChaseBreakDistance": "chaseBreakDistance",
            "ConfiguredAttackCooldownSeconds": "attackCooldownSeconds",
            "ConfiguredAttackDamage": "attackDamage", "ConfiguredMaxHealth": "maxHealth",
        }[name]
        require(f"{field_name} = config.{name.removeprefix('Configured')};" in
                method(authoring, "private void ApplyConfigIfAvailable()"),
                f"ApplyConfigIfAvailable no longer binds {name} to backing field {field_name}")
    config_models_all = (repo / "Assets/Game/Scripts/Configs/GameplayConfigModels.cs").read_text(encoding="utf-8")
    class_start = config_models_all.find("public class UnitGridAuthoringConfig : ScriptableObject")
    require(class_start >= 0, "UnitGridAuthoringConfig source type missing")
    class_end = config_models_all.find("public class ", class_start + len("public class "))
    config_models = config_models_all[class_start:class_end if class_end >= 0 else None]
    config_getters = {
        "CanAttack": "canAttack", "AllowAutoEngage": "allowAutoEngage", "AggroRangeCells": "aggroRangeCells",
        "AttackRange": "attackRange", "ChaseBreakDistance": "chaseBreakDistance",
        "AttackCooldownSeconds": "attackCooldownSeconds", "AttackDamage": "attackDamage", "MaxHealth": "maxHealth",
    }
    for name, backing in config_getters.items():
        matches = re.findall(rf"(?m)^\s*public\s+[^;\r\n]+?\s+{name}\s*=>\s*([^;\r\n]+);", config_models)
        require(len(matches) == 1 and matches[0].strip() == backing,
                f"expected exactly one raw Config property passthrough for {name}")
    # Exercise failure modes without touching repository source or output.
    duplicate_getter = authoring + "\npublic int ConfiguredAggroRangeCells => 99;\n"
    changed_getter = authoring.replace(
        "Mathf.Max(0, config != null ? config.AggroRangeCells : aggroRangeCells)",
        "Mathf.Max(1, config != null ? config.AggroRangeCells : aggroRangeCells)", 1)
    require(not getter_matches(duplicate_getter, "ConfiguredAggroRangeCells", getter_checks["ConfiguredAggroRangeCells"]),
            "negative control failed to reject duplicate getter")
    require(not getter_matches(changed_getter, "ConfiguredAggroRangeCells", getter_checks["ConfiguredAggroRangeCells"]),
            "negative control failed to reject changed clamp")

    values = {
        "aggroRangeCells": one(cfg, "aggroRangeCells", int),
        "chaseBreakDistance": one(cfg, "chaseBreakDistance", float),
        "canAttack": one(cfg, "canAttack", bool),
        "allowAutoEngage": one(cfg, "allowAutoEngage", bool),
        "attackRange": one(cfg, "attackRange", float),
        "attackCooldownSeconds": one(cfg, "attackCooldownSeconds", float),
        "attackDamage": one(cfg, "attackDamage", int),
        "maxHealth": one(cfg, "maxHealth", int),
        "threatDetectionKind": one(cfg, "threatDetectionKind", int),
        "threatDetectionRadiusCells": one(cfg, "threatDetectionRadiusCells", int),
        "groundOffset": one(cfg, "groundOffset", float),
    }
    # This control intentionally does not resolve the model child authoring objects,
    # Unity's transform/baking context, or linked object references.
    receipt = {
        "kind": "Alt04CurrentCoreComponentSourceFeasibilityProbe",
        "status": "PassedSourceExpressionPreflight",
        "scope": "Current scalar raw-config expressions for UnitCombat, UnitAttack scalar fields, UnitHealth, and UnitSurfaceComponent source expression; no entity/import/spawn/readiness claim.",
        "config_guid": guid.group(1),
        "raw_config_values": values,
        "components": {
            "UnitCombat": "source-bound raw scalar inputs; Config getter passthrough, ApplyConfigIfAvailable backing-field copy, and all eight authoring getter expressions checked",
            "UnitAttack": "only Range/CooldownSeconds/Damage inputs source-bound; trace fields and visual refs unsupported here",
            "UnitHealth": "Current=Max from current MaxHealth getter; clamp source-checked",
            "UnitSurfaceComponent": "source expression identified; concrete Height is not computed because prefab transform/baking context is unresolved",
            "Faction": "unsupported: Baker branches on resolved model child components and ownership role; this probe does not prove the resolved linked model closure",
        },
        "unsupported_branches": [
            "Faction model-child identity/ownership branch unresolved",
            "UnitSurfaceComponent evaluated transform.position.y unresolved",
            "UnitAttack trace color/width/scroll/density/visible/tracer raw source mapping not compiled",
            "VFX/presentation/entity references and complete prefab component closure not evaluated",
        ],
        "negative_controls": {
            "duplicate_authoring_getter_rejected": True,
            "changed_aggro_clamp_rejected": True,
        },
        "compile": {"status": "NotRun", "reason": "Parent requested reporting before any build; current source-build pins are stale after UnitGridAuthoring.cs edit."},
        "source_pins": {str(p.relative_to(repo)): sha(p) for p in
                        (cfg_path, meta_path, prefab_path, authoring_path, *[repo / p for p in COMPONENTS.values()])},
    }
    staging = Path(tempfile.mkdtemp(prefix=output.name + ".staging-", dir=output.parent))
    try:
        (staging / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
        staging.replace(output)
    except Exception:
        shutil.rmtree(staging, ignore_errors=True)
        raise
    return receipt


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", type=Path, default=Path.cwd())
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        receipt = run(args.repo, args.output)
        print(json.dumps(receipt, indent=2))
        return 0
    except Exception as exc:
        print(f"[Alt04CurrentCoreComponentSourceFeasibilityProbe] result=Failed error={exc}")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
