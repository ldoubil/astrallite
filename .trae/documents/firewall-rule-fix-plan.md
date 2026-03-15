# 防火墙规则实现修复计划

## 问题分析

### 当前配置
```
规则组1 - Payday 2:
1. 放行AS网络 - allow, udp, RemoteAddress=100.100.0.0/20
2. 放行DNS - allow, udp, Port=53
3. 阻止IPv4端口1024-65535 - block, udp, Port=1024-65535, IpVersion=ipv4
4. 阻止IPv6端口1024-65535 - block, udp, Port=1024-65535, IpVersion=ipv6
```

### 核心问题
**Windows 防火墙规则优先级：阻止规则 > 允许规则**

即使先添加允许规则，阻止规则仍会优先匹配。解决方案是：
- 阻止规则的 `RemoteAddresses` 设置为**排除已放行地址**

### 当前代码错误
`AddFirewallRuleWithExclusions` 方法把 `excludedAddresses` 直接作为 `remoteAddress` 传入，这是错误的！

正确做法：
- 阻止规则的 `RemoteAddresses` 应该是 `*` (所有地址)，但排除放行的地址
- Windows 防火墙不支持排除语法，需要换一种方式

## 解决方案

### 方案：简化配置，直接使用 Windows 防火墙语义

修改配置和实现逻辑：

1. **允许规则**：直接设置 `RemoteAddresses` 为特定地址
2. **阻止规则**：不设置 `RemoteAddresses`（阻止所有）

由于 Windows 防火墙阻止规则优先，需要改变策略：
- 不阻止已放行的地址

### 实现步骤

1. **修改 `FirewallRule.cs`**
   - 阻止规则：设置 `RemoteAddresses` 为排除地址（使用 Windows 防火墙的排除语法）

2. **修改 `ProcessMonitorService.cs`**
   - 收集所有允许规则的 `RemoteAddress`
   - 阻止规则排除这些地址

3. **Windows 防火墙排除语法**
   - 格式：`0.0.0.0-255.255.255.255,100.100.0.0/20` 
   - 表示：阻止所有，但排除 100.100.0.0/20

## 具体修改

### 1. 修改 `CreateSingleRule` 方法
- 阻止规则（action=0）且需要排除地址时：
  - `RemoteAddresses` = `0.0.0.0-255.255.255.255,{排除地址}`

### 2. 修改 `ApplyRules` 方法
- 收集允许规则的 `RemoteAddress`
- 传递给阻止规则

### 3. 测试验证
- 检查规则是否正确创建
- 验证放行地址是否生效
