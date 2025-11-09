// If you ever want real Try On integration, define ENABLE_TRYON in the .csproj
// and fill in the signature logic in the guarded section.
#if ENABLE_TRYON
using System;
using System.Runtime.InteropServices;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace TheCrystariumBoutique;

internal unsafe sealed class TryOnInterop : IDisposable
{
    // Example skeleton; signatures need to be updated per game patch.

    [Signature("48 89 5C 24 ?? 57 48 83 EC 20 48 8B F9 E8 ?? ?? ?? ?? 33 FF", ScanType = ScanType.StaticAddress)]
    private readonly nint openTryOnPtr = nint.Zero;

    [Signature("E8 ?? ?? ?? ?? 41 88 7E 18 48 8B CB E8 ?? ?? ?? ?? 48 8D 4D", ScanType = ScanType.Text)]
    private readonly nint previewItemPtr = nint.Zero;

    private delegate byte OpenTryOnDelegate(AgentTryon* self);
    private delegate byte PreviewItemDelegate(AgentTryon* self, uint itemId, ushort stainId);

    private readonly OpenTryOnDelegate? openTryOn;
    private readonly PreviewItemDelegate? previewItem;

    public TryOnInterop()
    {
        Svc.PluginInterface.Create<TryOnInterop>(); // initializes [Signature]s

        if (openTryOnPtr != nint.Zero)
            openTryOn = Marshal.GetDelegateForFunctionPointer<OpenTryOnDelegate>(openTryOnPtr);

        if (previewItemPtr != nint.Zero)
            previewItem = Marshal.GetDelegateForFunctionPointer<PreviewItemDelegate>(previewItemPtr);
    }

    private static AgentTryon* GetAgent()
        => (AgentTryon*)AgentModule.Instance()->GetAgentByInternalId(AgentId.Tryon);

    public void EnsureOpen()
    {
        var agent = GetAgent();
        if (agent == null || openTryOn == null)
            return;

        openTryOn(agent);
    }

    public void Preview(uint itemId, ushort stainId)
    {
        var agent = GetAgent();
        if (agent == null || previewItem == null)
            return;

        EnsureOpen();
        previewItem(agent, itemId, stainId);
    }

    public void Dispose()
    {
    }
}
#else

// Stub version used when ENABLE_TRYON is not defined.
// This keeps your project compiling and lets TryOnService fall back cleanly.
namespace TheCrystariumBoutique;

internal sealed class TryOnInterop : System.IDisposable
{
    public void EnsureOpen()
    {
    }

    public void Preview(uint itemId, ushort stainId)
    {
    }

    public void Dispose()
    {
    }
}
#endif
