"""Generate synthetic objective-rule probes from the actual catalog/setup matrix."""
import argparse
import copy
import json
from pathlib import Path
import sys
sys.dont_write_bytecode = True
from build_skirmish_authoring_batches import rows, validate
from skirmish_offline_sim import sha

T = 10_000_000


def case(sid, family, size, deadline, mode):
    originals = [f"original.{i}" for i in range(12 if family == "BT" else 3 if family == "CE" else 0)]
    areas = [f"area.{i}" for i in range({"BA": 0, "FC": 3, "BT": 2, "CE": 1}[family])]
    common = dict(Playing=True, Paused=False, SimulationDeltaTicks=T)
    zero = dict(PlayerEligibleInfantry=0, EnemyEligibleInfantry=0)
    if family == "BA":
        facts = dict(PlayerDesignatedAlive=True, EnemyDesignatedAlive=True,
                     ReplacementBarracksPresent=False, FieldArmyWiped=False, Playing=True,
                     Paused=False, Surrender=False, ElapsedSeconds=0, DeadlineSeconds=deadline)
    elif family == "FC":
        facts = dict(**common, OriginalBaseFactsPresent=True, PlayerOriginalBaseAlive=True,
                     EnemyOriginalBaseAlive=True, ZoneA=dict(zero), ZoneB=dict(zero), ZoneC=dict(zero))
    elif family == "BT":
        facts = dict(**common, CorridorA=dict(zero), CorridorB=dict(zero), OriginalSubjects=[
            dict(StableOriginalId=i, AuthoritativeLifeKnown=True, Alive=True, Dismounted=True, InsideExit=False)
            for i in originals])
    else:
        facts = dict(**common, OriginalTrucks=[
            dict(StableOriginalId=i, AuthoritativeLifeKnown=True, Alive=True, InsideDestination=False)
            for i in originals])
    expected = dict(Outcome="None", Reason="None", Terminal=False)
    steps = []
    if mode == "pause":
        facts["Paused"] = True
        if family != "BA":
            facts["SimulationDeltaTicks"] = deadline*T
            expected["ElapsedTicks"] = 0
    elif mode == "deadline":
        if family == "BA":
            facts["ElapsedSeconds"] = deadline
        else:
            facts["SimulationDeltaTicks"] = deadline*T
            expected["DeadlineTicks"] = deadline*T
        expected.update(Outcome="Draw" if family in ("BA", "FC") else "Defeat", Terminal=True,
                        Reason={"BA": "TimeLimit", "FC": "TicketsAtDeadline", "BT": "ObjectiveDeadline", "CE": "ObjectiveDeadline"}[family])
    elif mode in ("win", "lose", "surrender"):
        win = mode != "lose"
        expected.update(Outcome="Victory" if win else "Defeat", Terminal=True)
        if family == "BA":
            facts["EnemyDesignatedAlive" if win else "PlayerDesignatedAlive"] = False
            expected["Reason"] = "MainBaseDestroyed"
        elif family == "FC":
            for key in ("ZoneA", "ZoneB"):
                facts[key]["PlayerEligibleInfantry" if win else "EnemyEligibleInfantry"] = 1
            expected["Reason"] = "TicketsDepleted"
        elif family == "BT":
            if win:
                opening = copy.deepcopy(facts)
                opening["CorridorA"]["PlayerEligibleInfantry"] = 1
                steps.append(dict(facts=opening, repeat=20, expect={"ExitOpen": True, "Outcome": "None"}))
                for row in facts["OriginalSubjects"][:8]:
                    row["InsideExit"] = True
            else:
                for row in facts["OriginalSubjects"][:5]:
                    row["Alive"] = False
            expected["Reason"] = "BreakthroughEvacuated" if win else "BreakthroughImpossible"
        else:
            for row in facts["OriginalTrucks"][:2]:
                row["InsideDestination" if win else "Alive"] = True if win else False
            expected["Reason"] = "ConvoyDelivered" if win else "ConvoyImpossible"
        if mode == "surrender":
            steps.insert(0, dict(surrender_playing=True))
            # No intermediate open-corridor assertion applies after terminal surrender.
            for step in steps:
                step.pop("expect", None)
            expected.update(Outcome="Defeat", Reason="Surrender")
    else:
        raise ValueError("Unknown synthetic probe mode")
    repeat = 600 if family == "FC" and mode in ("win", "lose", "surrender") else (
        3 if family == "BT" and mode in ("win", "surrender") else
        5 if family == "CE" and mode in ("win", "surrender") else 1)
    steps.append(dict(facts=facts, repeat=repeat))
    return dict(id=f"{sid}.{size}.{mode}", family=family, size=size, area_ids=areas,
                original_ids=originals, steps=steps, expect=expected)


def generate(project):
    root = Path(project)
    catalog_path = root/"Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv"
    matrix_path = root/"Design/Roadmap/Skirmish_Expansion/INITIAL_SETUP_MATRIX.csv"
    catalog, matrix = rows(catalog_path), rows(matrix_path)
    validate(catalog, matrix)
    kinds = {row["scenario_id"]: row["objective_id"] for row in catalog}
    cases = [case(row["catalog_id"], kinds[row["catalog_id"]], row["size_id"],
                  int(row["deadline_seconds"]), mode)
             for row in matrix for mode in ("pause", "deadline", "win", "lose", "surrender")]
    return dict(schema_version=1, cases=cases), {
        "scope": "Synthetic objective probes parameterized by catalog and size deadlines; not real mission journeys",
        "catalog_sha256": sha(catalog_path), "matrix_sha256": sha(matrix_path),
        "catalog_entries": len(catalog), "size_vectors": len(matrix), "cases": len(cases),
        "mission_qualified": False, "full_battle_simulated": False}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    from skirmish_offline_sim import new_output
    output = new_output(args.project, args.output)
    suite, manifest = generate(args.project)
    (output/"cases.json").write_text(json.dumps(suite, indent=2)+"\n")
    (output/"manifest.json").write_text(json.dumps(manifest, indent=2)+"\n")
    print(json.dumps(manifest))
