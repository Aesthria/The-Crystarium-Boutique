using System.Collections.Generic;
using Lumina.Excel.Sheets;
using TheCrystariumBoutique.Data;

namespace TheCrystariumBoutique;

public sealed class TryOnService
{
#if ENABLE_TRYON
    private readonly TryOnInterop _interop = new();
#endif

    public void Preview(Item item, ushort stainId)
    {
#if ENABLE_TRYON
        _interop.Preview(item.RowId, stainId);
#else
        var dye = stainId == 0 ? "(no dye)" : $"dye #{stainId}";
        Svc.Chat.Print($"[Boutique] Try On (stub): {item.Name} {dye}");
#endif
    }

    public void ApplyOutfit(Dictionary<GearSlot, OutfitPiece> pieces)
    {
#if ENABLE_TRYON
        foreach (var kv in pieces)
            _interop.Preview(kv.Value.ItemId, kv.Value.StainId);
#else
        Svc.Chat.Print("[Boutique] Apply Outfit (stub): Try On integration not enabled.");
#endif
    }
}
