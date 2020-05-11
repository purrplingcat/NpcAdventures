# Upgrade your content pack

## Update to version 0.12.0 beta

### Bag delivery content source changed

Text source for bag delivery letters is now in `Strings/Mail` under key `bagItemsSentLetter.<NPC_name>`. As a content pack author you must define string for bag delivery letter for each custom NPC which you have in your content pack. See [How to define bag delivery letters](bag-letters.md)

**NOTE:** If you don't define custom bag letters for your custom NPC companions, delivered package with bag items will be without any message letter for farmer. If farmer try to open it, the on't see a letter and open the box immediatelly instead of show the letter.

## Update to version 0.10.0 beta

### Content Pack format version changed

Version of `content.json` content pack definition file was changed from `1.0` to `1.1`. Older version is not longer supported and content packs with this version format will no longer loaded into game! Please update your content packs to new CP format version and follow update notes bellow. **It's important to do** if you want your content pack compatible with beta!

### Location dialogues engine remaked

Location dialogues was redone. Now known key `companion_<location>*` is not pushed only when you enter a location, now this kind of dialogue can be pushed in game time tick. If you want still you dialogue will be pushed when farmer and companion enter location, use key pattern `companionEnter_<location>*`. Dialogues are not repeated when dialogue was shown and you player enter this location again. If you want repeat your dialogue every location enter, use key pattern `companionRepeatEnter_<location>*`

Location dialogues are shown once one per day for companion which you are recruited.

NOTE: `*` means variation key patterns

### Spouse dialogues

Asset `Dialogue/<companion>Spouse` is no longer exists! For spouse dialogue lines definition use asset `Dialogue/<companion>` and suffix your spouse dialogue line with `_Spouse`.

```yaml
"companionAccepted": "Do you want to go for an adventure with me?$u#$b# Well @, what are you waiting for? Let's go!$h", # normal line
"companionAccepted_Spouse": "Adventure? Oh, of course I will @!$l#$b#I hope we can delve into the mines today.$h", # spouse line
"companion_Farm_Night_rainy: "Blah blah blah"
"companion_Farm_Night_rainy_Spouse: "Spouses's blah blah blah"
```

### Randomized dialogues

For randomized dialogues now use `~` instead of `$`, dollar no longer exists!

```js
{
  "companion_Mine": "Be blessed my sword!",
  "companion_Mine$1": "I love adventure in mines!",
  "companion_Mine$2": "Taking me to mines, {0}?"
}

// Change it to:
{
  "companion_Mine": "Be blessed my sword!",
  "companion_Mine~1": "I love adventure in mines!",
  "companion_Mine~2": "Taking me to mines, {0}?"
}
```

### Location speech bubbles

Location speech bubbles in `Strings/SpeechBubbles` are now prefixed with `ambient_`

```js
// This old definitions
{
  "Mine_Abigail": "Be blessed my sword!",
  "Mine_Abigail$1": "I love adventure in mines!",
  "Mine_Abigail$2": "Taking me to mines, {0}?",
  "Mine_Abigail$3": "I < it!",
}

// Change to:
{
  "ambient_Mine_Abigail": "Be blessed my sword!",
  "ambient_Mine_Abigail~1": "I love adventure in mines!",
  "ambient_Mine_Abigail~2": "Taking me to mines, {0}?",
  "ambient_Mine_Abigail~3": "I < it!",
}
```

## See also

- [How to create a content pack](modding/content-packs.md)
