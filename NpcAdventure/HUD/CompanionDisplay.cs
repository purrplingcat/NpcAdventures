using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        public CompanionDisplay(Config config)
        {
            this.Skills = new List<CompanionSkill>();
            this.Config = config;
        }

        public void AddSkill(string type, string description)
        {
            this.Skills.Add(new CompanionSkill(type, description));
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!this.Config.ShowHUD || Game1.eventUp || this.Skills.Count < 1)
                return;

            CompanionSkill toolTipedSkill = null;
            Rectangle titleSafeArea = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();

            for (int i = 0; i < this.Skills.Count; i++)
            {
                var skill = this.Skills[i];
                Vector2 iconPosition = new Vector2(titleSafeArea.Left + 38 + (i * 76), titleSafeArea.Bottom - 52);
                Vector2 framePosition = new Vector2(titleSafeArea.Left + 18 + (i * 76), titleSafeArea.Bottom - 76);

                if (Game1.isOutdoorMapSmallerThanViewport())
                {
                    iconPosition.X = Math.Max(titleSafeArea.Left + 38 + (i * 76), -Game1.viewport.X + 38 + (i * 76));
                    framePosition.X = Math.Max(titleSafeArea.Left + 18 + (i * 76), -Game1.viewport.X + 18 + (i * 76));
                }

                if (skill.ShowTooltip)
                    toolTipedSkill = skill;

                skill.UpdatePosition(framePosition, iconPosition);
                skill.Draw(spriteBatch);
            }

            if (toolTipedSkill != null)
            {
                IClickableMenu.drawHoverText(spriteBatch, toolTipedSkill.HoverText, Game1.smallFont);
            }
        }

        public void Update(UpdateTickedEventArgs e)
        {
            for (int i = 0; i < this.Skills.Count; i++)
            {
                var skill = this.Skills[i];
                skill.Update(e);
                skill.PerformHoverAction(Game1.getMouseX(), Game1.getMouseY());
            }
        }
    }
}
