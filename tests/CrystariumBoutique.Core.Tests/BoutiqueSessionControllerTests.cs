using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Integrations;
using CrystariumBoutique.Core.Loadouts;
using CrystariumBoutique.Core.Sessions;

namespace CrystariumBoutique.Core.Tests;

public sealed class BoutiqueSessionControllerTests
{
    [Fact]
    public void OpenWhenDependencyUnavailableEntersDegradedState()
    {
        var service = new FakeAppearanceService { IsAvailable = false };
        var controller = new BoutiqueSessionController(service);

        var result = controller.Open();

        Assert.False(result.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.DependencyUnavailable, result.Error?.Code);
        Assert.Equal(BoutiqueSessionState.DependencyUnavailable, controller.Session.State);
    }

    [Fact]
    public void OpenWhenCaptureSucceedsActivatesSession()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);

        var result = controller.Open();

        Assert.True(result.IsSuccess);
        Assert.Equal(BoutiqueSessionState.Active, controller.Session.State);
        Assert.Equal(service.Snapshot, controller.Session.OriginalAppearance);
    }

    [Fact]
    public void OpenPopulatesWardrobeFromCapturedEquipmentAndStains()
    {
        var service = new FakeAppearanceService
        {
            Snapshot = AppearanceSnapshot.Create(
                "original",
                "test",
                equipment:
                [
                    new CapturedEquipmentState(
                        EquipmentSlot.Head,
                        44820,
                        [new StainId(17), new StainId(42)]),
                ]),
        };
        var controller = new BoutiqueSessionController(service);

        var result = controller.Open();

        Assert.True(result.IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var head));
        Assert.Equal((uint)44820, head.Appearance.SourceItemId);
        Assert.Equal(new StainId(17), head.GetStain(0));
        Assert.Equal(new StainId(42), head.GetStain(1));
        Assert.Equal(0, controller.ChangedSlotCount);
    }

    [Fact]
    public void InitialGameStainsSeedActiveDyesForAReplacementItem()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();

        var result = controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(900), 900),
            2,
            "Replacement Hat");

        Assert.True(result.IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var replacement));
        Assert.Equal(new StainId(17), replacement.GetStain(0));
        Assert.Equal(new StainId(42), replacement.GetStain(1));
    }

    [Fact]
    public void InitialGameStainsRespectReplacementDyeChannelSupport()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();

        var result = controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(900), 900),
            1,
            "Single-Dye Replacement Hat");

        Assert.True(result.IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var replacement));
        Assert.Equal(new StainId(17), replacement.GetStain(0));
        Assert.Equal(StainId.None, replacement.GetStain(1));
    }

    [Fact]
    public void ClearingCapturedEquipmentEmptiesOnlyThatWardrobeSlot()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();

        var result = controller.ClearEquipment(EquipmentSlot.Head);

        Assert.True(result.IsSuccess);
        Assert.False(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out _));
        var application = Assert.Single(service.EquipmentApplications);
        Assert.Equal(EquipmentSlot.Head, application.Slot);
        Assert.Equal(AppearanceId.None, application.Appearance.AppearanceId);
        Assert.Equal((uint)0, application.Appearance.SourceItemId);
    }

    [Fact]
    public void ClearingOneDyeChannelLeavesTheOtherChannelIntact()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();

        var result = controller.PreviewDye(EquipmentSlot.Head, 1, StainId.None);

        Assert.True(result.IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var head));
        Assert.Equal(new StainId(17), head.GetStain(0));
        Assert.Equal(StainId.None, head.GetStain(1));
    }

    [Fact]
    public void UserSelectedDyeSurvivesEquipmentReplacementAndClear()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewDye(EquipmentSlot.Head, 0, new StainId(55));
        controller.ClearEquipment(EquipmentSlot.Head);

        var result = controller.PreviewEquipment(
            EquipmentSlot.Head,
            new AppearanceSelection(
                new AppearanceId(900),
                900,
                [new StainId(3), new StainId(4)]),
            2,
            "Replacement Hat");

        Assert.True(result.IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var head));
        Assert.Equal(new StainId(55), head.GetStain(0));
        Assert.Equal(new StainId(42), head.GetStain(1));
    }

    [Fact]
    public void RevertAndActorTransferPreserveAnExplicitlyClearedCapturedSlot()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.ClearEquipment(EquipmentSlot.Head);
        controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(901), 901),
            displayName: "Preview Body");

        var undo = controller.UndoLastEquipmentPreview();
        service.EquipmentApplications.Clear();
        var transfer = controller.TransferPreviewToCurrentActor();

        Assert.True(undo.IsSuccess);
        Assert.True(transfer.IsSuccess);
        Assert.False(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out _));
        Assert.Contains(
            service.EquipmentApplications,
            application => application.Slot == EquipmentSlot.Head
                && application.Appearance.AppearanceId == AppearanceId.None);
    }

    [Theory]
    [InlineData(55, 0)]
    [InlineData(0, 66)]
    [InlineData(55, 66)]
    public void ClearAllDyeSlotsClearsPersistentOverridesAndLeavesEquipmentSelected(
        byte firstStain,
        byte secondStain)
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        if (firstStain != 0)
        {
            controller.PreviewDye(EquipmentSlot.Head, 0, new StainId(firstStain));
        }

        if (secondStain != 0)
        {
            controller.PreviewDye(EquipmentSlot.Head, 1, new StainId(secondStain));
        }

        var result = controller.ClearAllDyeSlots();

        Assert.True(result.IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var head));
        Assert.Equal((uint)44820, head.Appearance.SourceItemId);
        Assert.Equal(StainId.None, head.GetStain(0));
        Assert.Equal(StainId.None, head.GetStain(1));

        controller.PreviewEquipment(
            EquipmentSlot.Head,
            new AppearanceSelection(
                new AppearanceId(900),
                900,
                [new StainId(3), new StainId(4)]),
            2,
            "Replacement Hat");
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var replacement));
        Assert.Equal(new StainId(3), replacement.GetStain(0));
        Assert.Equal(new StainId(4), replacement.GetStain(1));
    }

    [Fact]
    public void ClearAllDyeSlotsUsesTheWorkingPerChannelClearPath()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewDye(EquipmentSlot.Head, 0, new StainId(55));
        controller.PreviewDye(EquipmentSlot.Head, 1, new StainId(66));
        service.DyeApplications.Clear();

        var result = controller.ClearAllDyeSlots();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, service.DyeApplications.Count);
        Assert.Equal(StainId.None, service.DyeApplications[0].Appearance.Stains[0]);
        Assert.Equal(new StainId(66), service.DyeApplications[0].Appearance.Stains[1]);
        Assert.Equal(StainId.None, service.DyeApplications[1].Appearance.Stains[0]);
        Assert.Equal(StainId.None, service.DyeApplications[1].Appearance.Stains[1]);
    }

    [Fact]
    public void ClearAllDyeSlotsClearsGameLoadedStainsAndPreservesOriginalBaseline()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        var original = controller.Session.OriginalAppearance;
        service.DyeApplications.Clear();

        var clear = controller.ClearAllDyeSlots();

        Assert.True(clear.IsSuccess);
        Assert.Equal(2, service.DyeApplications.Count);
        Assert.All(
            service.DyeApplications,
            application => Assert.Equal(EquipmentSlot.Head, application.Slot));
        Assert.Equal(StainId.None, service.DyeApplications[0].Appearance.Stains[0]);
        Assert.Equal(new StainId(42), service.DyeApplications[0].Appearance.Stains[1]);
        Assert.Equal(StainId.None, service.DyeApplications[1].Appearance.Stains[0]);
        Assert.Equal(StainId.None, service.DyeApplications[1].Appearance.Stains[1]);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var head));
        Assert.Equal((uint)44820, head.Appearance.SourceItemId);
        Assert.Equal(StainId.None, head.GetStain(0));
        Assert.Equal(StainId.None, head.GetStain(1));
        Assert.Equal(original, controller.Session.OriginalAppearance);
        Assert.Equal(new StainId(17), original!.Equipment[0].Stains[0]);
        Assert.Equal(new StainId(42), original.Equipment[0].Stains[1]);
    }

    [Fact]
    public void ClearAllDyeSlotsClearsMixedGameAndBoutiqueDyeOrigins()
    {
        var service = new FakeAppearanceService
        {
            Snapshot = AppearanceSnapshot.Create(
                "original",
                "test",
                equipment:
                [
                    new CapturedEquipmentState(
                        EquipmentSlot.Head,
                        44820,
                        [new StainId(17), new StainId(42)]),
                    new CapturedEquipmentState(
                        EquipmentSlot.Body,
                        51162,
                        [StainId.None, StainId.None]),
                ]),
        };
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewDye(EquipmentSlot.Body, 0, new StainId(66));
        service.DyeApplications.Clear();

        var clear = controller.ClearAllDyeSlots();

        Assert.True(clear.IsSuccess);
        Assert.Equal(3, service.DyeApplications.Count);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var head));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out var body));
        Assert.Equal(StainId.None, head.GetStain(0));
        Assert.Equal(StainId.None, head.GetStain(1));
        Assert.Equal(StainId.None, body.GetStain(0));

        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(900), 900),
            2,
            "Replacement Hat");
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var replacement));
        Assert.Equal(StainId.None, replacement.GetStain(0));
        Assert.Equal(StainId.None, replacement.GetStain(1));
    }

    [Fact]
    public void ClearAllDyeSlotsClearsOverridesAcrossMultipleEquipmentSlots()
    {
        var service = new FakeAppearanceService
        {
            Snapshot = AppearanceSnapshot.Create(
                "original",
                "test",
                equipment:
                [
                    new CapturedEquipmentState(
                        EquipmentSlot.Head,
                        44820,
                        [StainId.None, StainId.None]),
                    new CapturedEquipmentState(
                        EquipmentSlot.Body,
                        51162,
                        [StainId.None, StainId.None]),
                ]),
        };
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewDye(EquipmentSlot.Head, 0, new StainId(55));
        controller.PreviewDye(EquipmentSlot.Body, 0, new StainId(66));
        service.DyeApplications.Clear();

        var clear = controller.ClearAllDyeSlots();

        Assert.True(clear.IsSuccess);
        Assert.Equal(2, service.DyeApplications.Count);
        Assert.Collection(
            service.DyeApplications,
            application =>
            {
                Assert.Equal(EquipmentSlot.Head, application.Slot);
                Assert.Equal(StainId.None, application.Appearance.Stains[0]);
            },
            application =>
            {
                Assert.Equal(EquipmentSlot.Body, application.Slot);
                Assert.Equal(StainId.None, application.Appearance.Stains[0]);
            });
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var head));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out var body));
        Assert.Equal((uint)44820, head.Appearance.SourceItemId);
        Assert.Equal((uint)51162, body.Appearance.SourceItemId);
        Assert.Equal(StainId.None, head.GetStain(0));
        Assert.Equal(StainId.None, body.GetStain(0));
    }

    [Fact]
    public void ClearAllDyeSlotsRemovesOverrideForAnEmptyWardrobeCard()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewDye(EquipmentSlot.Head, 0, new StainId(55));
        controller.ClearEquipment(EquipmentSlot.Head);

        var clear = controller.ClearAllDyeSlots();
        var replace = controller.PreviewEquipment(
            EquipmentSlot.Head,
            new AppearanceSelection(
                new AppearanceId(900),
                900,
                [new StainId(3), new StainId(4)]),
            2,
            "Replacement Hat");

        Assert.True(clear.IsSuccess);
        Assert.True(replace.IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var replacement));
        Assert.Equal(new StainId(3), replacement.GetStain(0));
        Assert.Equal(new StainId(4), replacement.GetStain(1));
    }

    [Fact]
    public void ClearAllDyeSlotsRestoresEquipmentAndOverridesWhenAChannelFails()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewDye(EquipmentSlot.Head, 0, new StainId(55));
        controller.PreviewDye(EquipmentSlot.Head, 1, new StainId(66));
        var previousPreview = controller.Session.PreviewAppearance;
        service.FailOnDyeCall = service.ApplyDyeCallCount + 2;

        var clear = controller.ClearAllDyeSlots();

        Assert.False(clear.IsSuccess);
        Assert.Equal(previousPreview, service.AppliedSnapshots[^1]);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var restored));
        Assert.Equal(new StainId(55), restored.GetStain(0));
        Assert.Equal(new StainId(66), restored.GetStain(1));

        service.FailOnDyeCall = null;
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(900), 900),
            2,
            "Replacement Hat");
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var replacement));
        Assert.Equal(new StainId(55), replacement.GetStain(0));
        Assert.Equal(new StainId(66), replacement.GetStain(1));
    }

    [Fact]
    public void ResetAllImmediatelyRestoresCapturedWardrobeEquipmentAndStains()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewDye(EquipmentSlot.Head, 0, new StainId(55));
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(900), 900),
            2,
            "Replacement Hat");

        var result = controller.ResetAll();

        Assert.True(result.IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var head));
        Assert.Equal((uint)44820, head.Appearance.SourceItemId);
        Assert.Equal(new StainId(17), head.GetStain(0));
        Assert.Equal(new StainId(42), head.GetStain(1));

        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(901), 901),
            2,
            "Another Hat");
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var replacement));
        Assert.Equal(new StainId(55), replacement.GetStain(0));
    }

    [Fact]
    public void ResetAllRepopulatesMultipleClearedWardrobeCardsFromCapturedState()
    {
        var service = new FakeAppearanceService
        {
            Snapshot = AppearanceSnapshot.Create(
                "original",
                "test",
                equipment:
                [
                    new CapturedEquipmentState(
                        EquipmentSlot.Head,
                        44820,
                        [new StainId(17), new StainId(42)]),
                    new CapturedEquipmentState(
                        EquipmentSlot.Body,
                        51162,
                        [new StainId(8), StainId.None]),
                ]),
        };
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.ClearEquipment(EquipmentSlot.Head);
        controller.ClearEquipment(EquipmentSlot.Body);

        var result = controller.ResetAll();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, service.RevertCallCount);
        Assert.Equal(2, service.EquipmentApplications.Count);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var head));
        Assert.Equal((uint)44820, head.Appearance.SourceItemId);
        Assert.Equal(new StainId(17), head.GetStain(0));
        Assert.Equal(new StainId(42), head.GetStain(1));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out var body));
        Assert.Equal((uint)51162, body.Appearance.SourceItemId);
        Assert.Equal(new StainId(8), body.GetStain(0));
        Assert.Equal(StainId.None, body.GetStain(1));
    }

    [Fact]
    public void RefreshSlotsReplacesBaselineAndWardrobeWithCurrentGameState()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.ClearEquipment(EquipmentSlot.Head);
        var refreshed = AppearanceSnapshot.Create(
            "refreshed",
            "test",
            equipment:
            [
                new CapturedEquipmentState(
                    EquipmentSlot.MainHand,
                    60001,
                    [new StainId(9), new StainId(10)]),
                new CapturedEquipmentState(
                    EquipmentSlot.OffHand,
                    60004,
                    [new StainId(13), new StainId(14)]),
                new CapturedEquipmentState(
                    EquipmentSlot.Body,
                    60002,
                    [new StainId(11), new StainId(12)]),
            ]);
        service.NextCaptureSnapshot = refreshed;

        var result = controller.RefreshSlots();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, service.RevertCallCount);
        Assert.Equal(refreshed, controller.Session.OriginalAppearance);
        Assert.Equal(refreshed, controller.Session.PreviewAppearance);
        Assert.False(controller.Session.IsDirty);
        Assert.False(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out _));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out var mainHand));
        Assert.Equal((uint)60001, mainHand.Appearance.SourceItemId);
        Assert.Equal(new StainId(9), mainHand.GetStain(0));
        Assert.Equal(new StainId(10), mainHand.GetStain(1));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.OffHand, out var offHand));
        Assert.Equal((uint)60004, offHand.Appearance.SourceItemId);
        Assert.Equal(new StainId(13), offHand.GetStain(0));
        Assert.Equal(new StainId(14), offHand.GetStain(1));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out var body));
        Assert.Equal((uint)60002, body.Appearance.SourceItemId);
        Assert.Equal(new StainId(11), body.GetStain(0));
        Assert.Equal(new StainId(12), body.GetStain(1));

        controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(60003), 60003),
            2,
            "Refreshed Replacement Body");
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out var replacement));
        Assert.Equal(new StainId(11), replacement.GetStain(0));
        Assert.Equal(new StainId(12), replacement.GetStain(1));
    }

    [Fact]
    public void ClearAllDyeSlotsAfterRefreshKeepsSynchronizedEquipmentAndBaseline()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        var refreshed = AppearanceSnapshot.Create(
            "gearset",
            "test",
            equipment:
            [
                new CapturedEquipmentState(
                    EquipmentSlot.Head,
                    60005,
                    [new StainId(21), new StainId(22)]),
            ]);
        service.NextCaptureSnapshot = refreshed;

        Assert.True(controller.RelinquishPreviewForSynchronization().IsSuccess);
        Assert.True(controller.CaptureSynchronizedGameState().IsSuccess);
        Assert.True(controller.ClearAllDyeSlots().IsSuccess);

        Assert.Equal(refreshed, controller.Session.OriginalAppearance);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var head));
        Assert.Equal((uint)60005, head.Appearance.SourceItemId);
        Assert.Equal(StainId.None, head.GetStain(0));
        Assert.Equal(StainId.None, head.GetStain(1));

        Assert.True(controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(60006), 60006),
            2,
            "Replacement After Clear").IsSuccess);
        Assert.Equal(StainId.None, controller.PreviewEquipmentBySlot[EquipmentSlot.Head].GetStain(0));
        Assert.Equal(StainId.None, controller.PreviewEquipmentBySlot[EquipmentSlot.Head].GetStain(1));
    }

    [Fact]
    public void ConfirmedPreviewRelinquishCapturesNewGearsetAndMakesItCloseBaseline()
    {
        var service = CreateServiceWithCapturedHead();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(70001), 70001),
            2,
            "Temporary Boutique Head");
        controller.PreviewDye(EquipmentSlot.Head, 0, new StainId(55));
        var gearset = AppearanceSnapshot.Create(
            "gearset-b",
            "test",
            equipment:
            [
                new CapturedEquipmentState(
                    EquipmentSlot.MainHand,
                    70002,
                    [new StainId(31), new StainId(32)]),
                new CapturedEquipmentState(
                    EquipmentSlot.OffHand,
                    70003,
                    [new StainId(33), new StainId(34)]),
                new CapturedEquipmentState(
                    EquipmentSlot.Head,
                    70004,
                    [new StainId(35), new StainId(36)]),
            ]);
        service.NextCaptureSnapshot = gearset;

        var relinquish = controller.RelinquishPreviewForSynchronization();

        Assert.True(relinquish.IsSuccess);
        Assert.Equal(1, service.RevertCallCount);
        Assert.NotEqual(gearset, controller.Session.OriginalAppearance);
        Assert.True(controller.Session.IsDirty);

        var capture = controller.CaptureSynchronizedGameState();

        Assert.True(capture.IsSuccess);
        Assert.Equal(gearset, controller.Session.OriginalAppearance);
        Assert.Equal(gearset, controller.Session.PreviewAppearance);
        Assert.False(controller.Session.IsDirty);
        Assert.DoesNotContain(
            controller.PreviewEquipmentBySlot.Values,
            equipment => equipment.Appearance.SourceItemId == 70001);
        Assert.Equal((uint)70002, controller.PreviewEquipmentBySlot[EquipmentSlot.MainHand].Appearance.SourceItemId);
        Assert.Equal((uint)70003, controller.PreviewEquipmentBySlot[EquipmentSlot.OffHand].Appearance.SourceItemId);
        Assert.Equal(new StainId(35), controller.PreviewEquipmentBySlot[EquipmentSlot.Head].GetStain(0));
        Assert.Equal(new StainId(36), controller.PreviewEquipmentBySlot[EquipmentSlot.Head].GetStain(1));

        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(70005), 70005),
            2,
            "Post-Sync Boutique Head");
        Assert.Equal(new StainId(35), controller.PreviewEquipmentBySlot[EquipmentSlot.Head].GetStain(0));
        Assert.Equal(new StainId(36), controller.PreviewEquipmentBySlot[EquipmentSlot.Head].GetStain(1));

        Assert.True(controller.Close().IsSuccess);
        Assert.Equal(gearset, service.RestoredSnapshot);
    }

    [Fact]
    public void RetryOpenAfterDependencyBecomesAvailableActivatesBrowseOnlySession()
    {
        var service = new FakeAppearanceService { IsAvailable = false };
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        service.IsAvailable = true;

        var result = controller.RetryOpenAfterDependencyAvailable();

        Assert.True(result.IsSuccess);
        Assert.Equal(BoutiqueSessionState.Active, controller.Session.State);
        Assert.Equal(service.Snapshot, controller.Session.OriginalAppearance);
        Assert.Equal(1, service.CaptureCallCount);
    }

    [Fact]
    public void RetryOpenAfterActiveDependencyLossRestoresBeforeStartingFreshSession()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(42), 420));
        service.IsAvailable = false;
        var interrupted = controller.MarkDependencyUnavailable(
            "Expected dependency interruption.");
        service.IsAvailable = true;

        var recovered = controller.RetryOpenAfterDependencyAvailable();

        Assert.True(interrupted.IsSuccess);
        Assert.True(recovered.IsSuccess);
        Assert.Equal(1, service.RestoreCallCount);
        Assert.Equal(service.Snapshot, service.RestoredSnapshot);
        Assert.Equal(BoutiqueSessionState.Active, controller.Session.State);
        Assert.Equal(0, controller.ChangedSlotCount);
        Assert.Equal(3, service.CaptureCallCount);
    }

    [Fact]
    public void CloseDuringDependencyLossPreservesRestoreFailureForLaterRetry()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(7), 70));
        controller.MarkDependencyUnavailable("Expected dependency interruption.");
        service.RestoreShouldFail = true;

        var result = controller.Close();

        Assert.False(result.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.RestoreFailed, result.Error?.Code);
        Assert.Equal(BoutiqueSessionState.RestoreFailed, controller.Session.State);
        Assert.Equal(1, controller.ChangedSlotCount);
    }

    [Fact]
    public void CloseActiveSessionRestoresOriginalAppearance()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();

        var result = controller.Close();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, service.RestoreCallCount);
        Assert.Equal(BoutiqueSessionState.Closed, controller.Session.State);
    }

    [Fact]
    public void CloseWhenRestoreFailsPreservesFailureState()
    {
        var service = new FakeAppearanceService { RestoreShouldFail = true };
        var controller = new BoutiqueSessionController(service);
        controller.Open();

        var result = controller.Close();

        Assert.False(result.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.RestoreFailed, result.Error?.Code);
        Assert.Equal(BoutiqueSessionState.RestoreFailed, controller.Session.State);
    }

    [Fact]
    public void CloseRetriesRestorationAfterPriorFailure()
    {
        var service = new FakeAppearanceService { RestoreShouldFail = true };
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        var first = controller.Close();
        service.RestoreShouldFail = false;

        var retry = controller.Close();

        Assert.False(first.IsSuccess);
        Assert.True(retry.IsSuccess);
        Assert.Equal(2, service.RestoreCallCount);
        Assert.Equal(service.Snapshot, service.RestoredSnapshot);
        Assert.Equal(BoutiqueSessionState.Closed, controller.Session.State);
    }

    [Fact]
    public void RepeatedEquipmentPreviewsUpdatePreviewAndPreserveOriginalForRestoration()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        var original = controller.Session.OriginalAppearance;

        var first = controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(100), 100));
        var firstPreview = controller.Session.PreviewAppearance;
        var second = controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(200), 200));
        var secondPreview = controller.Session.PreviewAppearance;
        var close = controller.Close();

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(close.IsSuccess);
        Assert.Equal(2, service.ApplyEquipmentCallCount);
        Assert.NotEqual(original, firstPreview);
        Assert.NotEqual(firstPreview, secondPreview);
        Assert.Equal(original, service.RestoredSnapshot);
        Assert.Equal(BoutiqueSessionState.Closed, controller.Session.State);
    }

    [Fact]
    public void UndoEquipmentPreviewRestoresThePreviousSelectedItem()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(101), 101),
            1,
            "First Hat",
            501);
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(102), 102),
            2,
            "Second Hat",
            502);

        var result = controller.UndoLastEquipmentPreview();

        Assert.True(result.IsSuccess);
        Assert.True(controller.CanUndoEquipmentPreview);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var restored));
        Assert.Equal("First Hat", restored.DisplayName);
        Assert.Equal((uint)101, restored.Appearance.SourceItemId);
        Assert.Equal(1, service.RevertCallCount);
        Assert.Equal((uint)101, service.EquipmentApplications[^1].Appearance.SourceItemId);
    }

    [Fact]
    public void UndoFirstEquipmentPreviewReturnsToTheCapturedGameAppearance()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(103), 103));

        var result = controller.UndoLastEquipmentPreview();

        Assert.True(result.IsSuccess);
        Assert.False(controller.CanUndoEquipmentPreview);
        Assert.Empty(controller.PreviewEquipmentBySlot);
        Assert.Equal(service.Snapshot, controller.Session.PreviewAppearance);
        Assert.Equal(1, service.RevertCallCount);
    }

    [Fact]
    public void DyeChangesDoNotCreateSeparateEquipmentUndoSteps()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Hands,
            AppearanceSelection.WithoutStains(new AppearanceId(104), 104),
            1,
            "First Gloves");
        controller.PreviewEquipment(
            EquipmentSlot.Hands,
            AppearanceSelection.WithoutStains(new AppearanceId(105), 105),
            1,
            "Second Gloves");
        controller.PreviewDye(EquipmentSlot.Hands, 0, new StainId(7));

        var firstUndo = controller.UndoLastEquipmentPreview();
        var secondUndo = controller.UndoLastEquipmentPreview();

        Assert.True(firstUndo.IsSuccess);
        Assert.True(secondUndo.IsSuccess);
        Assert.Empty(controller.PreviewEquipmentBySlot);
        Assert.False(controller.CanUndoEquipmentPreview);
    }

    [Fact]
    public void FailedUndoKeepsCurrentEquipmentAndHistory()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Feet,
            AppearanceSelection.WithoutStains(new AppearanceId(106), 106),
            displayName: "First Boots");
        controller.PreviewEquipment(
            EquipmentSlot.Feet,
            AppearanceSelection.WithoutStains(new AppearanceId(107), 107),
            displayName: "Second Boots");
        service.ApplyEquipmentShouldFail = true;

        var result = controller.UndoLastEquipmentPreview();

        Assert.False(result.IsSuccess);
        Assert.True(controller.CanUndoEquipmentPreview);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Feet, out var current));
        Assert.Equal("Second Boots", current.DisplayName);
        Assert.NotEmpty(service.AppliedSnapshots);
    }

    [Fact]
    public void UndoLinkedWeaponRestoresTheCompletePriorWeaponState()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.MainHand,
            AppearanceSelection.WithoutStains(new AppearanceId(108), 108),
            displayName: "Standalone Weapon");
        controller.PreviewEquipment(
            EquipmentSlot.MainHand,
            AppearanceSelection.WithoutStains(new AppearanceId(109), 109),
            displayName: "Linked Weapon",
            hasLinkedOffHandComponent: true);
        service.EquipmentApplications.Clear();

        var result = controller.UndoLastEquipmentPreview();

        Assert.True(result.IsSuccess);
        var application = Assert.Single(service.EquipmentApplications);
        Assert.Equal(EquipmentSlot.MainHand, application.Slot);
        Assert.Equal((uint)108, application.Appearance.SourceItemId);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out var restored));
        Assert.Equal("Standalone Weapon", restored.DisplayName);
        Assert.False(controller.TryGetPreviewEquipment(EquipmentSlot.OffHand, out _));
    }

    [Fact]
    public void LinkedWeaponPreviewAppliesAndTracksMatchingMainAndOffHandComponents()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        var appearance = AppearanceSelection.WithoutStains(new AppearanceId(2100), 2100);

        var result = controller.PreviewEquipment(
            EquipmentSlot.MainHand,
            appearance,
            2,
            "Test Katana",
            700,
            hasLinkedOffHandComponent: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, service.RevertCallCount);
        Assert.Empty(service.AppliedSnapshots);
        Assert.Equal(
            [EquipmentSlot.MainHand, EquipmentSlot.OffHand],
            service.EquipmentApplications.Select(application => application.Slot));
        Assert.Equal(2, controller.ChangedSlotCount);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out var mainHand));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.OffHand, out var offHand));
        Assert.Equal(appearance, mainHand.Appearance);
        Assert.Equal(appearance, offHand.Appearance);
        Assert.Equal("Test Katana", offHand.DisplayName);
    }

    [Fact]
    public void StandaloneShieldPreviewChangesOnlyOffHandAndPreservesMainHandState()
    {
        var service = new FakeAppearanceService
        {
            Snapshot = AppearanceSnapshot.Create(
                "paladin-original",
                "test",
                equipment:
                [
                    new CapturedEquipmentState(
                        EquipmentSlot.MainHand,
                        50001,
                        [new StainId(3), StainId.None]),
                    new CapturedEquipmentState(
                        EquipmentSlot.OffHand,
                        50002,
                        [new StainId(4), StainId.None]),
                ]),
        };
        var controller = new BoutiqueSessionController(service);
        Assert.True(controller.Open().IsSuccess);
        service.EquipmentApplications.Clear();

        var result = controller.PreviewEquipment(
            EquipmentSlot.OffHand,
            AppearanceSelection.WithoutStains(new AppearanceId(51131), 51131),
            1,
            "Augmented Courtly Lover's Shield",
            12345);

        Assert.True(result.IsSuccess);
        var application = Assert.Single(service.EquipmentApplications);
        Assert.Equal(EquipmentSlot.OffHand, application.Slot);
        Assert.Equal((uint)51131, application.Appearance.SourceItemId);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out var sword));
        Assert.Equal((uint)50001, sword.Appearance.SourceItemId);
        Assert.Equal(new StainId(3), sword.GetStain(0));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.OffHand, out var shield));
        Assert.Equal((uint)51131, shield.Appearance.SourceItemId);
        Assert.Equal("Augmented Courtly Lover's Shield", shield.DisplayName);
        Assert.Equal(1, controller.ChangedSlotCount);
    }

    [Theory]
    [InlineData(51131u, "Paladin Favorite Shield")]
    [InlineData(50750u, "Beastmaster Favorite Shield")]
    public void SharedFavoritesRevertRestoresPriorShieldWithoutChangingMainHand(
        uint favoriteShieldItemId,
        string favoriteShieldName)
    {
        var service = new FakeAppearanceService
        {
            Snapshot = AppearanceSnapshot.Create(
                "independent-shield-original",
                "test",
                equipment:
                [
                    new CapturedEquipmentState(
                        EquipmentSlot.MainHand,
                        50001,
                        [new StainId(3), StainId.None]),
                    new CapturedEquipmentState(
                        EquipmentSlot.OffHand,
                        50002,
                        [new StainId(4), StainId.None]),
                ]),
        };
        var controller = new BoutiqueSessionController(service);
        Assert.True(controller.Open().IsSuccess);
        Assert.True(controller.PreviewEquipment(
            EquipmentSlot.OffHand,
            AppearanceSelection.WithoutStains(
                new AppearanceId(favoriteShieldItemId),
                favoriteShieldItemId),
            1,
            favoriteShieldName).IsSuccess);

        var result = controller.UndoLastEquipmentPreview();

        Assert.True(result.IsSuccess);
        Assert.False(controller.CanUndoEquipmentPreview);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out var mainHand));
        Assert.Equal((uint)50001, mainHand.Appearance.SourceItemId);
        Assert.Equal(new StainId(3), mainHand.GetStain(0));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.OffHand, out var offHand));
        Assert.Equal((uint)50002, offHand.Appearance.SourceItemId);
        Assert.Equal(new StainId(4), offHand.GetStain(0));
    }

    [Fact]
    public void StandaloneShieldDyeTargetsOnlyOffHandAndKeepsShieldEquipped()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        Assert.True(controller.Open().IsSuccess);
        Assert.True(controller.PreviewEquipment(
            EquipmentSlot.OffHand,
            AppearanceSelection.WithoutStains(new AppearanceId(51131), 51131),
            2,
            "Augmented Courtly Lover's Shield").IsSuccess);

        var result = controller.PreviewDye(EquipmentSlot.OffHand, 1, new StainId(42));

        Assert.True(result.IsSuccess);
        var application = Assert.Single(service.DyeApplications);
        Assert.Equal(EquipmentSlot.OffHand, application.Slot);
        Assert.Equal((uint)51131, application.Appearance.SourceItemId);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.OffHand, out var shield));
        Assert.Equal(new StainId(42), shield.GetStain(1));
        Assert.DoesNotContain(
            service.DyeApplications,
            dye => dye.Slot == EquipmentSlot.MainHand);
    }

    [Fact]
    public void ReplacingLinkedWeaponWithStandaloneMainHandRestoresCounterpart()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.MainHand,
            AppearanceSelection.WithoutStains(new AppearanceId(2200), 2200),
            hasLinkedOffHandComponent: true);
        service.AppliedSnapshots.Clear();
        service.EquipmentApplications.Clear();

        var result = controller.PreviewEquipment(
            EquipmentSlot.MainHand,
            AppearanceSelection.WithoutStains(new AppearanceId(2201), 2201),
            displayName: "Standalone Weapon");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, service.RevertCallCount);
        Assert.Empty(service.AppliedSnapshots);
        var application = Assert.Single(service.EquipmentApplications);
        Assert.Equal(EquipmentSlot.MainHand, application.Slot);
        Assert.Equal((uint)2201, application.Appearance.SourceItemId);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out _));
        Assert.False(controller.TryGetPreviewEquipment(EquipmentSlot.OffHand, out _));
        Assert.Equal(1, controller.ChangedSlotCount);
    }

    [Fact]
    public void LinkedWeaponFailureRollsBackAndKeepsPriorPreviewState()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(2300), 2300),
            displayName: "Prior Hat");
        var previousPreview = controller.Session.PreviewAppearance;
        service.FailOnEquipmentCall = service.ApplyEquipmentCallCount + 2;

        var result = controller.PreviewEquipment(
            EquipmentSlot.MainHand,
            AppearanceSelection.WithoutStains(new AppearanceId(2301), 2301),
            displayName: "Failed Pair",
            hasLinkedOffHandComponent: true);

        Assert.False(result.IsSuccess);
        Assert.Equal(1, service.RevertCallCount);
        Assert.Equal(previousPreview, Assert.Single(service.AppliedSnapshots));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var head));
        Assert.Equal("Prior Hat", head.DisplayName);
        Assert.False(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out _));
        Assert.False(controller.TryGetPreviewEquipment(EquipmentSlot.OffHand, out _));
    }

    [Fact]
    public void EquipmentPreviewFailureDoesNotReplaceCapturedOriginal()
    {
        var service = new FakeAppearanceService { ApplyEquipmentShouldFail = true };
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        var original = controller.Session.OriginalAppearance;

        var result = controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(300), 300));

        Assert.False(result.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.AppearanceApplyFailed, result.Error?.Code);
        Assert.Equal(original, controller.Session.OriginalAppearance);
        Assert.Equal(original, controller.Session.PreviewAppearance);
    }

    [Fact]
    public void EquipmentPreviewOutsideActiveSessionIsRejectedWithoutCallingIntegration()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);

        var result = controller.PreviewEquipment(
            EquipmentSlot.Feet,
            AppearanceSelection.WithoutStains(new AppearanceId(400), 400));

        Assert.False(result.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.InvalidGameState, result.Error?.Code);
        Assert.Equal(0, service.ApplyEquipmentCallCount);
    }

    [Fact]
    public void PreviewCaptureFailureStillRestoresOriginalOnClose()
    {
        var service = new FakeAppearanceService { PreviewCaptureShouldFail = true };
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        var original = controller.Session.OriginalAppearance;

        var preview = controller.PreviewEquipment(
            EquipmentSlot.Hands,
            AppearanceSelection.WithoutStains(new AppearanceId(500), 500));
        var close = controller.Close();

        Assert.False(preview.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.AppearanceCaptureFailed, preview.Error?.Code);
        Assert.True(close.IsSuccess);
        Assert.Equal(original, service.RestoredSnapshot);
        Assert.Equal(BoutiqueSessionState.Closed, controller.Session.State);
    }

    [Fact]
    public void DyePreviewUpdatesOneChannelAndPreservesTheOther()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(600), 600),
            2,
            "Test Coat");

        var first = controller.PreviewDye(EquipmentSlot.Body, 0, new StainId(7));
        var second = controller.PreviewDye(EquipmentSlot.Body, 1, new StainId(42));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, service.ApplyDyeCallCount);
        Assert.Equal([new StainId(7), new StainId(42)], service.LastDyeAppearance!.Stains);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out var equipment));
        Assert.Equal("Test Coat", equipment.DisplayName);
        Assert.Equal(new StainId(7), equipment.GetStain(0));
        Assert.Equal(new StainId(42), equipment.GetStain(1));
    }

    [Fact]
    public void LinkedWeaponDyeAppliesAndTracksBothComponents()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.MainHand,
            AppearanceSelection.WithoutStains(new AppearanceId(2400), 2400),
            2,
            "Dyeable Pair",
            hasLinkedOffHandComponent: true);

        var result = controller.PreviewDye(EquipmentSlot.MainHand, 1, new StainId(42));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [EquipmentSlot.MainHand, EquipmentSlot.OffHand],
            service.DyeApplications.Select(application => application.Slot));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out var mainHand));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.OffHand, out var offHand));
        Assert.Equal(new StainId(42), mainHand.GetStain(1));
        Assert.Equal(new StainId(42), offHand.GetStain(1));
    }

    [Fact]
    public void LinkedWeaponDyeFailureRollsBackAndKeepsBothPriorChannelStates()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.MainHand,
            AppearanceSelection.WithoutStains(new AppearanceId(2500), 2500),
            1,
            "Dyeable Pair",
            hasLinkedOffHandComponent: true);
        controller.PreviewDye(EquipmentSlot.MainHand, 0, new StainId(7));
        var previousPreview = controller.Session.PreviewAppearance;
        service.FailOnDyeCall = service.ApplyDyeCallCount + 2;

        var result = controller.PreviewDye(EquipmentSlot.MainHand, 0, new StainId(9));

        Assert.False(result.IsSuccess);
        Assert.Equal(previousPreview, service.AppliedSnapshots[^1]);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out var mainHand));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.OffHand, out var offHand));
        Assert.Equal(new StainId(7), mainHand.GetStain(0));
        Assert.Equal(new StainId(7), offHand.GetStain(0));
    }

    [Fact]
    public void DyePreviewRejectsMissingItemAndUnavailableChannel()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();

        var missingItem = controller.PreviewDye(EquipmentSlot.Head, 0, new StainId(1));
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(700), 700),
            1);
        var missingChannel = controller.PreviewDye(EquipmentSlot.Head, 1, new StainId(2));

        Assert.False(missingItem.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.InvalidGameState, missingItem.Error?.Code);
        Assert.False(missingChannel.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.InvalidDye, missingChannel.Error?.Code);
        Assert.Equal(0, service.ApplyDyeCallCount);
    }

    [Fact]
    public void ClosingAfterDyePreviewRestoresOriginalAndClearsSlotState()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        var original = controller.Session.OriginalAppearance;
        controller.PreviewEquipment(
            EquipmentSlot.Hands,
            AppearanceSelection.WithoutStains(new AppearanceId(800), 800),
            1);
        controller.PreviewDye(EquipmentSlot.Hands, 0, new StainId(3));

        var close = controller.Close();

        Assert.True(close.IsSuccess);
        Assert.Equal(original, service.RestoredSnapshot);
        Assert.Empty(controller.PreviewEquipmentBySlot);
    }

    [Fact]
    public void FailedDyeApplyKeepsThePreviousChannelState()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Feet,
            AppearanceSelection.WithoutStains(new AppearanceId(900), 900),
            1);
        controller.PreviewDye(EquipmentSlot.Feet, 0, new StainId(4));
        service.ApplyDyeShouldFail = true;

        var failed = controller.PreviewDye(EquipmentSlot.Feet, 0, new StainId(5));

        Assert.False(failed.IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Feet, out var equipment));
        Assert.Equal(new StainId(4), equipment.GetStain(0));
    }

    [Fact]
    public void ResetSlotRestoresOriginalAndReplaysEveryOtherChangedSlot()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(1000), 1000),
            1,
            "Test Hat",
            10);
        controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(1001), 1001),
            2,
            "Test Coat",
            11);

        var reset = controller.ResetSlot(EquipmentSlot.Head);

        Assert.True(reset.IsSuccess);
        Assert.Equal(1, service.RevertCallCount);
        Assert.Empty(service.AppliedSnapshots);
        Assert.Equal(3, service.ApplyEquipmentCallCount);
        Assert.Equal(EquipmentSlot.Body, service.EquipmentApplications[^1].Slot);
        Assert.False(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out _));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out var body));
        Assert.Equal((uint)11, body.IconId);
        Assert.Equal(1, controller.ChangedSlotCount);
        Assert.True(controller.Session.IsDirty);
    }

    [Fact]
    public void ResetLinkedWeaponSlotRestoresBothComponentsAndReplaysOtherSlots()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.MainHand,
            AppearanceSelection.WithoutStains(new AppearanceId(2600), 2600),
            displayName: "Linked Pair",
            hasLinkedOffHandComponent: true);
        controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(2601), 2601),
            displayName: "Changed Body");
        service.AppliedSnapshots.Clear();
        service.EquipmentApplications.Clear();

        var result = controller.ResetSlot(EquipmentSlot.OffHand);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, service.RevertCallCount);
        Assert.Empty(service.AppliedSnapshots);
        Assert.Equal(EquipmentSlot.Body, Assert.Single(service.EquipmentApplications).Slot);
        Assert.False(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out _));
        Assert.False(controller.TryGetPreviewEquipment(EquipmentSlot.OffHand, out _));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out _));
        Assert.Equal(1, controller.ChangedSlotCount);
    }

    [Fact]
    public void ResetLastChangedSlotReturnsToCleanOriginalWithoutRecapture()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Hands,
            AppearanceSelection.WithoutStains(new AppearanceId(1100), 1100));
        var capturesBeforeReset = service.CaptureCallCount;

        var reset = controller.ResetSlot(EquipmentSlot.Hands);

        Assert.True(reset.IsSuccess);
        Assert.Empty(controller.PreviewEquipmentBySlot);
        Assert.Equal(service.Snapshot, controller.Session.PreviewAppearance);
        Assert.False(controller.Session.IsDirty);
        Assert.Equal(capturesBeforeReset, service.CaptureCallCount);
    }

    [Fact]
    public void ResetAllRestoresOriginalAndClearsCompleteChangedSlotState()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Feet,
            AppearanceSelection.WithoutStains(new AppearanceId(1200), 1200));
        controller.PreviewEquipment(
            EquipmentSlot.Ears,
            AppearanceSelection.WithoutStains(new AppearanceId(1201), 1201));

        var reset = controller.ResetAll();

        Assert.True(reset.IsSuccess);
        Assert.Equal(1, service.RevertCallCount);
        Assert.Empty(service.AppliedSnapshots);
        Assert.Empty(controller.PreviewEquipmentBySlot);
        Assert.Equal(service.Snapshot, controller.Session.PreviewAppearance);
        Assert.False(controller.Session.IsDirty);
    }

    [Fact]
    public void GposeTransferRevertsNewActorAndReplaysEveryChangedSlot()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(1210), 1210),
            displayName: "Transferred Hat");
        controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(1211), 1211),
            displayName: "Transferred Coat");
        service.EquipmentApplications.Clear();

        var transfer = controller.TransferPreviewToCurrentActor();

        Assert.True(transfer.IsSuccess);
        Assert.Equal(1, service.RevertCallCount);
        Assert.Equal(
            [EquipmentSlot.Head, EquipmentSlot.Body],
            service.EquipmentApplications.Select(application => application.Slot));
        Assert.Equal(2, controller.ChangedSlotCount);
        Assert.True(controller.Session.IsDirty);
    }

    [Fact]
    public void EmptyGposeTransferLeavesTheNewActorAtGameState()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();

        var transfer = controller.TransferPreviewToCurrentActor();

        Assert.True(transfer.IsSuccess);
        Assert.Equal(1, service.RevertCallCount);
        Assert.Empty(service.EquipmentApplications);
        Assert.Equal(service.Snapshot, controller.Session.PreviewAppearance);
        Assert.False(controller.Session.IsDirty);
    }

    [Fact]
    public void FailedGposeTransferCleansNewActorWithoutApplyingOldSnapshot()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(1212), 1212),
            displayName: "Prior Hat");
        service.ApplyEquipmentShouldFail = true;
        service.AppliedSnapshots.Clear();

        var transfer = controller.TransferPreviewToCurrentActor();

        Assert.False(transfer.IsSuccess);
        Assert.Equal(2, service.RevertCallCount);
        Assert.Empty(service.AppliedSnapshots);
        Assert.Equal(1, controller.ChangedSlotCount);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out _));
    }

    [Fact]
    public void FailedSlotReplayRollsBackAndKeepsChangedSlotState()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(1300), 1300));
        controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(1301), 1301));
        var previousPreview = controller.Session.PreviewAppearance;
        service.ApplyEquipmentShouldFail = true;

        var reset = controller.ResetSlot(EquipmentSlot.Head);

        Assert.False(reset.IsSuccess);
        Assert.Equal(1, service.RevertCallCount);
        Assert.Equal(previousPreview, Assert.Single(service.AppliedSnapshots));
        Assert.Equal(2, controller.ChangedSlotCount);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out _));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out _));
    }

    [Fact]
    public void FailedPostResetCaptureRollsBackAndKeepsChangedSlotState()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(1400), 1400));
        controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(1401), 1401));
        var previousPreview = controller.Session.PreviewAppearance;
        service.PreviewCaptureShouldFail = true;

        var reset = controller.ResetSlot(EquipmentSlot.Head);

        Assert.False(reset.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.AppearanceCaptureFailed, reset.Error?.Code);
        Assert.Equal(1, service.RevertCallCount);
        Assert.Equal(previousPreview, Assert.Single(service.AppliedSnapshots));
        Assert.Equal(2, controller.ChangedSlotCount);
    }

    [Fact]
    public void FailedResetAllKeepsChangedSlotStateAndDirtyPreview()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Legs,
            AppearanceSelection.WithoutStains(new AppearanceId(1500), 1500));
        service.RevertToGameShouldFail = true;

        var reset = controller.ResetAll();

        Assert.False(reset.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.AppearanceApplyFailed, reset.Error?.Code);
        Assert.Equal(1, controller.ChangedSlotCount);
        Assert.True(controller.Session.IsDirty);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Legs, out _));
    }

    [Fact]
    public void ResetOutsideActiveSessionIsRejectedWithoutCallingIntegration()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);

        var slot = controller.ResetSlot(EquipmentSlot.Head);
        var all = controller.ResetAll();

        Assert.False(slot.IsSuccess);
        Assert.False(all.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.InvalidGameState, slot.Error?.Code);
        Assert.Equal(BoutiqueErrorCode.InvalidGameState, all.Error?.Code);
        Assert.Empty(service.AppliedSnapshots);
        Assert.Equal(0, service.RevertCallCount);
    }

    [Fact]
    public void ApplyLoadoutRestoresOriginalThenReplacesCompleteChangedSlotState()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Feet,
            AppearanceSelection.WithoutStains(new AppearanceId(1600), 1600));
        var loadout = CreateLoadout(
            new LoadoutEquipmentState(
                EquipmentSlot.Head,
                new AppearanceSelection(
                    new AppearanceId(1601),
                    1601,
                    [new StainId(7), new StainId(42)]),
                2,
                "Saved Hat",
                501),
            new LoadoutEquipmentState(
                EquipmentSlot.Body,
                AppearanceSelection.WithoutStains(new AppearanceId(1602), 1602),
                1,
                "Saved Coat",
                502));
        service.NextCaptureSnapshot = AppearanceSnapshot.Create(
            "loaded-design",
            "test",
            equipment:
            [
                new CapturedEquipmentState(
                    EquipmentSlot.Head,
                    1601,
                    [new StainId(7), new StainId(42)]),
                new CapturedEquipmentState(
                    EquipmentSlot.Body,
                    1602,
                    [StainId.None, StainId.None]),
            ]);

        var result = controller.ApplyLoadout(loadout);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, service.RevertCallCount);
        Assert.Empty(service.AppliedSnapshots);
        Assert.Equal(
            [EquipmentSlot.Head, EquipmentSlot.Body],
            service.EquipmentApplications.TakeLast(2).Select(item => item.Slot));
        Assert.Equal(2, controller.ChangedSlotCount);
        Assert.False(controller.TryGetPreviewEquipment(EquipmentSlot.Feet, out _));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var head));
        Assert.Equal(new StainId(42), head.GetStain(1));
        Assert.Equal((uint)501, head.IconId);
        Assert.True(controller.Session.IsDirty);
    }

    [Fact]
    public void ApplyLoadoutAdoptsActualCrossJobWeaponAndDesignDyesThenClearAllClearsThem()
    {
        var original = AppearanceSnapshot.Create(
            "viper-original",
            "test",
            equipment:
            [
                new CapturedEquipmentState(EquipmentSlot.MainHand, 4100, [StainId.None, StainId.None]),
                new CapturedEquipmentState(EquipmentSlot.OffHand, 4101, [StainId.None, StainId.None]),
                new CapturedEquipmentState(EquipmentSlot.Body, 4200, [new StainId(5), StainId.None]),
                new CapturedEquipmentState(EquipmentSlot.Hands, 4300, [StainId.None, StainId.None]),
            ]);
        var service = new FakeAppearanceService { Snapshot = original };
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        var loadout = CreateLoadout(
            new LoadoutEquipmentState(
                EquipmentSlot.MainHand,
                new AppearanceSelection(new AppearanceId(5100), 5100, [new StainId(1), new StainId(2)]),
                2,
                "Machinist Gun",
                510),
            new LoadoutEquipmentState(
                EquipmentSlot.OffHand,
                new AppearanceSelection(new AppearanceId(5100), 5100, [new StainId(1), new StainId(2)]),
                2,
                "Machinist Gun",
                510),
            new LoadoutEquipmentState(
                EquipmentSlot.Body,
                new AppearanceSelection(new AppearanceId(5200), 5200, [new StainId(7), new StainId(42)]),
                2,
                "Saved Coat",
                520),
            new LoadoutEquipmentState(
                EquipmentSlot.Hands,
                new AppearanceSelection(new AppearanceId(5300), 5300, [new StainId(9), StainId.None]),
                1,
                "Saved Gloves",
                530));
        var storedEquipment = loadout.Equipment.ToArray();
        service.NextCaptureSnapshot = AppearanceSnapshot.Create(
            "viper-with-design-armor",
            "test",
            equipment:
            [
                new CapturedEquipmentState(EquipmentSlot.MainHand, 4100, [StainId.None, StainId.None]),
                new CapturedEquipmentState(EquipmentSlot.OffHand, 4101, [StainId.None, StainId.None]),
                new CapturedEquipmentState(EquipmentSlot.Body, 5200, [new StainId(7), new StainId(42)]),
                new CapturedEquipmentState(EquipmentSlot.Hands, 5300, [new StainId(9), StainId.None]),
            ]);

        var apply = controller.ApplyLoadout(loadout);

        Assert.True(apply.IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out var mainHand));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.OffHand, out var offHand));
        Assert.Equal((uint)4100, mainHand.Appearance.SourceItemId);
        Assert.Equal((uint)4101, offHand.Appearance.SourceItemId);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out var body));
        Assert.Equal((uint)5200, body.Appearance.SourceItemId);
        Assert.Equal(new StainId(7), body.GetStain(0));
        Assert.Equal(new StainId(42), body.GetStain(1));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Hands, out var hands));
        Assert.Equal(new StainId(9), hands.GetStain(0));
        Assert.Equal(storedEquipment, loadout.Equipment);
        var appliedBody = service.EquipmentApplications.Single(application =>
            application.Slot == EquipmentSlot.Body);
        Assert.Equal(new StainId(7), appliedBody.Appearance.Stains[0]);
        Assert.Equal(new StainId(42), appliedBody.Appearance.Stains[1]);

        service.DyeApplications.Clear();
        var clear = controller.ClearAllDyeSlots();

        Assert.True(clear.IsSuccess);
        Assert.Equal(3, service.DyeApplications.Count);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out body));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Hands, out hands));
        Assert.Equal(StainId.None, body.GetStain(0));
        Assert.Equal(StainId.None, body.GetStain(1));
        Assert.Equal(StainId.None, hands.GetStain(0));
        Assert.Equal((uint)5200, body.Appearance.SourceItemId);
        Assert.Equal((uint)5300, hands.Appearance.SourceItemId);
        Assert.Equal(original, controller.Session.OriginalAppearance);
        Assert.Equal(storedEquipment, loadout.Equipment);

        var swap = controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(5201), 5201),
            2,
            "Replacement Coat");
        Assert.True(swap.IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out body));
        Assert.Equal(StainId.None, body.GetStain(0));
        Assert.Equal(StainId.None, body.GetStain(1));

        Assert.True(controller.PreviewDye(EquipmentSlot.Body, 0, new StainId(55)).IsSuccess);
        Assert.True(controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(5202), 5202),
            2,
            "Second Replacement Coat").IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out body));
        Assert.Equal(new StainId(55), body.GetStain(0));
    }

    [Fact]
    public void ReloadingDesignReappliesStoredDyesWithoutMutatingStoredDesign()
    {
        var service = new FakeAppearanceService
        {
            Snapshot = AppearanceSnapshot.Create(
                "original",
                "test",
                equipment:
                [
                    new CapturedEquipmentState(EquipmentSlot.Body, 6000, [StainId.None, StainId.None]),
                ]),
        };
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        var loadout = CreateLoadout(new LoadoutEquipmentState(
            EquipmentSlot.Body,
            new AppearanceSelection(new AppearanceId(6001), 6001, [new StainId(7), new StainId(42)]),
            2,
            "Dyed Design Coat",
            601));
        var expectedStoredAppearance = loadout.Equipment[0].Appearance;
        var loadedSnapshot = AppearanceSnapshot.Create(
            "loaded",
            "test",
            equipment:
            [
                new CapturedEquipmentState(EquipmentSlot.Body, 6001, [new StainId(7), new StainId(42)]),
            ]);
        service.NextCaptureSnapshot = loadedSnapshot;

        Assert.True(controller.ApplyLoadout(loadout).IsSuccess);
        Assert.True(controller.ClearAllDyeSlots().IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out var cleared));
        Assert.Equal(StainId.None, cleared.GetStain(0));
        Assert.Equal(StainId.None, cleared.GetStain(1));

        service.NextCaptureSnapshot = loadedSnapshot;
        Assert.True(controller.ApplyLoadout(loadout).IsSuccess);

        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out var reloaded));
        Assert.Equal(new StainId(7), reloaded.GetStain(0));
        Assert.Equal(new StainId(42), reloaded.GetStain(1));
        Assert.Equal(expectedStoredAppearance, loadout.Equipment[0].Appearance);
    }

    [Theory]
    [InlineData(1, 2, 2)]
    [InlineData(2, 1, 1)]
    public void DesignAdoptionUsesCurrentItemDyeCapability(
        byte originalDyeChannels,
        byte designDyeChannels,
        int expectedClearOperations)
    {
        var originalStains = originalDyeChannels == 1
            ? new[] { new StainId(5), StainId.None }
            : [new StainId(5), new StainId(6)];
        var designStains = designDyeChannels == 1
            ? new[] { new StainId(7), StainId.None }
            : [new StainId(7), new StainId(42)];
        var service = new FakeAppearanceService
        {
            Snapshot = AppearanceSnapshot.Create(
                "original",
                "test",
                equipment:
                [
                    new CapturedEquipmentState(EquipmentSlot.Head, 7000, [.. originalStains]),
                ]),
        };
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        service.NextCaptureSnapshot = AppearanceSnapshot.Create(
            "design",
            "test",
            equipment:
            [
                new CapturedEquipmentState(EquipmentSlot.Head, 7001, [.. designStains]),
            ]);
        var loadout = CreateLoadout(new LoadoutEquipmentState(
            EquipmentSlot.Head,
            new AppearanceSelection(new AppearanceId(7001), 7001, [.. designStains]),
            designDyeChannels,
            "Design Hat",
            701));

        Assert.True(controller.ApplyLoadout(loadout).IsSuccess);
        service.DyeApplications.Clear();
        Assert.True(controller.ClearAllDyeSlots().IsSuccess);

        Assert.Equal(expectedClearOperations, service.DyeApplications.Count);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out var head));
        Assert.Equal(StainId.None, head.GetStain(0));
        Assert.Equal(StainId.None, head.GetStain(1));
    }

    [Fact]
    public void MatchingJobDesignWeaponIsAdoptedWhenCaptureConfirmsItApplied()
    {
        var service = new FakeAppearanceService
        {
            Snapshot = AppearanceSnapshot.Create(
                "machinist-original",
                "test",
                equipment:
                [
                    new CapturedEquipmentState(EquipmentSlot.MainHand, 8000, [StainId.None, StainId.None]),
                    new CapturedEquipmentState(EquipmentSlot.OffHand, 8000, [StainId.None, StainId.None]),
                ]),
        };
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        service.NextCaptureSnapshot = AppearanceSnapshot.Create(
            "machinist-design",
            "test",
            equipment:
            [
                new CapturedEquipmentState(EquipmentSlot.MainHand, 8001, [new StainId(7), StainId.None]),
                new CapturedEquipmentState(EquipmentSlot.OffHand, 8001, [new StainId(7), StainId.None]),
            ]);
        var loadout = CreateLoadout(
            new LoadoutEquipmentState(
                EquipmentSlot.MainHand,
                new AppearanceSelection(new AppearanceId(8001), 8001, [new StainId(7), StainId.None]),
                1,
                "Machinist Design Gun",
                801),
            new LoadoutEquipmentState(
                EquipmentSlot.OffHand,
                new AppearanceSelection(new AppearanceId(8001), 8001, [new StainId(7), StainId.None]),
                1,
                "Machinist Design Gun",
                801));

        Assert.True(controller.ApplyLoadout(loadout).IsSuccess);

        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out var mainHand));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.OffHand, out var offHand));
        Assert.Equal((uint)8001, mainHand.Appearance.SourceItemId);
        Assert.Equal((uint)8001, offHand.Appearance.SourceItemId);
        Assert.Equal("Machinist Design Gun", mainHand.DisplayName);
        Assert.Equal("Machinist Design Gun", offHand.DisplayName);
    }

    [Fact]
    public void SynchronizedGameStateReplacesCrossJobDesignAdoptionAndSeedsItsActiveDyes()
    {
        var service = new FakeAppearanceService
        {
            Snapshot = AppearanceSnapshot.Create(
                "viper-original",
                "test",
                equipment:
                [
                    new CapturedEquipmentState(EquipmentSlot.MainHand, 9000, [StainId.None, StainId.None]),
                    new CapturedEquipmentState(EquipmentSlot.Body, 9001, [StainId.None, StainId.None]),
                ]),
        };
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        var loadout = CreateLoadout(
            new LoadoutEquipmentState(
                EquipmentSlot.MainHand,
                AppearanceSelection.WithoutStains(new AppearanceId(9100), 9100),
                0,
                "Other Job Weapon",
                910),
            new LoadoutEquipmentState(
                EquipmentSlot.Body,
                new AppearanceSelection(new AppearanceId(9101), 9101, [new StainId(7), new StainId(42)]),
                2,
                "Design Coat",
                911));
        service.NextCaptureSnapshot = AppearanceSnapshot.Create(
            "viper-design-preview",
            "test",
            equipment:
            [
                new CapturedEquipmentState(EquipmentSlot.MainHand, 9000, [StainId.None, StainId.None]),
                new CapturedEquipmentState(EquipmentSlot.Body, 9101, [new StainId(7), new StainId(42)]),
            ]);
        Assert.True(controller.ApplyLoadout(loadout).IsSuccess);

        var synchronized = AppearanceSnapshot.Create(
            "machinist-gearset",
            "test",
            equipment:
            [
                new CapturedEquipmentState(EquipmentSlot.MainHand, 9200, [StainId.None, StainId.None]),
                new CapturedEquipmentState(EquipmentSlot.Body, 9201, [new StainId(12), new StainId(34)]),
            ]);
        service.NextCaptureSnapshot = synchronized;

        Assert.True(controller.CaptureSynchronizedGameState().IsSuccess);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out var mainHand));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out var body));
        Assert.Equal((uint)9200, mainHand.Appearance.SourceItemId);
        Assert.Equal((uint)9201, body.Appearance.SourceItemId);
        Assert.Equal(new StainId(12), body.GetStain(0));
        Assert.Equal(new StainId(34), body.GetStain(1));
        Assert.Equal(synchronized, controller.Session.OriginalAppearance);

        service.DyeApplications.Clear();
        Assert.True(controller.ClearAllDyeSlots().IsSuccess);
        Assert.Equal(2, service.DyeApplications.Count);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out body));
        Assert.Equal(StainId.None, body.GetStain(0));
        Assert.Equal(StainId.None, body.GetStain(1));
    }

    [Fact]
    public void ResetAllAfterCrossJobDesignRestoresOriginalWeaponAndDyes()
    {
        var original = AppearanceSnapshot.Create(
            "viper-original",
            "test",
            equipment:
            [
                new CapturedEquipmentState(EquipmentSlot.MainHand, 9300, [new StainId(5), StainId.None]),
                new CapturedEquipmentState(EquipmentSlot.Body, 9301, [new StainId(6), StainId.None]),
            ]);
        var service = new FakeAppearanceService { Snapshot = original };
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        var loadout = CreateLoadout(
            new LoadoutEquipmentState(
                EquipmentSlot.MainHand,
                AppearanceSelection.WithoutStains(new AppearanceId(9400), 9400),
                0,
                "Other Job Weapon",
                940),
            new LoadoutEquipmentState(
                EquipmentSlot.Body,
                new AppearanceSelection(new AppearanceId(9401), 9401, [new StainId(7), new StainId(42)]),
                2,
                "Design Coat",
                941));
        service.NextCaptureSnapshot = AppearanceSnapshot.Create(
            "viper-design-preview",
            "test",
            equipment:
            [
                new CapturedEquipmentState(EquipmentSlot.MainHand, 9300, [new StainId(5), StainId.None]),
                new CapturedEquipmentState(EquipmentSlot.Body, 9401, [new StainId(7), new StainId(42)]),
            ]);
        Assert.True(controller.ApplyLoadout(loadout).IsSuccess);

        Assert.True(controller.ResetAll().IsSuccess);

        Assert.Equal(original, controller.Session.PreviewAppearance);
        Assert.False(controller.Session.IsDirty);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.MainHand, out var mainHand));
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Body, out var body));
        Assert.Equal((uint)9300, mainHand.Appearance.SourceItemId);
        Assert.Equal((uint)9301, body.Appearance.SourceItemId);
        Assert.Equal(new StainId(5), mainHand.GetStain(0));
        Assert.Equal(new StainId(6), body.GetStain(0));
    }

    [Fact]
    public void FailedLoadoutApplyRollsBackAndKeepsPriorChangedSlotState()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Hands,
            AppearanceSelection.WithoutStains(new AppearanceId(1700), 1700),
            displayName: "Prior Gloves");
        var previousPreview = controller.Session.PreviewAppearance;
        service.ApplyEquipmentShouldFail = true;
        var loadout = CreateLoadout(new LoadoutEquipmentState(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(1701), 1701),
            0,
            "Failed Hat",
            601));

        var result = controller.ApplyLoadout(loadout);

        Assert.False(result.IsSuccess);
        Assert.Equal(1, service.RevertCallCount);
        Assert.Equal(previousPreview, Assert.Single(service.AppliedSnapshots));
        Assert.Equal(1, controller.ChangedSlotCount);
        Assert.True(controller.TryGetPreviewEquipment(EquipmentSlot.Hands, out var hands));
        Assert.Equal("Prior Gloves", hands.DisplayName);
        Assert.False(controller.TryGetPreviewEquipment(EquipmentSlot.Head, out _));
    }

    [Fact]
    public void InvalidLoadoutIsRejectedBeforeCallingIntegration()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        var now = DateTimeOffset.UtcNow;
        var invalid = new BoutiqueLoadout(
            LoadoutSchema.CurrentVersion,
            Guid.NewGuid(),
            "Empty",
            now,
            now,
            [],
            [],
            null);

        var result = controller.ApplyLoadout(invalid);

        Assert.False(result.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.LoadoutInvalid, result.Error?.Code);
        Assert.Empty(service.AppliedSnapshots);
        Assert.Equal(0, service.RevertCallCount);
        Assert.Equal(0, service.ApplyEquipmentCallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeadPreviewAppliesRequestedDeterministicVisorState(bool enabled)
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();

        var result = controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(44820), 44820),
            visorState: enabled);

        Assert.True(result.IsSuccess);
        Assert.Equal([enabled], service.VisorStates);
    }

    [Fact]
    public void NonHeadPreviewDoesNotChangeVisorState()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();

        var result = controller.PreviewEquipment(
            EquipmentSlot.Body,
            AppearanceSelection.WithoutStains(new AppearanceId(44821), 44821));

        Assert.True(result.IsSuccess);
        Assert.Empty(service.VisorStates);
    }

    [Fact]
    public void UndoHeadPreviewRestoresPreviousBoutiqueVisorState()
    {
        var service = new FakeAppearanceService();
        var controller = new BoutiqueSessionController(service);
        controller.Open();
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(44820), 44820),
            visorState: false);
        controller.PreviewEquipment(
            EquipmentSlot.Head,
            AppearanceSelection.WithoutStains(new AppearanceId(51162), 51162),
            visorState: true);

        var result = controller.UndoLastEquipmentPreview();

        Assert.True(result.IsSuccess);
        Assert.Equal([false, true, false], service.VisorStates);
    }

    private static BoutiqueLoadout CreateLoadout(params LoadoutEquipmentState[] equipment)
    {
        var now = new DateTimeOffset(2026, 8, 25, 20, 0, 0, TimeSpan.Zero);
        return new BoutiqueLoadout(
            LoadoutSchema.CurrentVersion,
            Guid.NewGuid(),
            "Saved Look",
            now,
            now,
            [.. equipment],
            [],
            null);
    }

    private static FakeAppearanceService CreateServiceWithCapturedHead()
        => new()
        {
            Snapshot = AppearanceSnapshot.Create(
                "original",
                "test",
                equipment:
                [
                    new CapturedEquipmentState(
                        EquipmentSlot.Head,
                        44820,
                        [new StainId(17), new StainId(42)]),
                ]),
        };

    private sealed class FakeAppearanceService : IAppearanceService, IVisorAppearanceService
    {
        public AppearanceSnapshot Snapshot { get; init; } = AppearanceSnapshot.Create("original", "test");

        public AppearanceSnapshot? NextCaptureSnapshot { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DependencyStatus Status => IsAvailable ? DependencyStatus.Available : DependencyStatus.Unavailable;

        public string? DetectedVersion => "test";

        public bool RestoreShouldFail { get; set; }

        public bool ApplyEquipmentShouldFail { get; set; }

        public bool PreviewCaptureShouldFail { get; set; }

        public bool ApplyDyeShouldFail { get; set; }

        public bool ApplyAppearanceShouldFail { get; set; }

        public bool RevertToGameShouldFail { get; set; }

        public int? FailOnEquipmentCall { get; set; }

        public int? FailOnDyeCall { get; set; }

        public int RestoreCallCount { get; private set; }

        public int RevertCallCount { get; private set; }

        public int ApplyEquipmentCallCount { get; private set; }

        public int ApplyDyeCallCount { get; private set; }

        public int CaptureCallCount => captureCallCount;

        public AppearanceSelection? LastDyeAppearance { get; private set; }

        public AppearanceSnapshot? RestoredSnapshot { get; private set; }

        public List<AppearanceSnapshot> AppliedSnapshots { get; } = [];

        public List<(EquipmentSlot Slot, AppearanceSelection Appearance)> EquipmentApplications { get; } = [];

        public List<(EquipmentSlot Slot, AppearanceSelection Appearance)> DyeApplications { get; } = [];

        public List<bool> VisorStates { get; } = [];

        private int captureCallCount;

        public Result<AppearanceSnapshot> CapturePlayerAppearance()
        {
            captureCallCount++;
            if (captureCallCount > 1 && PreviewCaptureShouldFail)
            {
                return Result.Failure<AppearanceSnapshot>(new BoutiqueError(
                    BoutiqueErrorCode.AppearanceCaptureFailed,
                    "Expected test failure."));
            }

            if (NextCaptureSnapshot is { } nextCapture)
            {
                NextCaptureSnapshot = null;
                return Result.Success(nextCapture);
            }

            return Result.Success(captureCallCount == 1
                ? Snapshot
                : AppearanceSnapshot.Create($"preview-{captureCallCount - 1}", "test"));
        }

        public Result ApplyEquipment(EquipmentSlot slot, AppearanceSelection appearance)
        {
            ApplyEquipmentCallCount++;
            EquipmentApplications.Add((slot, appearance));
            return ApplyEquipmentShouldFail || ApplyEquipmentCallCount == FailOnEquipmentCall
                ? Result.Failure(new BoutiqueError(
                    BoutiqueErrorCode.AppearanceApplyFailed,
                    "Expected test failure."))
                : Result.Ok;
        }

        public Result ApplyDye(EquipmentSlot slot, AppearanceSelection appearance)
        {
            ApplyDyeCallCount++;
            LastDyeAppearance = appearance;
            DyeApplications.Add((slot, appearance));
            return ApplyDyeShouldFail || ApplyDyeCallCount == FailOnDyeCall
                ? Result.Failure(new BoutiqueError(
                    BoutiqueErrorCode.InvalidDye,
                    "Expected dye failure."))
                : Result.Ok;
        }

        public Result ApplyAppearance(AppearanceSnapshot appearance)
        {
            AppliedSnapshots.Add(appearance);
            return ApplyAppearanceShouldFail
                ? Result.Failure(new BoutiqueError(
                    BoutiqueErrorCode.AppearanceApplyFailed,
                    "Expected snapshot apply failure."))
                : Result.Ok;
        }

        public Result RevertToGame()
        {
            RevertCallCount++;
            return RevertToGameShouldFail
                ? Result.Failure(new BoutiqueError(
                    BoutiqueErrorCode.AppearanceApplyFailed,
                    "Expected game-state revert failure."))
                : Result.Ok;
        }

        public Result SetVisorState(bool enabled)
        {
            VisorStates.Add(enabled);
            return Result.Ok;
        }

        public Result RestoreAppearance(AppearanceSnapshot snapshot)
        {
            RestoreCallCount++;
            RestoredSnapshot = snapshot;
            return RestoreShouldFail
                ? Result.Failure(new BoutiqueError(BoutiqueErrorCode.RestoreFailed, "Expected test failure."))
                : Result.Ok;
        }
    }
}
