using PurrplingMod.AI.Controller;
using PurrplingMod.Utils;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingMod.AI
{
    /// <summary>
    /// State machine for companion AI
    /// </summary>
    public class AI_StateMachine : Internal.IUpdateable
    {
        public enum State
        {
            FOLLOW,
        }

        public readonly NPC npc;
        public readonly Character player;
        private Dictionary<State, IController> controllers;

        public AI_StateMachine(NPC npc, Character player)
        {
            this.npc = npc ?? throw new ArgumentNullException(nameof(npc));
            this.player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public State CurrentState { get; private set; }
        internal IController CurrentController { get => this.controllers[this.CurrentState]; }

        public event EventHandler<EventArgsLocationChanged> LocationChanged;

        /// <summary>
        /// Setup AI state machine
        /// </summary>
        public void Setup()
        {
            this.controllers = new Dictionary<State, IController>()
            {
                [State.FOLLOW] = new FollowController(this),
            };

            // By default AI following the player
            this.CurrentState = State.FOLLOW;
        }
        public void Update(UpdateTickedEventArgs e)
        {
            if (this.CurrentController != null)
                this.CurrentController.Update(e);
        }

        public void ChangeLocation(GameLocation l)
        {
            GameLocation previousLocation = this.npc.currentLocation;
            
            // Warp NPC to player's location at theirs position
            Helper.WarpTo(this.npc, l, this.player.getTileLocationPoint());

            // Fire location changed event
            this.OnLocationChanged(previousLocation, this.npc.currentLocation);
        }

        private void OnLocationChanged(GameLocation previous, GameLocation next)
        {
            EventArgsLocationChanged args = new EventArgsLocationChanged()
            {
                PreviousLocation = previous,
                CurrentLocation = next,
            };

            this.LocationChanged?.Invoke(this, args);
        }

        public void Dispose()
        {
            this.controllers.Clear();
            this.controllers = null;
        }
    }
}
