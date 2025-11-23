# libs/ Directory

This directory will contain OWML DLLs needed for compilation.

**These files are NOT included in source control** - you need to copy them from your OWML installation.

## Setup

Copy these files from your OWML installation to this directory:

- `OWML.Common.dll`
- `OWML.ModHelper.dll`
- `OWML.ModHelper.Menus.dll`

**OWML Location:**
- Steam: `C:/Program Files (x86)/Steam/steamapps/common/Outer Wilds/OuterWilds_Data/Managed/`
- Epic: `C:/Program Files/Epic Games/OuterWilds/OuterWilds_Data/Managed/`

## Automated Setup (Recommended)

If you have Pixi installed, just run:

```bash
pixi run setup-libs
```

This automatically copies the DLLs from your OWML installation.

## What About Game and Unity DLLs?

Game and Unity assemblies are automatically downloaded via the `OuterWildsGameLibs` NuGet package when you run `dotnet restore`. You don't need to copy them manually.
