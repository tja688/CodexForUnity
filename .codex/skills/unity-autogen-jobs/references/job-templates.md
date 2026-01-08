# Job Templates

Replace `{{PLACEHOLDER}}` values before submitting.

## 1) Create Empty GameObject

```json
{
  "schemaVersion": 1,
  "jobId": "create_empty_{{TIMESTAMP}}",
  "projectWriteRoot": "Assets/AutoGen",
  "commands": [
    {
      "cmd": "CreateGameObject",
      "args": {
        "name": "{{NAME}}",
        "position": [0, 0, 0],
        "ensure": true
      },
      "out": { "go": "$go" }
    }
  ]
}
```

## 2) Create Sprite Object

```json
{
  "schemaVersion": 1,
  "jobId": "create_sprite_{{TIMESTAMP}}",
  "projectWriteRoot": "Assets/AutoGen",
  "commands": [
    {
      "cmd": "CreateGameObject",
      "args": {
        "name": "{{NAME}}",
        "position": [{{X}}, {{Y}}, 0],
        "ensure": true
      },
      "out": { "go": "$spriteObj" }
    },
    {
      "cmd": "AddComponent",
      "args": {
        "target": { "ref": "$spriteObj" },
        "type": "UnityEngine.SpriteRenderer"
      },
      "out": { "component": "$sr" }
    },
    {
      "cmd": "SetSerializedProperty",
      "args": {
        "target": { "ref": "$sr" },
        "propertyPath": "m_Sprite",
        "value": { "assetPath": "{{SPRITE_PATH}}" }
      }
    },
    {
      "cmd": "SetSerializedProperty",
      "args": {
        "target": { "ref": "$sr" },
        "propertyPath": "m_Color",
        "value": [{{R}}, {{G}}, {{B}}, 1]
      }
    }
  ]
}
```

## 3) Create ScriptableObject Config

```json
{
  "schemaVersion": 1,
  "jobId": "create_config_{{TIMESTAMP}}",
  "projectWriteRoot": "Assets/AutoGen",
  "requiresTypes": ["{{SO_TYPE}}"],
  "commands": [
    {
      "cmd": "CreateScriptableObject",
      "args": {
        "type": "{{SO_TYPE}}",
        "assetPath": "Assets/AutoGen/Configs/{{CONFIG_NAME}}.asset",
        "overwrite": false,
        "init": {
          {{INIT_FIELDS}}
        }
      },
      "out": { "asset": "$config" }
    },
    { "cmd": "SaveAssets", "args": { "refresh": true } }
  ]
}
```

## 4) Create Prefab With Components

```json
{
  "schemaVersion": 1,
  "jobId": "create_prefab_{{TIMESTAMP}}",
  "projectWriteRoot": "Assets/AutoGen",
  "requiresTypes": ["{{COMPONENT_TYPE}}"],
  "commands": [
    {
      "cmd": "CreateOrEditPrefab",
      "args": {
        "prefabPath": "Assets/AutoGen/Prefabs/{{PREFAB_NAME}}.prefab",
        "rootName": "{{PREFAB_NAME}}",
        "edits": [
          {
            "cmd": "AddComponent",
            "args": {
              "target": { "ref": "$prefabRoot" },
              "type": "UnityEngine.SpriteRenderer"
            }
          },
          {
            "cmd": "AddComponent",
            "args": {
              "target": { "ref": "$prefabRoot" },
              "type": "{{COMPONENT_TYPE}}"
            },
            "out": { "component": "$mainComponent" }
          },
          {
            "cmd": "SetSerializedProperty",
            "args": {
              "target": { "ref": "$mainComponent" },
              "propertyPath": "{{PROPERTY_NAME}}",
              "value": {{PROPERTY_VALUE}}
            }
          }
        ]
      },
      "out": { "prefab": "$prefab" }
    },
    { "cmd": "SaveAssets", "args": { "refresh": true } }
  ]
}
```

## 5) Instantiate Prefab in Scene

```json
{
  "schemaVersion": 1,
  "jobId": "instantiate_{{TIMESTAMP}}",
  "projectWriteRoot": "Assets/AutoGen",
  "commands": [
    {
      "cmd": "InstantiatePrefabInScene",
      "args": {
        "prefabPath": "{{PREFAB_PATH}}",
        "nameOverride": "{{INSTANCE_NAME}}",
        "parentPath": "{{PARENT_PATH}}",
        "position": [{{X}}, {{Y}}, {{Z}}],
        "ensure": true,
        "ensureTag": "{{UNIQUE_TAG}}"
      },
      "out": { "instance": "$instance" }
    }
  ]
}
```

## 6) Full Flow: Config + Prefab + Scene

```json
{
  "schemaVersion": 1,
  "jobId": "full_flow_{{ENTITY_NAME}}_{{TIMESTAMP}}",
  "projectWriteRoot": "Assets/AutoGen",
  "requiresTypes": ["{{CONFIG_TYPE}}", "{{CONTROLLER_TYPE}}"],
  "commands": [
    {
      "cmd": "CreateScriptableObject",
      "args": {
        "type": "{{CONFIG_TYPE}}",
        "assetPath": "Assets/AutoGen/Configs/{{ENTITY_NAME}}Config.asset",
        "init": { {{CONFIG_INIT}} }
      },
      "out": { "asset": "$config" }
    },
    {
      "cmd": "CreateOrEditPrefab",
      "args": {
        "prefabPath": "Assets/AutoGen/Prefabs/{{ENTITY_NAME}}.prefab",
        "rootName": "{{ENTITY_NAME}}",
        "edits": [
          {
            "cmd": "AddComponent",
            "args": {
              "target": { "ref": "$prefabRoot" },
              "type": "{{CONTROLLER_TYPE}}"
            },
            "out": { "component": "$controller" }
          },
          {
            "cmd": "SetSerializedProperty",
            "args": {
              "target": { "ref": "$controller" },
              "propertyPath": "config",
              "value": { "ref": "$config" }
            }
          }
        ]
      },
      "out": { "prefab": "$prefab" }
    },
    {
      "cmd": "InstantiatePrefabInScene",
      "args": {
        "prefabPath": "Assets/AutoGen/Prefabs/{{ENTITY_NAME}}.prefab",
        "parentPath": "{{PARENT_PATH}}",
        "position": [{{X}}, {{Y}}, 0],
        "ensure": true,
        "ensureTag": "{{ENTITY_NAME}}_instance"
      }
    },
    { "cmd": "SaveAssets", "args": { "refresh": true } }
  ]
}
```
