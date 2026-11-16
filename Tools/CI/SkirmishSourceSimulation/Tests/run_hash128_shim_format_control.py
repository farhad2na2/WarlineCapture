#!/usr/bin/env python3
"""Compile and run the focused Hash128 shim/package parity control."""
from __future__ import annotations

import argparse
import subprocess
import tempfile
from pathlib import Path
from xml.sax.saxutils import escape


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[4])
    args = parser.parse_args()
    root = args.root.resolve()
    source = Path(__file__).with_name("Hash128ShimFormattingControl.cs").resolve()
    with tempfile.TemporaryDirectory(prefix="hash128-control-launcher-") as temp:
        project = Path(temp) / "Hash128ShimFormattingControl.csproj"
        project.write_text(
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>"
            "<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>"
            "<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable>"
            "</PropertyGroup><ItemGroup><Compile Include=\""
            + escape(str(source))
            + "\" /></ItemGroup></Project>\n",
            encoding="utf-8",
        )
        return subprocess.run(
            ["dotnet", "run", "--project", str(project), "--configuration", "Release", "-p:NuGetAudit=false", "--", str(root)],
            cwd=root,
            check=False,
        ).returncode


if __name__ == "__main__":
    raise SystemExit(main())
