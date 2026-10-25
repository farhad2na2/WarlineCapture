"""Finite owned CLI module epoch; no import hook or system Python changes."""
from pathlib import Path
import hashlib
import sys
import types


class OwnedSourceError(ValueError):
    pass


def _sha(raw):
    return hashlib.sha256(raw).hexdigest()


class _Epoch:
    def __init__(self, path, digest, module):
        self.poisoned = False
        self.records = {}
        self.modules = {}
        self.cli_root = None
        self._record('skirmish_owned_source', path, digest, module, True)
        self.verify()

    def fail(self, message):
        self.poisoned = True
        raise OwnedSourceError(message)

    def ready(self):
        if self.poisoned:
            raise OwnedSourceError('Owned Python source epoch is poisoned; start a fresh CLI process')

    def _record(self, name, path, digest, module, complete):
        path = str(Path(path).resolve(strict=True))
        if path in self.records and self.records[path] != digest:
            self.fail('Conflicting executed owned-source epoch: ' + path)
        self.records[path] = digest
        self.modules[name] = (path, digest, module, complete)

    def verify(self):
        self.ready()
        try:
            for name, (path, digest, module, complete) in self.modules.items():
                if not complete or sys.modules.get(name) is not module:
                    self.fail('Owned module binding changed or incomplete: ' + name)
                if str(Path(module.__file__).resolve(strict=True)) != path:
                    self.fail('Owned module origin changed: ' + name)
            for path, digest in self.records.items():
                if _sha(Path(path).read_bytes()) != digest:
                    self.fail('Owned Python source changed since execution: ' + path)
        except BaseException:
            self.poisoned = True
            raise
        return dict(self.records)

    def load(self, name, path):
        self.ready()
        path = Path(path).resolve(strict=True)
        if path.suffix != '.py' or path.stat().st_size > 2 * 1024 * 1024:
            self.fail('Bounded owned Python source required: ' + str(path))
        raw = path.read_bytes()
        if len(raw) > 2 * 1024 * 1024:
            self.fail('Owned Python source grew beyond bound: ' + str(path))
        digest = _sha(raw)
        if name in self.modules:
            prior_path, prior_sha, module, complete = self.modules[name]
            if not complete or sys.modules.get(name) is not module or prior_path != str(path) or prior_sha != digest:
                self.fail('Owned module path/source epoch changed: ' + name)
            self.verify()
            return module
        if name in sys.modules:
            self.fail('Unowned module collision: ' + name)
        module = types.ModuleType(name)
        module.__file__ = str(path)
        sys.modules[name] = module
        self._record(name, path, digest, module, False)
        try:
            exec(compile(raw, str(path), 'exec'), module.__dict__)
            if path.read_bytes() != raw:
                self.fail('Owned source drifted during execution: ' + str(path))
            if sys.modules.get(name) is not module or str(Path(module.__file__).resolve(strict=True)) != str(path):
                self.fail('Owned module replaced its admitted binding: ' + name)
            self.modules[name] = (str(path), digest, module, True)
        except BaseException:
            self.poisoned = True
            if sys.modules.get(name) is module:
                del sys.modules[name]
            raise
        return module


if '__executed_source_pin__' not in globals():
    raise OwnedSourceError('Owned loader requires the direct-entry exact-byte bootstrap')
_self_pin = globals()['__executed_source_pin__']
_epoch = _Epoch(_self_pin['path'], _self_pin['sha256'], sys.modules[__name__])


def load_cli(root):
    try:
        root = Path(root).resolve(strict=True)
        if _epoch.cli_root is not None and _epoch.cli_root != root:
            _epoch.fail('Owned CLI root changed within this process')
        _epoch.cli_root = root
        for name in ['skirmish_source_syntax', 'skirmish_production_worker', 'skirmish_causal_worker',
                     'skirmish_source_manifest', 'skirmish_source_build']:
            _epoch.load(name, root / (name + '.py'))
        return _epoch.verify()
    except BaseException:
        _epoch.poisoned = True
        raise


def load_placement(root):
    try:
        root = Path(root).resolve(strict=True)
        _epoch.verify()
        _epoch.load('regenerate_placement_numeric', root / 'regenerate_placement_numeric.py')
        module = _epoch.load('source_placement_build', root / 'build_source_placement_domain.py')
        _epoch.verify()
        return module
    except BaseException:
        _epoch.poisoned = True
        raise


def require(name):
    _epoch.verify()
    if name not in _epoch.modules:
        _epoch.fail('Owned module was not admitted: ' + name)
    return _epoch.modules[name][2]


def module_pin(name, path):
    # Called by Manifest while its own exact bytes are still executing.
    _epoch.ready()
    if name not in _epoch.modules:
        _epoch.fail('No executed-source pin for module: ' + name)
    admitted_path, digest, module, _ = _epoch.modules[name]
    if admitted_path != str(Path(path).resolve(strict=True)) or sys.modules.get(name) is not module:
        _epoch.fail('Wrong owned module origin at pin request: ' + name)
    return digest


def pins():
    return _epoch.verify()
