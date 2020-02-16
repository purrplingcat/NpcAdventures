using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcAdventure.AI.Controller
{
    internal class ForageController : IController
    {
        private readonly AI_StateMachine ai;
        private readonly PathFinder pathFinder;
        private readonly FollowJoystick joystick;
        private readonly List<LargeTerrainFeature> ignoreList;
        private readonly Random r;
        private LargeTerrainFeature targetObject;
        protected List<StardewValley.Object> foragedObjects;
        protected int[] springForage = new int[] { 16, 18, 20, 22, 296, 399 };
        protected int[] summerForage = new int[] { 396, 398, 402 };
        protected int[] fallForage = new int[] { 404, 406, 408, 410 };
        protected int[] winterForage = new int[] { 283, 412, 414, 416, 418 };
        protected int[] caveForage = new int[] { 78, 420, 422 };
        protected int[] desertForage = new int[] { 88, 90 };
        protected int[] beachForage = new int[] { 372, 392, 393, 394, 397, 718, 719, 723 };
        protected int[] woodsSpringForage = new int[] { 257, 404 };
        protected int[] woodsSummerForage = new int[] { 259, 420 };
        protected int[] woodsFallForage = new int[] { 281, 420 };

        public NPC Forager => this.ai.npc;
        public Farmer Leader => this.ai.player;

        public ForageController(AI_StateMachine ai)
        {
            this.ai = ai;
            this.ignoreList = new List<LargeTerrainFeature>();
            this.pathFinder = new PathFinder(this.Forager.currentLocation, this.Forager, this.ai.player);
            this.joystick = new FollowJoystick(this.Forager, this.pathFinder);
            this.joystick.EndOfRouteReached += this.Joystick_EndOfRouteReached;
            this.ai.LocationChanged += this.Ai_LocationChanged;
            this.r = new Random((int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed);
            this.foragedObjects = new List<StardewValley.Object>();
        }

        private void Joystick_EndOfRouteReached(object sender, FollowJoystick.EndOfRouteReachedEventArgs e)
        {
            if (this.ai.CurrentController != this)
                return;

            if (this.targetObject != null)
            {
                this.targetObject.performUseAction(this.Forager.getTileLocation(), this.Forager.currentLocation);
                this.ignoreList.Add(this.targetObject);
                this.targetObject = null;
            }

            if (this.r.Next(9) == 1)
            {
                this.DoForage();
            }

            this.IsIdle = true;
        }

        public void DoForage()
        {
            StardewValley.Object foragedObject = null;
            int quality;

            if (this.Leader.ForagingLevel - 2 <= 2)
                quality = 1;
            else if (this.Leader.ForagingLevel - 2 <= 6)
                quality = 2;
            else if (this.Leader.ForagingLevel - 2 <= 9)
                quality = 3;
            else
                quality = 4;

            string locationName = this.Forager.currentLocation.Name;

            if (locationName.Equals("Woods"))
            {
                string season = Game1.currentSeason;
                switch (season)
                {
                    case "spring":
                        foragedObject = new StardewValley.Object(this.woodsSpringForage[this.r.Next(2)], 1, false, -1, quality); break;
                    case "summer":
                        foragedObject = new StardewValley.Object(this.woodsSummerForage[this.r.Next(2)], 1, false, -1, quality); break;
                    case "fall":
                        foragedObject = new StardewValley.Object(this.woodsFallForage[this.r.Next(2)], 1, false, -1, quality); break;
                    default:
                        foragedObject = new StardewValley.Object(this.winterForage[this.r.Next(5)], 1, false, -1, quality); break;
                }
            }
            else if (locationName.Equals("Beach"))
            {
                foragedObject = new StardewValley.Object(this.beachForage[this.r.Next(8)], 1, false, -1, quality);
            }
            else if (locationName.Equals("Desert"))
            {
                foragedObject = new StardewValley.Object(this.desertForage[this.r.Next(2)], 1, false, -1, quality);
            }
            else
            {
                string season = Game1.currentSeason;
                switch (season)
                {
                    case "spring":
                        foragedObject = new StardewValley.Object(this.springForage[this.r.Next(6)], 1, false, -1, quality); break;
                    case "summer":
                        foragedObject = new StardewValley.Object(this.summerForage[this.r.Next(3)], 1, false, -1, quality); break;
                    case "fall":
                        foragedObject = new StardewValley.Object(this.fallForage[this.r.Next(4)], 1, false, -1, quality); break;
                    case "winter":
                        foragedObject = new StardewValley.Object(this.winterForage[this.r.Next(5)], 1, false, -1, quality); break;
                }
            }

            if (foragedObject != null)
            {
                this.Forager.doEmote(Game1.random.NextDouble() < .1f ? 20 : 16);
                this.foragedObjects.Add(foragedObject);
            }
        }

        internal void GiveForagesTo(Farmer player)
        {
            foreach (var o in this.foragedObjects)
            {
                player.addItemToInventory(o);
            }

            this.foragedObjects.Clear();
        }

        internal bool HasAnyForage()
        {
            return this.foragedObjects.Count > 0;
        }

        private void Ai_LocationChanged(object sender, EventArgsLocationChanged e)
        {
            this.targetObject = null;
            this.ignoreList.Clear();
            this.joystick.Reset();
        }

        public bool IsIdle { get; private set; }

        public void Activate()
        {
            this.IsIdle = false;
        }

        public void Deactivate()
        {
            this.targetObject = null;
            this.joystick.Reset();
        }

        protected virtual Vector2 PickTile(Vector2 source, int tilesAround)
        {
            Vector2 thisTile = source;

            int dir = Game1.random.Next(0, 4);
            Vector2 nextTile;
            switch (dir)
            {
                case 0:
                    nextTile = new Vector2(thisTile.X, thisTile.Y - tilesAround); break;
                case 1:
                    nextTile = new Vector2(thisTile.X + tilesAround, thisTile.Y); break;
                case 2:
                    nextTile = new Vector2(thisTile.X, thisTile.Y + tilesAround); break;
                case 3:
                    nextTile = new Vector2(thisTile.X - tilesAround, thisTile.Y); break;
                default:
                    nextTile = thisTile; break;
            }

            if (this.pathFinder.IsWalkableTile(nextTile))
                thisTile = nextTile;
            return thisTile;
        }

        protected virtual Vector2 PickTile()
        {
            return this.PickTile(this.Forager.getTileLocation(), Game1.random.Next(1, 4));
        }

        private void AcquireTerrainFeature()
        {
            var bushes = this.Forager.currentLocation.largeTerrainFeatures.Where(
                (feature) => feature is Bush && !this.ignoreList.Contains(feature)
            ).ToList();

            if (bushes.Count <= 0)
            {
                this.joystick.AcquireTarget(this.PickTile());
                return;
            }

            bushes.Sort((f1, f2) =>
            {
                Vector2 v1 = this.Forager.getTileLocation();
                Vector2 v2 = this.Forager.getTileLocation();
                float d1 = Utility.distance(v1.X, f1.tilePosition.Value.X, f1.tilePosition.Value.Y, v2.Y);
                float d2 = Utility.distance(v2.X, f2.tilePosition.Value.X, f2.tilePosition.Value.Y, v2.Y);

                if (d1 == d2)
                {
                    return 0;
                }

                return d1.CompareTo(d2);
            });

            var bush = bushes.First();

            if (this.joystick.AcquireTarget(this.PickTile(bush.tilePosition.Value, 2)))
            {
                this.targetObject = bush;
            } else
            {
                this.joystick.AcquireTarget(this.PickTile());
            }
        }

        public void Update(UpdateTickedEventArgs e)
        {
            if (!this.joystick.IsFollowing && this.Forager.currentLocation.IsOutdoors)
            {
                if (this.r.NextDouble() > .5f)
                {
                    this.AcquireTerrainFeature();
                } else if (this.r.Next(5) == 3)
                {
                    this.joystick.AcquireTarget(this.PickTile());
                } else
                {
                    this.IsIdle = true;
                }
            }

            this.joystick.Speed = 4f;
            this.joystick.Update(e);
        }
    }
}
