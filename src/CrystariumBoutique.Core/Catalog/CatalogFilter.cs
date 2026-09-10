using System.Globalization;
using System.Text;

namespace CrystariumBoutique.Core.Catalog;

public enum DyeSupportFilter
{
    Any,
    None,
    SingleChannel,
    TwoChannels,
}

[Flags]
public enum EquipmentRoles
{
    None = 0,
    Tank = 1 << 0,
    Healer = 1 << 1,
    MeleeDps = 1 << 2,
    PhysicalRangedDps = 1 << 3,
    MagicalRangedDps = 1 << 4,
    Crafter = 1 << 5,
    Gatherer = 1 << 6,
    Limited = 1 << 7,
    All = Tank | Healer | MeleeDps | PhysicalRangedDps | MagicalRangedDps | Crafter | Gatherer,
}

public enum JobRoleFilter
{
    Any,
    Tank,
    Healer,
    MeleeDps,
    PhysicalRangedDps,
    MagicalRangedDps,
    Limited,
    Crafter,
    Gatherer,
}

public sealed record CatalogFilter(
    string SearchText,
    byte? MinimumEquipLevel,
    byte? MaximumEquipLevel,
    uint? MinimumItemLevel,
    uint? MaximumItemLevel,
    DyeSupportFilter DyeSupport,
    JobRoleFilter JobRole = JobRoleFilter.Any,
    uint? ActiveClassJobId = null)
{
    public static CatalogFilter Default { get; } = new(
        string.Empty,
        null,
        null,
        null,
        null,
        DyeSupportFilter.Any,
        JobRoleFilter.Any);

    public bool IsDefault
        => string.IsNullOrWhiteSpace(SearchText)
            && MinimumEquipLevel is null
            && MaximumEquipLevel is null
            && MinimumItemLevel is null
            && MaximumItemLevel is null
            && DyeSupport == DyeSupportFilter.Any
            && JobRole == JobRoleFilter.Any
            && ActiveClassJobId is null;
}

public static class CatalogSearch
{
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);
        var pendingSeparator = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                if (pendingSeparator && result.Length > 0)
                {
                    result.Append(' ');
                }

                result.Append(char.ToLowerInvariant(character));
                pendingSeparator = false;
            }
            else
            {
                pendingSeparator = true;
            }
        }

        return result.ToString();
    }

    public static bool MatchesAllTerms(string normalizedSearchKey, string normalizedQuery)
    {
        ArgumentNullException.ThrowIfNull(normalizedSearchKey);
        ArgumentNullException.ThrowIfNull(normalizedQuery);

        var termStart = 0;
        while (termStart < normalizedQuery.Length)
        {
            var termEnd = normalizedQuery.IndexOf(' ', termStart);
            if (termEnd < 0)
            {
                termEnd = normalizedQuery.Length;
            }

            if (!normalizedSearchKey.AsSpan().Contains(
                    normalizedQuery.AsSpan(termStart, termEnd - termStart),
                    StringComparison.Ordinal))
            {
                return false;
            }

            termStart = termEnd + 1;
        }

        return true;
    }
}
