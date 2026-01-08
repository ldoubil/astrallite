using System;
using System.Runtime.InteropServices;
using H.Firewall;
using H.Wfp;
using H.Wfp.Interop;
namespace AstralLite.Services;
internal sealed class FirewallRule : IDisposable
{
    private const byte DefaultWeight = 10;
    private readonly SafeHandle _session;
    private readonly Guid _providerKey;
    private readonly Guid _subLayerKey;
    private bool _disposed;
    private FirewallRule(SafeHandle session, Guid providerKey, Guid subLayerKey)
    {
        _session = session;
        _providerKey = providerKey;
        _subLayerKey = subLayerKey;
    }
    public static FirewallRule CreateBlockRule(string name, string applicationPath)
    {
        var session = WfpMethods.CreateWfpSession("AstralLite", name)
            ?? throw new InvalidOperationException("Failed to create WFP session.");
        try
        {
            WfpMethods.BeginTransaction(session);
            var providerKey = WfpMethods.AddProvider(session, $"AstralLite:{name}", "AstralLite firewall rules");
            var subLayerKey = WfpMethods.AddSubLayer(session, providerKey, $"AstralLite:{name}", "AstralLite firewall rules");
            var rule = new FirewallRule(session, providerKey, subLayerKey);
            rule.AddAppBlockFilters(name, applicationPath);
            WfpMethods.CommitTransaction(session);
            return rule;
        }
        catch
        {
            try
            {
                WfpMethods.AbortTransaction(session);
            }
            catch
            {
            }
            session.Dispose();
            throw;
        }
    }
    private void AddAppBlockFilters(string name, string applicationPath)
    {
        using var appId = WfpMethods.GetAppIdFromFileName(applicationPath);
        if (appId == null || appId.IsInvalid)
        {
            throw new InvalidOperationException($"Failed to resolve app id for: {applicationPath}");
        }
        AddAppFilter($"{name}_out", "Outbound app block", Layers.V4["IPv4 outbound"], appId);
        AddAppFilter($"{name}_in", "Inbound app block", Layers.V4["IPv4 inbound"], appId);
    }
    private void AddAppFilter(string ruleName, string description, Guid layerKey, SafeFwpmHandle appId)
    {
        WfpMethods.AddAppId(
            _session,
            ActionType.Block,
            _providerKey,
            _subLayerKey,
            layerKey,
            appId,
            DefaultWeight,
            ruleName,
            description);
    }
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _session.Dispose();
        _disposed = true;
    }
}