using NpcAdventure.StateMachine;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace NpcAdventure.Driver
{
    class EmergencySaveDriver
    {
        private const string EMERGENCY_SAVE_KEY = "emergencySavedChests";
        private readonly IDataHelper helper;
        private readonly IMonitor monitor;

        public EmergencySaveDriver(IDataHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;
        }
        public void EmergencySaveBags(List<CompanionStateMachine> companions)
        {
            var serializedBagMap = companions.ToDictionary(
                keySelector: csm => csm.Name,
                elementSelector: csm => SerializeChest(csm.Bag));

            this.helper.WriteSaveData(EMERGENCY_SAVE_KEY, serializedBagMap);
            this.monitor.Log($"Emergency saved chests for {companions.Count} companions");
        }

        public void EmergencyRestoreBags(List<CompanionStateMachine> companions)
        {
            var savedChests = this.helper.ReadSaveData<Dictionary<string, string>>(EMERGENCY_SAVE_KEY);
            int restoredCount = 0;

            if (savedChests == null)
                return;

            foreach(var csm in companions)
            {
                if (savedChests.TryGetValue(csm.Name, out string xmlAsString))
                {
                    DeserializeChest(xmlAsString, csm.Bag);
                    restoredCount++;
                }
            }

            this.monitor.Log($"Restored emergency saved chests for {restoredCount} companions");
        }

        private static string SerializeChest(Chest chest)
        {
            var settings = new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Auto,
                CloseOutput = true
            };
            var stringWriter = new StringWriter();
            var serializer = SaveGame.farmerSerializer;
            var fakeFarmer = new Farmer();

            fakeFarmer.maxItems.Value = 36;
            foreach (var item in chest.items.Where(itm => itm != null))
                fakeFarmer.Items.Add(item);

            using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
                serializer.Serialize(xmlWriter, fakeFarmer);

            return stringWriter.ToString();
        }

        private static void DeserializeChest(string xmlAsString, Chest chest)
        {
            var xmlSettings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Auto
            };
            var stringReader = new StringReader(xmlAsString);

            Farmer farmer;
            using (var reader = XmlReader.Create(stringReader, xmlSettings))
                farmer = (Farmer)SaveGame.farmerSerializer.Deserialize(reader);

            foreach (var item in farmer.Items.Where(itm => itm != null))
                chest.addItem(item);
        }
    }
}
