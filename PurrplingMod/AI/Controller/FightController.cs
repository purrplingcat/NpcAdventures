using Microsoft.Xna.Framework;
using PurrplingMod.Utils;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingMod.AI.Controller
{
    internal class FightController : FollowController
    {
        private bool idle = false;
        private readonly IModEvents events;
        private readonly Character realLeader;
        private int weaponSwingCooldown = 0;

        public FightController(AI_StateMachine ai, IModEvents events) : base(ai)
        {
            this.realLeader = ai.player;
            this.leader = null;
            this.pathFinder.GoalCharacter = null;
            this.events = events;
        }

        private void World_NpcListChanged(object sender, NpcListChangedEventArgs e)
        {
            if (e.Removed != null && e.Removed.Count() > 0 && e.Removed.Contains(this.leader)) {
                this.leader = null;
                this.pathFinder.GoalCharacter = null;
            }
        }

        public override bool IsIdle => this.idle;

        private bool IsValidMonster(Monster monster)
        {
            if (monster is GreenSlime || monster is BigSlime)
                return true;

            if (monster is RockCrab crab)
                return crab.isMoving();

            if (monster is Bug bug)
                return !bug.isArmoredBug.Value;

            return !monster.IsInvisible;
        }

        private void CheckMonsterToFight()
        {
            Monster monster = Helper.GetNearestMonsterToCharacter(this.follower, 7f);

            if (monster == null || !this.IsValidMonster(monster))
            {
                this.idle = true;
                this.leader = null;
                this.pathFinder.GoalCharacter = null;
                return;
            }

            this.idle = false;
            this.leader = monster;
            this.pathFinder.GoalCharacter = this.leader;
        }

        public override void Update(UpdateTickedEventArgs e)
        {
            if (this.idle)
                return;

            if (Helper.Distance(this.realLeader.getTileLocationPoint(), this.follower.getTileLocationPoint()) > 11f)
            {
                this.idle = true;
                this.ai.monitor.Log("Fight controller iddle, because player is too far");
                return;
            }

            if (this.leader == null)
            {
                this.CheckMonsterToFight();
                return;
            }

            if (this.leader is Monster && !this.IsValidMonster(this.leader as Monster))
            {
                this.idle = true;
                return;
            }
                
            if (this.weaponSwingCooldown > 0)
            {
                this.weaponSwingCooldown--;
            }

            if (this.weaponSwingCooldown > 36)
            {
                this.DoDamage();
            }

            base.Update(e);
        }

        private void DoDamage()
        {
            MeleeWeapon weapon = new MeleeWeapon(11); 
            Rectangle effectiveArea = this.follower.GetBoundingBox();
            effectiveArea.Inflate(96, 96);
            if (this.follower.currentLocation.damageMonster(effectiveArea, weapon.minDamage.Value, weapon.maxDamage.Value, false, weapon.knockback.Value, weapon.addedPrecision.Value, weapon.critChance.Value, weapon.critMultiplier.Value, true, this.realLeader as Farmer))
            {
                this.follower.currentLocation.playSound("clubhit");
            }
        }

        private void AnimateMe()
        {
            this.weaponSwingCooldown = 50;
            this.DoDamage();
        }

        protected override float GetMovementSpeedBasedOnDistance(float distanceFromTarget)
        {
            if (this.weaponSwingCooldown > 36)
                return 0;

            if (distanceFromTarget < 1.25f * Game1.tileSize)
            {
                if (this.weaponSwingCooldown == 0)
                {
                    this.AnimateMe();
                }
                return Math.Max(this.speed - 0.1f, 0.1f);
            }

            return 5.28f;
        }

        protected override void PathfindingRemakeCheck()
        {
            base.PathfindingRemakeCheck();

            if (this.pathToFollow == null)
            {
                this.idle = true;
                this.ai.monitor.Log($"Fight controller iddle, because can't find a path to monster '{this.leader?.Name}'");
            }
        }

        public override void Activate()
        {
            this.events.World.NpcListChanged += this.World_NpcListChanged;
            this.idle = false;
        }

        public override void Deactivate()
        {
            this.events.World.NpcListChanged -= this.World_NpcListChanged;
            this.leader = null;
            this.pathFinder.GoalCharacter = null;
            this.idle = true;
        }
    }
}
