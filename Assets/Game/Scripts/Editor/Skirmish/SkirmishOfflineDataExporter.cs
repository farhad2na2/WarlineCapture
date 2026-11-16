#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Runtime.Pathfinding;
using Game.Skirmish.Contracts;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    // No asset creation, scene loading, gameplay systems, publication changes or LFS fetches.
    public sealed partial class SkirmishOfflineDataExporter
    {
        public const int MaximumCells = 16 * 1024 * 1024;
        public const long MaximumArtifactBytes = 256L * 1024 * 1024;
        public const string PassMarker = "[SkirmishOfflineDataExporter] result=Passed";
        readonly SortedDictionary<string, OfflineSourcePin> pins = new(StringComparer.Ordinal);
        readonly SortedDictionary<string, OfflineMap> maps = new(StringComparer.Ordinal);
        readonly List<OfflineArtifact> artifacts = new();
        readonly List<OfflineCampaignRow> campaign = new();
        readonly string output;
        string explicitCatalogPath;
        SkirmishOfflineDataExporter(string directory) { output = directory; }

        public static void RunFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-skirmishOfflineExport");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Explicit -skirmishOfflineExport <new directory> required.");
            int catalogAt=Array.IndexOf(args,"-skirmishOfflineCatalog");
            Export(args[at + 1],catalogAt>=0 && catalogAt+1<args.Length ? args[catalogAt+1] : null);
            Debug.Log(PassMarker);
        }
        public static OfflineExportManifest Export(string directory,string catalogPath = null)
        {
            string full = ValidateOutputDirectory(directory);
            Directory.CreateDirectory(full);
            return new SkirmishOfflineDataExporter(full) {explicitCatalogPath=catalogPath}.Build();
        }
        public static string ValidateOutputDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Explicit output directory required.");
            string full = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar);
            string logs = Path.GetFullPath("Logs") + Path.DirectorySeparatorChar;
            string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            // macOS /tmp aliases /private/tmp; both are explicit temporary output locations.
            bool allowed = full.StartsWith(logs, StringComparison.Ordinal) || full.StartsWith(temp, StringComparison.Ordinal) ||
                full.StartsWith("/private/tmp/", StringComparison.Ordinal) || full.StartsWith("/tmp/", StringComparison.Ordinal);
            if (!allowed || Directory.Exists(full) || File.Exists(full)) throw new ArgumentException("Output must be a NEW directory under project Logs or system temp.");
            string allowedRoot=full.StartsWith(logs,StringComparison.Ordinal)?logs.TrimEnd(Path.DirectorySeparatorChar):
                full.StartsWith(temp,StringComparison.Ordinal)?temp.TrimEnd(Path.DirectorySeparatorChar):full.StartsWith("/private/tmp/",StringComparison.Ordinal)?"/private/tmp":"/tmp";
            for(var parent=new DirectoryInfo(full).Parent;parent!=null;parent=parent.Parent)
            {
                if(parent.FullName==allowedRoot){if(allowedRoot==logs.TrimEnd(Path.DirectorySeparatorChar) && parent.Exists && (parent.Attributes&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Logs output root must not be a symlink");break;}
                if(parent.Exists && (parent.Attributes&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Output parent must not be a symlink: "+parent.FullName);
            }
            return full;
        }
        OfflineExportManifest Build()
        {
            // Pin executable policy before reading any data. This deliberately covers the complete
            // gameplay/compiler source closure, including resource-based compiler helpers.
            foreach (string path in Directory.GetFiles("Assets/Game/Scripts", "*.cs", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal)) Pin(path);
            foreach (string path in Directory.GetFiles("Assets/Game/Scripts", "*.asmdef", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal)) Pin(path);
            Pin("Assets/Tests/Editor/SkirmishOfflineDataExportTests.cs"); Pin("Assets/Tests/Editor/SkirmishOfflineCampaignReferencesTests.cs");
            foreach (string path in Directory.GetFiles("ProjectSettings", "*", SearchOption.TopDirectoryOnly).OrderBy(p => p, StringComparer.Ordinal)) Pin(path);
            Pin("Packages/manifest.json"); Pin("Packages/packages-lock.json");
            // Compiler helpers also use Resources.Load outside serialized catalog references.
            // Pin the resource inventory before any compiler call; no resource creation/fetch.
            foreach(string path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal) && p.Contains("/Resources/") && !AssetDatabase.IsValidFolder(p)).OrderBy(p=>p,StringComparer.Ordinal))Pin(path);
            Track(SkirmishConvoyTruckDefinition.Load());Track(Resources.Load<OperationMapDefinition>("SkirmishS004OperationMap"));
            foreach (string path in Directory.GetFiles("Design", "*.csv", SearchOption.AllDirectories).Where(p => p.IndexOf("Skirmish", StringComparison.OrdinalIgnoreCase) >= 0).OrderBy(p => p, StringComparer.Ordinal)) Pin(path);
            Pin(SkirmishOffline.SkirmishOfflineCampaignReferences.MenuScenePath);
            Pin(SkirmishOffline.SkirmishOfflineCampaignReferences.BootstrapSourcePath);
            var refs = SkirmishOffline.SkirmishOfflineCampaignReferences.ReadCurrent();
            Pin(refs.MenuPath); Pin(refs.BootstrapScriptPath);
            Track(refs.MissionCatalog); Track(refs.OperationMapCatalog);
            var battle = SkirmishBattleCatalogConfig.Load();
            var expansion = string.IsNullOrEmpty(explicitCatalogPath) ? SkirmishExpansionCatalogConfig.Load() : AssetDatabase.LoadAssetAtPath<SkirmishExpansionCatalogConfig>(explicitCatalogPath);
            if(!string.IsNullOrEmpty(explicitCatalogPath) && (expansion==null || AssetDatabase.GetAssetPath(expansion)!=explicitCatalogPath || ReadPin(explicitCatalogPath).status!="complete"))throw new ArgumentException("Explicit catalog must resolve exactly to a complete authored catalog asset");
            Track(battle); Track(expansion);
            using var selectedScope=string.IsNullOrEmpty(explicitCatalogPath)?null:SkirmishExpansionCatalogConfig.UseEditorValidationCatalog(expansion);
            var matrixAsset = Resources.Load<TextAsset>(SkirmishSetupMatrixTable.PackagedResourceName); Track(matrixAsset);
            bool matrixOk = SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError);
            string catalogGuid="";long catalogLocalId=0;
            if(expansion!=null)AssetDatabase.TryGetGUIDAndLocalFileIdentifier(expansion,out catalogGuid,out catalogLocalId);
            var report = new OfflineExportManifest { catalog_selection=string.IsNullOrEmpty(explicitCatalogPath)?"DefaultPackaged":"ExplicitCandidateCatalog",compiler_catalog_scope=string.IsNullOrEmpty(explicitCatalogPath)?"PackagedRuntimeResources":"ExistingDisposableEditorValidationCatalog",selected_catalog_guid=catalogGuid,selected_catalog_local_id=catalogLocalId,menu_path = refs.MenuPath, campaign_catalog_path = refs.MissionCatalogPath,
                campaign_map_catalog_path = refs.OperationMapCatalogPath, battle_catalog_path = AssetDatabase.GetAssetPath(battle),
                expansion_catalog_path = AssetDatabase.GetAssetPath(expansion), matrix_path = AssetDatabase.GetAssetPath(matrixAsset) };
            var errors = new List<string>();
            if (battle == null) errors.Add("Packaged battle catalog missing");
            if (expansion == null) errors.Add("Packaged expansion catalog missing");
            if (!matrixOk) errors.Add("Packaged setup matrix unavailable: " + matrixError);
            CaptureCampaign(refs.MissionCatalog, refs.OperationMapCatalog);
            // Preserve unknown IDs and null serialized slots as coverage failures, never silently discard them.
            if (battle != null) foreach (var row in battle.Entries)
                if (!Canonical(row.ScenarioId)) errors.Add("Noncanonical battle catalog ID: " + row.ScenarioId);
            if (expansion != null) for (int i=0;i<expansion.Definitions.Count;i++)
                if (expansion.Definitions[i] == null) errors.Add("Null expansion definition slot " + i);
                else if (!Canonical(expansion.Definitions[i].CatalogId)) errors.Add("Noncanonical expansion definition ID: " + expansion.Definitions[i].CatalogId);
            var missions = new List<OfflineMission>();
            for (int number = 1; number <= 120; number++)
            {
                string id = "S" + number.ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
                var definitions = expansion == null ? Array.Empty<SkirmishScenarioDefinitionConfig>() : expansion.Definitions.Where(d => d != null && d.CatalogId == id).ToArray();
                var mission = new OfflineMission { canonical_id = id,
                    catalog_rows = battle == null ? Array.Empty<SkirmishBattleCatalogEntry>() : battle.Entries.Where(r => r.ScenarioId == id).ToArray(),
                    definition_refs = definitions.Select(AssetDatabase.GetAssetPath).ToArray() };
                var reasons = new List<string>();
                if (mission.catalog_rows.Length != 1) reasons.Add("Expected exactly one battle catalog row; observed " + mission.catalog_rows.Length);
                if (definitions.Length != 1) reasons.Add("Expected exactly one actual serialized expansion definition; observed " + definitions.Length);
                var definition = definitions.Length == 1 ? definitions[0] : null;
                OfflineMap map = null;
                if (definition != null)
                {
                    if(mission.catalog_rows.Length==1 && (mission.catalog_rows[0].DefinitionId!=definition.DefinitionId || mission.catalog_rows[0].ContentVersion!=definition.ContentVersion)) reasons.Add("Selected definition differs from published battle row identity/version; no publication inferred");
                    Track(definition); mission.definition_id = definition.DefinitionId;
                    if (definition.CampaignMap == null) reasons.Add("No explicit CampaignMap binding; no legacy/name fallback used");
                    else { map = CaptureMap(definition.CampaignMap); mission.map_key = map.key; }
                    var layout = definition.MapLayout;
                    if (layout == null) reasons.Add("Layout missing");
                    else
                    {
                        mission.world_binding = layout.WorldBinding;
                        mission.anchors = (layout.Anchors ?? Array.Empty<SkirmishLayoutAnchorConfig>()).Select(a => new OfflineAnchor { authored=a, projected=Project(layout,map,a.AnchorId,a.RoleId,a.WorldX,a.WorldZ) }).ToArray();
                        mission.pads = (layout.Pads ?? Array.Empty<SkirmishLegalPadConfig>()).Select(p => new OfflinePad { authored=p, projected=Project(layout,map,p.PadId,p.RoleId,p.CenterX,p.CenterZ) }).ToArray();
                        mission.routes = layout.Routes ?? Array.Empty<SkirmishLayoutRouteConfig>();
                    }
                    mission.objective_json = definition.ObjectiveConfig == null ? null : JsonUtility.ToJson(definition.ObjectiveConfig);
                    mission.logical_binding_json = definition.MissionLogicalBinding == null ? null : JsonUtility.ToJson(definition.MissionLogicalBinding);
                }
                var variants = new List<OfflineVariant>();
                foreach (var size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
                foreach (var difficulty in new[] { SkirmishDifficultyId.Recruit, SkirmishDifficultyId.Regular, SkirmishDifficultyId.Veteran, SkirmishDifficultyId.Commander })
                    variants.Add(CaptureVariantSafely(definition,map,size,difficulty,matrixOk ? matrix : null));
                mission.variants=variants.ToArray(); mission.reasons=reasons.ToArray(); missions.Add(mission);
            }
            report.missions=missions.ToArray(); report.maps=maps.Values.ToArray(); report.campaign_rows=campaign.ToArray();
            report.reasons=errors.ToArray(); report.artifacts=artifacts.OrderBy(a => a.path,StringComparer.Ordinal).ToArray();
            AssertCleanLoadedSources();
            foreach (var pin in pins.Values)
                if (!SamePin(pin, ReadPin(pin.path))) throw new InvalidOperationException("Source drift during export: " + pin.path);
            report.source_pins=pins.Values.ToArray();
            string manifestPath=Path.Combine(output,"manifest.json"); WriteText(manifestPath,JsonUtility.ToJson(report,true));
            WriteText(Path.Combine(output,"receipt.json"),JsonUtility.ToJson(new OfflineExportReceipt { manifest_sha256=HashFile(manifestPath),source_pins_unchanged=true },true));
            return report;
        }
        void CaptureCampaign(MissionDefinitionCatalogConfig catalog, OperationMapCatalogConfig mapCatalog)
        {
            if (catalog == null || mapCatalog == null) throw new InvalidOperationException("Current Menu Campaign catalogs required.");
            var missionCounts=new Dictionary<string,int>(StringComparer.Ordinal);
            foreach(var entry in catalog.Entries){string id=entry.MissionId??"";missionCounts.TryGetValue(id,out int count);missionCounts[id]=count+1;}
            foreach (var entry in catalog.Entries)
            {
                Track(entry.Definition); Track(entry.Scenario);
                var row=new OfflineCampaignRow {mission_id=entry.MissionId,definition_path=AssetDatabase.GetAssetPath(entry.Definition),scenario_path=AssetDatabase.GetAssetPath(entry.Scenario),operation_map_id=entry.Definition?.OperationMapId};
                var reasons=new List<string>(); var matches=new List<OperationMapDefinition>();
                if(string.IsNullOrWhiteSpace(entry.MissionId))reasons.Add("Campaign mission ID missing");
                else if(missionCounts[entry.MissionId]!=1)reasons.Add("Duplicate Campaign mission ID: "+entry.MissionId);
                if (entry.Definition==null || entry.Scenario==null) reasons.Add("Missing actual Campaign definition/scenario");
                else if(entry.Scenario.OperationMapId!=entry.Definition.OperationMapId)reasons.Add("Campaign scenario and definition operation-map IDs differ");
                foreach(var candidate in mapCatalog.Definitions) if(candidate!=null && candidate.OperationMapId==row.operation_map_id) matches.Add(candidate);
                if(matches.Count!=1) reasons.Add("Campaign operation map matches: "+matches.Count);
                else { Track(matches[0]); row.map_path=AssetDatabase.GetAssetPath(matches[0]); row.scene_guid=matches[0].SourceSceneReference?.AssetGUID; }
                row.reasons=reasons.ToArray(); campaign.Add(row);
            }
            foreach(var row in campaign) if(row.reasons.Length==0) CaptureMap(AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(row.map_path));
        }
        OfflineVariant CaptureVariantSafely(SkirmishScenarioDefinitionConfig definition,OfflineMap map,SkirmishSizeId size,SkirmishDifficultyId difficulty,IReadOnlyList<SkirmishSetupMatrixRow> matrix)
        {
            try { return CaptureVariant(definition,map,size,difficulty,matrix); }
            catch(Exception error) { return new OfflineVariant {size=size.ToString(),difficulty=difficulty.ToString(),compile_reasons=new[]{new OfflineReason {code="NativeCompilerException",field="variant",detail=error.GetType().Name+": "+error.Message}}}; }
        }
        OfflineVariant CaptureVariant(SkirmishScenarioDefinitionConfig definition, OfflineMap map, SkirmishSizeId size, SkirmishDifficultyId difficulty, IReadOnlyList<SkirmishSetupMatrixRow> matrix)
        {
            var v=new OfflineVariant {size=size.ToString(),difficulty=difficulty.ToString(),seed=0};
            if(definition==null || matrix==null) {v.compile_reasons=new[] {new OfflineReason {code="UnavailableInputs",field="definition/matrix",detail="Unique actual definition and packaged matrix required"}};return v;}
            var manifest=new SkirmishContentManifest {RequiredFeatureIds=definition.RequiredFeatureIds ?? Array.Empty<string>()};
            v.compile_success=SkirmishSetupCompiler.TryCompile(definition,difficulty,size,0,manifest,matrix,out var setup,out var reasons);
            v.compile_reasons=reasons.Select(r=>new OfflineReason {code=r.Code.ToString(),field=r.Field,detail=r.Detail}).ToArray();
            if(v.compile_success) {v.setup=setup;v.forces=setup.Forces;v.structures=setup.Structures;}
            v.static_budget_available=SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition,difficulty,size,0,manifest,matrix,out var budget,out _);
            if(v.static_budget_available) {v.static_budget_exact=budget.ExactCanonicalQuantities;v.static_budget_errors=budget.CanonicalBudgetErrors;if(!v.compile_success){v.forces=budget.Forces;v.structures=budget.Structures;}}
            foreach(var force in v.forces) if(!string.IsNullOrEmpty(force.SourceConfigPath)) Pin(force.SourceConfigPath);
            v.desired_force_positions=v.forces.Select((f,i)=>Point(map,i.ToString(),f.RoleId,f.SpawnWorldX,f.SpawnWorldZ,"compiler desired anchor; not final spawn")).ToArray();
            v.desired_structure_positions=v.structures.Select((s,i)=>Point(map,i.ToString(),s.StructureId,s.SpawnWorldX,s.SpawnWorldZ,"compiler desired anchor; not final placement")).ToArray();
            v.objective_area_configs=(definition.MissionLogicalBinding?.Variants ?? Array.Empty<SkirmishMissionLogicalVariant>()).Where(a=>a!=null && a.Size==size && a.Difficulty==difficulty).Select(a=>a.Areas).ToArray();
            v.objective_area_points=v.objective_area_configs.Where(c=>c!=null).SelectMany(c=>c.Areas ?? Array.Empty<SkirmishObjectiveArea>()).SelectMany(a=>(a.Points ?? Array.Empty<SkirmishObjectiveAreaPoint>()).Select((p,i)=>Point(map,a.StableAreaId+"/"+i,a.Role.ToString(),p.WorldX,p.WorldZ,"authored logical area vertex"))).ToArray();
            return v;
        }
        public static OfflinePoint Point(OfflineMap map,string id,string role,float x,float z,string source)
        {
            var point=new OfflinePoint {id=id,role=role,world_x=x,world_z=z,coordinate_source=source};
            if(map==null || !map.authored_grid_available || !float.IsFinite(x) || !float.IsFinite(z))return point;
            var cell=GridCoordinateKernel.WorldToCell(new GridCoordinateFrame {OriginX=map.origin_x,OriginY=map.origin_y,OriginZ=map.origin_z,CellSize=map.cell_size},x,z);
            point.has_cell=true;point.cell_x=cell.X;point.cell_z=cell.Z;point.in_bounds=(uint)cell.X<(uint)map.width && (uint)cell.Z<(uint)map.height;return point;
        }
        static OfflinePoint Project(SkirmishMapLayoutConfig layout,OfflineMap map,string id,string role,float x,float z)
        {
            if(layout.TryProjectLocalToMap(x,z,out float px,out float pz)) return Point(map,id,role,px,pz,"SkirmishMapLayoutConfig.TryProjectLocalToMap");
            return new OfflinePoint {id=id,role=role,world_x=x,world_z=z,coordinate_source="Unbound authored layout coordinates; not interpreted as world"};
        }
        public static bool Canonical(string id)=>id!=null && id.Length==4 && id[0]=='S' && int.TryParse(id.Substring(1),out int n) && n>=1 && n<=120 && id=="S"+n.ToString("D3");
        void Track(Object asset)
        {
            if(asset==null)return;string path=AssetDatabase.GetAssetPath(asset);if(string.IsNullOrEmpty(path))return;
            if(EditorUtility.IsDirty(asset))throw new InvalidOperationException("Unsaved authored source cannot be pinned: "+path);
            foreach(string dependency in AssetDatabase.GetDependencies(path,true).OrderBy(p=>p,StringComparer.Ordinal)) Pin(dependency);
        }
        void AssertCleanLoadedSources()
        {
            foreach(var asset in Resources.FindObjectsOfTypeAll<ScriptableObject>())
            {
                string path=AssetDatabase.GetAssetPath(asset);
                if(!string.IsNullOrEmpty(path) && pins.ContainsKey(path) && EditorUtility.IsDirty(asset))throw new InvalidOperationException("Unsaved loaded source differs from pinned disk: "+path);
            }
        }
        void Pin(string path)
        {
            if(string.IsNullOrEmpty(path))return;path=path.Replace('\\','/');if(pins.ContainsKey(path))return;
            pins.Add(path,ReadPin(path));if(!path.EndsWith(".meta",StringComparison.Ordinal) && File.Exists(path+".meta"))Pin(path+".meta");
        }
        public static OfflineSourcePin ReadPin(string path)
        {
            var pin=new OfflineSourcePin {path=path,guid=AssetDatabase.AssetPathToGUID(path),status="missing",sha256=""};
            if(!File.Exists(path))return pin;pin.bytes=new FileInfo(path).Length;
            if(pin.bytes==0){pin.status="empty";pin.sha256=HashFile(path);return pin;}
            using(var stream=File.OpenRead(path)){var prefix=new byte[Math.Min(128,(int)Math.Min(pin.bytes,128))];stream.Read(prefix,0,prefix.Length);if(Encoding.UTF8.GetString(prefix).StartsWith("version https://git-lfs.github.com/spec/v1",StringComparison.Ordinal)){pin.status="lfs_pointer";pin.sha256=HashFile(path);return pin;}}
            pin.status="complete";pin.sha256=HashFile(path);return pin;
        }
        public static bool SamePin(OfflineSourcePin a,OfflineSourcePin b)=>a.path==b.path && a.guid==b.guid && a.status==b.status && a.bytes==b.bytes && a.sha256==b.sha256;
        public static string HashFile(string path){using var hash=SHA256.Create();using var stream=File.OpenRead(path);return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
        static void WriteText(string path,string text)=>File.WriteAllText(path,text+"\n",new UTF8Encoding(false));
    }
}
#endif
