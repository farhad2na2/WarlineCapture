from pathlib import Path
import importlib.util


ROOT = Path(__file__).resolve().parents[4]
SOURCE = (ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/project_current_authoring_logical_binding.py").read_text()
SPEC = importlib.util.spec_from_file_location("logical_binding_projector", ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/project_current_authoring_logical_binding.py")
PROJECTOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROJECTOR)


def test_projector_consumes_existing_current_owner_inputs():
    assert "SkirmishMissionAuthoringDraft" in SOURCE
    assert "run_skirmish_source_simulation as source_cli" in SOURCE
    assert "Fresh compiler-selected authoring result drifted from retained r3 selection" in SOURCE
    for required in (
        "invocation.get(\"status\") == \"ConfigurationCompiled\"",
        'sha(replay_request_path) == author_pins.get(str(replay_request_path), {}).get("sha256")',
        "replay_request.get(\"binding\") == binding_descriptor",
        "result[\"resolved_setup\"][\"setup_hash\"]",
        "materializer.LogicalBinding",
        "DecodeDeclaredFields",
        "PortableLogicalValue(binding,0)",
    ):
        assert required in SOURCE


def test_projector_roundtrips_compiler_and_rejects_tampering():
    for required in (
        "TryCompile(definition",
        "TryCompile(decodedDefinition",
        "decodedBinding.Fingerprint==compiledBinding.Fingerprint",
        "wrong_source_content_hash_rejected",
        "missing_role_framing_rejected",
        "OriginalCompilerDeclaredLogicalBinding",
        "metadata_compiler_context\": factory_descriptor",
        "OriginalCompilerDerivedNativeFactoryContext",
        "Qualified factory-context consumed input",
        "verify_pin_map(host_pins, \"Source host after replay\")",
        '["world_allocated"]=false',
        '["mission_ready"]=false',
    ):
        assert required in SOURCE


def test_source_evidence_accepts_both_owned_log_roots_and_rejects_symlinks(tmp_path, monkeypatch):
    monkeypatch.setattr(PROJECTOR, "ROOT", tmp_path)
    for root_name in ("Logs", "T Logs"):
        root = tmp_path / root_name / "qualification"
        root.mkdir(parents=True)
        evidence = root / "qualification.json"
        evidence.write_text("{}")
        assert PROJECTOR.repository_source_evidence(evidence, "qualification") == evidence
    outside = tmp_path / "outside.json"
    outside.write_text("{}")
    try:
        PROJECTOR.repository_source_evidence(outside, "qualification")
    except ValueError:
        pass
    else:
        raise AssertionError("Evidence outside owned log roots was accepted")
    alias = tmp_path / "Logs" / "qualification" / "alias.json"
    alias.symlink_to(alias.parent / "qualification.json")
    try:
        PROJECTOR.repository_source_evidence(alias, "qualification")
    except ValueError:
        pass
    else:
        raise AssertionError("Symlinked evidence was accepted")


if __name__ == "__main__":
    test_projector_consumes_existing_current_owner_inputs()
    test_projector_roundtrips_compiler_and_rejects_tampering()
    print("[current-authoring-logical-binding-projection] result=Passed focused source checks")
