using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcAdventure.Compatibility
{
    /// <summary>
    /// Third party mod compatibility gateway
    /// </summary>
    internal class TPMC
    {
        public static TPMC Instance { get; private set; }

        public CustomKissing CustomKissing { get; }

        private TPMC(IModRegistry registry)
        {
            this.CustomKissing = new CustomKissing(registry);
        }

        public static void Setup(IModRegistry registry)
        {
            if (Instance == null)
            {
                Instance = new TPMC(registry);
            }
        }
    }
}
