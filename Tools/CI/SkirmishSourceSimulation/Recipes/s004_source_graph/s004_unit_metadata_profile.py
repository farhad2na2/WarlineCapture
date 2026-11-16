"""Generate an explicitly diagnostic S004 unit metadata materializer."""
import json
from pathlib import Path


def apply(arguments, output, candidate_sha, role_domain=False, current_epoch=False):
    matches = [i for i, path in enumerate(arguments)
               if path.endswith('/413_GeneratedUnitProductionMetadata.cs')]
    if len(matches) != 1:
        raise ValueError('Exact original metadata materializer required')
    original = Path(arguments[matches[0]]).read_text()
    admission = 'if ((int?)root["schema_version"] != (compatibility?.FactoryDomainQualified==true?2:1)'
    if original.count(admission) != 1:
        raise ValueError('Original metadata admission seam changed')
    original = original.replace(admission,
        'bool s004Candidate = S004DiagnosticUnitMetadataGuard.RequireOrDecline(root, sha, setupHash, seed, compatibility);\n            if (!s004Candidate && ((int?)root["schema_version"] != (compatibility?.FactoryDomainQualified==true?2:1)', 1)
    ending = '(bool?)root["runtime_camera_basis_exported"] != false)\n                throw new InvalidDataException'
    if original.count(ending) != 1:
        raise ValueError('Original metadata admission end changed')
    original = original.replace(ending, '(bool?)root["runtime_camera_basis_exported"] != false))\n                throw new InvalidDataException', 1)
    counts = [
        'units.Count != (compatibility?.FactoryDomainQualified==true?compatibility.UnitCount:12)',
        'units.Count != (sourceAuthority!=null?sourceAuthority.UnitCount:compatibility?.FactoryDomainQualified==true?compatibility.UnitCount:12)',
    ]
    matched_counts = [value for value in counts if original.count(value) == 1]
    if len(matched_counts) != 1:
        raise ValueError('Original metadata count seam changed')
    count=matched_counts[0]
    original = original.replace(count, 'units.Count != (s004Candidate?'+('19' if role_domain else '20')+':'+count[len('units.Count != '):]+')', 1)
    derived = output / 'S004DiagnosticUnitMetadataMaterializer.cs'
    derived.write_text(original)
    arguments[matches[0]] = str(derived)
    guard = output / 'S004DiagnosticUnitMetadataGuard.cs'
    guard.write_text('''using System;
using System.IO;
using Newtonsoft.Json.Linq;
namespace Game.Runtime {
internal static class S004DiagnosticUnitMetadataGuard {
 internal static bool RequireOrDecline(JObject root,string sha,uint setupHash,int seed,SourceMetadataCompatibility compatibility) {
  if ((string)root["kind"] != %s) return false;
  if (compatibility != null || sha != %s || setupHash != 1130726651u || seed != 104733 ||
      (int?)root["schema_version"] != 1 || (string)root["catalog_id"] != "S004" ||
      (uint?)root["setup_hash"] != setupHash || (int?)root["seed"] != seed ||
      (string)root["authority"] != %s ||
      (bool?)root["runtime_admission"] != false || (bool?)root["native_prefab_resolution"] != false ||
      (bool?)root["mission_ready"] != false || (root["unsupported_values"] as JArray)?.Count != 0 ||
      (bool?)root["baked_actor_footprint_substituted"] != false || (bool?)root["runtime_camera_basis_exported"] != false)
    throw new InvalidDataException("Exact unqualified S004 unit metadata candidate required");
  return true;
 }
}}
''' % (json.dumps('S004SourceRoleCatalogMetadataCandidate' if role_domain else 'S004SourceDerivedUnitMetadataCandidate'),
       json.dumps(candidate_sha),json.dumps(('ObservedHistoricalS004RolesAndCurrentAuditedPrefabMetadata_DiagnosticOnly' if current_epoch else 'ObservedS004SourceRolesAndRetainedPrefabMetadata_DiagnosticOnly') if role_domain else 'RetainedFactoryAndSerializedSourcePolicies_DiagnosticOnly')))
    arguments.append(str(guard))
    defines = [i for i, value in enumerate(arguments) if value.startswith('-define:')]
    if len(defines) != 1:
        raise ValueError('One source define set required')
    arguments[defines[0]] += ',WARLINE_S004_UNIT_METADATA_DIAGNOSTIC'


def apply_catalog_rejection_probe(arguments, output):
    matches = [i for i, path in enumerate(arguments)
               if path.endswith('/522_GeneratedS002ProductionInputs.cs')]
    if len(matches) != 1:
        raise ValueError('Exact original production input adapter required')
    source = Path(arguments[matches[0]]).read_text()
    original = '''if (!catalog.TryRole(row.Prefab.name, out var role))
                    throw new NotSupportedException("Actual configured unit has no retained role overlay");'''
    replacement = '''if (!catalog.TryRole(row.Prefab.name, out var role))
                {
                    // Preserve the factory row and original catalog. The native owner
                    // must reject this unit at every actual same-world producer.
                    foreach (var building in Buildings.Values)
                        if (SkirmishNativeProduction.CanQueue(Entities, Session, building, row.Prefab))
                            throw new InvalidOperationException("Source catalog rejection proof changed");
                    continue;
                }'''
    if source.count(original) != 1:
        raise ValueError('Exact missing-role rejection seam required')
    derived = output / 'S004DiagnosticProductionInputs.cs'
    derived.write_text(source.replace(original, replacement, 1))
    arguments[matches[0]] = str(derived)


def apply_catalog_getter_probe(arguments, output, candidate_sha, role_domain=False, current_epoch=False):
    matches = [i for i, path in enumerate(arguments)
               if path.endswith('/473_GeneratedS002CatalogAvailabilityBinding.cs')]
    if len(matches) != 1:
        raise ValueError('Exact source catalog binding adapter required')
    source = Path(arguments[matches[0]]).read_text()
    header = '  if(root==null||admitted==null||(string)root["kind"]!='
    if source.count(header) != 2:
        raise ValueError('Exact catalog getter admission seams required')
    source = source.replace(header, '  bool s004Source=S004CatalogSourceGuard.RequireOrDecline(root,admitted);\n  if(!s004Source&&(root==null||admitted==null||(string)root["kind"]!=')
    ending = '(bool?)root["ui_catalog_runtime_policy_sampled"]!=false)throw new InvalidDataException('
    if source.count(ending) != 2:
        raise ValueError('Exact catalog getter admission ends required')
    source = source.replace(ending, '(bool?)root["ui_catalog_runtime_policy_sampled"]!=false))throw new InvalidDataException(')
    counts = ['(!admitted.FactoryDomainQualified&&admitted.Ordered.Count!=12)',
              '(!admitted.FactoryDomainQualified&&!admitted.SourceAuthoredDomainQualified&&admitted.Ordered.Count!=12)']
    matched_counts=[value for value in counts if source.count(value)==2]
    if len(matched_counts) != 1:
        raise ValueError('Exact original catalog count guards required')
    count=matched_counts[0]
    source = source.replace(count, count.replace('admitted.Ordered.Count!=12',
        'admitted.Ordered.Count!=(s004Source?'+('19' if role_domain else '20')+':12)'))
    codec = 'var a=t as JObject;if((string)a?["codec"]!="source-asset-reference-v1"||(string)a?["type"]!="UnityEngine.Sprite")'
    if source.count(codec) != 1:
        raise ValueError('Exact original sprite source codec required')
    source = source.replace(codec, 'var a=t as JObject;bool sourceSprite=s004Source&&(string)a?["codec"]=="source-sprite-reference-v1";if((!sourceSprite&&(string)a?["codec"]!="source-asset-reference-v1")||(string)a?["type"]!="UnityEngine.Sprite")')
    key = '||string.IsNullOrEmpty(key)||assetPath==null'
    if source.count(key) != 1:
        raise ValueError('Exact original sprite identity guard required')
    source = source.replace(key, '||(!sourceSprite&&string.IsNullOrEmpty(key))||assetPath==null')
    derived = output / 'S004DiagnosticCatalogGetterBinding.cs'; derived.write_text(source); arguments[matches[0]] = str(derived)
    guard = output / 'S004CatalogSourceGuard.cs'
    guard.write_text('''using System;
using System.IO;
using Newtonsoft.Json.Linq;
namespace Game.Runtime { internal static class S004CatalogSourceGuard {
 internal static bool RequireOrDecline(JObject root,GeneratedUnitProductionMetadata admitted) {
  if ((string)root?["kind"] != %s) return false;
  if (admitted==null || admitted.FactoryDomainQualified || admitted.ArtifactSha256!=%s ||
      (string)root["authority"]!=%s ||
      (string)root["catalog_id"]!="S004" || (uint?)root["setup_hash"]!=1130726651u || (int?)root["seed"]!=104733 ||
      (bool?)root["native_prefab_resolution"]!=false || (bool?)root["runtime_admission"]!=false ||
      (bool?)root["mission_ready"]!=false || (bool?)root["ui_catalog_runtime_policy_sampled"]!=false)
    throw new InvalidDataException("Exact unqualified S004 source catalog required");
  return true;
 }
}}
''' % (json.dumps('S004SourceRoleCatalogMetadataCandidate' if role_domain else 'S004SourceDerivedUnitMetadataCandidate'),
       json.dumps(candidate_sha),json.dumps(('ObservedHistoricalS004RolesAndCurrentAuditedPrefabMetadata_DiagnosticOnly' if current_epoch else 'ObservedS004SourceRolesAndRetainedPrefabMetadata_DiagnosticOnly') if role_domain else 'RetainedFactoryAndSerializedSourcePolicies_DiagnosticOnly')))
    arguments.append(str(guard))
    defines = [i for i, value in enumerate(arguments) if value.startswith('-define:')]
    if len(defines) != 1:
        raise ValueError('One source define set required')
    arguments[defines[0]] += ',WARLINE_S004_CATALOG_GETTER_PROBE'


def apply_catalog_binding_probe(arguments, output, setup_path, setup_sha):
    matches = [i for i, path in enumerate(arguments) if path.endswith('/SourceGroundMissionFamily.cs')]
    if len(matches) != 1:
        raise ValueError('Exact original source mission family boundary required')
    source = Path(arguments[matches[0]]).read_text()
    anchor = 'if(setup.CatalogId=="S002")return;'
    if source.count(anchor) != 1:
        raise ValueError('Exact source mission family identity seam required')
    insertion = 'if(setup.CatalogId=="S004"){SourceS004CompiledSetupInput.RequireExact(setup,' + json.dumps(str(setup_path)) + ',' + json.dumps(setup_sha) + ');return;}\n  '
    derived = output / 'S004DiagnosticCatalogSourceFamily.cs'; derived.write_text(source.replace(anchor, insertion + anchor, 1))
    arguments[matches[0]] = str(derived)
    defines = [i for i, value in enumerate(arguments) if value.startswith('-define:')]
    if len(defines) != 1:
        raise ValueError('One source define set required')
    arguments[defines[0]] += ',WARLINE_S004_CATALOG_BINDING_PROBE'


def apply_unplaced_airport_probe(arguments, output):
    def project(suffix,name,replacements):
        matches=[i for i,path in enumerate(arguments) if path.endswith(suffix)]
        if len(matches)!=1:raise ValueError('Exact source owner required: '+suffix)
        source=Path(arguments[matches[0]]).read_text()
        for old,new in replacements:
            if source.count(old)!=1:raise ValueError('Exact unplaced airport source seam required: '+old)
            source=source.replace(old,new,1)
        target=output/name;target.write_text(source);arguments[matches[0]]=str(target)
    project('/522_GeneratedS002ProductionInputs.cs','S004UnplacedAirportProductionInputs.cs',[
        ('if (!definition.SourceMetadataComplete || definition.Prefab == null || definition.IsWall || definition.HasRunway)',
         'if (!definition.SourceMetadataComplete || definition.Prefab == null || definition.IsWall || (definition.HasRunway && definition.Prefab.name != "Building_Airport"))'),
        ('RequireActualStartupDomain(Setup,Buildings.Count,OpeningActors.Count);',
         'RequireActualStartupDomain(Setup,Buildings.Count,OpeningActors.Count);RequireS004NoLiveRunway();')])
    project('/530_GeneratedS002PaidContext.cs','S004UnplacedAirportPaidContext.cs',[
        ('if(SourceDefinitions.Definitions.Any(d=>d.HasRunway))throw new NotSupportedException("Runway owner is outside current ordinary S002 ground profile");',
         'RequireS004NoLiveRunway();var sourceNoRunway=new SourceCampProvenNoRunwayReads(Buildings,true);'),
        ('ActualProductionContext.TryGetPrefabLocalBounds,null,true,true)',
         'ActualProductionContext.TryGetPrefabLocalBounds,sourceNoRunway,true,true)'),
        ('internal void AdvanceActualFactionRequests(){if(ActualFactionDispatcher!=null)',
         'internal void AdvanceActualFactionRequests(){RequireS004NoLiveRunway();if(ActualFactionDispatcher!=null)')])
    project('/528_GeneratedS002ProductionTick.cs','S004UnplacedAirportProductionTick.cs',[
        ('internal void AdvanceActualPaidProduction(){',
         'internal void AdvanceActualPaidProduction(){RequireS004NoLiveRunway();')])
    guard=output/'S004NoLiveRunwayGuard.cs'
    guard.write_text('''using System;
using System.Linq;
namespace Game.Runtime { internal sealed partial class AutonomousWorld {
 internal void RequireS004NoLiveRunway() {
  if(Setup==null || Setup.CatalogId!="S004" || SourceDefinitions==null ||
     SourceDefinitions.Definitions.Count(d=>d!=null && d.HasRunway && d.Prefab?.name=="Building_Airport")!=1 ||
     SourceDefinitions.Definitions.Any(d=>d!=null && d.HasRunway && d.Prefab?.name!="Building_Airport"))
   throw new NotSupportedException("Exact unplaced source airport definition required");
  foreach(var building in Buildings.Values)
   if(building==null || (!building.IsDestroyed && (building.Definition==null || !building.Definition.SourceMetadataComplete || building.Definition.HasRunway)))
    throw new NotSupportedException("Live source runway domain unavailable");
 }
}}
''')
    arguments.append(str(guard))


def apply_retained_map_probe(arguments, output):
    matches=[i for i,path in enumerate(arguments) if path.endswith('/S004UnplacedAirportPaidContext.cs')]
    if len(matches)!=1:raise ValueError('Exact unplaced-airport paid context required')
    path=Path(arguments[matches[0]]);source=path.read_text()
    old='InitializeActualSelectedOperationMapPublication();'
    if source.count(old)!=1:raise ValueError('Exact selected map publication call required')
    path.write_text(source.replace(old,'RequireS004RetainedMapPublication();',1))
