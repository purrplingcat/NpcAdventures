﻿using Microsoft.Xna.Framework.Graphics;
using NpcAdventure.AI.Controller;
using NpcAdventure.HUD;
using NpcAdventure.Loader;
using NpcAdventure.Model;
using NpcAdventure.NetCode;
using NpcAdventure.StateMachine;
using NpcAdventure.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NpcAdventure.NetCode.NetEvents;

namespace NpcAdventure.AI
{
    /// <summary>
    /// State machine for companion AI
    /// </summary>
    internal partial class AI_StateMachine : Internal.IUpdateable, Internal.IDrawable
    {
        public enum State
        {
            FOLLOW,
            FIGHT,
            IDLE,
        }

        private const float MONSTER_DISTANCE = 9f;
        public NPC npc { get => this.csm.Companion; }
        public readonly Farmer player;
        private readonly CompanionDisplay hud;
        private readonly IModEvents events;
        internal IMonitor Monitor { get; private set; }

        private NetEvents netEvents;

        private readonly IContentLoader loader;
        private Dictionary<State, IController> controllers;
        private int changeStateCooldown = 0;

        private CompanionStateMachine csm;

        internal AI_StateMachine(CompanionStateMachine csm, Farmer leader, CompanionDisplay hud, IModEvents events, IMonitor monitor, NetEvents netEvents)
        {
            this.csm = csm;
            this.player = leader;
            this.events = events ?? throw new ArgumentException(nameof(events));
            this.Monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            this.Csm = csm;
            this.hud = hud;
            this.loader = csm.ContentLoader;

            this.netEvents = netEvents;
        }

        public State CurrentState { get; private set; }
        internal IController CurrentController { get => this.controllers[this.CurrentState]; }

        internal CompanionStateMachine Csm { get; }

        public event EventHandler<EventArgsLocationChanged> LocationChanged;

        /// <summary>
        /// Setup AI state machine
        /// </summary>
        public void Setup()
        {
            this.controllers = new Dictionary<State, IController>()
            {
                [State.FOLLOW] = new FollowController(this),
                [State.FIGHT] = new FightController(this, this.loader, this.events, this.Csm.Metadata.Sword, this.netEvents),
                [State.IDLE] = new IdleController(this, this.loader),
            };

            // By default AI following the player
            this.ChangeState(State.FOLLOW);

            this.events.GameLoop.TimeChanged += this.GameLoop_TimeChanged;
        }

        private void GameLoop_TimeChanged(object sender, TimeChangedEventArgs e)
        {
            this.lifeSaved = false;
        }

        public bool PerformAction()
        {
            if (this.Csm.HasSkill("doctor") && (this.player.health < this.player.maxHealth / 3) && this.healCooldown == 0 && this.medkits != -1)
            {
                this.TryHealFarmer();
                return true;
            }

            return false;
        }

        private void ChangeState(State state)
        {
            this.Monitor.Log($"AI changes state request {this.CurrentState} -> {state}");

            if (Context.IsMainPlayer)
            {
                // if we're main player we're going to dispatch the network update to everybody
                this.netEvents.FireEvent(new AIChangeState(this.npc, state), null, true);
            }
        }

        public void ChangeStateLocal(State state)
        {
            this.Monitor.Log($"AI change state activated {this.CurrentState} -> {state}");
            if (this.CurrentController != null)
            {
                this.CurrentController.Deactivate();
            }

            this.CurrentState = state;
            this.CurrentController.Activate();
            this.hud.SetCompanionState(state);
        }

        private bool IsThereAnyMonster()
        {
            return Helper.GetNearestMonsterToCharacter(this.npc, MONSTER_DISTANCE) != null;
        }

        private bool PlayerIsNear()
        {
            return Helper.Distance(this.player.getTileLocationPoint(), this.npc.getTileLocationPoint()) < 11f;
        }

        private void CheckPotentialStateChange()
        {
            if (!Context.IsMainPlayer)
                return;

            if (this.Csm.HasSkillsAny("fighter", "warrior") && this.changeStateCooldown == 0 && this.CurrentState != State.FIGHT && this.PlayerIsNear() && this.IsThereAnyMonster())
            {
                this.ChangeState(State.FIGHT);
                this.Monitor.Log("A 50ft monster is here!");
            }

            if (this.CurrentState != State.FOLLOW && this.CurrentController.IsIdle)
            {
                this.changeStateCooldown = 100;
                this.ChangeState(State.FOLLOW);
            }

            if (this.CurrentState == State.FOLLOW && this.CurrentController.IsIdle)
            {
                this.ChangeState(State.IDLE);
            }
        }

        public void Update(UpdateTickedEventArgs e)
        {
            if (e.IsMultipleOf(15))
            {
                this.CheckPotentialStateChange();
            }

            if (this.changeStateCooldown > 0)
                this.changeStateCooldown--;

            if (this.Csm.HasSkill("doctor"))
                this.UpdateDoctor(e);

            if (this.CurrentController != null)
                this.CurrentController.Update(e);
        }

        public void ChangeLocation(GameLocation l)
        {
            GameLocation previousLocation = this.npc.currentLocation;
            
            // Warp NPC to player's location at theirs position
            Helper.WarpTo(this.npc, l, this.player.getTileLocationPoint());

            this.changeStateCooldown = 30;

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
            this.events.GameLoop.TimeChanged -= this.GameLoop_TimeChanged;
            this.CurrentController.Deactivate();
            this.controllers.Clear();
            this.controllers = null;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (Context.IsWorldReady && this.CurrentController is Internal.IDrawable drawableController)
            {
                drawableController.Draw(spriteBatch);
            }
        }
    }
}
