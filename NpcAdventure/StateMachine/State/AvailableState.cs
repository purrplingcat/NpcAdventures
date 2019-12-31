using NpcAdventure.Utils;
using NpcAdventure.StateMachine.StateFeatures;
using StardewModdingAPI.Events;
using StardewValley;
using StardewModdingAPI;

namespace NpcAdventure.StateMachine.State
{
    internal class AvailableState : CompanionState, IRequestedDialogueCreator, IDialogueDetector
    {
        private Dialogue acceptalDialogue;
        private Dialogue rejectionDialogue;
        private bool recruitRequestsEnabled;

        public bool CanCreateDialogue { get => this.recruitRequestsEnabled && this.StateMachine.CompanionManager.CanRecruit(); }

        public AvailableState(CompanionStateMachine stateMachine, IModEvents events, IMonitor monitor) : base(stateMachine, events, monitor) {}

        public override void Entry()
        {
            this.recruitRequestsEnabled = true;
        }

        public override void Exit()
        {
            this.recruitRequestsEnabled = false;
            this.acceptalDialogue = null;
            this.rejectionDialogue = null;
        }

        private void ReactOnAnswer(NPC n, Farmer leader)
        {
            if (leader.getFriendshipHeartLevelForNPC(n.Name) < this.StateMachine.CompanionManager.Config.HeartThreshold || Game1.timeOfDay >= 2200)
            {
                Dialogue rejectionDialogue = new Dialogue(
                    DialogueHelper.GetSpecificDialogueText(
                        n, leader, Game1.timeOfDay >= 2200 ? "companionRejectedNight" : "companionRejected"), n);

                this.rejectionDialogue = rejectionDialogue;
                DialogueHelper.DrawDialogue(rejectionDialogue);
            }
            else
            {
                Dialogue acceptalDialogue = new Dialogue(DialogueHelper.GetSpecificDialogueText(n, leader, "companionAccepted"), n);

                this.acceptalDialogue = acceptalDialogue;
                DialogueHelper.DrawDialogue(acceptalDialogue);
            }
        }

        public void CreateRequestedDialogue()
        {
            Farmer leader = this.StateMachine.CompanionManager.Farmer;
            NPC companion = this.StateMachine.Companion;
            GameLocation location = this.StateMachine.CompanionManager.Farmer.currentLocation;
            string question = this.StateMachine.ContentLoader.LoadString("Strings/Strings:askToFollow", companion.displayName);

            location.createQuestionDialogue(question, location.createYesNoResponses(), (_, answer) =>
            {
                if (answer == "Yes")
                {
                    if (!this.StateMachine.Companion.doingEndOfRouteAnimation.Value)
                    {
                        this.StateMachine.Companion.Halt();
                        this.StateMachine.Companion.facePlayer(leader);
                    }
                    this.ReactOnAnswer(this.StateMachine.Companion, leader);
                }
            }, null);
        }

        public void OnDialogueSpeaked(Dialogue speakedDialogue)
        {
            if (speakedDialogue == this.acceptalDialogue)
            {
                this.StateMachine.CompanionManager.Farmer.changeFriendship(40, this.StateMachine.Companion);
                this.StateMachine.Recruit();
            }
            else if (speakedDialogue == this.rejectionDialogue)
            {
                this.StateMachine.MakeUnavailable();
            }
        }
    }
}
