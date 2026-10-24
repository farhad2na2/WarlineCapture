#!/usr/bin/env python3
"""Exercise long-session protocol bounds in-process; no gameplay/native evidence."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess


PROBE = r'''
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
class Probe {
    static readonly BindingFlags Fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Session;
    static object Fresh(int previous) {
        object value = Activator.CreateInstance(Session, true);
        Session.GetField("initialized", Fields).SetValue(value, true);
        Session.GetField("lastRequest", Fields).SetValue(value, previous);
        return value;
    }
    static object Request(object session, int id) {
        using var json = JsonDocument.Parse("{\"request_id\":"+id+",\"op\":\"close\"}");
        return Session.GetMethod("Handle", Fields).Invoke(session, new object[]{json.RootElement});
    }
    static void Contact(int previous, bool succeeds) {
        var value = Fresh(0);
        Session.GetField("nextContact", Fields).SetValue(value, previous);
        var field = Session.GetField("session", Fields);
        var state = field.GetValue(value);
        var phase = state.GetType().GetField("Phase");
        phase.SetValue(state, Enum.Parse(phase.FieldType, "Touching"));
        state.GetType().GetField("TargetId").SetValue(state, 42);
        field.SetValue(value, state);
        var contextType = Session.GetNestedType("Context", BindingFlags.NonPublic);
        var context = Activator.CreateInstance(contextType);
        contextType.GetField("Frame", Fields).SetValue(context, 1);
        contextType.GetField("Now", Fields).SetValue(context, 10f);
        bool accepted = false;
        try {
            Session.GetMethod("StartGesture", Fields).Invoke(value,
                new object[]{context, new Dictionary<int,string>{{42,"probe-control"}}});
            accepted = true;
        } catch (TargetInvocationException) { }
        if (accepted != succeeds || (accepted && (int)Session.GetField("nextContact",Fields).GetValue(value) != previous+1))
            throw new Exception("Contact boundary violated");
    }
    static int Main(string[] args) {
        var resolver = new AssemblyDependencyResolver(args[0]);
        AssemblyLoadContext.Default.Resolving += (context, name) => {
            var path = resolver.ResolveAssemblyToPath(name);
            return path == null ? null : context.LoadFromAssemblyPath(path);
        };
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(args[0]);
        Session = assembly.GetType("Program", true).GetNestedType("AutonomousSession", BindingFlags.NonPublic);
        foreach (int previous in new[]{65535,65536,int.MaxValue-1}) {
            object result = Request(Fresh(previous), previous+1);
            if (!(bool)result.GetType().GetProperty("ok").GetValue(result))
                throw new Exception("Long-session request refused");
        }
        bool refused = false;
        try { Request(Fresh(int.MaxValue), 1); } catch (TargetInvocationException) { refused = true; }
        if (!refused) throw new Exception("Request overflow admitted");
        Contact(65536, true);
        Contact(int.MaxValue, false);
        Console.WriteLine("[AriaProtocolBounds] result=Passed controls=6 synthetic=True nativeExecution=0 hardwareInput=0");
        return 0;
    }
}
'''


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build-dir', required=True)
    parser.add_argument('--evidence', required=True)
    args = parser.parse_args()
    build = Path(args.build_dir).resolve()
    manifest_path = build/'manifest.json'
    manifest = json.loads(manifest_path.read_text())
    pins = {str(manifest_path): sha(manifest_path), **manifest['pins']}

    def verify():
        for name, wanted in pins.items():
            if sha(name) != wanted:
                raise ValueError('Pinned source/build changed: '+name)

    verify()
    output = Path(args.evidence).resolve()
    output.mkdir(parents=True, exist_ok=False)
    (output/'Probe.cs').write_text(PROBE)
    (output/'global.json').write_text((build/'global.json').read_text())
    (output/'NuGet.Config').write_text((build/'NuGet.Config').read_text())
    (output/'Probe.csproj').write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
        '<TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit>'
        '<UseAppHost>false</UseAppHost></PropertyGroup></Project>')
    result = subprocess.run([manifest['dotnet'], 'build', str(output/'Probe.csproj'),
                             '--nologo', '--configfile', str(output/'NuGet.Config')],
                            cwd=output, text=True, capture_output=True, timeout=120)
    (output/'build.log').write_text(result.stdout+result.stderr)
    if result.returncode:
        raise ValueError('Probe compilation failed: '+str(output/'build.log'))
    result = subprocess.run([manifest['dotnet'], str(output/'bin/Debug/net8.0/Probe.dll'), manifest['host']],
                            cwd=output, text=True, capture_output=True, timeout=30)
    (output/'probe.log').write_text(result.stdout+result.stderr)
    verify()
    marker = '[AriaProtocolBounds] result=Passed controls=6'
    if result.returncode or marker not in result.stdout:
        raise ValueError('Probe failed: '+str(output/'probe.log')+'\n'+result.stdout+result.stderr)
    print(result.stdout.strip())


if __name__ == '__main__':
    main()
