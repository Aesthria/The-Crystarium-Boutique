using Lumina;
using Lumina.Data;
using LuminaBNpcName = Lumina.Excel.Sheets.BNpcName;
using LuminaContentFinderCondition = Lumina.Excel.Sheets.ContentFinderCondition;
using LuminaItem = Lumina.Excel.Sheets.Item;
using LuminaMap = Lumina.Excel.Sheets.Map;
using LuminaTerritoryType = Lumina.Excel.Sheets.TerritoryType;
using LuminaTreasure = Lumina.Excel.Sheets.Treasure;

namespace CrystariumBoutique.AcquisitionGenerator;

public sealed class LuminaGameDataIndex : IGameDataIndex
{
    private readonly Dictionary<uint, GameItem> items;
    private readonly Dictionary<uint, GameDuty> duties;
    private readonly HashSet<uint> territories;
    private readonly HashSet<uint> treasures;
    private readonly Dictionary<uint, uint> mapTerritories;
    private readonly Dictionary<uint, string> bossNames;

    private LuminaGameDataIndex(
        string gameBuild,
        string luminaVersion,
        Dictionary<uint, GameItem> items,
        Dictionary<uint, GameDuty> duties,
        HashSet<uint> territories,
        HashSet<uint> treasures,
        Dictionary<uint, uint> mapTerritories,
        Dictionary<uint, string> bossNames)
    {
        GameBuild = gameBuild;
        LuminaVersion = luminaVersion;
        this.items = items;
        this.duties = duties;
        this.territories = territories;
        this.treasures = treasures;
        this.mapTerritories = mapTerritories;
        this.bossNames = bossNames;
    }

    public string GameBuild { get; }

    public string LuminaVersion { get; }

    public int EquipmentItemCount => items.Values.Count(item => item.IsEquipment);

    public static LuminaGameDataIndex Load(string gamePath, string? gameBuildOverride = null)
    {
        var sqpackPath = ResolveSqpackPath(gamePath);
        var options = new LuminaOptions
        {
            DefaultExcelLanguage = Language.English,
            LoadMultithreaded = true,
        };
        using var data = new GameData(sqpackPath, options);
        var items = (data.GetExcelSheet<LuminaItem>(Language.English)
                ?? throw new InvalidDataException("The local Lumina Item sheet is unavailable."))
            .Where(row => row.RowId > 0)
            .ToDictionary(
                row => row.RowId,
                row => new GameItem(
                    row.RowId,
                    row.Name.ExtractText().Trim(),
                    row.EquipSlotCategory.RowId > 0));
        var duties = (data.GetExcelSheet<LuminaContentFinderCondition>(Language.English)
                ?? throw new InvalidDataException("The local Lumina ContentFinderCondition sheet is unavailable."))
            .Where(row => row.RowId > 0)
            .ToDictionary(
                row => row.RowId,
                row => CreateDuty(row));
        var territories = (data.GetExcelSheet<LuminaTerritoryType>(Language.English)
                ?? throw new InvalidDataException("The local Lumina TerritoryType sheet is unavailable."))
            .Where(row => row.RowId > 0)
            .Select(row => row.RowId)
            .ToHashSet();
        var treasures = (data.GetExcelSheet<LuminaTreasure>(Language.English)
                ?? throw new InvalidDataException("The local Lumina Treasure sheet is unavailable."))
            .Where(row => row.RowId > 0)
            .Select(row => row.RowId)
            .ToHashSet();
        var maps = (data.GetExcelSheet<LuminaMap>(Language.English)
                ?? throw new InvalidDataException("The local Lumina Map sheet is unavailable."))
            .Where(row => row.RowId > 0)
            .ToDictionary(row => row.RowId, row => row.TerritoryType.RowId);
        var bossNames = (data.GetExcelSheet<LuminaBNpcName>(Language.English)
                ?? throw new InvalidDataException("The local Lumina BNpcName sheet is unavailable."))
            .Where(row => row.RowId > 0)
            .Select(row => new
            {
                row.RowId,
                Name = row.Singular.ExtractText().Trim(),
            })
            .Where(row => row.Name.Length > 0)
            .ToDictionary(row => row.RowId, row => row.Name);
        var luminaVersion = $"Lumina {typeof(GameData).Assembly.GetName().Version}; "
            + $"Lumina.Excel {typeof(LuminaItem).Assembly.GetName().Version}";
        return new LuminaGameDataIndex(
            ResolveGameBuild(sqpackPath, gameBuildOverride),
            luminaVersion,
            items,
            duties,
            territories,
            treasures,
            maps,
            bossNames);
    }

    public bool TryGetItem(uint itemId, out GameItem item)
        => items.TryGetValue(itemId, out item!);

    public bool TryGetDuty(uint contentFinderConditionId, out GameDuty duty)
        => duties.TryGetValue(contentFinderConditionId, out duty!);

    public bool ContainsTerritory(uint territoryTypeId)
        => territories.Contains(territoryTypeId);

    public bool ContainsTreasure(uint treasureId)
        => treasures.Contains(treasureId);

    public bool TryGetMapTerritory(uint mapId, out uint territoryTypeId)
        => mapTerritories.TryGetValue(mapId, out territoryTypeId);

    public bool TryGetBossName(uint bNpcNameId, out string bossName)
        => bossNames.TryGetValue(bNpcNameId, out bossName!);

    private static GameDuty CreateDuty(LuminaContentFinderCondition row)
    {
        var name = row.Name.ExtractText().Trim();
        var rawType = row.ContentType.ValueNullable is { } contentType
            ? contentType.Name.ExtractText().Trim()
            : "";
        var normalizedType = row.AllianceRoulette
            ? "Alliance Raid"
            : rawType switch
            {
                var value when value.Contains("Dungeon", StringComparison.OrdinalIgnoreCase) => "Dungeon",
                var value when value.Contains("Trial", StringComparison.OrdinalIgnoreCase) => "Trial",
                var value when value.Contains("Raid", StringComparison.OrdinalIgnoreCase) => "Raid",
                _ => rawType,
            };
        var difficulty = ResolveDifficulty(name, normalizedType);
        var expansion = row.RequiredExVersion.ValueNullable is { } exVersion
            ? exVersion.Name.ExtractText().Trim()
            : "";
        return new GameDuty(
            row.RowId,
            name,
            normalizedType,
            row.TerritoryType.RowId,
            difficulty,
            expansion);
    }

    public static string ResolveDifficulty(string dutyName, string contentType)
    {
        if (dutyName.Contains("Minstrel's Ballad", StringComparison.OrdinalIgnoreCase))
        {
            return "Extreme";
        }

        foreach (var value in new[] { "Ultimate", "Savage", "Extreme", "Hard", "Unreal" })
        {
            if (dutyName.Contains(value, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return contentType switch
        {
            "Alliance Raid" => "Alliance Raid",
            "Raid" => "Normal Raid",
            "Dungeon" or "Trial" => "Normal",
            _ => "",
        };
    }

    private static string ResolveSqpackPath(string gamePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gamePath);
        var fullPath = Path.GetFullPath(gamePath);
        var candidates = new[]
        {
            fullPath,
            Path.Combine(fullPath, "sqpack"),
            Path.Combine(fullPath, "game", "sqpack"),
        };
        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Directory.Exists(Path.Combine(candidate, "ffxiv")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not locate a game sqpack directory beneath '{fullPath}'.");
    }

    private static string ResolveGameBuild(string sqpackPath, string? gameBuildOverride)
    {
        if (!string.IsNullOrWhiteSpace(gameBuildOverride))
        {
            return gameBuildOverride.Trim();
        }

        var marker = Path.Combine(Directory.GetParent(sqpackPath)?.FullName ?? "", "ffxivgame.ver");
        if (!File.Exists(marker))
        {
            throw new FileNotFoundException(
                "The local ffxivgame.ver marker was not found; provide --game-build explicitly.",
                marker);
        }

        return File.ReadAllText(marker).Trim();
    }
}
