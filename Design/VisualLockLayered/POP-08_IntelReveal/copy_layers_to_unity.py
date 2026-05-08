from pathlib import Path
import shutil


ROOT = Path(__file__).resolve().parent
PROJECT_ROOT = ROOT.parents[2]

DESTINATIONS = {
    "background_intel_archive.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Content/background_intel_archive.png",
    "thumbnail_supply_ledger.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Content/thumbnail_supply_ledger.png",
    "thumbnail_cargo_manifest.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Content/thumbnail_cargo_manifest.png",
    "thumbnail_radio_intercept.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Content/thumbnail_radio_intercept.png",
    "modal_frame.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Frames/modal_frame.png",
    "modal_fill.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Frames/modal_fill.png",
    "evidence_card_frame.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Frames/evidence_card_frame.png",
    "evidence_content_frame.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Frames/evidence_content_frame.png",
    "confidence_chip_frame.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Frames/confidence_chip_frame.png",
    "notice_bar_frame.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Frames/notice_bar_frame.png",
    "inspect_button_background.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Buttons/inspect_button_background.png",
    "secondary_button_background.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Buttons/secondary_button_background.png",
    "primary_button_background.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Buttons/primary_button_background.png",
    "close_button_background.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Buttons/close_button_background.png",
    "header_document_magnifier_icon.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Icons/header_document_magnifier_icon.png",
    "close_icon.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Icons/close_icon.png",
    "inspect_icon.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Icons/inspect_icon.png",
    "notice_intel_icon.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Icons/notice_intel_icon.png",
    "radio_play_icon.png": "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Icons/radio_play_icon.png",
}


def main():
    for source_name, destination in DESTINATIONS.items():
        source = ROOT / "layers" / source_name
        target = PROJECT_ROOT / destination
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)


if __name__ == "__main__":
    main()
