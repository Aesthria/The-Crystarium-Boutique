using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Dalamud.Interface.Textures.TextureWraps;
using Lumina.Excel.Sheets;

namespace TheCrystariumBoutique.Data;

public sealed class ItemRepository : IDisposable
{
    private readonly List<Stain> _stains;
    private readonly Dictionary<GearSlot, List<Item>> _bySlot = new();
    private readonly Dictionary<uint, IDalamudTextureWrap> _iconCache = new();

    public IReadOnlyDictionary<GearSlot, List<Item>> ItemsBySlot => _bySlot;
    public IReadOnlyList<Stain> Stains => _stains;

    public ItemRepository()
    {
        var itemSheet = Svc.DataManager.GetExcelSheet<Item>()
                        ?? throw new InvalidOperationException("Missing Item sheet.");
        var stainSheet = Svc.DataManager.GetExcelSheet<Stain>()
                         ?? throw new InvalidOperationException("Missing Stain sheet.");

        _stains = stainSheet.ToList();

        foreach (var slot in Enum.GetValues(typeof(GearSlot)).Cast<GearSlot>())
            _bySlot[slot] = new List<Item>();

        foreach (var item in itemSheet)
        {
            if (item.RowId == 0 || item.Icon == 0)
                continue;

            var slot = GuessSlot(item);
            if (slot == null)
                continue;

            _bySlot[slot.Value].Add(item);
        }

        // Sort per-slot for nicer browsing
        foreach (var list in _bySlot.Values)
        {
            list.Sort((a, b) =>
                string.Compare(a.Name.ToString(), b.Name.ToString(), StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Reflection-based slot guesser that tolerates Lumina schema changes.
    /// If we can't confidently classify, returns null.
    /// </summary>
    private static GearSlot? GuessSlot(Item item)
    {
        // In current Lumina this is typically a struct; no null-check.
        var cat = item.EquipSlotCategory.Value;

        // Look for flag-like properties (byte/bool) and infer from their names.
        foreach (var prop in cat.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var t = prop.PropertyType;
            if (t != typeof(byte) && t != typeof(bool))
                continue;

            var raw = prop.GetValue(cat);
            var on = raw switch
            {
                bool b => b,
                byte b => b != 0,
                _ => false
            };

            if (!on)
                continue;

            var name = prop.Name.ToLowerInvariant();

            if (name.Contains("mainhand") || name == "main")
                return GearSlot.MainHand;
            if (name.Contains("offhand"))
                return GearSlot.OffHand;
            if (name.Contains("head"))
                return GearSlot.Head;
            if (name.Contains("body"))
                return GearSlot.Body;
            if (name.Contains("hand") && !name.Contains("off"))
                return GearSlot.Hands;
            if (name.Contains("leg"))
                return GearSlot.Legs;
            if (name.Contains("feet") || name.Contains("foot") || name.Contains("shoe"))
                return GearSlot.Feet;
            if (name.Contains("ear"))
                return GearSlot.Ears;
            if (name.Contains("neck"))
                return GearSlot.Neck;
            if (name.Contains("wrist"))
                return GearSlot.Wrist;
            if (name.Contains("fingerl") || name.Contains("ringl"))
                return GearSlot.RingLeft;
            if (name.Contains("fingerr") || name.Contains("ringr"))
                return GearSlot.RingRight;
        }

        return null;
    }

    public IEnumerable<Item> GetFilteredForSlot(
        GearSlot slot,
        string search,
        bool raceExclusiveOnly,
        bool applyJobFilterForWeapons,
        ArmorTypeFilter armorTypeFilter)
    {
        if (!_bySlot.TryGetValue(slot, out var list))
            return Array.Empty<Item>();

        IEnumerable<Item> query = list;

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i =>
                i.Name.ToString().Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // Armor / job / race filters can be expanded later; keep simple & stable for now.
        return query;
    }

    public ArmorTypeFilter ClassifyArmorType(Item _)
        => ArmorTypeFilter.Universal;

    public IDalamudTextureWrap? GetIcon(uint iconId)
    {
        if (iconId == 0)
            return null;

        if (_iconCache.TryGetValue(iconId, out var cached))
            return cached;

        try
        {
            var provider = Svc.TextureProvider;
            if (provider == null)
                return null;

            // Try common signatures: GetFromGameIcon(uint) or GetIcon(uint)
            var mi =
                provider.GetType().GetMethod("GetFromGameIcon", new[] { typeof(uint) }) ??
                provider.GetType().GetMethod("GetIcon", new[] { typeof(uint) });

            if (mi == null)
                return null;

            var tex = mi.Invoke(provider, new object[] { iconId });
            if (tex == null)
                return null;

            // Direct wrap
            if (tex is IDalamudTextureWrap wrapDirect)
            {
                _iconCache[iconId] = wrapDirect;
                return wrapDirect;
            }

            // Indirect: property exposing a wrap
            var wrapProp =
                tex.GetType().GetProperty("Wrap") ??
                tex.GetType().GetProperty("TextureWrap");

            if (wrapProp?.GetValue(tex) is IDalamudTextureWrap wrap)
            {
                _iconCache[iconId] = wrap;
                return wrap;
            }
        }
        catch
        {
            // Icon loading is cosmetic – swallow failures.
        }

        return null;
    }

    public void Dispose()
    {
        foreach (var kv in _iconCache)
        {
            try
            {
                kv.Value.Dispose();
            }
            catch
            {
                // ignore
            }
        }

        _iconCache.Clear();
    }
}
