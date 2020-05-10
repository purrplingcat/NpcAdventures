using NpcAdventure.Loader;
using NpcAdventure.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NpcAdventure.Dialogues
{
    /// <summary>
    /// Provides a companion dialogues for assigned NPC.
    /// Also provides static helper tools for work with static dialogues (like speech bubbles)
    /// </summary>
    public partial class DialogueProvider
    {
        public const char FLAG_RANDOM = '~';
        public const char FLAG_CHANCE = '^';
        private readonly NPC npc;
        private readonly IContentLoader contentLoader;
        private readonly string sourcePrefix;
        private Dictionary<string, string> dialogueCache;

        /// <summary>
        /// Create an instance of dialogue provider with assigned NPC and content loader
        /// </summary>
        /// <param name="npc">NPC for assign</param>
        /// <param name="contentLoader">Source of dialogues</param>
        public DialogueProvider(NPC npc, IContentLoader contentLoader, string sourcePrefix = "Dialogue/")
        {
            this.npc = npc;
            this.contentLoader = contentLoader;
            this.sourcePrefix = sourcePrefix;
            this.dialogueCache = new Dictionary<string, string>();
        }

        /// <summary>
        /// (Re)load dialogues from source to NPC's dialogue registry
        /// </summary>
        public void LoadDialogues()
        {
            this.dialogueCache = this.contentLoader.LoadStrings($"{this.sourcePrefix}{this.npc.Name}");

            SetupCompanionDialogues(this.npc, this.dialogueCache);
            Console.Write($"Dialogues for {this.npc.Name} loaded.");
        }

        private void ReloadIfMissing(string key)
        {
            if (this.dialogueCache.ContainsKey(key) && !this.npc.Dialogue.ContainsKey(key))
            {
                // Reaload and refresh NPC dialogues only when 
                // the key is missing in NPC dialogue registry but in pre-loaded cache this key exists.
                this.LoadDialogues();
            }
        }

        public static bool GetRawDialogue(Dictionary<string, string> dialogues, string key, out KeyValuePair<string, string> rawDialogue)
        {
            var keys = from _key in dialogues.Keys
                       where _key.StartsWith(key + FLAG_RANDOM) || _key.StartsWith(key + FLAG_CHANCE)
                       select _key;
            var randKeys = keys.Where((k) => k.Contains(FLAG_RANDOM)).ToList();
            var chanceKeys = keys.Where((k) => k.Contains(FLAG_CHANCE)).ToList();

            if (chanceKeys.Count > 0)
            {
                // Chance conditioned dialogue
                foreach (string k in chanceKeys)
                {
                    var s = k.Split(FLAG_CHANCE);
                    float chance = float.Parse(s[1]) / 100;
                    if (Game1.random.NextDouble() <= chance && dialogues.TryGetValue(k, out string chancedText))
                    {
                        rawDialogue = new KeyValuePair<string, string>(k, chancedText);
                        return true;
                    }
                }
            }

            if (randKeys.Count > 0)
            {
                // Randomized dialogue
                int i = Game1.random.Next(0, randKeys.Count() + 1);

                if (i < randKeys.Count() && dialogues.TryGetValue(randKeys[i], out string randomText))
                {
                    rawDialogue = new KeyValuePair<string, string>(randKeys[i], randomText);
                    return true;
                }
            }

            if (dialogues.TryGetValue(key, out string text))
            {
                // Standard dialogue
                rawDialogue = new KeyValuePair<string, string>(key, text);
                return true;
            }

            rawDialogue = new KeyValuePair<string, string>(key, key);

            return false;
        }

        public bool GetRawDialogue(string key, out KeyValuePair<string, string> rawDialogue, bool retryReload = true)
        {
            if (GetRawDialogue(this.npc.Dialogue, key, out rawDialogue))
                return true;

            if (retryReload)
            {
                // Dialogue not found? Companion dialogue list probably erased, reload it...
                this.ReloadIfMissing(key);

                // ...and try again to fetch dialogue
                if (GetRawDialogue(this.npc.Dialogue, key, out rawDialogue))
                    return true;
            }

            // Dialogue still can't be fetch? So we mark this dialogue as undefined and return dialogue key path as text
            rawDialogue = new KeyValuePair<string, string>(key, $"{this.npc.Name}.{key}");

            return false;
        }

        /// <summary>
        /// Returns a dialogue text for NPC as string.
        /// Can returns spouse dialogue, if famer are married with NPC and this dialogue is defined
        /// 
        /// Lookup dialogue key patterns: {key}_Spouse, {key}_Dating {key}
        /// </summary>
        /// <param name="n">NPC</param>
        /// <param name="f">Farmer</param>
        /// <param name="key">Dialogue key</param>
        /// <returns>A dialogue text</returns>
        public string GetFriendSpecificDialogueText(Farmer f, string key)
        {
            if (Helper.IsSpouseMarriedToFarmer(this.npc, f) && this.GetRawDialogue($"{key}_Spouse", out KeyValuePair<string, string> rawSpousedDialogue))
            {
                return rawSpousedDialogue.Value;
            }

            if (f.friendshipData.TryGetValue(this.npc.Name, out Friendship friendship)
                && friendship.IsDating()
                && this.GetRawDialogue($"{key}_Dating", out KeyValuePair<string, string> rawDatingDialogue))
            {
                return rawDatingDialogue.Value;
            }

            this.GetRawDialogue(key, out KeyValuePair<string, string> rawDialogue);
            return rawDialogue.Value;
        }

        public bool GetVariableRawDialogue(string key, out KeyValuePair<string, string> rawDialogue)
        {
            Farmer f = Game1.player;
            VariousKeyGenerator keygen = new VariousKeyGenerator()
            {
                Date = SDate.Now(),
                IsNight = Game1.isDarkOut(),
                FriendshipStatus = f.friendshipData.TryGetValue(this.npc.Name, out Friendship friendship) ? friendship.Status : FriendshipStatus.Friendly,
                FriendshipHeartLevel = f.getFriendshipHeartLevelForNPC(this.npc.Name),
                Weather = Helper.GetCurrentWeatherName(),
            };

            // Try to find a relevant dialogue
            if (!this.TryFetchVariableDialogue(key, keygen, out rawDialogue))
            {
                // No dialogue found? Companion dialogues are probably lost, reload them
                this.ReloadIfMissing(key);

                // And try dialogue fetch again. 
                // Returns false when dialogue really not exists and as text will be returned dialogue key path
                return this.TryFetchVariableDialogue(key, keygen, out rawDialogue);
            }

            return true;
        }

        private bool TryFetchVariableDialogue(string baseKey, VariousKeyGenerator keygen, out KeyValuePair<string, string> rawDialogue)
        {
            if (keygen.PossibleKeys.Count <= 0)
            {
                // Generate possible dialogue keys
                keygen.GenerateVariousKeys(baseKey);
            }

            foreach (string k in keygen.PossibleKeys)
                if (this.GetRawDialogue(k, out rawDialogue, false))
                    return true;

            rawDialogue = new KeyValuePair<string, string>(baseKey, $"{this.npc.Name}.{baseKey}");

            return false;
        }

        public bool GetVariableRawDialogue(string key, GameLocation l, out KeyValuePair<string, string> rawDialogue)
        {
            return this.GetVariableRawDialogue($"{key}_{l.Name}", out rawDialogue);
        }

        /// <summary>
        /// Returns a specific speech bubble for an NPC.
        /// 
        /// Definition pattern: `<type>_<npc>`
        /// </summary>
        /// <param name="bubbles"></param>
        /// <param name="n"></param>
        /// <param name="type"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static bool GetBubbleString(Dictionary<string, string> bubbles, NPC n, string type, out string text)
        {
            bool fullyfill = GetRawDialogue(bubbles, $"{type}_{n.Name}", out KeyValuePair<string, string> rawDialogue);

            text = string.Format(rawDialogue.Value, Game1.player?.Name, n.Name);
            
            return fullyfill;
        }

        /// <summary>
        /// Returns a location speech bubble for an NPC. This bubble definition must be prefixed with `ambient_`.
        /// 
        /// Whole definition pattern: `ambient_<location>_<npc>`
        /// </summary>
        /// <param name="bubbles"></param>
        /// <param name="n"></param>
        /// <param name="l"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static bool GetAmbientBubbleString(Dictionary<string, string> bubbles, NPC n, GameLocation l, out string text)
        {
            return GetBubbleString(bubbles, n, $"ambient_{l.Name}", out text);
        }

        /// <summary>
        /// Generate a variable dialogue
        /// </summary>
        /// <param name="n"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public Dialogue GenerateDialogue(string key)
        {
            if (this.GetVariableRawDialogue(key, out KeyValuePair<string, string> rawDilogue))
                return CreateDialogueFromRaw(this.npc, rawDilogue);

            return null;
        }

        /// <summary>
        /// Generate a variable dialogue for location
        /// </summary>
        /// <param name="n"></param>
        /// <param name="l"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public Dialogue GenerateDialogue(GameLocation l, string key)
        {
            if (this.GetVariableRawDialogue(key, l, out KeyValuePair<string, string> rawDialogue))
            {
                return CreateDialogueFromRaw(this.npc, rawDialogue);
            }

            return null;
        }

        /// <summary>
        /// Generate pure static dialogue
        /// </summary>
        /// <param name="n"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public Dialogue GenerateStaticDialogue(string key)
        {
            if (this.GetRawDialogue(key, out KeyValuePair<string, string> rawDialogue))
            {
                return CreateDialogueFromRaw(this.npc, rawDialogue);
            }

            return null;
        }

        /// <summary>
        /// Generate pure static dialogue for a location
        /// </summary>
        /// <param name="n"></param>
        /// <param name="l"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public Dialogue GenerateStaticDialogue(GameLocation l, string key)
        {
            return this.GenerateStaticDialogue($"{key}_{l.Name}");
        }

        public static void SetupCompanionDialogues(NPC n, Dictionary<string, string> dialogues)
        {
            foreach (var pair in dialogues)
                n.Dialogue[pair.Key] = pair.Value;
        }

        private static Dialogue CreateDialogueFromRaw(NPC n, KeyValuePair<string, string> rawDialogue)
        {
            var dialogue = CompanionDialogue.Create(rawDialogue.Value, n, rawDialogue.Key);

            if (rawDialogue.Key.Contains(FLAG_RANDOM))
                dialogue.SpecialAttributes.Add("randomized");

            if (rawDialogue.Key.Contains(FLAG_CHANCE))
                dialogue.SpecialAttributes.Add("possibly");

            return dialogue;
        }

        internal static void DrawDialogue(Dialogue dialogue)
        {
            NPC speaker = dialogue.speaker;

            speaker.CurrentDialogue.Push(dialogue);
            Game1.drawDialogue(speaker);
        }

        public static void RemoveDialogueFromStack(NPC n, Dialogue dialogue)
        {
            Stack<Dialogue> temp = new Stack<Dialogue>(n.CurrentDialogue.Count);

            while (n.CurrentDialogue.Count > 0)
            {
                Dialogue d = n.CurrentDialogue.Pop();

                if (!d.Equals(dialogue))
                    temp.Push(d);
            }

            while (temp.Count > 0)
                n.CurrentDialogue.Push(temp.Pop());
        }
    }
}
