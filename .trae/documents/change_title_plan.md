# AstralLite - 修改标题和托盘悬浮标题

## 需求
将标题和托盘悬浮标题改为 "Astral-收获日联机工具"

## 需要修改的位置

### 1. MainWindow.xaml
- 第 10 行：`Title="AstralLite-PD2"` → `Title="Astral-收获日联机工具"`

### 2. MainWindow.xaml.cs
- 第 22 行：`Text = "AstralLite-PD2"` → `Text = "Astral-收获日联机工具"`
- 第 77 行：`notifyIcon.ShowBalloonTip(1001, "AstralLite-PD2", ...)` → `notifyIcon.ShowBalloonTip(1001, "Astral-收获日联机工具", ...)`

## 任务分解

### [ ] 任务 1: 修改 MainWindow.xaml 标题
- **Priority**: P0
- **Description**: 
  - 修改 MainWindow.xaml 中的 Title 属性
- **Success Criteria**:
  - 标题正确显示为 "Astral-收获日联机工具"
- **Test Requirements**:
  - `programmatic` TR-1.1: 编译通过

### [ ] 任务 2: 修改 MainWindow.xaml.cs 托盘标题
- **Priority**: P0
- **Description**: 
  - 修改托盘图标的 Text 属性
  - 修改托盘气泡提示的标题
- **Success Criteria**:
  - 托盘悬浮标题正确显示为 "Astral-收获日联机工具"
- **Test Requirements**:
  - `programmatic` TR-2.1: 编译通过

### [ ] 任务 3: 测试验证
- **Priority**: P1
- **Depends On**: 任务 1, 任务 2
- **Description**:
  - 编译项目，确保没有错误
  - 运行应用程序，验证标题和托盘悬浮标题正确显示
- **Success Criteria**:
  - 项目成功编译
  - 标题和托盘悬浮标题正确显示
- **Test Requirements**:
  - `programmatic` TR-3.1: 编译通过

## 预期成果

- 窗口标题显示为 "Astral-收获日联机工具"
- 托盘悬浮标题显示为 "Astral-收获日联机工具"
- 托盘气泡提示标题显示为 "Astral-收获日联机工具"
