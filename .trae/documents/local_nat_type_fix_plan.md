# AstralLite - 修复本机不显示 NAT 类型的问题

## 问题分析

在 `MainViewModel.cs` 的 `UpdatePlayerList` 方法中，本地玩家的 NAT 类型被硬编码为空字符串：

```csharp
Players.Insert(0, new Player
{
    InstanceId = localInstanceId,
    Name = PlayerName,
    Ping = "本机",
    UdpNatType = string.Empty,  // 问题：硬编码为空
    TcpNatType = string.Empty,  // 问题：硬编码为空
    TransportSummary = string.Empty,
    LossRate = string.Empty
});
```

但实际上，`NetworkInfo.MyNodeInfo.StunInfo` 包含了本地玩家的 NAT 类型信息：
- `UdpNatType`: UDP NAT 类型
- `TcpNatType`: TCP NAT 类型

## 任务分解

### [ ] 任务 1: 修改 UpdatePlayerList 方法，获取本地玩家的 NAT 类型
- **Priority**: P0
- **Description**: 
  - 修改 `UpdatePlayerList` 方法
  - 从 `NetworkInfo.MyNodeInfo.StunInfo` 获取本地玩家的 NAT 类型
  - 更新本地玩家的 `UdpNatType` 和 `TcpNatType` 属性
- **Success Criteria**:
  - 本地玩家正确显示 NAT 类型
  - 代码逻辑正确
- **Test Requirements**:
  - `programmatic` TR-1.1: 编译通过，无语法错误

### [ ] 任务 2: 测试验证
- **Priority**: P1
- **Depends On**: 任务 1
- **Description**:
  - 编译项目，确保没有错误
  - 运行应用程序，验证本地玩家显示 NAT 类型
- **Success Criteria**:
  - 项目成功编译
  - 本地玩家正确显示 NAT 类型
- **Test Requirements**:
  - `programmatic` TR-2.1: 编译通过

## 实现细节

### 任务 1 实现细节

修改 `UpdatePlayerList` 方法：

```csharp
// 收集当前在线的玩家 InstanceId 集合
var currentInstanceIds = new HashSet<string>();

// 获取本地玩家的 NAT 类型
string localUdpNatType = string.Empty;
string localTcpNatType = string.Empty;

foreach (var (networkName, info) in parsedInfo)
{
    if (info.MyNodeInfo?.StunInfo != null)
    {
        if (info.MyNodeInfo.StunInfo.UdpNatType > 0)
        {
            localUdpNatType = NatTypeHelper.GetNatTypeName(info.MyNodeInfo.StunInfo.UdpNatType);
        }
        
        if (info.MyNodeInfo.StunInfo.TcpNatType > 0)
        {
            localTcpNatType = NatTypeHelper.GetNatTypeName(info.MyNodeInfo.StunInfo.TcpNatType);
        }
        break; // 只需要获取一次
    }
}

// 确保本地玩家存在（InstanceId = "local" 表示本地玩家）
const string localInstanceId = "local";
var localPlayer = Players.FirstOrDefault(p => p.InstanceId == localInstanceId);
if (localPlayer == null)
{
    Players.Insert(0, new Player
    {
        InstanceId = localInstanceId,
        Name = PlayerName,
        Ping = "本机",
        UdpNatType = localUdpNatType,
        TcpNatType = localTcpNatType,
        TransportSummary = string.Empty,
        LossRate = string.Empty
    });
}
else
{
    // 更新本地玩家信息（名称和 NAT 类型可能改变）
    localPlayer.Name = PlayerName;
    localPlayer.Ping = "本机";
    localPlayer.UdpNatType = localUdpNatType;
    localPlayer.TcpNatType = localTcpNatType;
    localPlayer.TransportSummary = string.Empty;
    localPlayer.LossRate = string.Empty;
}
currentInstanceIds.Add(localInstanceId); // 本地玩家标记为在线
```

## 预期成果

- 本地玩家正确显示 NAT 类型
- 代码逻辑清晰，易于维护
- 与远程玩家的 NAT 类型显示逻辑一致
