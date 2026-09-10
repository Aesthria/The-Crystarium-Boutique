using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Loadouts;
using CrystariumBoutique.Core.Sessions;

namespace CrystariumBoutique.Core.Tests.Loadouts;

public sealed class LoadoutLibraryTests
{
    [Fact]
    public void CreateRenameDuplicateOverwriteAndDeletePersistBeforeMutatingLibrary()
    {
        var store = new FakeLoadoutStore();
        var now = new DateTimeOffset(2026, 8, 25, 20, 0, 0, TimeSpan.Zero);
        var library = new LoadoutLibrary(store, () => now = now.AddMinutes(1));
        Assert.True(library.Initialize().IsSuccess);

        var created = library.Create("First Look", [Preview(EquipmentSlot.Head, 100, "First Hat")]);
        var renamed = library.Rename(created.Value!.Id, "Renamed Look");
        var duplicateName = library.SuggestDuplicateName(renamed.Value!);
        var duplicated = library.Duplicate(renamed.Value!.Id, duplicateName);
        var overwritten = library.Overwrite(
            renamed.Value.Id,
            [Preview(EquipmentSlot.Body, 200, "New Coat")]);
        var deleted = library.Delete(duplicated.Value!.Id);

        Assert.True(created.IsSuccess);
        Assert.True(renamed.IsSuccess);
        Assert.True(duplicated.IsSuccess);
        Assert.True(overwritten.IsSuccess);
        Assert.True(deleted.IsSuccess);
        var remaining = Assert.Single(library.Loadouts);
        Assert.Equal("Renamed Look", remaining.Name);
        Assert.Equal(EquipmentSlot.Body, Assert.Single(remaining.Equipment).Slot);
        Assert.Equal(4, store.WriteCount);
        Assert.Equal(duplicated.Value.Id, Assert.Single(store.DeletedIds));
    }

    [Fact]
    public void DuplicateNamesAreRejectedCaseInsensitively()
    {
        var store = new FakeLoadoutStore();
        var library = new LoadoutLibrary(store);
        library.Initialize();
        var created = library.Create("Crystal Blue", [Preview(EquipmentSlot.Head, 100, "Hat")]);

        var duplicate = library.Create(" crystal blue ", [Preview(EquipmentSlot.Body, 200, "Coat")]);

        Assert.True(created.IsSuccess);
        Assert.False(duplicate.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.LoadoutInvalid, duplicate.Error?.Code);
        Assert.Single(library.Loadouts);
    }

    [Fact]
    public void FailedPersistenceDoesNotMutateTheInMemoryLibrary()
    {
        var store = new FakeLoadoutStore { WriteShouldFail = true };
        var library = new LoadoutLibrary(store);
        library.Initialize();

        var result = library.Create("Cannot Save", [Preview(EquipmentSlot.Head, 100, "Hat")]);

        Assert.False(result.IsSuccess);
        Assert.Equal(BoutiqueErrorCode.LoadoutPersistenceFailed, result.Error?.Code);
        Assert.Empty(library.Loadouts);
    }

    [Fact]
    public void SuggestedCopyNameRemainsUniqueWithinMaximumLength()
    {
        var store = new FakeLoadoutStore();
        var library = new LoadoutLibrary(store);
        library.Initialize();
        var longName = new string('A', LoadoutSchema.MaximumNameLength);
        var source = library.Create(longName, [Preview(EquipmentSlot.Head, 100, "Hat")]).Value!;
        var firstName = library.SuggestDuplicateName(source);
        var firstCopy = library.Duplicate(source.Id, firstName).Value!;

        var secondName = library.SuggestDuplicateName(source);

        Assert.True(firstName.Length <= LoadoutSchema.MaximumNameLength);
        Assert.True(secondName.Length <= LoadoutSchema.MaximumNameLength);
        Assert.NotEqual(source.Name, firstCopy.Name);
        Assert.NotEqual(firstName, secondName);
    }

    [Fact]
    public void ImportCreatesANewLocalIdentityAndDisambiguatesItsName()
    {
        var store = new FakeLoadoutStore();
        var timestamp = new DateTimeOffset(2026, 8, 26, 20, 0, 0, TimeSpan.Zero);
        var library = new LoadoutLibrary(store, () => timestamp);
        library.Initialize();
        var existing = library.Create("Shared Look", [Preview(EquipmentSlot.Head, 100, "Hat")]).Value!;
        var portable = new LoadoutExchangeDesign(
            existing.Name,
            existing.Equipment,
            existing.Tags,
            existing.Notes);

        var imported = library.Import(portable);

        Assert.True(imported.IsSuccess);
        Assert.NotEqual(existing.Id, imported.Value!.Id);
        Assert.Equal("Shared Look Imported", imported.Value.Name);
        Assert.Equal(2, library.Loadouts.Length);
        Assert.Equal(2, store.WriteCount);
    }

    private static PreviewEquipmentState Preview(
        EquipmentSlot slot,
        uint itemId,
        string name)
        => new(
            slot,
            AppearanceSelection.WithoutStains(new AppearanceId(itemId), itemId),
            0,
            name,
            itemId);

    private sealed class FakeLoadoutStore : ILoadoutStore
    {
        private readonly Dictionary<Guid, BoutiqueLoadout> persisted = [];

        public bool WriteShouldFail { get; set; }

        public int WriteCount { get; private set; }

        public List<Guid> DeletedIds { get; } = [];

        public Result<LoadoutStoreSnapshot> ReadAll()
            => Result.Success(new LoadoutStoreSnapshot(
                persisted.Values.ToImmutableArray(),
                ImmutableArray<string>.Empty,
                0,
                0));

        public Result Write(BoutiqueLoadout loadout)
        {
            WriteCount++;
            if (WriteShouldFail)
            {
                return Result.Failure(new BoutiqueError(
                    BoutiqueErrorCode.LoadoutPersistenceFailed,
                    "Expected persistence failure."));
            }

            persisted[loadout.Id] = loadout;
            return Result.Ok;
        }

        public Result Delete(Guid loadoutId)
        {
            DeletedIds.Add(loadoutId);
            persisted.Remove(loadoutId);
            return Result.Ok;
        }
    }
}
