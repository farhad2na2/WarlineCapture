"""Extract an explicit-world AI startup compile probe, never an admission gate."""
from pathlib import Path
import hashlib
import types


def generate(repo, output):
    root = Path(repo)
    paths = [root / "Assets/Game/Scripts/Systems/AIStartupSystem.cs",
             root / "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
             root / "Assets/Game/Scripts/Configs/GameplayConfigValueTypes.cs",
             Path(__file__).parent / "operation_map_publication/regenerate_config_metadata.py",
             Path(__file__).parent / "S073AIStartupSourceBinding.cs.txt"]
    raw = [p.read_bytes() for p in paths]
    startup, configs, values = [x.decode("utf-8").replace("\r\n", "\n") for x in raw[:3]]
    lex = types.ModuleType("ai_startup_probe_lexical")
    lex.__file__ = str(paths[3])
    exec(compile(raw[3], str(paths[3]), "exec"), lex.__dict__)
    ai_config = lex.class_span(configs, "AIControllerConfig")

    # Exclude only the implicit-world overload, presentation diagnostics and
    # singleton-world lookup. The explicit EntityManager owner body remains.
    first = startup.index("        public Result Initialize(")
    second = startup.index("        public Result Initialize(", first + 1)
    if startup.count("        public Result Initialize(") != 2:
        raise ValueError("AI startup overload shape changed")
    startup = startup[:first] + startup[second:]
    first = startup.index("        public void LogConfigValidation(")
    second = startup.index("        private void EnsureAIBuildPlansInitialized(", first)
    startup = startup[:first] + startup[second:]
    lookup = lex.member(startup, "TryGetLiveEntityManager")
    startup = startup.replace(lookup, "", 1)

    def exact(old, new):
        nonlocal startup
        if startup.count(old) != 1:
            raise ValueError("AI startup binding changed: " + old)
        startup = startup.replace(old, new, 1)

    exact("            AISettingsSnapshot aiSettings)\n        {\n            if (!em.World.IsCreated)",
          "            AISettingsSnapshot aiSettings, double sourceElapsedTime)\n        {\n            if (!em.World.IsCreated)")
    exact("em.World.Time.ElapsedTime", "sourceElapsedTime")
    exact("Mathf.Lerp(", "SourceAiNativeMath.Lerp(")
    enums = []
    for name in ("AIControllerRole", "AIControllerDifficulty"):
        declaration = "public enum " + name + " : byte"
        if values.count(declaration) != 1:
            raise ValueError("AI enum changed: " + name)
        start = values.index(declaration)
        end = values.index("}", values.index("{", start)) + 1
        enums.append(values[start:end])
    out = Path(output)
    out.mkdir(parents=True, exist_ok=True)
    (out / "AIStartupSystem.probe.cs").write_text(
        "extern alias ActualCore;\n" + startup +
        "\nnamespace Game.Runtime { internal static class SourceAiNativeMath { "
        "internal static float Lerp(float a,float b,float t) => ActualCore::UnityEngine.Mathf.Lerp(a,b,t); } }\n")
    (out / "AIControllerConfig.probe.cs").write_text(
        "using System.Collections.Generic;using UnityEngine;namespace Game.Configs {\n" + ai_config + "\n}\n")
    (out / "AIControllerEnums.probe.cs").write_text("namespace Game.Configs {\n" + "\n".join(enums) + "\n}\n")
    (out / "S073AIStartupSourceBinding.cs").write_bytes(raw[4])
    if any(path.read_bytes() != data for path, data in zip(paths, raw)):
        raise ValueError("AI startup probe source changed during extraction")
    return {"kind": "AIStartupCompileProbe", "source_sha256": [hashlib.sha256(x).hexdigest() for x in raw],
            "scope": "Explicit manager and elapsed time; compile probe only, no native parity or mission admission"}
