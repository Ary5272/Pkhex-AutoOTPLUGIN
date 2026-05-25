# OT/HT Ownership — PKHeX Plugin

A one-click PKHeX plugin that fixes **trainer ownership** across all your boxes, with every change **legality-checked**.

- Pokémon owned by **you** → cleared to **pure-OT** (no Handling Trainer, you are the handler) — *if it stays legal*.
- Everything else (events / foreign OT) → **Handling Trainer set to you** (max friendship), so you still "own" them legally.
- Anything that would become **illegal** is left exactly as it was.

It also undoes the side effect where importing your `.pk9` files into a blank "PKHeX" save stamps `HT = PKHeX` onto everything — one click puts it right.

## Install

1. Download `OTHandlerPlugin.dll` from the [Releases](../../releases) page.
2. Drop it into the **`plugins`** folder next to `PKHeX.exe` (create the folder if it isn't there).
3. Restart PKHeX.
4. It appears under **Tools → OT/HT Ownership**.

That's the entire install — it's a single self-contained DLL.

## Usage

1. Load your collection into the boxes.
2. **Tools → OT/HT Ownership → Settings…**
   - **Auto-detect owner from loaded boxes** (default): uses the most common `OT name + ID` in the boxes — works for whoever's dex is loaded.
   - Or untick it and set **OT name / TID / SID / Gender / Language** manually.
   - Optional: **Dump boxes to a folder after applying** + pick a folder — writes every box mon to disk as a PKHeX-named `.pk9` in the same click.
3. **Tools → OT/HT Ownership → Apply to boxes.**
4. A summary popup shows the detected owner and the counts (pure-OT / HT-set / kept).

> Without the dump option, changes live in the loaded save like any PKHeX edit — save/export (or right-click box → Dump Box) to persist.

## Compatibility

Built against **PKHeX 26.05.05** (build `20260505`, .NET 10). Other PKHeX versions may require a rebuild against their `PKHeX.Core` (the `IPlugin` interface changes between versions).

## Build from source

Requires the **.NET 10 SDK**.

```
src/
  OTHandlerPlugin.csproj
  Plugin.cs
  Settings.cs
  SettingsForm.cs
refs/
  PKHeX.Core.dll   <-- provide your own (not committed)
```

1. Put your PKHeX's `PKHeX.Core.dll` in `refs/`. (PKHeX is a single-file build; it extracts its DLLs to `%TEMP%\.net\PKHeX\<hash>\` while running — copy `PKHeX.Core.dll` from there.)
2. `dotnet build -c Release`
3. Copy `bin/Release/OTHandlerPlugin.dll` into your PKHeX `plugins` folder.

## How "yours" is decided

A mon is "yours" when its **OT name and full ID (TID/SID)** match the configured/auto-detected owner. Matching mons get HT cleared; everything else gets HT set to the owner. Both paths are validated with PKHeX's own `LegalityAnalysis` before being kept.

## Notes

- Settings persist to `OTHandlerPlugin.json` next to the DLL.
- SV (`.pk9`) entities only; other formats in the boxes are left untouched.
- Not affiliated with the PKHeX project. Use on copies/backups.

## License

MIT — see [LICENSE](LICENSE).
