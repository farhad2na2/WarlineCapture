#!/usr/bin/env python3
"""Exercise the original expanded planner with explicit S004-shaped fixture facts."""
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
        raise SystemExit("usage: run_compile_probe.py BUILD_RECEIPT OUTPUT_DIR")
    raise SystemExit(module.run(sys.argv[1], sys.argv[2],
                                here / "S004OriginalAriaStepFixture.cs", "S004"))
