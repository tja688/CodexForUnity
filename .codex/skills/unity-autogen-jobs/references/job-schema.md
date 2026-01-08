# Job Schema

## Top-Level Structure

Required fields:

- `schemaVersion` (number)
- `jobId` (string, unique)
- `projectWriteRoot` (string, must be `Assets/AutoGen`)
- `commands` (array)

Optional fields:

- `createdAtUtc` (ISO-8601 string)
- `runnerMinVersion` (number)
- `requiresTypes` (array of fully qualified C# type names)
- `dryRun` (bool)

```json
{
  "schemaVersion": 1,
  "jobId": "job_20260107_182000_abcd1234",
  "createdAtUtc": "2026-01-07T10:00:00Z",
  "runnerMinVersion": 3,
  "projectWriteRoot": "Assets/AutoGen",
  "requiresTypes": ["MyNamespace.MyComponent"],
  "dryRun": false,
  "commands": []
}
```

## Command Output and References

Use `out` to capture handles, then reference them with `{"ref": "$var"}`.

```json
{
  "cmd": "CreateGameObject",
  "args": { "name": "Root", "ensure": true },
  "out": { "go": "$rootGo" }
}
```

Reference formats:

```json
{ "ref": "$rootGo" }
{ "scenePath": "Canvas/Panel/Button" }
{ "assetPath": "Assets/AutoGen/Prefabs/Enemy.prefab" }
{ "assetGuid": "a1b2c3d4e5f6..." }
```

## Value Formats

- Numbers: `123`, `1.5`
- Bool: `true`, `false`
- String: `"hello"`
- Vector2/3/4: `[x, y]`, `[x, y, z]`
- Color: `[r, g, b, a]`
- Enum: `{"enum": "EnumType", "name": "ValueName"}`
- Asset ref: `{"assetPath": "Assets/..."}`
- Variable ref: `{"ref": "$var"}`
- Null: `{"null": true}`
- Arrays: `[element1, element2]`

## Command Reference (Core Set)

### CreateGameObject

```json
{
  "cmd": "CreateGameObject",
  "args": {
    "name": "MyObject",
    "parentPath": "Canvas/Panel",
    "position": [0, 0, 0],
    "rotation": [0, 0, 0],
    "scale": [1, 1, 1],
    "ensure": true,
    "ensureTag": "unique_marker"
  },
  "out": { "go": "$go" }
}
```

### AddComponent

```json
{
  "cmd": "AddComponent",
  "args": {
    "target": { "ref": "$go" },
    "type": "UnityEngine.SpriteRenderer",
    "ifMissing": true
  },
  "out": { "component": "$component" }
}
```

### SetSerializedProperty

```json
{
  "cmd": "SetSerializedProperty",
  "args": {
    "target": { "ref": "$component" },
    "propertyPath": "m_Color",
    "value": [1, 0.5, 0, 1]
  }
}
```

### CreateScriptableObject

```json
{
  "cmd": "CreateScriptableObject",
  "args": {
    "type": "MyNamespace.MyConfig",
    "assetPath": "Assets/AutoGen/Configs/MyConfig.asset",
    "overwrite": false,
    "init": {
      "health": 100,
      "speed": 5.0
    }
  },
  "out": { "asset": "$config" }
}
```

### CreateOrEditPrefab

```json
{
  "cmd": "CreateOrEditPrefab",
  "args": {
    "prefabPath": "Assets/AutoGen/Prefabs/Enemy.prefab",
    "rootName": "Enemy",
    "edits": [
      {
        "cmd": "AddComponent",
        "args": {
          "target": { "ref": "$prefabRoot" },
          "type": "UnityEngine.SpriteRenderer"
        },
        "out": { "component": "$sr" }
      }
    ]
  },
  "out": { "prefab": "$prefab" }
}
```

Rules for prefab edits:

- `$prefabRoot` is provided inside `edits`.
- Do not call `InstantiatePrefabInScene` inside `edits`.

### InstantiatePrefabInScene

```json
{
  "cmd": "InstantiatePrefabInScene",
  "args": {
    "prefabPath": "Assets/AutoGen/Prefabs/Enemy.prefab",
    "nameOverride": "Enemy_001",
    "parentPath": "Enemies",
    "position": [5, 0, 0],
    "ensure": true,
    "ensureTag": "enemy_001"
  },
  "out": { "instance": "$instance" }
}
```

### SetTransform

```json
{
  "cmd": "SetTransform",
  "args": {
    "target": { "ref": "$instance" },
    "position": [0, 1, 0],
    "rotation": [0, 180, 0],
    "scale": [1, 1, 1],
    "space": "local"
  }
}
```

### SaveAssets

```json
{ "cmd": "SaveAssets", "args": { "refresh": true } }
```

### ImportAssets

```json
{
  "cmd": "ImportAssets",
  "args": {
    "paths": ["Assets/AutoGen/Textures/new.png"],
    "force": false
  }
}
```
