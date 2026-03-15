# WFP 原生 API 实现计划

## 目标
停止使用 H.Wfp 和 H.Firewall 库，直接使用微软 WFP (Windows Filtering Platform) 原生 API 实现防火墙规则功能。

## 当前状态
- `WfpNative.cs` 已创建，包含基本的 P/Invoke 定义和辅助类
- 需要完善端口范围支持和 IPv6 地址条件
- 需要重写 `FirewallRule.cs`

## 配置模型支持的功能 (PortRule)
| 属性 | 说明 | 示例 |
|------|------|------|
| Action | allow / block | "allow" |
| Protocol | tcp / udp | "udp" |
| Direction | inbound / outbound / both | "both" |
| Port | 单端口、端口范围 | "1024-65535" |
| RemoteAddress | CIDR 格式 | "100.100.0.0/20" |
| LocalAddress | CIDR 格式 | "192.168.1.0/24" |
| Weight | 权重值 (越大优先级越高) | 3 |
| IpVersion | ipv4 / ipv6 / both | "both" |
| ExcludeDns | 是否排除 DNS 端口 | true |

## 实现步骤

### 步骤 1: 完善 WfpNative.cs
添加缺失的功能：
- [x] 基本 P/Invoke 定义
- [x] WfpSession 类
- [x] WfpFilterBuilder 类
- [ ] 端口范围条件支持 (FWP_MATCH_RANGE)
- [ ] IPv6 地址条件支持
- [ ] 权重设置

### 步骤 2: 重写 FirewallRule.cs
使用原生 API 完全重写：
- [ ] 移除 H.Wfp 和 H.Firewall 依赖
- [ ] 实现 CreateRule 静态方法
- [ ] 支持所有 PortRule 属性
- [ ] 支持端口范围解析
- [ ] 支持 IPv4/IPv6 双栈
- [ ] 支持 Direction (inbound/outbound/both)
- [ ] 支持 Weight 权重
- [ ] 支持 ExcludeDns

### 步骤 3: 更新项目文件
- [ ] 移除 H.Wfp 包引用

### 步骤 4: 编译测试
- [ ] 编译项目
- [ ] 验证功能

## 技术细节

### 端口范围处理策略
对于大范围端口 (如 1024-65535)，WFP 不直接支持端口范围匹配。
策略：创建阻止所有端口的规则，然后为允许的地址创建例外规则。

示例配置：
```
规则1: 放行 100.100.0.0/20 (Weight=3, 高优先级)
规则2: 放行 DNS 端口 53 (Weight=3)
规则3: 阻止所有 UDP (Weight=1, 低优先级)
```

### Layer 选择
| Direction | IPv4 Layer | IPv6 Layer |
|-----------|------------|------------|
| outbound | FWPM_LAYER_ALE_AUTH_CONNECT_V4 | FWPM_LAYER_ALE_AUTH_CONNECT_V6 |
| inbound | FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V4 | FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V6 |

### 权重实现
WFP 使用 `effectiveWeight` 字段设置过滤器优先级。
权重值越大，优先级越高。

## 文件变更
1. `Services/WfpNative.cs` - 完善 P/Invoke 定义
2. `Services/FirewallRule.cs` - 完全重写
3. `AstralLite.csproj` - 移除 H.Wfp 包引用
