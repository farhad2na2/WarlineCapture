import json,hashlib,re,shutil,zipfile
from pathlib import Path
root=Path(__file__).resolve().parents[4]
# This report lives at Design/AgentReports/MissionAtlasColorPolish/menu-card-followup.
out=Path(__file__).resolve().parent
root=out.parents[3]
state=json.loads((out/'VALIDATION.json').read_text())
assert state['status']=='Passed' and state['candidateSourceHash']==state['finalSourceHash']
visual=root/state['nativeVisualOutput'];sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def archive(dest,files,base=None):
 with zipfile.ZipFile(dest,'w',zipfile.ZIP_DEFLATED,6) as z:
  for p in sorted(set(files)):
   if p.is_file():z.write(p,str(p.relative_to(base)) if base else p.name)
 assert dest.stat().st_size<100*1024*1024
screens=out/'screens';screens.mkdir(exist_ok=True)
names=['campaign-false-front-en.png','skirmish-maps-en.png','skirmish-maps-1440x1080-en.png','skirmish-maps-fa-IR.png','campaign-chapters-en.png','campaign-missions-en.png','campaign-missions-1440x1080-en.png','campaign-missions-1440x1080-fa-IR.png','skirmish-s002-en.png','skirmish-missions-1440x1080-fa-IR.png']
for name in names:shutil.copy2(visual/name,screens/name)
files=list(visual.glob('*.png'));assert len(files)==40
(out/'NATIVE_CAPTURE_SHA256.json').write_text(json.dumps({p.name:sha(p) for p in sorted(files)},indent=2)+'\n');archive(out/'native-visual-captures.zip',files)
logs=[p for p in (out/'logs').rglob('*') if p.is_file()]+[out/'validate.py']
logs += [p for p in (out/'attempts').rglob('*') if p.is_file() and p.suffix in ('.log','.json','.txt','.xml','.py')] if (out/'attempts').exists() else []
archive(out/'full-validation-logs.zip',logs,out)
for stage in state['stages']:
 if 'proof' not in stage:continue
 p=Path(stage['proof']);prefix=p.name.split('.jsonl')[0];archive(out/(stage['name']+'-native-evidence.zip'),list(p.parent.glob(prefix+'*')))
 proof=json.loads(p.read_text());locale=proof['locale'];shutil.copy2(p,out/('s002-'+locale+'-medal-proof.json'))
 for suffix,name in [('.jsonl.terminal-destination.png','earned'),('.jsonl.terminal-result.png','result')]:
  source=p.parent/(prefix+suffix)
  if source.exists():shutil.copy2(source,screens/('s002-'+locale+'-'+name+'.png'))
text=(out/'logs/campaign.log').read_text(errors='replace');match=re.search(r'\[CampaignTutorialEntry\] evidence=(.+)',text);assert match
folder=Path(match.group(1).strip());archive(out/'campaign-native-evidence.zip',list(folder.rglob('*')),folder)
source=root/'Assets/Game/Art/SkirmishLibrary/Theatre/skirmish.theatre.base.png'
(out/'ATLAS_THUMBNAIL_PROVENANCE.json').write_text(json.dumps({'source':str(source.relative_to(root)),'sha256':sha(source),'method':'Native UV cover crops of the existing atlas illustration; no new bitmap or physical map','sourceRegions':[{'map':m,'rect':r} for m,r in zip(['DB','CC','MP','IB','AP'],[[.04,.66,.38,.30],[.50,.66,.48,.32],[.02,.26,.42,.39],[.52,.26,.46,.40],[.30,0,.50,.30]])]},indent=2)+'\n')
review='''# Map cards, Campaign comics and army-size choices

The requested visual changes are implemented. Owner review before merging remains pending.

- Map cards now retain thumbnails. Skirmish crops the matching district from the existing atlas painting; Campaign uses its existing exact chapter comics.
- Map and chapter headings share their accent colors between the map and left rail. Progress and availability have distinct colors, with a cyan outline for selection.
- Campaign mission comics occupy 40% of each card width with a taller crop and padded number/title/status areas.
- Standard, War and Large War remain mutually exclusive army-size choices. Their real bound buttons have bordered navy faces, a blue/cyan selected state and one check. Unpublished sizes retain their lock and disabled state.
- Physical maps, publication gates, scenario bindings, army budgets and gameplay rules are unchanged.

## Actual game screens

![Campaign missions with larger comics and colored chapter rail](screens/campaign-false-front-en.png)

![Skirmish map cards and colored rail](screens/skirmish-maps-en.png)

![Army-size choices](screens/skirmish-s002-en.png)

[Narrow Campaign](screens/campaign-missions-1440x1080-en.png), [narrow Persian Campaign](screens/campaign-missions-1440x1080-fa-IR.png), [narrow Persian Skirmish](screens/skirmish-missions-1440x1080-fa-IR.png).

## Separate validation and acceptance gates

'''
review+='Frozen worktree source SHA-256: `'+state['candidateSourceHash']+'`. Independent start/end source censuses match.\n\n'
review+='''- Native visuals: 40 screenshots and 24 page/locale/ratio cases pass in EN and fa-IR at 2400, 1920 and 1440 x 1080. Touch navigation, map choices, scroll, back/return, difficulty and locked army-size rejection pass. Artwork presence, map/rail color equality, glyph padding, truncation, scrollbar bounds and gear centering pass.
- Automated checks: 35 focused tests pass.
- Normal-input Campaign: fresh M01 touch victories and returns in both locales, first-launch fixture only, outcomesInjected=0.
- Normal-input Skirmish: fresh Standard/Regular S002 normal-speed ARIA victories, native results, returned mission-card medals and independent-process reloads in both locales. War and Large War remain unpublished and disabled. These journeys do not certify those sizes or all mission IDs.
- Owner visual review, clean-checkout, Windows and real player/device acceptance remain pending.

The exact worktree includes preserved pre-existing SCN08 HUD and NotoSansArabic font edits excluded from this PR. Its fingerprints do not certify a clean checkout. Layout screenshots use progress fixtures; the separate fresh normal-input journey archives supply result evidence.

## Interrupted evidence

A host reboot at 12:37 on 2026-10-21 Europe/Berlin interrupted the pre-reboot S002 run and removed its temporary logs. Interrupted status/source census and native captures were retained. Pre-reboot visual attempts caught an assumption that unpublished sizes were enabled, incomplete test-touch dispatch and Persian label wrapping; one attempt also timed out returning through Campaign briefing. Those missing raw logs are not represented as final evidence.

The post-reboot Editor waited at a Recovering Scene Backups dialog. The exact recovery file was preserved outside Assets before declining the additional Assets/_Recovery copy. The recovery receipt and startup process sample are retained; the backup itself remains local, outside the PR. Final validation uses persistent project logs. Failed or timed-out post-reboot attempts, if any, remain in the full log archive.

Final native and journey logs, test XML, all 40 visual captures and hashes, exact source manifest and native journey evidence accompany this report.
'''
(out/'REVIEW.md').write_text(review)
owned=['Assets/Game/Scripts/Editor/ApprovedMissionAtlasBuilder.cs','Assets/Game/Scripts/Editor/ApprovedMissionAtlasReview.cs','Assets/Game/Scripts/UI/Components/UISegmentedControlView.cs','Assets/Game/Scripts/UI/Screens/MissionAtlasArmySizeView.cs','Assets/Game/Scripts/UI/Screens/MissionAtlasArmySizeView.cs.meta','Assets/Game/Scripts/UI/Screens/MissionAtlasArtworkView.cs','Assets/Game/Scripts/UI/Screens/MissionAtlasScreenView.Polish.cs','Assets/Game/Scripts/UI/Screens/MissionAtlasScreenView.cs','Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab','Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab']
(out/'OWNED_SOURCE_PATHS.txt').write_text('\n'.join(owned)+'\n')
files=[p for p in out.rglob('*') if p.is_file() and p.name!='EVIDENCE_SHA256.json' and p.suffix not in ('.backup','.log') and '/logs/' not in str(p) and '/attempts/' not in str(p)]
(out/'EVIDENCE_SHA256.json').write_text(json.dumps({str(p.relative_to(out)):sha(p) for p in sorted(files)},indent=2)+'\n')
print('Collected final follow-up evidence:',out)
