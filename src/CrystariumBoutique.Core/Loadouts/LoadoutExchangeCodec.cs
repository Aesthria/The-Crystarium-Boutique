using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Errors;

namespace CrystariumBoutique.Core.Loadouts;

public sealed record LoadoutExchangeDesign(
    string Name,
    ImmutableArray<LoadoutEquipmentState> Equipment,
    ImmutableArray<string> Tags,
    string? Notes);

public static class LoadoutExchangeCodec
{
    public const string Prefix = "TCB-DESIGN-1:";
    private const int MaximumPayloadLength = 131_072;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Encode(BoutiqueLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        var validation = LoadoutValidation.Validate(loadout);
        if (!validation.IsSuccess)
        {
            throw new ArgumentException(validation.Error?.Message, nameof(loadout));
        }

        var document = new ExchangeDocument
        {
            SchemaVersion = LoadoutSchema.CurrentVersion,
            Name = loadout.Name,
            Equipment = loadout.Equipment
                .OrderBy(equipment => equipment.Slot)
                .Select(equipment => new ExchangeEquipmentDocument
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
        var json = JsonSerializer.Serialize(document, JsonOptions);
        return Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public static Result<LoadoutExchangeDesign> Decode(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return Invalid("The shared design text was empty.");
        }

        payload = payload.Trim();
        if (payload.Length > MaximumPayloadLength || !payload.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return Invalid("The shared design prefix or length was invalid.");
        }

        try
        {
            var bytes = Convert.FromBase64String(payload[Prefix.Length..]);
            var document = JsonSerializer.Deserialize<ExchangeDocument>(bytes, JsonOptions);
            if (document is null || document.SchemaVersion != LoadoutSchema.CurrentVersion)
            {
                return Invalid("The shared design schema is unsupported.");
            }

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
            var tags = (document.Tags ?? [])
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToImmutableArray();
            var design = new LoadoutExchangeDesign(
                document.Name?.Trim() ?? string.Empty,
                equipment,
                tags,
                string.IsNullOrWhiteSpace(document.Notes) ? null : document.Notes.Trim());
            var now = DateTimeOffset.UtcNow;
            var validation = LoadoutValidation.Validate(new BoutiqueLoadout(
                LoadoutSchema.CurrentVersion,
                Guid.NewGuid(),
                design.Name,
                now,
                now,
                design.Equipment,
                design.Tags,
                design.Notes));
            return validation.IsSuccess
                ? Result.Success(design)
                : Result.Failure<LoadoutExchangeDesign>(validation.Error!);
        }
        catch (Exception exception) when (exception is FormatException or JsonException or DecoderFallbackException)
        {
            return Invalid($"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static Result<LoadoutExchangeDesign> Invalid(string context)
        => Result.Failure<LoadoutExchangeDesign>(new BoutiqueError(
            BoutiqueErrorCode.LoadoutInvalid,
            "The shared Boutique design is invalid.",
            context));

    private sealed class ExchangeDocument
    {
        public int SchemaVersion { get; set; }

        public string? Name { get; set; }

        public List<ExchangeEquipmentDocument>? Equipment { get; set; }

        public List<string>? Tags { get; set; }

        public string? Notes { get; set; }
    }

    private sealed class ExchangeEquipmentDocument
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
