# AstralLite - 在 UI 上显示过滤器数量

## 需求

在 UI 底部栏的版本号左边显示当前活跃的过滤器数量。

## 任务分解

### \[ ] 任务 1: 在 ProcessMonitorService 中添加过滤器数量属性

* **Priority**: P0

* **Description**:

  * 在 ProcessMonitorService 中添加一个公共属性 `FilterCount`，返回当前活跃的过滤器数量

  * 需要添加一个事件，当过滤器数量变化时通知 UI 更新

* **Success Criteria**:

  * ProcessMonitorService 中成功添加 FilterCount 属性

  * 添加 FilterCountChanged 事件

* **Test Requirements**:

  * `programmatic` TR-1.1: 编译通过，无语法错误

### \[ ] 任务 2: 在 MainViewModel 中添加过滤器数量绑定属性

* **Priority**: P0

* **Depends On**: 任务 1

* **Description**:

  * 在 MainViewModel 中添加 `FilterCount` 属性

  * 订阅 ProcessMonitorService 的 FilterCountChanged 事件

  * 更新 UI 绑定

* **Success Criteria**:

  * MainViewModel 中成功添加 FilterCount 属性

  * 属性正确绑定到 ProcessMonitorService

* **Test Requirements**:

  * `programmatic` TR-2.1: 编译通过，无语法错误

### \[ ] 任务 3: 修改 MainWindow\.xaml 底部栏

* **Priority**: P0

* **Depends On**: 任务 2

* **Description**:

  * 修改 MainWindow\.xaml 中的底部栏

  * 在版本号左边添加过滤器数量显示

  * 使用合适的样式和格式

* **Success Criteria**:

  * 底部栏正确显示过滤器数量

  * 样式与现有 UI 一致

* **Test Requirements**:

  * `programmatic` TR-3.1: 编译通过，无语法错误

  * `human-judgement` TR-3.2: UI 显示正确，样式美观

### \[ ] 任务 4: 测试验证

* **Priority**: P1

* **Depends On**: 任务 1, 任务 2, 任务 3

* **Description**:

  * 编译项目，确保没有错误

  * 运行应用程序，验证过滤器数量显示正确

  * 测试过滤器数量变化时 UI 是否正确更新

* **Success Criteria**:

  * 项目成功编译

  * 过滤器数量显示正确

  * UI 更新正常

* **Test Requirements**:

  * `programmatic` TR-4.1: 编译通过

  * `human-judgement` TR-4.2: 功能正常，UI 显示正确

## 实现细节

### 任务 1 实现细节

在 `ProcessMonitorService.cs` 中：

```csharp
public int FilterCount => _firewallRules.Count;
public event EventHandler<int>? FilterCountChanged;

private void OnFilterCountChanged()
{
    FilterCountChanged?.Invoke(this, FilterCount);
}
```

在添加和删除过滤器后调用 `OnFilterCountChanged()`。

### 任务 2 实现细节

在 `MainViewModel.cs` 中：

```csharp
private int _filterCount;
public int FilterCount
{
    get => _filterCount;
    set => SetProperty(ref _filterCount, value);
}
```

在构造函数中订阅事件：

```csharp
_processMonitorService.FilterCountChanged += (s, count) => FilterCount = count;
```

### 任务 3 实现细节

在 `MainWindow.xaml` 中修改底部栏：

```xml
<Border Grid.Row="4" BorderBrush="{StaticResource BorderColor}" BorderThickness="0,1,0,0" Padding="10,6">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>
        
        <TextBlock Grid.Column="0" 
                   Text="{Binding FilterCount, StringFormat='过滤器: {0}'}"
                   Foreground="{StaticResource TertiaryText}"
                   FontSize="10"
                   VerticalAlignment="Center"
                   Margin="0,0,10,0"/>
        
        <TextBlock Grid.Column="2"
                   Text="{Binding AppVersion}"
                   Foreground="{StaticResource TertiaryText}"
                   FontSize="10"
                   HorizontalAlignment="Right"/>
    </Grid>
</Border>
```

## 预期成果

* UI 底部栏正确显示过滤器数量

* 过滤器数量变化时 UI 自动更新

* 样式与现有 UI 一致

* 代码结构清晰，易于维护

