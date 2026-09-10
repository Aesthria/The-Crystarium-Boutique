using System.Text.Json;

namespace CrystariumBoutique.AcquisitionGenerator;

public static class TrackyDatasetReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static TrackyReadResult Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var categories = JsonSerializer.Deserialize<List<TrackyCategory>>(json, JsonOptions)
            ?? throw new InvalidDataException("The Tracky input does not contain a category array.");
        var observations = new List<ObservedChestReward>();
        var inputRecordCount = 0;
        foreach (var category in categories)
        {
            foreach (var expansion in category.Expansions)
            {
                foreach (var header in expansion.Headers)
                {
                    foreach (var duty in header.Duties)
                    {
                        foreach (var patch in duty.Chests.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                        {
                            foreach (var chest in patch.Value)
                            {
                                foreach (var reward in chest.Rewards)
                                {
                                    inputRecordCount++;
                                    observations.Add(new ObservedChestReward(
                                        reward.Id,
                                        duty.Id,
                                        duty.Name.Trim(),
                                        chest.TerritoryId,
                                        chest.Id,
                                        chest.MapId,
                                        checked((int)Math.Min(reward.Amount, int.MaxValue)),
                                        patch.Key));
                                }
                            }
                        }
                    }
                }
            }
        }

        return new TrackyReadResult(inputRecordCount, observations);
    }

    private sealed class TrackyCategory
    {
        public List<TrackyExpansion> Expansions { get; init; } = [];
    }

    private sealed class TrackyExpansion
    {
        public List<TrackyHeader> Headers { get; init; } = [];
    }

    private sealed class TrackyHeader
    {
        public List<TrackyDuty> Duties { get; init; } = [];
    }

    private sealed class TrackyDuty
    {
        public uint Id { get; init; }

        public string Name { get; init; } = "";

        public Dictionary<string, List<TrackyChest>> Chests { get; init; } = new(StringComparer.Ordinal);
    }

    private sealed class TrackyChest
    {
        public uint Id { get; init; }

        public uint TerritoryId { get; init; }

        public uint MapId { get; init; }

        public List<TrackyReward> Rewards { get; init; } = [];
    }

    private sealed class TrackyReward
    {
        public uint Id { get; init; }

        public long Amount { get; init; }
    }
}

public sealed record TrackyReadResult(
    int InputRecordCount,
    IReadOnlyList<ObservedChestReward> Observations);

public sealed record ObservedChestReward(
    uint ItemId,
    uint ContentFinderConditionId,
    string UpstreamDutyName,
    uint TerritoryTypeId,
    uint TreasureId,
    uint MapId,
    int ObservationCount,
    string Patch);
