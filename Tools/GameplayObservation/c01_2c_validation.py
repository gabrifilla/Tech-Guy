"""Run one filtered Unity MCP test job and retain its exact request/result (no automatic retries)."""
import json, pathlib, sys, time
from c01_2b_mcp import rpc, SESSION
session = json.loads(SESSION.read_text())["session"]
request = json.loads(pathlib.Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
output = pathlib.Path(sys.argv[2])
output.with_suffix(".request.json").write_text(json.dumps(request, indent=2), encoding="utf-8")
def payload(raw):
    value = raw["result"]["structuredContent"]
    return value.get("result", value)
raw, _ = rpc("tools/call", {"name":"run_tests", "arguments":request}, session=session)
output.with_suffix(".start.json").write_text(json.dumps(raw), encoding="utf-8")
started = payload(raw)
if not started.get("success"): raise RuntimeError(started)
job = started["data"]["job_id"]
print("job="+job, flush=True)
deadline=time.monotonic()+600
while time.monotonic()<deadline:
    raw, _ = rpc("tools/call", {"name":"get_test_job", "arguments":{"job_id":job,"wait_timeout":30,"include_details":True}}, session=session)
    response=payload(raw)
    if not response.get("success"): raise RuntimeError(response)
    data=response["data"]
    if data["status"] not in ("running","queued"):
        output.write_text(json.dumps(raw, ensure_ascii=False), encoding="utf-8")
        print(json.dumps({"job":job,"status":data["status"],"summary":data.get("result",{}).get("summary"),"failures":data.get("progress",{}).get("failures_so_far")}),flush=True)
        sys.exit(0 if data["status"]=="succeeded" else 1)
    time.sleep(2)
raise TimeoutError(job)
