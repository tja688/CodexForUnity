---
name: Unity AutoGen Job Executor
description: 提供投递 Job、检查结果、诊断问题的实用工具。当需要实际执行 Unity 操作时使用此 Skill。
version: "1.0"
triggers:
  - 执行unity
  - 提交job
  - 检查结果
  - unity操作
---

# Unity AutoGen 执行器

这个 Skill 提供实际执行 Unity 操作的工具函数和诊断方法。

## 执行流程

### 步骤 1：生成 Job ID

```python
import uuid
from datetime import datetime

def generate_job_id(prefix="job"):
    """生成唯一的 Job ID"""
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    short_uuid = str(uuid.uuid4())[:8]
    return f"{prefix}_{timestamp}_{short_uuid}"

# 示例输出: job_20260107_182000_a1b2c3d4
```

### 步骤 2：构建 Job 数据

```python
def build_job(job_id, commands, requires_types=None):
    """构建标准 Job 结构"""
    job = {
        "schemaVersion": 1,
        "jobId": job_id,
        "createdAtUtc": datetime.utcnow().isoformat() + "Z",
        "runnerMinVersion": 3,
        "projectWriteRoot": "Assets/AutoGen",
        "dryRun": False,
        "commands": commands
    }
    
    if requires_types:
        job["requiresTypes"] = requires_types
    
    return job
```

### 步骤 3：安全投递

```python
import os
import json

PROJECT_ROOT = "."  # 或项目绝对路径
INBOX_PATH = os.path.join(PROJECT_ROOT, "AutoGenJobs", "inbox")

def submit_job(job_data):
    """安全投递 Job（原子写入）"""
    job_id = job_data["jobId"]
    
    # 确保目录存在
    os.makedirs(INBOX_PATH, exist_ok=True)
    
    pending_file = os.path.join(INBOX_PATH, f"{job_id}.job.json.pending")
    final_file = os.path.join(INBOX_PATH, f"{job_id}.job.json")
    
    # 1. 写入 pending 文件
    with open(pending_file, 'w', encoding='utf-8') as f:
        json.dump(job_data, f, indent=2, ensure_ascii=False)
        f.flush()
        os.fsync(f.fileno())  # 确保写入磁盘
    
    # 2. 原子重命名
    os.rename(pending_file, final_file)
    
    print(f"✅ Job submitted: {job_id}")
    print(f"   File: {final_file}")
    
    return job_id
```

### 步骤 4：检查结果

```python
import time

RESULTS_PATH = os.path.join(PROJECT_ROOT, "AutoGenJobs", "results")

def wait_for_result(job_id, timeout=30, poll_interval=0.5):
    """等待 Job 完成并返回结果"""
    result_file = os.path.join(RESULTS_PATH, f"{job_id}.result.json")
    
    start_time = time.time()
    while time.time() - start_time < timeout:
        if os.path.exists(result_file):
            with open(result_file, 'r', encoding='utf-8') as f:
                result = json.load(f)
            
            status = result.get("status", "UNKNOWN")
            
            if status == "DONE":
                print(f"✅ Job completed: {job_id}")
                return result
            elif status == "FAILED":
                print(f"❌ Job failed: {job_id}")
                print(f"   Error: {result.get('error', {}).get('message', 'Unknown')}")
                return result
            elif status == "WAITING":
                reason = result.get("error", {}).get("code", "Unknown")
                print(f"⏳ Job waiting: {reason}")
                # WAITING 状态继续等待
        
        time.sleep(poll_interval)
    
    print(f"⏱️ Timeout waiting for job: {job_id}")
    return None

def check_result(job_id):
    """检查 Job 结果（不等待）"""
    result_file = os.path.join(RESULTS_PATH, f"{job_id}.result.json")
    
    if not os.path.exists(result_file):
        return {"status": "NOT_FOUND", "message": "Result file not found"}
    
    with open(result_file, 'r', encoding='utf-8') as f:
        return json.load(f)
```

## 完整执行示例

```python
def execute_unity_operation(commands, requires_types=None, timeout=30):
    """一站式执行 Unity 操作"""
    
    # 1. 生成 Job ID
    job_id = generate_job_id("auto")
    
    # 2. 构建 Job
    job_data = build_job(job_id, commands, requires_types)
    
    # 3. 投递
    submit_job(job_data)
    
    # 4. 等待结果
    result = wait_for_result(job_id, timeout)
    
    return result

# 使用示例
result = execute_unity_operation([
    {
        "cmd": "CreateGameObject",
        "args": {"name": "MyTestObject", "ensure": True}
    }
])
```

## 诊断工具

### 检查 Runner 状态

```python
def check_runner_status():
    """检查 Runner 是否正常工作"""
    
    # 检查目录结构
    dirs_to_check = ["inbox", "working", "done", "results", "dead"]
    jobs_root = os.path.join(PROJECT_ROOT, "AutoGenJobs")
    
    print("📁 Directory structure:")
    for d in dirs_to_check:
        path = os.path.join(jobs_root, d)
        exists = os.path.exists(path)
        files = len(os.listdir(path)) if exists else 0
        status = "✅" if exists else "❌"
        print(f"   {status} {d}: {files} files" if exists else f"   {status} {d}: MISSING")
    
    # 检查 working 中是否有卡住的 job
    working_path = os.path.join(jobs_root, "working")
    if os.path.exists(working_path):
        working_files = os.listdir(working_path)
        if working_files:
            print(f"\n⚠️ Jobs in working ({len(working_files)}):")
            for f in working_files:
                print(f"   - {f}")
    
    # 检查 dead 中的失败 job
    dead_path = os.path.join(jobs_root, "dead")
    if os.path.exists(dead_path):
        dead_files = [f for f in os.listdir(dead_path) if f.endswith('.job.json')]
        if dead_files:
            print(f"\n❌ Dead jobs ({len(dead_files)}):")
            for f in dead_files[:5]:
                print(f"   - {f}")
```

### 读取日志

```python
def read_job_log(job_id):
    """读取 Job 执行日志"""
    log_file = os.path.join(RESULTS_PATH, f"{job_id}.log.txt")
    
    if not os.path.exists(log_file):
        print(f"Log not found for job: {job_id}")
        return None
    
    with open(log_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    print(f"📋 Log for {job_id}:")
    print(content)
    return content
```

### 清理工具

```python
def cleanup_old_results(days=7):
    """清理旧的结果文件"""
    import time
    
    cutoff = time.time() - (days * 24 * 60 * 60)
    
    for folder in ["done", "results"]:
        path = os.path.join(PROJECT_ROOT, "AutoGenJobs", folder)
        if not os.path.exists(path):
            continue
        
        for f in os.listdir(path):
            filepath = os.path.join(path, f)
            if os.path.getmtime(filepath) < cutoff:
                os.remove(filepath)
                print(f"Removed: {filepath}")
```

## 常见问题诊断

### Job 长时间未执行

1. **检查 Unity 是否打开**：Runner 只在 Editor 中运行
2. **检查是否在编译**：编译期间暂停处理
3. **检查 inbox 目录**：确认文件已正确投递

```python
def diagnose_stuck_job(job_id):
    """诊断卡住的 Job"""
    
    # 检查文件位置
    locations = {
        "inbox": os.path.join(PROJECT_ROOT, "AutoGenJobs", "inbox", f"{job_id}.job.json"),
        "working": os.path.join(PROJECT_ROOT, "AutoGenJobs", "working", f"{job_id}.job.json"),
        "done": os.path.join(PROJECT_ROOT, "AutoGenJobs", "done", f"{job_id}.job.json"),
        "dead": os.path.join(PROJECT_ROOT, "AutoGenJobs", "dead", f"{job_id}.job.json"),
    }
    
    print(f"🔍 Diagnosing job: {job_id}")
    for loc, path in locations.items():
        if os.path.exists(path):
            print(f"   📍 Found in: {loc}")
            return loc
    
    print("   ❓ Job file not found anywhere")
    return None
```

### 类型未找到错误

```python
def check_type_availability(type_name):
    """提示用户检查类型"""
    print(f"""
⚠️ Type not found: {type_name}

请确认：
1. 类型名称是否正确（包括命名空间）
2. 脚本是否已编译（检查 Unity Console）
3. 脚本是否在 Assets 目录下

常见类型格式：
- UnityEngine.SpriteRenderer
- UnityEngine.UI.Image  
- MyNamespace.MyComponent
""")
```

## 命令行工具

可以创建一个简单的 CLI 用于快速操作：

```python
#!/usr/bin/env python3
"""Unity AutoGen CLI"""

import sys
import argparse

def main():
    parser = argparse.ArgumentParser(description="Unity AutoGen Job CLI")
    subparsers = parser.add_subparsers(dest="command")
    
    # submit 命令
    submit_parser = subparsers.add_parser("submit", help="Submit a job file")
    submit_parser.add_argument("file", help="Job JSON file path")
    
    # status 命令
    status_parser = subparsers.add_parser("status", help="Check job status")
    status_parser.add_argument("job_id", help="Job ID to check")
    
    # diagnose 命令
    subparsers.add_parser("diagnose", help="Diagnose runner status")
    
    args = parser.parse_args()
    
    if args.command == "submit":
        with open(args.file, 'r') as f:
            job_data = json.load(f)
        submit_job(job_data)
    
    elif args.command == "status":
        result = check_result(args.job_id)
        print(json.dumps(result, indent=2))
    
    elif args.command == "diagnose":
        check_runner_status()
    
    else:
        parser.print_help()

if __name__ == "__main__":
    main()
```

## Typescript/Node.js 版本

```typescript
import * as fs from 'fs';
import * as path from 'path';

const PROJECT_ROOT = process.cwd();
const INBOX_PATH = path.join(PROJECT_ROOT, 'AutoGenJobs', 'inbox');
const RESULTS_PATH = path.join(PROJECT_ROOT, 'AutoGenJobs', 'results');

function generateJobId(prefix = 'job'): string {
  const timestamp = new Date().toISOString().replace(/[-:T.Z]/g, '').slice(0, 14);
  const uuid = Math.random().toString(36).substring(2, 10);
  return `${prefix}_${timestamp}_${uuid}`;
}

async function submitJob(jobData: object): Promise<string> {
  const jobId = (jobData as any).jobId;
  
  fs.mkdirSync(INBOX_PATH, { recursive: true });
  
  const pendingFile = path.join(INBOX_PATH, `${jobId}.job.json.pending`);
  const finalFile = path.join(INBOX_PATH, `${jobId}.job.json`);
  
  fs.writeFileSync(pendingFile, JSON.stringify(jobData, null, 2));
  fs.renameSync(pendingFile, finalFile);
  
  console.log(`✅ Job submitted: ${jobId}`);
  return jobId;
}

async function waitForResult(jobId: string, timeout = 30000): Promise<object | null> {
  const resultFile = path.join(RESULTS_PATH, `${jobId}.result.json`);
  const startTime = Date.now();
  
  while (Date.now() - startTime < timeout) {
    if (fs.existsSync(resultFile)) {
      const result = JSON.parse(fs.readFileSync(resultFile, 'utf-8'));
      if (result.status === 'DONE' || result.status === 'FAILED') {
        return result;
      }
    }
    await new Promise(r => setTimeout(r, 500));
  }
  
  return null;
}
```
