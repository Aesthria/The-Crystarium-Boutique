#if ENABLE_TRYON
using System;
using System.Runtime.InteropServices;
using Dalamud.Utility.Signatures;
using Lumina.Excel.GeneratedSheets;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace TheCrystariumBoutique;

internal sealed unsafe class TryOnInterop : IDisposable
{
    // 1) Acquire AgentTryon
    private static AgentTryon* GetAgentTryon()
    {
        var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.Tryon);
        return (AgentTryon*)agent;
    }

    // 2) Signatures (updated as client patches change)
    // Try-on: open/bring-to-front
    [Signature("40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 84 C0 75 10", ScanType = ScanType.StaticAddress)]
    private readonly nint _openTryOnAddr = nint.Zero;

    // Try-on: preview an item id + stain; auto-detects slot.
    // This pattern targets a call used by the item preview pipeline.
    [Signature("E8 ?? ?? ?? ?? 41 88 7E 18 48 8B CB E8 ?? ?? ?? ?? 48 8D 4D", ScanType = ScanType.Text)]
    private readonly nint _previewItemAddr = nint.Zero;

    // Delegates
    private delegate byte OpenTryOnDelegate(AgentTryon* self);
    private OpenTryOnDelegate? _openTryOn;

    // Prototype: byte TryOnItem(AgentTryon* self, uint itemId, ushort stainId)
    // Some patches accept (AgentTryon*, uint itemId, ushort stainId, int unkSlot=-1).
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate byte PreviewItemDelegate(AgentTryon* self, uint itemId, ushort stainId);
    private PreviewItemDelegate? _previewItem;

    internal TryOnInterop()
    {
        try
        {
            Svc.PluginInterface.Create(this); // fills [Signature] fields
            if (_openTryOnAddr != nint.Zero)
                _openTryOn = Marshal.GetDelegateForFunctionPointer<OpenTryOnDelegate>(_openTryOnAddr);

            if (_previewItemAddr != nint.Zero)
                _previewItem = Marshal.GetDelegateForFunctionPointer<PreviewItemDelegate>(_previewItemAddr);

            if (_openTryOn == null || _previewItem == null)
                Svc.Chat.PrintError("[Boutique] TryOn signatures not fully resolved; fallback will be used.");
        }
        catch (Exception ex)
        {
            Svc.Chat.PrintError($"[Boutique] TryOn interop init failed: {ex.Message}");
        }
    }

    public void EnsureOpen()
    {
        var agent = GetAgentTryon();
        if (agent == null) return;

        if (_openTryOn != null)
        {
            try
            {
                _openTryOn(agent);
                return;
            }
            catch (Exception ex)
            {
                Svc.Chat.PrintError($"[Boutique] TryOn open failed: {ex.Message}");
            }
        }

        // Fallback: silently ignore; window opens implicitly on first preview in many patches.
    }

    public void Preview(uint itemId, ushort stainId)
    {
        var agent = GetAgentTryon();
        if (agent == null) return;

        EnsureOpen();

        if (_previewItem != null)
        {
            try
            {
                _previewItem(agent, itemId, stainId);
                return;
            }
            catch (Exception ex)
            {
                Svc.Chat.PrintError($"[Boutique] TryOn preview failed: {ex.Message}");
            }
        }

        // As a fallback, log intent so behavior remains predictable
        Svc.Chat.Print($"[Boutique] Try On (fallback): item {itemId}, dye {stainId}");
    }

    public void Dispose() { }
}
#endif
