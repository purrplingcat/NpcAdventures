﻿using Microsoft.Xna.Framework;
using NpcAdventure.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace NpcAdventure.AI
{
    internal partial class AI_StateMachine
    {
        const int HEAL_COUNTDOWN = 4800;

        private int medkits = 3;
        private int healCooldown = 0;
        private bool lifeSaved;

        private bool TryHealFarmer()
        {
            if (this.medkits > 0 && this.player.health < (this.player.maxHealth / 3) && this.player.health > 0)
            {
                float healthBonus = (this.player.maxHealth / 100) * (this.player.getFriendshipHeartLevelForNPC(this.npc.Name) / 2); // % health bonus based on friendship hearts
                float medkitPower = 1 + (this.player.FarmingLevel * (this.player.ForagingLevel + 1)) / 100; 
                float baseHealth = Math.Max(10, (1 / this.player.health * 10) + Game1.random.Next(0, (int)(this.player.maxHealth * .1f))) + healthBonus;
                int health = (int)(baseHealth * medkitPower);
                this.player.health += health;
                this.healCooldown = HEAL_COUNTDOWN;
                this.medkits--;

                if (this.player.health > this.player.maxHealth)
                    this.player.health = this.player.maxHealth;

                Game1.drawDialogue(this.npc, this.Csm.Dialogues.GetFriendSpecificDialogueText(this.player, "heal"));
                Game1.addHUDMessage(new HUDMessage(this.Csm.ContentLoader.LoadString("Strings/Strings:healed", this.npc.displayName, health), HUDMessage.health_type));
                this.player.currentLocation.playSound("healSound");
                this.hud.GlowSkill("doctor", Color.Lime, HEAL_COUNTDOWN / 60);
                this.Monitor.Log($"{this.npc.Name} healed you! Remaining medkits: {this.medkits}", LogLevel.Info);
                this.Monitor.Log($"Health bonus: {healthBonus}; Medkit power: {medkitPower}; Base amount: {baseHealth}; Got total amount: {health}");
                return true;
            }

            if (this.medkits == 0)
            {
                this.Monitor.Log($"No medkits. {this.npc.Name} can't heal you!", LogLevel.Info);
                Game1.drawDialogue(this.npc, this.Csm.Dialogues.GetFriendSpecificDialogueText(this.player, "nomedkits"));
                this.medkits = -1;
            }

            return false;
        }

        /// <summary>
        /// Try to save player's life when player is in dangerous with first aid medikit.
        /// There are chance based on player's luck level and daily luck to life will be saved or not
        /// Can try save life when NPC and any monster are near to player
        /// </summary>
        private void TrySaveLife()
        {
            float npcPlayerDistance = Helper.Distance(this.player.GetBoundingBox().Center, this.npc.GetBoundingBox().Center);
            bool noMonstersNearPlayer = Helper.GetNearestMonsterToCharacter(this.player, 4f) == null;

            if (this.player.health <= 0 || npcPlayerDistance > 2.25 * Game1.tileSize || noMonstersNearPlayer)
                return;

            double chance = Math.Max(0.01, (this.player.DailyLuck / 2.0 + this.player.LuckLevel / 100.0 + this.player.getFriendshipHeartLevelForNPC(this.npc.Name) * 0.05));
            double random = Game1.random.NextDouble();
            this.Monitor.Log($"{this.npc.Name} try to save your poor life. Chance is: {chance}/{1.0 - chance}, Random pass: {random}");

            if (random <= chance || random >= (1.0 - chance))
            {
                this.lifeSaved = this.TryHealFarmer();

                if (this.lifeSaved)
                {
                    Game1.showGlobalMessage(this.loader.LoadString("Strings/Strings:lifeSaved"));
                    this.Monitor.Log($"{this.npc.Name} saved your life!", LogLevel.Info);
                }
            }
        }

        public void UpdateDoctor(UpdateTickedEventArgs e)
        {
            // Countdown to companion can heal you if heal cooldown greather than zero
            if (this.healCooldown > 0 && Context.IsPlayerFree)
            {
                // Every 3 seconds while countdown is not under 70% of progress add 1% of maxhealth to player's health (Companion heal side-effect) 
                // Take effect when cooldown half way though and player's health is lower than 61% of maxhealth
                // Adds count of friendship hearts as health bonus
                if (e.IsMultipleOf(180) && (this.healCooldown > HEAL_COUNTDOWN * .7f) && this.player.health < (this.player.maxHealth * .61f))
                    this.player.health += Math.Max(1, (int)Math.Round(this.player.maxHealth * .01f)) + (int)Math.Round(this.player.getFriendshipHeartLevelForNPC(this.npc.Name) / 4f);

                this.healCooldown--;
            }

            // Doctor companion try to save your life if you have less than 5% of health and your life not saved in last time
            if (e.IsOneSecond && this.medkits > 0 && this.player.health < this.player.maxHealth * 0.05 && !this.lifeSaved)
                this.TrySaveLife();
        }
    }
}
