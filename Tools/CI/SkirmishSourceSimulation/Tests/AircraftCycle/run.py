#!/usr/bin/env python3
"""Differential tests of both actual adapter methods against retained original.
Uses stubs for public controls, not a Unity/player readiness test.
"""
from pathlib import Path
import argparse, shutil, subprocess, tempfile
p=argparse.ArgumentParser();p.add_argument('--repo',required=True);a=p.parse_args();root=Path(a.repo).resolve();here=Path(__file__).resolve().parent
with tempfile.TemporaryDirectory(prefix='aria-aircraft-controls-') as tmp:
 out=Path(tmp)
 for f in ('Program.cs','Original.cs'):shutil.copyfile(here/f,out/f)
 shutil.copyfile(root/'Assets/Game/Scripts/Simulation/AriaAircraftCyclePolicy.cs',out/'AriaAircraftCyclePolicy.cs')
 for name,rel in [('native','Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.cs'),('shared','Assets/Game/Scripts/Simulation/AriaGroundBaseAssaultPolicy.cs')]:
  text=(root/rel).read_text();start=text.index('static bool StepAircraftCycle(');brace=text.index('{',start);depth=0
  for i in range(brace,len(text)):
   if text[i]=='{':depth+=1
   elif text[i]=='}':
    depth-=1
    if not depth:break
  else:raise ValueError('Unclosed actual adapter')
  method=text[start:i+1].replace('static bool','public static bool',1)
  for x,y in [('AriaSkirmishObservation','View'),('AriaS002Observation','View'),('AriaSkirmishPlanComponent','Plan'),('AriaS002PlanState','Plan'),('AriaPlaySessionComponent','Touch'),('AriaS002ContactState','Touch'),('AriaPlayObservationComponent','Output'),('AriaS002Decision','Output'),('AriaSkirmishIntent','Intent'),('AriaS002Intent','Intent'),('AriaPlayObservationKind','Kind'),('AriaS002DecisionKind','Kind'),('OutputKind','Kind'),('.Admitted','.Available'),('.CatalogOpen','.DrawerOpen')]:method=method.replace(x,y)
  (out/(name+'Adapter.cs')).write_text('using Game.Simulation; public static class '+name.title()+'Adapter {static void Target(TargetValue t,bool world,ref Output o){o.Kind=t.Available?Kind.Control:Kind.Unavailable;o.Id=t.Available?t.Id:0;}\n'+method+'\n}')
 (out/'controls.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 subprocess.run(['dotnet','run','--project',str(out/'controls.csproj')],check=True)
