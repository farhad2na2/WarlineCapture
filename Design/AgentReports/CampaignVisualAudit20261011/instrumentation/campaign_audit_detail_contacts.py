import json,math
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path('/Volumes/T7/Projects/WarlineCapture-CampaignVisualAudit/Design/AgentReports/CampaignVisualAudit20261011')
font=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf',18)
for row in json.loads((root/'inventory.json').read_text())['missions']:
 idx=row['index']+1
 groups=[]
 source=root/'source-previews'/row['missionId']/'run-01'
 if (source/'receipt.json').exists():groups.append(('source', sorted(source.glob('*.png'))))
 runway=root/'source-runway-previews'/row['missionId']/'run-01'
 if (runway/'receipt.json').exists():groups.append(('runway-source', sorted(runway.glob('*-runway-*-end-*.png'))))
 supp=root/'captures'/row['missionId']/'run-02'
 if (supp/'receipt.json').exists():groups.append(('details', [p for p in sorted(supp.glob('*.png')) if p.stem not in ['opening','opening-hud','objective-context','objective-detail','mission-overview']]))
 for label,files in groups:
  for offset in range(0,len(files),4):
   name=root/'contact-sheets'/f'{idx:02d}-{label}-{offset//4+1}.jpg'
   if name.exists():continue
   subset=files[offset:offset+4];canvas=Image.new('RGB',(1600,540*math.ceil(len(subset)/2)),'#152028');draw=ImageDraw.Draw(canvas)
   for i,file in enumerate(subset):
    x=(i%2)*800;y=(i//2)*540;draw.text((x+10,y+8),f'{idx:02d} {label}: {file.stem.replace("CH05M", "M")}',font=font,fill='white')
    im=Image.open(file).convert('RGB');im.thumbnail((800,500));canvas.paste(im,(x,y+40))
   canvas.save(name,quality=92);print(name.name)
