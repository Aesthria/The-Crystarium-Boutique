using System.Collections.Immutable;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Sessions;

namespace CrystariumBoutique.Core.Loadouts;

public static class LoadoutSchema
{
    public const int CurrentVersion = 1;
    public const int MaximumNameLength = 80;
    public const int MaximumNotesLength = 2000;
    public const int MaximumTags = 32;
    public const int MaximumTagLength = 40;
}

public sealed record LoadoutEquipmentState(
    EquipmentSlot Slot,
    AppearanceSelection Appearance,
    byte DyeChannelCount,
    string DisplayName,
    uint IconId)
{
    public static LoadoutEquipmentState FromPreview(PreviewEquipmentState equipment)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        return new LoadoutEquipmentState(
            equipment.Slot,
            equipment.Appearance,
            equipment.DyeChannelCount,
            equipment.DisplayName,
            equipment.IconId);
    }

    public PreviewEquipmentState ToPreview()
        => new(Slot, Appearance, DyeChannelCount, DisplayName, IconId);
}

public sealed record BoutiqueLoadout(
    int SchemaVersion,
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    ImmutableArray<LoadoutEquipmentState> Equipment,
    ImmutableArray<string> Tags,
    string? Notes);

public sealed record LoadoutStoreSnapshot(
    ImmutableArray<BoutiqueLoadout> Loadouts,
    ImmutableArray<string> Warnings,
    int MigratedCount,
    int QuarantinedCount);

public static class LoadoutValidation
{
    public static Result Validate(BoutiqueLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);

        if (loadout.SchemaVersion != LoadoutSchema.CurrentVersion)
        {
            return Invalid($"Unsupported schema version {loadout.SchemaVersion}.");
        }

        if (loadout.Id == Guid.Empty)
        {
            return Invalid("The loadout ID is empty.");
        }

        if (string.IsNullOrWhiteSpace(loadout.Name)
            || loadout.Name.Trim().Length > LoadoutSchema.MaximumNameLength)
        {
            return Invalid($"Loadout names must contain 1–{LoadoutSchema.MaximumNameLength} characters.");
        }

        if (loadout.CreatedAtUtc == default
            || loadout.UpdatedAtUtc == default
            || loadout.UpdatedAtUtc < loadout.CreatedAtUtc)
        {
            return Invalid("The loadout timestamps are invalid.");
        }

        if (loadout.Equipment.IsDefaultOrEmpty)
        {
            return Invalid("A loadout must contain at least one changed equipment slot.");
        }

        if (loadout.Equipment.Select(equipment => equipment.Slot).Distinct().Count() != loadout.Equipment.Length)
        {
            return Invalid("A loadout contains the same equipment slot more than once.");
        }

        foreach (var equipment in loadout.Equipment)
        {
            if (!Enum.IsDefined(equipment.Slot)
                || equipment.Appearance.AppearanceId.Value == 0
                || equipment.Appearance.SourceItemId is null or 0
                || equipment.DyeChannelCount > 2
                || equipment.Appearance.Stains.Length > 2
                || string.IsNullOrWhiteSpace(equipment.DisplayName))
            {
                return Invalid($"The saved {equipment.Slot} equipment state is invalid.");
            }
        }

        if (!loadout.Tags.IsDefault
            && (loadout.Tags.Length > LoadoutSchema.MaximumTags
                || loadout.Tags.Any(tag => string.IsNullOrWhiteSpace(tag)
                    || tag.Trim().Length > LoadoutSchema.MaximumTagLength)))
        {
            return Invalid("The loadout tags are invalid.");
        }

        if (loadout.Notes?.Length > LoadoutSchema.MaximumNotesLength)
        {
            return Invalid($"Loadout notes cannot exceed {LoadoutSchema.MaximumNotesLength} characters.");
        }

        return Result.Ok;
    }

    private static Result Invalid(string technicalContext)
        => Result.Failure(new BoutiqueError(
            BoutiqueErrorCode.LoadoutInvalid,
            "The saved Boutique loadout is invalid.",
            technicalContext));
}
