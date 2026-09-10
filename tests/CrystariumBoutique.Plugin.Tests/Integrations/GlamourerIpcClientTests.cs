using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Integrations;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Integrations;
using CrystariumBoutique.Integrations.GlamourerIpc;
using CrystariumBoutique.Data;

namespace CrystariumBoutiquePlugin.Tests.Integrations;

public sealed class GlamourerIpcClientTests
{
    [Fact]
    public void MissingOrDisabledProviderIsUnavailable()
    {
        using var client = new GlamourerIpcClient(new FakeTransport { HasVersionFunction = false });

        var availability = client.DetectAvailability();

        Assert.Equal(DependencyStatus.Unavailable, availability.Status);
        Assert.Null(availability.Version);
    }

    [Theory]
    [InlineData(1, 7, DependencyStatus.Incompatible)]
    [InlineData(1, 8, DependencyStatus.Available)]
    [InlineData(1, 99, DependencyStatus.Available)]
    [InlineData(2, 0, DependencyStatus.Incompatible)]
    public void ApiVersionPolicyIsFailClosed(int major, int minor, DependencyStatus expected)
    {
        using var client = new GlamourerIpcClient(new FakeTransport { Version = (major, minor) });

        var availability = client.DetectAvailability();

        Assert.Equal(expected, availability.Status);
        Assert.Equal($"{major}.{minor}", availability.Version);
    }

    [Fact]
    public void CompatibleVersionWithMissingEndpointIsIncompatible()
    {
        using var client = new GlamourerIpcClient(new FakeTransport { HasAllOperationalFunctions = false });

        Assert.Equal(DependencyStatus.Incompatible, client.DetectAvailability().Status);
    }

    [Fact]
    public void VersionInvocationFailureLeavesBoutiqueUnavailable()
    {
        using var client = new GlamourerIpcClient(new FakeTransport { VersionException = new InvalidOperationException("offline") });

        var availability = client.DetectAvailability();

        Assert.Equal(DependencyStatus.Unavailable, availability.Status);
        Assert.Contains("InvalidOperationException", availability.Detail);
    }

    [Fact]
    public void IndexAndNameStateCaptureRemainDistinct()
    {
        var transport = new FakeTransport();
        using var client = new GlamourerIpcClient(transport);

        var indexState = client.GetState(214, 7);
        var nameState = client.GetState("Fixture Character", 8);

        Assert.Equal("index-state", indexState.SerializedState);
        Assert.Equal("name-state", nameState.SerializedState);
        Assert.Equal((214, 7u), transport.IndexCapture);
        Assert.Equal(("Fixture Character", 8u), transport.NameCapture);
    }

    [Fact]
    public void StructuredStateCaptureIsSerializedAndKeepsTargetsDistinct()
    {
        var transport = new FakeTransport();
        using var client = new GlamourerIpcClient(transport);

        var indexState = client.GetStructuredState(214, 7);
        var nameState = client.GetStructuredState("Fixture Character", 8);

        Assert.Equal("{\"Equipment\":{}}", indexState.SerializedState);
        Assert.Equal("{\"Equipment\":{\"Head\":{\"ItemId\":44820}}}", nameState.SerializedState);
        Assert.Equal((214, 7u), transport.StructuredIndexCapture);
        Assert.Equal(("Fixture Character", 8u), transport.StructuredNameCapture);
    }

    [Fact]
    public void StructuredEquipmentParserPreservesExactItemsAndIndependentStains()
    {
        const string state = """
            {
              "Equipment": {
                "Head": { "ItemId": 44820, "Stain": 17, "Stain2": 42 },
                "RFinger": { "ItemId": "51162", "Stain": "3", "Stain2": 0 },
                "Body": { "ItemId": 0, "Stain": 1, "Stain2": 2 }
              }
            }
            """;

        var equipment = GlamourerAppearanceService.ParseCapturedEquipment(state);

        Assert.Collection(
            equipment,
            head =>
            {
                Assert.Equal(EquipmentSlot.Head, head.Slot);
                Assert.Equal((uint)44820, head.ItemId);
                Assert.Equal([new StainId(17), new StainId(42)], head.Stains);
            },
            ring =>
            {
                Assert.Equal(EquipmentSlot.RightRing, ring.Slot);
                Assert.Equal((uint)51162, ring.ItemId);
                Assert.Equal([new StainId(3), StainId.None], ring.Stains);
            });
    }

    [Fact]
    public void StructuredEquipmentParserRestoresExactSourceForAppliedCustomShield()
    {
        const string state = """
            {
              "Equipment": {
                "OffHand": { "ItemId": 322161203413106, "Stain": 17, "Stain2": 42 }
              }
            }
            """;
        IReadOnlyDictionary<EquipmentSlot, uint> customSources =
            new Dictionary<EquipmentSlot, uint>
            {
                [EquipmentSlot.OffHand] = 51131,
            };

        var equipment = GlamourerAppearanceService.ParseCapturedEquipment(state, customSources);

        var shield = Assert.Single(equipment);
        Assert.Equal(EquipmentSlot.OffHand, shield.Slot);
        Assert.Equal((uint)51131, shield.ItemId);
        Assert.Equal([new StainId(17), new StainId(42)], shield.Stains);
    }

    [Fact]
    public void StructuredEquipmentParserRejectsUnprovenCustomItemIdentity()
    {
        const string state = """
            {
              "Equipment": {
                "OffHand": { "ItemId": 322161203413106, "Stain": 17, "Stain2": 42 }
              }
            }
            """;

        var equipment = GlamourerAppearanceService.ParseCapturedEquipment(state);

        Assert.Empty(equipment);
    }

    [Fact]
    public void SnapshotApplicationBoxesTheBase64StringAndPreservesFlags()
    {
        var transport = new FakeTransport();
        using var client = new GlamourerIpcClient(transport);
        var flags = GlamourerIpcApplyFlags.Once
            | GlamourerIpcApplyFlags.Equipment
            | GlamourerIpcApplyFlags.Customization;

        client.ApplyState("opaque-base64", 214, 0, flags);
        client.ApplyState("opaque-name-base64", "Fixture Character", 0, flags);

        Assert.IsType<string>(transport.IndexAppliedState);
        Assert.Equal("opaque-base64", transport.IndexAppliedState);
        Assert.IsType<string>(transport.NameAppliedState);
        Assert.Equal("opaque-name-base64", transport.NameAppliedState);
        Assert.Equal((ulong)flags, transport.LastFlags);
    }

    [Fact]
    public void SetItemUsesAConcreteTwoElementListAndPreservesBothDyes()
    {
        var transport = new FakeTransport();
        using var client = new GlamourerIpcClient(transport);

        client.SetItem(214, GlamourerIpcEquipSlot.Head, 44820, new byte[] { 12, 34 }, 0, GlamourerIpcApplyFlags.Once);

        var stains = Assert.IsType<List<byte>>(transport.LastStains);
        Assert.Equal([12, 34], stains);
        Assert.Equal((214, (byte)3, 44820ul, 0u, 1ul), transport.IndexItem);
    }

    [Fact]
    public void SetItemNamePreservesNameTargetAndBothDyes()
    {
        var transport = new FakeTransport();
        using var client = new GlamourerIpcClient(transport);

        client.SetItem("Fixture Character", GlamourerIpcEquipSlot.MainHand, 19053, new List<byte> { 1, 2 }, 0, GlamourerIpcApplyFlags.Once);

        Assert.Equal(("Fixture Character", (byte)1, 19053ul, 0u, 1ul), transport.NameItem);
        Assert.Equal([1, 2], transport.LastStains);
    }

    [Fact]
    public void SetMetaStateUsesVerifiedVisorFlagAndPreservesTargetAndValue()
    {
        var transport = new FakeTransport();
        using var client = new GlamourerIpcClient(transport);

        client.SetMetaState(214, GlamourerIpcMetaFlags.VisorState, false, 7, GlamourerIpcApplyFlags.Once);
        client.SetMetaState("Fixture Character", GlamourerIpcMetaFlags.VisorState, true, 8, GlamourerIpcApplyFlags.Once);

        Assert.Equal((214, 4ul, false, 7u, 1ul), transport.IndexMetaState);
        Assert.Equal(("Fixture Character", 4ul, true, 8u, 1ul), transport.NameMetaState);
    }

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 1 })]
    [InlineData(new byte[] { 1, 2, 3 })]
    public void SetItemRejectsMalformedStainLists(byte[] stains)
    {
        using var client = new GlamourerIpcClient(new FakeTransport());

        Assert.Throws<ArgumentException>(() => client.SetItem(
            0,
            GlamourerIpcEquipSlot.Head,
            44820,
            stains,
            0,
            GlamourerIpcApplyFlags.Once));
    }

    [Theory]
    [InlineData(EquipmentSlot.MainHand, 1)]
    [InlineData(EquipmentSlot.OffHand, 2)]
    [InlineData(EquipmentSlot.Head, 3)]
    [InlineData(EquipmentSlot.Body, 4)]
    [InlineData(EquipmentSlot.Hands, 5)]
    [InlineData(EquipmentSlot.Legs, 7)]
    [InlineData(EquipmentSlot.Feet, 8)]
    [InlineData(EquipmentSlot.Ears, 9)]
    [InlineData(EquipmentSlot.Neck, 10)]
    [InlineData(EquipmentSlot.Wrists, 11)]
    [InlineData(EquipmentSlot.RightRing, 12)]
    [InlineData(EquipmentSlot.LeftRing, 14)]
    public void EveryEquipmentSlotUsesVerifiedWireValue(EquipmentSlot slot, byte expected)
        => Assert.Equal(expected, (byte)GlamourerIpcSlotMapper.FromEquipmentSlot(slot));

    [Fact]
    public void StandaloneShieldUsesVerifiedCustomModelIdForOffHandApplication()
    {
        const ulong model = 0x0000000100170072;
        var catalog = CreateCatalog(CreateItem(
            51131,
            "Augmented Courtly Lover's Shield",
            EquipmentSlot.OffHand,
            model,
            isShield: true));
        var appearance = AppearanceSelection.WithoutStains(new AppearanceId(51131), 51131);

        var itemId = GlamourerAppearanceService.ResolveApplicationItemId(
            EquipmentSlot.OffHand,
            appearance,
            catalog,
            19);

        Assert.Equal(0x0001250100170072ul, itemId);
        Assert.Equal(2, (byte)GlamourerIpcSlotMapper.FromEquipmentSlot(EquipmentSlot.OffHand));
    }

    [Fact]
    public void BeastmasterShieldUsesVerifiedCustomModelIdForOffHandApplication()
    {
        const uint beastmasterClassJobId = 43;
        const ulong model = 0x00000001001A0072;
        var catalog = CreateCatalog(CreateItem(
            50750,
            "Beastmaster's Hoplon +1",
            EquipmentSlot.OffHand,
            model,
            isShield: true,
            classJobs: default(ClassJobMask).Add(beastmasterClassJobId)));
        var appearance = AppearanceSelection.WithoutStains(new AppearanceId(50750), 50750);

        var itemId = GlamourerAppearanceService.ResolveApplicationItemId(
            EquipmentSlot.OffHand,
            appearance,
            catalog,
            beastmasterClassJobId);

        Assert.Equal(GlamourerIpcCustomItemId.FromShieldModel(model), itemId);
    }

    [Theory]
    [InlineData(11, true)]
    [InlineData(10, false)]
    [InlineData(12, false)]
    public void LuminaShieldClassificationUsesExactItemUiCategory(uint categoryId, bool expected)
        => Assert.Equal(expected, LuminaItemCatalogLoader.IsShieldItemUiCategory(categoryId));

    [Fact]
    public void NonShieldOffHandRetainsItsExactSourceItemId()
    {
        var catalog = CreateCatalog(CreateItem(
            41001,
            "Test Off-hand Tool",
            EquipmentSlot.OffHand,
            0x0000000100020001,
            isShield: false));
        var appearance = AppearanceSelection.WithoutStains(new AppearanceId(41001), 41001);

        var itemId = GlamourerAppearanceService.ResolveApplicationItemId(
            EquipmentSlot.OffHand,
            appearance,
            catalog,
            19);

        Assert.Equal(41001ul, itemId);
    }

    [Fact]
    public void ShieldResolutionDoesNotRedirectMainHandApplication()
    {
        var catalog = CreateCatalog(CreateItem(
            51131,
            "Augmented Courtly Lover's Shield",
            EquipmentSlot.OffHand,
            0x0000000100170072,
            isShield: true));
        var appearance = AppearanceSelection.WithoutStains(new AppearanceId(51131), 51131);

        var itemId = GlamourerAppearanceService.ResolveApplicationItemId(
            EquipmentSlot.MainHand,
            appearance,
            catalog,
            19);

        Assert.Equal(51131ul, itemId);
    }

    [Fact]
    public void IncompatibleJobDoesNotBypassShieldItemValidationWithCustomModelId()
    {
        var catalog = CreateCatalog(CreateItem(
            51131,
            "Augmented Courtly Lover's Shield",
            EquipmentSlot.OffHand,
            0x0000000100170072,
            isShield: true));
        var appearance = AppearanceSelection.WithoutStains(new AppearanceId(51131), 51131);

        var itemId = GlamourerAppearanceService.ResolveApplicationItemId(
            EquipmentSlot.OffHand,
            appearance,
            catalog,
            41);

        Assert.Equal(51131ul, itemId);
    }

    [Fact]
    public void ReapplyAndRevertForwardIndexAndNameTargets()
    {
        var transport = new FakeTransport();
        using var client = new GlamourerIpcClient(transport);

        client.ReapplyState(214, 3, GlamourerIpcApplyFlags.Once);
        client.ReapplyState("Fixture Character", 4, GlamourerIpcApplyFlags.Once);
        client.RevertState(215, 5, GlamourerIpcApplyFlags.Equipment | GlamourerIpcApplyFlags.Customization);
        client.RevertState("Fixture Character", 6, GlamourerIpcApplyFlags.Equipment | GlamourerIpcApplyFlags.Customization);

        Assert.Equal((214, 3u, 1ul), transport.IndexReapply);
        Assert.Equal(("Fixture Character", 4u, 1ul), transport.NameReapply);
        Assert.Equal((215, 5u, 6ul), transport.IndexRevert);
        Assert.Equal(("Fixture Character", 6u, 6ul), transport.NameRevert);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(int.MaxValue)]
    public void KnownResultCodesRetainVerifiedValues(int value)
        => Assert.Equal(value, (int)GlamourerIpcClient.ToErrorCode(value));

    [Fact]
    public void UnknownResultCodeIsNotReinterpretedAsSuccess()
    {
        var code = GlamourerIpcClient.ToErrorCode(12345);

        Assert.Equal(12345, (int)code);
        Assert.NotEqual(GlamourerIpcErrorCode.Success, code);
        Assert.NotEqual(GlamourerIpcErrorCode.NothingDone, code);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(7, false)]
    [InlineData(8, false)]
    [InlineData(int.MaxValue, false)]
    [InlineData(12345, false)]
    public void MutationResultsAcceptOnlySuccessAndNothingDone(int value, bool expectedSuccess)
    {
        var result = GlamourerAppearanceService.MapMutationResult(
            GlamourerIpcClient.ToErrorCode(value),
            BoutiqueErrorCode.AppearanceApplyFailed,
            "perform the test operation");

        Assert.Equal(expectedSuccess, result.IsSuccess);
    }

    [Fact]
    public void IpcInvocationExceptionPropagatesToAppearanceErrorBoundary()
    {
        using var client = new GlamourerIpcClient(new FakeTransport { OperationException = new InvalidOperationException("IPC failed") });

        Assert.Throws<InvalidOperationException>(() => client.RevertState(0, 0, GlamourerIpcApplyFlags.Equipment));
    }

    [Fact]
    public void ProviderLifecycleEventsAreForwardedAndUnsubscribed()
    {
        var transport = new FakeTransport();
        var client = new GlamourerIpcClient(transport);
        var initialized = 0;
        var disposed = 0;
        client.ProviderInitialized += () => initialized++;
        client.ProviderDisposed += () => disposed++;

        transport.RaiseInitialized();
        transport.RaiseDisposed();
        client.Dispose();
        transport.RaiseInitialized();
        transport.RaiseDisposed();

        Assert.Equal(1, initialized);
        Assert.Equal(1, disposed);
        Assert.True(transport.WasDisposed);
    }

    [Fact]
    public void StateFinalizedEventForwardsExactActorAndGearsetValueThenUnsubscribes()
    {
        var transport = new FakeTransport();
        var client = new GlamourerIpcClient(transport);
        var notifications = new List<(nint Actor, GlamourerIpcStateFinalizationType Type)>();
        client.StateFinalized += (actor, type) => notifications.Add((actor, type));

        transport.RaiseStateFinalized((nint)0x1234, 9);
        client.Dispose();
        transport.RaiseStateFinalized((nint)0x5678, 9);

        Assert.True(client.SupportsStateFinalized);
        var notification = Assert.Single(notifications);
        Assert.Equal((nint)0x1234, notification.Actor);
        Assert.Equal(GlamourerIpcStateFinalizationType.Gearset, notification.Type);
        Assert.Equal(9, (int)notification.Type);
        Assert.Equal(1, transport.StateFinalizedAddCount);
        Assert.Equal(1, transport.StateFinalizedRemoveCount);
    }

    [Fact]
    public void ProviderLifecycleDoesNotDuplicateStateFinalizedSubscription()
    {
        var transport = new FakeTransport();
        using var client = new GlamourerIpcClient(transport);
        var notifications = 0;
        client.StateFinalized += (_, _) => notifications++;

        transport.RaiseDisposed();
        transport.RaiseInitialized();
        transport.RaiseDisposed();
        transport.RaiseInitialized();
        transport.RaiseStateFinalized((nint)0x1234, 9);

        Assert.Equal(1, notifications);
        Assert.Equal(1, transport.StateFinalizedAddCount);
    }

    [Theory]
    [InlineData(9, 0x1234, 0x1234, false, true)]
    [InlineData(1, 0x1234, 0x1234, false, false)]
    [InlineData(9, 0x1234, 0x5678, false, false)]
    [InlineData(9, 0, 0x1234, false, false)]
    [InlineData(9, 0x1234, 0x1234, true, false)]
    public void OnlyValidLocalPlayerGearsetFinalizationIsAccepted(
        int finalizationType,
        int actor,
        int localPlayer,
        bool isGPosing,
        bool expected)
        => Assert.Equal(
            expected,
            GlamourerAppearanceService.IsLocalPlayerGearsetFinalization(
                (nint)actor,
                (GlamourerIpcStateFinalizationType)finalizationType,
                (nint)localPlayer,
                isGPosing));

    [Theory]
    [InlineData(2, 0x1234, 0x1234, false, true)]
    [InlineData(4, 0x1234, 0x1234, false, true)]
    [InlineData(9, 0x1234, 0x1234, false, false)]
    [InlineData(2, 0x1234, 0x5678, false, false)]
    [InlineData(2, 0x1234, 0x1234, true, false)]
    public void OnlyValidLocalPlayerGameStateRevertFinalizationIsAccepted(
        int finalizationType,
        int actor,
        int localPlayer,
        bool isGPosing,
        bool expected)
        => Assert.Equal(
            expected,
            GlamourerAppearanceService.IsLocalPlayerGameStateRevertFinalization(
                (nint)actor,
                (GlamourerIpcStateFinalizationType)finalizationType,
                (nint)localPlayer,
                isGPosing));

    [Fact]
    public void ProviderInitializationSupportsAvailabilityRecovery()
    {
        var transport = new FakeTransport { HasVersionFunction = false };
        using var client = new GlamourerIpcClient(transport);
        var statusAfterInitialization = DependencyStatus.Unknown;
        client.ProviderInitialized += () => statusAfterInitialization = client.DetectAvailability().Status;

        Assert.Equal(DependencyStatus.Unavailable, client.DetectAvailability().Status);
        transport.HasVersionFunction = true;
        transport.RaiseInitialized();

        Assert.Equal(DependencyStatus.Available, statusAfterInitialization);
    }

    [Fact]
    public void PluginAssemblyHasNoGlamourerApiOrLunaReference()
    {
        var references = typeof(GlamourerIpcClient).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("Glamourer.Api", references);
        Assert.DoesNotContain("Penumbra.Api", references);
        Assert.DoesNotContain("Luna", references);
    }

    private static EquipmentCatalog CreateCatalog(params EquipmentItem[] items)
        => new(items);

    private static EquipmentItem CreateItem(
        uint itemId,
        string name,
        EquipmentSlot slot,
        ulong modelMain,
        bool isShield,
        ClassJobMask? classJobs = null)
        => new(
            itemId,
            name,
            isShield ? "Shield" : "Tool",
            1,
            1,
            1,
            2,
            new AppearanceKey(modelMain, 0),
            ContentGroupResolver.Resolve(1),
            [slot],
            EquippableClassJobs: classJobs ?? new ClassJobMask().Add(1).Add(19),
            IsShield: isShield);

    private sealed class FakeTransport : GlamourerIpcClient.IGlamourerIpcTransport
    {
        private Action<nint, int>? stateFinalized;

        public event Action? Initialized;

        public event Action? Disposed;

        public event Action<nint, int>? StateFinalized
        {
            add
            {
                stateFinalized += value;
                StateFinalizedAddCount++;
            }
            remove
            {
                stateFinalized -= value;
                StateFinalizedRemoveCount++;
            }
        }

        public int StateFinalizedAddCount { get; private set; }

        public int StateFinalizedRemoveCount { get; private set; }

        public bool HasVersionFunction { get; set; } = true;

        public bool HasAllOperationalFunctions { get; set; } = true;

        public bool HasStateFinalizedEvent { get; set; } = true;

        public (int Major, int Minor) Version { get; init; } = (1, 8);

        public Exception? VersionException { get; init; }

        public Exception? OperationException { get; init; }

        public (int ObjectIndex, uint Key)? IndexCapture { get; private set; }

        public (string PlayerName, uint Key)? NameCapture { get; private set; }

        public (int ObjectIndex, uint Key)? StructuredIndexCapture { get; private set; }

        public (string PlayerName, uint Key)? StructuredNameCapture { get; private set; }

        public object? IndexAppliedState { get; private set; }

        public object? NameAppliedState { get; private set; }

        public ulong LastFlags { get; private set; }

        public IReadOnlyList<byte>? LastStains { get; private set; }

        public (int ObjectIndex, byte Slot, ulong ItemId, uint Key, ulong Flags)? IndexItem { get; private set; }

        public (string PlayerName, byte Slot, ulong ItemId, uint Key, ulong Flags)? NameItem { get; private set; }

        public (int ObjectIndex, ulong MetaFlags, bool Enabled, uint Key, ulong Flags)? IndexMetaState { get; private set; }

        public (string PlayerName, ulong MetaFlags, bool Enabled, uint Key, ulong Flags)? NameMetaState { get; private set; }

        public (int ObjectIndex, uint Key, ulong Flags)? IndexReapply { get; private set; }

        public (string PlayerName, uint Key, ulong Flags)? NameReapply { get; private set; }

        public (int ObjectIndex, uint Key, ulong Flags)? IndexRevert { get; private set; }

        public (string PlayerName, uint Key, ulong Flags)? NameRevert { get; private set; }

        public bool WasDisposed { get; private set; }

        public (int Major, int Minor) InvokeVersion()
            => VersionException is null ? Version : throw VersionException;

        public (int ErrorCode, string State) GetState(int objectIndex, uint key)
        {
            ThrowIfRequested();
            IndexCapture = (objectIndex, key);
            return (0, "index-state");
        }

        public (int ErrorCode, string State) GetState(string playerName, uint key)
        {
            ThrowIfRequested();
            NameCapture = (playerName, key);
            return (0, "name-state");
        }

        public (int ErrorCode, object? State) GetStructuredState(int objectIndex, uint key)
        {
            ThrowIfRequested();
            StructuredIndexCapture = (objectIndex, key);
            return (0, "{\"Equipment\":{}}");
        }

        public (int ErrorCode, object? State) GetStructuredState(string playerName, uint key)
        {
            ThrowIfRequested();
            StructuredNameCapture = (playerName, key);
            return (0, "{\"Equipment\":{\"Head\":{\"ItemId\":44820}}}");
        }

        public int ApplyState(object state, int objectIndex, uint key, ulong flags)
        {
            ThrowIfRequested();
            IndexAppliedState = state;
            LastFlags = flags;
            return 0;
        }

        public int ApplyState(object state, string playerName, uint key, ulong flags)
        {
            ThrowIfRequested();
            NameAppliedState = state;
            LastFlags = flags;
            return 0;
        }

        public int ReapplyState(int objectIndex, uint key, ulong flags)
        {
            ThrowIfRequested();
            IndexReapply = (objectIndex, key, flags);
            return 0;
        }

        public int ReapplyState(string playerName, uint key, ulong flags)
        {
            ThrowIfRequested();
            NameReapply = (playerName, key, flags);
            return 0;
        }

        public int RevertState(int objectIndex, uint key, ulong flags)
        {
            ThrowIfRequested();
            IndexRevert = (objectIndex, key, flags);
            return 0;
        }

        public int RevertState(string playerName, uint key, ulong flags)
        {
            ThrowIfRequested();
            NameRevert = (playerName, key, flags);
            return 0;
        }

        public int SetItem(int objectIndex, byte slot, ulong itemId, IReadOnlyList<byte> stains, uint key, ulong flags)
        {
            ThrowIfRequested();
            LastStains = stains;
            IndexItem = (objectIndex, slot, itemId, key, flags);
            return 0;
        }

        public int SetItem(string playerName, byte slot, ulong itemId, IReadOnlyList<byte> stains, uint key, ulong flags)
        {
            ThrowIfRequested();
            LastStains = stains;
            NameItem = (playerName, slot, itemId, key, flags);
            return 0;
        }

        public int SetMetaState(int objectIndex, ulong metaFlags, bool enabled, uint key, ulong flags)
        {
            ThrowIfRequested();
            IndexMetaState = (objectIndex, metaFlags, enabled, key, flags);
            return 0;
        }

        public int SetMetaState(string playerName, ulong metaFlags, bool enabled, uint key, ulong flags)
        {
            ThrowIfRequested();
            NameMetaState = (playerName, metaFlags, enabled, key, flags);
            return 0;
        }

        public void RaiseInitialized()
            => Initialized?.Invoke();

        public void RaiseDisposed()
            => Disposed?.Invoke();

        public void RaiseStateFinalized(nint actor, int finalizationType)
            => stateFinalized?.Invoke(actor, finalizationType);

        public void Dispose()
            => WasDisposed = true;

        private void ThrowIfRequested()
        {
            if (OperationException is not null)
            {
                throw OperationException;
            }
        }
    }
}
