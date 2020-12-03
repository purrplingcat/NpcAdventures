using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NpcAdventure.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace NpcAdventure.Compatibility
{
    internal class MobilePhoneAppProvider
    {
        private const string APP_ID = "PurrplingCat.NpcAdventuresStats";
        private IMonitor monitor;
        private GameMaster gameMaster;
        private IModEvents events;
        private IInputHelper input;
        private readonly IMobilePhoneApi api;

        public MobilePhoneAppProvider(IModRegistry registry, IMonitor monitor)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            this.monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            this.api = registry.GetApi<IMobilePhoneApi>(ModUids.MOBILE_PHONEMOD_UID);
        }

        public void Setup(GameMaster gameMaster, IModEvents events, IInputHelper input, Texture2D icon)
        {
            if (this.api == null)
                return;

            this.gameMaster = gameMaster;
            this.events = events;
            this.input = input;
            this.api.AddApp(APP_ID, "NPC Adventures stats", this.OpenApp, icon);
        }

        private void DrawApp(object sender, RenderedWorldEventArgs e)
        {
            if (this.api.IsCallingNPC())
                return;

            Rectangle screen = this.api.GetScreenRectangle();

            e.SpriteBatch.DrawString(Game1.smallFont, $"NPC Adventures stats", new Vector2(screen.X + 8, screen.Y + 8), Color.Black, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 1f);
            e.SpriteBatch.DrawString(Game1.smallFont, $"Eligible to recruit: {(this.gameMaster.Data.GetPlayerState().isEligible ? "Yes" : "No")}", new Vector2(screen.X + 8, screen.Y + 48), Color.Black, 0, Vector2.Zero, 0.86f, SpriteEffects.None, 1f);
            e.SpriteBatch.DrawString(Game1.smallFont, $"Recruited: {(this.gameMaster.Data.GetPlayerState().recruited.Count)}", new Vector2(screen.X + 8, screen.Y + 74), Color.Black, 0, Vector2.Zero, 0.86f, SpriteEffects.None, 1f);
        }

        private void CloseApp()
        {
            this.api.SetAppRunning(false);
            this.api.SetRunningApp(null);
            this.events.Display.RenderedWorld -= this.DrawApp;
            this.events.GameLoop.UpdateTicked -= this.UpdateApp;
            this.monitor.Log("Mobile phone NA stats app closed");
        }

        private void OpenApp()
        {
            this.events.Display.RenderedWorld += this.DrawApp;
            this.events.GameLoop.UpdateTicked += this.UpdateApp;
            this.api.SetAppRunning(true);
            this.api.SetRunningApp(APP_ID);
            this.monitor.Log("Opening mobile phone NA stats app");
        }

        private void UpdateApp(object sender, UpdateTickedEventArgs e)
        {
            if (!this.api.GetPhoneOpened() || !this.api.GetAppRunning() || this.api.GetRunningApp() != APP_ID)
            {
                this.monitor.Log($"Closing app: phone opened {this.api.GetPhoneOpened()} app running {this.api.GetAppRunning()} running app {this.api.GetRunningApp()}");
                this.CloseApp();
                return;
            }

            if (this.input.IsDown(SButton.MouseLeft) && this.api.GetScreenRectangle().Contains((int)this.input.GetCursorPosition().ScreenPixels.X, (int)this.input.GetCursorPosition().ScreenPixels.Y))
            {
                this.CloseApp();
            }
        }
    }

    public interface IMobilePhoneApi
    {
        bool AddApp(string id, string name, Action action, Texture2D icon);
        Vector2 GetScreenPosition();
        Vector2 GetScreenSize();
        Vector2 GetScreenSize(bool rotated);
        Rectangle GetPhoneRectangle();
        Rectangle GetScreenRectangle();
        bool GetPhoneRotated();
        void SetPhoneRotated(bool value);
        bool GetPhoneOpened();
        void SetPhoneOpened(bool value);
        bool GetAppRunning();
        void SetAppRunning(bool value);
        string GetRunningApp();
        void SetRunningApp(string value);
        void PlayRingTone();
        void PlayNotificationTone();
        NPC GetCallingNPC();
        bool IsCallingNPC();
    }
}