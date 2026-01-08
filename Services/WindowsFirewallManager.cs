using System;

namespace AstralLite.Services;

internal static class WindowsFirewallManager
{
    private const int NetFwRuleDirIn = 1;
    private const int NetFwRuleDirOut = 2;
    private const int NetFwActionBlock = 0;
    private const int NetFwIpProtocolTcp = 6;
    private const int NetFwIpProtocolUdp = 17;
    private const int NetFwProfileAll = 2147483647;

    public static void AddBlockRule(
        string nameBase,
        string applicationName,
        string protocol,
        string? localPorts,
        string? remotePorts,
        string? remoteAddresses)
    {
        AddRule(nameBase + "_in", NetFwRuleDirIn, applicationName, protocol, localPorts, remotePorts, remoteAddresses);
        AddRule(nameBase + "_out", NetFwRuleDirOut, applicationName, protocol, localPorts, remotePorts, remoteAddresses);
    }

    public static void RemoveRule(string nameBase)
    {
        RemoveRuleInternal(nameBase + "_in");
        RemoveRuleInternal(nameBase + "_out");
    }

    private static void AddRule(
        string name,
        int direction,
        string applicationName,
        string protocol,
        string? localPorts,
        string? remotePorts,
        string? remoteAddresses)
    {
        dynamic policy2 = CreateComInstance("HNetCfg.FwPolicy2");
        dynamic rule = CreateComInstance("HNetCfg.FWRule");

        rule.Name = name;
        rule.ApplicationName = applicationName;
        rule.Protocol = ToProtocol(protocol);
        rule.Direction = direction;
        rule.Action = NetFwActionBlock;
        rule.Enabled = true;
        rule.Profiles = NetFwProfileAll;

        if (!string.IsNullOrWhiteSpace(localPorts))
        {
            rule.LocalPorts = localPorts;
        }

        if (!string.IsNullOrWhiteSpace(remotePorts))
        {
            rule.RemotePorts = remotePorts;
        }

        if (!string.IsNullOrWhiteSpace(remoteAddresses))
        {
            rule.RemoteAddresses = remoteAddresses;
        }

        policy2.Rules.Add(rule);
    }

    private static void RemoveRuleInternal(string name)
    {
        dynamic policy2 = CreateComInstance("HNetCfg.FwPolicy2");
        policy2.Rules.Remove(name);
    }

    private static dynamic CreateComInstance(string progId)
    {
        var type = Type.GetTypeFromProgID(progId);
        if (type == null)
        {
            throw new InvalidOperationException($"COM ProgID not found: {progId}");
        }

        return Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Failed to create COM instance: {progId}");
    }

    private static int ToProtocol(string protocol)
    {
        return string.Equals(protocol, "udp", StringComparison.OrdinalIgnoreCase)
            ? NetFwIpProtocolUdp
            : NetFwIpProtocolTcp;
    }
}
