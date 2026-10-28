import csv,json,statistics,pathlib
out=pathlib.Path('Docs/validation/artifacts/gameplay-rework/c01-2c')
rows=list(csv.DictReader((out/'nexus-runtime.csv').open()))
result={'samples':len(rows),'source':'Editor real; Keyboard/Mouse/focus virtuais; não é CPU Profiler nem build','stages':{}}
for stage in sorted(set(r['stage'] for r in rows),key=int):
 r=[x for x in rows if x['stage']==stage];dt=sorted(float(x['dt']) for x in r)
 pos=lambda x:[float(x[k]) for k in ('x','y','z')]
 result['stages'][stage]={'samples':len(r),'start':pos(r[0]),'end':pos(r[-1]),'medianActualSpeed':statistics.median(float(x['actualSpeed']) for x in r),'maxActualSpeed':max(float(x['actualSpeed']) for x in r),'medianFrameMs':statistics.median(dt)*1000,'p95FrameMs':dt[int((len(dt)-1)*.95)]*1000,'maxFrameMs':max(dt)*1000,'abilityUses':int(r[-1]['abilityUses'])}
(out/'nexus-measurements.json').write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding='utf-8');print(json.dumps(result,ensure_ascii=False))
