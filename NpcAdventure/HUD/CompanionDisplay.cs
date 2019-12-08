using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NpcAdventure.Loader;
using NpcAdventure.Model;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace NpcAdventure.HUD
{
    class CompanionDisplay : Internal.IDrawable, Internal.IUpdateable
    {
        public List<CompanionSkill> Skills { get; }
        public Config Config { get; }
        private ClickableTextureComponent avatar;
        private string hoverText;
        private readonly IContentLoader contentLoader;

        public CompanionDisplay(Config config, IContentLoader contentLoader)
        {
            this.Skills = new List<CompanionSkill>();
            this.Config = config;
            this.contentLoader = contentLoader;
        }

        public void AddSkill(string type, string description)
        {
            this.Skills.Add(new CompanionSkill(type, description));
        }

        public void AssignCompanion(NPC companion)
        {
            string hoverText = this.contentLoader.LoadString("Strings/Strings:recruitedCompanionHint", companion.displayName);
            this.avatar = new ClickableTextureComponent("", Rectangle.Empty, null, hoverText, companion.Sprite.Texture, companion.getMugShotSourceRect(), 4f, false);
        }

        public void Reset()
        {
            this.Skills.Clear();
            this.avatar = null;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!this.Config.ShowHUD || Game1.eventUp)
                return;

            if (this.Skills.Count > 0)
            {
                this.DrawSkills(spriteBatch);
            }

            if (this.avatar != null)
            {
                this.DrawAvatar(spriteBatch);
            }

            if (!string.IsNullOrEmpty(this.hoverText))
            {
                IClickableMenu.drawHoverText(spriteBatch, this.hoverText, Game1.smallFont);
            }
        }

        public void DrawSkills(SpriteBatch spriteBatch)
        {
            Rectangle titleSafeArea = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();

            for (int i = 0; i < this.Skills.Count; i++)
            {
                var skill = this.Skills[i];
                float xOffset = 96;
                float yOffset = 52;
                float iconOffset = 20;
                Vector2 iconPosition = new Vector2(titleSafeArea.Left + xOffset + iconOffset + (i * 76), titleSafeArea.Bottom - yOffset);
                Vector2 framePosition = new Vector2(titleSafeArea.Left + xOffset + (i * 76), titleSafeArea.Bottom - (yOffset + iconOffset) - 4);

                if (Game1.isOutdoorMapSmallerThanViewport())
                {
                    iconPosition.X = Math.Max(titleSafeArea.Left + xOffset + iconOffset + (i * 76), -Game1.viewport.X + xOffset + iconOffset + (i * 76));
                    framePosition.X = Math.Max(titleSafeArea.Left + xOffset + (i * 76), -Game1.viewport.X + xOffset + (i * 76));
                }

                skill.UpdatePosition(framePosition, iconPosition);
                skill.Draw(spriteBatch);
            }
        }

        public void DrawAvatar(SpriteBatch spriteBatch)
        {
            Vector2 position = new Vector2(0, Game1.viewport.Height - 64 - IClickableMenu.borderWidth + 4);
            if (Game1.isOutdoorMapSmallerThanViewport())
                position.X = Math.Max(position.X, (float)(-Game1.viewport.X));
            Utility.makeSafe(ref position, 64, 64);
            this.avatar.bounds = new Rectangle((int)position.X + 16, (int)position.Y, 64, 64);
            this.avatar.draw(spriteBatch, Color.White, 1);
        }

        public void PerformHoverAction(int x, int y)
        {
            this.hoverText = "";

            if (this.avatar != null)
            {
                this.avatar.tryHover(x, y, .15f);
                if (this.avatar.containsPoint(x, y))
                    this.hoverText = this.avatar.hoverText;
            }

            foreach (var skill in this.Skills)
            {
                skill.PerformHoverAction(x, y);
                if (skill.ShowTooltip)
                    this.hoverText = skill.HoverText;

            }
        }

        public void Update(UpdateTickedEventArgs e)
        {
            foreach (var skill in this.Skills)
                skill.Update(e);

            this.PerformHoverAction(Game1.getMouseX(), Game1.getMouseY());
        }
    }
}
