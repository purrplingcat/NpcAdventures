﻿using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;

namespace NpcAdventure.Dialogues
{
    internal class VariousKeyGenerator
    {
        private int friendship;

        public List<string> PossibleKeys { get; private set; } = new List<string>();
        public int FriendshipHeartLevel
        {
            get => this.friendship;
            set
            {
                List<int> scale = new List<int>();
                if (value >= 12)
                    scale.Add(12);
                if (value >= 10)
                    scale.Add(10);
                if (value >= 8)
                    scale.Add(8);
                if (value >= 6)
                    scale.Add(6);
                if (value >= 4)
                    scale.Add(4);
                if (value >= 2)
                    scale.Add(2);

                this.FriendshipHeartScale = scale.ToArray();
                this.friendship = value;
            }
        }
        public int[] FriendshipHeartScale { get; private set; }
        public SDate Date { get; set; } = SDate.Now();
        public bool IsNight { get; set; } = false;
        public FriendshipStatus FriendshipStatus { get; set; } = FriendshipStatus.Friendly;
        public string Weather { get; set; }

        private void Enhance(string key)
        {
            foreach (int friendship in this.FriendshipHeartScale)
                this.PossibleKeys.Add(key + friendship);
            this.PossibleKeys.Add(key);
        }

        private string[] CreateConditionbasedVariants()
        {
            List<string> variants = new List<string>();
            Tuple<bool, string>[] conditions =
            {
                    new Tuple<bool, string>(this.IsNight && this.FriendshipStatus == FriendshipStatus.Married && this.Weather != null, "_Night_" + this.Weather + "_Spouse"),
                    new Tuple<bool, string>(this.Weather != null && this.FriendshipStatus == FriendshipStatus.Married, "_" + this.Weather + "_Spouse"),
                    new Tuple<bool, string>(this.FriendshipStatus == FriendshipStatus.Married, "_Spouse"),
                    new Tuple<bool, string>(this.IsNight && this.FriendshipStatus == FriendshipStatus.Dating && this.Weather != null, "_Night_" + this.Weather + "_Dating"),
                    new Tuple<bool, string>(this.Weather != null && this.FriendshipStatus == FriendshipStatus.Dating, "_" + this.Weather + "_Dating"),
                    new Tuple<bool, string>(this.FriendshipStatus == FriendshipStatus.Dating, "_Dating"),
                    new Tuple<bool, string>(this.IsNight && this.Weather != null, "_Night_" + this.Weather),
                    new Tuple<bool, string>(this.IsNight, "_Night"),
                    new Tuple<bool, string>(this.Weather != null, "_" + this.Weather),
                };

            foreach (var conditioned in conditions)
            {
                if (conditioned.Item1)
                    variants.Add(conditioned.Item2);
            }

            return variants.ToArray();
        }

        private string[] CreateTimebasedVariants()
        {
            return new string[]
            {
                    "_" + this.Date.Season + "_" + this.Date.Day, // *_spring_14
                    "_" + this.Date.Season + "_" + this.Date.DayOfWeek, // *_spring_Monday
                    "_" + this.Date.DayOfWeek, // *_Monday
                    "_" + this.Date.Season, // *_spring
            };
        }

        public void GenerateVariousKeys()
        {
            string[] conditionbasedVariants = this.CreateConditionbasedVariants();
            string[] timebasedVariants = this.CreateTimebasedVariants();

            // <key>_<time>_<cond>[<friendship>]
            foreach (string timebased in timebasedVariants)
                foreach (string conditioned in conditionbasedVariants)
                    this.Enhance(timebased + conditioned);

            // <key>_<cond>[<friendship>]
            foreach (string conditioned in conditionbasedVariants)
                this.Enhance(conditioned);

            // <key>_<time>[<friendship>]
            foreach (string timebased in timebasedVariants)
                this.Enhance(timebased);

            // <key>[<friendship>]
            this.Enhance("");
        }
    }
}
