namespace CrystariumBoutique.Ui;

internal static class HelpGuideContent
{
    public static IReadOnlyList<string> WelcomePoints { get; } =
    [
        "Commands: /boutique, /tcb, and /cb toggle The Crystarium Boutique.",
        "Left Click an item to preview it immediately. Use Revert in the bottom-right to return to the previous item.",
        "Right Click an item to manage Favorites. Named Favorite lists persist between sessions.",
        "Head items: Left Click previews with the optional visor piece hidden. Ctrl + Left Click shows the full or alternate visor appearance when supported.",
        "Crystal Wardrobe displays synchronized equipment and Dye Slot 1 / Dye Slot 2. Right Click an equipped item card to remove it from the current Boutique appearance. Click a dye slot to open Dye Studio; its button hides or reopens the Wardrobe.",
        "Crystal Wardrobe supports Vertical and Horizontal layouts; changing orientation swaps the current window dimensions.",
        "Automatically Sync Wardrobe updates it after FFXIV finishes applying a gearset. Reset Character manually resynchronizes current FFXIV equipment and dyes.",
        "Dye selections carry forward while trying compatible items. Clear All Dye Slots removes Boutique dyes while keeping the equipment.",
        "Save Design stores the current appearance with a custom name and optional notes. The Designs tab supports management, Import Design, Export Design, and item-reference lists.",
        "Settings opens the Settings tab for themes, automatic Wardrobe sync, Favorite highlighting, and other interface options.",
    ];

    public static IReadOnlyList<string> BoutiquePoints { get; } =
    [
        "Use /boutique, /tcb, or /cb to toggle The Crystarium Boutique.",
        "Use First, Previous, Next, and Last below the item grid, or hover the grid and move the mouse wheel forward/back to change pages.",
        "Left Click an item to preview it in the corresponding Crystal Wardrobe equipment slot.",
        "For Head items, Left Click hides the optional visor piece. Ctrl + Left Click shows the full or alternate visor appearance when supported.",
        "Right Click any Boutique or Favorites item to manage it in All Favorites and named lists.",
        "A blue glow follows the hovered item. A red frame identifies an item restricted by the active character's race, gender, or Grand Company.",
    ];

    public static IReadOnlyList<string> WardrobePoints { get; } =
    [
        "Crystal Wardrobe displays the equipment and Dye Slot 1 / Dye Slot 2 currently synchronized with the Boutique session. Click a dye slot to open Dye Studio.",
        "Right Click an equipped item card to remove that item from the current Boutique appearance.",
        "Vertical and Horizontal layouts are available. Changing orientation automatically swaps the current Wardrobe window dimensions.",
        "When Automatically Sync Wardrobe is enabled, the Wardrobe updates after FFXIV finishes applying a local-player gearset change.",
        "Reset Character manually resynchronizes the Boutique and Crystal Wardrobe with the character's current FFXIV equipment and dyes.",
        "Dye Slot 1 and Dye Slot 2 selections persist while trying other compatible items in the same slot, including dyes loaded from synchronized game gear.",
        "Clear All Dye Slots removes active Boutique dyes from supported equipment slots while keeping the equipment itself.",
    ];

    public static IReadOnlyList<string> FavoritePoints { get; } =
    [
        "Right Click an item to add, remove, or organize it in Favorites.",
        "Favorites can belong to named lists and persist between plugin sessions.",
        "Favorited Boutique items show a badge; Settings can also enable and customize their frame highlight.",
    ];

    public static IReadOnlyList<string> DesignPoints { get; } =
    [
        "Choose Save Design on the Boutique tab to store the current Boutique/Wardrobe appearance with a custom name and optional notes.",
        "Use the Designs tab to load, rename, duplicate, overwrite, delete, or generate a movable item reference for a saved Design.",
        "Hover a saved Design Item List icon to see its tooltip. These cards are visual references and do not apply equipment.",
        "Export Design copies a shareable Design string from a saved Design. Import Design accepts another user's compatible string and saves it to your local Designs list so you can load, try, and modify it.",
    ];

    public static IReadOnlyList<string> EorzeaPoints { get; } =
    [
        "Find the glamour you want on Eorzea Collection and copy its public glamour URL.",
        "Open the Designs dropdown on the Boutique tab and choose Eorzea Collection Import from URL.",
        "Paste the URL and choose Import and Apply. Use Save Design afterward to keep the compatible appearance locally, then modify it with the normal Boutique tools.",
        "If the glamour omits an armor or accessory slot, the Boutique uses the matching Emperor's New item so older equipment does not blend into it.",
    ];

    public static IReadOnlyList<string> ThemePoints { get; } =
    [
        "Settings opens directly to the Settings tab. Current themes are Simple, The Crystarium Boutique, and Simple Crystarium Boutique; fresh configurations default to The Crystarium Boutique.",
        "Settings also controls automatic Wardrobe synchronization, Favorite frame highlighting, tooltip presentation, interface scale, transparency, and combat safety.",
        "Item tooltips show locally indexed acquisition sources and other items sharing the same appearance. Exact boss information appears only for a verified exact-item relationship.",
        "Marketboard labels report local marketability only; the Boutique never contacts a live price service and acquisition lookup remains fully offline.",
        "Hidden tooltips appear only while Shift is held. Theme changes affect presentation only and do not alter Designs, equipment, dyes, or restoration.",
    ];
}
