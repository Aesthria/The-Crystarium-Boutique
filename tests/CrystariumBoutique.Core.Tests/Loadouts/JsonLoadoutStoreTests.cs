using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Loadouts;

namespace CrystariumBoutique.Core.Tests.Loadouts;

public sealed class JsonLoadoutStoreTests
{
    [Fact]
    public void WriteAndReadRoundTripPreservesCompleteEquipmentState()
    {
        using var directory = new TestDirectory();
        var store = new JsonLoadoutStore(directory.Path);
        var loadout = CreateLoadout(Guid.NewGuid(), "Raid Night");

        var write = store.Write(loadout);
        var read = store.ReadAll();

        Assert.True(write.IsSuccess);
        Assert.True(read.IsSuccess);
        var restored = Assert.Single(read.Value!.Loadouts);
        AssertLoadout(loadout, restored);
        Assert.Empty(read.Value.Warnings);
        Assert.Equal(0, read.Value.MigratedCount);
        Assert.Equal(0, read.Value.QuarantinedCount);
    }

    [Fact]
    public void SchemaZeroDocumentMigratesInPlaceToCurrentVersion()
    {
        using var directory = new TestDirectory();
        var id = Guid.NewGuid();
        var path = System.IO.Path.Combine(directory.Path, $"{id:N}.json");
        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 0,
              "name": "Legacy Look",
              "equipment": [
                {
                  "slot": "Head",
                  "appearanceId": 700,
                  "sourceItemId": 700,
                  "stains": [7, 42],
                  "dyeChannelCount": 2,
                  "displayName": "Legacy Hat",
                  "iconId": 123
                }
              ],
              "tags": ["legacy"],
              "notes": "Migrated locally"
            }
            """);
        var store = new JsonLoadoutStore(directory.Path);

        var read = store.ReadAll();

        Assert.True(read.IsSuccess);
        var migrated = Assert.Single(read.Value!.Loadouts);
        Assert.Equal(LoadoutSchema.CurrentVersion, migrated.SchemaVersion);
        Assert.Equal(id, migrated.Id);
        Assert.Equal("Legacy Look", migrated.Name);
        Assert.Equal(1, read.Value.MigratedCount);
        Assert.Contains("\"schemaVersion\": 1", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void CorruptRecordIsQuarantinedWithoutBlockingValidLoadouts()
    {
        using var directory = new TestDirectory();
        var now = new DateTimeOffset(2026, 8, 25, 20, 0, 0, TimeSpan.Zero);
        var store = new JsonLoadoutStore(directory.Path, () => now);
        var valid = CreateLoadout(Guid.NewGuid(), "Valid Look");
        Assert.True(store.Write(valid).IsSuccess);
        var corruptPath = System.IO.Path.Combine(directory.Path, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(corruptPath, "{ this is not valid json");

        var read = store.ReadAll();

        Assert.True(read.IsSuccess);
        AssertLoadout(valid, Assert.Single(read.Value!.Loadouts));
        Assert.Equal(1, read.Value.QuarantinedCount);
        Assert.Single(read.Value.Warnings);
        Assert.False(File.Exists(corruptPath));
        Assert.Single(Directory.EnumerateFiles(
            System.IO.Path.Combine(directory.Path, "quarantine"),
            "*.invalid.json"));
    }

    [Fact]
    public void RecordWithoutReplayableSourceItemIsQuarantined()
    {
        using var directory = new TestDirectory();
        var id = Guid.NewGuid();
        var path = System.IO.Path.Combine(directory.Path, $"{id:N}.json");
        File.WriteAllText(
            path,
            $$"""
            {
              "schemaVersion": 1,
              "id": "{{id}}",
              "name": "Cannot Replay",
              "createdAtUtc": "2026-08-25T19:00:00+00:00",
              "updatedAtUtc": "2026-08-25T19:00:00+00:00",
              "equipment": [
                {
                  "slot": "Head",
                  "appearanceId": 700,
                  "sourceItemId": null,
                  "stains": [],
                  "dyeChannelCount": 0,
                  "displayName": "Unidentified Hat",
                  "iconId": 123
                }
              ],
              "tags": []
            }
            """);
        var store = new JsonLoadoutStore(directory.Path);

        var read = store.ReadAll();

        Assert.True(read.IsSuccess);
        Assert.Empty(read.Value!.Loadouts);
        Assert.Equal(1, read.Value.QuarantinedCount);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void DeleteMovesLoadoutIntoRecoverableDeletedDirectory()
    {
        using var directory = new TestDirectory();
        var now = new DateTimeOffset(2026, 8, 25, 20, 0, 0, TimeSpan.Zero);
        var store = new JsonLoadoutStore(directory.Path, () => now);
        var loadout = CreateLoadout(Guid.NewGuid(), "Delete Me");
        Assert.True(store.Write(loadout).IsSuccess);

        var deleted = store.Delete(loadout.Id);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(store.ReadAll().Value!.Loadouts);
        Assert.Single(Directory.EnumerateFiles(
            System.IO.Path.Combine(directory.Path, "deleted"),
            "*.json"));
    }

    private static BoutiqueLoadout CreateLoadout(Guid id, string name)
    {
        var created = new DateTimeOffset(2026, 8, 25, 19, 0, 0, TimeSpan.Zero);
        return new BoutiqueLoadout(
            LoadoutSchema.CurrentVersion,
            id,
            name,
            created,
            created.AddMinutes(5),
            [
                new LoadoutEquipmentState(
                    EquipmentSlot.MainHand,
                    new AppearanceSelection(
                        new AppearanceId(100),
                        100,
                        ImmutableArray.Create(new StainId(7), new StainId(42))),
                    2,
                    "Test Weapon",
                    500),
                new LoadoutEquipmentState(
                    EquipmentSlot.OffHand,
                    AppearanceSelection.WithoutStains(new AppearanceId(101), 101),
                    0,
                    "Test Shield",
                    501),
            ],
            ["raid", "blue"],
            "A complete weapon pairing.");
    }

    private static void AssertLoadout(BoutiqueLoadout expected, BoutiqueLoadout actual)
    {
        Assert.Equal(expected.SchemaVersion, actual.SchemaVersion);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.CreatedAtUtc, actual.CreatedAtUtc);
        Assert.Equal(expected.UpdatedAtUtc, actual.UpdatedAtUtc);
        Assert.Equal(expected.Tags.ToArray(), actual.Tags.ToArray());
        Assert.Equal(expected.Notes, actual.Notes);
        Assert.Equal(expected.Equipment.Length, actual.Equipment.Length);
        for (var index = 0; index < expected.Equipment.Length; index++)
        {
            var expectedItem = expected.Equipment[index];
            var actualItem = actual.Equipment[index];
            Assert.Equal(expectedItem.Slot, actualItem.Slot);
            Assert.Equal(expectedItem.Appearance.AppearanceId, actualItem.Appearance.AppearanceId);
            Assert.Equal(expectedItem.Appearance.SourceItemId, actualItem.Appearance.SourceItemId);
            Assert.Equal(expectedItem.Appearance.Stains.ToArray(), actualItem.Appearance.Stains.ToArray());
            Assert.Equal(expectedItem.DyeChannelCount, actualItem.DyeChannelCount);
            Assert.Equal(expectedItem.DisplayName, actualItem.DisplayName);
            Assert.Equal(expectedItem.IconId, actualItem.IconId);
        }
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "CrystariumBoutique.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
