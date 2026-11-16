"""Add a separately opted-in flow probe to an existing generated S073 owner runner."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path


ADMISSION_ANCHOR = "        JToken uiShellStateOwnerDiagnosticOption = run[\"s073_ui_shell_state_owner_diagnostic\"]"
OWNER_CALL = "uiShellStateOwnerInitialization = SourceS073UiShellStateOwnerBootstrap.Initialize(em, host);"
PROFILE_ANCHOR = '            ["ui_shell_state_owner_initialization"] = uiShellStateOwnerInitialization,'
FLOW_ADMISSION = '''        JToken uiShellFlowDiagnosticOption = run["s073_ui_shell_flow_diagnostic"];
        if (uiShellFlowDiagnosticOption != null && uiShellFlowDiagnosticOption.Type != JTokenType.Boolean)
            throw new InvalidDataException("S073 shell flow diagnostic selector must be an explicit boolean");
        bool uiShellFlowDiagnosticRequested = (bool?)uiShellFlowDiagnosticOption == true;
        bool uiShellFlowDiagnosticEnvironmentOptIn = string.Equals(Environment.GetEnvironmentVariable("WARLINE_S073_UI_SHELL_FLOW_DIAGNOSTIC"), "1", StringComparison.Ordinal);
        if (uiShellFlowDiagnosticRequested != uiShellFlowDiagnosticEnvironmentOptIn)
            throw new InvalidDataException("S073 shell flow diagnostic requires matching explicit run-profile and environment opt-ins");
        if (uiShellFlowDiagnosticRequested && !uiShellStateOwnerDiagnosticRequested)
            throw new InvalidDataException("S073 shell flow diagnostic requires the S073 shell owner diagnostic opt-in");
        JObject uiShellFlowInitialization = null;
        if (uiShellFlowDiagnosticRequested)
        {
            uiShellFlowInitialization = SourceS073UiShellFlowBootstrap.Initialize(em, host, uiShellStateOwnerInitialization);
        }'''
FLOW_PROFILE = '            ["ui_shell_flow_diagnostic_admitted"] = uiShellFlowDiagnosticRequested,\n            ["ui_shell_flow_initialization"] = uiShellFlowInitialization,'


def generate(source: Path, output: Path) -> dict:
    source = source.resolve(strict=True)
    output = output.resolve()
    if output.exists():
        raise FileExistsError(f"Fresh output file required; preserving existing source: {output}")
    text = source.read_text(encoding="utf-8")
    if text.count(ADMISSION_ANCHOR) != 1 or text.count(OWNER_CALL) != 1:
        raise ValueError("Input must be the generated S073 owner runner with one owner admission and call")
    if text.count(PROFILE_ANCHOR) != 1:
        raise ValueError("Input must contain one generated owner result profile field")
    admission_start = text.index(ADMISSION_ANCHOR)
    owner_call = text.index(OWNER_CALL)
    admission_end = text.index("\n        }", owner_call) + len("\n        }")
    if owner_call < admission_start or "if (uiShellStateOwnerDiagnosticRequested)" not in text[admission_start:owner_call]:
        raise ValueError("Owner bootstrap must remain inside the explicit owner opt-in block")
    generated = text[:admission_end] + "\n" + FLOW_ADMISSION + text[admission_end:]
    generated = generated.replace(PROFILE_ANCHOR, PROFILE_ANCHOR + "\n" + FLOW_PROFILE, 1)
    restored = generated.replace("\n" + FLOW_ADMISSION, "", 1).replace("\n" + FLOW_PROFILE, "", 1)
    if restored != text:
        raise ValueError("Flow integration changed bytes outside its admission and result fields")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(generated, encoding="utf-8")
    return {
        "source_runner": str(source),
        "source_runner_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
        "output_runner": str(output),
        "output_runner_sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
        "scope": "Independent S073 source-host opt-in; original shell owner and flow methods retained; exactly one no-input flow OnUpdate; no fabricated route, presentation, completion, input, native UI, or readiness claim.",
        "authorized_transformations": [
            "Require matching explicit profile/environment flow opt-ins.",
            "Require owner diagnostic opt-in before flow diagnostic opt-in.",
            "Invoke SourceS073UiShellFlowBootstrap after original owner initialization.",
            "Add flow admission and initialization to derived result JSON.",
        ],
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("owner_runner", type=Path, help="Generated SourceS073LegacyNormalRun.UiShellStateOwner.cs")
    parser.add_argument("output_runner", type=Path, help="New flow-enabled derived runner path")
    parser.add_argument("--receipt", type=Path, help="Optional new JSON provenance receipt path")
    args = parser.parse_args()
    receipt = generate(args.owner_runner, args.output_runner)
    if args.receipt:
        if args.receipt.exists():
            raise FileExistsError(f"Fresh receipt path required: {args.receipt}")
        args.receipt.parent.mkdir(parents=True, exist_ok=True)
        args.receipt.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
