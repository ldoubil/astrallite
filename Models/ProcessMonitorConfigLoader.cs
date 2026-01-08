using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AstralLite.Models;

public static class ProcessMonitorConfigLoader
{
    public static List<ProcessMonitorConfiguration> Load(string filePath)
    {
        if (!File.Exists(filePath))
            return new List<ProcessMonitorConfiguration>();
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<ProcessMonitorConfiguration>>(json) ?? new List<ProcessMonitorConfiguration>();
    }
}
