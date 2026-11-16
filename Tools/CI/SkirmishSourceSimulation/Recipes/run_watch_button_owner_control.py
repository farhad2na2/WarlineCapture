#!/usr/bin/env python3
"""Exercise the original button and hit-test owners with explicit fixture UI inputs."""
import argparse
import json
import os
from pathlib import Path
import subprocess

from run_watch_reachability_owner_control import SOURCE, extract, sha

ROOT = Path(__file__).resolve().parents[4]
BUTTON_SOURCE = ROOT / "Assets/Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.SkirmishWatch.cs"


def button_body(raw):
    text = raw.decode("utf-8")
    signature = "private AriaTouchTarget ObserveWatchButton(Button button)"
    if text.count(signature) != 1:
        raise ValueError("Exact native button observer required")
    start = text.index(signature)
    opening = text.index("{", start)
    depth = 0
    for index in range(opening, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[start:index + 1]
    raise ValueError("Unbalanced native button observer")


FIXTURE = r'''using System;
using System.Collections.Generic;
struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
struct Rect { public Vector2 center; public bool Contains(Vector2 p)=>p.x>=0&&p.y>=0&&p.x<100&&p.y<100; }
static class Screen { public static Rect safeArea=new Rect(); }
sealed class RectTransform { public Rect rect; public Vector2 TransformPoint(Vector2 p)=>p; }
static class RectTransformUtility { public static Vector2 WorldToScreenPoint(object camera,Vector2 p)=>p; }
sealed class PointerEventData { public Vector2 position; public PointerEventData(EventSystem value){} }
struct EntityId { public int value; public override int GetHashCode()=>value; }
struct AriaTouchTarget { public int Id; public Vector2 Position; public bool Available; }
sealed class Button { public bool active,interactable; public int id; public object transform; public bool IsActive()=>active; public bool IsInteractable()=>interactable; public EntityId GetEntityId()=>new EntityId{value=id}; }
sealed class GameObject { public Button button; public T GetComponentInParent<T>() where T:class => button as T; }
struct RaycastResult { public GameObject gameObject; }
sealed class EventSystem { public static EventSystem current; public List<RaycastResult> hits=new List<RaycastResult>(); public void RaycastAll(PointerEventData p,List<RaycastResult> result){result.AddRange(hits);} }
sealed class OwnerFixture { PointerEventData watchRaycast; EventSystem watchEventSystem; readonly List<RaycastResult> watchHits=new List<RaycastResult>();
object ResolveEventCamera(Button button)=>null;
'''
END = r'''
public AriaTouchTarget Invoke(Button button)=>ObserveWatchButton(button);
}
static class Program {
static void Need(bool value,string name){if(!value)throw new Exception(name);}
static RaycastResult Hit(Button b)=>new RaycastResult{gameObject=new GameObject{button=b}};
static void Main(){var owner=new OwnerFixture(); var button=new Button{active=true,interactable=true,id=11,transform=new RectTransform{rect=new Rect{center=new Vector2(5,5)}}};
Need(!owner.Invoke(null).Available,"null"); button.active=false; Need(!owner.Invoke(button).Available,"inactive"); button.active=true;button.interactable=false;Need(!owner.Invoke(button).Available,"noninteractable");
button.interactable=true;EventSystem.current=null;Need(!owner.Invoke(button).Available,"missing event system");var system=new EventSystem();EventSystem.current=system;system.hits.Add(Hit(button));
var target=owner.Invoke(button);Need(target.Available&&target.Id==11&&target.Position.x==5&&target.Position.y==5,"reachable actual target");
system.hits.Insert(0,Hit(new Button{active=true,interactable=true,id=12}));Need(!owner.Invoke(button).Available,"occluded target");
system.hits.RemoveAt(0);((RectTransform)button.transform).rect=new Rect{center=new Vector2(105,5)};Need(!owner.Invoke(button).Available,"outside safe area");
Console.WriteLine("[WatchButtonOwnerControl] result=Passed cases=7 fixtureOnly=1 nativeInput=0 readiness=0");}}
'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    output = Path(args.output).absolute()
    output.mkdir(parents=True, exist_ok=False)
    watch_raw, button_raw = SOURCE.read_bytes(), BUTTON_SOURCE.read_bytes()
    watch, button = extract(watch_raw), button_body(button_raw)
    (output / "Program.cs").write_text(FIXTURE + watch + "\n" + button + END)
    (output / "control.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>\n')
    env = os.environ.copy()
    env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    result = subprocess.run(["dotnet", "run", "--project", str(output / "control.csproj"), "--configuration", "Release"], cwd=ROOT, env=env, capture_output=True, text=True, timeout=180)
    (output / "run.log").write_text(result.stdout + result.stderr)
    marker = "[WatchButtonOwnerControl] result=Passed cases=7 fixtureOnly=1 nativeInput=0 readiness=0"
    if result.returncode != 0 or marker not in result.stdout:
        raise RuntimeError("Original button observer fixture failed; see run.log")
    if SOURCE.read_bytes() != watch_raw or BUTTON_SOURCE.read_bytes() != button_raw:
        raise ValueError("Native owner source changed during fixture")
    receipt = {"status": "PassedOriginalOwnerFixture", "watch_source_sha256": sha(watch_raw),
               "button_source_sha256": sha(button_raw), "watch_method_sha256": sha(watch.encode()),
               "button_method_sha256": sha(button.encode()), "cases": 7, "source_bodies_modified": False,
               "fixture_ui_inputs": True, "actual_hud_observed": False, "runtime_admission": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()
