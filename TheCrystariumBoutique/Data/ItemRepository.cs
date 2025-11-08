using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection; // <-- Add this using directive
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Interface.Textures;

namespace TheCrystariumBoutique;

public sealed class ItemRepository : IDisposable
{
    private readonly ExcelSheet<Item> _items = new ExcelSheet<Item>();
    private readonly ExcelSheet<Stain> _stains = new ExcelSheet<Stain>();
    private readonly ExcelSheet<ClassJob> _classJobs = new ExcelSheet<ClassJob>();
    private readonly Dictionary<uint, IDalamudTextureWrap> _iconCache = new Dictionary<uint, IDalamudTextureWrap>();
    private readonly Dictionary<GearSlot, List<Item>> _bySlot = new Dictionary<GearSlot, List<Item>>();

    // Cache of enabled job abbrevs per ClassJobCategory row id
    private readonly Dictionary<uint, HashSet<string>> _cjcEnabledCache = new Dictionary<uint, HashSet<string>>();

    public IReadOnlyDictionary<GearSlot, List<Item>> ItemsBySlot => _bySlot;
    public IReadOnlyList<Stain> Stains => _stains;

    // Precompute bool properties on ClassJobCategory for reflection-lite access
    private static readonly PropertyInfo[] ClassJobBoolProps = typeof(ClassJobCategory)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.PropertyType == typeof(bool))
        .ToArray();

    private static readonly Dictionary<string, PropertyInfo> JobPropMap = ClassJobBoolProps
        .ToDictionary(p => p.Name.ToUpperInvariant(), p => p, StringComparer.OrdinalIgnoreCase);

    // Job groups used to infer armor family (case sensitivity not critical in stubs, drop comparer to avoid constructor issues)
    private static readonly HashSet<string> Tanks = NewSet("GLA", "PLD", "MRD", "WAR", "DRK", "GNB");
    private static readonly HashSet<string> Maiming = NewSet("LNC", "DRG");
    private static readonly HashSet<string> Striking = NewSet("PGL", "MNK", "SAM");
    private static readonly HashSet<string> Scouting = NewSet("ROG", "NIN");
    private static readonly HashSet<string> Aiming = NewSet("ARC", "BRD", "MCH", "DNC");
    private static readonly HashSet<string> Healing = NewSet("CNJ", "WHM", "SCH", "AST", "SGE");
    private static readonly HashSet<string> Casting = NewSet("THM", "BLM", "ACN", "SMN", "RDM", "BLU", "PCT");
    private static readonly HashSet<string> Crafting = NewSet("CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL");
    private static readonly HashSet<string> Gathering = NewSet("MIN", "BTN", "FSH");

    private static readonly HashSet<string> DoW = CombineSets(Tanks, Maiming, Striking, Scouting, Aiming);
    private static readonly HashSet<string> DoM = CombineSets(Healing, Casting);

    private static HashSet<string> NewSet(params string[] values)
    {
        var hs = new HashSet<string>();
        foreach (var v in values) hs.Add(v);
        return hs;
    }

    private static HashSet<string> CombineSets(params HashSet<string>[] groups)
    {
        var hs = new HashSet<string>();
        foreach (var g in groups)
            foreach (var s in g) hs.Add(s);
        return hs;
    }

    public ItemRepository()
    {
        foreach (var slot in Enum.GetValues(typeof(GearSlot)).Cast<GearSlot>())
            _bySlot[slot] = new List<Item>();
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
    private HashSet<string> GetEnabledJobAbbrevs(ClassJobCategory cat) => new HashSet<string>();
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
