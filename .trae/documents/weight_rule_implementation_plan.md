# AstralLite - 权重规则实现计划

## 概述

当前 AstralLite 系统支持指定程序和组织网络，但缺少权重规则功能。本计划旨在添加权重属性，使系统能够根据权重值优先级处理不同的网络配置。

## 项目使用的库分析

通过分析项目代码，我们发现项目使用了以下关键库：

1. **H.Firewall 和 H.Wfp** - 用于处理防火墙规则和 Windows 筛选平台 (WFP) 操作
2. **System.Collections.Generic** - 用于集合操作
3. **System.Linq** - 用于 LINQ 查询
4. **System.Text.Json** - 用于配置文件的序列化和反序列化
5. **System.Runtime.InteropServices** - 用于与原生 DLL 交互
6. **System.Threading** - 用于线程和定时器操作

## 权重规则实现方案

### 实现原理

权重规则的实现基于以下原理：

1. **权重属性添加**：在 `ProcessMonitorConfiguration` 类中添加 `Weight` 属性，用于表示配置的优先级
2. **权重排序**：在 `ProcessMonitorService` 中，当多个配置同时适用时，按照权重值降序排序，优先应用权重值高的配置
3. **规则应用**：根据排序结果，优先应用权重值高的配置的规则

### 实现优势

1. **灵活性**：通过权重值可以灵活调整不同配置的优先级
2. **可扩展性**：权重规则可以与现有的网络组织功能无缝集成
3. **简单直观**：权重值越大，优先级越高，规则简单明了

## 任务分解与优先级

### [ ] 任务 1: 在 ProcessMonitorConfiguration 类中添加 Weight 属性
- **优先级**: P0
- **Depends On**: None
- **Description**: 
  - 在 ProcessMonitorConfiguration 类中添加一个 Weight 属性，用于表示该配置的优先级
  - 权重值越大，优先级越高
- **Success Criteria**:
  - ProcessMonitorConfiguration 类中成功添加 Weight 属性
  - Weight 属性有合理的默认值
- **Test Requirements**:
  - `programmatic` TR-1.1: 编译通过，无语法错误
  - `human-judgement` TR-1.2: 代码结构清晰，注释完整
- **Notes**: 由于不需要向后兼容，可以设置默认权重值为 0 或其他合适的值

### [ ] 任务 2: 更新 ProcessMonitorConfigurationList 中的示例配置
- **优先级**: P1
- **Depends On**: 任务 1
- **Description**:
  - 为 ProcessMonitorConfigurationList 中的示例配置添加权重值
  - 确保不同配置有不同的权重值，以展示权重功能
- **Success Criteria**:
  - 示例配置中成功添加权重值
  - 权重值设置合理，体现优先级差异
- **Test Requirements**:
  - `programmatic` TR-2.1: 编译通过，无语法错误
  - `human-judgement` TR-2.2: 权重值设置合理，符合预期

### [ ] 任务 3: 在 ProcessMonitorService 中实现权重逻辑
- **优先级**: P0
- **Depends On**: 任务 1
- **Description**:
  - 修改 ProcessMonitorService 中的规则应用逻辑，考虑权重值
  - 当多个配置同时适用时，按照权重值降序排序，优先应用权重值高的配置
- **Success Criteria**:
  - ProcessMonitorService 能够根据权重值优先级处理配置
  - 权重值高的配置优先于权重值低的配置
- **Test Requirements**:
  - `programmatic` TR-3.1: 编译通过，无语法错误
  - `human-judgement` TR-3.2: 权重逻辑正确，符合预期

### [ ] 任务 4: 测试权重规则功能
- **优先级**: P1
- **Depends On**: 任务 1, 任务 2, 任务 3
- **Description**:
  - 运行应用程序，测试权重规则功能
  - 验证权重值高的配置是否优先于权重值低的配置
- **Success Criteria**:
  - 应用程序能够正常运行
  - 权重规则功能正常工作
- **Test Requirements**:
  - `programmatic` TR-4.1: 应用程序能够正常启动和运行
  - `human-judgement` TR-4.2: 权重规则功能符合预期，优先级正确

## 实现细节

### 任务 1 实现细节
- 在 `ProcessMonitorConfiguration.cs` 文件中添加 `Weight` 属性
- 属性类型为 `int`，默认值为 0
- 添加相应的 XML 注释，说明权重的作用

### 任务 2 实现细节
- 在 `ProcessMonitorConfigurationList.cs` 文件中为每个示例配置添加 `Weight` 属性
- 为不同的配置设置不同的权重值，例如：
  - Payday 2: 100
  - Steam: 50

### 任务 3 实现细节
- 在 `ProcessMonitorService.cs` 文件中修改 `EvaluateRules` 方法
- 当多个配置同时适用时，按照权重值降序排序，优先应用权重值高的配置
- 确保权重值相同的配置按照原有顺序处理

### 任务 4 实现细节
- 运行应用程序，启动相关进程
- 观察防火墙规则的应用情况，验证权重规则功能是否正常工作
- 测试不同权重值的配置，确保优先级正确

## 风险评估

- **风险 1**: 权重规则可能会影响现有功能
  - **缓解措施**: 确保权重规则的实现与现有功能无缝集成，只在必要的地方添加权重逻辑

- **风险 2**: 权重规则可能会导致配置冲突
  - **缓解措施**: 实现清晰的优先级逻辑，确保权重值高的配置优先于权重值低的配置

- **风险 3**: 权重规则可能会增加系统复杂度
  - **缓解措施**: 保持实现简洁，只在必要的地方添加权重逻辑

## 预期成果

- 成功添加权重规则功能
- 系统能够根据权重值优先级处理不同的网络配置
- 权重规则功能与现有功能无缝集成
- 代码结构清晰，注释完整，易于维护