#!/usr/bin/env python3
"""Generate the finite NAV facts identity from the ACTUAL compiler source list.

Call after resolving current sources, before csc. No historical source hashes
are accepted or required. Whole-file build pins detect concurrent drift; only
the listed decoder/helper/type slices contribute to the cache producer key.
"""
from __future__ import annotations
import argparse, hashlib, json, re
from pathlib import Path

def sha(data): return hashlib.sha256(data).hexdigest()

def generate(response, output):
    response, output = Path(response), Path(output)
    paths = [Path(x.strip().strip('"')).resolve(strict=True) for x in response.read_text().splitlines()
             if x.strip().strip('"').endswith('.cs') and Path(x.strip().strip('"')).resolve() != output.resolve()]
    captured = {p: p.read_bytes() for p in paths}
    text = {p: b.decode('utf-8') for p, b in captured.items()}
    specs = []
    def one(kind, name):
        pattern = re.compile(r'\b' + kind + r'\s+' + re.escape(name) + r'\b')
        matches = [(p, pattern.search(s)) for p, s in text.items() if pattern.search(s)]
        if len(matches) != 1: raise ValueError(f'Exactly one current {kind} {name} source required')
        return matches[0]
    def add(label, path, start=0, length=None):
        data = captured[path]
        if length is None: length = len(data)
        specs.append(dict(label=label, path=str(path), start=start, length=length,
                          source_sha256=sha(data), slice_sha256=sha(data[start:start+length])))
    for name in ['ExactInitialValueCodec', 'GeneratedAuthoredNavigationImporter', 'SourceNavigationFacts', 'SourceNavigationFactsTool']:
        path, _ = one('class', name); add(name, path)
    for name in ['FixedString128Bytes', 'LocalTransform', 'UnitGrid', 'GridBlockerSize', 'StaticGridBlocker',
                 'OperationMapBuildingComponent', 'RuntimeBuildingCombatInfo', 'UnitHealth', 'Faction',
                 'OperationMapBuildingBlockerPolicy']:
        path, match = one('enum' if name.endswith('BlockerPolicy') else 'struct', name)
        s = text[path]; a = match.start(); brace = s.index('{', match.end()); depth = 1; end = brace + 1
        # Finite admitted declarations have balanced braces in strings/comments.
        # This is intentionally not a permissive general C# parser.
        while depth and end < len(s):
            depth += (s[end] == '{') - (s[end] == '}'); end += 1
        if depth: raise ValueError('Unbalanced finite declaration: ' + name)
        start = len(s[:a].encode('utf-8')); length = len(s[a:end].encode('utf-8'))
        add(name, path, start, length)
    generator = Path(__file__).resolve(); captured[generator] = generator.read_bytes(); add('IdentityGenerator', generator)
    producer = sha(('NAVFACTS-PRODUCER-v1\n' + ''.join(x['label']+'\0'+x['slice_sha256']+'\n' for x in specs)).encode())
    rows = []
    for spec in specs:
        args = [json.dumps(spec['label']), json.dumps(spec['path']), str(spec['start']), str(spec['length']), json.dumps(spec['source_sha256'])]
        rows.append('            new Spec(' + ', '.join(args) + ')')
    code = '''using System;using System.IO;using System.Text;using System.Collections.Generic;
namespace Game.Runtime {
internal static class SourceNavigationFactsIdentity {
    sealed class Spec { internal readonly string Label,Path,SourceSha;internal readonly int Start,Length;
        internal Spec(string label,string path,int start,int length,string sha){Label=label;Path=path;Start=start;Length=length;SourceSha=sha;} }
    static readonly Spec[] Inputs = {
ROWS
    };
    static readonly Lazy<string> value = new Lazy<string>(ReadCurrent);
    internal static string Producer => value.Value;
    static string ReadCurrent() {
        var captured = new Dictionary<string,byte[]>(StringComparer.Ordinal);
        var key = new StringBuilder("NAVFACTS-PRODUCER-v1\\n");
        foreach(var spec in Inputs) {
            if(!captured.TryGetValue(spec.Path,out var bytes)) {
                var file=new FileInfo(spec.Path);
                if(!file.Exists||file.Length>8*1024*1024)throw new InvalidDataException("Finite compiled producer source missing/bounded");
                bytes=File.ReadAllBytes(spec.Path);captured.Add(spec.Path,bytes);
            }
            if(AutonomousWorld.Hash(bytes)!=spec.SourceSha||spec.Start<0||spec.Length<0||spec.Start>bytes.Length-spec.Length)
                throw new InvalidDataException("Compiled producer source changed; rebuild current sources");
            var part=new byte[spec.Length];Array.Copy(bytes,spec.Start,part,0,spec.Length);
            key.Append(spec.Label).Append('\\0').Append(AutonomousWorld.Hash(part)).Append('\\n');
        }
        return AutonomousWorld.Hash(Encoding.UTF8.GetBytes(key.ToString()));
    }
}}
'''.replace('ROWS', ',\n'.join(rows))
    output.parent.mkdir(parents=True, exist_ok=True); output.write_text(code)
    for path, data in captured.items():
        if path.read_bytes() != data: raise ValueError('Producer source changed during identity generation: '+str(path))
    receipt = {'kind':'CurrentNavigationFactsProducerIdentity','producer_sha256':producer,'dependencies':specs,
               'generated_identity_sha256':sha(output.read_bytes()),
               'scope':'Cache key binds exact decoder/importer/format/helper/type declaration bytes; whole-file build pins only reject source drift and are not part of cache key.'}
    output.with_suffix('.json').write_text(json.dumps(receipt,indent=2))
    return receipt

if __name__ == '__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--response',required=True);p.add_argument('--output',required=True);a=p.parse_args()
    print(json.dumps(generate(a.response,a.output),separators=(',',':')))
