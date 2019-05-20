using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.Locations;
using Microsoft.Xna.Framework;

namespace PurrplingMod
{
    public class FollowController : IUpdateable
    {
        public const int FOLLOWING_LOST_TIMEOUT = 25;
        public const float SPEEDUP_DISTANCE_THRESHOLD = 7;
        public const float MOVE_THRESHOLD_DISTANCE = 3;
        public const float PROXIMITY_THRESHOLD = 80;
        public const float LOST_DISTANCE = 16;
        public const float OUT_OF_RANGE_DISTANCE = 64;
        public const int PATH_MAX_NODE_COUNT = 28;
        public Character leader;
        public NPC follower;
        public int followingLostTime = 0;
        public Queue<Point> pathToFollow;
        public Point currentFollowedPoint;
        public Point leaderLastTileCheckPoint;

        public FollowController()
        {
            this.pathToFollow = new Queue<Point>();
        }

        public void Update(GameTime time)
        {
            if (this.follower == null || this.leader == null)
                return;

            if (this.leader.currentLocation != this.follower.currentLocation || this.FollowerLeaderIsTooFar())
            {
                // Warp follower to leader if leader and follower locations are direrent or theirs distance is too far
                // Reset following leaders's steps path
                FollowController.WarpTo(this.follower, this.leader.currentLocation, this.leader.getTileLocationPoint());
                this.pathToFollow.Clear();
                this.currentFollowedPoint = Point.Zero;
                this.leaderLastTileCheckPoint = Point.Zero;
            }

            // Update follower movement
            this.follower.farmerPassesThrough = true;
            this.UpdateFollowing(time, this.follower, this.leader);
        }

        protected void UpdateFollowing(GameTime time, NPC follower, Character leader)
        {
            Point leaderTilePoint = leader.getTileLocationPoint();
            Point followerTilePoint = follower.getTileLocationPoint();
            Point leaderBoxCenter = leader.GetBoundingBox().Center;
            Point followerBoxCenter = follower.GetBoundingBox().Center;

            if (this.follower.speed != this.leader.speed)
                this.follower.speed = this.leader.speed; // Sync follower's speed with leader

            if (Helper.Distance(leaderTilePoint, followerTilePoint) > SPEEDUP_DISTANCE_THRESHOLD)
                this.follower.addedSpeed = 2; // Leader little bit far? Increase speed a little bit

            if (follower.isMoving())
                this.followingLostTime = 0; // Is follower moving? Reset following lost timer
            else
            {
                // Follower don't moving
                if (Helper.Distance(leaderTilePoint, followerTilePoint) < MOVE_THRESHOLD_DISTANCE)
                    return; // Stay standing if distance between follower and leader is too short

                if (this.followingLostTime++ >= FOLLOWING_LOST_TIMEOUT)
                {
                    // Follower is lost? Try to find direct path to follower or warp on
                    this.ResolveLostFollow();
                    return;
                }
            }

            if (leaderTilePoint != this.leaderLastTileCheckPoint)
            {
                // Leader's position changed? Add theirs current position to steps path
                this.AddPathPoint(leaderTilePoint);
            }

            if (Helper.Distance(leaderBoxCenter, followerBoxCenter) < PROXIMITY_THRESHOLD)
            {
                // Follower stops move when approached to leader and clear step path
                follower.Halt();
                this.pathToFollow.Clear();
                this.currentFollowedPoint = leaderTilePoint;
                return;
            }

            if (this.pathToFollow.Count > PATH_MAX_NODE_COUNT || Helper.Distance(leaderTilePoint, followerTilePoint) > LOST_DISTANCE)
            {
                // Step path is too long or lost leader? Try to find direct path to follower or warp on
                this.ResolveLostFollow(forceWarp: true, emoteWhenPathIsFound: true);
                return;
            }

            if (follower.controller == null) 
            {
                // Follow leader's step path if follower has'nt direct path
                this.CurrentFollowedPointUpdateCheck();
                this.FollowCurrentPoint(); 
            }
        }

        private void ResolveLostFollow(bool forceWarp = false, bool emoteWhenPathIsFound = false)
        {
            Point endTilePoint = this.leader.getTileLocationPoint();

            this.currentFollowedPoint = endTilePoint;
            this.followingLostTime = 0;
            
            // Try to find alternative path to leader's position.
            if (FollowController.ComeTo(this.follower, endTilePoint, emoteWhenPathIsFound))
            {
                // Path found, we are not lost now!
                this.pathToFollow.Clear();
                return; 
            }
            
            if (this.pathToFollow.Count > 0 && !forceWarp) 
            {
                // Force walk through everything, when we have original path and warp is not forced
                this.follower.isCharging = true;
                this.follower.addedSpeed = 4;
            }
            else
            {
                // Warp to leader when no original path exists or warp is forced
                this.pathToFollow.Clear();
                this.follower.isCharging = false;
                FollowController.WarpTo(this.follower, endTilePoint);
            }
        }

        private void CurrentFollowedPointUpdateCheck()
        {
            if (this.currentFollowedPoint == Point.Zero)
                return;
            
            if (this.currentFollowedPoint == this.follower.getTileLocationPoint()) 
            {
                if (this.pathToFollow.Count == 0)
                {
                    // Step path is empty? We are on end of path
                    this.currentFollowedPoint = Point.Zero;
                    return;
                }
                // Followed point reached? Pop next point to follow
                this.currentFollowedPoint = this.pathToFollow.Dequeue();
            }
        }
        
        private void FollowCurrentPoint()
        {
            Rectangle tileBox = new Rectangle(this.currentFollowedPoint.X * 64, this.currentFollowedPoint.Y * 64, 64, 64);
            Rectangle followerBox = this.follower.GetBoundingBox();
            tileBox.Inflate(-2, 0);
            
            if (this.currentFollowedPoint == Point.Zero) 
            {
                this.follower.Halt();
                return;
            }

            if (followerBox.Right > tileBox.Right && followerBox.Left > tileBox.Left)
                this.follower.SetMovingOnlyLeft();
            else if (followerBox.Left < tileBox.Left && followerBox.Right < tileBox.Right)
                this.follower.SetMovingOnlyRight();
            else if (followerBox.Bottom >= tileBox.Bottom - 2)
                this.follower.SetMovingOnlyUp();
            else if (followerBox.Top <= tileBox.Top)
                this.follower.SetMovingOnlyDown();

            this.follower.willDestroyObjectsUnderfoot = false; // Nothing destroy and not moving across walls and solid objects
            this.follower.MovePosition(Game1.currentGameTime, Game1.viewport, this.follower.currentLocation); // Update follower movement
        }

        private void AddPathPoint(Point p)
        {
            if (this.pathToFollow.Count == 0)
                this.currentFollowedPoint = p; // Step path empty? Target current leader's position
            this.pathToFollow.Enqueue(p);
            this.leaderLastTileCheckPoint = p; // Last known leader's position is current position
        }

        private bool FollowerLeaderIsTooFar()
        {
            return Helper.Distance(this.leader.getTileLocationPoint(), this.follower.getTileLocationPoint()) > OUT_OF_RANGE_DISTANCE;
        }

        public static void WarpTo(NPC follower, Point tilePosition)
        {
            follower.Halt();
            follower.controller = follower.temporaryController = null;
            follower.setTilePosition(tilePosition);
        }

        public static void WarpTo(NPC follower, GameLocation location, Point tilePosition)
        {
            if (follower.currentLocation == location)
                FollowController.WarpTo(follower, tilePosition);

            follower.Halt();
            follower.controller = follower.temporaryController = null;
            follower.currentLocation.characters.Remove(follower);
            follower.currentLocation = location;
            follower.setTilePosition(tilePosition);

            location.addCharacter(follower);
        }

        public static bool ComeTo(NPC follower, Point endPointTile, bool emoteWhenPathIsFound = false)
        {
            Point npcTilePosition = follower.getTileLocationPoint();
            bool pathFound = false;

            if (follower.controller == null)
            {
                follower.Halt();
                follower.temporaryController = null;
                follower.willDestroyObjectsUnderfoot = false;

                Stack<Point> path = FollowController.FindPath(npcTilePosition, endPointTile, follower.currentLocation, follower, PATH_MAX_NODE_COUNT);
                follower.controller = new PathFindController(path, follower, follower.currentLocation);

                if (follower.controller.pathToEndPoint == null)
                    follower.doEmote(8); // Question mark emote
                else
                {
                    if (emoteWhenPathIsFound)
                        follower.doEmote(40); // Three dots emote
                    pathFound = true;
                }
            }

            follower.updateEmote(Game1.currentGameTime);
            return pathFound;
        }

        public static Stack<Point> FindPath(Point startTilePoint, Point endPointTile, GameLocation location, NPC n, int limit = 100)
        {
            Stack<Point> path;
            PathFindController.isAtEnd endFunction = new PathFindController.isAtEnd(PathFindController.isAtEndPoint);
            Stack<Point> nearPointsStack = new Stack<Point>(
                Helper.SortPointsByNearest(
                    Helper.NearPoints(endPointTile, 1), startTilePoint
                )
            );

            while (nearPointsStack.Count > 0)
            {
                Point target = nearPointsStack.Pop();
                path = PathFindController.findPath(startTilePoint, target, endFunction, location, n, limit);
                if (path != null)
                    return path;
            }

            return null;
        }

    }
}
