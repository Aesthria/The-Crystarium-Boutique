using System.Globalization;

namespace CrystariumBoutique.AcquisitionGenerator;

public static class LuminaSupplementalDatasetReader
{
    public static readonly string[] RequiredFiles =
    [
        "DungeonBoss.csv",
        "DungeonBossChest.csv",
        "DungeonBossDrop.csv",
        "DungeonChest.csv",
        "DungeonChestItem.csv",
        "DungeonDrop.csv",
    ];

    public static LuminaSupplementalDataset Read(
        string directory,
        LuminaSupplementalSourceDescriptor source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(source);
        ValidateDescriptor(source);

        var root = Path.GetFullPath(directory);
        var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var fileName in RequiredFiles)
        {
            var path = ResolveFile(root, fileName);
            var actual = AcquisitionSupplementGenerator.ComputeSha256File(path);
            var expected = NormalizeHash(source.Files[fileName]);
            if (!actual.Equals(expected, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"LuminaSupplemental {fileName} hash mismatch; expected {expected}, got {actual}.");
            }

            hashes.Add(fileName, actual);
        }

        return new LuminaSupplementalDataset(
            ReadRows(ResolveFile(root, "DungeonBoss.csv"), 4, values => new DungeonBossRow(
                Parse(values[0]), Parse(values[1]), Parse(values[2]), Parse(values[3]))),
            ReadRows(ResolveFile(root, "DungeonBossChest.csv"), 6, values => new DungeonBossChestRow(
                Parse(values[0]), Parse(values[1]), Parse(values[2]), Parse(values[3]), Parse(values[4]), Parse(values[5]))),
            ReadRows(ResolveFile(root, "DungeonBossDrop.csv"), 5, values => new DungeonBossDropRow(
                Parse(values[0]), Parse(values[1]), Parse(values[2]), Parse(values[3]), Parse(values[4]))),
            ReadRows(ResolveFile(root, "DungeonChest.csv"), 8, values => new DungeonChestRow(
                Parse(values[0]), byte.Parse(values[1], CultureInfo.InvariantCulture), Parse(values[2]),
                Parse(values[3]), Parse(values[4]), Parse(values[5]), Parse(values[6]))),
            ReadRows(ResolveFile(root, "DungeonChestItem.csv"), 6, values => new DungeonChestItemRow(
                Parse(values[0]), Parse(values[1]), Parse(values[2]))),
            ReadRows(ResolveFile(root, "DungeonDrop.csv"), 3, values => new DungeonDropRow(
                Parse(values[0]), Parse(values[1]), Parse(values[2]))),
            hashes);
    }

    private static List<T> ReadRows<T>(
        string path,
        int expectedColumns,
        Func<string[], T> factory)
    {
        var result = new List<T>();
        var lineNumber = 0;
        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            if (lineNumber == 1 || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = line.Split(',');
            if (values.Length != expectedColumns)
            {
                throw new InvalidDataException(
                    $"{Path.GetFileName(path)} line {lineNumber} has {values.Length} columns; expected {expectedColumns}.");
            }

            try
            {
                result.Add(factory(values));
            }
            catch (Exception exception) when (exception is FormatException or OverflowException)
            {
                throw new InvalidDataException(
                    $"{Path.GetFileName(path)} line {lineNumber} contains an invalid numeric value.",
                    exception);
            }
        }

        return result;
    }

    private static uint Parse(string value)
        => uint.Parse(value, CultureInfo.InvariantCulture);

    private static string ResolveFile(string root, string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(root, fileName));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || !File.Exists(path))
        {
            throw new FileNotFoundException($"Required LuminaSupplemental dataset file is missing: {fileName}", path);
        }

        return path;
    }

    private static void ValidateDescriptor(LuminaSupplementalSourceDescriptor source)
    {
        if (!source.UpstreamSource.Equals("Critical-Impact/LuminaSupplemental", StringComparison.Ordinal)
            || source.UpstreamRevision.Length != 40
            || !source.UpstreamRevision.All(Uri.IsHexDigit)
            || string.IsNullOrWhiteSpace(source.UpstreamVersion)
            || !source.License.Equals("GPL-3.0-only", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(source.LicenseUrl))
        {
            throw new InvalidDataException("The pinned LuminaSupplemental source descriptor is incomplete or unsupported.");
        }

        foreach (var fileName in RequiredFiles)
        {
            if (!source.Files.TryGetValue(fileName, out var hash))
            {
                throw new InvalidDataException($"The source descriptor does not pin {fileName}.");
            }

            _ = NormalizeHash(hash);
        }
    }

    private static string NormalizeHash(string value)
    {
        var cleaned = value.Trim();
        if (cleaned.StartsWith("SHA256:", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[7..];
        }

        if (cleaned.Length != 64 || !cleaned.All(Uri.IsHexDigit))
        {
            throw new InvalidDataException("Expected a 64-character SHA-256 value.");
        }

        return "SHA256:" + cleaned.ToUpperInvariant();
    }
}

public sealed record LuminaSupplementalDataset(
    IReadOnlyList<DungeonBossRow> Bosses,
    IReadOnlyList<DungeonBossChestRow> BossChests,
    IReadOnlyList<DungeonBossDropRow> BossDrops,
    IReadOnlyList<DungeonChestRow> Chests,
    IReadOnlyList<DungeonChestItemRow> ChestItems,
    IReadOnlyList<DungeonDropRow> DutyDrops,
    SortedDictionary<string, string> FileHashes);

public sealed record DungeonBossRow(uint RowId, uint BNpcNameId, uint DutyId, uint FightNumber);

public sealed record DungeonBossChestRow(
    uint RowId, uint ItemId, uint DutyId, uint Quantity, uint FightNumber, uint CofferNumber);

public sealed record DungeonBossDropRow(
    uint RowId, uint DutyId, uint FightNumber, uint ItemId, uint Quantity);

public sealed record DungeonChestRow(
    uint RowId, byte ChestNumber, uint DutyId, uint MapId, uint TerritoryTypeId,
    uint TreasureId, uint DungeonBossId);

public sealed record DungeonChestItemRow(uint RowId, uint ItemId, uint ChestId);

public sealed record DungeonDropRow(uint RowId, uint ItemId, uint DutyId);
