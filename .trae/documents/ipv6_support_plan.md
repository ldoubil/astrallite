# AstralLite - 为规则组1和规则组2添加 IPv6 支持

## 需求
为规则组1和规则组2添加 IPv6 规则，规则与 IPv4 规则相同。

## 当前状态
- 当前代码只使用 `Layers.V4["IPv4 outbound"]` 和 `Layers.V4["IPv4 inbound"]`
- 没有使用 `Layers.V6` 来添加 IPv6 过滤器

## 任务分解

### [ ] 任务 1: 修改 FirewallRule 类，添加 IPv6 支持
- **Priority**: P0
- **Description**: 
  - 修改 `AddAppFilters` 方法，同时添加 IPv4 和 IPv6 过滤器
  - 使用 `Layers.V6["IPv6 outbound"]` 和 `Layers.V6["IPv6 inbound"]` 添加 IPv6 过滤器
  - 确保 IPv6 过滤器与 IPv4 过滤器使用相同的规则
- **Success Criteria**:
  - FirewallRule 类同时支持 IPv4 和 IPv6
  - IPv6 过滤器正确创建
- **Test Requirements**:
  - `programmatic` TR-1.1: 编译通过，无语法错误

### [ ] 任务 2: 测试验证
- **Priority**: P1
- **Depends On**: 任务 1
- **Description**:
  - 编译项目，确保没有错误
  - 验证 IPv6 过滤器正确创建
- **Success Criteria**:
  - 项目成功编译
  - IPv6 过滤器正确创建
- **Test Requirements**:
  - `programmatic` TR-2.1: 编译通过

## 实现细节

### 任务 1 实现细节

修改 `AddAppFilters` 方法：

```csharp
private void AddAppFilters(
    string name,
    string applicationPath,
    string action,
    string protocol,
    string direction,
    IReadOnlyCollection<ushort> ports,
    bool isAnyPort,
    string? localAddress,
    string? remoteAddress,
    byte weight)
{
    using var appId = WfpMethods.GetAppIdFromFileName(applicationPath);
    if (appId == null || appId.IsInvalid)
    {
        throw new InvalidOperationException($"Failed to resolve app id for: {applicationPath}");
    }
    var portList = isAnyPort ? Array.Empty<ushort>() : ports;
    var remoteAddresses = ParseRemoteAddresses(remoteAddress);
    var localAddresses = ParseRemoteAddresses(localAddress);
    
    var addOutbound = direction == "outbound" || direction == "both";
    var addInbound = direction == "inbound" || direction == "both";
    
    // IPv4 过滤器
    if (addOutbound)
    {
        AddFiltersForLayer($"{name}_v4_out", $"Outbound app {action} (IPv4)", Layers.V4["IPv4 outbound"], appId, action, protocol, portList, isAnyPort, localAddresses, remoteAddresses, weight);
    }
    if (addInbound)
    {
        AddFiltersForLayer($"{name}_v4_in", $"Inbound app {action} (IPv4)", Layers.V4["IPv4 inbound"], appId, action, protocol, portList, isAnyPort, localAddresses, remoteAddresses, weight);
    }
    
    // IPv6 过滤器
    if (addOutbound)
    {
        AddFiltersForLayer($"{name}_v6_out", $"Outbound app {action} (IPv6)", Layers.V6["IPv6 outbound"], appId, action, protocol, portList, isAnyPort, localAddresses, remoteAddresses, weight);
    }
    if (addInbound)
    {
        AddFiltersForLayer($"{name}_v6_in", $"Inbound app {action} (IPv6)", Layers.V6["IPv6 inbound"], appId, action, protocol, portList, isAnyPort, localAddresses, remoteAddresses, weight);
    }
}
```

## 注意事项

1. **IPv6 地址解析**：当前 `ParseRemoteAddresses` 方法只支持 IPv4 地址解析。如果需要支持 IPv6 地址，需要修改该方法。但是，根据用户的需求，规则组1和规则组2的 IPv6 规则与 IPv4 规则相同，所以不需要修改地址解析逻辑。

2. **过滤器数量**：添加 IPv6 支持后，过滤器数量会翻倍。UI 上显示的过滤器数量会相应增加。

## 预期成果

- 规则组1和规则组2同时支持 IPv4 和 IPv6
- IPv6 过滤器与 IPv4 过滤器使用相同的规则
- 过滤器数量正确显示
- 代码结构清晰，易于维护
