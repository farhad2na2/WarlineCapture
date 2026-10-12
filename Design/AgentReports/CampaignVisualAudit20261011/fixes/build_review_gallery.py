from pathlib import Path
import json, html, re
root=Path(__file__).resolve().parent.parent
findings=json.loads((root/"findings.json").read_text())
rows=[];cards=[]
notes_path=root/'fixes/visual-review-notes.json'
notes=json.loads(notes_path.read_text()) if notes_path.exists() else {}
def image(path,label):
    if not (root/path).exists():return '<p class="pending">'+html.escape(label)+': no completed image</p>'
    url='../'+path
    return '<figure><a href="'+html.escape(url)+'"><img loading="lazy" src="'+html.escape(url)+'"></a><figcaption>'+html.escape(label)+'</figcaption></figure>'
for m in findings['missions']:
    candidates=list((root/'captures'/m['missionId']).glob('run-visual-fix-*/receipt.json'))
    capture=max(candidates,key=lambda p:p.stat().st_mtime).parent if candidates else root/'captures'/m['missionId']/'run-visual-fix-01'
    receipt=capture/'receipt.json';d=json.loads(receipt.read_text()) if receipt.exists() else {}
    failures=[r for r in d.get('spawnRequests',[]) if 'status=2 ' in r]
    required=[r for r in failures if 'authoredStartingGrant=1 ' in r or 'requirePreferred=1 ' in r]
    status={'index':m['index'],'missionId':m['missionId'],'title':m['title'],'captureResult':d.get('result','pending'),'outcome':d.get('missionOutcome'),'phase':d.get('missionPhase'),'cameraSettled':d.get('cameraSettled'),'failedRequests':failures,'failedRequiredRequests':required,'receipt':str(receipt.relative_to(root)) if receipt.exists() else None,'visualReview':'pending','normalInputAcceptance':'pending','deviceAcceptance':'pending'}
    status['visualReview']=notes.get(status['receipt'],{'status':'pending'})
    rows.append(status)
    before=next((x for x in m.get('images',[]) if x.endswith('/opening-hud.png')),'missing')
    after=str((capture/'opening-hud.png').relative_to(root))
    text='Capture: '+status['captureResult']+'; phase: '+str(status['phase'])+'; outcome: '+str(status['outcome'])+'; settled: '+str(status['cameraSettled'])
    links=''.join('<a href="../'+html.escape(str(x.relative_to(root)))+'">'+html.escape(x.stem)+'</a> ' for x in sorted(capture.glob('*.png')) if x.name!='opening-hud.png')
    cards.append('<section><h2>'+str(m['index']+1).zfill(2)+'. '+html.escape(m['title'])+'</h2><p>'+html.escape(text)+'</p><div class="pair">'+image(before,'Audit baseline')+image(after,'Visual fixes — native HUD')+'</div><details><summary>Other captured views</summary>'+links+'</details></section>')
summary={'captureComplete':sum(x['captureResult']=='Passed' for x in rows),'total':25,'missions':rows,'note':'Capture success is not a mission win, visual approval, normal-input playthrough or device acceptance.'}
(root/'fixes/native-review.json').write_text(json.dumps(summary,indent=2)+'\n')
head='<!doctype html><html><head><meta charset="utf-8"><title>Campaign visual fixes — native review</title><style>body{font:16px system-ui;margin:32px;background:#151a20;color:#e8e9ea}h1,h2{font-weight:600}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0}img{width:100%;display:block}figcaption,.pending{color:#bbc4ce;padding:8px}section{padding:20px 0;border-bottom:1px solid #445}a{color:#94c8ff}details a{display:inline-block;margin:6px}p{line-height:1.5}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style></head><body><h1>Campaign visual fixes</h1><p>Fresh native opening captures, compared with the original audit. Images use PC/Metal at 1600 × 900. Programmatic deployment and narrative Skip use an isolated profile; these captures do not establish normal-input mission or Android acceptance.</p>'
(root/'fixes/gallery.html').write_text(head+''.join(cards)+'</body></html>')
print(json.dumps({'captures':summary['captureComplete'],'total':25,'failedRequiredRequests':sum(len(x['failedRequiredRequests']) for x in rows)}))
