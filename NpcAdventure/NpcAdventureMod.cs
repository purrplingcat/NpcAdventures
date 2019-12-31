using StardewModdingAPI;
using StardewModdingAPI.Events;
using NpcAdventure.Loader;
using NpcAdventure.Driver;
using Harmony;
using NpcAdventure.Events;
using NpcAdventure.Model;
using NpcAdventure.HUD;
using NpcAdventure.Compatibility;
using NpcAdventure.Story;
using NpcAdventure.Story.Scenario;

namespace NpcAdventure
{
    /// <summary>The mod entry point.</summary>
    public class NpcAdventureMod : Mod
    {
        private CompanionManager companionManager;
        private Commander commander;
        private CompanionDisplay companionHud;
        public static IMonitor GameMonitor { get; private set; }
        private ContentLoader contentLoader;
        private GameMaster gameMaster;
        private Config config;
        private DialogueDriver DialogueDriver { get; set; }
        private HintDriver HintDriver { get; set; }
        private StuffDriver StuffDriver { get; set; }
        public MailDriver MailDriver { get; private set; }
        private ISpecialModEvents SpecialEvents { get; set; }

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.RegisterEvents(helper.Events);
            this.config = helper.ReadConfig<Config>();
            this.contentLoader = new ContentLoader(this.Helper.Content, this.Helper.ContentPacks, this.ModManifest.UniqueID, "assets", this.Monitor);
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
        }

        private void GameLoop_Saving(object sender, SavingEventArgs e)
        {
            this.gameMaster.SaveData();
        }

        private void Display_RenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (Context.IsWorldReady && this.companionHud != null)
                this.companionHud.Draw(e.SpriteBatch);
        }

        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Context.IsWorldReady && this.companionHud != null)
                this.companionHud.Update(e);
        }

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Setup third party mod compatibility bridge
            TPMC.Setup(this.Helper.ModRegistry);

            // Mod's services and drivers
            this.SpecialEvents = new SpecialModEvents();
            this.DialogueDriver = new DialogueDriver(this.Helper.Events);
            this.HintDriver = new HintDriver(this.Helper.Events);
            this.StuffDriver = new StuffDriver(this.Helper.Data, this.Monitor);
            this.MailDriver = new MailDriver(this.contentLoader, this.Monitor);
            this.gameMaster = new GameMaster(this.Helper, new StoryHelper(this.contentLoader), this.Monitor);
            this.companionHud = new CompanionDisplay(this.config, this.contentLoader);
            this.companionManager = new CompanionManager(this.gameMaster, this.DialogueDriver, this.HintDriver, this.companionHud, this.config, this.Monitor);
            this.commander = new Commander(this.gameMaster, this.companionManager, this.Monitor);
            this.StuffDriver.RegisterEvents(this.Helper.Events);
            this.MailDriver.RegisterEvents(this.SpecialEvents);
            
            // Harmony
            HarmonyInstance harmony = HarmonyInstance.Create("Purrplingcat.NpcAdventure");

            Patches.MailBoxPatch.Setup(harmony, (SpecialModEvents)this.SpecialEvents);
            Patches.QuestPatch.Setup(harmony, (SpecialModEvents)this.SpecialEvents);
            Patches.SpouseReturnHomePatch.Setup(harmony);
            Patches.CompanionSayHiPatch.Setup(harmony, this.companionManager);
            Patches.GameLocationDrawPatch.Setup(harmony, this.SpecialEvents);

            if (this.config.EnableDebug)
                this.commander.SetupCommands(this.Helper.ConsoleCommands);

            this.InitializeScenarios();
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

        private void InitializeScenarios()
        {
            if (!this.config.AdventureMode)
                return; // Don't init gamem aster scenarios when adventure mode is disabled

            this.gameMaster.RegisterScenario(new AdventureBegins(this.SpecialEvents, this.Helper.Events, this.contentLoader, this.Monitor));
            this.gameMaster.RegisterScenario(new QuestScenario(this.SpecialEvents, this.contentLoader, this.Monitor));
        }

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            if (Context.IsMultiplayer)
                return;
            this.companionManager.NewDaySetup();
        }

        private void GameLoop_DayEnding(object sender, DayEndingEventArgs e)
        {
            if (Context.IsMultiplayer)
                return;

            this.companionManager.ResetStateMachines();
            this.companionManager.DumpCompanionNonEmptyBags();
        }

        private void GameLoop_ReturnedToTitle(object sender, StardewModdingAPI.Events.ReturnedToTitleEventArgs e)
        {
            if (Context.IsMultiplayer)
                return;

            this.gameMaster.Uninitialize();
            this.companionManager.UninitializeCompanions();
            this.contentLoader.InvalidateCache();

            // Clean data in patches
            Patches.SpouseReturnHomePatch.recruitedSpouses.Clear();
        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (Context.IsMultiplayer)
            {
                this.Monitor.Log("Companions not initalized, because multiplayer currently unsupported by NPC Adventures.", LogLevel.Warn);
                return;
            }

            if (this.config.AdventureMode)
                this.gameMaster.Initialize();
            else
                this.Monitor.Log("Started in non-adventure mode", LogLevel.Info);

            this.companionManager.InitializeCompanions(this.contentLoader, this.Helper.Events, this.SpecialEvents, this.Helper.Reflection);
        }
    }
}