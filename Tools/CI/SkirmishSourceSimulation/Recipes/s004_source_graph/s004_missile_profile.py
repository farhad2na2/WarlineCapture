"""Source-only original missile owner closure for the experimental S004 tick."""
from pathlib import Path
import hashlib
import re
import s004_transport_profile as transport

ROOT = transport.ROOT

def apply(arguments, output):
    pins = transport.verify()["source_and_reference_pins"]
    def pinned(path):
        path = Path(path)
        if pins.get(str(path)) != hashlib.sha256(path.read_bytes()).hexdigest():
            raise ValueError("Exact missile source pin required: " + str(path))
        return path
    generated = transport.S026 / "source/generated/s026"
    for name in ("extra-622-597-GroundMissileLauncherSystems.cs", "extra-626-602-MissileFlightAudioSystem.cs"):
        arguments.append(str(pinned(generated / name)))
    arguments.append(str(pinned(ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/extra-625-600-MissileEngineAndPresentationBoundary.cs")))
    original = pinned(generated / "extra-624-599-CurrentMissileDependencies.cs").read_text()
    text, count = re.subn(r"internal struct SkirmishNativeTransientOwner\s*:\s*IComponentData\s*\{\s*public FixedString64Bytes SessionId;\s*\}", "", original)
    if count != 1: raise ValueError("Exact existing transient owner seam required")
    dependencies = output / "S004MissileDependencies.cs"; dependencies.write_text(text); arguments.append(str(dependencies))
    matches = [i for i, value in enumerate(arguments) if value.endswith("/S004SupportAutonomousEcs.cs")]
    if len(matches) != 1: raise ValueError("Exact Support ECS projection required")
    text = Path(arguments[matches[0]]).read_text()
    replacements = (
        ("public struct ArchetypeChunk{internal Game.Runtime.CanonicalStorageBank", "public struct ArchetypeChunk{internal bool SourceMissileChunk;internal Game.Runtime.CanonicalStorageBank"),
        ("public Unity.Collections.NativeArray<T> GetNativeArray<T>(ref ComponentTypeHandle<T> h)\n {", "public Unity.Collections.NativeArray<T> GetNativeArray<T>(ref ComponentTypeHandle<T> h)\n {\n  if(SourceMissileChunk && typeof(T)!=typeof(Game.Components.GroundMissileProjectileComponent) && typeof(T)!=typeof(Unity.Transforms.LocalTransform)) throw new NotSupportedException(\"Exact projectile chunk read domain required\");"),
        ("public static LocalTransform Identity=>", "public static LocalTransform FromPositionRotation(Unity.Mathematics.float3 position,Unity.Mathematics.quaternion rotation)=>new LocalTransform{Position=position,Scale=1f,Rotation=rotation};public static LocalTransform Identity=>"),
    )
    for old, new in replacements:
        if text.count(old) != 1: raise ValueError("Exact missile ECS seam changed")
        text = text.replace(old, new)
    projection = output / "S004MissileAutonomousEcs.cs"; projection.write_text(text); arguments[matches[0]] = str(projection)
    arguments.append(str(ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/S004MissileTickOwners.cs"))

def apply_tick(arguments, output):
    def amend(suffix, name, edits):
        matches = [i for i, value in enumerate(arguments) if value.endswith(suffix)]
        if len(matches) != 1: raise ValueError("Exact experimental tick projection required: " + suffix)
        source = Path(arguments[matches[0]]).read_text()
        for old, new in edits:
            if source.count(old) != 1: raise ValueError("Exact missile tick seam changed: " + old)
            source = source.replace(old, new)
        target = output / name; target.write_text(source); arguments[matches[0]] = str(target)
    amend("/S004ExperimentalSourceOwnerOrder.cs", "S004MissileSourceOwnerOrder.cs", [
        ('"UnitAttackSystem","BuildingDefenseAttackSystem"', '"UnitAttackSystem","GroundMissileLauncherFireSystem","GroundMissileLauncherVisualSystem","GroundMissileFlyingRocketVisualSystem","GroundMissileRocketTrailSystem","BuildingDefenseAttackSystem"'),
        ('"SkirmishWorldMovementSystem","UnitDeathSystem"', '"SkirmishWorldMovementSystem","GroundMissileProjectileFlightSystem","MissileFlightAudioSystem","GroundMissileImpactSystem","UnitDeathSystem"'),
        ('{"defense",new[]{"BuildingDefenseAttackSystem"}},', '{"s004_missile_fire",new[]{"GroundMissileLauncherFireSystem"}},\n {"s004_missile_visual",new[]{"GroundMissileLauncherVisualSystem"}},\n {"s004_missile_flying",new[]{"GroundMissileFlyingRocketVisualSystem"}},\n {"s004_missile_trail",new[]{"GroundMissileRocketTrailSystem"}},\n {"defense",new[]{"BuildingDefenseAttackSystem"}},'),
        ('{"death",new[]{"UnitDeathSystem"}},', '{"s004_missile_flight",new[]{"GroundMissileProjectileFlightSystem"}},\n {"s004_missile_audio",new[]{"MissileFlightAudioSystem"}},\n {"s004_missile_impact",new[]{"GroundMissileImpactSystem"}},\n {"death",new[]{"UnitDeathSystem"}},'),
    ])
    amend("/S004ExperimentalOwnerStep.cs", "S004MissileOwnerStep.cs", [
        ('Phase("attack",()=>combat.AttackPhase());Phase("defense",', 'Phase("attack",()=>combat.AttackPhase());Phase("s004_missile_fire",()=>s004MissileTickOwners.Fire());Phase("s004_missile_visual",()=>s004MissileTickOwners.Visual());Phase("s004_missile_flying",()=>s004MissileTickOwners.Flying());Phase("s004_missile_trail",()=>s004MissileTickOwners.Trail());Phase("defense",'),
        ('Phase("s004_expanded_movement",()=>s004SupplementalTickOwners.ExpandedMovement());Phase("death",', 'Phase("s004_expanded_movement",()=>s004SupplementalTickOwners.ExpandedMovement());Phase("s004_missile_flight",()=>s004MissileTickOwners.Flight());Phase("s004_missile_audio",()=>s004MissileTickOwners.Audio());Phase("s004_missile_impact",()=>s004MissileTickOwners.Impact());Phase("death",'),
    ])
    matches = [i for i, value in enumerate(arguments) if value.startswith("-define:")]
    if len(matches) != 1: raise ValueError("One source define set required")
    arguments[matches[0]] += ",WARLINE_S004_MISSILE_TICK"
