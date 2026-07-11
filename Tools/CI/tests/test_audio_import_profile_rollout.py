import importlib.util
import json
import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "Tools/Audio/apply_audio_import_profiles.py"
SPEC = importlib.util.spec_from_file_location("audio_import_profiles", MODULE_PATH)
PROFILES = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROFILES)


class AudioImportProfileRolloutTests(unittest.TestCase):
    PILOT_PATHS = {
        "Assets/Game/Audio/Voice/ARIA/aria_message_confirm_destroy_01.wav",
        "Assets/Game/Audio/Voice/ARIA/aria_message_not_enough_money_01.wav",
        "Assets/Game/Audio/Voice/ARIA/aria_message_warning_ground_attack_type_01.wav",
        "Assets/Game/Audio/Voice/ARIA/aria_message_warning_air_attack_type_01.wav",
        "Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_no_selection_01.wav",
        "Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_move_01.wav",
        "Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_attack_01.wav",
        "Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_contacts_01.wav",
    }

    def test_voice_profile_owns_the_accepted_pilot_policy_without_overrides(self):
        config = json.loads(PROFILES.PROFILE_PATH.read_text(encoding="utf-8"))

        self.assertNotIn("VoicePilot", config["profiles"])
        self.assertEqual([], config["overrides"])
        self.assertEqual(
            {
                "loadType": "CompressedInMemory",
                "compressionFormat": "Vorbis",
                "forceToMono": True,
                "preloadAudioData": False,
                "loadInBackground": True,
                "sampleRateOverride": 44100,
            },
            config["profiles"]["Voice"],
        )

    def test_aph405_android_evidence_set_remains_the_original_eight_clips(self):
        config = json.loads(PROFILES.PROFILE_PATH.read_text(encoding="utf-8"))
        pilot_paths = config["validationSets"]["APH405VoicePilot"]

        self.assertEqual(8, len(pilot_paths))
        self.assertEqual(self.PILOT_PATHS, set(pilot_paths))
        self.assertEqual(8, len(set(pilot_paths)))

    def test_all_catalog_voice_importers_match_the_category_profile(self):
        profiles, overrides = PROFILES.load_profile_config()
        self.assertEqual({}, overrides)

        voice_paths = [
            path
            for path in PROFILES.load_catalog_clip_paths()
            if PROFILES.category_for(path) == "Voice"
        ]
        self.assertEqual(163, len(voice_paths))

        expected = profiles["Voice"]
        for path in voice_paths:
            meta = Path(f"{path}.meta").read_text(encoding="utf-8")
            self.assertEqual(PROFILES.UNITY_LOAD_TYPES[expected["loadType"]], scalar(meta, "loadType", 4), path)
            self.assertEqual(0, scalar(meta, "preloadAudioData", 4), path)
            self.assertEqual(0, scalar(meta, "preloadAudioData", 2), path)
            self.assertEqual(1, scalar(meta, "loadInBackground", 2), path)
            self.assertEqual(1, scalar(meta, "forceToMono", 2), path)
            self.assertEqual(44100, scalar(meta, "sampleRateOverride", 4), path)


def scalar(text: str, key: str, indent: int) -> int:
    match = re.search(rf"^{' ' * indent}{re.escape(key)}:\s*(\d+)\s*$", text, re.MULTILINE)
    if match is None:
        raise AssertionError(f"Missing {key} at indent {indent}")
    return int(match.group(1))


if __name__ == "__main__":
    unittest.main()
