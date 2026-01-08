---
name: Unity AutoGen Quick Templates
description: 常用 Unity 操作的 Job 模板快速生成。当用户请求创建 Prefab、SO、场景对象等时，直接使用这些模板。
version: "1.0"
triggers:
  - 创建预制体
  - 创建配置
  - 场景布置
  - 批量生成
---

# Unity AutoGen 快速模板

这个 Skill 提供常见 Unity 操作的 Job 模板，可直接复制修改使用。

## 模板索引

1. [创建空 GameObject](#1-创建空-gameobject)
2. [创建带 Sprite 的对象](#2-创建带-sprite-的对象)
3. [创建 ScriptableObject 配置](#3-创建-scriptableobject-配置)
4. [创建 UI 元素](#4-创建-ui-元素)
5. [创建简单 Prefab](#5-创建简单-prefab)
6. [创建带组件的 Prefab](#6-创建带组件的-prefab)
7. [实例化 Prefab 到场景](#7-实例化-prefab-到场景)
8. [批量创建对象](#8-批量创建对象)
9. [完整工作流：配置 + Prefab + 场景](#9-完整工作流配置--prefab--场景)

---

## 1. 创建空 GameObject

```json
{
  "schemaVersion": 1,
  "jobId": "create_empty_go_{{TIMESTAMP}}",
  "projectWriteRoot": "Assets/AutoGen",
  "commands": [
    {
      "cmd": "CreateGameObject",
      "args": {
        "name": "{{NAME}}",
        "position": [0, 0, 0],
        "ensure": true
      },
      "out": { "go": "$newObject" }
    }
  ]
}
```

**替换：** `{{NAME}}` = 对象名称, `{{TIMESTAMP}}` = 时间戳

---

## 2. 创建带 Sprite 的对象

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

**替换：** 
- `{{NAME}}` = 对象名称
- `{{X}}, {{Y}}` = 位置
- `{{SPRITE_PATH}}` = Sprite 资产路径，如 `Assets/Sprites/Icon.png`
- `{{R}}, {{G}}, {{B}}` = 颜色值 (0-1)

---

## 3. 创建 ScriptableObject 配置

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
    {
      "cmd": "SaveAssets",
      "args": { "refresh": true }
    }
  ]
}
```

**替换：**
- `{{SO_TYPE}}` = SO 类型全名，如 `MyNamespace.EnemyConfig`
- `{{CONFIG_NAME}}` = 配置文件名
- `{{INIT_FIELDS}}` = 初始化字段，如 `"health": 100, "speed": 5.0`

---

## 4. 创建 UI 元素

```json
{
  "schemaVersion": 1,
  "jobId": "create_ui_{{TIMESTAMP}}",
  "projectWriteRoot": "Assets/AutoGen",
  "commands": [
    {
      "cmd": "CreateGameObject",
      "args": {
        "name": "{{UI_NAME}}",
        "parentPath": "Canvas",
        "ensure": true
      },
      "out": { "go": "$uiElement" }
    },
    {
      "cmd": "AddComponent",
      "args": {
        "target": { "ref": "$uiElement" },
        "type": "UnityEngine.UI.Image"
      },
      "out": { "component": "$image" }
    },
    {
      "cmd": "AddComponent",
      "args": {
        "target": { "ref": "$uiElement" },
        "type": "UnityEngine.RectTransform"
      }
    },
    {
      "cmd": "SetSerializedProperty",
      "args": {
        "target": { "ref": "$image" },
        "propertyPath": "m_Color",
        "value": [{{R}}, {{G}}, {{B}}, {{A}}]
      }
    }
  ]
}
```

---

## 5. 创建简单 Prefab

```json
{
  "schemaVersion": 1,
  "jobId": "create_prefab_{{TIMESTAMP}}",
  "projectWriteRoot": "Assets/AutoGen",
  "commands": [
    {
      "cmd": "CreateOrEditPrefab",
      "args": {
        "prefabPath": "Assets/AutoGen/Prefabs/{{PREFAB_NAME}}.prefab",
        "rootName": "{{PREFAB_NAME}}",
        "edits": []
      },
      "out": { "prefab": "$prefab" }
    },
    {
      "cmd": "SaveAssets",
      "args": { "refresh": true }
    }
  ]
}
```

---

## 6. 创建带组件的 Prefab

```json
{
  "schemaVersion": 1,
  "jobId": "create_component_prefab_{{TIMESTAMP}}",
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
            },
            "out": { "component": "$sr" }
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
    {
      "cmd": "SaveAssets",
      "args": { "refresh": true }
    }
  ]
}
```

---

## 7. 实例化 Prefab 到场景

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

**替换：**
- `{{PREFAB_PATH}}` = Prefab 资产路径
- `{{INSTANCE_NAME}}` = 实例名称
- `{{PARENT_PATH}}` = 父对象路径，空则为根
- `{{UNIQUE_TAG}}` = 唯一标识，用于 ensure 模式

---

## 8. 批量创建对象

```json
{
  "schemaVersion": 1,
  "jobId": "batch_create_{{TIMESTAMP}}",
  "projectWriteRoot": "Assets/AutoGen",
  "commands": [
    {
      "cmd": "CreateGameObject",
      "args": { "name": "Item_01", "position": [0, 0, 0], "ensure": true }
    },
    {
      "cmd": "CreateGameObject",
      "args": { "name": "Item_02", "position": [2, 0, 0], "ensure": true }
    },
    {
      "cmd": "CreateGameObject",
      "args": { "name": "Item_03", "position": [4, 0, 0], "ensure": true }
    },
    {
      "cmd": "CreateGameObject",
      "args": { "name": "Item_04", "position": [6, 0, 0], "ensure": true }
    }
  ]
}
```

---

## 9. 完整工作流：配置 + Prefab + 场景

这是一个完整的工作流示例，展示如何：
1. 创建配置 SO
2. 创建 Prefab 并引用配置
3. 在场景中实例化

```json
{
  "schemaVersion": 1,
  "jobId": "full_workflow_{{ENTITY_NAME}}_{{TIMESTAMP}}",
  "projectWriteRoot": "Assets/AutoGen",
  "requiresTypes": ["{{CONFIG_TYPE}}", "{{CONTROLLER_TYPE}}"],
  "commands": [
    {
      "cmd": "CreateScriptableObject",
      "args": {
        "type": "{{CONFIG_TYPE}}",
        "assetPath": "Assets/AutoGen/Configs/{{ENTITY_NAME}}Config.asset",
        "init": {
          {{CONFIG_INIT}}
        }
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
              "type": "UnityEngine.SpriteRenderer"
            }
          },
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
      },
      "out": { "instance": "$instance" }
    },
    {
      "cmd": "SaveAssets",
      "args": { "refresh": true }
    }
  ]
}
```

---

## 使用说明

1. 选择合适的模板
2. 替换所有 `{{PLACEHOLDER}}` 为实际值
3. 生成唯一的 `jobId`（推荐使用时间戳）
4. 写入 `AutoGenJobs/inbox/` 目录
5. 检查 `AutoGenJobs/results/` 确认成功

## 常用路径模式

| 用途 | 路径格式 |
|------|---------|
| Prefab | `Assets/AutoGen/Prefabs/Name.prefab` |
| SO 配置 | `Assets/AutoGen/Configs/Name.asset` |
| 场景父对象 | `ParentName` 或 `Parent/Child` |
| Sprite | `Assets/Sprites/Name.png` |

## 时间戳生成

```python
from datetime import datetime
timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
# 输出: 20260107_182000
```
