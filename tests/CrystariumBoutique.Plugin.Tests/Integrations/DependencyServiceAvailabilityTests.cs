using System.Reflection;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Integrations;
using CrystariumBoutique.Integrations;
using CrystariumBoutique.Integrations.GlamourerIpc;
using CrystariumBoutique.Integrations.PenumbraIpc;
using Dalamud.Plugin.Services;

namespace CrystariumBoutiquePlugin.Tests.Integrations;

public sealed class DependencyServiceAvailabilityTests
{
    [Fact]
    public void GlamourerUnavailableAtStartupCanRefreshToAvailable()
    {
        var client = new FakeGlamourerIpcClient
        {
            Availability = UnavailableGlamourer,
        };
        using var service = CreateGlamourerService(client);

        Assert.False(service.IsAvailable);
        Assert.False(service.RefreshAvailability());

        client.Availability = AvailableGlamourer;
        Assert.True(service.RefreshAvailability());
        Assert.True(service.IsAvailable);
        Assert.Equal("1.8", service.DetectedVersion);
    }

    [Fact]
    public void GlamourerConstructionDoesNotAccessThreadAffineCharacterState()
    {
        var client = new FakeGlamourerIpcClient
        {
            Availability = UnavailableGlamourer,
        };

        using var service = new GlamourerAppearanceService(
            client,
            new UnavailablePenumbraService(),
            CreateProxy<IPluginLog>(),
            CreateThrowingProxy<IClientState>(),
            CreateThrowingProxy<ITargetManager>(),
            CreateThrowingProxy<IPlayerState>(),
            CreateThrowingProxy<IObjectTable>(),
            EquipmentCatalog.Empty);

        Assert.False(service.IsAvailable);
        Assert.Equal(1, client.DetectionCount);
    }

    [Fact]
    public void GlamourerInitializedEventRefreshesImmediatelyWithoutDuplicateTransitions()
    {
        var client = new FakeGlamourerIpcClient
        {
            Availability = UnavailableGlamourer,
        };
        using var service = CreateGlamourerService(client);
        client.Availability = AvailableGlamourer;

        client.RaiseInitialized();
        Assert.True(service.IsAvailable);

        var detectionCount = client.DetectionCount;
        client.RaiseInitialized();
        Assert.True(service.IsAvailable);
        Assert.Equal(detectionCount + 1, client.DetectionCount);
        Assert.False(service.RefreshAvailability());
    }

    [Fact]
    public void GlamourerDisposedThenInitializedRecoversWithoutServiceReload()
    {
        var client = new FakeGlamourerIpcClient
        {
            Availability = AvailableGlamourer,
        };
        using var service = CreateGlamourerService(client);

        client.RaiseDisposed();
        Assert.False(service.IsAvailable);
        Assert.Equal(DependencyStatus.Unavailable, service.Status);

        client.Availability = AvailableGlamourer;
        client.RaiseInitialized();
        Assert.True(service.IsAvailable);
        Assert.Equal("1.8", service.DetectedVersion);
    }

    [Fact]
    public void PenumbraUnavailableAtStartupCanRefreshAndRecoverAfterReload()
    {
        var client = new FakePenumbraIpcClient
        {
            Availability = UnavailablePenumbra,
        };
        using var service = new PenumbraRedrawService(client, CreateProxy<IPluginLog>());

        Assert.False(service.IsAvailable);
        client.Availability = AvailablePenumbra;
        client.RaiseInitialized();
        Assert.True(service.IsAvailable);
        Assert.Equal("5.19", service.DetectedVersion);

        client.RaiseDisposed();
        Assert.False(service.IsAvailable);
        client.RaiseInitialized();
        Assert.True(service.IsAvailable);
    }

    private static readonly GlamourerIpcAvailability UnavailableGlamourer =
        new(DependencyStatus.Unavailable, null, null, "Provider missing.");

    private static readonly GlamourerIpcAvailability AvailableGlamourer =
        new(DependencyStatus.Available, 1, 8);

    private static readonly PenumbraIpcAvailability UnavailablePenumbra =
        new(DependencyStatus.Unavailable, null, null, "Provider missing.");

    private static readonly PenumbraIpcAvailability AvailablePenumbra =
        new(DependencyStatus.Available, 5, 19);

    private static GlamourerAppearanceService CreateGlamourerService(FakeGlamourerIpcClient client)
        => new(
            client,
            new UnavailablePenumbraService(),
            CreateProxy<IPluginLog>(),
            CreateProxy<IClientState>(),
            CreateProxy<ITargetManager>(),
            CreateProxy<IPlayerState>(),
            CreateProxy<IObjectTable>(),
            EquipmentCatalog.Empty);

    private static T CreateProxy<T>()
        where T : class
        => DispatchProxy.Create<T, DefaultInterfaceProxy>();

    private static T CreateThrowingProxy<T>()
        where T : class
        => DispatchProxy.Create<T, ThrowingInterfaceProxy>();

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1852:Seal internal types",
        Justification = "DispatchProxy requires the proxy base type to remain unsealed.")]
    private class DefaultInterfaceProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => targetMethod?.ReturnType == typeof(void)
                ? null
                : targetMethod?.ReturnType.IsValueType == true
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1852:Seal internal types",
        Justification = "DispatchProxy requires the proxy base type to remain unsealed.")]
    private class ThrowingInterfaceProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => throw new InvalidOperationException(
                $"Thread-affine dependency was accessed during construction: {targetMethod?.Name}");
    }

    private sealed class FakeGlamourerIpcClient : IGlamourerIpcClient
    {
        public event Action? ProviderInitialized;

        public event Action? ProviderDisposed;

        public event Action<nint, GlamourerIpcStateFinalizationType>? StateFinalized;

        public bool SupportsStateFinalized => true;

        public GlamourerIpcAvailability Availability { get; set; }

        public int DetectionCount { get; private set; }

        public GlamourerIpcAvailability DetectAvailability()
        {
            DetectionCount++;
            return Availability;
        }

        public GlamourerIpcStateResult GetState(int objectIndex, uint key) => default;

        public GlamourerIpcStateResult GetState(string playerName, uint key) => default;

        public GlamourerIpcStateResult GetStructuredState(int objectIndex, uint key) => default;

        public GlamourerIpcStateResult GetStructuredState(string playerName, uint key) => default;

        public GlamourerIpcErrorCode ApplyState(string serializedState, int objectIndex, uint key, GlamourerIpcApplyFlags flags)
            => GlamourerIpcErrorCode.Success;

        public GlamourerIpcErrorCode ApplyState(string serializedState, string playerName, uint key, GlamourerIpcApplyFlags flags)
            => GlamourerIpcErrorCode.Success;

        public GlamourerIpcErrorCode ReapplyState(int objectIndex, uint key, GlamourerIpcApplyFlags flags)
            => GlamourerIpcErrorCode.Success;

        public GlamourerIpcErrorCode ReapplyState(string playerName, uint key, GlamourerIpcApplyFlags flags)
            => GlamourerIpcErrorCode.Success;

        public GlamourerIpcErrorCode RevertState(int objectIndex, uint key, GlamourerIpcApplyFlags flags)
            => GlamourerIpcErrorCode.Success;

        public GlamourerIpcErrorCode RevertState(string playerName, uint key, GlamourerIpcApplyFlags flags)
            => GlamourerIpcErrorCode.Success;

        public GlamourerIpcErrorCode SetItem(int objectIndex, GlamourerIpcEquipSlot slot, ulong itemId, IReadOnlyList<byte> stains, uint key, GlamourerIpcApplyFlags flags)
            => GlamourerIpcErrorCode.Success;

        public GlamourerIpcErrorCode SetItem(string playerName, GlamourerIpcEquipSlot slot, ulong itemId, IReadOnlyList<byte> stains, uint key, GlamourerIpcApplyFlags flags)
            => GlamourerIpcErrorCode.Success;

        public GlamourerIpcErrorCode SetMetaState(int objectIndex, GlamourerIpcMetaFlags metaFlags, bool enabled, uint key, GlamourerIpcApplyFlags flags)
            => GlamourerIpcErrorCode.Success;

        public GlamourerIpcErrorCode SetMetaState(string playerName, GlamourerIpcMetaFlags metaFlags, bool enabled, uint key, GlamourerIpcApplyFlags flags)
            => GlamourerIpcErrorCode.Success;

        public void RaiseInitialized() => ProviderInitialized?.Invoke();

        public void RaiseDisposed() => ProviderDisposed?.Invoke();

        public void RaiseStateFinalized()
            => StateFinalized?.Invoke(nint.Zero, GlamourerIpcStateFinalizationType.Reapply);

        public void Dispose()
        {
        }
    }

    private sealed class FakePenumbraIpcClient : IPenumbraIpcClient
    {
        public event Action? ProviderInitialized;

        public event Action? ProviderDisposed;

        public PenumbraIpcAvailability Availability { get; set; }

        public PenumbraIpcAvailability DetectAvailability() => Availability;

        public void RedrawObject(int objectIndex, PenumbraIpcRedrawType redrawType)
        {
        }

        public void RaiseInitialized() => ProviderInitialized?.Invoke();

        public void RaiseDisposed() => ProviderDisposed?.Invoke();

        public void Dispose()
        {
        }
    }
}
