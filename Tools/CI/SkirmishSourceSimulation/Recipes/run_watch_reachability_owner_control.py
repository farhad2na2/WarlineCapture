#!/usr/bin/env python3
"""Compile the exact native watch-hit owner with explicit fixture raycast inputs."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
SOURCE = ROOT / "Assets/Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.Watch.cs"


def sha(raw): return hashlib.sha256(raw).hexdigest()


def extract(raw):
    text = raw.decode("utf-8")
    signature = "private bool WatchTargetIsReachable(Vector2 point, int targetId, bool world)"
    if text.count(signature) != 1: raise ValueError("Exact native watch owner required")
    start = text.index(signature); opening = text.index("{", start); depth = 0
    for index in range(opening, len(text)):
        if text[index] == "{": depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0: return text[start:index + 1]
    raise ValueError("Unbalanced native owner")


PREFIX = r'''using System;
using System.Collections.Generic;
struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
struct Rect { public bool Contains(Vector2 p)=>p.x>=0&&p.y>=0&&p.x<100&&p.y<100; }
static class Screen { public static Rect safeArea=new Rect(); }
sealed class PointerEventData { public Vector2 position; public PointerEventData(EventSystem value){} }
struct EntityId { public int value; public override int GetHashCode()=>value; }
sealed class Button { public bool active,interactable;public int id;public bool IsActive()=>active;public bool IsInteractable()=>interactable;public EntityId GetEntityId()=>new EntityId{value=id}; }
sealed class GameObject { public Button button; public T GetComponentInParent<T>() where T:class => button as T; }
struct RaycastResult { public GameObject gameObject; }
sealed class EventSystem { public static EventSystem current;public List<RaycastResult> hits=new List<RaycastResult>();public void RaycastAll(PointerEventData p,List<RaycastResult> result){result.AddRange(hits);} }
sealed class OwnerFixture { PointerEventData watchRaycast; EventSystem watchEventSystem; readonly List<RaycastResult> watchHits=new List<RaycastResult>();
'''
SUFFIX = r'''
public bool Invoke(Vector2 point,int targetId,bool world)=>WatchTargetIsReachable(point,targetId,world);
}
static class Program {
static void Need(bool value,string name){if(!value)throw new Exception(name);}
static RaycastResult Hit(Button b)=>new RaycastResult{gameObject=new GameObject{button=b}};
static void Main(){var owner=new OwnerFixture();var p=new Vector2(5,5);EventSystem.current=null;
Need(!owner.Invoke(p,11,false),"missing event system");var system=new EventSystem();EventSystem.current=system;
Need(!owner.Invoke(new Vector2(105,5),11,false),"outside safe area");Need(owner.Invoke(p,11,true),"world with no hits");
system.hits.Add(Hit(null));Need(!owner.Invoke(p,11,true),"world blocked by graphic");
system.hits.Add(Hit(new Button{active=false,interactable=true,id=9}));system.hits.Add(Hit(new Button{active=true,interactable=true,id=11}));
Need(owner.Invoke(p,11,false),"decorative and inactive hits ignored");system.hits.Insert(1,Hit(new Button{active=true,interactable=true,id=12}));
Need(!owner.Invoke(p,11,false),"front interactable blocks target");Need(owner.Invoke(p,12,false),"first interactable selected");
Console.WriteLine("[WatchReachabilityOwnerControl] result=Passed cases=7 fixtureOnly=1 nativeInput=0 readiness=0");}}
'''


def main():
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument("--output", required=True); args = parser.parse_args()
    output = Path(args.output).absolute(); output.mkdir(parents=True, exist_ok=False)
    raw = SOURCE.read_bytes(); body = extract(raw); generated = PREFIX + body + SUFFIX
    (output / "Program.cs").write_text(generated)
    (output / "control.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>\n')
    env = os.environ.copy(); env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    result = subprocess.run(["dotnet", "run", "--project", str(output / "control.csproj"), "--configuration", "Release"], cwd=ROOT, env=env, capture_output=True, text=True, timeout=180)
    (output / "run.log").write_text(result.stdout + result.stderr)
    if result.returncode != 0 or "[WatchReachabilityOwnerControl] result=Passed cases=7 fixtureOnly=1 nativeInput=0 readiness=0" not in result.stdout:
        raise RuntimeError("Exact watch owner fixture failed; see run.log")
    if sha(SOURCE.read_bytes()) != sha(raw): raise ValueError("Native owner source changed during fixture")
    receipt = {"status": "PassedOriginalOwnerFixture", "source": str(SOURCE), "source_sha256": sha(raw),
               "extracted_method_sha256": sha(body.encode()), "cases": 7, "source_body_modified": False,
               "fixture_raycast_inputs": True, "actual_hud_observed": False, "runtime_admission": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n"); print(json.dumps(receipt))


if __name__ == "__main__": main()
