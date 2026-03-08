# Let Me Fight

`Let Me Fight` adds a rescue option to Bannerlord when your character is in bad shape.

## What The Mod Does

- Adds `Santas Rescue Crew (Let Me Fight Option B)` to the `encounter` and `menu_siege_strategies` menus.
- Only shows the option when the main hero is wounded or below `21` HP.
- Sets the player back to `21` HP.
- Adds `12` `Vlandian Knights` to the main party.
- Shows a short Santa rescue flavor message in the message log.
- Sends the player to their home settlement through Bannerlord's settlement encounter flow.

## Patch Notes

### v1.3.15.1

- Updated for Bannerlord `v1.3.15` with this branch's `.1` version suffix.
- Hardened the rescue travel flow so it stops using raw settlement/menu jumps that could crash.
- Cleans up the current encounter state before sending the player home.
- Falls back to other usable hero settlements if `HomeSettlement` is missing or invalid.
- Wraps the rescue action in error handling so a mod exception fails safely instead of crashing the game.
- Removed dead project references so `dotnet build` now runs clean with `0 warnings`.
