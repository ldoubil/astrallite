# AstralLite - 去除底部过滤器数量显示并更新版本号

## 需求
1. 去除底部的过滤器数量显示
2. 版本号改为 1.3

## 需要修改的位置

### 1. MainWindow.xaml
- 第 140-145 行：删除过滤器数量显示
- 第 134-138 行：简化 Grid 布局，只保留版本显示

### 2. MainViewModel.cs
- 第 93 行：版本号从 "版本 1.2" 改为 "版本 1.3"

## 任务分解

### [ ] 任务 1: 修改 MainWindow.xaml，去除过滤器数量显示
- **Priority**: P0
- **Description**: 
  - 删除过滤器数量显示的 TextBlock
  - 简化 Grid 布局，只保留版本显示
- **Success Criteria**:
  - 底部只显示版本号
- **Test Requirements**:
  - `programmatic` TR-1.1: 编译通过

### [ ] 任务 2: 修改 MainViewModel.cs，更新版本号
- **Priority**: P0
- **Description**: 
  - 版本号从 "版本 1.2" 改为 "版本 1.3"
- **Success Criteria**:
  - 版本号正确显示为 1.3
- **Test Requirements**:
  - `programmatic` TR-2.1: 编译通过

### [ ] 任务 3: 测试验证
- **Priority**: P1
- **Depends On**: 任务 1, 任务 2
- **Description**:
  - 编译项目，确保没有错误
  - 运行应用程序，验证底部只显示版本号
- **Success Criteria**:
  - 项目成功编译
  - 底部只显示版本号
- **Test Requirements**:
  - `programmatic` TR-3.1: 编译通过

## 预期成果

- 底部只显示版本号 "版本 1.3"
- 不再显示过滤器数量
- UI 布局简洁
