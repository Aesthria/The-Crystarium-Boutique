using System.Collections.Immutable;
using System.Text.Json;

namespace CrystariumBoutique.Core.Catalog;

public sealed record ItemAvailabilityRecord(
    uint ItemId,
    string ItemName,
    bool Excluded,
    string Category,
    string Reason,
    string? Notes = null);

public sealed record ItemAvailabilityExactGroup(
    string Category,
    string Reason,
    string RequiredNamePrefix,
    ImmutableHashSet<uint> ItemIds,
    string? Notes = null);

public sealed record ItemAvailabilitySupplement(
    int SchemaVersion,
    string GameBuild,
    ImmutableDictionary<uint, ItemAvailabilityRecord> Records,
    ImmutableArray<ItemAvailabilityExactGroup> ExactGroups)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static ItemAvailabilitySupplement Empty { get; } = new(
        1,
        string.Empty,
        ImmutableDictionary<uint, ItemAvailabilityRecord>.Empty,
        []);

    public ImmutableHashSet<uint> AllItemIds
        => Records.Keys
            .Concat(ExactGroups.SelectMany(group => group.ItemIds))
            .ToImmutableHashSet();

    public bool TryGetExactGroup(uint itemId, out ItemAvailabilityExactGroup group)
    {
        foreach (var candidate in ExactGroups)
        {
            if (candidate.ItemIds.Contains(itemId))
            {
                group = candidate;
                return true;
            }
        }

        group = null!;
        return false;
    }

    public static ItemAvailabilitySupplement Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = File.OpenRead(path);
        var document = JsonSerializer.Deserialize<AvailabilityDocument>(stream, SerializerOptions)
            ?? throw new InvalidDataException("The item availability supplement is empty.");
        if (document.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported availability schema {document.SchemaVersion}.");
        }

        var records = ImmutableDictionary.CreateBuilder<uint, ItemAvailabilityRecord>();
        foreach (var record in document.Records ?? [])
        {
            if (record.ItemId == 0
                || string.IsNullOrWhiteSpace(record.ItemName)
                || string.IsNullOrWhiteSpace(record.Category)
                || string.IsNullOrWhiteSpace(record.Reason))
            {
                throw new InvalidDataException("Availability records require an exact Item ID, name, category, and reason.");
            }

            if (!records.TryAdd(record.ItemId, record))
            {
                throw new InvalidDataException($"Duplicate availability Item ID {record.ItemId}.");
            }
        }

        var exactGroups = ImmutableArray.CreateBuilder<ItemAvailabilityExactGroup>();
        var allIds = records.Keys.ToHashSet();
        foreach (var group in document.ExactGroups ?? [])
        {
            if (string.IsNullOrWhiteSpace(group.Category)
                || string.IsNullOrWhiteSpace(group.Reason)
                || string.IsNullOrWhiteSpace(group.RequiredNamePrefix)
                || group.ItemIds is null
                || group.ItemIds.Length == 0)
            {
                throw new InvalidDataException(
                    "Exact availability groups require a category, reason, name prefix, and Item IDs.");
            }

            var itemIds = group.ItemIds.ToImmutableHashSet();
            if (itemIds.Count != group.ItemIds.Length || itemIds.Any(itemId => itemId == 0))
            {
                throw new InvalidDataException("Exact availability groups contain a zero or duplicate Item ID.");
            }

            foreach (var itemId in itemIds)
            {
                if (!allIds.Add(itemId))
                {
                    throw new InvalidDataException($"Duplicate availability Item ID {itemId}.");
                }
            }

            exactGroups.Add(new ItemAvailabilityExactGroup(
                group.Category.Trim(),
                group.Reason.Trim(),
                group.RequiredNamePrefix,
                itemIds,
                group.Notes));
        }

        return new ItemAvailabilitySupplement(
            document.SchemaVersion,
            document.GameBuild ?? string.Empty,
            records.ToImmutable(),
            exactGroups.ToImmutable());
    }

    private sealed class AvailabilityDocument
    {
        public int SchemaVersion { get; init; }

        public string? GameBuild { get; init; }

        public ItemAvailabilityRecord[]? Records { get; init; }

        public ExactGroupDocument[]? ExactGroups { get; init; }
    }

    private sealed class ExactGroupDocument
    {
        public string? Category { get; init; }

        public string? Reason { get; init; }

        public string? RequiredNamePrefix { get; init; }

        public uint[]? ItemIds { get; init; }

        public string? Notes { get; init; }
    }
}
