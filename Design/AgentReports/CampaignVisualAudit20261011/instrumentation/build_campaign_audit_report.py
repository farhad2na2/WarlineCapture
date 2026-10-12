import json, hashlib, html, re, shutil
from pathlib import Path
p=Path('/Volumes/T7/Projects/WarlineCapture-CampaignVisualAudit')
root=p/'Design/AgentReports/CampaignVisualAudit20261011'
rows=json.loads((root/'inventory.json').read_text())['missions']
initial=json.loads(Path('/private/tmp/campaign_audit_findings.json').read_text())
findings=initial['missions']
for name in ['ch02','m10','ch03','m14_m16','m17_m20','m22','ch05_blocked']:
 findings+=json.loads(Path('/private/tmp/campaign_audit_'+name+'.json').read_text())
findings.sort(key=lambda f:f['index'])
assert [f['index'] for f in findings]==list(range(25))
manifest=[]
for f,row in zip(findings,rows):
 f.update({k:row[k] for k in ['missionId','mapId','definitionPath','scenarioPath','mapPath','sourceScene','sourceBinding']})
 f['physicalSource']=row['sourceScene'].replace('RuntimeBinding.unity','PreparedEntities.unity') if 'dense_city' not in row['sourceScene'] else 'Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity'
 anchors=json.loads(row['mapJson'])['MonoBehaviour']['anchors']
 f['anchors']=[a for a in anchors if any(a['anchorId'].endswith('.'+s) for s in f['focusAnchors'])]
 f['allAnchors']=anchors
 captures=sorted((root/'captures'/f['missionId']).glob('run-*'))
 receipts=[(d,json.loads((d/'receipt.json').read_text())) for d in captures if (d/'receipt.json').exists()]
 passed=[(d,r) for d,r in receipts if r.get('result')=='Passed']
 source=root/'source-previews'/f['missionId']/'run-01'
 f['runs']=[dict(path=str(d.relative_to(root)),result=r.get('result'),missionOutcome=r.get('missionOutcome','unrecorded-v1'),error=r.get('error',''),cameraSettled=r.get('cameraSettled','unrecorded-v1')) for d,r in receipts]
 if passed:
  d,r=passed[-1]; f['evidenceKind']='Native runtime environment; failed result visible' if f['index']==16 else 'Native runtime environment and opening HUD'
  f['selectedRun']=str(d.relative_to(root)); f['cameraSettled']=r.get('cameraSettled','unrecorded-v1')
  f['images']=[str((d/(n+'.png')).relative_to(root)) for n in ['opening-hud','objective-context','objective-detail','mission-overview']]
  f['images'] += [str(x.relative_to(root)) for x in sorted(d.glob('*.png')) if x.stem not in ['opening','opening-hud','objective-context','objective-detail','mission-overview']]
  f['evidenceReceipt']=str((d/'receipt.json').relative_to(root))
  f['lightStates']=r.get('lightStates',[])
 else:
  assert (source/'receipt.json').exists(),f['missionId']
  f['evidenceKind']='Authored source preview — runtime readiness failed'
  f['selectedRun']=str(source.relative_to(root)); f['images']=[str(x.relative_to(root)) for x in sorted(source.glob('*.png'))]
  f['evidenceReceipt']=str((source/'receipt.json').relative_to(root)); f['cameraSettled']='not-applicable';f['lightStates']=[]
 runway=root/'source-runway-previews'/f['missionId']/'run-01'
 if (runway/'receipt.json').exists():
  f['images'] += [str(x.relative_to(root)) for x in sorted(runway.glob('*-runway-*-end-*.png'))]
  f['runwayPreviewReceipt']=str((runway/'receipt.json').relative_to(root))
  for file in runway.iterdir():
   if file.is_file():manifest.append({'path':str(file.relative_to(root)),'bytes':file.stat().st_size,'sha256':hashlib.sha256(file.read_bytes()).hexdigest()})
 f['reviewStatus']='Viewed native contact sheet and mission-specific evidence; recommendations documented'
 for file in (root/'captures'/f['missionId']).rglob('*'):
  if file.is_file() and file.suffix in ['.png','.json','.txt'] and 'isolated-profile' not in file.parts:
   manifest.append({'path':str(file.relative_to(root)),'bytes':file.stat().st_size,'sha256':hashlib.sha256(file.read_bytes()).hexdigest()})
 if source.exists():
  for file in source.iterdir():
   if file.is_file():manifest.append({'path':str(file.relative_to(root)),'bytes':file.stat().st_size,'sha256':hashlib.sha256(file.read_bytes()).hexdigest()})
for f in findings:
 for x in f['images']:assert (root/x).is_file(),x
data={'date':'2026-10-11','baseline':json.loads((root/'workspace.json').read_text()),'ownerPriorities':initial['owner_priorities'],'missions':findings}
(root/'findings.json').write_text(json.dumps(data,indent=2))
(root/'evidence-manifest.json').write_text(json.dumps({'files':manifest,'missionCount':25,'runtimeEnvironmentMissions':21,'usableOpeningHudMissions':20,'sourcePreviewOnlyMissions':4,'normalInputPlaythroughs':0,'deviceAcceptance':'pending'},indent=2))
intro='''# Campaign visual audit — all 25 missions

Audited 2026-10-11 at baseline `c88c79c5453532a3dca9c9c2fd0c9d37603daed7` in the separate T7 checkout, branch `codex/campaign-visual-audit-20261011`.

The biggest visual gains are coherent civic/bazaar paving, properly finished road/sidewalk transitions and runway ends, stronger destination identities, and opening cameras that show the player's actual force. More random texture or prop scatter would leave the main composition problems unresolved.

Start with the [visual gallery](gallery.html) and [implementation plan](IMPLEMENTATION_PLAN.md). [Structured findings](findings.json), [source/mission inventory](inventory.json), [hashed evidence manifest](evidence-manifest.json), and [workspace provenance](workspace.json) and [reproduction notes](REPRODUCTION.md) support the review.

## Scope and evidence

All 25 missions have a visual entry. Twenty-one produced native runtime environment captures; twenty have usable opening HUD evidence. Steel Push instead shows an immediate failure result. Four Chapter 5 missions fail native map readiness and were reviewed through freshly rendered authored-source previews. Those previews do not demonstrate a playable mission or its opening camera.

Captures use the existing GUI Unity Editor, Metal, 1600×900 runtime views and 1600×1000 source previews. The checked macOS wrapper runs every capture. An isolated unlocked save profile, programmatic menu deployment and native narrative Skip actions make the work reproducible without altering the user's save. Inspection cameras move only the camera; objective results are not injected. The broad overview deliberately exceeds ordinary gameplay framing and may show fog, terrain edges or voids unavailable to the player. Source-preview overview fog/post processing is suppressed for inspection; source detail views retain authored scene lighting.

Capture-helper pass markers mean images were written, not that gameplay passed. No complete normal-input mission playthrough or Android/player acceptance is claimed. Generated compile checks, wrapper logs and failed attempts are retained under `validation/`. V1 diagnostics list disabled lights and include an unused hour=0 field; neither is evidence of actual double lighting or midnight. V2 receipts record enabled light states, settled camera status and mission outcome.

## Priority findings

| Priority | Finding | Recommended first change |
|---|---|---|
| P0 | Four Chapter 5 maps reject packed entity identity counts | Trace bake/metadata ownership and regenerate coherent content; keep readiness guard intact |
| P0 | Steel Push opens on RESERVE DEFENSE FAILED with reserve depot/Barracks placement failures | Repair the functional placement/launch issue before dressing the site |
| P0 verification | Supply Line logs a failed refinery placement | Confirm the intended resource facility exists and its normal mission sequence works |
| P1 | Hall/bazaar looks like an asphalt grid with disconnected paving and scattered shade | A continuous civic stone forecourt, defined vehicle edge and usable stall clusters |
| P1 | Sidewalk and uncurbed road classes meet abruptly | Matched corner/curb returns, flush driveways and deliberate shoulder transitions |
| P1 | Runways look like strips ending abruptly in sand | Treat both assembled endpoints, thresholds, shoulders, taxiway joins and end ground |
| P1 | Several opening cameras show empty ground or crop the force/destination | Reframe after narrative settling, within actual HUD safe area |
| P1 | Shared maps repeat the same street composition across distinct missions | Function-specific destination props and camera views, preserving map bindings |
| P1 | Some bindings keep a source sun and Match sun active | Verify enabled ownership per map, compare one deliberate key, then tune materials |
| P2 | Uniform concrete/sand, isolated rocks and repetitive clutter | Large-scale material variation and small groups tied to real activity |

## Native launch gates

| Mission | Observed gate | Evidence |
|---|---|---|
| 07 Supply Line | Refinery at (740,475), footprint (25,15), placement rejected; runtime capture continues | [native log](validation/m07-01.log) |
| 17 Steel Push | Reserve depot at (805,650), footprint (24,23), and Barracks at (740,650), footprint (28,15), rejected; failure screen visible | [HUD](captures/saga.ch04.m02.steel_push/run-01/opening-hud.png), [native log](validation/m17-01.log) |
| 21 Citywide Alert | Generated identity count 2942 / expected 2979 | [retry log](validation/m21-02.log) |
| 23 Network Collapse | 2935 / 2972 | [retry log](validation/m23-02.log) |
| 24 Last Corridor | 2935 / 2972 | [retry log](validation/m24-02.log) |
| 25 Command Node | 2633 / 2695 | [retry log](validation/m25-02.log) |

Each failed Chapter 5 load was retried after a synchronous force import of its authored source; the mismatch persisted. This rules out that narrow cache refresh as a remedy, not every possible cache/content cause. Source previews preserve the authored file hash before/after. The repeated 37-identity difference in three maps is a useful lead; Command Node differs by 62. Recurring tent placement failures are also retained in logs for a shared placement audit. Art must not hide missing gameplay owners.

## Shared-source impact

The 25 mission definitions resolve to 11 physical source bindings. DenseCity is shared by 11 missions; CityEdgeAirfield by two; RefineryDistrict by four; AshLinePort by one. Grounded Signal, Armor Break and the five Chapter 5 missions each have their own existing authored source. Coordinate a source edit with every Campaign, Skirmish and Operations consumer. Do not infer new physical maps from mission count.

## Mission review

World coordinates below are (X, Z) from the shipping map's authored anchors. Suggestions describe future changes; observations describe the captured evidence. All anchors, asset paths and raw receipts are also in findings.json.
'''
md=[intro]
for f in findings:
 n=f['index']+1
 md.append(f'### {n:02d}. {f["title"]} — {f["priority"]}\n\n`{f["missionId"]}` · **{f["evidenceKind"]}**\n\n{f["intent"]}\n\n**Observed:** {f["observed"]}\n')
 for key,label in [('camera','Camera'),('light','Lighting'),('ground','Ground and roads'),('props','Props and city design'),('landscape','Landscape')]:md.append(f'**{label}:** {f[key]}\n')
 if f['anchors']:md.append('**Locations:** '+ '; '.join(f'`{a["anchorId"].split(".")[-1]}` ({a["position"]["x"]:g}, {a["position"]["z"]:g})' for a in f['anchors'])+'.\n')
 else:md.append('**Locations:** use the complete authored anchor list in findings.json; suggested suffixes do not match this map verbatim.\n')
 md.append('**Evidence:** '+', '.join(f'[{Path(s).stem}]({s})' for s in f['images'])+f'; [receipt]({f["evidenceReceipt"]}).\n')
 if f.get('runwayPreviewReceipt'):md.append(f'**Additional runway evidence:** endpoint images marked source-runway are authored-source previews, without runtime actors/HUD. [Source hash and receipt]({f["runwayPreviewReceipt"]}).\n')
 md.append(f'**Physical source:** `{f["physicalSource"]}`.\n')
(root/'FINDINGS.md').write_text('\n'.join(md))
shutil.copy2('/private/tmp/campaign_implementation_plan.md',root/'IMPLEMENTATION_PLAN.md')
cards=[]
for f in findings:
 n=f['index']+1;esc=html.escape
 pics=''.join(f'<a href="{esc(s)}" target="_blank"><figure><img loading="lazy" src="{esc(s)}" alt="{esc(f["title"]+" — "+Path(s).stem)}"><figcaption>{esc(Path(s).stem)}</figcaption></figure></a>' for s in f['images'])
 detail=''.join(f'<dt>{label}</dt><dd>{esc(f[key])}</dd>' for key,label in [('camera','Camera'),('light','Lighting'),('ground','Ground / roads'),('props','Props / city'),('landscape','Landscape')])
 cls='blocked' if not f['evidenceKind'].startswith('Native') else 'runtime'
 cards.append(f'<article id="m{n:02}" data-chapter="{f["index"]//5+1}" data-kind="{cls}"><header><span class="number">{n:02}</span><div><h2>{esc(f["title"])}</h2><p class="tag">{esc(f["priority"])} · {esc(f["evidenceKind"])}</p></div></header><p>{esc(f["observed"])}</p><div class="images">{pics}</div><details><summary>Recommendations and evidence</summary><p><strong>Intent:</strong> {esc(f["intent"])}</p><dl>{detail}</dl><p><a href="{f["evidenceReceipt"]}">Raw receipt</a> · <a href="FINDINGS.md">Full report and coordinates</a></p><p class="path">{esc(f["physicalSource"])}</p></details></article>')
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Campaign visual audit · 25 missions</title><style>
:root{color-scheme:dark;font-family:system-ui,sans-serif;background:#10171c;color:#e6ecee}*{box-sizing:border-box}body{margin:0 auto;max-width:1500px;padding:32px}h1{font-size:clamp(28px,4vw,48px);margin:10px 0}h2{font-size:24px;margin:0}.lead{max-width:960px;line-height:1.6;color:#c7d0d5}a{color:#8dcbd9}nav{position:sticky;top:0;background:#10171cf2;z-index:2;padding:16px 0;display:flex;flex-wrap:wrap;gap:12px;border-bottom:1px solid #33434b}select,input{background:#223039;color:#fff;border:1px solid #52636c;border-radius:6px;padding:10px;font:inherit}input{flex:1;min-width:200px}.stats{display:flex;gap:16px;flex-wrap:wrap;margin:24px 0}.stats span{background:#20313a;padding:16px;border-radius:8px}.note{border-left:3px solid #d4b268;padding:10px 18px;line-height:1.6;background:#282820}article{margin:26px 0;padding:24px;background:#19252c;border:1px solid #34434b;border-radius:12px}article header{display:flex;gap:16px;align-items:center}.number{font-size:28px;color:#78b0bf}.tag{color:#d2bc8c;font-size:14px}article>p{line-height:1.6;max-width:1200px}.images{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}figure{margin:0;background:#0a1115;border-radius:5px;overflow:hidden}img{width:100%;display:block;aspect-ratio:16/9;object-fit:contain}figcaption{padding:8px 12px;color:#b6c8cf;font-size:13px}details{margin-top:20px}summary{cursor:pointer;color:#99d3db;padding:8px 0}dl{line-height:1.65}dt{font-weight:700;margin-top:14px}dd{margin:3px 0;color:#d2dce0}.path{overflow-wrap:anywhere;color:#91a8b3;font-size:13px}[hidden]{display:none!important}footer{padding:24px 0;color:#a2b7c0}@media(max-width:720px){body{padding:16px}.images{grid-template-columns:1fr}article{padding:14px}.stats span{padding:10px}}
</style><body><p>WARLINE / CAMPAIGN / 2026-10-11</p><h1>Make each mission feel like a place.</h1><p class="lead">A visual audit of all 25 Campaign missions: civic paving and bazaars, connected sidewalks, believable runway ends, functional destinations and useful opening views. These are native evidence images and proposed improvements, not implemented mockups.</p><p><a href="FINDINGS.md">Detailed findings</a> · <a href="IMPLEMENTATION_PLAN.md">Implementation plan</a> · <a href="evidence-manifest.json">Evidence manifest</a></p><div class="stats"><span><b>25</b> visual reviews</span><span><b>20</b> usable opening HUDs</span><span><b>1</b> failed-result capture</span><span><b>4</b> source-only previews</span></div><p class="note">Runtime readiness failed for Citywide Alert, Network Collapse, Last Corridor and Command Node. Their source previews do not prove a playable mission. Steel Push shows a failure result. Wide overviews are diagnostic camera views; normal-input playthroughs and Android acceptance remain pending.</p><nav><label>Chapter <select id="chapter"><option value="all">All five</option><option>1</option><option>2</option><option>3</option><option>4</option><option>5</option></select></label><label>Evidence <select id="kind"><option value="all">All</option><option value="runtime">Runtime captures</option><option value="blocked">Source previews</option></select></label><input id="search" placeholder="Find a mission or topic…" aria-label="Find a mission or topic"><span id="count"></span></nav><main>'''+''.join(cards)+'''</main><footer>Baseline c88c79c5453532a3dca9c9c2fd0c9d37603daed7 · Separate T7 audit checkout · No production art/gameplay edits.</footer><script>const cards=[...document.querySelectorAll('article')];function filter(){const c=document.getElementById('chapter').value,k=document.getElementById('kind').value,q=document.getElementById('search').value.toLowerCase();let n=0;for(const a of cards){a.hidden=!((c==='all'||a.dataset.chapter===c)&&(k==='all'||a.dataset.kind===k)&&a.textContent.toLowerCase().includes(q));if(!a.hidden)n++}document.getElementById('count').textContent=n+(n===1?' mission':' missions')}for(const id of ['chapter','kind','search'])document.getElementById(id).addEventListener('input',filter);filter();</script></body></html>'''
(root/'gallery.html').write_text(page)
for file in ['/private/tmp/CampaignVisualAudit20261011.cs','/private/tmp/CampaignSourceVisualAudit20261011.cs','/private/tmp/run_campaign_visual_audit.py','/private/tmp/run_campaign_visual_audit_tail.py','/private/tmp/run_campaign_visual_audit_supplemental.py','/private/tmp/campaign_audit_contacts.py','/private/tmp/campaign_audit_detail_contacts.py','/private/tmp/run_campaign_audit_final_captures.py','/private/tmp/run_campaign_runway_capture.py','/private/tmp/build_campaign_audit_report.py']:
 shutil.copy2(file,root/'instrumentation'/Path(file).name)
print(json.dumps({'missions':len(findings),'evidenceFiles':len(manifest),'physicalSources':len(set(f['physicalSource'] for f in findings)),'report':str(root/'FINDINGS.md')},indent=2))
