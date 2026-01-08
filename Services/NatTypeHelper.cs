namespace AstralLite.Services;

/// <summary>
/// NAT 类型辅助类
/// </summary>
public static class NatTypeHelper
{
    /// <summary>
    /// 将 NAT 类型数字转换为英文名称
    /// </summary>
    /// <param name="natType">NAT 类型数字 (1-10)</param>
    /// <returns>NAT 类型英文名称</returns>
    public static string GetNatTypeName(int natType)
    {
        return natType switch
        {
            1 => "Open Internet",
            2 => "No PAT NAT",
            3 => "FullCone NAT",
            4 => "Restricted NAT",
            5 => "Port Restricted NAT",
            6 => "Symmetric Firewall",
            7 => "Symmetric Easy Increase NAT",
            8 => "Symmetric Easy Decrease NAT",
            9 => "Symmetric NAT",
            10 => "Blocked",
            _ => "Unknown"
        };
    }
    
    /// <summary>
    /// 获取 NAT 类型的难度等级
    /// </summary>
    /// <param name="natType">NAT 类型数字 (1-10)</param>
    /// <returns>难度等级</returns>
    public static string GetNatDifficulty(int natType)
    {
        return natType switch
        {
            1 => "简单",
            2 or 3 or 4 => "普通",
            5 or 6 => "中等",
            7 or 8 => "困难",
            9 => "极难",
            10 => "不可能",
            _ => "未知"
        };
    }
}
