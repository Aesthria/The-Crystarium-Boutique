using System.Numerics;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Catalog;
using CrystariumBoutique.Core.Sessions;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using LuminaUIColor = Lumina.Excel.Sheets.UIColor;

namespace CrystariumBoutique.Ui;

public sealed class ItemRarityColorResolver
{
    private readonly IDataManager dataManager;
    private readonly Dictionary<byte, Vector4> colors = [];

    public ItemRarityColorResolver(IDataManager dataManager)
        => this.dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));

    public Vector4 GetColor(EquipmentItem item)
    {
        if (colors.TryGetValue(item.Rarity, out var cached))
        {
            return cached;
        }

        try
        {
            var colorRowId = ItemUtil.GetItemRarityColorType(item.ItemId, isEdgeColor: false);
            var packedColor = dataManager.GetExcelSheet<LuminaUIColor>().GetRow(colorRowId).Dark;
            var resolved = new Vector4(
                ((packedColor >> 24) & 0xFF) / 255f,
                ((packedColor >> 16) & 0xFF) / 255f,
                ((packedColor >> 8) & 0xFF) / 255f,
                (packedColor & 0xFF) / 255f);
            colors[item.Rarity] = resolved;
            return resolved;
        }
        catch
        {
            var fallback = GetFallbackColor(item.Rarity);
            colors[item.Rarity] = fallback;
            return fallback;
        }
    }

    public Vector4 GetColor(
        PreviewEquipmentState equipment,
        EquipmentSlot slot,
        IItemRepository itemRepository)
        => equipment.Appearance.SourceItemId is { } itemId
            && itemRepository.TryGetItem(itemId, slot, out var item)
                ? GetColor(item)
                : GetFallbackColor(1);

    private static Vector4 GetFallbackColor(byte rarity)
        => rarity switch
        {
            2 => new Vector4(0.45f, 0.78f, 0.38f, 1f),
            3 => new Vector4(0.42f, 0.64f, 0.94f, 1f),
            4 => new Vector4(0.72f, 0.48f, 0.90f, 1f),
            5 => new Vector4(0.94f, 0.73f, 0.28f, 1f),
            6 => new Vector4(0.95f, 0.52f, 0.25f, 1f),
            7 => new Vector4(0.95f, 0.47f, 0.70f, 1f),
            _ => new Vector4(0.91f, 0.89f, 0.84f, 1f),
        };
}
