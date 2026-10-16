"""Prepare explicit successors of the installed BatchV3; never imports Assets."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[4]
OUT=Path(__file__).resolve().parent
files=[]
def once(s,a,b):
    assert s.count(a)==1,(a,s.count(a))
    return s.replace(a,b)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def project(target,transform):
    actual=ROOT/target;parent=None
    if target=='Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.cs':
        parent=dict(target=target,source='Tools/CI/SkirmishProductionDrafts/AriaUiOwnedStorage/SkirmishS002AriaRunHarness.cs.txt',before_sha256='2adae13a6fc405320d2e9db086efc8139d37b46aa1c7e0f1e36681bb53d42c15',sha256='4a73c542b7d3be956a1a5db03679b4349e06a379cef0d2e2a94c601468fcb8ba')
        assert sha(ROOT/parent['source'])==parent['sha256']
        assert sha(actual) in (parent['before_sha256'],parent['sha256'])
    s=(ROOT/parent['source'] if parent else actual).read_text(encoding='utf-8-sig');changed=transform(s)
    source=OUT/'Sources'/target;source.parent.mkdir(parents=True,exist_ok=True);source.write_text(changed,encoding='utf-8',newline='\n')
    row=dict(target=target,source=source.relative_to(ROOT).as_posix(),before_sha256=parent['sha256'] if parent else sha(actual),sha256=sha(source))
    if parent:row.update(composed_from=parent,actual_before_at_preparation=sha(actual))
    files.append(row)
base='Assets/Game/Scripts/Editor/Skirmish/'
def request(s):
    s=once(s,'bool verifySourceBytes = true)','bool verifySourceBytes = true, SkirmishProductionMatrixAuthority matrix = null)')
    s=once(s,'Require(catalog == "S004" || catalog == "S005", "Only S004/S005 canary dispatch exists.");','Require(matrix != null || catalog == "S004" || catalog == "S005", "Only S004/S005 canary dispatch exists without an explicit matrix plan.");')
    s=once(s,'Require(seeds.Contains((int)job["seed"]), "Seed outside bounded baseline canary.");','if(matrix==null) Require(seeds.Contains((int)job["seed"]), "Seed outside bounded baseline canary.");\n            else matrix.ValidateJob(job);')
    s=once(s,'Require(packet == "Design/Roadmap/Skirmish_Expansion/Scenarios/DB_BA.md", "Canary must use governing DB_BA packet.");','Require(matrix != null || packet == "Design/Roadmap/Skirmish_Expansion/Scenarios/DB_BA.md", "Canary must use governing DB_BA packet without an explicit matrix plan.");')
    return s
project(base+'SkirmishProductionCanaryRequest.cs',request)
def batch(s):
    s=once(s,'private readonly JArray jobs;','private readonly JArray jobs;\n            internal readonly SkirmishProductionMatrixAuthority Matrix;')
    s=once(s,'Exact(descriptor,"schema_version batch_id artifact_directory candidate_sha256 build_sha256 candidate_manifest_path candidate_manifest_sha256 jobs");','bool matrixMode=(int?)descriptor["schema_version"]==2;\n                Exact(descriptor,"schema_version batch_id artifact_directory candidate_sha256 build_sha256 candidate_manifest_path candidate_manifest_sha256 jobs"+(matrixMode?" sampling_plan_path sampling_plan_sha256":""));')
    s=once(s,'(int)descriptor["schema_version"]!=1)','((int)descriptor["schema_version"]!=1&&!matrixMode))')
    s=once(s,'if(jobs==null||jobs.Count!=2)throw new InvalidDataException("This bounded adapter requires exactly two development canaries.");','if(jobs==null||jobs.Count<1||jobs.Count>(matrixMode?1200:2)||(!matrixMode&&jobs.Count!=2))throw new InvalidDataException("Finite matrix batches require1..1200 jobs; legacy canary requires exactly two.");\n                if(matrixMode)Matrix=new SkirmishProductionMatrixAuthority(Path.GetFullPath(Path.Combine(Application.dataPath,"..")),Text(descriptor,"sampling_plan_path"),Text(descriptor,"sampling_plan_sha256"));')
    s=s.replace('Text(row,"claim_path"),false);','Text(row,"claim_path"),false,Matrix);')
    s=once(s,'var catalogs=new HashSet<string>(StringComparer.Ordinal);','var catalogs=new HashSet<string>(StringComparer.Ordinal);\n                var samplingSlots=new HashSet<string>(StringComparer.Ordinal);')
    s=once(s,'catalogs.Add(request.CatalogId);','catalogs.Add(request.CatalogId);\n                    if(Matrix!=null&&!samplingSlots.Add(request.CatalogId+"|"+request.Seed+"|"+(string)request.Job["locale"]))throw new InvalidDataException("Duplicate matrix sampling slot in finite batch");')
    s=once(s,'if(catalogs.Count!=2||!catalogs.Contains("S004")||!catalogs.Contains("S005"))','if(Matrix==null&&(catalogs.Count!=2||!catalogs.Contains("S004")||!catalogs.Contains("S005")))')
    s=once(s,'for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)','Matrix?.ValidateBatch(jobs.OfType<JObject>().Select(v=>SkirmishProductionCanaryRequest.ReadObject(Text(v,"job_path"))));\n                for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)')
    s=once(s,'PrepareNativeSceneBaseline();\n                preparationTimer.Stop();','PrepareNativeSceneBaseline(jobs.Select(v=>SkirmishProductionCanaryRequest.ReadObject(Text((JObject)v,"job_path"))));\n                preparationTimer.Stop();')
    s=once(s,'private void CheckLiveInputs()\n            {','private void CheckLiveInputs()\n            {\n                Matrix?.Recheck();')
    s=once(s,'if(kind=="TerminalReturnOwnerCompleted")\n                {','''if(kind=="TerminalReturnOwnerCompleted")
                {
                    if(Matrix!=null&&(facts["native_terminal_session_count"]?.Type!=JTokenType.Integer||
                        (int)facts["native_terminal_session_count"]!=0||
                        facts["native_outcome_matches_rendered_title"]?.Type!=JTokenType.Boolean||
                        !(bool)facts["native_outcome_matches_rendered_title"]||
                        facts["localized_main_menu_presentation"]==null||facts["localized_main_menu_presentation"].Type==JTokenType.Null))
                        throw new InvalidDataException("Matrix continuation requires actual zero native sessions and joined rendered result/localized MainMenu");''')
    s=once(s,'internal static void PrepareNativeSceneBaseline()','internal static void PrepareNativeSceneBaseline(IEnumerable<JObject> plannedJobs=null)')
    s=once(s,'foreach(string id in new[]{"S004","S005"})','foreach(string id in plannedJobs==null?new[]{"S004","S005"}:plannedJobs.Select(v=>(string)v["catalog_id"]).Distinct(StringComparer.Ordinal))')
    # Warm exact sources serially, rather than increasing the original shared
    # preparation World to hold four full Campaign maps concurrently.
    s=once(s,'''                using(var world=new World("ProductionBatchNativeScenePreparation",WorldFlags.Game))
                {
                    DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world,systems);
                    var catalog=SkirmishExpansionCatalogConfig.Load();''', '''                var catalog=SkirmishExpansionCatalogConfig.Load();''')
    s=once(s,'''                    foreach(string guid in guids.OrderBy(x=>x,StringComparer.Ordinal))
                    {
                        if(string.IsNullOrWhiteSpace(guid))''', '''                    foreach(string guid in guids.OrderBy(x=>x,StringComparer.Ordinal))
                    {
                      using(var world=new World("ProductionBatchNativeScenePreparation",WorldFlags.Game))
                      {
                        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world,systems);
                        if(string.IsNullOrWhiteSpace(guid))''')
    s=once(s,'private static string[] Worlds()=>World.All.Where(w=>w.IsCreated).Select(w=>w.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)).OrderBy(x=>x,StringComparer.Ordinal).ToArray();','''private static string[] Worlds()
            {var rows=new List<string>();foreach(var world in World.All)if(world.IsCreated)rows.Add(world.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));return rows.OrderBy(v=>v,StringComparer.Ordinal).ToArray();}''')
    return s
project(base+'SkirmishS002AriaRunHarness.ProductionBatch.cs',batch)
def census(s):
    s=once(s,'using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));','''using var sessions = em.CreateEntityQuery(new EntityQueryDesc {
                    Any = new[] { ComponentType.ReadOnly<SkirmishExpandedSessionComponent>(), ComponentType.ReadOnly<SkirmishMatchState>() },
                    Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState });''')
    return once(s,'foreach (var world in World.All.Where(w => w.IsCreated).OrderBy(w => w.SequenceNumber))',
        'var liveWorlds=new List<World>();foreach(var candidate in World.All)if(candidate.IsCreated)liveWorlds.Add(candidate);\n            foreach (var world in liveWorlds.OrderBy(w => w.SequenceNumber))')
project(base+'SkirmishProductionBatchOwnershipCensus.cs',census)
def canary(s):
    s=once(s,'Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_CLAIM"),productionBatch==null);','Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_CLAIM"),productionBatch==null,productionBatch?.Matrix);')
    s=once(s,'throw new InvalidDataException("Actual resolved canary identity/setup differs from owned request.");','throw new InvalidDataException("Actual resolved canary identity/setup differs from owned request.");\n            productionCanary.CaptureVerifiedIdentity(identity,(string)facts["native_config_sha256"]);')
    s=once(s,'internal readonly SkirmishProductionCanaryRequest Request;','''internal readonly SkirmishProductionCanaryRequest Request;
            private JObject verifiedNativeIdentity;
            private string verifiedNativeConfigHash;
            internal void CaptureVerifiedIdentity(JObject identity,string configHash)
            {
                Request.VerifyNativeSetupIdentity(identity);
                if(!System.Text.RegularExpressions.Regex.IsMatch(configHash??"","^[a-f0-9]{64}$")||
                    (verifiedNativeIdentity!=null&&(!JToken.DeepEquals(verifiedNativeIdentity,identity)||verifiedNativeConfigHash!=configHash)))
                    throw new InvalidDataException("Verified native setup/config identity changed within attempt");
                verifiedNativeIdentity=(JObject)identity.DeepClone();verifiedNativeConfigHash=configHash;
            }
            internal bool TryApplyNativeProvenance(ref SkirmishS002AriaTerminalFacts facts,string actualSourceHash)
            {
                if(verifiedNativeIdentity==null)return false;
                Request.VerifyNativeSetupIdentity(verifiedNativeIdentity);
                if((string)verifiedNativeIdentity["catalog_id"]!=facts.CatalogId||(int)verifiedNativeIdentity["seed"]!=facts.Seed||
                    (string)verifiedNativeIdentity["locale"]!=(facts.Locale=="en"?"EN":"FA")||
                    actualSourceHash!=(string)Request.Job["candidate_sha256"])throw new InvalidDataException("Native run provenance differs from exact claimed attempt");
                facts.DefinitionVersion=(int)verifiedNativeIdentity["definition_version"];facts.CodeHash=actualSourceHash;facts.ConfigHash=verifiedNativeConfigHash;return true;
            }''')
    return s
project(base+'SkirmishS002AriaRunHarness.ProductionCanary.cs',canary)
def main(s):
    a='''        {
            if (catalog == "S005")
                return SkirmishAriaAcceptancePayload.TryCreate("S005", "skirmish.s005", 2,'''
    b='''        {
            if(productionCanary!=null)
            {
                var request=productionCanary.Request;
                if(catalog!=request.CatalogId||selectedSeed!=request.Seed||selectedLocale!=request.Locale)
                    throw new InvalidOperationException("Payload differs from exact claimed normal journey");
                return SkirmishAriaAcceptancePayload.TryCreate(catalog,(string)request.Job["definition_id"],
                    (int)request.Job["definition_version"],SkirmishSizeId.Standard,SkirmishDifficultyId.Regular,
                    selectedSeed,selectedLocale,out payload,out error);
            }
            if (catalog == "S005")
                return SkirmishAriaAcceptancePayload.TryCreate("S005", "skirmish.s005", 2,'''
    s=once(s,a,b)
    s=once(s,'if (!SkirmishS002AriaRunLog.TryNextAriaRunId(csv, catalogId, seed, locale, out string runId, out string idError))','''string runId=null,idError=null;
            bool matrixJob=productionBatch?.Matrix!=null;
            if(matrixJob)
            {
                productionBatch.Matrix.ValidateJob(productionCanary.Request.Job);
                runId=catalogId+"-aria-matrix-"+(string)productionCanary.Request.Claim["attempt_id"];
            }
            if (!matrixJob&&!SkirmishS002AriaRunLog.TryNextAriaRunId(csv, catalogId, seed, locale, out runId, out idError))''')
    s=once(s,'if (TryReadCensus(root, out SkirmishAcceptanceCensus census))\n            {\n                facts.DefinitionVersion', '''bool nativeProvenance=!matrixJob;
            if(matrixJob)nativeProvenance=productionCanary.TryApplyNativeProvenance(ref facts,SessionState.GetString(CandidateHashKey,""));
            else if (TryReadCensus(root, out SkirmishAcceptanceCensus census))
            {
                facts.DefinitionVersion''')
    s=once(s,'bool counted = appended && SkirmishS002AriaRunLog.IsCountedAriaWin(in facts, RowResult(row));','bool counted = appended && nativeProvenance && SkirmishS002AriaRunLog.IsCountedAriaWin(in facts, RowResult(row));')
    s=once(s,'RequestQuitIfAsked(counted ? 0 : 2);','''bool operational=matrixJob&&SkirmishProductionMatrixAuthority.IsOperationallyComplete(in facts,appended,terminalSnapshotValid);
            if(matrixJob)Debug.Log("[SkirmishProductionOperationalRun] result="+(operational?"Completed":"Failed")+
                " outcome="+outcome+" countedWin="+(counted?1:0)+" queueSuccessEnabled=0");
            RequestQuitIfAsked(matrixJob?(operational?0:2):(counted?0:2));''')
    s=once(s,'private static bool TryReadCensus(string root, out SkirmishAcceptanceCensus census)\n        {\n            census = null;',
        'private static bool TryReadCensus(string root, out SkirmishAcceptanceCensus census)\n        {\n            census = null;\n            if(productionBatch?.Matrix!=null)return false; // Matrix records use verified native setup observations, never the S002 fallback.')
    # Matrix jobs use normal scene/loading/play ownership for every catalog,
    # matching the existing strict S005 normal-input canary path.
    return once(s,'bool nativeS005MenuJourney = catalogId == "S005" && Environment.GetEnvironmentVariable("WARLINE_S004_MENU_JOURNEY") == "1";',
        'bool nativeS005MenuJourney = (catalogId == "S005" || productionCanary != null) && Environment.GetEnvironmentVariable("WARLINE_S004_MENU_JOURNEY") == "1";')
project(base+'SkirmishS002AriaRunHarness.cs',main)
def menu(s):
    # A production job must use its actual persisted catalog availability, including
    # the normal refusal of unpublished/missing definitions. Legacy review stays.
    return once(s,'if (selectedCatalog == "S005")','if (selectedCatalog == "S005" && productionCanary == null)')
project(base+'SkirmishS002AriaRunHarness.MenuProbe.cs',menu)
authority=OUT/'SkirmishProductionMatrixAuthority.cs.txt'
files.append(dict(target=base+'SkirmishProductionMatrixAuthority.cs',source=authority.relative_to(ROOT).as_posix(),before_sha256=None,sha256=sha(authority)))
validation=OUT/'SkirmishProductionMatrixAuthorityValidation.cs.txt'
files.append(dict(target=base+'SkirmishProductionMatrixAuthorityValidation.cs',source=validation.relative_to(ROOT).as_posix(),before_sha256=None,sha256=sha(validation)))
tests=OUT/'SkirmishProductionMatrixAuthorityTests.cs.txt'
files.append(dict(target='Assets/Tests/Editor/SkirmishExpansion/SkirmishProductionMatrixAuthorityTests.cs',source=tests.relative_to(ROOT).as_posix(),before_sha256=None,sha256=sha(tests)))
tools=[]
def tool(target,transform):
    actual=ROOT/target;source=OUT/'Sources'/target;source.parent.mkdir(parents=True,exist_ok=True)
    source.write_text(transform(actual.read_text(encoding='utf-8-sig')),encoding='utf-8',newline='\n')
    tools.append(dict(target=target,source=source.relative_to(ROOT).as_posix(),before_sha256=sha(actual),sha256=sha(source)))
def queue(s):
    s=once(s,'def preflight(job, project):','def preflight(job, project, sampling_authority=None):')
    s=once(s,'require(job["seed"] in allowed, "seed does not match packet/configuration")','''if sampling_authority is None:
        require(job["seed"] in allowed, "seed does not match packet/configuration")
    else:
        sampling_authority.validate_job(job)''')
    s=once(s,'def claim(directory, worker, project):','def claim(directory, worker, project, sampling_authority=None):')
    s=once(s,'        preflight(job, project)\n        require(not (directory / "claim.json")','        preflight(job, project, sampling_authority)\n        require(not (directory / "claim.json")')
    s=once(s,'def enqueue(queue, job, project):\n    preflight(job, project)','def enqueue(queue, job, project, sampling_authority=None):\n    preflight(job, project, sampling_authority)')
    s=once(s,'    args = parser.parse_args()\n    try:', '''    parser.add_argument("--sampling-plan", type=Path)
    parser.add_argument("--sampling-sha")
    args = parser.parse_args()
    try:
        require((args.sampling_plan is None)==(args.sampling_sha is None), "Both sampling plan/path SHA required")
        authority=None
        if args.sampling_plan is not None:
            from skirmish_production_matrix_authority import SamplingAuthority
            authority=SamplingAuthority(args.project,args.sampling_plan,args.sampling_sha)''')
    s=once(s,'result = preflight(job, args.project)','result = preflight(job, args.project, authority)')
    s=once(s,'result = enqueue(args.queue, job, args.project)','result = enqueue(args.queue, job, args.project, authority)')
    s=once(s,'result = claim(args.directory, args.worker, args.project)','result = claim(args.directory, args.worker, args.project, authority)')
    return s
tool('Tools/CI/skirmish_production_queue.py',queue)
def prep(s):
    s=once(s,'from s005_windows_source_snapshot import candidate_hash','from s005_windows_source_snapshot import candidate_hash\nfrom skirmish_production_matrix_authority import SamplingAuthority')
    s=once(s,'def prepare(project,job_directories,snapshot_path,batch_id):','def prepare(project,job_directories,snapshot_path,batch_id,sampling_plan=None,sampling_sha=None):')
    s=once(s,'    if len(job_directories)!=2 or len({p.resolve() for p in job_directories})!=2:',
        '    authority=SamplingAuthority(project,sampling_plan,sampling_sha) if sampling_plan is not None else None\n    if not 1<=len(job_directories)<=(1200 if authority else 2) or len({p.resolve() for p in job_directories})!=len(job_directories) or (authority is None and len(job_directories)!=2):')
    s=once(s,"job=load(directory/'job.json');preflight(job,project)","job=load(directory/'job.json');preflight(job,project,authority)")
    s=once(s,"if job['catalog_id'] not in ('S004','S005') or job['size']!='Standard' or job['difficulty']!='Regular':","if (authority is None and job['catalog_id'] not in ('S004','S005')) or job['size']!='Standard' or job['difficulty']!='Regular':")
    s=once(s,"    with (output/'batch.json').open", "    if authority is not None:\n        descriptor.update(schema_version=2,sampling_plan_path=str(authority.path),sampling_plan_sha256=authority.expected)\n    with (output/'batch.json').open")
    s=once(s,'    # No queue metadata is mutated here.','    if authority is not None: authority.validate_batch([load(Path(row["job_path"])) for row in jobs])\n    # No queue metadata is mutated here.')
    s=once(s,"    parser.add_argument('--snapshot',type=Path,required=True)","    parser.add_argument('--snapshot',type=Path,required=True)\n    parser.add_argument('--sampling-plan',type=Path)\n    parser.add_argument('--sampling-sha')")
    s=once(s,"args.snapshot,'batch-'+uuid.uuid4().hex)","args.snapshot,'batch-'+uuid.uuid4().hex,args.sampling_plan,args.sampling_sha)")
    return s
tool('Tools/CI/prepare_skirmish_production_batch.py',prep)
def launch(s):
    s=once(s,'from s005_windows_source_snapshot import snapshot, build_snapshot, candidate_hash','from s005_windows_source_snapshot import snapshot, build_snapshot, candidate_hash\nfrom skirmish_production_matrix_authority import SamplingAuthority')
    s=once(s,'def verify_native_events(descriptor, events, log):','def verify_native_events(descriptor, events, log, project=None):')
    s=once(s,'    if MARKER not in log or re.search(',"    if descriptor.get('schema_version')==2:\n        if project is None: raise ValueError('Actual project required to revalidate matrix authority')\n        SamplingAuthority(project,descriptor['sampling_plan_path'],descriptor['sampling_plan_sha256']).validate_batch([load(Path(row['job_path'])) for row in descriptor['jobs']])\n    count=len(descriptor['jobs'])\n    marker='[SkirmishProductionBatch] result=Completed jobs='+str(count)+' queueSuccessEnabled=0'\n    if marker not in log or re.search(")
    s=s.replace("completed[0]['completed_jobs'] != 2","completed[0]['completed_jobs'] != count").replace('len(set(dispatch)) != 2','len(set(dispatch)) != count').replace("native_canaries=2","native_jobs=count")
    s=once(s,'return dict(native_jobs=count,','return dict(native_jobs=count, native_canaries=(count if descriptor.get("schema_version",1)==1 else 0),')
    s=once(s,"for owner in ('LaunchCatalog', 'Finish', 'LaunchCatalog', 'Finish')","for owner in ('LaunchCatalog', 'Finish') * count")
    s=once(s,"    if len(descriptor['jobs']) != 2 or not 1 <= args.timeout <= 28800:","    matrix=descriptor.get('schema_version')==2\n    authority=SamplingAuthority(project,descriptor['sampling_plan_path'],descriptor['sampling_plan_sha256']) if matrix else None\n    if not 1<=len(descriptor['jobs'])<=(1200 if matrix else 2) or (not matrix and len(descriptor['jobs'])!=2) or not 1 <= args.timeout <= 28800:")
    s=once(s,"    output = Path(descriptor['artifact_directory']).resolve()","    if authority is not None:\n        for row in descriptor['jobs']: authority.validate_job(load(Path(row['job_path'])))\n    output = Path(descriptor['artifact_directory']).resolve()")
    s=once(s,"        for row in descriptor['jobs']: authority.validate_job(load(Path(row['job_path'])))","        authority.validate_batch([load(Path(row['job_path'])) for row in descriptor['jobs']])")
    s=s.replace("result='TwoCanariesOperationallyCompletedUnqualified'","result='FiniteBatchOperationallyCompletedUnqualified'")
    s=once(s,"events, log.read_text(encoding='utf-8-sig', errors='replace'))","events, log.read_text(encoding='utf-8-sig', errors='replace'), project)")
    return s
tool('Tools/CI/invoke_skirmish_production_batch.py',launch)
newtool=OUT/'skirmish_production_matrix_authority.py'
tools.append(dict(target='Tools/CI/skirmish_production_matrix_authority.py',source=newtool.relative_to(ROOT).as_posix(),before_sha256=None,sha256=sha(newtool)))
(OUT/'manifest.draft.json').write_text(json.dumps(dict(schema=1,scope='ExistingBatchV3FiniteBaseline720AndExplicitStress1200Admission',files=files,tools=tools,native_execution=False,queue_success_enabled=False),indent=2)+'\n',encoding='utf-8')
print('Prepared',len(files),'draft targets; no Assets writes')
