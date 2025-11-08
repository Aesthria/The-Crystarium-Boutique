using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace TheCrystariumBoutique;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // Saved outfits by name
    public Dictionary<string, Outfit> Outfits { get; set; } = new();

    // UI prefs
    public bool OpenOnLogin { get; set; } = false;

    [NonSerialized]
    private IDalamudPluginInterface? _pi;

    public void Initialize(IDalamudPluginInterface pi) => _pi = pi;

    public void Save() => _pi?.SavePluginConfig(this);
}

[Serializable]
public class Outfit
{
    public Dictionary<GearSlot, OutfitPiece> Pieces { get; set; } = new();
}

[Serializable]
public class OutfitPiece
{
    public uint ItemId { get; set; }
    public ushort StainId { get; set; } // 0 = none
}

public enum GearSlot
{
    Head,
    Body,
    Hands,
    Legs,
    Feet,
    MainHand,
    OffHand,
    Back,   // For future cape mods/slots if needed
    Ears,
    Neck,
    Wrist,
    RingLeft,
    RingRight
}
