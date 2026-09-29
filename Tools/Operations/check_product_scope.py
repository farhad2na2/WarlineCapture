#!/usr/bin/env python3
"""Pure contract/model validation; never launches Unity or certifies input readiness."""
import csv
import os
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]

def main():
    rows = list(csv.DictReader((ROOT / 'Design/Monetization/Mission_Product_Policies_2026-09-28.csv').open()))
    source = (ROOT / 'Assets/Game/Scripts/Missions/Contracts/ContentAccessAuthority.cs').read_text()
    actual = {key: (product, free == 'true') for key, product, free in
              re.findall(r'\["([^"]+)"\] = new\("[^"]+", (\w+), (true|false)\)', source)}
    expected = {row['canonical_id']: ('CampaignProduct' if row['mode'] != 'Operations' else 'OperationsProduct',
                row['free_access_scope'] != 'none') for row in rows}
    if len(rows) != 205 or actual != expected:
        raise RuntimeError('Canonical membership differs from adopted policy register')
    dotnet = Path.home() / '.dotnet/dotnet'
    env = os.environ.copy()
    env['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    with tempfile.TemporaryDirectory(prefix='mission-product-host-') as directory:
        folder = Path(directory)
        includes = [f'Assets/Game/Scripts/Operations/{part}/*.cs' for part in
                    ['Contracts', 'Strategic', 'Tactical', 'Loop', 'Content']]
        includes += ['Assets/Game/Scripts/Missions/Contracts/ContentAccessAuthority.cs',
                     'Assets/Tests/Editor/ImplementedContentAccessChecks.cs',
                     'Assets/Tests/Editor/Operations/OperationsProductScopeChecks.cs',
                     'Tools/Operations/OperationsCheckpointCompatibilityChecks.cs']
        xml = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'
        xml += ''.join(f'<Compile Include="{ROOT / path}" />' for path in includes)
        xml += '<Compile Include="Runner.cs" /></ItemGroup></Project>'
        (folder / 'Host.csproj').write_text(xml)
        (folder / 'Runner.cs').write_text('''using System;
using Game.Tests.Editor.Operations;
class Runner { static int Main() { try {
ImplementedContentAccessChecks.RunAll(); Console.WriteLine(ImplementedContentAccessChecks.PassMarker);
OperationsProductScopeChecks.RunAll(); Console.WriteLine(OperationsProductScopeChecks.PassMarker);
OperationsCheckpointCompatibilityChecks.Run();
return 0; } catch(Exception e) { Console.Error.WriteLine(e); return 1; } } }
''')
        result = subprocess.run([str(dotnet) if dotnet.exists() else 'dotnet', 'run', '--project', str(folder / 'Host.csproj'), '-c', 'Release'], env=env)
        return result.returncode

if __name__ == '__main__':
    raise SystemExit(main())
