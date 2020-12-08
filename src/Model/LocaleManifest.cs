using StardewModdingAPI;

namespace NpcAdventure.Model
{
    internal class LocaleManifest
    {
        public string Language { get; set; }
        public string Code { get; set; }
        public ISemanticVersion Version { get; set; }
        public string Translator { get; set; }
    }
}
