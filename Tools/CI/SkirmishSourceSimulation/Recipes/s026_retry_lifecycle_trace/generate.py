#!/usr/bin/env python3
"""Generate a source-only, opt-in trace overlay without editing gameplay sources."""
import hashlib
from pathlib import Path

RETRY = 'Assets/Game/Scripts/Systems/UnitPathRetry.cs'
SYSTEM = 'Assets/Game/Scripts/Systems/UnitManualMoveRetrySystem.cs'
RETRY_OUT = 'SourceUnitPathRetryTrace.cs'
SYSTEM_OUT = 'SourceUnitManualMoveRetryTrace.cs'
RETRY_ANCHOR = 'int resumeFrame = Movement.InfantryPathLifecycleKernel.RetryResumeFrame(Time.frameCount);'
SYSTEM_ANCHORS = (
    'CurrentFrame = Time.frameCount,\n                Ecb = ecb',
    'TargetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true),\n                ManualMoveLookup = SystemAPI.GetComponentLookup<ManualMoveOrderTag>(true),\n                Ecb = ecb',
    'if (PathRetryRestorationKernel.ShouldClearCooldown(cooldown.ResumeFrame, CurrentFrame))\n                    Ecb.RemoveComponent<UnitPathRetryCooldown>(entity);',
)

PROBE = r'''
namespace Game.Runtime
{
    // Diagnostic-only overlay. The fixed IDs come from the first S026 baseline
    // prefix drift; disabled runs do not allocate the row buffer or write output.
    internal static class SourceRetryLifecycleTrace
    {
        private const int RetryCreate = 1, CooldownClear = 2, ManualRestore = 3, LongRestore = 4;
        private const int Capacity = 4096;
        internal static readonly bool Enabled = System.Environment.GetEnvironmentVariable("WARLINE_S026_RETRY_TRACE") == "1";
        private static readonly string[] Ids = { "role.rifle.2.6.5", "role.rifle.2.6.6", "role.rifle.2.6.7", "role.rifle.2.6.8" };
        private static readonly Row[] Rows = Enabled ? new Row[Capacity] : null;
        private static int Count;
        private static bool Overflow;
        private struct Row { internal string Id; internal int Kind, Frame, ResumeFrame, GoalX, GoalY; }

        static SourceRetryLifecycleTrace()
        {
            if (Enabled) System.AppDomain.CurrentDomain.ProcessExit += (sender, args) => Write();
        }

        internal static void Observe(Unity.Collections.FixedString64Bytes stableId, int kind,
            int frame, int resumeFrame, int goalX, int goalY)
        {
            if (!Enabled) return;
            string id = stableId.ToString();
            bool selected = false;
            for (int i = 0; i < Ids.Length; i++) if (Ids[i] == id) { selected = true; break; }
            if (!selected) return;
            lock (Rows)
            {
                if (Count == Capacity) { Overflow = true; return; }
                Rows[Count++] = new Row { Id = id, Kind = kind, Frame = frame,
                    ResumeFrame = resumeFrame, GoalX = goalX, GoalY = goalY };
            }
        }

        private static void Write()
        {
            if (!Enabled) return;
            try
            {
                var rows = new Newtonsoft.Json.Linq.JArray();
                lock (Rows)
                {
                    for (int i = 0; i < Count; i++)
                    {
                        var row = Rows[i];
                        rows.Add(new Newtonsoft.Json.Linq.JObject {
                            ["stable_id"] = row.Id,
                            ["event"] = row.Kind == RetryCreate ? "retry_create" : row.Kind == CooldownClear ? "cooldown_clear" : row.Kind == ManualRestore ? "manual_request_restore" : "long_distance_request_restore",
                            ["engine_frame"] = row.Frame, ["resume_frame"] = row.ResumeFrame,
                            ["goal_x"] = row.GoalX, ["goal_y"] = row.GoalY });
                    }
                }
                var output = new Newtonsoft.Json.Linq.JObject {
                    ["kind"] = "S026SourceRetryLifecycleTrace", ["rows"] = rows,
                    ["overflow"] = Overflow, ["engine_frame_only"] = true,
                    ["scope"] = "Selected stable IDs; source-only diagnostic; event order within one frame is unspecified; no gameplay readiness" };
                System.Console.Error.WriteLine("[S026RetryLifecycleTrace] " + output.ToString(Newtonsoft.Json.Formatting.None));
            }
            catch (System.Exception error) { System.Console.Error.WriteLine("[S026RetryLifecycleTrace] failure=" + error); }
        }
    }
}
'''

def _replace_once(text, anchor, replacement, label):
    if text.count(anchor) != 1:
        raise ValueError('Expected one ' + label + ' anchor')
    return text.replace(anchor, replacement, 1)

def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    paths = [repo / RETRY, repo / SYSTEM]
    raw = [p.read_bytes() for p in paths]
    retry = raw[0].decode('utf-8').replace('\r\n', '\n')
    system = raw[1].decode('utf-8').replace('\r\n', '\n')

    retry_insert = RETRY_ANCHOR + '''
            if (SourceRetryLifecycleTrace.Enabled && em.HasComponent<SkirmishAttemptOwnedComponent>(entity))
                SourceRetryLifecycleTrace.Observe(em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).StableObjectId,
                    1, Time.frameCount, resumeFrame, request.Goal.x, request.Goal.y);'''
    retry = _replace_once(retry, RETRY_ANCHOR, retry_insert, 'retry creation')

    system = _replace_once(system, SYSTEM_ANCHORS[0], '''CurrentFrame = Time.frameCount,
                OwnerLookup = SourceRetryLifecycleTrace.Enabled ? SystemAPI.GetComponentLookup<SkirmishAttemptOwnedComponent>(true) : default,
                TraceEnabled = SourceRetryLifecycleTrace.Enabled,
                Ecb = ecb''', 'clear job initializer')
    system = _replace_once(system, SYSTEM_ANCHORS[1], '''TargetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true),
                ManualMoveLookup = SystemAPI.GetComponentLookup<ManualMoveOrderTag>(true),
                OwnerLookup = SourceRetryLifecycleTrace.Enabled ? SystemAPI.GetComponentLookup<SkirmishAttemptOwnedComponent>(true) : default,
                TraceEnabled = SourceRetryLifecycleTrace.Enabled,
                CurrentFrame = Time.frameCount,
                Ecb = ecb''', 'long-distance job initializer')
    # The manual restoration job also receives a frame and optional owner lookup.
    anchor = '''            state.Dependency = new RestoreManualPathRequestJob
            {
                Ecb = ecb'''.replace('+', '')
    system = _replace_once(system, anchor, '''            state.Dependency = new RestoreManualPathRequestJob
            {
                OwnerLookup = SourceRetryLifecycleTrace.Enabled ? SystemAPI.GetComponentLookup<SkirmishAttemptOwnedComponent>(true) : default,
                TraceEnabled = SourceRetryLifecycleTrace.Enabled,
                CurrentFrame = Time.frameCount,
                Ecb = ecb''', 'manual restore initializer')
    system = _replace_once(system, SYSTEM_ANCHORS[2], '''if (PathRetryRestorationKernel.ShouldClearCooldown(cooldown.ResumeFrame, CurrentFrame))
                {
                    if (TraceEnabled && OwnerLookup.HasComponent(entity)) SourceRetryLifecycleTrace.Observe(OwnerLookup[entity].StableObjectId, 2, CurrentFrame, cooldown.ResumeFrame, 0, 0);
                    Ecb.RemoveComponent<UnitPathRetryCooldown>(entity);
                }'''.replace('+', ''), 'cooldown clear')
    # Inject optional lookups into jobs without changing their query requirements.
    system = _replace_once(system, 'public EntityCommandBuffer Ecb;\n\n            private void Execute(Entity entity, in UnitPathRetryCooldown cooldown)',
        '[ReadOnly] public ComponentLookup<SkirmishAttemptOwnedComponent> OwnerLookup;\n            public bool TraceEnabled;\n            public EntityCommandBuffer Ecb;\n\n            private void Execute(Entity entity, in UnitPathRetryCooldown cooldown)', 'cooldown owner lookup')
    system = _replace_once(system, '            public EntityCommandBuffer Ecb;\n\n            private void Execute(Entity entity, in UnitTarget target)',
        '            [ReadOnly] public ComponentLookup<SkirmishAttemptOwnedComponent> OwnerLookup;\n            public bool TraceEnabled;\n            public int CurrentFrame;\n            public EntityCommandBuffer Ecb;\n\n            private void Execute(Entity entity, in UnitTarget target)', 'manual owner lookup')
    system = _replace_once(system, '            [ReadOnly] public ComponentLookup<ManualMoveOrderTag> ManualMoveLookup;\n            public EntityCommandBuffer Ecb;',
        '            [ReadOnly] public ComponentLookup<ManualMoveOrderTag> ManualMoveLookup;\n            [ReadOnly] public ComponentLookup<SkirmishAttemptOwnedComponent> OwnerLookup;\n            public bool TraceEnabled;\n            public int CurrentFrame;\n            public EntityCommandBuffer Ecb;', 'long-distance owner lookup')
    manual_anchor = '                if (intent.RestoreRequest)\n                    Ecb.AddComponent(entity, new UnitPathRequest { Goal = new Unity.Mathematics.int2(intent.Goal.x, intent.Goal.y) });'
    system = _replace_once(system, manual_anchor, '''                if (intent.RestoreRequest)
                {
                    if (TraceEnabled && OwnerLookup.HasComponent(entity)) SourceRetryLifecycleTrace.Observe(OwnerLookup[entity].StableObjectId, 3, CurrentFrame, -1, intent.Goal.x, intent.Goal.y);
                    Ecb.AddComponent(entity, new UnitPathRequest { Goal = new Unity.Mathematics.int2(intent.Goal.x, intent.Goal.y) });
                }'''.replace('+',''), 'manual request restore')
    long_anchor = '                if (intent.RestoreRequest)\n                    Ecb.AddComponent(entity, new UnitPathRequest { Goal = goal });'
    system = _replace_once(system, long_anchor, '''                if (intent.RestoreRequest)
                {
                    if (TraceEnabled && OwnerLookup.HasComponent(entity)) SourceRetryLifecycleTrace.Observe(OwnerLookup[entity].StableObjectId, 4, CurrentFrame, -1, goal.x, goal.y);
                    Ecb.AddComponent(entity, new UnitPathRequest { Goal = goal });
                }'''.replace('+',''), 'long-distance request restore')

    if [p.read_bytes() for p in paths] != raw:
        raise ValueError('Original retry sources changed during extraction')
    output = Path(output)
    output.mkdir(parents=True, exist_ok=False)
    generated = [retry.encode('utf-8'), (system + PROBE).encode('utf-8')]
    outputs = [RETRY_OUT, SYSTEM_OUT]
    for name, contents in zip(outputs, generated): (output / name).write_bytes(contents)
    return {'source_pins': {str(p): hashlib.sha256(b).hexdigest() for p, b in zip(paths, raw)},
            'outputs': {n: hashlib.sha256(b).hexdigest() for n, b in zip(outputs, generated)},
            'experimental_only': True,
            'scope': 'Retry create, cooldown clear and request restore observations for four observed stable IDs; no production source changes'}

