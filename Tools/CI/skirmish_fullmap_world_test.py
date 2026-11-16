"""Compile the exact production MovingWorldMap reader and test complete-grid admission.

This focused harness extracts Read verbatim, uses production WorldJson and
GridCoordinateKernel, and replaces only unrelated simulation holder types.
It does not validate scheduling, native parity, or mission readiness.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import shutil
import struct
import subprocess
import tempfile
import unittest

PROJECT = Path(__file__).resolve().parents[2]
OUTPUT = None
FIELDS = dict(walkable=(1, 'u8'), dynamic_blocked=(0, 'u8'), friendly_pass=(0, 'u8'),
              sidewalk=(0, 'u8'), dirt_road=(0, 'u8'), surface_cell=(1, 'u8'),
              surface_footprint=(1, 'u8'), cardinal_cost=(10, 'i32-le'), diagonal_cost=(14, 'i32-le'))


def policy():
    return dict(profile='complete_cell_facts_ordinary_1x1_infantry_v1', complete=True,
                footprint_x=1, footprint_z=1, manual=False, vehicle=False, hierarchical=False,
                surface_policy='synthetic explicit reader controls, not a Campaign policy capture',
                traversal_policy='synthetic explicit evaluated costs')


class FullMapReaderTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.build = OUTPUT / 'reader-build'
        cls.build.mkdir()
        source = (PROJECT / 'Tools/CI/SkirmishOfflineSim/MovingWorldMap.cs').read_text()
        start = source.index('    internal static MovingWorldMap Read(')
        end = source.index('    internal GridCellValue Cell', start)
        reader = source[start:end]
        # The executed reader body is byte-for-byte from the checked-in source.
        (cls.build / 'Reader.cs').write_text('using System;\nusing System.Linq;\nusing System.IO;\nusing System.Security.Cryptography;\nusing System.Buffers.Binary;\nusing System.Text.Json;\nusing Game.Components;\nclass MovingWorldMap {\n'
            + 'internal int Width,Height; internal bool FullMap; internal GridCoordinateFrame Frame; internal byte[] Walkable,Friendly,Sidewalk,Dirt; internal bool[] Blocked,CellSurface,FootprintSurface,Occupied; internal int[] Cardinal,Diagonal,Counts;\n'
            + reader + '}')
        shutil.copyfile(PROJECT / 'Assets/Game/Scripts/Components/GridCoordinateKernel.cs', cls.build / 'Coordinates.cs')
        # The unused namespace import has no runtime dependency in WorldJson.
        validation = (PROJECT / 'Tools/CI/SkirmishOfflineSim/WorldJson.cs').read_text().replace('using Game.Runtime.Combat;', '')
        (cls.build / 'WorldJson.cs').write_text(validation)
        (cls.build / 'Program.cs').write_text('''using System; using System.IO; using System.Text.Json;
class Program { static int Main(string[] args) { try { using var doc=JsonDocument.Parse(File.ReadAllBytes(args[0])); WorldJson.UniqueKeys(doc.RootElement); var map=MovingWorldMap.Read(doc.RootElement,args[2]=="true",args[1]=="null"?null:args[1]); Console.WriteLine(JsonSerializer.Serialize(new{map.Width,map.Height,count=map.Walkable.Length,lastWalkable=map.Walkable[^1],lastCardinal=map.Cardinal[^1],lastFootprint=map.FootprintSurface[^1]})); return 0;} catch(Exception e) {Console.WriteLine(e.Message);return 1;}}}
''')
        (cls.build / 'Reader.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
        (cls.build / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
        result = subprocess.run(['dotnet', 'build', str(cls.build / 'Reader.csproj'), '-o', str(cls.build / 'bin'), '--configfile', str(cls.build / 'NuGet.Config')], capture_output=True, text=True)
        (OUTPUT / 'build.log').write_text(result.stdout + result.stderr)
        if result.returncode: raise AssertionError(result.stdout + result.stderr)
        (OUTPUT / 'reader-source.json').write_text(json.dumps(dict(source='Tools/CI/SkirmishOfflineSim/MovingWorldMap.cs', sha256=hashlib.sha256(source.encode()).hexdigest(), reader_sha256=hashlib.sha256(reader.encode()).hexdigest(), scope='reader_only', mission_qualified=False), indent=2))

    def setUp(self):
        self.root = OUTPUT / self._testMethodName
        self.root.mkdir()
        self.index = 0

    def facts(self, width=16, height=5, full=False):
        value = dict(width=width,height=height,coordinates=dict(OriginX=0,OriginY=0,OriginZ=0,CellSize=1),provenance='synthetic complete reader controls')
        for name,(default,encoding) in FIELDS.items():
            if not full: value[name] = [default]*(width*height)
            else:
                raw = (bytes([default]) if encoding=='u8' else struct.pack('<i',default))*(width*height)
                path=self.root/(name+'.bin');path.write_bytes(raw)
                value[name]=dict(path=str(path),sha256=hashlib.sha256(raw).hexdigest(),bytes=len(raw),encoding=encoding)
        if full:value['full_map_policy']=policy()
        return value

    def read(self, value, allow=True, root=True):
        self.index += 1
        path=self.root/f'input-{self.index}.json';path.write_text(json.dumps(value))
        r=subprocess.run(['dotnet',str(self.build/'bin/Reader.dll'),str(path),str(self.root) if root else 'null',str(allow).lower()],capture_output=True,text=True)
        (self.root/f'result-{self.index}.log').write_text(r.stdout+r.stderr)
        return r

    def rejects(self, value, text, **kwargs):
        result=self.read(value, **kwargs);self.assertEqual(result.returncode,1,result.stdout);self.assertIn(text,result.stdout)

    def test_legacy_small_inline_and_dimension_guard(self):
        result=self.read(self.facts());self.assertEqual(result.returncode,0,result.stdout)
        self.rejects(self.facts(65,1),'1..64')

    def test_entire_2048_by_1024_grid_and_last_cell_facts(self):
        value=self.facts(2048,1024,True)
        for name,last in [('walkable',0),('surface_footprint',0),('cardinal_cost',987)]:
            a=value[name];path=Path(a['path']);raw=bytearray(path.read_bytes());stride=1 if a['encoding']=='u8' else 4
            raw[-stride:]=bytes([last]) if stride==1 else struct.pack('<i',last);path.write_bytes(raw);a['sha256']=hashlib.sha256(raw).hexdigest()
        result=self.read(value);self.assertEqual(result.returncode,0,result.stdout)
        self.assertEqual(json.loads(result.stdout),dict(Width=2048,Height=1024,count=2097152,lastWalkable=0,lastCardinal=987,lastFootprint=False))

    def test_profile_root_and_complete_policy_required(self):
        value=self.facts(65,2,True)
        self.rejects(value,'multi-unit',allow=False)
        self.rejects(value,'artifact root',root=False)
        for field,bad in [('complete',False),('manual',True),('vehicle',True),('hierarchical',True),('footprint_x',2),('profile','other')]:
            changed=copy.deepcopy(value);changed['full_map_policy'][field]=bad;self.rejects(changed,'Unsupported or incomplete')
        changed=copy.deepcopy(value);del changed['full_map_policy']['surface_policy'];self.rejects(changed,'Missing fields')

    def test_no_partial_inline_omitted_or_fictional_defaults(self):
        value=self.facts(65,2,True)
        for name in FIELDS:
            changed=copy.deepcopy(value);del changed[name];self.rejects(changed,'Missing fields')
        changed=copy.deepcopy(value);changed['surface_cell']=[1]*130;self.rejects(changed,'Complete captured artifacts')
        changed=self.facts();changed['surface_cell'].pop();self.rejects(changed,'Complete captured artifacts')

    def test_hash_size_encoding_and_values_fail_closed(self):
        value=self.facts(65,2,True)
        for field,bad,text in [('sha256','0'*64,'bytes differ'),('bytes',1,'shape/encoding'),('encoding','i32-le','shape/encoding')]:
            changed=copy.deepcopy(value);changed['walkable'][field]=bad;self.rejects(changed,text)
        for name,raw in [('walkable',bytes([2])*130),('cardinal_cost',struct.pack('<i',0)*130),('diagonal_cost',struct.pack('<i',10001)*130)]:
            changed=copy.deepcopy(value);path=self.root/(name+'-invalid.bin');path.write_bytes(raw);changed[name].update(path=str(path),sha256=hashlib.sha256(raw).hexdigest());self.rejects(changed,'Invalid '+name)
        path=Path(value['walkable']['path']);path.write_bytes(path.read_bytes()[:-1]);self.rejects(value,'length differs')

    def test_escape_symlink_and_dimension_overflow_rejected(self):
        value=self.facts(65,2,True)
        changed=copy.deepcopy(value);changed['walkable']['path']=str(self.root.parent/'outside.bin');self.rejects(changed,'Artifact escape')
        link=self.root/'link.bin';link.symlink_to(Path(value['walkable']['path']));changed=copy.deepcopy(value);changed['walkable']['path']=str(link);self.rejects(changed,'symlink')
        for width,height in [(0,1),(2**31-1,2**31-1),(16777217,1)]:
            changed=copy.deepcopy(value);changed.update(width=width,height=height);self.rejects(changed,'1..16777216')


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    OUTPUT=args.output.resolve();OUTPUT.mkdir(parents=True,exist_ok=False)
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(FullMapReaderTests))
    print(f"[SkirmishFullMapReaderTests] result={'Passed' if result.wasSuccessful() else 'Failed'} tests={result.testsRun} skipped={len(result.skipped)} native_runs=0 mission_qualified=false")
    raise SystemExit(0 if result.wasSuccessful() else 1)
