# Dalamud API 15 Verification

Verified 2026-08-25 from both the installed runtime and official documentation.

- Installed Dalamud version: `15.0.3.4`.
- Installed development assembly: API 15-era runtime targeting .NET 10.
- Official v15 baseline: API level 15, .NET 10, released for FFXIV patch 7.5.
- Current build SDK: `Dalamud.NET.Sdk/15.0.0`.
- Current UI namespace: `Dalamud.Bindings.ImGui` with Dalamud Windowing API.
- Supported lifecycle interfaces: `IDalamudPlugin` and `IAsyncDalamudPlugin`.
- Verified APIs used by v0.1: constructor-injected services, `ICommandManager`, `IDalamudPluginInterface.UiBuilder`, `WindowSystem`, `IPluginLog`, and `IPluginConfiguration` persistence.

Primary sources:

- https://dalamud.dev/versions/v15/
- https://dalamud.dev/versions/
- https://dalamud.dev/plugin-development/project-layout/
- https://dalamud.dev/plugin-development/technical-considerations/
- https://www.nuget.org/packages/Dalamud.NET.Sdk/
