using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Textures.TextureWraps;

using Lumina.Excel.Sheets;

using TheCrystariumBoutique;
using TheCrystariumBoutique.Data;

using ImGui = Dalamud.Bindings.ImGui.ImGui;
using ImGuiWindowFlags = Dalamud.Bindings.ImGui.ImGuiWindowFlags;
using ImGuiTableFlags = Dalamud.Bindings.ImGui.ImGuiTableFlags;
using FontAwesomeIcon = Dalamud.Interface.FontAwesomeIcon;

namespace TheCrystariumBoutique.UI;

public sealed class MainWindow : Window, IDisposable
{
    private readonly Configuration _config;
    private readonly ItemRepository _items;
    private readonly TryOnService _tryOn;

    private readonly List<GearSlot> _slotOrder = new()
    {
        GearSlot.Head, GearSlot.Body, GearSlot.Hands, GearSlot.Legs, GearSlot.Feet,
        GearSlot.MainHand, GearSlot.OffHand,
        GearSlot.Ears, GearSlot.Neck, GearSlot.Wrist, GearSlot.RingLeft, GearSlot.RingRight,
    };

    private int _currentTabIndex;
    private string _search = string.Empty;
    private bool _raceExclusiveOnly;
    private int _page;
    private const int ItemsPerPage = 18;

    private ArmorTypeFilter _armorFilter = ArmorTypeFilter.All;
    private bool _weaponJobFilter = true;

    private readonly Dictionary<GearSlot, ushort> _selectedStain = new();
    private readonly Dictionary<GearSlot, uint> _selectedItemBySlot = new();

    private string _saveName = string.Empty;
    private string _shareText = string.Empty;

    public MainWindow(Configuration config, ItemRepository items, TryOnService tryOn)
        : base(
            "The Crystarium Boutique##Main",
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(850, 600),
            MaximumSize = new Vector2(4096, 4096),
        };

        _config = config;
        _items = items;
        _tryOn = tryOn;

        foreach (var slot in Enum.GetValues(typeof(GearSlot)).Cast<GearSlot>())
            _selectedStain[slot] = 0;
    }

    public override void Draw()
    {
        DrawHeader();
        ImGui.Separator();

        if (!ImGui.BeginTabBar("BoutiqueTabs"))
            return;

        foreach (var (slot, idx) in _slotOrder.Select((s, i) => (s, i)))
        {
            if (!ImGui.BeginTabItem(slot.ToString()))
                continue;

            _currentTabIndex = idx;
            DrawSlotTab(slot);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Sets"))
        {
            DrawSetsTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Race Exclusive"))
        {
            _raceExclusiveOnly = true;
            var slot = _slotOrder[Math.Clamp(_currentTabIndex, 0, _slotOrder.Count - 1)];
            DrawSlotTab(slot);
            _raceExclusiveOnly = false;
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawHeader()
    {
        ImGui.PushItemWidth(220 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##Search", "Search items...", ref _search, 64);
        ImGui.PopItemWidth();
        ImGui.SameLine();

        ImGui.PushItemWidth(180 * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginCombo("##armorfilter", _armorFilter.ToString()))
        {
            foreach (ArmorTypeFilter v in Enum.GetValues(typeof(ArmorTypeFilter)))
            {
                var selected = v == _armorFilter;
                if (ImGui.Selectable(v.ToString(), selected))
                {
                    _armorFilter = v;
                    _page = 0;
                }

                if (selected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }
        ImGui.PopItemWidth();
        ImGui.SameLine();

        if (ImGuiComponents.IconButton(
                _weaponJobFilter ? FontAwesomeIcon.UserCheck : FontAwesomeIcon.User))
        {
            _weaponJobFilter = !_weaponJobFilter;
            _page = 0;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(
                _weaponJobFilter
                    ? "Job filter: ON (weapons only)"
                    : "Job filter: OFF");
            ImGui.EndTooltip();
        }

        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Sync))
        {
            _search = string.Empty;
            _page = 0;
            _armorFilter = ArmorTypeFilter.All;
            _weaponJobFilter = true;
        }

        ImGui.SameLine();
        ImGui.TextColored(
            ImGuiColors.DalamudGrey,
            "Filters: Armor family (armor only) • Job filter (weapons) • Search");
    }

    private void DrawSlotTab(GearSlot slot)
    {
        var filtered = _items.GetFilteredForSlot(
            slot,
            _search,
            _raceExclusiveOnly,
            applyJobFilterForWeapons: _weaponJobFilter,
            armorTypeFilter: _armorFilter);

        var items = filtered.ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(items.Count / (float)ItemsPerPage));
        _page = Math.Clamp(_page, 0, totalPages - 1);

        // Top pager
        DrawPager(totalPages);

        var start = _page * ItemsPerPage;
        var end = Math.Min(start + ItemsPerPage, items.Count);
        var slice = items.Skip(start).Take(end - start).ToList();

        const int columns = 6;
        var iconSize = 48f * ImGuiHelpers.GlobalScale;

        if (ImGui.BeginTable("Grid", columns,
                ImGuiTableFlags.PadOuterX | ImGuiTableFlags.SizingStretchProp))
        {
            for (var i = 0; i < slice.Count; i++)
            {
                if (i % columns == 0)
                    ImGui.TableNextRow();

                ImGui.TableNextColumn();

                var it = slice[i];
                var cursor = ImGui.GetCursorPos();

                // Icon
                var iconWrap = _items.GetIcon(it.Icon);
                if (iconWrap != null)
                {
                    // Try to pull an ImGui texture ID in a version-agnostic way.
                    var handleProp =
                        iconWrap.GetType().GetProperty("ImGuiHandle") ??
                        iconWrap.GetType().GetProperty("ImGuiTextureId") ??
                        iconWrap.GetType().GetProperty("Id") ??
                        iconWrap.GetType().GetProperty("Handle");

                    if (handleProp != null && handleProp.GetValue(iconWrap) is nint texId)
                    {
                        ImGui.Image(texId, new Vector2(iconSize, iconSize));
                    }
                    else
                    {
                        // Fallback if we couldn't find a usable handle.
                        ImGui.Button($"##icon_{it.RowId}", new Vector2(iconSize, iconSize));
                    }
                }
                else
                {
                    ImGui.Button($"##icon_{it.RowId}", new Vector2(iconSize, iconSize));
                }

                // Click = preview
                if (ImGui.IsItemClicked())
                {
                    _selectedItemBySlot[slot] = it.RowId;
                    RePreview(slot, it);
                }

                // Tooltip
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted(it.Name.ToString());
                    ImGui.EndTooltip();
                }

                // Name under icon
                ImGui.SetCursorPos(new Vector2(cursor.X, cursor.Y + iconSize + 2));
                ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + iconSize);
                ImGui.TextUnformatted(Truncate(it.Name.ToString(), 18));
                ImGui.PopTextWrapPos();
            }

            ImGui.EndTable();
        }

        // Dye picker
        DrawDyePicker(slot);

        // Selected summary
        if (_selectedItemBySlot.TryGetValue(slot, out var selectedId))
        {
            var selectedItem = items.FirstOrDefault(i => i.RowId == selectedId);
            if (selectedItem.RowId != 0)
            {
                ImGui.Separator();
                ImGui.TextColored(
                    ImGuiColors.HealerGreen,
                    $"Selected: {selectedItem.Name} (ID {selectedItem.RowId})");
            }
        }

        // Bottom pager
        DrawPager(totalPages);

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        // Outfit pane
        ImGui.BeginChild("OutfitPane", new Vector2(260 * ImGuiHelpers.GlobalScale, 0), true);
        DrawOutfitPane();
        ImGui.EndChild();
    }

    private void RePreview(GearSlot slot, Item item)
    {
        var stain = _selectedStain.TryGetValue(slot, out var s) ? s : (ushort)0;
        _tryOn.Preview(item, stain);
    }

    private void DrawPager(int totalPages)
    {
        if (ImGuiComponents.IconButton(FontAwesomeIcon.ChevronLeft) && _page > 0)
            _page--;

        ImGui.SameLine();
        ImGui.Text($"Page {_page + 1} / {totalPages}");
        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.ChevronRight)
            && _page < totalPages - 1)
            _page++;
    }

    private void DrawDyePicker(GearSlot slot)
    {
        ImGui.Separator();
        ImGui.Text("Dye:");
        ImGui.SameLine();

        var stains = _items.Stains;
        var idx = 0;
        var current = _selectedStain.TryGetValue(slot, out var s) ? s : (ushort)0;

        if (current != 0)
        {
            var pos = stains.ToList().FindIndex(x => x.RowId == current);
            if (pos >= 0)
                idx = pos + 1;
        }

        var names = new List<string> { "None" };
        names.AddRange(stains.Select(st => $"{st.RowId}: {st.Name}"));

        var comboIndex = idx;

        ImGui.PushItemWidth(220 * ImGuiHelpers.GlobalScale);
        if (ImGui.Combo("##stain", ref comboIndex, names.ToArray(), names.Count))
        {
            if (comboIndex <= 0)
            {
                _selectedStain[slot] = 0;
            }
            else
            {
                _selectedStain[slot] = (ushort)stains.ElementAt(comboIndex - 1).RowId;
            }

            if (_selectedItemBySlot.TryGetValue(slot, out var itemId))
            {
                var item = _items.ItemsBySlot.TryGetValue(slot, out var list)
                    ? list.FirstOrDefault(i => i.RowId == itemId)
                    : default;

                if (item.RowId != 0)
                    RePreview(slot, item);
            }
        }
        ImGui.PopItemWidth();
    }

    private void DrawOutfitPane()
    {
        if (ImGui.CollapsingHeader("Current Preview"))
        {
            foreach (var slot in _slotOrder)
            {
                if (_selectedItemBySlot.TryGetValue(slot, out var itemId))
                {
                    var stain = _selectedStain.TryGetValue(slot, out var s) ? s : (ushort)0;
                    ImGui.BulletText($"{slot}: Item {itemId}, Dye {stain}");
                }
            }

            if (ImGui.Button("Clear Slot Selections"))
                _selectedItemBySlot.Clear();
        }

        ImGui.Separator();

        if (!ImGui.CollapsingHeader("Save / Load Outfits", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.InputTextWithHint("##savename", "Outfit name...", ref _saveName, 64);

        if (ImGui.Button("Save"))
        {
            var outfit = new Outfit();

            foreach (var kv in _selectedItemBySlot)
            {
                outfit.Pieces[kv.Key] = new OutfitPiece
                {
                    ItemId = kv.Value,
                    StainId = _selectedStain.TryGetValue(kv.Key, out var st)
                        ? st
                        : (ushort)0
                };
            }

            if (!string.IsNullOrWhiteSpace(_saveName))
            {
                _config.Outfits[_saveName] = outfit;
                _config.Save();
            }
        }

        ImGui.SameLine();

        if (ImGui.Button("Delete")
            && !string.IsNullOrWhiteSpace(_saveName)
            && _config.Outfits.Remove(_saveName))
        {
            _config.Save();
        }

        ImGui.Separator();
        ImGui.Text("Saved:");

        foreach (var kv in _config.Outfits.ToList())
        {
            if (!ImGui.Selectable(kv.Key))
                continue;

            _selectedItemBySlot.Clear();
            foreach (var p in kv.Value.Pieces)
            {
                _selectedItemBySlot[p.Key] = p.Value.ItemId;
                _selectedStain[p.Key] = p.Value.StainId;
            }

            _saveName = kv.Key;
        }

        ImGui.Separator();

        if (ImGui.Button("Share (Copy JSON)")
            && !string.IsNullOrWhiteSpace(_saveName)
            && _config.Outfits.TryGetValue(_saveName, out var shareOutfit))
        {
            _shareText = System.Text.Json.JsonSerializer.Serialize(shareOutfit);
            ImGui.SetClipboardText(_shareText);
            Svc.Chat.Print("[Boutique] Outfit JSON copied to clipboard.");
        }

        ImGui.InputTextMultiline(
            "##share",
            ref _shareText,
            4096,
            new Vector2(240, 120));

        ImGui.SameLine();
        ImGui.BeginGroup();

        if (ImGui.Button("Load From JSON"))
        {
            try
            {
                var loaded = System.Text.Json.JsonSerializer.Deserialize<Outfit>(_shareText);
                if (loaded != null)
                {
                    _selectedItemBySlot.Clear();

                    foreach (var p in loaded.Pieces)
                    {
                        _selectedItemBySlot[p.Key] = p.Value.ItemId;
                        _selectedStain[p.Key] = p.Value.StainId;
                    }

                    Svc.Chat.Print("[Boutique] Outfit loaded from JSON.");
                }
            }
            catch (Exception ex)
            {
                Svc.Chat.PrintError($"[Boutique] Failed to load outfit JSON: {ex.Message}");
            }
        }

        ImGui.EndGroup();
    }

    private void DrawSetsTab()
    {
        ImGui.TextColored(
            ImGuiColors.DalamudGrey,
            "Sets view is planned. It will group curated item sets with quick preview.");
        ImGui.BulletText("Design: left list of sets, right preview + slot breakdown.");
        ImGui.BulletText("Optional filters: role, expansion, event.");
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";

    public void Dispose()
    {
    }
}
