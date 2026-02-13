# LetMeFight (Bannerlord Mod)

This repo is set up so you can edit in Codex and build in Visual Studio 2022.

## Build In Visual Studio 2022

1. Open `LetMeFight.sln`.
2. Select `Debug|x64` to build directly into your Bannerlord module folder.
3. Build the solution.

By default, the project assumes Bannerlord is installed at:

`C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord`

## Custom Bannerlord Path

If your game is installed elsewhere, set one of these before building:

- MSBuild property: `BannerlordDir`
- Environment variable: `BANNERLORD_DIR`

Example:

```powershell
$env:BANNERLORD_DIR = "D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord"
```

## Output Behavior

- `Debug`: deploys `LetMeFight.dll` to `Modules\LetMeFight\bin\Win64_Shipping_Client`
- `Debug`: also copies `SubModule.xml` to `Modules\LetMeFight\SubModule.xml`
- `Release`: outputs to `BuyTroops\bin\<Platform>\Release`
