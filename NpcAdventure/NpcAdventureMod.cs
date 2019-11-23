using StardewModdingAPI;
using StardewModdingAPI.Events;
using NpcAdventure.Loader;
using NpcAdventure.Driver;
using StardewValley;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Harmony;
using System.Reflection;
using NpcAdventure.Events;

namespace NpcAdventure
{
    /// <summary>The mod entry point.</summary>
    public class NpcAdventureMod : Mod
    {
        private CompanionManager companionManager;

        public static IMonitor GameMonitor { get; private set; }

        private ContentLoader contentLoader;
        private DialogueDriver DialogueDriver { get; set; }
        private HintDriver HintDriver { get; set; }
        private StuffDriver StuffDriver { get; set; }
        private ISpecialModEvents SpecialEvents { get; set; }

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += this.GameLoop_SaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += this.GameLoop_ReturnedToTitle;
            helper.Events.GameLoop.DayEnding += this.GameLoop_DayEnding;
            helper.Events.GameLoop.DayStarted += this.GameLoop_DayStarted;
            helper.Events.GameLoop.GameLaunched += this.GameLoop_GameLaunched;

            this.SpecialEvents = new SpecialModEvents();
            this.DialogueDriver = new DialogueDriver(helper.Events);
            this.HintDriver = new HintDriver(helper.Events);
            this.StuffDriver = new StuffDriver(helper.Events, helper.Data, this.Monitor);
            this.contentLoader = new ContentLoader(helper.Content, helper.DirectoryPath, "assets", this.Monitor);
            this.companionManager = new CompanionManager(this.DialogueDriver, this.HintDriver, this.Monitor);

            //Harmony
            HarmonyInstance harmony = HarmonyInstance.Create("Purrplingcat.NpcAdventure");
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), "draw"),
                postfix: new HarmonyMethod(typeof(Patches.GameLocationDrawPatch), nameof(Patches.GameLocationDrawPatch.Postfix))
            );

            Patches.GameLocationDrawPatch.Setup(this.SpecialEvents);

            NpcAdventureMod.GameMonitor = this.Monitor;
        }

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            /* Preload assets to cache */
            this.Monitor.Log("Preloading assets...", LogLevel.Info);

            var dispositions = this.contentLoader.LoadStrings("CompanionDispositions");

            this.contentLoader.LoadStrings("Data/AnimationDescriptions");
            this.contentLoader.LoadStrings("Data/IdleBehaviors");
            this.contentLoader.LoadStrings("Data/IdleNPCDefinitions");
            this.contentLoader.LoadStrings("Strings/Strings");
            this.contentLoader.LoadStrings("Strings/SpeechBubbles");

            // Preload dialogues for companions
            foreach (string npcName in dispositions.Keys)
            {
                this.contentLoader.LoadStrings($"Dialogue/{npcName}");
            }

            this.Monitor.Log("Assets preloaded!", LogLevel.Info);
        }

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            this.StuffDriver.ReviveDeliveredBags();
            this.companionManager.NewDaySetup();
        }

        private void GameLoop_DayEnding(object sender, DayEndingEventArgs e)
        {
            this.companionManager.ResetStateMachines();
            this.companionManager.DumpCompanionNonEmptyBags();
        }

        private void GameLoop_ReturnedToTitle(object sender, StardewModdingAPI.Events.ReturnedToTitleEventArgs e)
        {
            this.companionManager.UninitializeCompanions();
        }

        private void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            this.companionManager.InitializeCompanions(this.contentLoader, this.Helper.Events, this.SpecialEvents, this.Helper.Reflection);
        }
    }
}