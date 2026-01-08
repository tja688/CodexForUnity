---
name: unity-autogen-jobs
description: Automate Unity Editor asset generation, prefab editing, and scene placement by submitting AutoGenJobs JSON job files and checking results in this repo. Use when tasks involve CreateGameObject, CreateOrEditPrefab, CreateScriptableObject, AddComponent, SetSerializedProperty, InstantiatePrefabInScene, or when the user asks to submit/check Unity AutoGen jobs. Do not use for manual Unity editor steps or non-AutoGen workflows.
---

# Unity AutoGen Jobs

## Workflow

1. Verify Unity Editor is open and the AutoGenJobs runner is enabled.
2. Prefer the Python SDK at `AutoGenJobs/autogen_unity.py` to build and submit jobs.
3. When asked for raw job JSON, follow the schema and atomic submit protocol.
4. Always write assets under `Assets/AutoGen` and use unique `jobId` values.
5. Check `AutoGenJobs/results/<jobId>.result.json` for status and errors.

## Resources

- `references/job-schema.md` for job file structure, command list, and value formats.
- `references/job-templates.md` for common prefab/scene/config templates.
- `references/execution-and-diagnostics.md` for submission, polling, and troubleshooting steps.

## Guardrails

- Use `.job.json.pending` then rename to `.job.json` for atomic submits.
- Include `requiresTypes` when using custom C# component types.
- Use `SetSerializedProperty` for most field edits; fall back to custom IJobCommand only when needed.
