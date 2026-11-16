#!/usr/bin/env python3
"""Compare the exact original and indexed child scans on bounded synthetic graphs."""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path
import generate

PROGRAM = r'''using System;using System.Collections.Generic;using System.Diagnostics;using System.Linq;
readonly struct Entity:IEquatable<Entity>{public readonly int Index,Version;public Entity(int i,int v=1){Index=i;Version=v;}public bool Equals(Entity o)=>Index==o.Index&&Version==o.Version;public override bool Equals(object o)=>o is Entity e&&Equals(e);public override int GetHashCode()=>HashCode.Combine(Index,Version);public static bool operator==(Entity a,Entity b)=>a.Equals(b);public static bool operator!=(Entity a,Entity b)=>!a.Equals(b);}
static class P{
 static void Need(bool v,string why){if(!v)throw new InvalidOperationException(why);}
 ORIGINAL
 CANDIDATE
 static string Run(Func<IEnumerable<Entity>,Dictionary<Entity,Entity>,Dictionary<Entity,Entity[]>,Dictionary<Entity,List<Entity>>> f,Entity[] nodes,Dictionary<Entity,Entity> parents,Dictionary<Entity,Entity[]> old){try{var v=f(nodes,parents,old);return string.Join("|",v.Select(p=>p.Key.Index+":"+string.Join(",",p.Value.Select(c=>c.Index+"/"+c.Version))));}catch(Exception e){return "EX:"+e.GetType().FullName+":"+e.Message;}}
 static void Main(){var rng=new Random(711);for(int trial=0;trial<1000;trial++){int n=rng.Next(0,100);var nodes=Enumerable.Range(0,n).Select(i=>new Entity(i)).OrderBy(_=>rng.Next()).ToArray();var parents=new Dictionary<Entity,Entity>();foreach(var e in nodes)if(rng.Next(3)!=0)parents.Add(e,new Entity(rng.Next(n+3)));var old=new Dictionary<Entity,Entity[]>();foreach(var e in nodes)if(rng.Next(4)==0)old.Add(e,Enumerable.Range(0,rng.Next(0,8)).Select(_=>new Entity(rng.Next(n+3))).ToArray());var a=Run(Baseline,nodes,parents,old);var b=Run(Indexed,nodes,parents,old);if(a!=b)throw new Exception("Differential mismatch at "+trial+"\n"+a+"\n"+b);}
 var duplicate=new Entity(5);var parent=new Entity(2);var dupNodes=new[]{parent};var dupParents=new Dictionary<Entity,Entity>{{duplicate,parent}};var dupOld=new Dictionary<Entity,Entity[]>{{parent,new[]{duplicate,duplicate}}};if(!Run(Baseline,dupNodes,dupParents,dupOld).StartsWith("EX:")||Run(Baseline,dupNodes,dupParents,dupOld)!=Run(Indexed,dupNodes,dupParents,dupOld))throw new Exception("Duplicate control failed");
 var large=Enumerable.Range(0,4972).Select(i=>new Entity(i)).ToArray();var rel=new Dictionary<Entity,Entity>();foreach(var e in large)if(e.Index%5!=0)rel.Add(e,new Entity(e.Index/5));var empty=new Dictionary<Entity,Entity[]>();Run(Baseline,large,rel,empty);Run(Indexed,large,rel,empty);var baselines=new List<double>();var candidates=new List<double>();for(int i=0;i<6;i++){foreach(bool isOld in (i%2==0?new[]{true,false}:new[]{false,true})){var clock=Stopwatch.StartNew();var value=Run(isOld?Baseline:Indexed,large,rel,empty);clock.Stop();if(value.Length==0)throw new Exception("empty result");(isOld?baselines:candidates).Add(clock.Elapsed.TotalMilliseconds);}}baselines.Sort();candidates.Sort();Console.WriteLine("[IndexedTransformChildControl] result=Passed random=1000 duplicate=Passed syntheticNodes=4972 baselineMedianMs="+baselines[3].ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" candidateMedianMs="+candidates[3].ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" scope=SyntheticOnly");}}
'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[5]
    output = args.output.resolve()
    if not output.is_relative_to(root):
        raise ValueError('Repository output required')
    source = root / generate.SOURCE
    raw = source.read_bytes()
    original, _ = generate.transform(raw)
    baseline = original.replace('BuildChildren(', 'Baseline(', 1)
    candidate = generate.CANDIDATE.replace('BuildChildren(', 'Indexed(', 1)
    output.mkdir(parents=True, exist_ok=False)
    (output / 'Program.cs').write_text(PROGRAM.replace(' ORIGINAL', baseline).replace(' CANDIDATE', candidate))
    (output / 'Control.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>\n')
    run = subprocess.run(['dotnet', 'run', '--project', str(output / 'Control.csproj'), '--configuration', 'Release'], capture_output=True, text=True, timeout=300)
    (output / 'run.log').write_text(run.stdout + run.stderr)
    marker = '[IndexedTransformChildControl] result=Passed random=1000 duplicate=Passed'
    if run.returncode or sum(line.startswith(marker) for line in run.stdout.splitlines()) != 1 or source.read_bytes() != raw:
        raise RuntimeError('Indexed transform control failed; see preserved run.log')
    receipt = {'status': 'PassedSyntheticControl', 'source_sha256': hashlib.sha256(raw).hexdigest(),
               'generator_sha256': hashlib.sha256(Path(generate.__file__).read_bytes()).hexdigest(),
               'control_sha256': hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
               'log_sha256': hashlib.sha256((output / 'run.log').read_bytes()).hexdigest(),
               'scope': 'Synthetic managed fixture only; no native parity or simulator speed claim', 'mission_ready': False}
    (output / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))


if __name__ == '__main__':
    main()
