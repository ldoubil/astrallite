# AstralLite - 规则组启用逻辑分析与修复计划

## 问题分析

根据用户的要求，规则组的启用逻辑应该是：

1. **steam.exe+payday_win32_release.exe**：魔法墙启用规则组2，**不启用规则组1**
2. **steam.exe+payday2_win32_release.exe**：魔法墙启用规则组1+2
3. **仅payday2_win32_release.exe**：魔法墙启用规则组1，**不启用规则组2**
4. **仅steam.exe**：魔法墙规则不生效
5. **不考虑仅payday_win32_release.exe**

## 当前配置分析

### 规则组1 - Payday 2
```csharp
ProcessName = "payday2_win32_release"
RequireAllProcesses = [] // 空
RequireAnyProcesses = [] // 空
```

**启用条件**：
- 进程 `payday2_win32_release` 正在运行
- 没有 RequireAllProcesses 和 RequireAnyProcesses 的限制

**问题**：
- 当 `steam.exe+payday_win32_release.exe` 运行时，规则组1 会被启用（因为 payday2_win32_release 正在运行）
- 但用户要求这种情况下**不启用规则组1**

### 规则组2 - Steam
```csharp
ProcessName = "steam"
RequireAllProcesses = [] // 空
RequireAnyProcesses = ["payday_win32_release", "payday2_win32_release"]
```

**启用条件**：
- 进程 `steam` 正在运行
- 且 `payday_win32_release` 或 `payday2_win32_release` 正在运行

**问题**：
- 这个配置是正确的

## 问题根源

规则组1 的配置不正确。当 `steam.exe+payday_win32_release.exe` 运行时，规则组1 不应该启用，但当前配置会因为 `payday2_win32_release` 正在运行而启用规则组1。

## 解决方案

需要修改规则组1 的配置，添加条件来区分不同的场景：

### 方案1：使用 RequireAllProcesses

修改规则组1 的配置：
```csharp
ProcessName = "payday2_win32_release"
RequireAllProcesses = [] // 空
RequireAnyProcesses = [] // 空
```

但这样无法区分 `steam.exe+payday_win32_release.exe` 和 `steam.exe+payday2_win32_release.exe` 的情况。

### 方案2：添加排除条件

需要添加一个新的属性来表示"排除条件"，当某些进程运行时不启用该规则组。

### 方案3：重新设计配置逻辑

重新分析用户的需求：

1. **steam.exe+payday_win32_release.exe**：
   - 规则组2 启用（steam 运行，且 payday_win32_release 运行）
   - 规则组1 不启用（因为 steam 运行，但 payday2_win32_release 运行，而不是 payday2_win32_release）

2. **steam.exe+payday2_win32_release.exe**：
   - 规则组1 启用（payday2_win32_release 运行）
   - 规则组2 启用（steam 运行，且 payday2_win32_release 运行）

3. **仅payday2_win32_release.exe**：
   - 规则组1 启用（payday2_win32_release 运行）
   - 规则组2 不启用（steam 没有运行）

4. **仅steam.exe**：
   - 规则组1 不启用（payday2_win32_release 没有运行）
   - 规则组2 不启用（虽然 steam 运行，但没有 payday_win32_release 或 payday2_win32_release 运行）

## 最终解决方案

修改规则组1 的配置，添加 `ExcludeProcesses` 属性：

```csharp
new ProcessMonitorConfiguration
{
    GroupName = "1",
    DisplayName = "规则组1 - Payday 2",
    ProcessName = "payday2_win32_release",
    ExcludeProcesses = new List<string> { "payday_win32_release" }, // 当 payday_win32_release 运行时不启用
    Rules = new List<PortRule>
    {
        // ... 规则配置
    }
}
```

这样：
- 当 `steam.exe+payday_win32_release.exe` 运行时，规则组1 不启用（因为 payday_win32_release 运行，触发排除条件）
- 当 `steam.exe+payday2_win32_release.exe` 运行时，规则组1 启用（payday2_win32_release 运行，且 payday_win32_release 没有运行，不触发排除条件）
- 当 `仅payday2_win32_release.exe` 运行时，规则组1 启用（payday2_win32_release 运行，且 payday_win32_release 没有运行，不触发排除条件）

## 任务分解

### [ ] 任务 1: 添加 ExcludeProcesses 属性到 ProcessMonitorConfiguration 类
- **Priority**: P0
- **Description**: 
  - 在 ProcessMonitorConfiguration 类中添加 ExcludeProcesses 属性
  - 当列表中的任何进程运行时，该规则组不启用
- **Success Criteria**:
  - ProcessMonitorConfiguration 类中成功添加 ExcludeProcesses 属性
- **Test Requirements**:
  - `programmatic` TR-1.1: 编译通过，无语法错误

### [ ] 任务 2: 修改 ProcessMonitorService 中的 ShouldApplyRules 方法
- **Priority**: P0
- **Depends On**: 任务 1
- **Description**:
  - 修改 ShouldApplyRules 方法，添加排除逻辑
  - 当 ExcludeProcesses 中的任何进程运行时，返回 false
- **Success Criteria**:
  - ShouldApplyRules 方法能够正确处理排除条件
- **Test Requirements**:
  - `programmatic` TR-2.1: 编译通过，无语法错误

### [ ] 任务 3: 更新 ProcessMonitorConfigurationList 配置
- **Priority**: P1
- **Depends On**: 任务 1
- **Description**:
  - 更新规则组1 的配置，添加 ExcludeProcesses
  - 确保 steam.exe+payday_win32_release.exe 时不启用规则组1
- **Success Criteria**:
  - 配置正确，满足用户要求
- **Test Requirements**:
  - `programmatic` TR-3.1: 编译通过，无语法错误

### [ ] 任务 4: 测试验证
- **Priority**: P1
- **Depends On**: 任务 1, 任务 2, 任务 3
- **Description**:
  - 编译项目，确保没有错误
  - 验证配置逻辑是否正确
- **Success Criteria**:
  - 项目成功编译
  - 配置逻辑正确
- **Test Requirements**:
  - `programmatic` TR-4.1: 编译通过
