import csv,json,pathlib,statistics
out=pathlib.Path('Docs/validation/artifacts/gameplay-rework/c01-3a');rows=list(csv.DictReader((out/'nexus-input.csv').open()));start=rows[0]
f=lambda k:[float(r[k]) for r in rows]
payload={'samples':len(rows),'physicalHuman':False,'seconds':f('t')[-1],'basicCount':int(rows[-1]['basicCount']),'asuraUses':int(rows[-1]['asuraUses']),'xRange':[min(f('x')),max(f('x'))],'zRange':[min(f('z')),max(f('z'))],'frameMsMedian':statistics.median(f('dt'))*1000,'frameMsMax':max(f('dt'))*1000,'manaMin':min(f('mana')),'note':'Editor real com input/foco virtuais; energia100 em memória; regeneração de mana ativa; não é benchmark de CPU/build'}
(out/'nexus-measurements.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2),encoding='utf-8');print(payload)
