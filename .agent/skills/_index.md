# Unity AutoGen Skills 索引

本目录包含 Unity AutoGen Job System 的 Agent Skills，让 AI 能够通过 JSON Job 文件远程操控 Unity Editor。

## Skills 列表

| Skill 文件 | 用途 | 何时使用 |
|------------|------|---------|
| `unity-autogen-jobs.md` | **核心参考** | 了解系统能力、命令语法、完整 API |
| `unity-autogen-templates.md` | **快速模板** | 快速生成常见操作的 Job JSON |
| `unity-autogen-executor.md` | **执行工具** | 投递 Job、检查结果、诊断问题 |

## 快速开始

### 1. 理解系统

阅读 `unity-autogen-jobs.md` 了解：
- Job 文件结构
- 可用命令和参数
- 变量引用机制
- 安全约束

### 2. 选择模板

查看 `unity-autogen-templates.md` 找到合适的模板：
- 创建 GameObject
- 创建 Prefab
- 创建 ScriptableObject
- 实例化到场景
- 完整工作流

### 3. 执行操作

使用 `unity-autogen-executor.md` 中的工具：
- 生成唯一 Job ID
- 安全投递到 inbox
- 等待并检查结果

## 核心流程

```
用户请求 → AI 选择模板 → 填充参数 → 生成 Job JSON → 投递到 inbox → Unity 执行 → 检查结果
```

## 目录结构

```
项目根目录/
├── .agent/skills/              ← Skills 文档
│   ├── _index.md               ← 本文件
│   ├── unity-autogen-jobs.md   ← 核心参考
│   ├── unity-autogen-templates.md ← 快速模板
│   └── unity-autogen-executor.md  ← 执行工具
│
├── AutoGenJobs/                ← Job 文件系统
│   ├── inbox/                  ← 投递新 Job
│   ├── working/                ← 执行中
│   ├── done/                   ← 已完成
│   ├── results/                ← 结果和日志
│   └── dead/                   ← 失败的 Job
│
└── Assets/
    ├── Editor/AutoGenJobs/     ← Runner 代码
    └── AutoGen/                ← 自动生成的资产
        ├── Prefabs/
        ├── Configs/
        └── ...
```

## 常见操作映射

| 用户请求 | 对应 Skill | 使用的命令 |
|---------|-----------|-----------|
| "创建一个预制体" | templates#5 | CreateOrEditPrefab |
| "在场景放置对象" | templates#7 | InstantiatePrefabInScene |
| "创建配置文件" | templates#3 | CreateScriptableObject |
| "给对象加组件" | jobs.md | AddComponent |
| "修改组件属性" | jobs.md | SetSerializedProperty |
| "完整链式任务" | templates#9 | 多命令组合 |

## 注意事项

1. **Unity 必须打开**：Runner 在 Editor 中运行
2. **路径限制**：只能写入 `Assets/AutoGen/`
3. **类型必须存在**：使用的 C# 类型需要已编译
4. **使用 ensure 模式**：避免重复创建

## 版本信息

- **Runner Version**: 3
- **Schema Version**: 1
- **Skills Version**: 1.0
