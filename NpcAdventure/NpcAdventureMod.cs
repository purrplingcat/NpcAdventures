﻿using StardewModdingAPI;
using StardewModdingAPI.Events;
using NpcAdventure.Loader;
using NpcAdventure.Driver;
using NpcAdventure.Events;
using NpcAdventure.Model;
using NpcAdventure.HUD;
using NpcAdventure.Compatibility;
using NpcAdventure.Story;
using NpcAdventure.Story.Scenario;
using NpcAdventure.Internal.Patching;

namespace NpcAdventure
{
    /// <summary>The mod entry point.</summary>
    public class NpcAdventureMod : Mod
    {
        private bool firstTick = true;

        private DialogueDriver DialogueDriver { get; set; }
        private HintDriver HintDriver { get; set; }
        private StuffDriver StuffDriver { get; set; }
        private MailDriver MailDriver { get; set; }
        internal ISpecialModEvents SpecialEvents { get; private set; }
        internal CompanionManager CompanionManager { get; private set; }
        internal CompanionDisplay CompanionHud { get; private set; }
        internal ContentLoader ContentLoader { get; private set; }
        internal GamePatcher Patcher { get; private set; }
        internal GameMaster GameMaster { get; private set; }
        internal Config Config { get; private set; } = new Config();

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            if (Constants.TargetPlatform == GamePlatform.Android)
            {
                this.Monitor.Log("Android support is an experimental feature, may cause some problems. Before you report a bug please content me on my discord https://discord.gg/wnEDqKF Thank you.", LogLevel.Alert);
            }

            this.Config = helper.ReadConfig<Config>();
            this.ContentLoader = new ContentLoader(this.Helper.Content, this.Helper.ContentPacks, this.ModManifest.UniqueID, "assets", this.Monitor);
            this.Patcher = new GamePatcher(this.ModManifest.UniqueID, this.Monitor, this.Config.EnableDebug);
            this.RegisterEvents(helper.Events);
            Commander.Register(this);
        }

        private void RegisterEvents(IModEvents events)
        {
            events.GameLoop.SaveLoaded += this.GameLoop_SaveLoaded;
            events.GameLoop.Saving += this.GameLoop_Saving;
            events.Specialized.LoadStageChanged += this.Specialized_LoadStageChanged;
            events.GameLoop.ReturnedToTitle += this.GameLoop_ReturnedToTitle;
            events.GameLoop.DayEnding += this.GameLoop_DayEnding;
            events.GameLoop.DayStarted += this.GameLoop_DayStarted;
            events.GameLoop.GameLaunched += this.GameLoop_GameLaunched;
            events.GameLoop.UpdateTicked += this.GameLoop_UpdateTicked;
            events.Display.RenderingHud += this.Display_RenderingHud;
            events.Player.Warped += this.Player_Warped;
        }

        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            if (!this.Config.Experimental.UseCheckForEventsPatch && Context.IsWorldReady && this.GameMaster.Mode != GameMasterMode.OFFLINE)
            {
                // Check for NPC Adventures events in the old way by player warped event. This way will be removed in 0.16.0
                this.GameMaster.CheckForEvents(e.NewLocation, e.Player);
            }
        }

        private void GameLoop_Saving(object sender, SavingEventArgs e)
        {
            this.GameMaster.SaveData();
        }

        private void Display_RenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (Context.IsWorldReady && this.CompanionHud != null)
                this.CompanionHud.Draw(e.SpriteBatch);
        }

        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (this.firstTick)
            {
                // Check if methods patched by NA are patched by other mods
                this.Patcher.CheckPatches();
                this.firstTick = false;
            }

            if (Context.IsWorldReady && this.CompanionHud != null)
                this.CompanionHud.Update(e);
        }

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Setup third party mod compatibility bridge
            TPMC.Setup(this.Helper.ModRegistry, this.Monitor);

            // Mod's services and drivers
            this.SpecialEvents = new SpecialModEvents();
            this.DialogueDriver = new DialogueDriver(this.Helper.Events);
            this.HintDriver = new HintDriver(this.Helper.Events);
            this.StuffDriver = new StuffDriver(this.Helper.Data, this.Monitor);
            this.MailDriver = new MailDriver(this.ContentLoader, this.Monitor);
            this.GameMaster = new GameMaster(this.Helper, new StoryHelper(this.ContentLoader), this.Monitor);
            this.CompanionHud = new CompanionDisplay(this.Config, this.ContentLoader);
            this.CompanionManager = new CompanionManager(this.GameMaster, this.DialogueDriver, this.HintDriver, this.CompanionHud, this.Config, this.Monitor);
            
            this.StuffDriver.RegisterEvents(this.Helper.Events);
            this.MailDriver.RegisterEvents(this.SpecialEvents);

            this.ApplyPatches(); // Apply harmony patches
            this.InitializeScenarios();
        }

        private void ApplyPatches()
        {
            // Core patches (important)
            this.Patcher.Apply(
                new Patches.MailBoxPatch((SpecialModEvents)this.SpecialEvents),
                new Patches.QuestPatch((SpecialModEvents)this.SpecialEvents),
                new Patches.SpouseReturnHomePatch(this.CompanionManager),
                new Patches.GetCharacterPatch(this.CompanionManager),
                new Patches.NpcCheckActionPatch(this.CompanionManager, this.Helper.Input, this.Config),
                new Patches.GameLocationDrawPatch((SpecialModEvents)this.SpecialEvents)
            );

            if (this.Config.AvoidSayHiToMonsters)
            {
                // Optional patch: Avoid say hi to monsters while companioning (this patch enabled by default)
                this.Patcher.Apply(new Patches.CompanionSayHiPatch(this.CompanionManager));
            }

            if (this.Config.Experimental.FightThruCompanion)
            {
                // Optional experimental patch: Avoid annoying dialogue shown while use sword over companion (patch disabled by default)
                this.Patcher.Apply(new Patches.GameUseToolPatch(this.CompanionManager));
                this.LogExperimental("FightOverCompanion");
            }

            if (this.Config.Experimental.UseCheckForEventsPatch)
            {
                this.Patcher.Apply(new Patches.CheckEventPatch(this.GameMaster));
                this.LogExperimental("NewEventChecking");
            }
        }

        private void LogExperimental(string featureName)
        {
            this.Monitor.Log($"You are enabled experimental feature '{featureName}' in mod's config.json.", LogLevel.Warn);
            this.Monitor.Log("   This feature may affect game stability, you can disable it in config.json", LogLevel.Warn);
        }

        private void Specialized_LoadStageChanged(object sender, LoadStageChangedEventArgs e)
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

            var dispositions = this.ContentLoader.LoadStrings("Data/CompanionDispositions");

            this.ContentLoader.LoadStrings("Data/AnimationDescriptions");
            this.ContentLoader.LoadStrings("Data/IdleBehaviors");
            this.ContentLoader.LoadStrings("Data/IdleNPCDefinitions");
            this.ContentLoader.LoadStrings("Strings/Strings");
            this.ContentLoader.LoadStrings("Strings/SpeechBubbles");

            // Preload dialogues for companions
            foreach (string npcName in dispositions.Keys)
            {
                this.ContentLoader.LoadStrings($"Dialogue/{npcName}");
            }

            this.Monitor.Log("Assets preloaded!", LogLevel.Info);
        }

        private void InitializeScenarios()
        {
            if (!this.Config.AdventureMode)
                return; // Don't init gamem aster scenarios when adventure mode is disabled

            this.GameMaster.RegisterScenario(new AdventureBegins(this.SpecialEvents, this.Helper.Events, this.ContentLoader, this.Config, this.Monitor));
            this.GameMaster.RegisterScenario(new QuestScenario(this.SpecialEvents, this.ContentLoader, this.Monitor));
        }

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            if (Context.IsMultiplayer)
                return;
            this.CompanionManager.NewDaySetup();
        }

        private void GameLoop_DayEnding(object sender, DayEndingEventArgs e)
        {
            if (Context.IsMultiplayer)
                return;

            this.CompanionManager.ResetStateMachines();
            this.CompanionManager.DumpCompanionNonEmptyBags();
        }

        private void GameLoop_ReturnedToTitle(object sender, StardewModdingAPI.Events.ReturnedToTitleEventArgs e)
        {
            if (Context.IsMultiplayer)
                return;

            this.GameMaster.Uninitialize();
            this.CompanionManager.UninitializeCompanions();
            this.ContentLoader.InvalidateCache();
        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (Context.IsMultiplayer)
            {
                this.Monitor.Log("Companions not initalized, because multiplayer currently unsupported by NPC Adventures.", LogLevel.Warn);
                return;
            }

            if (this.Config.AdventureMode)
                this.GameMaster.Initialize();
            else
                this.Monitor.Log("Started in non-adventure mode", LogLevel.Info);

            this.CompanionManager.InitializeCompanions(this.ContentLoader, this.Helper.Events, this.SpecialEvents, this.Helper.Reflection);
            this.Patcher.CheckPatches();
        }
    }
}