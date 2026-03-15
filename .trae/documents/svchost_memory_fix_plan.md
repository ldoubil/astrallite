# AstralLite - svchost.exe 内存疯涨和崩溃问题修复计划

## 问题分析

根据代码分析，我发现了以下关键问题：

1. **FirewallRule.CreateBlockRule 创建了 WFP 会话、提供者和子层
2. **FirewallRule.Dispose 只释放了会话，但没有清理提供者和子层
3. **当频繁创建和删除规则时，大量的提供者和子层没有被清理，导致 svchost.exe 内存疯涨
4. **最后 AstralLite 因为等待 svchost.exe 进程而崩溃

## 任务分解与优先级

### [x] 任务 1: 修改 FirewallRule 类，确保清理提供者和子层
- **Priority**: P0
- **Depends On**: None
- **Description**: 
  - 修改 FirewallRule 类，跟踪创建的提供者和子层
  - 在 Dispose 方法中添加清理提供者和子层的逻辑
  - 确保所有资源都被正确释放
- **Success Criteria**:
  - FirewallRule 类能够正确清理提供者和子层
  - 所有 WFP 资源都被正确释放
- **Test Requirements**:
  - `programmatic` TR-1.1: 编译通过，无语法错误
  - `human-judgement` TR-1.2: 代码结构清晰，注释完整

### [x] 任务 2: 优化规则创建和删除的逻辑
- **Priority**: P1
- **Depends On**: 任务 1
- **Description**:
  - 优化 ProcessMonitorService 中规则的创建和删除逻辑
  - 确保规则被正确管理，避免重复操作
  - 添加错误处理和重试机制
- **Success Criteria**:
  - 规则创建和删除逻辑更加健壮
  - 错误处理机制完善
- **Test Requirements**:
  - `programmatic` TR-2.1: 编译通过，无语法错误
  - `human-judgement` TR-2.2: 逻辑清晰，易于理解

### [x] 任务 3: 测试修复效果
- **Priority**: P1
- **Depends On**: 任务 1, 任务 2
- **Description**:
  - 编译项目，确保没有错误
  - 测试规则的创建和删除
  - 验证 svchost.exe 内存使用情况
- **Success Criteria**:
  - 项目成功编译
  - 规则创建和删除正常
  - svchost.exe 内存使用稳定
- **Test Requirements**:
  - `programmatic` TR-3.1: 编译通过
  - `human-judgement` TR-3.2: 测试通过，内存使用稳定

## 实现细节

### 任务 1 实现细节
- 在 FirewallRule 类中添加字段来跟踪提供者和子层
- 修改 CreateBlockRule 方法，保存提供者和子层的信息
- 修改 Dispose 方法，先清理子层，然后清理提供者，最后释放会话
- 添加异常处理，确保即使清理过程中出现错误也能继续清理其他资源

### 任务 2 实现细节
- 在 ProcessMonitorService 中添加更健壮的错误处理
- 确保规则删除时不会因为异常而中断
- 添加重试机制处理临时错误

### 任务 3 实现细节
- 运行 dotnet build 确保项目
- 测试规则的创建和删除
- 监控 svchost.exe 的内存使用情况

## 风险评估

- **风险 1**: 修改 FirewallRule 类可能会影响现有功能
  - **缓解措施**: 确保修改向后兼容，不改变公共接口

- **风险 2**: 清理提供者和子层可能会导致错误
  - **缓解措施**: 添加异常处理，确保清理过程中出现错误也能继续清理其他资源

## 预期成果

- 成功修复 svchost.exe 内存疯涨问题
- AstralLite 不再因为等待 svchost.exe 进程而崩溃
- 所有 WFP 资源都被正确释放
- 代码结构清晰，注释完整，易于维护
