﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NpcAdventure.Utils;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NpcAdventure.AI.Controller
{
    internal class LovePeaceController : FollowController
    {
        public const float DEFEND_TILE_RADIUS = 3f;

        private readonly List<LovedMonster> lovedMonsters;
        private readonly Texture2D loveTexture;

        public LovePeaceController(AI_StateMachine ai) : base(ai)
        {
            this.lovedMonsters = new List<LovedMonster>();
            this.loveTexture = Game1.content.Load<Texture2D>(ai.ContentLoader.GetAssetKey("~/Sprites/love.png"));
        }

        public bool IsAngryMonstersHere => this.FindAngryMonsters().Count() > 0;
        public override bool IsIdle => !this.IsAngryMonstersHere;

        public override void Update(UpdateTickedEventArgs e)
        {
            if (e.IsMultipleOf(30) && this.IsAngryMonstersHere)
            {
                foreach (var monster in this.FindAngryMonsters())
                {
                    if (this.lovedMonsters.Any(lm => lm.Monster == monster))
                        continue;

                    this.lovedMonsters.Add(new LovedMonster(monster, 10000, 1200));
                    monster.doEmote(20);
                }

                this.follower.doEmote(20);
            }
            
            base.Update(e);
        }

        public bool IsLovedMonster(Monster monster)
        {
            return this.lovedMonsters.Any(lm => lm.Monster == monster && lm.TTL > 0);
        }

        public override void SideUpdate(UpdateTickedEventArgs e)
        {
            this.GiveLove();
            base.SideUpdate(e);
        }

        private void GiveLove()
        {
            for (int i = 0; i < this.lovedMonsters.Count; i++)
            {
                if (this.lovedMonsters[i].TTL <= 0)
                {
                    this.lovedMonsters[i].RevokeLove();

                    if (this.lovedMonsters[i].TTL < -this.lovedMonsters[i].LoveInvicibility)
                        this.lovedMonsters.RemoveAt(i--);

                    continue;
                }

                if (this.lovedMonsters[i].LastHealth != this.lovedMonsters[i].Monster.Health)
                {
                    int lostPoints = 10;

                    this.follower.doEmote(12);

                    if (this.lovedMonsters[i].Monster.Health <= 0)
                    {
                        lostPoints = 75;
                        this.follower.showTextAboveHead($"Why did you kill {(this.lovedMonsters[i].Monster.Gender == 1 ? "her" : "him")}?");
                    }

                    if (this.leader is Farmer farmer)
                        farmer.changeFriendship(-lostPoints, this.follower);
                }

                this.lovedMonsters[i].AcceptLove();
            }
        }

        /// <summary>
        /// Find angry monsters in defined radius. 
        /// </summary>
        /// <returns>Null if no monsters found, otherwise the monsters</returns>
        private IEnumerable<Monster> FindAngryMonsters()
        {
            return Helper.GetNearestMonstersToCharacter(this.follower, DEFEND_TILE_RADIUS)
                .Select(mKv => mKv.Value)
                .Where(m => Helper.IsValidMonster(m) && !this.lovedMonsters.Any(lm => lm.Monster == m));
        }

        public void DrawLove(SpriteBatch spriteBatch)
        {
            Vector2 lovePosition;

            foreach (var lovedMonster in this.lovedMonsters.Where(lm => lm.TTL > 0))
            {
                lovePosition = lovedMonster.Monster.getLocalPosition(Game1.viewport);
                lovePosition.X += lovedMonster.Monster.Sprite.SpriteWidth / 2 + this.loveTexture.Width;
                lovePosition.Y -= 24f;

                spriteBatch.Draw(this.loveTexture, lovePosition, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
            }
        }

        private class LovedMonster
        {
            public Monster Monster { get; }
            public int OriginalDamage { get; }
            public int OriginalThreshold { get; }
            public int TTL { get; private set; }
            public int LoveInvicibility { get; private set; }
            public int LastHealth { get; private set; }
            public Vector2 LastVelocity { get; private set; }
            public bool LastCharging { get; private set; }

            public LovedMonster(Monster monster, int ttl, int loveInvicibility)
            {
                this.Monster = monster;
                this.OriginalDamage = monster.DamageToFarmer;
                this.OriginalThreshold = monster.moveTowardPlayerThreshold.Value;
                this.LoveInvicibility = loveInvicibility;
                this.LastHealth = this.Monster.Health;
                this.LastVelocity = new Vector2(monster.xVelocity, monster.yVelocity);
                this.LastCharging = monster.isCharging;
                this.TTL = ttl;
            }

            public void AcceptLove()
            {
                this.Monster.focusedOnFarmers = false;
                this.Monster.IsWalkingTowardPlayer = false;
                this.Monster.DamageToFarmer = 0;
                this.Monster.moveTowardPlayerThreshold.Value = 0;
                this.Monster.farmerPassesThrough = true;
                this.LastHealth = this.Monster.Health;
                this.TTL -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;

                if (this.Monster is Fly fly)
                {
                    fly.xVelocity = this.LastVelocity.X;
                    fly.yVelocity = this.LastVelocity.Y;
                }

                if (this.Monster is Bug bug)
                {
                    bug.setMovingInFacingDirection();
                }

                if ((double)this.Monster.Position.X < 0.0 || (double)this.Monster.Position.X > (double)(this.Monster.currentLocation.map.GetLayer("Back").LayerWidth * 64) || ((double)this.Monster.Position.Y < 0.0 || (double)this.Monster.Position.Y > (double)(this.Monster.currentLocation.map.GetLayer("Back").LayerHeight * 64)))
                {
                    this.TTL = 0;
                    this.LoveInvicibility = 0;
                    this.Monster.currentLocation.characters.Remove(this.Monster);
                }
            }

            public void RevokeLove()
            {
                this.Monster.DamageToFarmer = this.OriginalDamage;
                this.Monster.moveTowardPlayerThreshold.Value = this.OriginalThreshold;
                this.LastHealth = this.Monster.Health;
                this.Monster.isCharging = this.LastCharging;
                this.TTL -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
            }
        }
    }
}
