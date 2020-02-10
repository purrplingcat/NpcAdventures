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
        private List<LargeTerrainFeature> ignoreList;
        private LargeTerrainFeature targetObject;

        public NPC Forager => this.ai.npc;

        public ForageController(AI_StateMachine ai)
        {
            this.ai = ai;
            this.ignoreList = new List<LargeTerrainFeature>();
            this.pathFinder = new PathFinder(this.Forager.currentLocation, this.Forager, this.ai.player);
            this.joystick = new FollowJoystick(this.Forager, this.pathFinder);
            this.joystick.EndOfRouteReached += this.Joystick_EndOfRouteReached;
            this.ai.LocationChanged += this.Ai_LocationChanged;
        }

        private void Joystick_EndOfRouteReached(object sender, FollowJoystick.EndOfRouteReachedEventArgs e)
        {
            if (this.ai.CurrentController != this)
                return;

            this.targetObject.performUseAction(this.Forager.getTileLocation(), this.Forager.currentLocation);
            this.ignoreList.Add(this.targetObject);
            this.targetObject = null;
            this.IsIdle = true;
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

        protected virtual Vector2 PickTile(Vector2 source, int tilesToWalk)
        {
            Vector2 thisTile = source;

            int dir = Game1.random.Next(0, 4);
            Vector2 nextTile;
            switch (dir)
            {
                case 0:
                    nextTile = new Vector2(thisTile.X, thisTile.Y - tilesToWalk); break;
                case 1:
                    nextTile = new Vector2(thisTile.X + tilesToWalk, thisTile.Y); break;
                case 2:
                    nextTile = new Vector2(thisTile.X, thisTile.Y + tilesToWalk); break;
                case 3:
                    nextTile = new Vector2(thisTile.X - tilesToWalk, thisTile.Y); break;
                default:
                    nextTile = thisTile; break;
            }

            if (this.pathFinder.IsWalkableTile(nextTile))
                thisTile = nextTile;
            return thisTile;
        }

        private void AcquireTerrainFeature()
        {
            var bushes = this.Forager.currentLocation.largeTerrainFeatures.Where(
                (feature) => feature is Bush && !this.ignoreList.Contains(feature)
            ).ToList();

            if (bushes.Count <= 0)
                return;

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
            }
        }

        public void Update(UpdateTickedEventArgs e)
        {
            if (this.targetObject == null)
            {
                this.AcquireTerrainFeature();
            }

            this.joystick.Speed = 4f;
            this.joystick.Update(e);
        }
    }
}
