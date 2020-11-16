using NpcAdventure.Utils;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;
using System.Collections.Generic;
using System.Linq;

namespace NpcAdventure.AI.Controller
{
    internal class LovePeaceController : FollowController
    {
        public const float DEFEND_TILE_RADIUS = 3f;

        private readonly List<LovedMonster> lovedMonsters;

        public LovePeaceController(AI_StateMachine ai) : base(ai)
        {
            this.lovedMonsters = new List<LovedMonster>();
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
                    monster.showTextAboveHead("<");
                }

                this.follower.doEmote(20);
            }
            
            base.Update(e);
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

        private class LovedMonster
        {
            public Monster Monster { get; }
            public int OriginalDamage { get; }
            public int OriginalThreshold { get; }
            public int TTL { get; private set; }
            public int LoveInvicibility { get; }
            public int LastHealth { get; private set; }

            public LovedMonster(Monster monster, int ttl, int loveInvicibility)
            {
                this.Monster = monster;
                this.OriginalDamage = monster.DamageToFarmer;
                this.OriginalThreshold = monster.moveTowardPlayerThreshold.Value;
                this.LoveInvicibility = loveInvicibility;
                this.LastHealth = this.Monster.Health;
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
            }

            public void RevokeLove()
            {
                this.Monster.DamageToFarmer = this.OriginalDamage;
                this.Monster.moveTowardPlayerThreshold.Value = this.OriginalThreshold;
                this.LastHealth = this.Monster.Health;
                this.TTL -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
            }
        }
    }
}
