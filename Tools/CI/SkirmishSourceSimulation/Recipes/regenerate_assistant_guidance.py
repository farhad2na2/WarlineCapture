"""Extract four consumed Assistant defaults from the current native short ctor."""
from pathlib import Path
import argparse, hashlib, json, re

CONTRACT = 'Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs'
GATEWAY = 'Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Assistant.cs'
FIELDS = ('TutorialStep', 'TutorialStepCount', 'RecommendationKind', 'RecommendationTargetKind')
def sha(raw): return hashlib.sha256(raw).hexdigest()
def need(ok, reason):
    if not ok: raise ValueError(reason)

def mask(text):
    # Preserve offsets and newlines; only declarations/delimiters are inspected.
    pattern = r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\''
    need('"""' not in text, 'Raw-string source shape needs explicit review')
    return re.sub(pattern, lambda m: ''.join('\n' if c == '\n' else ' ' for c in m[0]), text)

def balanced(code, start, opening, closing):
    need(code[start] == opening, 'Expected source delimiter')
    depth = 1
    for i in range(start + 1, len(code)):
        depth += (code[i] == opening) - (code[i] == closing)
        if depth == 0: return i + 1
    raise ValueError('Unclosed source delimiter')

def unique(code, pattern, label):
    matches = list(re.finditer(pattern, code))
    need(len(matches) == 1, 'Unique current source ' + label + ' required')
    return matches[0]

def arity(code, start):
    end = balanced(code, start, '(', ')')
    depth = 0; count = 1
    for c in code[start + 1:end - 1]:
        if c in '([{<': depth += 1
        elif c in ')]}>': depth -= 1
        elif c == ',' and depth == 0: count += 1
    return count

def generate(repo, output):
    repo, output = Path(repo), Path(output)
    inputs = {rel: (repo / rel).read_bytes() for rel in (CONTRACT, GATEWAY)}
    texts = {rel: raw.decode('utf-8').replace('\r\n', '\n') for rel, raw in inputs.items()}
    code = mask(texts[CONTRACT])
    owner = unique(code, r'public\s+readonly\s+struct\s+UiAssistantPanelModel\b', 'Assistant owner')
    start = code.index('{', owner.end()); end = balanced(code, start, '{', '}')
    owner_code = code[start:end]
    ctor = unique(owner_code, r'public\s+UiAssistantPanelModel\s*\(\s*uint\s+version\s*,\s*string\s+goalsText\s*,', 'short constructor')
    signature_start = start + ctor.start()
    paren = code.index('(', signature_start)
    need(arity(code, paren) == 17, 'Native Assistant short constructor arity changed')
    body_start = code.index('{', balanced(code, paren, '(', ')'))
    body_end = balanced(code, body_start, '{', '}')
    body = code[body_start:body_end]
    expressions = {}
    for field in FIELDS:
        assignment = unique(body, r'\b' + field + r'\s*=\s*([^;]+);', field + ' assignment')
        expression = assignment[1].strip()
        need(re.fullmatch(r'\d+', expression) is not None and 0 <= int(expression) <= 255,
             'Nonliteral byte default needs full constructor projection: ' + field)
        expressions[field] = expression
    gateway = mask(texts[GATEWAY])
    method = unique(gateway, r'public\s+static\s+bool\s+TryReadMatchHudAssistantPanel\s*\(', 'Assistant gateway')
    method_start = gateway.index('{', method.end()); method_end = balanced(gateway, method_start, '{', '}')
    section = gateway[method_start:method_end]
    branch = unique(section, r'if\s*\(\s*TrySkirmish\s*\(', 'Skirmish Assistant branch')
    condition = method_start + section.index('(', branch.start())
    branch_start = gateway.index('{', balanced(gateway, condition, '(', ')'))
    branch_end = balanced(gateway, branch_start, '{', '}')
    call = unique(gateway[branch_start:branch_end], r'assistantPanel\s*=\s*new\s+UiAssistantPanelModel\s*\(', 'Skirmish short constructor call')
    call_paren = gateway.index('(', branch_start + call.start())
    need(arity(gateway, call_paren) == 17, 'Skirmish no longer calls the native short constructor')
    result = 'namespace Game.Runtime { internal static class SourceAssistantShortConstructorGuidance {\n'
    result += ''.join(' internal const byte ' + field + ' = ' + expressions[field] + ';\n' for field in FIELDS)
    result += '} }\n'
    def recheck():
        need(all((repo / rel).read_bytes() == raw for rel, raw in inputs.items()), 'Native source changed during extraction')
    recheck(); need(not output.exists(), 'New isolated output required'); output.mkdir(parents=True)
    target = output / 'SourceAssistantShortConstructorGuidance.cs'
    target.write_bytes(result.encode('utf-8')); recheck()
    proof = {'kind': 'CurrentNativeAssistantShortConstructorDefaults',
             'source_pins': {rel: sha(raw) for rel, raw in inputs.items()},
             'constructor_arity': 17, 'skirmish_call_arity': 17, 'expressions': expressions,
             'constructor_sha256': sha(texts[CONTRACT][signature_start:body_end].encode()),
             'skirmish_branch_sha256': sha(texts[GATEWAY][branch_start:branch_end].encode()),
             'output_sha256': sha(target.read_bytes()),
             'scope': 'Four consumed byte defaults only; no native UI/input/readiness or other Assistant fields inferred.'}
    (output / 'source-extraction.json').write_bytes((json.dumps(proof, indent=2) + '\n').encode())
    return proof

if __name__ == '__main__':
    parser = argparse.ArgumentParser(); parser.add_argument('--repo', required=True); parser.add_argument('--output', required=True)
    args = parser.parse_args(); generate(args.repo, args.output)
