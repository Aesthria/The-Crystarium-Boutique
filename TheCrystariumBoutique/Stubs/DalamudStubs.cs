// Minimal stubs for building outside Dalamud dev environment.
// These types are intentionally incomplete and only satisfy compile-time references.

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Dalamud.IoC
{
 [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
 public sealed class PluginServiceAttribute : Attribute { }
}

namespace Dalamud.Configuration
{
 public interface IPluginConfiguration { int Version { get; set; } }
}

namespace Dalamud.Plugin
{
 public interface IDalamudPlugin { }

 public interface IDalamudPluginInterface
 {
 object? GetPluginConfig();
 T? GetPluginConfig<T>();
 void SavePluginConfig(object config);
 Dalamud.Interface.UiBuilder UiBuilder { get; }
 void Create<T>();
 }
}

namespace Dalamud.Game
{
 public sealed class SigScanner { }
}

namespace Dalamud.Game.Command
{
 public class CommandInfo
 {
 public CommandInfo(Action<string, string> handler) { }
 public string HelpMessage { get; set; } = string.Empty;
 }

 public interface ICommandManager
 {
 void AddHandler(string command, CommandInfo info);
 void RemoveHandler(string command);
 }
}

namespace Dalamud.Game.Gui
{
 public interface IChatGui
 {
 void Print(string message);
 void PrintError(string message);
 }
}

namespace Dalamud.Game.ClientState
{
 public interface IClientState { }
}

namespace Dalamud.Data
{
 public interface IDataManager { }
}

namespace Dalamud.Interface.Textures
{
 public interface IDalamudTextureWrap : IDisposable
 {
 IntPtr ImGuiHandle { get; }
 }

 public interface ITextureProvider
 {
 IDalamudTextureWrap? GetFromGameIcon(uint iconId);
 }
}

namespace Dalamud.Interface
{
 public sealed class UiBuilder
 {
 public event Action? Draw;
 public event Action? OpenConfigUi;
 public void RaiseDraw() => Draw?.Invoke();
 public void RaiseOpenConfigUi() => OpenConfigUi?.Invoke();
 }
}

namespace Dalamud.Interface.Colors
{
 public static class ImGuiColors
 {
 public static Vector4 DalamudGrey => new(0.6f,0.6f,0.6f,1f);
 public static Vector4 HealerGreen => new(0.2f,0.9f,0.5f,1f);
 }
}

namespace Dalamud.Interface.Components
{
 public static class ImGuiComponents
 {
 public static bool IconButton(FontAwesomeIcon icon) => false;
 }

 public enum FontAwesomeIcon
 {
 User,
 UserCheck,
 Sync,
 ChevronLeft,
 ChevronRight,
 }
}

namespace Dalamud.Interface.Utility
{
 public static class ImGuiHelpers
 {
 public static float GlobalScale =>1f;
 }
}

namespace Dalamud.Interface.Windowing
{
 public class Window
 {
 public class WindowSizeConstraints
 {
 public Vector2 MinimumSize { get; set; }
 public Vector2 MaximumSize { get; set; }
 }

 public string WindowName { get; }
 public bool IsOpen { get; set; }
 public WindowSizeConstraints SizeConstraints { get; set; } = new();

 public Window(string name, ImGuiNET.ImGuiWindowFlags flags = ImGuiNET.ImGuiWindowFlags.None)
 {
 WindowName = name;
 }

 public virtual void Draw() { }
 }

 public sealed class WindowSystem
 {
 public WindowSystem(string name) { }
 public void AddWindow(Window window) { }
 public void Draw() { }
 }
}

namespace ImGuiNET
{
 [Flags]
 public enum ImGuiWindowFlags { None =0 }

 [Flags]
 public enum ImGuiTableFlags { None =0, PadOuterX =1, SizingStretchProp =2 }

 public static class ImGui
 {
 public static void Separator() { }
 public static void PushItemWidth(float width) { }
 public static void PopItemWidth() { }
 public static void SameLine() { }
 public static bool BeginTabBar(string id) => true;
 public static void EndTabBar() { }
 public static bool BeginTabItem(string label) => true;
 public static void EndTabItem() { }
 public static bool BeginCombo(string label, string previewValue) => false;
 public static void EndCombo() { }
 public static bool Selectable(string label, bool selected = false) => false;
 public static void SetItemDefaultFocus() { }
 public static void Text(string text) { }
 public static void TextUnformatted(string text) { }
 public static bool Button(string label, Vector2 size) => false;
 public static Vector2 GetCursorPos() => Vector2.Zero;
 public static void SetCursorPos(Vector2 pos) { }
 public static void Image(IntPtr textureId, Vector2 size) { }
 public static bool IsItemClicked() => false;
 public static bool IsItemHovered() => false;
 public static void BeginTooltip() { }
 public static void EndTooltip() { }
 public static void PushTextWrapPos(float wrapPosX) { }
 public static void PopTextWrapPos() { }
 public static bool Combo(string label, ref int currentItem, string[] items, int itemsCount) => false;
 public static void BeginChild(string strId, Vector2 size, bool border) { }
 public static void EndChild() { }
 public static bool CollapsingHeader(string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None) => false;
 public static void BulletText(string text) { }
 public static void BeginGroup() { }
 public static void EndGroup() { }
 public static void InputTextWithHint(string label, string hint, ref string input, uint maxLength) { }
 public static void InputTextMultiline(string label, ref string input, int maxLength, Vector2 size) { }
 public static void SetClipboardText(string text) { }
 public static bool BeginTable(string id, int columns, ImGuiTableFlags flags) => false;
 public static void EndTable() { }
 public static void TableNextRow() { }
 public static void TableNextColumn() { }
 }

 [Flags]
 public enum ImGuiTreeNodeFlags { None =0, DefaultOpen =1 }
}

namespace Lumina.Excel
{
 public class ExcelSheet<T> : List<T> { }
}

namespace Lumina.Excel.GeneratedSheets
{
 public class Item
 {
 public uint RowId { get; set; }
 public string Name { get; set; } = string.Empty;
 public uint Icon { get; set; }
 }

 public class Stain
 {
 public uint RowId { get; set; }
 public string Name { get; set; } = string.Empty;
 }

 public class ClassJob { }
 public class ClassJobCategory { }
}
