using StardewModdingAPI;
using StardewModdingAPI.Events;
using NpcAdventure.Loader;
using NpcAdventure.Driver;
using StardewValley;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace NpcAdventure
{
    /// <summary>The mod entry point.</summary>
    public class NpcAdventureMod : Mod
    {
        private CompanionManager companionManager;
        private ContentLoader contentLoader;
        private DialogueDriver DialogueDriver { get; set; }
        private HintDriver HintDriver { get; set; }
        private StuffDriver StuffDriver { get; set; }

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += this.GameLoop_SaveLoaded;
            helper.Events.Specialised.LoadStageChanged += this.Specialised_LoadStageChanged;
            helper.Events.GameLoop.ReturnedToTitle += this.GameLoop_ReturnedToTitle;
            helper.Events.GameLoop.DayEnding += this.GameLoop_DayEnding;
            helper.Events.GameLoop.DayStarted += this.GameLoop_DayStarted;

            this.DialogueDriver = new DialogueDriver(helper.Events);
            this.HintDriver = new HintDriver(helper.Events);
            this.StuffDriver = new StuffDriver(helper.Events, helper.Data, this.Monitor);
            this.contentLoader = new ContentLoader(helper.Content, this.ModManifest.UniqueID, "assets", helper.DirectoryPath, this.Monitor);
            this.companionManager = new CompanionManager(this.DialogueDriver, this.HintDriver, this.Monitor);
        }

        private void Specialised_LoadStageChanged(object sender, LoadStageChangedEventArgs e)
        {
            if (e.NewStage == StardewModdingAPI.Enums.LoadStage.Loaded)
            {
                this.PreloadAssets();
            }
        }

        private void PreloadAssets()
        {
            /* Preload assets to cache */
            this.Monitor.Log("Preloading assets...", LogLevel.Info);

            var dispositions = this.contentLoader.LoadStrings("Data/CompanionDispositions");

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
            this.contentLoader.InvalidateCache();
        }

        private void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            this.companionManager.InitializeCompanions(this.contentLoader, this.Helper.Events, this.Helper.Reflection);
        }
    }
}