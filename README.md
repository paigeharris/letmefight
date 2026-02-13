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
- `Debug`: also creates release zips in:
  - `releases\LetMeFight v<module-version>.zip`
  - `releases\<module-version>\LetMeFight v<module-version>.zip`
- `Release`: outputs to `BuyTroops\bin\<Platform>\Release`

## Intended Gameplay Behavior

This is the core intended flow for this mod and should be preserved when changing code:

1. Start a normal fight encounter.
2. Retreat troops as usual.
3. Player gets wounded after dropping below 20 HP.
4. Player retreats from that encounter.
5. Next encounter against the same party should still work as a normal fight:
   - player should be back at 20 HP,
   - player should be able to fight,
   - surviving party members should still be present/accounted for.

Notes:
- Do not force mission-agent state changes during mission initialization/spawn unless explicitly needed.
- Prefer campaign-state recovery hooks around encounter transition/end events.

## Test Status

- `Plan A` (post-encounter recovery with HP floor `25`) was tested on `2026-02-12` and did **not** reliably fix the "Cannot issue order while dead" / encounter carry-over issue.
- `Plan B` v1 (`CampaignEvents.PlayerDesertedBattleEvent`) did not fire in the reported retreat path.
- Current `Plan B` v2: detect retreat processing via `PlayerEncounter.LeaveEncounter` / `BattleSimulation.IsPlayerRetreated` on `CampaignEvents.TickEvent` and recover immediately when that signal appears.
