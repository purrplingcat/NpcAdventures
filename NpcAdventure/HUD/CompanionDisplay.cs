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
            Vector2 position = new Vector2(Game1.viewport.Width - 70 - IClickableMenu.borderWidth, 370);

            for (int i = 0; i < this.Skills.Count; i++)
            {
                var skill = this.Skills[i];
                float xOffset = 50;
                float iconOffset = 16;
                float iconGrid = 68;
                Vector2 iconPosition = new Vector2(position.X - xOffset + iconOffset - (i * iconGrid), position.Y);
                Vector2 framePosition = new Vector2(position.X - xOffset - (i * iconGrid), position.Y - iconOffset - 3);

                if (Game1.isOutdoorMapSmallerThanViewport())
                {
                    iconPosition.X = Math.Min(position.X, -Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64 - 70 - IClickableMenu.borderWidth) - xOffset + iconOffset - (i * iconGrid);
                    framePosition.X = Math.Min(position.X, -Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64 - 70 - IClickableMenu.borderWidth) - xOffset - (i * iconGrid);
                }

                skill.UpdatePosition(framePosition, iconPosition);
                skill.Draw(spriteBatch);
            }
        }

        public void DrawAvatar(SpriteBatch spriteBatch)
        {
            Vector2 position = new Vector2(Game1.viewport.Width - 70 - IClickableMenu.borderWidth, 318);
            if (Game1.isOutdoorMapSmallerThanViewport())
                position.X = Math.Min(position.X, -Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64 - 70 - IClickableMenu.borderWidth);
            Utility.makeSafe(ref position, 64, 64);
            this.avatar.bounds = new Rectangle((int)position.X + 16, (int)position.Y, 64, 96);
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
