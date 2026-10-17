"""Schema3 measured development admission; never gameplay/qualification evidence."""
import hashlib
import json
import re
from pathlib import Path

BINDINGS={"S001":(0,"SkirmishBaseAssault","DB_BA"),"S025":(1,"SkirmishCityCrossroads","CC_BA"),"S073":(3,"SkirmishIndustrialBasin","IB_BA")}
STAGES={"Standard","War","LargeWar"}

def validate_counterpart_authority(job,project,require):
    root=Path(project).resolve()
    def exact(value,keys):require(isinstance(value,dict) and set(value)==set(keys.split()),"counterpart authority missing/unknown fields")
    def load(path):
        def pairs(rows):
            value={}
            for k,v in rows:
                require(k not in value,"duplicate counterpart authority JSON key");value[k]=v
            return value
        return json.loads(path.read_text(encoding="utf-8"),object_pairs_hook=pairs)
    def pin(value):
        exact(value,"path sha256");path=value["path"];h=value["sha256"]
        require(isinstance(path,str) and path and "\\" not in path and ":" not in path and not Path(path).is_absolute() and ".." not in Path(path).parts,"unsafe counterpart authority path")
        actual=(root/path).resolve();require(actual.is_relative_to(root),"counterpart authority path escapes root")
        valid=isinstance(h,str) and re.fullmatch(r"[a-f0-9]{64}",h) and actual.is_file()
        if valid:
            hasher=hashlib.sha256()
            with actual.open("rb") as stream:
                for block in iter(lambda:stream.read(64*1024),b""):
                    hasher.update(block)
            valid=hasher.hexdigest()==h
        require(valid,"counterpart authority file missing/changed: "+path)
        return actual
    require(type(job.get("schema_version")) is int and job["schema_version"]==3 and job.get("catalog_id") in BINDINGS,"explicit schema3 counterpart required")
    authority=load(pin(job.get("counterpart_authority")))
    exact(authority,"schema_version kind catalog_id definition_id definition_version runtime_index source_pins layout_semantic_sha256 candidate_sha256 build_sha256 assets measurement_report materialization_receipt normal_selection")
    id=job["catalog_id"];index,preset,packet=BINDINGS[id]
    require(type(authority["schema_version"]) is int and authority["schema_version"]==1 and authority["kind"]=="MeasuredExpandedCounterpartDevelopmentAuthority","unknown counterpart authority kind")
    for key in ("catalog_id","definition_id","definition_version","candidate_sha256","build_sha256"):
        require(authority[key]==job[key] and type(authority[key]) is type(job[key]),"counterpart authority/job identity differs: "+key)
    require(type(authority["runtime_index"]) is int and authority["runtime_index"]==index,"counterpart must preserve actual practice transport index")
    prefix="Assets/Game/Configs/SkirmishExpansion/Scenarios/"+id+"/"
    assets=authority["assets"];exact(assets,"development_catalog definition layout binding views practice_preset main_catalog main_expansion_catalog")
    expected={"development_catalog":prefix+"Resources/SkirmishProtectedCounterpart_"+id+".asset",
        "definition":prefix+"SkirmishScenario_"+id+".asset","layout":prefix+"Logical/Layout_"+id+".asset",
        "binding":prefix+"Logical/Binding_"+id+".asset","practice_preset":"Assets/Game/Resources/"+preset+".asset",
        "main_catalog":"Assets/Game/Resources/SkirmishBattleCatalog.asset","main_expansion_catalog":"Assets/Game/Resources/SkirmishExpansionCatalog.asset"}
    graph=[]
    for key,path in expected.items():
        pin(assets[key]);require(assets[key]["path"]==path,"counterpart actual asset path differs: "+key);graph.append(assets[key])
    require(isinstance(assets["views"],list) and len(assets["views"])==3,"three exact persisted counterpart views required")
    expected_views={prefix+"Logical/View_"+id+"_"+size+"_Regular.asset" for size in STAGES}
    require({v.get("path") for v in assets["views"] if isinstance(v,dict)}==expected_views,"counterpart size views missing/ambiguous")
    for value in assets["views"]:pin(value);graph.append(value)
    manifest={r["path"]:r["sha256"] for r in job["source_manifest"]}
    require(all(manifest.get(v["path"])==v["sha256"] for v in graph),"actual counterpart/practice graph absent from job source manifest")
    report=load(pin(authority["measurement_report"]));receipt=load(pin(authority["materialization_receipt"]))
    require(report.get("cleanupVerified") is True and report.get("result")=="CollectedUnqualified","counterpart source World/scene cleanup not verified")
    rows=[r for r in report.get("missions",[]) if isinstance(r,dict) and r.get("catalogId")==id]
    require(len(rows)==1 and rows[0].get("result")=="CollectedUnqualified" and not rows[0].get("error"),"exact successful counterpart measurement row required")
    artifacts=[r for r in report.get("missionArtifacts",[]) if isinstance(r,dict) and r.get("catalogId")==id]
    require(len(artifacts)==1,"exact durable counterpart mission artifact required")
    artifact=artifacts[0].get("path")
    require(isinstance(artifact,str),"durable counterpart artifact path missing")
    if Path(artifact).is_absolute():
        resolved=Path(artifact).resolve();require(resolved.is_relative_to(root),"durable counterpart artifact outside project");artifact=resolved.relative_to(root).as_posix()
    require(load(pin({"path":artifact,"sha256":artifacts[0].get("sha256")}))==rows[0],"durable counterpart mission artifact differs")
    measurement=rows[0].get("measurement",{});selected=measurement.get("strictThreeSizePlacementPass")
    require(type(selected) is int and measurement.get("normalThreeSizeCompilerPassed") is True,"strict actual counterpart placement/compiler pass missing")
    source=authority["source_pins"]
    require(isinstance(source,dict) and source==report.get("sourcePins")==measurement.get("sourcePins"),"counterpart report source pins differ")
    for path_key,hash_key in (("sourcePath","sourceAssetSha256"),("gridPath","gridAssetSha256"),("surfacePath","surfaceAssetSha256")):
        pin({"path":source.get(path_key),"sha256":source.get(hash_key)})
    require(isinstance(authority["layout_semantic_sha256"],str) and re.fullmatch(r"[a-f0-9]{64}",authority["layout_semantic_sha256"]) and
        measurement.get("proposedLayoutSemanticSha256")==authority["layout_semantic_sha256"],"counterpart report layout semantic fingerprint differs")
    passes=[p for p in measurement.get("passes",[]) if isinstance(p,dict) and p.get("pass")==selected]
    require(len(passes)==1,"strict counterpart pass missing/ambiguous")
    sizes=passes[0].get("sizes",[])
    require(isinstance(sizes,list) and len(sizes)==3 and all(isinstance(s,dict) for s in sizes) and {s.get("size") for s in sizes}==STAGES,"three exact actual counterpart sizes required")
    for size in sizes:
        queries=size.get("routeQueriesWithSameFacilityReservations",{}).get("queries",[])
        require(size.get("completeFacilities") is True and size.get("allActorsInsidePads") is True and queries and
            all(isinstance(q,dict) and q.get("status")=="NativeQueryFoundExactGoal" and type(q.get("failureCode")) is int and q["failureCode"]==0 for q in queries),"counterpart facility/army/same-reservation route configuration incomplete")
    require(receipt.get("catalogId")==id and receipt.get("result")=="MaterializedDevelopmentOnly" and not receipt.get("error") and
        receipt.get("reviewedReportSha256")==authority["measurement_report"]["sha256"] and receipt.get("publicationManifestChanged") is False and
        receipt.get("developmentOnly") is True and receipt.get("normalMissionReady") is False,"counterpart materialization receipt differs")
    resource=receipt.get("counterpartDevelopmentResource",{});materialized=receipt.get("developmentCatalogMaterialization",{})
    require(resource.get("path")==assets["development_catalog"]["path"] and resource.get("sha256")==assets["development_catalog"]["sha256"] and
        resource.get("mainCatalogChanged") is False and resource.get("practiceBindingChanged") is False and
        materialized.get("definitionPath")==assets["definition"]["path"] and materialized.get("definitionSha256")==assets["definition"]["sha256"] and
        materialized.get("layoutPath")==assets["layout"]["path"] and materialized.get("bindingPath")==assets["binding"]["path"] and
        materialized.get("runtimeIndex")==index and materialized.get("catalogPlayableChanged") is False,"counterpart graph/main binding materialization join differs")
    selection=authority["normal_selection"];exact(selection,"kind driver actual_normal_input_play baseline_credit readbacks")
    require(selection["kind"]=="ExpandedDefinition" and selection["driver"]=="PublicSetupMethods_EditModeFixture" and
        selection["actual_normal_input_play"] is False and type(selection["baseline_credit"]) is int and selection["baseline_credit"]==0,"counterpart fixture cannot claim genuine touch/gameplay/coverage")
    packet_path=root/("Design/Roadmap/Skirmish_Expansion/Scenarios/"+packet+".md")
    section=re.search(r"^## "+id+r"\s*$([\s\S]*?)(?=^## |\Z)",packet_path.read_text(encoding="utf-8"),re.MULTILINE)
    seeds=re.search(r"Regular seeds ((?:`\d+`[ ,and]*)+) in both EN and FA at Standard",section.group(1) if section else "")
    expected_seeds={int(v) for v in re.findall(r"`(\d+)`",seeds.group(1) if seeds else "")}
    readbacks=selection["readbacks"]
    require(len(expected_seeds)==3 and isinstance(readbacks,list) and len(readbacks)==3 and all(isinstance(r,dict) for r in readbacks) and {r.get("seed") for r in readbacks}==expected_seeds,"exact three packet-seed typed selection readbacks required")
    for row in readbacks:
        exact(row,"seed scenario_index catalog_id definition_id definition_version size difficulty native_session_id native_world_sequence native_setup_hash_uint32 native_content_identity")
        require(type(row["seed"]) is int and type(row["scenario_index"]) is int and row["scenario_index"]==index and
            row["catalog_id"]==id and row["definition_id"]==job["definition_id"] and type(row["definition_version"]) is int and
            row["definition_version"]==job["definition_version"] and row["size"]=="Standard" and row["difficulty"]=="Regular" and
            isinstance(row["native_session_id"],str) and row["native_session_id"] and isinstance(row["native_world_sequence"],str) and row["native_world_sequence"].isdigit() and int(row["native_world_sequence"])>0 and
            type(row["native_setup_hash_uint32"]) is int and 0<=row["native_setup_hash_uint32"]<=4294967295 and
            isinstance(row["native_content_identity"],str) and row["native_content_identity"],"actual typed normal Start readback differs")
    require(len({r["native_session_id"] for r in readbacks})==3 and len({r["native_world_sequence"] for r in readbacks})==3,"fresh independent typed-selection attempts/Worlds required")
    return authority
