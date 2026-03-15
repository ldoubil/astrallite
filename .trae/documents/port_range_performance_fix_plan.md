# AstralLite - 端口范围过滤器性能问题修复计划

## 问题分析

在 `FirewallRule.cs` 的 `AddFiltersForLayer` 方法中，存在一个严重的性能问题：

```csharp
foreach (var port in ports)
{
    AddFilter(baseName + "_p" + port, ...);
}
```

当端口范围是 `1024-65535` 时，这会循环创建 **64512 个过滤器**！这会导致：
1. 严重的性能问题
2. 内存占用疯涨
3. WFP 资源耗尽
4. svchost.exe 内存疯涨

## 解决方案

修改 `AddFiltersForLayer` 方法，当端口数量很大时（比如超过 100 个），不指定端口条件，匹配所有端口。这样可以避免创建大量过滤器，提高性能。

## 任务分解

### [x] 任务 1: 修改 AddFiltersForLayer 方法
- **Priority**: P0
- **Description**: 
  - 修改 AddFiltersForLayer 方法，当端口数量超过 100 时，不指定端口条件
  - 这样可以避免创建大量过滤器
- **Success Criteria**:
  - 不再循环创建大量过滤器
  - 使用通配符匹配所有端口
- **Test Requirements**:
  - `programmatic` TR-1.1: 编译通过

### [x] 任务 2: 测试验证
- **Priority**: P1
- **Depends On**: 任务 1
- **Description**:
  - 编译项目，确保没有错误
  - 验证不会创建大量过滤器
- **Success Criteria**:
  - 项目成功编译
  - 性能问题解决
- **Test Requirements**:
  - `programmatic` TR-2.1: 编译通过

## 实现细节

修改后的 `AddFiltersForLayer` 方法：

```csharp
private void AddFiltersForLayer(
    string baseName,
    string description,
    Guid layerKey,
    SafeFwpmHandle appId,
    string action,
    string protocol,
    IReadOnlyCollection<ushort> ports,
    IReadOnlyList<V4AddrMask> localAddresses,
    IReadOnlyList<V4AddrMask> remoteAddresses,
    byte weight)
{
    var hasPorts = ports.Count > 0;
    var hasLocalAddresses = localAddresses.Count > 0;
    var hasRemoteAddresses = remoteAddresses.Count > 0;
    
    // 当端口数量很大时（比如超过 100 个），不指定端口条件，匹配所有端口
    // 这样可以避免创建大量过滤器，提高性能
    if (hasPorts && ports.Count > 100)
    {
        AddFilter(baseName, description, layerKey, appId, action, protocol, null, 
            hasLocalAddresses ? localAddresses : null, 
            hasRemoteAddresses ? remoteAddresses : null, 
            weight);
        return;
    }
    
    // 否则，为每个端口创建一个过滤器
    foreach (var port in ports)
    {
        AddFilter(baseName + "_p" + port, description, layerKey, appId, action, protocol, port, 
            hasLocalAddresses ? localAddresses : null, 
            hasRemoteAddresses ? remoteAddresses : null, 
            weight);
    }
}
```

## 预期成果

- 解决端口范围导致的性能问题
- 不再创建大量过滤器
- 内存占用稳定
- svchost.exe 不会内存疯涨
