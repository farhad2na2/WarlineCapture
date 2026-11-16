from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / "Tools/CI/SkirmishMissionLogicalBindingAuthor.cs").read_text()


def test_author_is_request_driven_and_not_mission_specific():
    assert 'JObject.Parse(File.ReadAllText(args[0]))' in SOURCE
    assert 'request["catalog_id"]' in SOURCE
    assert 'request["seed"]' in SOURCE
    assert 'request["size"]' in SOURCE and 'request["difficulty"]' in SOURCE
    assert 'request["profiles"]' in SOURCE
    assert 'request["campaign_source_path"]' in SOURCE
    assert 'request["graph"]' in SOURCE and 'request["layout"]' in SOURCE
    assert 'request["setup_matrix"]' in SOURCE
    assert not re.search(r'\bS07[45]\b|10480[34]|IndustrialBasin|SkirmishArmy_AirMobile|SkirmishStart_Field', SOURCE)


def test_author_keeps_compiler_and_explicit_nonclaims():
    for required in (
        'SkirmishEngineeringViewProfileFactory.Create(definition, inputs)',
        'SkirmishSetupCompiler.TryCompile',
        'TryCaptureMissionLogicalAuthoringInputs',
        'SkirmishMissionLogicalBindingCompiler.TryCompile',
        'SelectPerspectiveBattleCamera(source.BattleCameraId, source.Cameras.ToArray())',
        'Expected a perspective battle camera.',
        'materializer.LogicalBinding(bindingJson)',
        'SHA256.Create()',
        'Output directory must be fresh and empty',
        'native_pngs_observed\"] = false',
        'screen_fit_verified\"] = false',
        'source_fit_verified\"] = false',
        'runtime_admission\"] = false',
        'mission_ready\"] = false',
        'native_camera_observed\"] = false',
        'validation_passed\"] = false',
    ):
        assert required in SOURCE
    assert 'CreateInstance<SkirmishMissionLogicalBindingConfig>' in SOURCE
    assert 'SkirmishRoleIds.ToId(role) + "."' in SOURCE
    assert 'No opening player actor matches a supported required framing role.' in SOURCE
    assert 'ExperimentalCompiledSourceOnlyCandidate' in SOURCE
    assert 'production_ready\"] = false' in SOURCE


def test_runner_contains_pre_output_controls_and_post_run_pin_checks():
    runner = (ROOT / "Tools/CI/run_skirmish_logical_binding_author.py").read_text()
    for required in ("build.verify(pins)", "merge_authoring_graph_pins", "invalid_actor_role_pair", "wrong_proof_identity", "validate_identity_joins", "not probe_out.exists()", "not output.exists()", "'production_ready':False", "native_camera_observed", "mesh_bounds_observed", "validation_passed"):
        assert required in runner
    for required in ('distinct_planning_and_battle', 'shared_planning_and_battle', 'missing_battle_camera', 'duplicate_battle_camera', 'orthographic_battle_camera', 'camera_selection_probe'):
        assert required in runner


if __name__ == "__main__":
    test_author_is_request_driven_and_not_mission_specific()
    test_author_keeps_compiler_and_explicit_nonclaims()
    test_runner_contains_pre_output_controls_and_post_run_pin_checks()
    print("[mission-logical-binding-author] result=Passed focused source checks")
