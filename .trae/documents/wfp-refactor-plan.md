# 防火墙重构计划 - Rust DLL 封装方案

## 方案概述

将 Rust 的 WFP 实现封装为 DLL，C# 通过 P/Invoke 调用。

## 项目结构

```
f:\mg\
├── Cargo.toml
├── src\
│   └── lib.rs          # DLL 导出
├── include\
│   └── mg_wall.h       # C 头文件（供 C# 使用）
└── README.md
```

## API 设计

### 导出函数

```c
// 引擎管理
int32_t mg_start(void);
int32_t mg_stop(void);

// 规则管理
int32_t mg_add_rule(
    const char* id,
    const char* name,
    int32_t enabled,
    const char* action,        // "allow" or "block"
    const char* protocol,      // "tcp", "udp", "both", "any"
    const char* direction,     // "inbound", "outbound", "both"
    const char* app_path,      // NT 路径格式
    const char* remote_ip,
    const char* local_ip,
    const char* remote_port,   // 支持 "53" 或 "1024-65535"
    const char* local_port,
    uint8_t weight             // 权重：0-255，越大优先级越高
);

int32_t mg_remove_rule(const char* id);
int32_t mg_update_rule(...);  // 同 mg_add_rule 参数
int32_t mg_get_status(int32_t* is_running, int32_t* active_rules, int32_t* total_rules);
```

### 返回值

* `0` = 成功

* `-1` = 引擎未启动

* `-2` = 规则已存在

* `-3` = 规则不存在

* `<-10` = 其他错误

## 关键实现细节

### 1. 路径转换

C# 端调用 `mg_add_rule` 时，`app_path` 参数需要传递 **NT 路径格式**：

* DOS 路径: `C:\Program Files\Steam\steam.exe`

* NT 路径: `\device\harddiskvolume3\program files\steam\steam.exe`

C# 端负责转换（参考 `nt.rs` 实现）。

### 2. 权重系统

```rust
filter.weight.type = FWP_UINT8;
filter.weight.Anonymous.uint8 = weight;  // 0-255
```

权重规则：

* 权重越大，优先级越高

* 允许规则权重 > 阻止规则权重 → 允许生效

* 建议范围：阻止规则 1-10，允许规则 11-20

### 3. 端口范围

```rust
// 单端口: "53"
PortFilter::Single(53)

// 范围: "1024-65535"
PortFilter::Range(1024, 65535)
```

### 4. 动态会话

使用 `FWPM_SESSION_FLAG_DYNAMIC`，规则随引擎自动清理。

## C# 调用示例

```csharp
public class MgWall : IDisposable
{
    private const string DllPath = @"f:\mg\target\release\mg_wall.dll";

    [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
    private static extern int mg_start();

    [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
    private static extern int mg_stop();

    [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
    private static extern int mg_add_rule(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string id,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        int enabled,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string action,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string protocol,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string direction,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string appPath,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string remoteIp,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string localIp,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string remotePort,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string localPort,
        byte weight);

    [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
    private static extern int mg_remove_rule(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string id);

    public void Start() => mg_start();
    public void Stop() => mg_stop();

    public void AddRule(FirewallRuleConfig config)
    {
        var ntPath = GetNtPath(config.AppPath);
        mg_add_rule(
            config.Id,
            config.Name,
            config.Enabled ? 1 : 0,
            config.Action,
            config.Protocol,
            config.Direction,
            ntPath,
            config.RemoteIp,
            config.LocalIp,
            config.RemotePort,
            config.LocalPort,
            config.Weight);
    }

    public void RemoveRule(string id) => mg_remove_rule(id);
}
```

## 执行步骤

1. **创建 Rust DLL 项目** (`f:\mg`)

   * 复用 `astral/rust/src/api/magic_wall.rs` 的实现

   * 添加 C 兼容的导出函数

   * 添加权重参数

2. **编译 Rust DLL**

   * `cargo build --release`

   * 生成 `mg_wall.dll`

3. **修改 AstralLite**

   * 删除现有的 `FirewallRule.cs`

   * 创建 `MgWall.cs` 封装 P/Invoke

   * 修改 `ProcessMonitorService.cs` 使用新实现

   * 添加 NT 路径转换（参考 `nt.rs`）

4. **测试验证**

   * 启动游戏测试规则生效

   * 关闭游戏测试规则清理

