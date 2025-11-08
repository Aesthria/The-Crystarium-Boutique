using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Interface.Textures;

namespace TheCrystariumBoutique;

public sealed class ItemRepository : IDisposable
{
    private readonly ExcelSheet<Item> _items = new();
    private readonly ExcelSheet<Stain> _stains = new();
    private readonly ExcelSheet<ClassJob> _classJobs = new();
    private readonly Dictionary<uint, IDalamudTextureWrap> _iconCache = new();
    private readonly Dictionary<GearSlot, List<Item>> _bySlot = new();

    // Cache of enabled job abbrevs per ClassJobCategory row id
    private readonly Dictionary<uint, HashSet<string>> _cjcEnabledCache = new();

    public IReadOnlyDictionary<GearSlot, List<Item>> ItemsBySlot => _bySlot;
    public IReadOnlyList<Stain> Stains => _stains;

    // Precompute bool properties on ClassJobCategory for reflection-lite access
    private static readonly PropertyInfo[] ClassJobBoolProps = typeof(ClassJobCategory)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.PropertyType == typeof(bool))
        .ToArray();

    private static readonly Dictionary<string, PropertyInfo> JobPropMap = ClassJobBoolProps
        .ToDictionary(p => p.Name.ToUpperInvariant(), p => p, StringComparer.OrdinalIgnoreCase);

    // Job groups used to infer armor family
    private static readonly HashSet<string> Tanks = new(StringComparer.OrdinalIgnoreCase) { "GLA", "PLD", "MRD", "WAR", "DRK", "GNB" };
    private static readonly HashSet<string> Maiming = new(StringComparer.OrdinalIgnoreCase) { "LNC", "DRG" };
    private static readonly HashSet<string> Striking = new(StringComparer.OrdinalIgnoreCase) { "PGL", "MNK", "SAM" };
    private static readonly HashSet<string> Scouting = new(StringComparer.OrdinalIgnoreCase) { "ROG", "NIN" };
    private static readonly HashSet<string> Aiming = new(StringComparer.OrdinalIgnoreCase) { "ARC", "BRD", "MCH", "DNC" };
    private static readonly HashSet<string> Healing = new(StringComparer.OrdinalIgnoreCase) { "CNJ", "WHM", "SCH", "AST", "SGE" };
    private static readonly HashSet<string> Casting = new(StringComparer.OrdinalIgnoreCase) { "THM", "BLM", "ACN", "SMN", "RDM", "BLU", "PCT" };

    private static readonly HashSet<string> Crafting = new(StringComparer.OrdinalIgnoreCase) { "CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL" };
    private static readonly HashSet<string> Gathering = new(StringComparer.OrdinalIgnoreCase) { "MIN", "BTN", "FSH" };

    private static readonly HashSet<string> DoW = new(StringComparer.OrdinalIgnoreCase)
        .Concat(Tanks).Concat(Maiming).Concat(Striking).Concat(Scouting).Concat(Aiming).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> DoM = new(StringComparer.OrdinalIgnoreCase)
        .Concat(Healing).Concat(Casting).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public ItemRepository()
    {
        // Stub data for compilation environment. Real plugin populates from Lumina via Svc.DataManager.
        foreach (var slot in Enum.GetValues(typeof(GearSlot)).Cast<GearSlot>())
        {
            _bySlot[slot] = new List<Item>();
        }
    }

    public IEnumerable<Item> GetFilteredForSlot(
        GearSlot slot,
        string search,
        bool raceExclusiveOnly,
        bool applyJobFilterForWeapons,
        ArmorTypeFilter armorTypeFilter)
    {
        var list = _bySlot.TryGetValue(slot, out var items) ? items : new List<Item>();
        IEnumerable<Item> query = list;
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        return query;
    }

    public ArmorTypeFilter ClassifyArmorType(Item item) => ArmorTypeFilter.Universal;
    private HashSet<string> GetEnabledJobAbbrevs(ClassJobCategory cat) => new();
    private bool IsAllowedForCurrentJob(Item item) => true;
    public IDalamudTextureWrap? GetIcon(uint iconId)
    {
        if (_iconCache.TryGetValue(iconId, out var wrap))
            return wrap;
        return null; // Stub: real implementation would use Svc.TextureProvider.GetFromGameIcon(iconId)
    }
    public void Dispose()
    {
        foreach (var kv in _iconCache)
            kv.Value.Dispose();
        _iconCache.Clear();
    }
}
