import json,re,hashlib
from pathlib import Path
from html.parser import HTMLParser
from PIL import Image
root=Path('/Volumes/T7/Projects/WarlineCapture-CampaignVisualAudit/Design/AgentReports/CampaignVisualAudit20261011')
data=json.loads((root/'findings.json').read_text());fs=data['missions']
assert len(fs)==25 and len({f['missionId'] for f in fs})==25
assert all(f['anchors'] and f['images'] for f in fs)
errors=[];links=0
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  global links
  for k,v in attrs:
   if k in ['href','src'] and v and not v.startswith(('#','http:','https:','data:')):
    links+=1
    if not (root/v).exists():errors.append('Missing HTML link '+v)
parser=Links();parser.feed((root/'gallery.html').read_text())
for name in ['FINDINGS.md','IMPLEMENTATION_PLAN.md','REPRODUCTION.md']:
 for v in re.findall(r'\]\(([^)]+)\)',(root/name).read_text()):
  links+=1
  if not v.startswith(('http','#')) and not (root/v).exists():errors.append('Missing MD link '+v)
manifest=json.loads((root/'evidence-manifest.json').read_text());images=0
for row in manifest['files']:
 file=root/row['path']
 if hashlib.sha256(file.read_bytes()).hexdigest()!=row['sha256']:errors.append('Hash mismatch '+row['path'])
 if file.suffix=='.png':
  with Image.open(file) as im:im.verify()
  images+=1
for receipt in list(root.glob('source-previews/*/run-*/receipt.json'))+list(root.glob('source-runway-previews/*/run-*/receipt.json')):
 r=json.loads(receipt.read_text());assert r['sourceHashBefore']==r['sourceHashAfter'],receipt
assert not errors,errors
receipt={'result':'Passed','missions':25,'imageFilesVerified':images,'evidenceFilesVerified':len(manifest['files']),'localLinksVerified':links,'sourceHashChecks':'passed','normalInputPlaythroughs':0,'deviceAcceptance':'pending','scope':'Report integrity and evidence files only'}
(root/'validation/report-integrity.json').write_text(json.dumps(receipt,indent=2))
print('[CampaignVisualReportIntegrity] result=Passed '+json.dumps(receipt))
