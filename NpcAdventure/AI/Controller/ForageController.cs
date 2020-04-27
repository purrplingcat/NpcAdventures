﻿using Microsoft.Xna.Framework;
using NpcAdventure.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Locations;
using StardewModdingAPI.Utilities;

namespace NpcAdventure.AI.Controller
{
    internal class ForageController : IController
    {
        private readonly AI_StateMachine ai;
        private readonly PathFinder pathFinder;
        private readonly FollowJoystick joystick;
        private readonly List<TerrainFeature> ignoreList;
        private readonly Random r;
        private TerrainFeature targetObject;
        protected Stack<Item> foragedObjects;
        protected int[] springForage = new int[] { 16, 18, 20, 22, 399 };
        protected int[] summerForage = new int[] { 396, 398, 402 };
        protected int[] fallForage = new int[] { 404, 406, 408, 410 };
        protected int[] winterForage = new int[] { 283, 412, 414, 416, 418 };
        protected int[] caveForage = new int[] { 78, 420, 422 };
        protected int[] desertForage = new int[] { 88, 90 };
        protected int[] beachForage = new int[] { 372, 392, 393, 394, 397, 718, 719, 723 };
        protected int[] woodsSpringForage = new int[] { 257, 404 };
        protected int[] woodsSummerForage = new int[] { 259, 420 };
        protected int[] woodsFallForage = new int[] { 281, 420 };
        protected int[] rareForage = new int[] { 347, 114 };

        public NPC Forager => this.ai.npc;
        public Farmer Leader => this.ai.player;
        public int ForagingLevel => Math.Max(this.Leader.ForagingLevel
            - (this.Leader.professions.Contains(Farmer.gatherer) ? 0 : 2), 0);

        public ForageController(AI_StateMachine ai, IModEvents events)
        {
            this.ai = ai;
            this.ignoreList = new List<TerrainFeature>();
            this.pathFinder = new PathFinder(this.Forager.currentLocation, this.Forager, this.ai.player);
            this.joystick = new FollowJoystick(this.Forager, this.pathFinder);
            this.joystick.EndOfRouteReached += this.Joystick_EndOfRouteReached;
            this.ai.LocationChanged += this.Ai_LocationChanged;
            this.r = new Random((int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed);
            this.foragedObjects = new Stack<Item>();

            events.GameLoop.TimeChanged += this.GameLoop_TimeChanged;
        }

        private void GameLoop_TimeChanged(object sender, TimeChangedEventArgs e)
        {
            int keepMindChanceNum = this.Leader.getFriendshipHeartLevelForNPC(this.Forager.Name) - this.foragedObjects.Count + 1;

            if (Helper.IsSpouseMarriedToFarmer(this.Forager, this.Leader)) {
                keepMindChanceNum = (int)(keepMindChanceNum * 1.5);
            }

            if (this.HasAnyForage() && this.r.Next(Math.Max(keepMindChanceNum, 2)) == 1)
            {
                // Chance 1:<count of frindship hearts> to forager changes their mind 
                // to share some foraged item with you and don't share it
                this.foragedObjects.Pop();
                this.ai.Monitor.Log($"{this.Forager.Name} changed her/his mind and don't share last forage with farmer.");
            }
        }

        private void Joystick_EndOfRouteReached(object sender, FollowJoystick.EndOfRouteReachedEventArgs e)
        {
            if (this.ai.CurrentController != this)
                return;

            if (this.targetObject != null)
            {
                this.targetObject.performUseAction(this.Forager.getTileLocation(), this.Forager.currentLocation);
                this.ignoreList.Add(this.targetObject);
            } else
            {
                this.Forager.currentLocation.localSound("leafrustle");
                this.Forager.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 1085, 58, 58), 60f, 8, 0, this.Forager.GetGrabTile() * 64, false, this.Forager.FacingDirection == 3, 1f, 0.0f, Color.White, 1f, 0.0f, 0.0f, 0.0f, false));
            }

            double potentialChance = 0.02 + 1.0 / (this.foragedObjects.Count + 1) + this.Leader.LuckLevel / 100.0 + this.Leader.DailyLuck;
            double boost = this.Leader.professions.Contains(Farmer.gatherer) ? 2.0 : 1.0;
            double realChance = potentialChance * 0.33 + this.Leader.ForagingLevel * 0.005 * boost;

            if (this.r.NextDouble() < realChance)
            {
                this.PickForageObject(this.targetObject);
            }

            this.targetObject = null;
            this.IsIdle = true;
        }

        public void PickForageObject(TerrainFeature source)
        {
            int skill = this.ForagingLevel;
            int quality = 0;

            if (skill >= 8)
                quality = 3;
            else if (skill >= 6)
                quality = 2;
            else if (skill >= 2)
                quality = 1;

            GameLocation location = this.Forager.currentLocation;
            string locationName = location.Name;
            string season = Game1.currentSeason;
            int objectIndex = -1;

            if (source != null && source is Tree tree && tree.growthStage.Value >= Tree.treeStage)
            {
                if (season == "winter" || this.ForagingLevel < 1)
                    return;

                switch (tree.treeType)
                {
                    case Tree.bushyTree:
                        objectIndex = 309;
                        break;
                    case Tree.leafyTree:
                        objectIndex = 310;
                        break;
                    case Tree.pineTree:
                        objectIndex = 311;
                        break;
                }

                if (season == "fall" && tree.treeType == Tree.leafyTree && SDate.Now().Day >= 14)
                    objectIndex = 408;

                if (objectIndex != -1)
                    this.SaveForage(new SObject(objectIndex, 1, false, -1, quality));

                return;
            }

            if (source != null && source is Bush bush)
            {
                if (this.ForagingLevel > 5 && this.r.NextDouble() < 0.005)
                {
                    // There is a chance <1% to get a rare forage item
                    this.SaveForage(new SObject(this.rareForage[this.r.Next(this.rareForage.Length)], 1, false, -1, 0));
                    return;
                }

                if (BushIsInBloom(bush))
                {
                    objectIndex = 296;

                    if (season == "fall")
                        objectIndex = 410;
                    if (bush.size == 3)
                        objectIndex = 815;

                    this.SaveForage(new SObject(objectIndex, 1, false, -1, quality));
                    return;
                }
            }

            if (locationName.Equals("Woods"))
            {
                switch (season)
                {
                    case "spring":
                        objectIndex = this.woodsSpringForage[this.r.Next(2)];
                        break;
                    case "summer":
                        objectIndex = this.woodsSummerForage[this.r.Next(2)];
                        break;
                    case "fall":
                        objectIndex = this.woodsFallForage[this.r.Next(2)];
                        break;
                    default:
                        objectIndex = this.winterForage[this.r.Next(5)];
                        break;
                }
            }
            else if (locationName.Equals("Beach"))
            {
                objectIndex = this.beachForage[this.r.Next(8)];
            }
            else if (locationName.Equals("Desert"))
            {
                objectIndex = this.desertForage[this.r.Next(2)];
            }
            else if (location is MineShaft)
            {
                objectIndex = this.caveForage[this.r.Next(2)];
            }
            else
            {
                switch (season)
                {
                    case "spring":
                        objectIndex = this.springForage[this.r.Next(5)];

                        if (objectIndex == 399 && !locationName.Equals("Forest"))
                        {
                            // Spring onion can be found only in Cindersap Forest
                            objectIndex = this.springForage[this.r.Next(4)];
                        }
                        break;
                    case "summer":
                        objectIndex = this.summerForage[this.r.Next(3)];
                        break;
                    case "fall":
                        objectIndex = this.fallForage[this.r.Next(4)];
                        break;
                    case "winter":
                        objectIndex = this.winterForage[this.r.Next(5)];
                        break;
                }
            }

            if (objectIndex != -1)
                this.SaveForage(new SObject(objectIndex, 1, false, -1, quality));
        }

        private static bool BushIsInBloom(Bush bush)
        {
            SDate date = SDate.Now();

            return !bush.townBush.Value && bush.inBloom(date.Season, date.Day);
        }

        private void SaveForage(SObject foragedObject)
        {
            if (foragedObject == null)
                return;

            double shareChance = 0.42 + this.Leader.getFriendshipHeartLevelForNPC(this.Forager.Name) * 0.016;

            this.Forager.doEmote(Game1.random.NextDouble() < .1f ? 20 : 16);

            if (this.r.NextDouble() < shareChance)
            {
                this.foragedObjects.Push(foragedObject);
                this.ai.Monitor.Log($"{this.Forager.Name} wants to share a foraged item with farmer");
            }
        }

        internal bool CanForage()
        {
            GameLocation location = this.Forager.currentLocation;

            return (location.IsOutdoors || location is MineShaft mines && mines.mineLevel % 10 != 0) && !this.IsLeaderTooFar();
        }

        internal bool GiveForageTo(Farmer player)
        {
            if (player.addItemToInventoryBool(this.foragedObjects.Peek()))
            {
                this.foragedObjects.Pop();
                return true;
            }

            this.ai.Monitor.Log("Can't add shared forages to inventory, it's probably full!");
            return false;
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

        protected virtual bool IsLeaderTooFar()
        {
            return Helper.Distance(this.Leader.getTileLocationPoint(), this.Forager.getTileLocationPoint()) > 12f;
        }

        /// <summary>
        /// Pick tile for walk to and forage
        /// </summary>
        /// <param name="source"></param>
        /// <param name="xTilesAround"></param>
        /// <returns></returns>
        protected virtual Vector2 PickTile(Vector2 source, int xTilesAround, int yTilesAround)
        {
            Vector2 thisTile = source;

            int dir = Game1.random.Next(0, 4);
            Vector2 nextTile;
            switch (dir)
            {
                case 0:
                    nextTile = new Vector2(thisTile.X, thisTile.Y - yTilesAround); break;
                case 1:
                    nextTile = new Vector2(thisTile.X + xTilesAround, thisTile.Y); break;
                case 2:
                    nextTile = new Vector2(thisTile.X, thisTile.Y + yTilesAround); break;
                case 3:
                    nextTile = new Vector2(thisTile.X - xTilesAround, thisTile.Y); break;
                default:
                    nextTile = thisTile; break;
            }

            if (this.pathFinder.IsWalkableTile(nextTile))
                thisTile = nextTile;
            return thisTile;
        }

        protected virtual Vector2 PickTile()
        {
            int tilesAround = Game1.random.Next(2, 4);

            return this.PickTile(this.Forager.getTileLocation(), tilesAround, tilesAround);
        }

        private Vector2 GetTerrainFeatureTilePosition(TerrainFeature feature)
        {
            if (feature is Bush bush)
            {
                return bush.tilePosition.Value;
            }

            return feature.currentTileLocation;
        }

        /// <summary>
        /// Acquire a place with a terrain feature (like bush or tree) for foraging
        /// </summary>
        private void AcquireTerrainFeature()
        {
            var bushes = this.Forager.currentLocation.largeTerrainFeatures
                .Where((feature) => feature is Bush && !this.ignoreList.Contains(feature));

            var trees = this.Forager.currentLocation.terrainFeatures.Values
                .Where((feature) => feature is Tree t && t.growthStage.Value > Tree.sproutStage && !this.ignoreList.Contains(feature))
                .Union(bushes.Cast<TerrainFeature>())
                .ToList();

            if (trees.Count <= 0)
            {
                this.joystick.AcquireTarget(this.PickTile());
                return;
            }

            trees.Sort((f1, f2) =>
            {
                Vector2 vfo = this.Forager.getTileLocation(); // vfo as Vector of Forager
                Vector2 vtf1 = this.GetTerrainFeatureTilePosition(f1); // vtf as Vector of Terrain Feature (like a Bush or Tree)
                Vector2 vtf2 = this.GetTerrainFeatureTilePosition(f2);
                float d1 = Utility.distance(vfo.X, vtf1.X, vtf1.Y, vfo.Y);
                float d2 = Utility.distance(vfo.X, vtf2.X, vtf2.Y, vfo.Y);

                if (d1 == d2)
                {
                    return 0;
                }

                return d1.CompareTo(d2);
            });

            var tree = trees.First();
            Vector2 vTree = this.GetTerrainFeatureTilePosition(tree);

            if (Vector2.Distance(vTree, this.Forager.getTileLocation()) > 8)
            {
                // Nearest bush/tree is too far, fallback to pick a tile around
                this.joystick.AcquireTarget(this.PickTile());
                return;
            }

            if (this.joystick.AcquireTarget(this.PickTile(vTree, (tree is Bush bush ? bush.size + 1 : 1), 1)))
            {
                this.targetObject = tree;
            } else
            {
                this.joystick.AcquireTarget(this.PickTile());
            }
        }

        public void Update(UpdateTickedEventArgs e)
        {
            if (this.IsIdle || (!Context.IsPlayerFree && !Context.IsMultiplayer))
            {
                return;
            }

            if (!this.CanForage())
            {
                this.IsIdle = true;
                return;
            }

            if (e.IsMultipleOf(30) && this.IsLeaderTooFar())
            {
                this.YellAndAbort();
                return;
            }

            if (!this.joystick.IsFollowing)
            {
                if (this.r.NextDouble() > .5f)
                {
                    this.AcquireTerrainFeature();
                } else 
                {
                    this.joystick.AcquireTarget(this.PickTile());
                }
            }

            this.joystick.Speed = 4f;
            this.joystick.Update(e);
        }

        private void YellAndAbort()
        {
            this.IsIdle = true;
            this.Leader.changeFriendship(-5, this.Forager);
            Game1.drawDialogue(this.Forager, DialogueHelper.GetFriendSpecificDialogueText(this.Forager, this.Leader, "farmerRunAway"));

            if (this.HasAnyForage() && this.r.Next(3) == 1)
            {
                // Chance 1:3 forager changes their mind 
                // to share some foraged item with you and don't share it
                this.foragedObjects.Pop();
                this.ai.Monitor.Log($"{this.Forager.Name} changed her/his mind and don't share last forage with farmer.");
            }
        }
    }
}
