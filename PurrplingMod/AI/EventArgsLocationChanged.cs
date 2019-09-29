using StardewValley;
using System;

namespace PurrplingMod.AI
{
    public class EventArgsLocationChanged : EventArgs
    {
        public GameLocation PreviousLocation { get; set; }
        public GameLocation CurrentLocation { get; set; }
    }
}