# APH-802 Editor P95 Run

APH802-Artifact-Id: aph802-editor-p95-YYYYMMDD-HHMMSS-run01
APH802-Exact-Commit: 0000000000000000000000000000000000000000

This Markdown file must share its stem with the run JSON. Replace both markers with the JSON `artifactId` and `exactCommit`. Preserve every run pair under a unique name; never overwrite a prior capture.

Author the companion JSON against `Tools/CI/aph802_editor_p95_run.schema.json`. After at least five accepted same-revision pairs exist in a dedicated directory, generate a new append-only summary pair:

```bash
python3 Tools/CI/aph802_editor_p95_series.py \
  --input-dir Design/AgentReports/aph802/<series-id>/runs \
  --expected-commit <40-character-commit> \
  --max-age-hours 24 \
  --output-json Design/AgentReports/aph802/<series-id>/summary.json \
  --output-markdown Design/AgentReports/aph802/<series-id>/summary.md
```

## Capture Notes

- Fresh Unity process:
- Background-load check:
- Thermal/contention check:
- Ready/stable gate notes:
- Rejection or declared-outlier rationale:
