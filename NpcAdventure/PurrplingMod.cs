using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using PurrplingMod.Loader;
using PurrplingMod.Driver;
using System.Collections.Generic;
using StardewValley.Objects;
using StardewValley;
using System.Xml;
using System.IO;
using Microsoft.Xna.Framework;
using PurrplingMod.Model;

namespace PurrplingMod
{
    /// <summary>The mod entry point.</summary>
    public class PurrplingMod : Mod
    {
        private CompanionManager companionManager;
        private ContentLoader contentLoader;
        private DialogueDriver DialogueDriver { get; set; }
        private HintDriver HintDriver { get; set; }
        private StuffDriver StuffDriver { get; set; }
        internal static PurrplingMod Mod { get; private set; }

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += this.GameLoop_SaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += this.GameLoop_ReturnedToTitle;
            helper.Events.GameLoop.DayEnding += this.GameLoop_DayEnding;
            helper.Events.GameLoop.DayStarted += this.GameLoop_DayStarted;
            helper.Events.GameLoop.GameLaunched += this.GameLoop_GameLaunched;

            this.DialogueDriver = new DialogueDriver(helper.Events);
            this.HintDriver = new HintDriver(helper.Events);
            this.StuffDriver = new StuffDriver(helper.Events, helper.Data, this.Monitor);
            this.contentLoader = new ContentLoader(helper.Content, helper.DirectoryPath, "assets", this.Monitor);
            this.companionManager = new CompanionManager(this.DialogueDriver, this.HintDriver, this.Monitor);

            PurrplingMod.Mod = this;
        }

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            /* Preload assets to cache */
            this.Monitor.Log("Preloading assets...", LogLevel.Info);

            string[] dispositions = this.contentLoader.Load<string[]>("CompanionDispositions");

            this.contentLoader.LoadStrings("Strings/Strings");
            this.contentLoader.LoadStrings("Strings/SpeechBubbles");

            // Preload dialogues for companions
            foreach (string npcName in dispositions)
            {
                this.contentLoader.LoadStrings($"Dialogue/{npcName}");
            }

            this.Monitor.Log("Assets preloaded!", LogLevel.Info);
        }

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            this.StuffDriver.RevivePossibleBags();
            this.companionManager.NewDaySetup();
        }

        private void GameLoop_DayEnding(object sender, DayEndingEventArgs e)
        {
            this.companionManager.ResetStateMachines();
            this.companionManager.DumpCompanionNonEmptyBags();
            this.StuffDriver.DetectAndPrepareBagsToSave();
        }

        private void GameLoop_ReturnedToTitle(object sender, StardewModdingAPI.Events.ReturnedToTitleEventArgs e)
        {
            this.companionManager.UninitializeCompanions();
        }

        private void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            this.companionManager.InitializeCompanions(this.contentLoader, this.Helper.Events);
        }
    }
}