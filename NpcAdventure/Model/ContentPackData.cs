using StardewModdingAPI;

namespace NpcAdventure.Model
{
    internal class ContentPackData
    {
        public string Format { get; set; }
        public DataChanges[] Changes { get; set; }

        internal class DataChanges
        {
            public string Action { get; set; }
            public string Target { get; set; }
            public string FromFile { get; set; }
        }
    }
}