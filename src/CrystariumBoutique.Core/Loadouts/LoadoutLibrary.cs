using System.Collections.Immutable;
using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Sessions;

namespace CrystariumBoutique.Core.Loadouts;

public sealed class LoadoutLibrary
{
    private readonly ILoadoutStore store;
    private readonly Func<DateTimeOffset> getUtcNow;
    private readonly Dictionary<Guid, BoutiqueLoadout> loadouts = [];
    private ImmutableArray<BoutiqueLoadout> orderedLoadouts = ImmutableArray<BoutiqueLoadout>.Empty;

    public LoadoutLibrary(ILoadoutStore store, Func<DateTimeOffset>? getUtcNow = null)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.getUtcNow = getUtcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public ImmutableArray<BoutiqueLoadout> Loadouts => orderedLoadouts;

    public LoadoutStoreSnapshot? LastRead { get; private set; }

    public Result Initialize()
    {
        var readResult = store.ReadAll();
        if (!readResult.IsSuccess || readResult.Value is null)
        {
            return Result.Failure(readResult.Error ?? new BoutiqueError(
                BoutiqueErrorCode.LoadoutPersistenceFailed,
                "The Boutique loadout library could not be initialized."));
        }

        loadouts.Clear();
        foreach (var loadout in readResult.Value.Loadouts)
        {
            loadouts[loadout.Id] = loadout;
        }

        RefreshOrderedLoadouts();
        LastRead = readResult.Value;
        return Result.Ok;
    }

    public bool TryGet(Guid id, out BoutiqueLoadout loadout)
        => loadouts.TryGetValue(id, out loadout!);

    public Result<BoutiqueLoadout> Create(
        string name,
        IEnumerable<PreviewEquipmentState> equipment,
        string? notes = null,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        var normalizedName = NormalizeName(name);
        var nameValidation = ValidateUniqueName(normalizedName);
        if (!nameValidation.IsSuccess)
        {
            return Result.Failure<BoutiqueLoadout>(nameValidation.Error!);
        }

        var now = getUtcNow();
        var loadout = new BoutiqueLoadout(
            LoadoutSchema.CurrentVersion,
            Guid.NewGuid(),
            normalizedName,
            now,
            now,
            equipment
                .Select(LoadoutEquipmentState.FromPreview)
                .OrderBy(item => item.Slot)
                .ToImmutableArray(),
            NormalizeTags(tags),
            NormalizeNotes(notes));
        return PersistNew(loadout);
    }

    public Result<BoutiqueLoadout> Rename(Guid id, string name)
    {
        if (!loadouts.TryGetValue(id, out var existing))
        {
            return NotFound<BoutiqueLoadout>(id);
        }

        var normalizedName = NormalizeName(name);
        var nameValidation = ValidateUniqueName(normalizedName, id);
        if (!nameValidation.IsSuccess)
        {
            return Result.Failure<BoutiqueLoadout>(nameValidation.Error!);
        }

        return PersistUpdate(existing with
        {
            Name = normalizedName,
            UpdatedAtUtc = getUtcNow(),
        });
    }

    public Result<BoutiqueLoadout> Duplicate(Guid id, string name)
    {
        if (!loadouts.TryGetValue(id, out var existing))
        {
            return NotFound<BoutiqueLoadout>(id);
        }

        var normalizedName = NormalizeName(name);
        var nameValidation = ValidateUniqueName(normalizedName);
        if (!nameValidation.IsSuccess)
        {
            return Result.Failure<BoutiqueLoadout>(nameValidation.Error!);
        }

        var now = getUtcNow();
        return PersistNew(existing with
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
    }

    public Result<BoutiqueLoadout> Import(LoadoutExchangeDesign design)
    {
        ArgumentNullException.ThrowIfNull(design);
        var name = SuggestImportedName(NormalizeName(design.Name));
        var now = getUtcNow();
        return PersistNew(new BoutiqueLoadout(
            LoadoutSchema.CurrentVersion,
            Guid.NewGuid(),
            name,
            now,
            now,
            design.Equipment.OrderBy(item => item.Slot).ToImmutableArray(),
            NormalizeTags(design.Tags),
            NormalizeNotes(design.Notes)));
    }

    public Result<BoutiqueLoadout> Overwrite(
        Guid id,
        IEnumerable<PreviewEquipmentState> equipment,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        if (!loadouts.TryGetValue(id, out var existing))
        {
            return NotFound<BoutiqueLoadout>(id);
        }

        return PersistUpdate(existing with
        {
            Equipment = equipment
                .Select(LoadoutEquipmentState.FromPreview)
                .OrderBy(item => item.Slot)
                .ToImmutableArray(),
            Notes = notes is null ? existing.Notes : NormalizeNotes(notes),
            UpdatedAtUtc = getUtcNow(),
        });
    }

    public Result Delete(Guid id)
    {
        if (!loadouts.ContainsKey(id))
        {
            return NotFound(id);
        }

        var result = store.Delete(id);
        if (result.IsSuccess)
        {
            loadouts.Remove(id);
            RefreshOrderedLoadouts();
        }

        return result;
    }

    public string SuggestDuplicateName(BoutiqueLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        var copyNumber = 1;
        var candidate = BuildCopyName(loadout.Name, copyNumber);
        while (HasName(candidate))
        {
            candidate = BuildCopyName(loadout.Name, ++copyNumber);
        }

        return candidate;
    }

    private Result<BoutiqueLoadout> PersistNew(BoutiqueLoadout loadout)
    {
        var validation = LoadoutValidation.Validate(loadout);
        if (!validation.IsSuccess)
        {
            return Result.Failure<BoutiqueLoadout>(validation.Error!);
        }

        var writeResult = store.Write(loadout);
        if (!writeResult.IsSuccess)
        {
            return Result.Failure<BoutiqueLoadout>(writeResult.Error!);
        }

        loadouts.Add(loadout.Id, loadout);
        RefreshOrderedLoadouts();
        return Result.Success(loadout);
    }

    private Result<BoutiqueLoadout> PersistUpdate(BoutiqueLoadout loadout)
    {
        var validation = LoadoutValidation.Validate(loadout);
        if (!validation.IsSuccess)
        {
            return Result.Failure<BoutiqueLoadout>(validation.Error!);
        }

        var writeResult = store.Write(loadout);
        if (!writeResult.IsSuccess)
        {
            return Result.Failure<BoutiqueLoadout>(writeResult.Error!);
        }

        loadouts[loadout.Id] = loadout;
        RefreshOrderedLoadouts();
        return Result.Success(loadout);
    }

    private Result ValidateUniqueName(string name, Guid? exceptId = null)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > LoadoutSchema.MaximumNameLength)
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.LoadoutInvalid,
                $"Loadout names must contain 1–{LoadoutSchema.MaximumNameLength} characters."));
        }

        if (loadouts.Values.Any(loadout => loadout.Id != exceptId
            && string.Equals(loadout.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(new BoutiqueError(
                BoutiqueErrorCode.LoadoutInvalid,
                "A Boutique loadout with that name already exists."));
        }

        return Result.Ok;
    }

    private bool HasName(string name)
        => loadouts.Values.Any(loadout => string.Equals(loadout.Name, name, StringComparison.OrdinalIgnoreCase));

    private string SuggestImportedName(string sourceName)
    {
        if (!HasName(sourceName))
        {
            return sourceName;
        }

        var number = 1;
        var candidate = BuildImportedName(sourceName, number);
        while (HasName(candidate))
        {
            candidate = BuildImportedName(sourceName, ++number);
        }

        return candidate;
    }

    private static string BuildImportedName(string sourceName, int number)
    {
        var suffix = number == 1 ? " Imported" : $" Imported {number}";
        var maximumSourceLength = Math.Max(1, LoadoutSchema.MaximumNameLength - suffix.Length);
        return $"{sourceName[..Math.Min(sourceName.Length, maximumSourceLength)]}{suffix}";
    }

    private static string BuildCopyName(string sourceName, int copyNumber)
    {
        var suffix = copyNumber == 1 ? " Copy" : $" Copy {copyNumber}";
        var maximumSourceLength = Math.Max(1, LoadoutSchema.MaximumNameLength - suffix.Length);
        var truncatedSource = sourceName.Length <= maximumSourceLength
            ? sourceName
            : sourceName[..maximumSourceLength];
        return $"{truncatedSource}{suffix}";
    }

    private void RefreshOrderedLoadouts()
        => orderedLoadouts = loadouts.Values
            .OrderByDescending(loadout => loadout.UpdatedAtUtc)
            .ThenBy(loadout => loadout.Name, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

    private static string NormalizeName(string? name)
        => name?.Trim() ?? string.Empty;

    private static string? NormalizeNotes(string? notes)
        => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    private static ImmutableArray<string> NormalizeTags(IEnumerable<string>? tags)
        => tags is null
            ? ImmutableArray<string>.Empty
            : tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToImmutableArray();

    private static Result<T> NotFound<T>(Guid id)
        where T : notnull
        => Result.Failure<T>(new BoutiqueError(
            BoutiqueErrorCode.LoadoutNotFound,
            "The requested Boutique loadout does not exist.",
            id.ToString()));

    private static Result NotFound(Guid id)
        => Result.Failure(new BoutiqueError(
            BoutiqueErrorCode.LoadoutNotFound,
            "The requested Boutique loadout does not exist.",
            id.ToString()));
}
