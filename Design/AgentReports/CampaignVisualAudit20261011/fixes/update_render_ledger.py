from pathlib import Path
import re
import hashlib
root = Path(__file__).resolve().parents[4]
ledger = dict(line.split("=", 1) for line in (Path(__file__).parent / "render-reconciliation.txt").read_text().splitlines())
scene = root / "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity"
assert hashlib.sha256(scene.read_bytes()).hexdigest() == ledger["sourceSha256"]
assert ledger["marketStockMaterialRows"] == "52" and ledger["destinationMaterialRows"] == "85"
p = root / "Assets/Game/Scripts/Editor/OperationMapRenderEligibilityInventoryProbe.cs"
s = p.read_text()
assert "ExpectedCampaignVisualRenderRowAddition" not in s, "Ledger patch already applied; review before updating again"
s = s.replace("internal const int ExpectedReviewedRunwayMaterialRowAddition = 1;", "internal const int ExpectedReviewedRunwayMaterialRowAddition = 1;\n        // CampaignVisualAudit20261011/fixes/render-reconciliation.txt explains every new row.\n        internal const int ExpectedCampaignVisualRenderRowAddition = 52 + 85;")
s = s.replace("ExpectedReviewedRunwayMaterialRowAddition;", "ExpectedReviewedRunwayMaterialRowAddition +\n            ExpectedCampaignVisualRenderRowAddition;")
s = s.replace("ExpectedReviewedRunwayMaterialRowAddition)", "ExpectedReviewedRunwayMaterialRowAddition -\n                    ExpectedCampaignVisualRenderRowAddition)")
s = s.replace('["AuthoredOperationMapEntityPresentation/RenderOnly"] = 10843', '["AuthoredOperationMapEntityPresentation/RenderOnly"] = 10843 + 85')
s = s.replace('["Generated_GiantDenseMiddleEasternCity_EntityPresentation/RenderOnly"] = 98597', '["Generated_GiantDenseMiddleEasternCity_EntityPresentation/RenderOnly"] = 98597 + 52')
s = s.replace("c76df867657c0ec2bd17685764db8a1e468d39ed34d828d245ab2d4c5dde523b", ledger["sourceSha256"])
s = s.replace("// scene change requires its own reconciliation, even if row totals", "// CampaignVisualAudit20261011/fixes/render-reconciliation.txt records this revision. A later\n            // scene change requires its own reconciliation, even if row totals")
assert "ExpectedCampaignVisualRenderRowAddition = 52 + 85" in s
p.write_text(s)
print("Pinned reconciled Campaign visual source", ledger["sourceSha256"], "rows=124305")
