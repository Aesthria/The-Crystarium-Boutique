using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Errors;

namespace CrystariumBoutique.Core.Loadouts;

public sealed class JsonLoadoutStore : ILoadoutStore
{
    private const string FileExtension = ".json";
    private readonly string rootDirectory;
    private readonly Func<DateTimeOffset> getUtcNow;
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public JsonLoadoutStore(string rootDirectory, Func<DateTimeOffset>? getUtcNow = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        this.rootDirectory = Path.GetFullPath(rootDirectory);
        this.getUtcNow = getUtcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public Result<LoadoutStoreSnapshot> ReadAll()
    {
        try
        {
            Directory.CreateDirectory(rootDirectory);
            var loadouts = ImmutableArray.CreateBuilder<BoutiqueLoadout>();
            var warnings = ImmutableArray.CreateBuilder<string>();
            var migratedCount = 0;
            var quarantinedCount = 0;

            foreach (var filePath in Directory.EnumerateFiles(rootDirectory, $"*{FileExtension}", SearchOption.TopDirectoryOnly)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var readResult = ReadOne(filePath);
                if (!readResult.IsSuccess || readResult.Value is null)
                {
                    quarantinedCount++;
                    warnings.Add(readResult.Error?.ToString() ?? $"Could not read {Path.GetFileName(filePath)}.");
                    Quarantine(filePath, warnings);
                    continue;
                }

                loadouts.Add(readResult.Value.Loadout);
                if (readResult.Value.WasMigrated)
                {
                    migratedCount++;
                    var rewriteResult = Write(readResult.Value.Loadout);
                    if (!rewriteResult.IsSuccess)
                    {
                        warnings.Add($"Migrated {Path.GetFileName(filePath)} in memory but could not rewrite it: {rewriteResult.Error}");
                    }
                }
            }

            var duplicateIds = loadouts
                .GroupBy(loadout => loadout.Id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet();
            if (duplicateIds.Count > 0)
            {
                warnings.Add($"Ignored {duplicateIds.Count} duplicated loadout IDs.");
            }

            return Result.Success(new LoadoutStoreSnapshot(
                loadouts
                    .Where(loadout => !duplicateIds.Contains(loadout.Id))
                    .OrderByDescending(loadout => loadout.UpdatedAtUtc)
                    .ThenBy(loadout => loadout.Name, StringComparer.OrdinalIgnoreCase)
                    .ToImmutableArray(),
                warnings.ToImmutable(),
                migratedCount,
                quarantinedCount));
        }
        catch (Exception exception)
        {
            return Result.Failure<LoadoutStoreSnapshot>(PersistenceFailure(
                "The Boutique loadout library could not be opened.",
                exception));
        }
    }

    public Result Write(BoutiqueLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        var validation = LoadoutValidation.Validate(loadout);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        try
        {
            Directory.CreateDirectory(rootDirectory);
            var path = GetPath(loadout.Id);
            var temporaryPath = $"{path}.tmp";
            var document = ToDocument(loadout);
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, jsonOptions));
            File.Move(temporaryPath, path, overwrite: true);
            return Result.Ok;
        }
        catch (Exception exception)
        {
            return Result.Failure(PersistenceFailure(
                "The Boutique loadout could not be saved.",
                exception));
        }
    }

    public Result Delete(Guid loadoutId)
    {
        if (loadoutId == Guid.Empty)
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.LoadoutNotFound,
                "The requested Boutique loadout does not exist."));
        }

        try
        {
            var path = GetPath(loadoutId);
            if (!File.Exists(path))
            {
                return Result.Failure(new BoutiqueError(
                    BoutiqueErrorCode.LoadoutNotFound,
                    "The requested Boutique loadout does not exist.",
                    loadoutId.ToString()));
            }

            var deletedDirectory = Path.Combine(rootDirectory, "deleted");
            Directory.CreateDirectory(deletedDirectory);
            var timestamp = getUtcNow().UtcDateTime.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
            var destination = Path.Combine(deletedDirectory, $"{loadoutId:N}.{timestamp}{FileExtension}");
            File.Move(path, destination);
            return Result.Ok;
        }
        catch (Exception exception)
        {
            return Result.Failure(PersistenceFailure(
                "The Boutique loadout could not be deleted.",
                exception));
        }
    }

    private Result<ReadDocumentResult> ReadOne(string filePath)
    {
        try
        {
            var fileId = ParseFileId(filePath);
            var json = File.ReadAllText(filePath);
            using var parsed = JsonDocument.Parse(json);
            var schemaVersion = GetSchemaVersion(parsed.RootElement);
            var document = JsonSerializer.Deserialize<LoadoutDocument>(json, jsonOptions)
                ?? throw new JsonException("The loadout document was empty.");
            var wasMigrated = schemaVersion == 0;

            if (schemaVersion is not (0 or LoadoutSchema.CurrentVersion))
            {
                return Result.Failure<ReadDocumentResult>(new BoutiqueError(
                    BoutiqueErrorCode.ConfigurationCorrupt,
                    "A Boutique loadout uses an unsupported schema version.",
                    $"File={Path.GetFileName(filePath)}; Schema={schemaVersion}"));
            }

            if (schemaVersion == 0)
            {
                var timestamp = new DateTimeOffset(File.GetLastWriteTimeUtc(filePath), TimeSpan.Zero);
                document.SchemaVersion = LoadoutSchema.CurrentVersion;
                document.Id = fileId;
                document.CreatedAtUtc = timestamp;
                document.UpdatedAtUtc = timestamp;
            }
            else if (document.Id != fileId)
            {
                throw new JsonException("The document ID does not match its GUID filename.");
            }

            var conversion = FromDocument(document);
            return conversion.IsSuccess && conversion.Value is not null
                ? Result.Success(new ReadDocumentResult(conversion.Value, wasMigrated))
                : Result.Failure<ReadDocumentResult>(conversion.Error!);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or FormatException)
        {
            return Result.Failure<ReadDocumentResult>(new BoutiqueError(
                BoutiqueErrorCode.ConfigurationCorrupt,
                "A saved Boutique loadout is corrupt and was isolated.",
                $"{Path.GetFileName(filePath)}: {exception.GetType().Name}: {exception.Message}"));
        }
    }

    private static Result<BoutiqueLoadout> FromDocument(LoadoutDocument document)
    {
        var equipment = (document.Equipment ?? [])
            .Select(item => new LoadoutEquipmentState(
                item.Slot,
                new AppearanceSelection(
                    new AppearanceId(item.AppearanceId),
                    item.SourceItemId,
                    (item.Stains ?? []).Take(2).Select(stain => new StainId(stain)).ToImmutableArray()),
                item.DyeChannelCount,
                item.DisplayName ?? string.Empty,
                item.IconId))
            .OrderBy(item => item.Slot)
            .ToImmutableArray();
        var loadout = new BoutiqueLoadout(
            document.SchemaVersion,
            document.Id,
            document.Name?.Trim() ?? string.Empty,
            document.CreatedAtUtc,
            document.UpdatedAtUtc,
            equipment,
            (document.Tags ?? [])
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToImmutableArray(),
            string.IsNullOrWhiteSpace(document.Notes) ? null : document.Notes.Trim());
        var validation = LoadoutValidation.Validate(loadout);
        return validation.IsSuccess
            ? Result.Success(loadout)
            : Result.Failure<BoutiqueLoadout>(validation.Error!);
    }

    private static LoadoutDocument ToDocument(BoutiqueLoadout loadout)
        => new()
        {
            SchemaVersion = LoadoutSchema.CurrentVersion,
            Id = loadout.Id,
            Name = loadout.Name,
            CreatedAtUtc = loadout.CreatedAtUtc,
            UpdatedAtUtc = loadout.UpdatedAtUtc,
            Equipment = loadout.Equipment
                .OrderBy(equipment => equipment.Slot)
                .Select(equipment => new LoadoutEquipmentDocument
                {
                    Slot = equipment.Slot,
                    AppearanceId = equipment.Appearance.AppearanceId.Value,
                    SourceItemId = equipment.Appearance.SourceItemId,
                    Stains = equipment.Appearance.Stains.Select(stain => stain.Value).ToList(),
                    DyeChannelCount = equipment.DyeChannelCount,
                    DisplayName = equipment.DisplayName,
                    IconId = equipment.IconId,
                })
                .ToList(),
            Tags = loadout.Tags.IsDefault ? [] : loadout.Tags.ToList(),
            Notes = loadout.Notes,
        };

    private void Quarantine(string filePath, ImmutableArray<string>.Builder warnings)
    {
        try
        {
            var quarantineDirectory = Path.Combine(rootDirectory, "quarantine");
            Directory.CreateDirectory(quarantineDirectory);
            var timestamp = getUtcNow().UtcDateTime.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
            var destination = Path.Combine(
                quarantineDirectory,
                $"{Path.GetFileNameWithoutExtension(filePath)}.{timestamp}.invalid{FileExtension}");
            File.Move(filePath, destination);
        }
        catch (Exception exception)
        {
            warnings.Add($"Could not quarantine {Path.GetFileName(filePath)}: {exception.Message}");
        }
    }

    private string GetPath(Guid loadoutId)
        => Path.Combine(rootDirectory, $"{loadoutId:N}{FileExtension}");

    private static Guid ParseFileId(string filePath)
        => Guid.TryParseExact(Path.GetFileNameWithoutExtension(filePath), "N", out var id)
            ? id
            : throw new FormatException("Loadout filenames must be GUIDs.");

    private static int GetSchemaVersion(JsonElement root)
    {
        if (root.TryGetProperty("schemaVersion", out var schema)
            || root.TryGetProperty("SchemaVersion", out schema))
        {
            return schema.GetInt32();
        }

        return 0;
    }

    private static BoutiqueError PersistenceFailure(string message, Exception exception)
        => new(
            BoutiqueErrorCode.LoadoutPersistenceFailed,
            message,
            $"{exception.GetType().Name}: {exception.Message}");

    private sealed record ReadDocumentResult(BoutiqueLoadout Loadout, bool WasMigrated);

    private sealed class LoadoutDocument
    {
        public int SchemaVersion { get; set; }

        public Guid Id { get; set; }

        public string? Name { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public List<LoadoutEquipmentDocument>? Equipment { get; set; }

        public List<string>? Tags { get; set; }

        public string? Notes { get; set; }
    }

    private sealed class LoadoutEquipmentDocument
    {
        public EquipmentSlot Slot { get; set; }

        public ulong AppearanceId { get; set; }

        public uint? SourceItemId { get; set; }

        public List<byte>? Stains { get; set; }

        public byte DyeChannelCount { get; set; }

        public string? DisplayName { get; set; }

        public uint IconId { get; set; }
    }
}
