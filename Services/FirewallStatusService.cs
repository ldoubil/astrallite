using System;

namespace AstralLite.Services;

internal static class FirewallStatusService
{
    private const int NetFwProfileDomain = 1;
    private const int NetFwProfilePrivate = 2;
    private const int NetFwProfilePublic = 4;

    public static bool IsFirewallEnabled()
    {
        dynamic policy2 = CreatePolicy2();

        bool domainEnabled = policy2.FirewallEnabled[NetFwProfileDomain];
        bool privateEnabled = policy2.FirewallEnabled[NetFwProfilePrivate];
        bool publicEnabled = policy2.FirewallEnabled[NetFwProfilePublic];

        return domainEnabled || privateEnabled || publicEnabled;
    }

    public static void SetFirewallEnabled(bool enabled)
    {
        dynamic policy2 = CreatePolicy2();

        policy2.FirewallEnabled[NetFwProfileDomain] = enabled;
        policy2.FirewallEnabled[NetFwProfilePrivate] = enabled;
        policy2.FirewallEnabled[NetFwProfilePublic] = enabled;
    }

    private static dynamic CreatePolicy2()
    {
        var type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
        if (type == null)
        {
            throw new InvalidOperationException("COM ProgID not found: HNetCfg.FwPolicy2");
        }

        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("Failed to create COM instance: HNetCfg.FwPolicy2");
    }
}
