using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;

namespace NpcAdventure.HUD
{
    class CompanionSkill : Internal.IDrawable, Internal.IUpdateable
    {
        private Vector2 framePosition;
        private Vector2 iconPosition;

        public CompanionSkill(string type, string description)
        {
            this.Type = type;
            this.HoverText = description;
        }

        public string Type { get; }
        public int Index { get; set; }
        public bool ShowTooltip { get; private set; }
        public string HoverText { get; set; }

        public void Draw(SpriteBatch spriteBatch)
        {
            Rectangle icon;

            switch (this.Type)
            {
                case "doctor":
                    icon = new Rectangle(0, 428, 10, 10);
                    break;
                case "warrior":
                    icon = new Rectangle(120, 428, 10, 10);
                    break;
                case "fighter":
                    icon = new Rectangle(40, 428, 10, 10);
                    break;
                default:
                    return;
            }

            spriteBatch.Draw(Game1.mouseCursors, this.framePosition, new Rectangle(384, 373, 18, 18), Color.White * 1f, 0f, Vector2.Zero, 3.4f, SpriteEffects.None, 1f);
            spriteBatch.Draw(Game1.mouseCursors, this.iconPosition, icon, Color.White * 1f, 0f, Vector2.Zero, 2.8f, SpriteEffects.None, 1f);
        }

        public void PerformHoverAction(int x, int y)
        {
            Rectangle frameBounding = new Rectangle((int)this.framePosition.X, (int)this.framePosition.Y, 18 * 4, 18 * 4);

            if (frameBounding.Contains(x, y))
            {
                this.ShowTooltip = true;
                return;
            }

            this.ShowTooltip = false;
        }

        internal void UpdatePosition(Vector2 framePosition, Vector2 iconPosition)
        {
            this.framePosition = framePosition;
            this.iconPosition = iconPosition;
        }

        public void Update(UpdateTickedEventArgs e)
        {

        }
    }
}
