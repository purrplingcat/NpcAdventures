# Sprites

## Sprite definition file

You can define sprite sources in `Data/Sprites`

```js
{
  "<SpriteName>": "<SourceFilePath>"
}
```

**IMPORTANT:** Sprite source files can't be patched by `Edit` patch in mod's content packs and can't be localized directly by NPC Adventures! If you want to patch sprite files, load sprites from the game folder and use **Content Patcher**

### Sprite source files

Every sprite which is definet in the sprite definition file must have specified a path to the sprite file. This path can target to:

- Mod's assets folder (use path prefix `~/`). Suffix must be specified (like `.png`)
- Mod's content pack folder (use path prefix `@<contentpackUID>/`). Suffix must be specified (like `.png`)
- Game folder (use relative path without any prefix (like `Characters/Abigail_Swimsuit`)). Without file suffix.

### Use sprites in content packs

If you want define custom sprites, you must register them in `Data/Sprites`. As a file path you can specify from which main folder your sprite will be loaded. You can specify:

- `@yourcp.uid/path/to/sprite.png` for lookup sprite in your content pack folder.
- `Path/In/Game/Folder/Sprite` for lookup sprite in the game folder.

If you want to lookup your sprite in game folder, you must to load your file in your custom way. NPC Adventures can't place your sprite to game folder. Use **Content Patcher** to do it.

## Type of sprites

### Swimsuits

For define swimsuit sprite source for any companion define `<NPC_Name>_Swimsuit` as key in the definition file.

```js
// Data/Sprites
{
  "Abigail_Swimsuit": "~/Sprites/Abigail_swimsuit.png", // Lookup `Sprites/Abigail_swimsuit.png` in the mod folder
  "Maru_Swimsuit": "Characters/Maru_swimsuit", // Lookup `Characters/Maru_swimsuit.xnb` in the game folder. Can be patched by Content Patcher
  "Shane_Swimsuit": "@purrplingcat.customshanecompanion/assets/Shane_swimsuit.png" // Lookup `assets/Shane_swimsuit.png` in the 'purrplingcat.customshanecompanion' content pack folder
}
```
