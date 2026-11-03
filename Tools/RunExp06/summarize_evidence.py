"""Read current06 evidence and hashes. Never restore preferences or overwrite the baseline."""
from pathlib import Path
import base64, hashlib, json, shutil, subprocess, winreg
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/validation/artifacts/run-exp-06'

def write(name, value):
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def stats(values):
    values = sorted(values)
    return {name: values[round((len(values)-1)*p)] if values else None for name, p in [('p50', .5), ('p95', .95)]}

baseline = json.loads((OUT / 'baseline-files-prefs.json').read_text(encoding='utf-8'))
moves = json.loads((OUT / 'presentation-moves.json').read_text(encoding='utf-8'))
mapped = {m['from']+suffix: m['to']+suffix for m in moves for suffix in ('', '.meta')}
changed, missing, same, moved = [], [], [], []
for name, previous in baseline['sha256'].items():
    actual = mapped.get(name, name)
    path = ROOT / actual
    if name != actual:
        moved.append({'from': name, 'to': actual, 'unchangedBytes': path.exists() and sha(path) == previous})
    if not path.exists():
        missing.append({'baseline': name, 'actual': actual})
    elif sha(path) != previous:
        changed.append(actual)
    else:
        same.append(actual)

preferences, differences = [], []
for previous in baseline['prefs']:
    values = []
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER, previous['path']) as key:
        for i in range(winreg.QueryInfoKey(key)[1]):
            name, value, kind = winreg.EnumValue(key, i)
            encoded = base64.b64encode(value).decode('ascii') if isinstance(value, bytes) else str(value)
            values.append({'name': name, 'kind': kind, 'hash': hashlib.sha256(encoded.encode()).hexdigest()})
    preferences.append({'path': previous['path'], 'values': values})
    old = {v['name']: v for v in previous['values']}
    new = {v['name']: v for v in values}
    for name in sorted(old.keys() | new.keys()):
        if old.get(name) != new.get(name):
            differences.append({'path': previous['path'], 'name': name, 'gameplay': name.startswith('TechGuy'), 'change': 'added' if name not in old else 'removed' if name not in new else 'modified'})

git = lambda *args: subprocess.check_output(['git', *args], cwd=ROOT, text=True, encoding='utf-8').strip()
write('integrity-final.json', {
    'baselineFiles': len(baseline['sha256']), 'unchangedFiles': len(same), 'changedFiles': changed,
    'missingFiles': missing, 'moves': moved, 'headBeforeDeliveryCommit': git('rev-parse', 'HEAD'),
    'baselineHead': baseline['head'], 'branch': git('branch', '--show-current'),
    'prefsHashed': preferences, 'preferenceChanges': differences,
    'gameplayPreferencesCompared': sum(v['name'].startswith('TechGuy') for p in baseline['prefs'] for v in p['values']),
    'baselineGameplayPreferencesChanged': sum(d['gameplay'] and d['change'] != 'added' for d in differences),
    'addedGameplayPreferences': sum(d['gameplay'] and d['change'] == 'added' for d in differences),
    'limit': 'Current06 entry baseline, including all47 legitimate existing gameplay values. Hash comparison only in the two listed registry scopes and2739 files. No historical save restore or universal disk guarantee. Moves preserve identity; docs and explicitly listed UI/fixture/reference changes are reviewed separately.'})

performance = {'editor': [], 'standalone': [], 'limits': 'Editor UTF/LINQ/IMGUI sampler overhead; ongoing AI phases/positions differ. Standalone reuses unchanged05 art-comparison harness with current06 runtime. Art04/05 labels are NOT old executable versions. No optimization or global hardware-budget claim; reward-modal GPU/GC and individual VFX costs not isolated.'}
for version, folder in [('baseline06', OUT/'baseline/firstsector'), ('current06', ROOT/'Docs/validation/artifacts/run-exp-03/firstsector')]:
    for count in (8, 32, 64):
        path = sorted(folder.glob('*/integrated-active-'+str(count)+'.json'))[-1]
        item = json.loads(path.read_text(encoding='utf-8'))
        rows = item.pop('samples')
        shutil.copy2(path, OUT/('performance-'+version+'-'+str(count)+'.json'))
        markers = {key: {**stats([r['markers'][i] for r in rows]), 'unit': 'bytes' if key == 'GC Allocated In Frame' else 'count' if key in ('Draw Calls Count', 'Batches Count') else 'nanoseconds'} for i, key in enumerate(item['markerNames'])}
        performance['editor'].append({**item, 'version': version, 'source': path.relative_to(ROOT).as_posix(), 'frames': len(rows), 'frameMs': stats([r['dtMs'] for r in rows]), 'activeMin': min(r['active'] for r in rows), 'movingMax': max(r['moving'] for r in rows), 'windupsMax': max(r['windups'] for r in rows), 'projectilesMax': max(r['projectiles'] for r in rows), 'drawCallsEditor': stats([r['drawCalls'] for r in rows]), 'markers': markers})
for path in sorted((ROOT/'Builds/RunExp06Profile/ProfileResults').glob('*.json')):
    item = json.loads(path.read_text(encoding='utf-8'))
    rows = item.pop('samples')
    shutil.copy2(path, OUT/path.name)
    gpu = [r['gpuMs'] for r in rows if r['gpuMs'] > 0]
    markers = {key: stats([r['markers'][i] for r in rows]) for i, key in enumerate(item['markerNames'])}
    performance['standalone'].append({**item, 'frames': len(rows), 'frameMs': stats([r['frameMs'] for r in rows]), 'gpuMs': stats(gpu), 'gpuValidFrames': len(gpu), 'allocatedMemoryBytes': stats([r['memory'] for r in rows]), 'activeMin': min(r['active'] for r in rows), 'movingMax': max(r['moving'] for r in rows), 'windupsMax': max(r['windups'] for r in rows), 'projectilesMax': max(r['projectiles'] for r in rows), 'markers': markers})
    capture = path.with_suffix('.png')
    if capture.exists(): shutil.copy2(capture, OUT/capture.name)
write('performance-summary.json', performance)

job = json.loads((OUT/'final-play.json').read_text(encoding='utf-8'))['data']
if job['status'] != 'succeeded': raise RuntimeError('Final integrated job not successful; do not label captures as final')
start, end = job['started_unix_ms']/1000, job['finished_unix_ms']/1000
index = []
for kind in ('firstsector', 'rewards'):
    for folder in sorted((ROOT/'Docs/validation/artifacts/run-exp-03'/kind).iterdir()):
        # Unity DateTime uses seven fractional digits; Python datetime stores six.
        try: instant = datetime.strptime(folder.name[:22], '%Y%m%d-%H%M%S-%f').replace(tzinfo=timezone.utc).timestamp()
        except ValueError: continue
        if not start <= instant <= end: continue
        for path in folder.iterdir():
            if not path.is_file(): continue
            target = OUT/'final'/kind/folder.name/path.name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, target)
            index.append({'source': path.relative_to(ROOT).as_posix(), 'copy': target.relative_to(ROOT).as_posix(), 'sha256': sha(path)})
if not index: raise RuntimeError('No captures matched the actual final job interval; inspect the UTC folder parser')
write('final-capture-index.json', {'jobId': job['job_id'], 'startedUnixMs': job['started_unix_ms'], 'finishedUnixMs': job['finished_unix_ms'], 'files': index})
print(json.dumps({'changedFiles': changed, 'missingFiles': missing, 'gameplayPreferenceChanges': [d for d in differences if d['gameplay']], 'editorSamples': len(performance['editor']), 'standaloneSamples': len(performance['standalone']), 'finalFilesCopied': len(index)}, ensure_ascii=False, indent=2))
