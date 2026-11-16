"""Validate source-only activation after a corroborated natural terminal event."""
import json
from pathlib import Path


def activation_admitted(observed, terminal_mode, output, requested_ticks):
    if not isinstance(observed,dict): return False
    game=observed.get('source_gameplay_diagnostic')
    bank=game.get('source_bank_diagnostic') if isinstance(game,dict) else None
    production=bank.get('source_production_ticks') if isinstance(bank,dict) else None
    terminal=isinstance(production,dict) and production.get('terminal_after') is True
    active=observed.get('source_simulation_active')
    if not terminal:
        return type(active) is int and active==1
    if not terminal_mode or type(active) is not int or active!=0: return False
    outcome=production.get('objective_outcome_after')
    completed=production.get('completed_ticks');tick=production.get('source_tick_after')
    initial=bank.get('source_experimental_tick')
    initial_tick=initial.get('tick') if isinstance(initial,dict) else None
    if (outcome not in ('Victory','Defeat','Draw') or type(completed) is not int or type(tick) is not int or
        type(initial_tick) is not int or type(requested_ticks) is not int or production.get('requested_ticks')!=requested_ticks or
        not 1<=completed<=requested_ticks or tick!=initial_tick+completed or
        production.get('scope')!='SourceOwnerSubset_NoNativeInputOrReadiness' or
        game.get('native_readiness_claimed') is not False or observed.get('mission_ready') is not False):
        return False
    path=Path(production.get('progress_path',''))
    expected=Path(output).resolve()/'source-progress.jsonl'
    if not path.is_absolute() or path.resolve()!=expected or not expected.is_file(): return False
    try:
        rows=[json.loads(line) for line in expected.read_text().splitlines()]
    except (OSError,ValueError): return False
    if not rows or len(rows)!=production.get('progress_rows'): return False
    last=rows[-1]
    return (last.get('scope')=='SourceOwnerSubset_NotNativeOrFullScheduler' and last.get('reason')=='Terminal' and
        last.get('tick')==tick and last.get('phase')=='Finished' and last.get('terminal') is True and
        last.get('match_outcome')==outcome and last.get('objective_outcome')==outcome and
        last.get('native_input_claimed') is False and last.get('mission_ready') is False)
