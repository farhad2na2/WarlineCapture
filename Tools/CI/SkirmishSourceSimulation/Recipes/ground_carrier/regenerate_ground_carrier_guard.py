"""Current native ground-carrier guard; active disembark remains unsupported."""
from pathlib import Path
import argparse, hashlib, json, re
SOURCE = 'Assets/Game/Scripts/Systems/TransportBoardingCommandDisembarkApplication.cs'
METHOD = 'internal static bool CanFinalizeSharedGroundCarrier(EntityManager em, Entity transport)'
OUTPUT = 'TransportBoardingCommandSystem.ground-guard.cs'
def sha(raw): return hashlib.sha256(raw).hexdigest()
def need(ok, message):
    if not ok: raise ValueError(message)
def mask(text):
    pattern = r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\''
    need('"""' not in text, 'Raw string shape needs explicit review')
    return re.sub(pattern, lambda m: ''.join('\n' if c == '\n' else ' ' for c in m[0]), text)
def block_end(code, start):
    depth = 1
    for i in range(start + 1, len(code)):
        depth += (code[i] == '{') - (code[i] == '}')
        if depth == 0: return i + 1
    raise ValueError('Unclosed native method')
def generate(repo, output):
    source, output = Path(repo) / SOURCE, Path(output)
    raw = source.read_bytes(); text = raw.decode('utf-8').replace('\r\n', '\n'); code = mask(text)
    need(code.count(METHOD) == 1, 'Unique current carrier helper required')
    start = code.index(METHOD); brace = code.index('{', start); end = block_end(code, brace)
    method_code = code[start:end]
    matches = list(re.finditer(r'if\s*\(\s*cargo\.Count\s*==\s*0\s*\)\s*return\s+true\s*;', method_code))
    need(len(matches) == 1, 'Unique source empty-cargo success boundary required')
    cutoff = start + matches[0].end(); prefix = text[start:cutoff]
    code_prefix = code[start:cutoff]
    need('TryDisembarkTransport' not in code_prefix and code[cutoff:end].count('TryDisembarkTransport(') == 1,
         'Original active disembark continuation boundary changed')
    need(re.search(r'\bem\.(?:SetComponent\w*|AddComponent\w*|RemoveComponent\w*|DestroyEntity|CreateEntity|Instantiate)\s*(?:<[^>]*>)?\s*\(', code_prefix) is None,
         'Guard now mutates actor state; requires complete native owner domain')
    # The tail is a declared admission rejection, never a replacement disembark rule.
    result = 'using System.Collections.Generic;using Unity.Collections;using Unity.Entities;using Game.Components;namespace Game.Runtime{public partial struct TransportBoardingCommandSystem{'
    result += prefix + '\n            throw new System.NotSupportedException("Active carrier cargo requires the original disembark continuation");\n        }}}\n'
    need(source.read_bytes() == raw, 'Native source changed during extraction')
    need(not output.exists(), 'New isolated output required'); output.mkdir(parents=True)
    target = output / OUTPUT; target.write_bytes(result.encode())
    need(source.read_bytes() == raw, 'Native source changed during emission')
    proof = {'kind': 'CurrentNativeGroundCarrierGuardPrefix', 'source': SOURCE, 'raw_source_sha256': sha(raw),
             'method': METHOD, 'method_sha256': sha(text[start:end].encode()), 'prefix_sha256': sha(prefix.encode()),
             'output_sha256': sha(target.read_bytes()), 'native_prefix_bytes_preserved': True,
             'active_disembark': 'Explicit unsupported domain; no native cargo writes, occupancy publication or fake success.',
             'scope': 'Actual no-work/ownership/session/passenger consistency prefix only; native active continuation omitted.'}
    (output / 'source-extraction.json').write_bytes((json.dumps(proof, indent=2) + '\n').encode())
    return proof
if __name__ == '__main__':
    p = argparse.ArgumentParser(); p.add_argument('--repo', required=True); p.add_argument('--output', required=True)
    a = p.parse_args(); generate(a.repo, a.output)
