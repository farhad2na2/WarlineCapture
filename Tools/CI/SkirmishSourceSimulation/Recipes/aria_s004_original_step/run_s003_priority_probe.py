#!/usr/bin/env python3
"""Compare original S003 planner priority with its partial supply bridge on fixture facts."""
import importlib.util
from pathlib import Path
import sys

here = Path(__file__).resolve().parent
shared = here.parent / "aria_s073_original_step" / "run_compile_probe.py"
spec = importlib.util.spec_from_file_location("original_aria_step_probe", shared)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("usage: run_s003_priority_probe.py BUILD_RECEIPT OUTPUT_DIR")
    raise SystemExit(module.run(sys.argv[1], sys.argv[2], here / "S003OriginalPriorityFixture.cs", "S003"))
