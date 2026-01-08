using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using H.Firewall;
using H.Wfp;
using H.Wfp.Extensions;
using H.Wfp.Interop;
namespace AstralLite.Services;
internal sealed class FirewallRule : IDisposable
{
    private const byte DefaultWeight = 10;
    private static readonly Assembly WfpAssembly = typeof(WfpMethods).Assembly;
    private static readonly Type FilterConditionType = WfpAssembly.GetType("Windows.Win32.NetworkManagement.WindowsFilteringPlatform.FWPM_FILTER_CONDITION0")
        ?? throw new InvalidOperationException("FWPM_FILTER_CONDITION0 type not found.");
    private static readonly Type ConditionValueType = WfpAssembly.GetType("Windows.Win32.NetworkManagement.WindowsFilteringPlatform.FWP_CONDITION_VALUE0")
        ?? throw new InvalidOperationException("FWP_CONDITION_VALUE0 type not found.");
    private static readonly Type ConditionValueUnionType = WfpAssembly.GetType("Windows.Win32.NetworkManagement.WindowsFilteringPlatform.FWP_CONDITION_VALUE0+_Anonymous_e__Union")
        ?? throw new InvalidOperationException("FWP_CONDITION_VALUE0 union type not found.");
    private static readonly Type MatchTypeEnum = WfpAssembly.GetType("Windows.Win32.NetworkManagement.WindowsFilteringPlatform.FWP_MATCH_TYPE")
        ?? throw new InvalidOperationException("FWP_MATCH_TYPE type not found.");
    private static readonly Type DataTypeEnum = WfpAssembly.GetType("Windows.Win32.NetworkManagement.WindowsFilteringPlatform.FWP_DATA_TYPE")
        ?? throw new InvalidOperationException("FWP_DATA_TYPE type not found.");
    private static readonly Type ActionTypeEnum = WfpAssembly.GetType("Windows.Win32.NetworkManagement.WindowsFilteringPlatform.FWP_ACTION_TYPE")
        ?? throw new InvalidOperationException("FWP_ACTION_TYPE type not found.");
    private static readonly Type V4AddrMaskType = WfpAssembly.GetType("Windows.Win32.NetworkManagement.WindowsFilteringPlatform.FWP_V4_ADDR_AND_MASK")
        ?? throw new InvalidOperationException("FWP_V4_ADDR_AND_MASK type not found.");
    private static readonly FieldInfo FilterFieldKeyField = FilterConditionType.GetField("fieldKey", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("FWPM_FILTER_CONDITION0.fieldKey not found.");
    private static readonly FieldInfo FilterMatchTypeField = FilterConditionType.GetField("matchType", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("FWPM_FILTER_CONDITION0.matchType not found.");
    private static readonly FieldInfo FilterConditionValueField = FilterConditionType.GetField("conditionValue", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("FWPM_FILTER_CONDITION0.conditionValue not found.");
    private static readonly FieldInfo ConditionValueTypeField = ConditionValueType.GetField("type", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("FWP_CONDITION_VALUE0.type not found.");
    private static readonly FieldInfo ConditionValueUnionField = ConditionValueType.GetField("Anonymous", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("FWP_CONDITION_VALUE0.Anonymous not found.");
    private static readonly FieldInfo UnionUint8Field = ConditionValueUnionType.GetField("uint8", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("FWP_CONDITION_VALUE0.uint8 not found.");
    private static readonly FieldInfo UnionUint16Field = ConditionValueUnionType.GetField("uint16", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("FWP_CONDITION_VALUE0.uint16 not found.");
    private static readonly FieldInfo UnionByteBlobField = ConditionValueUnionType.GetField("byteBlob", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("FWP_CONDITION_VALUE0.byteBlob not found.");
    private static readonly FieldInfo UnionV4AddrMaskField = ConditionValueUnionType.GetField("v4AddrMask", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("FWP_CONDITION_VALUE0.v4AddrMask not found.");
    private static readonly FieldInfo V4AddrMaskAddrField = V4AddrMaskType.GetField("addr", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("FWP_V4_ADDR_AND_MASK.addr not found.");
    private static readonly FieldInfo V4AddrMaskMaskField = V4AddrMaskType.GetField("mask", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("FWP_V4_ADDR_AND_MASK.mask not found.");
    private static readonly MethodInfo AddFilterMethod = typeof(WfpMethods).GetMethod("AddFilter", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("WfpMethods.AddFilter not found.");
    private static readonly Guid ConditionAleAppId = new("d78e1e87-8644-4ea5-9437-d809ecefc971");
    private static readonly Guid ConditionIpProtocol = new("3971ef2b-623e-4f9a-8cb1-6e79b806b9a7");
    private static readonly Guid ConditionIpRemotePort = new("c35a604d-d22b-4e1a-91b4-68f674ee674b");
    private static readonly Guid ConditionIpLocalPort = new("0c1ba1af-5765-453f-af22-a8f791ac775b");
    private static readonly Guid ConditionIpRemoteAddress = new("b235ae9a-1d64-49b8-a44c-5ff3d9095045");
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
    public static FirewallRule CreateBlockRule(
        string name,
        string applicationPath,
        string protocol,
        IReadOnlyCollection<ushort> ports,
        bool isAnyPort,
        bool isLocalPort,
        string? remoteAddress)
    {
        var session = WfpMethods.CreateWfpSession("AstralLite", name)
            ?? throw new InvalidOperationException("Failed to create WFP session.");
        try
        {
            WfpMethods.BeginTransaction(session);
            var providerKey = WfpMethods.AddProvider(session, $"AstralLite:{name}", "AstralLite firewall rules");
            var subLayerKey = WfpMethods.AddSubLayer(session, providerKey, $"AstralLite:{name}", "AstralLite firewall rules");
            var rule = new FirewallRule(session, providerKey, subLayerKey);
            rule.AddAppBlockFilters(name, applicationPath, protocol, ports, isAnyPort, isLocalPort, remoteAddress);
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
    private void AddAppBlockFilters(
        string name,
        string applicationPath,
        string protocol,
        IReadOnlyCollection<ushort> ports,
        bool isAnyPort,
        bool isLocalPort,
        string? remoteAddress)
    {
        using var appId = WfpMethods.GetAppIdFromFileName(applicationPath);
        if (appId == null || appId.IsInvalid)
        {
            throw new InvalidOperationException($"Failed to resolve app id for: {applicationPath}");
        }
        var portList = isAnyPort ? Array.Empty<ushort>() : ports;
        var remoteAddresses = ParseRemoteAddresses(remoteAddress);
        AddFiltersForLayer($"{name}_out", "Outbound app block", Layers.V4["IPv4 outbound"], appId, protocol, portList, isLocalPort, remoteAddresses);
        AddFiltersForLayer($"{name}_in", "Inbound app block", Layers.V4["IPv4 inbound"], appId, protocol, portList, isLocalPort, remoteAddresses);
    }
    private void AddFiltersForLayer(
        string baseName,
        string description,
        Guid layerKey,
        SafeFwpmHandle appId,
        string protocol,
        IReadOnlyCollection<ushort> ports,
        bool isLocalPort,
        IReadOnlyList<V4AddrMask> remoteAddresses)
    {
        var hasPorts = ports.Count > 0;
        var hasAddresses = remoteAddresses.Count > 0;
        if (!hasPorts)
        {
            AddFilter(baseName, description, layerKey, appId, protocol, null, isLocalPort, hasAddresses ? remoteAddresses : null);
            return;
        }
        foreach (var port in ports)
        {
            AddFilter(baseName + "_p" + port, description, layerKey, appId, protocol, port, isLocalPort, hasAddresses ? remoteAddresses : null);
        }
    }
    private void AddFilter(
        string name,
        string description,
        Guid layerKey,
        SafeFwpmHandle appId,
        string protocol,
        ushort? port,
        bool isLocalPort,
        IReadOnlyList<V4AddrMask>? remoteAddresses)
    {
        if (remoteAddresses == null || remoteAddresses.Count == 0)
        {
            AddFilterInternal(name, description, layerKey, appId, protocol, port, isLocalPort, null);
            return;
        }
        for (var index = 0; index < remoteAddresses.Count; index++)
        {
            var addrMask = remoteAddresses[index];
            AddFilterInternal(name + "_ra" + index, description, layerKey, appId, protocol, port, isLocalPort, addrMask);
        }
    }
    private void AddFilterInternal(
        string name,
        string description,
        Guid layerKey,
        SafeFwpmHandle appId,
        string protocol,
        ushort? port,
        bool isLocalPort,
        V4AddrMask? remoteAddress)
    {
        IntPtr addrMaskPtr = IntPtr.Zero;
        try
        {
            var conditions = new List<object>
            {
                CreateAppIdCondition(appId),
                CreateProtocolCondition(protocol)
            };
            if (port.HasValue)
            {
                conditions.Add(CreatePortCondition(port.Value, isLocalPort));
            }
            if (remoteAddress.HasValue && remoteAddress.Value.Mask != 0)
            {
                addrMaskPtr = AllocateV4AddrMask(remoteAddress.Value);
                conditions.Add(CreateRemoteAddressCondition(addrMaskPtr));
            }
            var array = Array.CreateInstance(FilterConditionType, conditions.Count);
            for (var i = 0; i < conditions.Count; i++)
            {
                array.SetValue(conditions[i], i);
            }
            var actionType = Enum.Parse(ActionTypeEnum, "FWP_ACTION_BLOCK");
            _ = (Guid)AddFilterMethod.Invoke(null, new object[]
            {
                _session,
                _providerKey,
                _subLayerKey,
                layerKey,
                DefaultWeight,
                name,
                description,
                actionType,
                array
            })!;
        }
        finally
        {
            if (addrMaskPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(addrMaskPtr);
            }
        }
    }
    private static object CreateAppIdCondition(SafeFwpmHandle appId)
    {
        var conditionValue = CreateConditionValue(
            Enum.Parse(DataTypeEnum, "FWP_BYTE_BLOB_TYPE"),
            UnionByteBlobField,
            appId.DangerousGetHandle());
        return CreateCondition(ConditionAleAppId, conditionValue);
    }
    private static object CreateProtocolCondition(string protocol)
    {
        var proto = string.Equals(protocol, "udp", StringComparison.OrdinalIgnoreCase) ? (byte)17 : (byte)6;
        var conditionValue = CreateConditionValue(
            Enum.Parse(DataTypeEnum, "FWP_UINT8"),
            UnionUint8Field,
            proto);
        return CreateCondition(ConditionIpProtocol, conditionValue);
    }
    private static object CreatePortCondition(ushort port, bool isLocalPort)
    {
        var conditionValue = CreateConditionValue(
            Enum.Parse(DataTypeEnum, "FWP_UINT16"),
            UnionUint16Field,
            port);
        var fieldKey = isLocalPort ? ConditionIpLocalPort : ConditionIpRemotePort;
        return CreateCondition(fieldKey, conditionValue);
    }
    private static object CreateRemoteAddressCondition(IntPtr addrMaskPtr)
    {
        var conditionValue = CreateConditionValue(
            Enum.Parse(DataTypeEnum, "FWP_V4_ADDR_MASK"),
            UnionV4AddrMaskField,
            addrMaskPtr);
        return CreateCondition(ConditionIpRemoteAddress, conditionValue);
    }
    private static object CreateCondition(Guid fieldKey, object conditionValue)
    {
        var condition = Activator.CreateInstance(FilterConditionType)
            ?? throw new InvalidOperationException("Failed to create FWPM_FILTER_CONDITION0 instance.");
        var matchType = Enum.Parse(MatchTypeEnum, "FWP_MATCH_EQUAL");
        FilterFieldKeyField.SetValue(condition, fieldKey);
        FilterMatchTypeField.SetValue(condition, matchType);
        FilterConditionValueField.SetValue(condition, conditionValue);
        return condition;
    }
    private static object CreateConditionValue(object dataType, FieldInfo unionField, byte value)
    {
        var conditionValue = Activator.CreateInstance(ConditionValueType)
            ?? throw new InvalidOperationException("Failed to create FWP_CONDITION_VALUE0 instance.");
        var union = ConditionValueUnionField.GetValue(conditionValue)
            ?? throw new InvalidOperationException("Failed to read FWP_CONDITION_VALUE0 union.");
        ConditionValueTypeField.SetValue(conditionValue, dataType);
        unionField.SetValue(union, value);
        ConditionValueUnionField.SetValue(conditionValue, union);
        return conditionValue;
    }
    private static object CreateConditionValue(object dataType, FieldInfo unionField, ushort value)
    {
        var conditionValue = Activator.CreateInstance(ConditionValueType)
            ?? throw new InvalidOperationException("Failed to create FWP_CONDITION_VALUE0 instance.");
        var union = ConditionValueUnionField.GetValue(conditionValue)
            ?? throw new InvalidOperationException("Failed to read FWP_CONDITION_VALUE0 union.");
        ConditionValueTypeField.SetValue(conditionValue, dataType);
        unionField.SetValue(union, value);
        ConditionValueUnionField.SetValue(conditionValue, union);
        return conditionValue;
    }
    private static object CreateConditionValue(object dataType, FieldInfo unionField, IntPtr pointer)
    {
        var conditionValue = Activator.CreateInstance(ConditionValueType)
            ?? throw new InvalidOperationException("Failed to create FWP_CONDITION_VALUE0 instance.");
        var union = ConditionValueUnionField.GetValue(conditionValue)
            ?? throw new InvalidOperationException("Failed to read FWP_CONDITION_VALUE0 union.");
        ConditionValueTypeField.SetValue(conditionValue, dataType);
        unsafe
        {
            var boxedPointer = Pointer.Box((void*)pointer, unionField.FieldType);
            unionField.SetValue(union, boxedPointer);
        }
        ConditionValueUnionField.SetValue(conditionValue, union);
        return conditionValue;
    }
    private static IntPtr AllocateV4AddrMask(V4AddrMask addrMask)
    {
        var instance = Activator.CreateInstance(V4AddrMaskType)
            ?? throw new InvalidOperationException("Failed to create FWP_V4_ADDR_AND_MASK instance.");
        V4AddrMaskAddrField.SetValue(instance, addrMask.Address);
        V4AddrMaskMaskField.SetValue(instance, addrMask.Mask);
        var size = Marshal.SizeOf(instance);
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(instance, ptr, false);
        return ptr;
    }
    private static IReadOnlyList<V4AddrMask> ParseRemoteAddresses(string? remoteAddress)
    {
        if (string.IsNullOrWhiteSpace(remoteAddress))
        {
            return Array.Empty<V4AddrMask>();
        }
        var results = new List<V4AddrMask>();
        var segments = remoteAddress.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in segments)
        {
            var trimmed = raw.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }
            if (TryParseCidr(trimmed, out var addrMask))
            {
                results.Add(addrMask);
            }
        }
        return results;
    }
    private static bool TryParseCidr(string input, out V4AddrMask addrMask)
    {
        addrMask = default;
        var parts = input.Split('/');
        if (!IPAddress.TryParse(parts[0], out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return false;
        }
        var prefixLength = 32;
        if (parts.Length > 1 && !int.TryParse(parts[1], out prefixLength))
        {
            return false;
        }
        prefixLength = Math.Clamp(prefixLength, 0, 32);
        var address = ip.ToInteger();
        var mask = prefixLength == 0 ? 0u : uint.MaxValue << (32 - prefixLength);
        addrMask = new V4AddrMask(address, mask);
        return true;
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
    private readonly struct V4AddrMask
    {
        public V4AddrMask(uint address, uint mask)
        {
            Address = address;
            Mask = mask;
        }
        public uint Address { get; }
        public uint Mask { get; }
    }
}
