import json, re
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
p=Path('/Volumes/T7/Projects/WarlineCapture-CampaignVisualAudit')
root=p/'Design/AgentReports/CampaignVisualAudit20261011'
rows=json.loads((root/'inventory.json').read_text())['missions']
out=root/'contact-sheets'; out.mkdir(exist_ok=True)
font=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf',20)
for row in rows:
 candidates=[]
 for d in sorted((root/'captures'/row['missionId']).glob('run-*')):
  receipt=d/'receipt.json'
  if receipt.exists() and json.loads(receipt.read_text()).get('result')=='Passed' and all((d/f'{n}.png').exists() for n in ['opening-hud','objective-context','objective-detail','mission-overview']): candidates.append(d)
 if not candidates: continue
 d=candidates[-1]
 f=out/(f'{row["index"]+1:02d}.jpg' if d.name=='run-01' else f'{row["index"]+1:02d}-{d.name}.jpg')
 if f.exists(): continue
 canvas=Image.new('RGB',(1600,984),'#142025'); draw=ImageDraw.Draw(canvas)
 title=row['missionId'].split('.')[-1].replace('_',' ').title()
 for i,name in enumerate(['opening-hud','objective-context','objective-detail','mission-overview']):
  x=(i%2)*800; y=(i//2)*492
  draw.text((x+12,y+10),f'{row["index"]+1:02d} {title} / {name}',font=font,fill='white')
  image=Image.open(d/f'{name}.png').convert('RGB'); image.thumbnail((800,450))
  canvas.paste(image,(x,y+42))
 canvas.save(f,quality=92)
 print(f.name)
