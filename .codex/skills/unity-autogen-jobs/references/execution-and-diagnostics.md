# Execution and Diagnostics

## Prefer the Python SDK

The repo includes `AutoGenJobs/autogen_unity.py` with a small client and helpers.
Import it by adding the folder to `sys.path` (there is no package init file).

```python
import sys
sys.path.append("AutoGenJobs")

from autogen_unity import UnityJobClient, Value

client = UnityJobClient(".")
cmds = [
    client.cmd_create_gameobject("MyObject", position=[0, 1, 0]),
    client.cmd_add_component("$go", "UnityEngine.SpriteRenderer"),
]
result = client.execute(cmds)
print(result)
```

## Raw JSON Submission (Atomic)

1. Write to `AutoGenJobs/inbox/<jobId>.job.json.pending`.
2. Rename to `AutoGenJobs/inbox/<jobId>.job.json`.

```python
import json
import os

job_id = "job_20260107_182000_abcd1234"
pending_path = f"AutoGenJobs/inbox/{job_id}.job.json.pending"
final_path = f"AutoGenJobs/inbox/{job_id}.job.json"

with open(pending_path, "w", encoding="utf-8") as f:
    json.dump(job_data, f, indent=2)

os.rename(pending_path, final_path)
```

## Result Checking

Results are written to:

`AutoGenJobs/results/<jobId>.result.json`

Status values: `DONE`, `FAILED`, `WAITING`, `TIMEOUT`.

```python
import json

with open(f"AutoGenJobs/results/{job_id}.result.json", "r", encoding="utf-8") as f:
    result = json.load(f)
print(result)
```

## Runner Health Checks

Verify directory structure exists:

- `AutoGenJobs/inbox`
- `AutoGenJobs/working`
- `AutoGenJobs/done`
- `AutoGenJobs/results`
- `AutoGenJobs/dead`

If jobs stall:

1. Confirm Unity Editor is open (not Play mode).
2. Check if Unity is compiling/importing assets.
3. Confirm the job file reached `AutoGenJobs/inbox`.
4. If in `dead`, read the result error message.

## Common Failure Reasons

- `Path not allowed`: asset path is outside `Assets/AutoGen`.
- `Type not found`: missing or misspelled C# type; ensure scripts compile.
- `Target not found`: `ref` or scene path does not resolve.
- `WAITING_COMPILING`: wait for Unity compile to finish.
