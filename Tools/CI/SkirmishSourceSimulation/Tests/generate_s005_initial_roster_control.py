#!/usr/bin/env python3
"""Generate a fixture from the actual S005 snapshot block; no mission state."""
import argparse
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument("--helper", type=Path, required=True)
p.add_argument("--output", type=Path, required=True)
a = p.parse_args()
source = a.helper.read_text()
start = source.index("        // Capture startup immediately after the spawn owner.")
end = source.index("        var forceRows=new JArray();", start)
block = source[start:end]
fixture = r'''using System;
using System.Linq;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
internal static class S005InitialRosterSnapshotControl {
    static void Need(bool ok,string label) { if(!ok) throw new Exception(label); }
    static JObject Snapshot(EntityManager em,Entity session,SkirmishResolvedSetup setup,bool forceSucceeded) {
/* BLOCK */
        return initialRosterSnapshot;
    }
    static void Main() {
        var em=new EntityManager(); int next=1; em.LogicalAllocatorAdmitted=true;
        em.AllocateLogicalIdentity=()=>new Entity { Index=next++,Version=1 };
        var session=em.CreateEntity();
        em.AddComponentData(session,new SkirmishExpandedSessionComponent { SessionId=new FixedString64Bytes("session") });
        var setup=new SkirmishResolvedSetup { Forces=new[] { new SkirmishResolvedForceEntry { FactionId=1,RoleKind=SkirmishRoleKind.Rifle,Quantity=2 } } };
        Entity Add(string id,string ownerSession) {
            var actor=em.CreateEntity();
            em.AddComponentData(actor,new SkirmishAttemptOwnedComponent { SessionId=new FixedString64Bytes(ownerSession),StableObjectId=new FixedString64Bytes(id),FactionId=1 });
            em.AddComponentData(actor,new SkirmishUnitRoleComponent { Role=SkirmishRoleKind.Rifle });
            return actor;
        }
        Add("one","session"); Add("two","session"); Add("outside","other");
        var initial=Snapshot(em,session,setup,true);
        Need((bool)initial["matches_compiled_counts"] && (int)initial["count"]==2,"initial roster and foreign session");
        Add("one","session");
        var duplicate=Snapshot(em,session,setup,true);
        Need(!(bool)duplicate["stable_ids_unique"] && !(bool)duplicate["matches_compiled_counts"] &&
             (int)duplicate["count"]==3 && (int)duplicate["unique_stable_id_count"]==2,"duplicate count and identity");
        Need((int)initial["count"]==2 && (bool)initial["matches_compiled_counts"],"snapshot changed after later entity creation");
        Need(!(bool)Snapshot(em,session,setup,false)["matches_compiled_counts"],"failed spawn must not match");
        Console.WriteLine("[S005InitialRosterSnapshotControl] result=Passed sameSession=2 foreignExcluded=1 duplicateRejected=1 snapshotRetained=1 failedSpawnRejected=1 fixtureOnly=1 readiness=0");
    }
}
'''.replace("/* BLOCK */", block)
a.output.write_text(fixture)
