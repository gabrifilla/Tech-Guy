"""Drive existing MCP UTF jobs sequentially; preserve selections, failures and job scope."""
from pathlib import Path
import json,sys,time

ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Docs/validation/artifacts/run-exp-06'
sys.path.insert(0,str(ROOT/'Tools/GameplayObservation'))
from observe_gameplay import UnityMcp

def call(name,args):
    client=UnityMcp(timeout=55);client.connect();client.select_project(ROOT)
    result=client.tool(name,args)
    if isinstance(result,dict) and result.get('success') is False:raise RuntimeError(result)
    return result
def write(name,value):(OUT/name).write_text(json.dumps(value,indent=2,ensure_ascii=False),encoding='utf-8')
def scope(which,label):
    code=(ROOT/('Tools/RunExp06/Source/'+which+'-currency-scope.cs.txt')).read_text(encoding='utf-8-sig')
    result=call('execute_code',{'action':'execute','compiler':'codedom','code':code});write('rerun-'+label+'-currency-'+which+'.json',result)

selections=list(json.loads((OUT/'validation-selections-complete.json').read_text(encoding='utf-8')).items())
write('rerun-selections.json',dict(selections))
for label,args in selections:
    protected=label in ('regression','skill-effects','extra-combo-camera')
    if protected:scope('begin',label)
    try:
        result=call('run_tests',args);write('rerun-'+label+'-start.json',result);job=result['data']['job_id'];print(label,job,flush=True)
        deadline=time.monotonic()+900
        while True:
            result=call('get_test_job',{'job_id':job,'wait_timeout':10,'include_details':True,'include_failed_tests':True})
            write('rerun-'+label+'.json',result);data=result.get('data',result)
            if data['status'] in ('succeeded','failed','cancelled'):
                print(label,data['status'],data.get('result',{}).get('summary') if data.get('result') else data.get('error'),flush=True)
                if data['status']!='succeeded':raise RuntimeError('Inspect preserved failing job '+label)
                break
            if time.monotonic()>deadline:raise TimeoutError('Job still active; inspect it before another run')
            time.sleep(2)
    finally:
        if protected:scope('end',label)
print('Selected validation sequence completed; not global suite.',flush=True)
