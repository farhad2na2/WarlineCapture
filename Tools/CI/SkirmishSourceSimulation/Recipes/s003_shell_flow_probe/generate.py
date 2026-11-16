"""Compose one no-input original shell-flow update with the S003 source probe."""
import hashlib
import importlib.util
from pathlib import Path

BASE = 'Tools/CI/SkirmishSourceSimulation/Recipes/s003_shell_owner_probe/generate.py'
FLOW = 'Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs'
ECS = 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs'
PROGRAM = 'S003OriginalPlannerProbeProgram.cs'
HOST = 'S003ShellFlowEcs.cs'
BOOTSTRAP = 'S003ShellFlowBootstrap.cs'
FLOW_OUTPUT = 'S003OriginalShellFlow.source.cs'


def module(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise ValueError('Original source generator unavailable')
    result = importlib.util.module_from_spec(spec); spec.loader.exec_module(result)
    return result


def replace_once(text, old, new):
    if text.count(old) != 1:
        raise ValueError('Exact source seam required: ' + old[:120])
    return text.replace(old, new, 1)


def generate(repo, output):
    repo = Path(repo).resolve(strict=True); out = Path(output); out.mkdir(parents=True, exist_ok=False)
    base = module(repo / BASE, 's003_original_shell_owner_gen').generate(repo, out / 'base')
    program = (out / 'base' / PROGRAM).read_text()
    anchor = 'File.WriteAllText(Path.Combine(dir,"s003-ui-shell-owner.json"),shellResult.ToString(Formatting.Indented));'
    call = '''if(Environment.GetEnvironmentVariable("WARLINE_S003_UI_SHELL_FLOW_DIAGNOSTIC")=="1"){var flowResult=S003ShellFlowBootstrap.Run(world.Entities);File.WriteAllText(Path.Combine(dir,"s003-ui-shell-flow.json"),flowResult.ToString(Formatting.Indented));}'''
    program = replace_once(program, anchor, anchor + call)
    source = (repo / ECS).read_text()
    old = 'public sealed class World{public bool IsCreated{get;internal set;}=true;}'
    new = 'public sealed class World{public bool IsCreated{get;internal set;}=true;public EntityManager EntityManager{get;set;}}'
    ecs = replace_once(source, old, new)
    flow = (repo / FLOW).read_text()
    seams = [('            UiShellEcsGateway.RegisterAsRuntimeGateway();', '            SourceS073UiShellFlowBoundary.RegisterGateway();'),
             ('            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(entityManager);', '            Entity audioEntity = SourceS073UiShellFlowBoundary.EnsureAudioEntity(entityManager);'),
             ('            AudioEventRequestSystem.EnqueueMusicState(', '            SourceS073UiShellFlowBoundary.EnqueueMusicState('),
             ('                UiAudioEventBridgeSystem.TryEnqueue(world, request);', '                SourceS073UiShellFlowBoundary.TryEnqueue(world, request);')]
    for old, new in seams:
        flow = replace_once(flow, old, new)
    restored = flow
    for old, new in seams:
        restored = replace_once(restored, new, old)
    if restored != (repo / FLOW).read_text():
        raise ValueError('Flow extraction changed outside explicit effect boundaries')
    bootstrap = '''using System;using Game.UI.Contracts;using Game.UI.Shell.Contracts.Ecs;using Game.UI.Shell.Ecs;using Newtonsoft.Json.Linq;using Unity.Entities;
internal static class S003ShellFlowBootstrap {internal static JObject Run(EntityManager em){if(em==null)throw new ArgumentNullException(nameof(em));using var roots=em.CreateEntityQuery(typeof(UiShellRootComponent),typeof(UiShellStateComponent));if(roots.CalculateEntityCount()!=1)throw new InvalidOperationException("Actual unique source shell required");var root=roots.GetSingletonEntity();var shell=em.GetComponentData<UiShellStateComponent>(root);if(shell.ActiveRoute!=UIRoute.Splash||shell.CurrentMode!=UiShellMode.None||shell.TransitionSequenceId!=0||em.GetBuffer<UiShellRouteRequestComponent>(root).Length!=0||em.GetBuffer<UiShellPresentationCommandComponent>(root).Length!=0||em.GetBuffer<UiShellTransitionCompleteComponent>(root).Length!=0)throw new InvalidOperationException("Original no-input shell boundary required");int a=SourceS073UiShellFlowBoundary.AudioCalls,m=SourceS073UiShellFlowBoundary.MusicCalls,e=SourceS073UiShellFlowBoundary.EnsureAudioCalls,g=SourceS073UiShellFlowBoundary.GatewayCalls;em.World.EntityManager=em;var state=new SystemState{EntityManager=em,Enabled=true,World=em.World};var owner=new UiShellFlowSystem();state.Begin();try{owner.OnCreate(ref state);owner.OnUpdate(ref state);}finally{state.End();}shell=em.GetComponentData<UiShellStateComponent>(root);if(shell.ActiveRoute!=UIRoute.Splash||shell.CurrentMode!=UiShellMode.None||shell.TransitionSequenceId!=0||em.GetBuffer<UiShellRouteRequestComponent>(root).Length!=0||em.GetBuffer<UiShellPresentationCommandComponent>(root).Length!=0||em.GetBuffer<UiShellTransitionCompleteComponent>(root).Length!=0||SourceS073UiShellFlowBoundary.AudioCalls!=a||SourceS073UiShellFlowBoundary.MusicCalls!=m||SourceS073UiShellFlowBoundary.EnsureAudioCalls!=e||SourceS073UiShellFlowBoundary.GatewayCalls!=g+1)throw new InvalidOperationException("Original no-input flow effect or shell state changed");return new JObject{["kind"]="S003SourceShellFlowDiagnostic",["scope"]="NonNative_PostSourceStartup_OneOriginalNoInputFlowUpdate",["route"]=shell.ActiveRoute.ToString(),["gateway_registration_captured"]=true,["native_gateway_registered"]=false,["route_requests"]=0,["presentation_commands"]=0,["transition_completions"]=0,["audio_or_music_effects"]=0,["input_injected"]=false,["native_ui_readiness"]=false,["mission_ready"]=false};}}
'''
    files = {PROGRAM: program.encode(), HOST: ecs.encode(), BOOTSTRAP: bootstrap.encode(), FLOW_OUTPUT: flow.encode()}
    for name, content in files.items():
        (out / name).write_bytes(content)
    pins = dict(base['source_pins'])
    for path in (repo / BASE, repo / FLOW, repo / ECS):
        pins[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    return {'source_pins': pins, 'outputs': {name: hashlib.sha256(raw).hexdigest() for name, raw in files.items()},
            'experimental_only': True, 'scope': 'Original flow no-input update; gateway/audio effects captured, no native or readiness claim'}
