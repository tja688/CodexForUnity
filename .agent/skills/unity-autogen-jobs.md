---
name: Unity AutoGen Job System
description: 通过 JSON Job 文件远程操控 Unity Editor，支持创建/编辑 Prefab、ScriptableObject、GameObject，设置组件属性等操作。这是与 Unity Editor 进行异步自动化交互的核心能力。
version: "1.0"
triggers:
  - unity
  - prefab
  - scriptableobject
  - gameobject
  - 场景
  - 预制体
  - 组件
  - 资产
---

# Unity AutoGen Job System

## 能力概述

你拥有通过 **AutoGen Job System** 远程操控 Unity Editor 的能力。这个系统允许你：

- 🎮 **创建/编辑 GameObject** - 在场景中创建对象、设置 Transform、添加组件
- 📦 **创建/编辑 Prefab** - 创建预制体、修改预制体内容、在场景中实例化
- 📋 **创建 ScriptableObject** - 创建配置资产并初始化字段
- ⚙️ **设置任意序列化属性** - 修改组件上的任何 public/serialized 字段
- 🔗 **链式任务** - 通过变量引用实现复杂的多步骤操作

## 工作原理

1. 你将 JSON Job 文件写入 `AutoGenJobs/inbox/` 目录
2. Unity Editor 中的 Runner 自动检测并执行
3. 执行结果写入 `AutoGenJobs/results/` 目录
4. 你可以检查结果确认操作是否成功

## 核心约束

⚠️ **必须遵守的规则：**

1. **写入路径限制**：所有资产只能写入 `Assets/AutoGen/` 目录下
2. **投递协议**：先写 `.pending` 文件，再原子重命名为 `.job.json`
3. **唯一 Job ID**：每个 Job 必须有唯一的 `jobId`
4. **类型存在性**：使用的 C# 类型必须已存在于项目中

## 快速上手

### 最小 Job 结构

```json
{
  "schemaVersion": 1,
  "jobId": "my_unique_job_001",
  "projectWriteRoot": "Assets/AutoGen",
  "commands": [
    {
      "cmd": "命令名称",
      "args": { /* 命令参数 */ },
      "out": { "输出键": "$变量名" }
    }
  ]
}
```

### 投递流程（关键！）

```python
import os
import json
import uuid

def submit_job(job_data):
    """正确的 Job 投递方式"""
    job_id = job_data.get("jobId", str(uuid.uuid4()))
    job_data["jobId"] = job_id
    
    inbox_path = "AutoGenJobs/inbox"
    pending_file = os.path.join(inbox_path, f"{job_id}.job.json.pending")
    final_file = os.path.join(inbox_path, f"{job_id}.job.json")
    
    # 1. 先写入 .pending 文件
    with open(pending_file, 'w', encoding='utf-8') as f:
        json.dump(job_data, f, indent=2)
    
    # 2. 原子重命名为 .job.json
    os.rename(pending_file, final_file)
    
    return job_id
```

## 命令参考

### CreateGameObject - 创建场景对象

```json
{
  "cmd": "CreateGameObject",
  "args": {
    "name": "MyObject",           // 必需：对象名称
    "parentPath": "Canvas/Panel", // 可选：父对象路径
    "position": [0, 5, 0],        // 可选：本地位置
    "rotation": [0, 45, 0],       // 可选：本地旋转（欧拉角）
    "scale": [1, 1, 1],           // 可选：本地缩放
    "ensure": true,               // 可选：幂等模式，已存在则复用
    "ensureTag": "unique_marker"  // 可选：唯一标记用于查找
  },
  "out": { "go": "$myGameObject" }
}
```

### AddComponent - 添加组件

```json
{
  "cmd": "AddComponent",
  "args": {
    "target": { "ref": "$myGameObject" },  // 目标对象
    "type": "UnityEngine.SpriteRenderer",  // 完整类型名
    "ifMissing": true                      // 可选：已存在则跳过
  },
  "out": { "component": "$spriteRenderer" }
}
```

### SetSerializedProperty - 设置属性（核心命令）

这是最强大的命令，可以设置任何序列化字段：

```json
{
  "cmd": "SetSerializedProperty",
  "args": {
    "target": { "ref": "$spriteRenderer" },
    "propertyPath": "m_Color",
    "value": [1, 0.5, 0, 1]  // RGBA
  }
}
```

**支持的值类型：**

| 类型 | 值格式示例 |
|------|-----------|
| int/float | `123`, `1.5` |
| bool | `true`, `false` |
| string | `"hello"` |
| Vector2/3/4 | `[x, y]`, `[x, y, z]`, `{"x": 0, "y": 1}` |
| Color | `[r, g, b, a]` 或 `{"r": 1, "g": 0, "b": 0, "a": 1}` |
| Enum | `{"enum": "EnumType", "name": "ValueName"}` |
| Asset引用 | `{"assetGuid": "..."}` 或 `{"assetPath": "Assets/..."}` |
| 变量引用 | `{"ref": "$variable"}` |
| 数组 | `[element1, element2, ...]` |
| null | `{"null": true}` |

**常用 propertyPath 示例：**
- `m_Color` - SpriteRenderer 颜色
- `m_Sprite` - SpriteRenderer 的 Sprite
- `m_Material` - Renderer 的材质
- `m_Script` - MonoBehaviour 脚本引用
- `fieldName` - 自定义脚本的 public 字段

### CreateScriptableObject - 创建 SO

```json
{
  "cmd": "CreateScriptableObject",
  "args": {
    "type": "MyNamespace.MyConfig",
    "assetPath": "Assets/AutoGen/Configs/MyConfig.asset",
    "overwrite": false,
    "init": {
      "configName": "Default",
      "maxHealth": 100,
      "spawnPoints": [[0,0,0], [10,0,0]]
    }
  },
  "out": { "asset": "$myConfig" }
}
```

### CreateOrEditPrefab - 创建/编辑预制体

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
      },
      {
        "cmd": "SetSerializedProperty",
        "args": {
          "target": { "ref": "$sr" },
          "propertyPath": "m_Color",
          "value": [1, 0, 0, 1]
        }
      }
    ]
  },
  "out": { "prefab": "$enemyPrefab" }
}
```

⚠️ **Prefab 编辑规则：**
- 嵌套命令中 `$prefabRoot` 自动指向 Prefab 根对象
- 对象查找限制在 Prefab 内部，不会污染场景
- 禁止在 edits 中调用 `InstantiatePrefabInScene`

### InstantiatePrefabInScene - 实例化到场景

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
  "out": { "instance": "$enemyInstance" }
}
```

### SetTransform - 设置变换

```json
{
  "cmd": "SetTransform",
  "args": {
    "target": { "ref": "$enemyInstance" },
    "position": [10, 0, 0],
    "rotation": [0, 180, 0],
    "scale": [2, 2, 2],
    "space": "local"  // "local" 或 "world"
  }
}
```

### SaveAssets - 保存资产

```json
{
  "cmd": "SaveAssets",
  "args": {
    "refresh": true  // 可选：是否刷新 AssetDatabase
  }
}
```

### ImportAssets - 导入资产

```json
{
  "cmd": "ImportAssets",
  "args": {
    "paths": ["Assets/AutoGen/Textures/new.png"],
    "force": false
  }
}
```

## 目标引用方式

命令中的 `target` 参数支持多种引用方式：

```json
// 1. 变量引用（最常用）
{ "ref": "$variableName" }

// 2. 场景路径
{ "scenePath": "Canvas/Panel/Button" }

// 3. Asset GUID
{ "assetGuid": "a1b2c3d4e5f6..." }

// 4. Asset 路径
{ "assetPath": "Assets/Prefabs/Player.prefab" }
```

## 链式任务示例

### 示例：创建带配置的敌人预制体

```json
{
  "schemaVersion": 1,
  "jobId": "create_enemy_with_config",
  "projectWriteRoot": "Assets/AutoGen",
  "commands": [
    {
      "cmd": "CreateScriptableObject",
      "args": {
        "type": "EnemyConfig",
        "assetPath": "Assets/AutoGen/Configs/Goblin.asset",
        "init": {
          "enemyName": "Goblin",
          "maxHealth": 50,
          "moveSpeed": 3.5
        }
      },
      "out": { "asset": "$goblinConfig" }
    },
    {
      "cmd": "CreateOrEditPrefab",
      "args": {
        "prefabPath": "Assets/AutoGen/Prefabs/Goblin.prefab",
        "rootName": "Goblin",
        "edits": [
          {
            "cmd": "AddComponent",
            "args": {
              "target": { "ref": "$prefabRoot" },
              "type": "SpriteRenderer"
            }
          },
          {
            "cmd": "AddComponent",
            "args": {
              "target": { "ref": "$prefabRoot" },
              "type": "EnemyController"
            },
            "out": { "component": "$controller" }
          },
          {
            "cmd": "SetSerializedProperty",
            "args": {
              "target": { "ref": "$controller" },
              "propertyPath": "config",
              "value": { "ref": "$goblinConfig" }
            }
          }
        ]
      },
      "out": { "prefab": "$goblinPrefab" }
    },
    {
      "cmd": "InstantiatePrefabInScene",
      "args": {
        "prefabPath": "Assets/AutoGen/Prefabs/Goblin.prefab",
        "parentPath": "Enemies",
        "position": [5, 0, 0],
        "ensure": true
      }
    },
    {
      "cmd": "SaveAssets",
      "args": { "refresh": true }
    }
  ]
}
```

## 错误处理

### 检查执行结果

Job 执行后，结果文件在 `AutoGenJobs/results/<jobId>.result.json`：

```json
{
  "jobId": "my_job",
  "status": "DONE",  // DONE, FAILED, WAITING
  "commandResults": [
    { "index": 0, "cmd": "CreateGameObject", "status": "DONE" }
  ],
  "error": null
}
```

### 常见错误及解决方案

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| `Path not allowed` | 写入路径不在 Assets/AutoGen | 修改资产路径 |
| `Type not found` | C# 类型不存在 | 检查类型名称和命名空间 |
| `Target not found` | 变量或路径无效 | 检查 out 和 ref 匹配 |
| `WAITING_COMPILING` | Unity 正在编译 | 等待编译完成 |

## 最佳实践

1. **使用 ensure 模式**：防止重复创建
2. **合理拆分 Job**：复杂任务拆分为多个 Job
3. **检查结果**：执行后检查 result.json 确认成功
4. **使用有意义的变量名**：`$playerSprite` 而不是 `$var1`
5. **先创建资产再引用**：确保依赖顺序正确

## 扩展能力

如果需要的操作不在内置命令中，你可以：

1. **组合现有命令**：大多数操作可通过 `SetSerializedProperty` 实现
2. **请求用户创建自定义命令**：提供 `IJobCommand` 接口实现
3. **使用 Unity 脚本配合**：创建 MonoBehaviour 在 Start 时执行逻辑

## 注意事项

- Unity Editor 必须处于运行状态（不是 Play 模式）
- Runner 默认启用，可通过 `Tools > AutoGen Jobs` 菜单控制
- 长时间无响应可能是 Unity 正在编译或导入资产
