#!/usr/bin/env python3
"""Run two original same-world roster lifecycle owners after S005 startup.

This is a bounded owner diagnostic, not native schedule admission.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("epoch", HERE / "prepare_s005_current_roster_epoch.py")
epoch = importlib.util.module_from_spec(spec)
spec.loader.exec_module(epoch)

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def prepare(out):
    epoch.prepare(out)
    helper = out / "helper.cs"
    source = helper.read_text()
    anchor = '        var forceRows=new JArray();var counts=new Dictionary<string,int>(StringComparer.Ordinal);var stableIds=new HashSet<string>(StringComparer.Ordinal);'
    block = '''        JObject groupOwners = new JObject { ["invoked"] = false };
        if (forceSucceeded)
        {
            Need(em.HasBuffer<SkirmishArmyGroupRecord>(session), "Actual session group buffer required");
            var capacityState = new SystemState { EntityManager = em, Enabled = true };
            var capacityOwner = new SkirmishCapacityLifecycleSystem();
            capacityState.Begin(); capacityOwner.OnCreate(ref capacityState); capacityState.End();
            capacityState.Begin(); capacityOwner.OnUpdate(ref capacityState); capacityState.End();
            var groupState = new SystemState { EntityManager = em, Enabled = true };
            var groupOwner = new SkirmishArmyGroupSystem();
            groupState.Begin(); groupOwner.OnCreate(ref groupState); groupState.End();
            groupState.Begin(); groupOwner.OnUpdate(ref groupState); groupState.End();
            var groups = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            var groupRows = new JArray(); int members = 0, alive = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i]; members += group.MemberCount; alive += group.AliveCount;
                groupRows.Add(new JObject { ["id"] = group.GroupId, ["faction"] = group.FactionId,
                    ["members"] = group.MemberCount, ["alive"] = group.AliveCount });
            }
            groupOwners = new JObject { ["invoked"] = true, ["groups"] = groupRows,
                ["members"] = members, ["alive"] = alive, ["scheduler_admitted"] = false };
        }
''' + anchor
    source = epoch.roster.base.once(source, anchor, block)
    source = epoch.roster.base.once(source, 'return new JObject { ["roster_owner"]=rosterOwner,',
        'return new JObject { ["group_owners"]=groupOwners, ["roster_owner"]=rosterOwner,')
    helper.write_text(source)
    prep = out / "preparation.json"
    doc = json.loads(prep.read_text())
    doc["source_pins"][str(helper)] = sha(helper)
    doc["source_pins"][str(Path(__file__).resolve())] = sha(Path(__file__).resolve())
    doc["group_owner_scope"] = "Original capacity then army-group OnUpdate on same retained startup; not a scheduler tick"
    prep.write_text(json.dumps(doc, indent=2) + "\n")

if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("output", type=Path)
    prepare(p.parse_args().output.resolve())
