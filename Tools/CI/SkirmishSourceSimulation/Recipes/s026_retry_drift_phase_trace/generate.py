#!/usr/bin/env python3
"""Add an opt-in, one-actor phase-boundary observer to the S026 source host."""
import hashlib
from pathlib import Path

SOURCE = "Tools/CI/SkirmishSourceSimulation/Adapters/S026/294-312_PaidSourceOwnerStep.retry.cs"
OUTPUT = "S026RetryDriftPhaseOverlay.cs"
ANCHOR = 'private void Phase(string name,Action action){sourceOrder.EnterPhase(name);var clock=System.Diagnostics.Stopwatch.StartNew();action();clock.Stop();'
INSERT = 'private void Phase(string name,Action action){sourceOrder.EnterPhase(name);var clock=System.Diagnostics.Stopwatch.StartNew();bool tracePhase=SourceS026RetryDriftPhaseTrace.Enabled&&(name=="path"||name=="movement");if(tracePhase)SourceS026RetryDriftPhaseTrace.Observe(Entities,Tick,name,"before");action();if(tracePhase)SourceS026RetryDriftPhaseTrace.Observe(Entities,Tick,name,"after");clock.Stop();'

PROBE = r'''
namespace Game.Runtime
{
    // Source-host phase boundary observation only. This does not enter gameplay
    // owner/job code and does not affect path or movement decisions.
    internal static class SourceS026RetryDriftPhaseTrace
    {
        internal static readonly bool Enabled = System.Environment.GetEnvironmentVariable("WARLINE_S026_RETRY_DRIFT_PHASE_TRACE") == "1";
        private const int FirstTick = 220, LastTick = 310, Capacity = 400;
        private const string StableId = "role.rifle.1.0.5";
        private static readonly System.Collections.Generic.List<Newtonsoft.Json.Linq.JObject> Rows = Enabled
            ? new System.Collections.Generic.List<Newtonsoft.Json.Linq.JObject>(Capacity) : null;
        private static bool Overflow;
        static SourceS026RetryDriftPhaseTrace()
        {
            if (Enabled) System.AppDomain.CurrentDomain.ProcessExit += (s, e) => Write();
        }
        internal static void Observe(Unity.Entities.EntityManager em, long tick, string phase, string edge)
        {
            if (!Enabled || tick < FirstTick || tick > LastTick) return;
            using (var query = em.CreateEntityQuery(typeof(Game.Components.SkirmishAttemptOwnedComponent)))
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var owner = em.GetComponentData<Game.Components.SkirmishAttemptOwnedComponent>(entity);
                    if (owner.StableObjectId.ToString() != StableId) continue;
                    if (Rows.Count >= Capacity) { Overflow = true; return; }
                    var row = new Newtonsoft.Json.Linq.JObject {
                        ["tick"] = tick, ["phase"] = phase, ["edge"] = edge,
                        ["stable_id"] = StableId, ["entity"] = entity.ToString(),
                        ["cell"] = Cell(em, entity), ["transform"] = Transform(em, entity),
                        ["target"] = Target(em, entity), ["path_request"] = Request(em, entity),
                        ["path_range"] = Range(em, entity), ["path_follow"] = Follow(em, entity),
                        ["retry_cooldown_resume_frame"] = em.HasComponent<Game.Components.UnitPathRetryCooldown>(entity)
                            ? (Newtonsoft.Json.Linq.JToken)em.GetComponentData<Game.Components.UnitPathRetryCooldown>(entity).ResumeFrame : null
                    };
                    Rows.Add(row);
                    return;
                }
            }
        }
        private static Newtonsoft.Json.Linq.JToken Cell(Unity.Entities.EntityManager em, Unity.Entities.Entity e)
        {
            if (!em.HasComponent<Game.Components.UnitGrid>(e)) return null;
            var v = em.GetComponentData<Game.Components.UnitGrid>(e).Cell;
            return new Newtonsoft.Json.Linq.JObject { ["x"] = v.x, ["y"] = v.y };
        }
        private static Newtonsoft.Json.Linq.JToken Transform(Unity.Entities.EntityManager em, Unity.Entities.Entity e)
        {
            if (!em.HasComponent<Unity.Transforms.LocalTransform>(e)) return null;
            var v = em.GetComponentData<Unity.Transforms.LocalTransform>(e).Position;
            return new Newtonsoft.Json.Linq.JObject { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };
        }
        private static Newtonsoft.Json.Linq.JToken Target(Unity.Entities.EntityManager em, Unity.Entities.Entity e)
        {
            if (!em.HasComponent<Game.Components.UnitTarget>(e)) return null;
            var v = em.GetComponentData<Game.Components.UnitTarget>(e).Cell;
            return new Newtonsoft.Json.Linq.JObject { ["x"] = v.x, ["y"] = v.y };
        }
        private static Newtonsoft.Json.Linq.JToken Request(Unity.Entities.EntityManager em, Unity.Entities.Entity e)
        {
            if (!em.HasComponent<Game.Components.UnitPathRequest>(e)) return null;
            var v = em.GetComponentData<Game.Components.UnitPathRequest>(e).Goal;
            return new Newtonsoft.Json.Linq.JObject { ["x"] = v.x, ["y"] = v.y };
        }
        private static Newtonsoft.Json.Linq.JToken Range(Unity.Entities.EntityManager em, Unity.Entities.Entity e)
        {
            if (!em.HasComponent<Game.Components.UnitPathRange>(e)) return null;
            var v = em.GetComponentData<Game.Components.UnitPathRange>(e);
            return new Newtonsoft.Json.Linq.JObject { ["start"] = v.Start, ["length"] = v.Length };
        }
        private static Newtonsoft.Json.Linq.JToken Follow(Unity.Entities.EntityManager em, Unity.Entities.Entity e)
        {
            if (!em.HasComponent<Game.Components.UnitPathFollow>(e)) return null;
            return new Newtonsoft.Json.Linq.JValue(em.GetComponentData<Game.Components.UnitPathFollow>(e).PathIndex);
        }
        private static void Write()
        {
            if (!Enabled) return;
            var output = new Newtonsoft.Json.Linq.JObject {
                ["kind"] = "S026RetryDriftPhaseObservation", ["first_tick"] = FirstTick,
                ["last_tick"] = LastTick, ["stable_id"] = StableId, ["overflow"] = Overflow,
                ["rows"] = new Newtonsoft.Json.Linq.JArray(Rows),
                ["scope"] = "Source-host before/after path and movement phase snapshots; observational only; phase boundaries do not isolate causality"
            };
            System.Console.Error.WriteLine("[S026RetryDriftPhaseTrace] " + output.ToString(Newtonsoft.Json.Formatting.None));
        }
    }
}
'''

def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    source = repo / SOURCE
    raw = source.read_bytes()
    text = raw.decode("utf-8").replace("\r\n", "\n")
    if text.count(ANCHOR) != 1:
        raise ValueError("Exact host phase boundary anchor required")
    transformed = text.replace(ANCHOR, INSERT, 1) + PROBE
    output = Path(output)
    output.mkdir(parents=True, exist_ok=False)
    data = transformed.encode("utf-8")
    (output / OUTPUT).write_bytes(data)
    if source.read_bytes() != raw:
        raise ValueError("Host adapter changed during extraction")
    return {"source_pins": {str(source): hashlib.sha256(raw).hexdigest()},
            "outputs": {OUTPUT: hashlib.sha256(data).hexdigest()}, "experimental_only": True,
            "scope": "One selected actor, ticks 220-310, before/after source-host path and movement phases"}
