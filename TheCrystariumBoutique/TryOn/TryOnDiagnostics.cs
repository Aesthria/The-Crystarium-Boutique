using System;

namespace TheCrystariumBoutique;

internal static class TryOnDiagnostics
{
    public static void PrintStatus()
    {
#if ENABLE_TRYON
        try
        {
            var enabled = "ENABLED";
            Svc.Chat.Print($"[Boutique] TryOn status: {enabled}");
        }
        catch (Exception ex)
        {
            Svc.Chat.PrintError($"[Boutique] TryOn status check failed: {ex.Message}");
        }
#else
        Svc.Chat.Print("[Boutique] TryOn status: DISABLED (define ENABLE_TRYON to enable)");
#endif
    }
}
