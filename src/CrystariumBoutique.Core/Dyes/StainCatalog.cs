using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Core.Dyes;

public interface IStainRepository
{
    int Count { get; }

    ImmutableArray<StainDefinition> All { get; }

    ImmutableArray<StainDefinition> Search(string searchText, StainGroup? group = null);

    bool TryGet(StainId stainId, out StainDefinition definition);
}

public sealed class StainCatalog : IStainRepository
{
    private readonly ImmutableDictionary<StainId, StainDefinition> byId;
    private readonly ImmutableDictionary<StainId, string> normalizedNames;

    public StainCatalog(IEnumerable<StainDefinition> stains)
    {
        ArgumentNullException.ThrowIfNull(stains);

        All = stains
            .Where(stain => stain.Id.Value > 0 && !string.IsNullOrWhiteSpace(stain.Name))
            .DistinctBy(stain => stain.Id)
            .OrderBy(stain => StainGroups.GetSortOrder(stain.Group))
            .ThenBy(stain => stain.SortOrder)
            .ThenBy(stain => stain.Name, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
        byId = All.ToImmutableDictionary(stain => stain.Id);
        normalizedNames = All.ToImmutableDictionary(
            stain => stain.Id,
            stain => CatalogSearch.Normalize(stain.Name));
    }

    public static StainCatalog Empty { get; } = new(Array.Empty<StainDefinition>());

    public int Count => All.Length;

    public ImmutableArray<StainDefinition> All { get; }

    public ImmutableArray<StainDefinition> Search(string searchText, StainGroup? group = null)
    {
        ArgumentNullException.ThrowIfNull(searchText);
        var normalizedSearch = CatalogSearch.Normalize(searchText);
        if (normalizedSearch.Length == 0 && group is null)
        {
            return All;
        }

        var results = ImmutableArray.CreateBuilder<StainDefinition>();
        foreach (var stain in All)
        {
            if (group.HasValue && stain.Group != group.Value)
            {
                continue;
            }

            if (normalizedSearch.Length == 0
                || CatalogSearch.MatchesAllTerms(normalizedNames[stain.Id], normalizedSearch))
            {
                results.Add(stain);
            }
        }

        return results.ToImmutable();
    }

    public bool TryGet(StainId stainId, out StainDefinition definition)
        => byId.TryGetValue(stainId, out definition!);
}
