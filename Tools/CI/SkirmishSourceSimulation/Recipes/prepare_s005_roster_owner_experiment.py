#!/usr/bin/env python3
"""Observe the original roster owner on the existing S005 source diagnostic.

This is a source-only experiment; it does not admit a scheduler or a native scene.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("s005_spawn_prepare", HERE / "prepare_s005_original_spawn_experiment.py")
base = importlib.util.module_from_spec(spec)
spec.loader.exec_module(base)

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def prepare(out):
    base.prepare(out)
    helper = out / "helper.cs"
    text = helper.read_text()
    anchor = '        var forceRows=new JArray();var counts=new Dictionary<string,int>(StringComparer.Ordinal);var stableIds=new HashSet<string>(StringComparer.Ordinal);'
    replacement = '''        JObject rosterOwner = new JObject { ["invoked"] = false };
        if (forceSucceeded)
        {
            Need(em.HasComponent<SkirmishResolvedSetupRecord>(session), "Original roster owner requires actual setup record");
            var recorded = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            Need(Object.ReferenceEquals(recorded, setup), "Roster setup must be the actual session setup");
            var rosterState = new SystemState { EntityManager = em, Enabled = true };
            var rosterSystem = new SkirmishRosterProjectionSystem();
            rosterState.Begin(); rosterSystem.OnCreate(ref rosterState); rosterState.End();
            rosterState.Begin(); rosterSystem.OnUpdate(ref rosterState); rosterState.End();
            int actors = 0, overlays = 0, suppressed = 0;
            var currentSession = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            using (var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent)))
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
                foreach (var actor in entities)
                {
                    if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).SessionId.Equals(currentSession.SessionId)) continue;
                    actors++;
                    if (em.HasComponent<SkirmishRoleOverlayComponent>(actor) && em.GetComponentData<SkirmishRoleOverlayComponent>(actor).Applied != 0) overlays++;
                    if (em.HasComponent<CampaignMissionCombatSuppressedTag>(actor)) suppressed++;
                }
            rosterOwner = new JObject { ["invoked"] = true, ["session_actors"] = actors,
                ["applied_overlays"] = overlays, ["combat_suppressed"] = suppressed,
                ["native_scene_validated"] = false, ["scheduler_admitted"] = false };
        }
''' + anchor
    text = base.once(text, anchor, replacement)
    text = base.once(text, 'return new JObject { ["opening_focus"]=openingFocus,',
                     'return new JObject { ["roster_owner"]=rosterOwner, ["opening_focus"]=openingFocus,')
    helper.write_text(text)
    receipt_path = out / "preparation.json"
    receipt = json.loads(receipt_path.read_text())
    receipt["source_pins"][str(helper)] = sha(helper)
    receipt["source_pins"][str(Path(__file__).resolve())] = sha(Path(__file__).resolve())
    receipt["roster_owner_scope"] = "Original OnCreate/OnUpdate on actual retained source world; no scheduler admission"
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n")

if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("output", type=Path)
    prepare(p.parse_args().output.resolve())
