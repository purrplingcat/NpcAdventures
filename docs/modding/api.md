# API

**NOTE:** This is an experimental feature. It may be changed or removed in future.  

NPC Adventures provides a [SMAPI mod API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Integrations#Mod-provided_APIs). You can make use of the API as follows.

## Create an interface for the API

Create the following interface in your project:

```cs
public interface INpcAdventureModApi
{
    bool CanRecruit();
    string[] GetPossibleCompanions();
    bool IsPossibleCompanion(string npc);
    bool CanRecruit(NPC npc);
    bool IsRecruited(NPC npc);
    bool IsAvailable(NPC npc);
    int GetNpcState(NPC npc);
    bool RecruitCompanion(Farmer who, NPC npc, GameLocation location, bool skipDialogue = false);
}
```

## Get the API 

Any time after the `GameLaunched` event, create an instance of the API interface:

```cs
public static void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
{
    if (Helper.ModRegistry.IsLoaded("purrplingcat.npcadventure"))
    {
        INpcAdventureModApi api = Helper.ModRegistry.GetApi<INpcAdventureModApi>("purrplingcat.npcadventure");
        if (api != null)
        {
            DoSomethingWithThe(api);
        }
    }
}
```

### API Methods

The following methods are implemented in the API

| Method                    | Arg Types                                 | Return type              | Description                                                                                              |
| ------------------------- | ----------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| `CanRecruit`              | none                                      | `bool`                   | Returns `true` if player is eligible to recruit followers.               |
| `GetPossibleCompanions`   | none                                      | `string[]`               | A string array of possible companions.                       |
| `IsPossibleCompanion`     | `string`                                  | `bool`                   | Returns `true` if the named NPC is a possible companion.     |
| `CanRecruit`              | `NPC`                                     | `bool`                   | Returns `true` if the given NPC can be recruited.            |
| `IsRecruited`             | `NPC`                                     | `bool`                   | Returns `true` if the given NPC is currently recruited by the player.   |
| `IsAvailable`             | `NPC`                                     | `bool`                   | Returns `true` if the given NPC is currently available to be recruited by the player.   |
| `GetNpcState`             | `NPC`                                     | `int`                    | Returns an integer value corresponding to the recruitment state for the NPC  (RESET = 0, AVAILABLE = 1, RECRUITED = 2, UNAVAILABLE = 3) or -1 if there is an error   |
| `RecruitCompanion`        | `Farmer`, `NPC`, `GameLocation, `bool`    | `bool`                   | if the `skipDialogue` argument is set to true, it tries to recruit the NPC directly. Otherwise it shows the recruitment dialogue. Returns whether the recruitment was successful.   |

## Future

Remember, this feature is **experimental**. In future the API may be changed, replaced or removed from the mod. 


