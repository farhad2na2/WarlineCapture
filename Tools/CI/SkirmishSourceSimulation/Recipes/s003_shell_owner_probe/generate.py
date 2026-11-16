"""Compose original shell OnCreate with the read-only S003 planner probe."""
import hashlib
import importlib.util
from pathlib import Path

PLANNER = 'Tools/CI/SkirmishSourceSimulation/Recipes/s003_original_planner_probe/generate.py'
SHELL = 'Tools/CI/SkirmishSourceSimulation/Recipes/s073_ui_shell_state_owner/generate.py'
HOST = 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/108-112_MetadataSystemTypes.cs'
OUTPUT_PROGRAM = 'S003OriginalPlannerProbeProgram.cs'
OUTPUT_HOST = 'S003ShellOwnerMetadataTypes.cs'
OUTPUT_BOOTSTRAP = 'S003ShellOwnerBootstrap.cs'
SHELL_OUTPUTS = ('UiShellStateSystem.source-host.cs', 'UiShellStartupDispositionState.source.cs', 'UiShellStateOwnerComponents.extracted.cs')


def load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise ValueError('Original source generator unavailable')
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def generate(repo, output):
    repo = Path(repo).resolve(strict=True); out = Path(output)
    out.mkdir(parents=True, exist_ok=False)
    planner = load(repo / PLANNER, 's003_original_planner_generator').generate(repo, out / 'planner')
    shell = load(repo / SHELL, 's073_original_shell_generator').generate(repo, out / 'shell')
    program = (out / 'planner' / OUTPUT_PROGRAM).read_text()
    anchor = 'world.WritePublicationStartup(dir);'
    if program.count(anchor) != 1:
        raise ValueError('Exact S003 same-world shell insertion seam required')
    call = '''if(Environment.GetEnvironmentVariable("WARLINE_S003_UI_SHELL_OWNER_DIAGNOSTIC")=="1"){var shellResult=S003ShellOwnerBootstrap.Initialize(world.Entities,world.Session);File.WriteAllText(Path.Combine(dir,"s003-ui-shell-owner.json"),shellResult.ToString(Formatting.Indented));}'''
    program = program.replace(anchor, anchor + call)
    metadata = (repo / HOST).read_text()
    old = 'public struct FixedString4096Bytes{string value;'
    new = old + 'public FixedString4096Bytes(string initial){if(System.Text.Encoding.UTF8.GetByteCount(initial??"")>4093)throw new System.ArgumentException();value=initial;}'
    if metadata.count(old) != 1:
        raise ValueError('Exact source-host fixed-string seam required')
    metadata = metadata.replace(old, new)
    bootstrap = '''using System;using Game.Components;using Game.UI.Contracts;using Game.UI.Shell.Contracts.Ecs;using Game.UI.Shell.Ecs;using Newtonsoft.Json.Linq;using Unity.Entities;
internal static class S003ShellOwnerBootstrap {internal static JObject Initialize(EntityManager em,Entity session){if(em==null||!em.Exists(session)||!em.HasComponent<SkirmishMatchState>(session))throw new InvalidOperationException("Actual S003 session required");var match=em.GetComponentData<SkirmishMatchState>(session);if(match.ScenarioIndex!=5||match.Outcome!=SkirmishOutcome.None)throw new InvalidOperationException("Active S003 source session required");using var roots=em.CreateEntityQuery(typeof(UiShellRootComponent));int before=roots.CalculateEntityCount();if(before!=0)throw new InvalidOperationException("Fresh source shell boundary required");var state=new SystemState{EntityManager=em,Enabled=true};var owner=new UiShellStateSystem();state.Begin();try{owner.OnCreate(ref state);}finally{state.End();}if(state.Enabled||roots.CalculateEntityCount()!=1)throw new InvalidOperationException("Original shell root creation failed");var root=roots.GetSingletonEntity();var shell=em.GetComponentData<UiShellStateComponent>(root);if(shell.ActiveRoute!=UIRoute.Splash||shell.CurrentMode!=UiShellMode.None||shell.TransitionSequenceId!=0||em.HasComponent<AriaPlaySessionComponent>(root)||em.HasComponent<AriaPlayObservationComponent>(root))throw new InvalidOperationException("Original shell defaults changed");return new JObject{["kind"]="S003SourceShellOwnerDiagnostic",["owner"]="UiShellStateSystem.OnCreate",["scope"]="NonNative_PostSourceStartup_OriginalOwnerExceptGatewayRegistration",["route"]=shell.ActiveRoute.ToString(),["root_count_before"]=before,["root_count_after"]=roots.CalculateEntityCount(),["gateway_registration_invoked"]=false,["flow_update_invoked"]=false,["native_ui_readiness"]=false,["mission_ready"]=false};}}
'''
    files = {OUTPUT_PROGRAM: program.encode(), OUTPUT_HOST: metadata.encode(), OUTPUT_BOOTSTRAP: bootstrap.encode()}
    for name in SHELL_OUTPUTS:
        files[name] = (out / 'shell' / name).read_bytes()
    for name, raw in files.items():
        (out / name).write_bytes(raw)
    pins = dict(planner['source_pins'])
    for name, digest in shell['source_pins'].items():
        if name in pins and pins[name] != digest:
            raise ValueError('Conflicting original source pin: ' + name)
        pins[name] = digest
    return {'source_pins': pins, 'outputs': {name: hashlib.sha256(raw).hexdigest() for name, raw in files.items()},
            'experimental_only': True, 'scope': 'Opt-in original shell OnCreate after source startup; no gateway, flow, rendered UI or readiness claim'}
