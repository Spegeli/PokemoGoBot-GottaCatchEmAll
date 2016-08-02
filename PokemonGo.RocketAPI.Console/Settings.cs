#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.GeneratedCode;
using PokemonGo.RocketAPI.Logging;
using System.Xml;


#endregion


namespace PokemonGo.RocketAPI.Console
{
    public class Settings : ISettings
    {
        private ICollection<PokemonId> _pokemonsToEvolve = new LinkedList<PokemonId>();
        private ICollection<PokemonId> _pokemonsToNotTransfer = new LinkedList<PokemonId>();
        private ICollection<PokemonId> _pokemonsToNotCatch = new LinkedList<PokemonId>();
        private ICollection<KeyValuePair<ItemId, int>> itemRecycleFilter = new Dictionary<ItemId, int>();
        
        public AuthType AuthType => PtcUsername == "" ? AuthType.Google : AuthType.Ptc;

        public string PtcUsername => GetConfigString("PtcUsername");
        public string PtcPassword => GetConfigString("PtcPassword");
        public double DefaultLatitude => GetConfigDouble("DefaultLatitude");
        public double DefaultLongitude => GetConfigDouble("DefaultLongitude");
        public double DefaultAltitude => GetConfigDouble("DefaultAltitude");
        public bool UseGPXPathing => GetConfigBool("UseGPXPathing");
        public string GPXFile => GetConfigString("GPXFile");
        public bool GPXIgnorePokestops => GetConfigBool("GPXIgnorePokestops");
        public bool GPXIgnorePokemon => GetConfigBool("GPXIgnorePokemon");

        public double WalkingSpeedInKilometerPerHour => GetConfigDouble("WalkingSpeedInKilometerPerHour");
        public int MaxTravelDistanceInMeters => GetConfigInt("MaxTravelDistanceInMeters");

        public bool UsePokemonToNotCatchList => GetConfigBool("UsePokemonToNotCatchList");
        public bool UsePokemonToNotTransferList => GetConfigBool("UsePokemonToNotTransferList");
        public bool EvolvePokemon => GetConfigBool("EvolvePokemon");
        public bool EvolveOnlyPokemonAboveIV => GetConfigBool("EvolveOnlyPokemonAboveIV");
        public float EvolveOnlyPokemonAboveIVValue => GetConfigFloat("EvolveOnlyPokemonAboveIVValue");
        public bool TransferPokemon => GetConfigBool("TransferPokemon");
        public int TransferPokemonKeepDuplicateAmount => GetConfigInt("TransferPokemonKeepDuplicateAmount");
        public bool NotTransferPokemonsThatCanEvolve => GetConfigBool("NotTransferPokemonsThatCanEvolve");
        public bool UseTransferPokemonKeepAboveCP => GetConfigBool("UseTransferPokemonKeepAboveCP");
        public int TransferPokemonKeepAboveCP => GetConfigInt("TransferPokemonKeepAboveCP");
        public bool UseTransferPokemonKeepAboveIV => GetConfigBool("UseTransferPokemonKeepAboveIV");
        public float TransferPokemonKeepAboveIVPercentage => GetConfigFloat("TransferPokemonKeepAboveIVPercentage");

        public bool UseLuckyEggs => GetConfigBool("UseLuckyEggs");
        public bool UseIncense => GetConfigBool("UseIncense");
        public bool PrioritizeIVOverCP => GetConfigBool("PrioritizeIVOverCP");

        public bool UseTeleportInsteadOfWalking => GetConfigBool("UseTeleportInsteadOfWalking");
        public int EvolveKeepCandiesValue => GetConfigInt("EvolveKeepCandiesValue");
        public bool DebugMode => GetConfigBool("DebugMode");

        public ICollection<PokemonId> PokemonsToEvolve => _pokemonsToEvolve;
        public ICollection<PokemonId> PokemonsToNotTransfer => _pokemonsToNotTransfer;
        public ICollection<PokemonId> PokemonsToNotCatch => _pokemonsToNotCatch;
        public ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter => itemRecycleFilter;

        public Settings()
        {
            LoadSettings();
        }

        

        /// <summary>
        /// Load the config.xml file
        /// </summary>
        public void LoadSettings()
        {
            if (defConfigRoot == null)
            {
                defConfigXml.LoadXml(PokemonGo.RocketAPI.Console.Properties.Resources.DefaultSettings);
                defConfigRoot = defConfigXml.SelectSingleNode("root");
            }



            if (File.Exists(configFile))
            {
                configXml.Load(configFile);
                configRoot = configXml.SelectSingleNode("root");
                Logger.Write("Existing config file loaded", LogLevel.Info);
            }
            else
            {
                configXml.LoadXml(PokemonGo.RocketAPI.Console.Properties.Resources.DefaultSettings);
                Logger.Write("Creating new config file", LogLevel.Info);
                configRoot = configXml.SelectSingleNode("root");
                configXml.Save(configFile);
            }

            

            if (File.Exists(savedDataFile))
            {
                savedDataXml.Load(savedDataFile);
                savedDataRoot = savedDataXml.SelectSingleNode("root");
            }

            LoadItemRecycleList();
            LoadPokemonList("PokemonsToEvolve", _pokemonsToEvolve);
            LoadPokemonList("PokemonsToNotTransfer", _pokemonsToNotTransfer);
            LoadPokemonList("PokemonsToNotCatch", _pokemonsToNotCatch);
        }

        #region SavedData
        string savedDataFile = Path.Combine(Directory.GetCurrentDirectory(), "LastData.xml");
        XmlDocument savedDataXml = new XmlDocument();
        XmlNode savedDataRoot;

        /// <summary>
        /// Save some data to savedData.xml file
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetSavedData(string name, string value)
        {
            if (savedDataRoot == null)
            {
                savedDataRoot = savedDataXml.CreateElement("root");
                savedDataRoot.InnerText = "";
                savedDataXml.AppendChild(savedDataRoot);
            }

            XmlNode node = savedDataRoot.SelectSingleNode(name);
            if (node == null)
            {
                savedDataRoot.AppendChild(savedDataXml.CreateElement(name)).InnerText = value;
            }
            else
            {
                node.InnerText = value;
            }

            savedDataXml.Save(savedDataFile);
        }

        /// <summary>
        /// Retrieve some data to savedData.xml file
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string GetSavedData(string name, string value = "")
        {
            if (savedDataRoot == null)
                return value;

            XmlNode node = savedDataRoot.SelectSingleNode(name);
            if (node == null)
            {
                return value;
            }
            else
            {
                return node.InnerText;
            }
        }
        #endregion

        #region Config
        string configFile = Path.Combine(Directory.GetCurrentDirectory(), "config.xml");
        XmlDocument configXml = new XmlDocument();
        XmlNode configRoot;

        XmlDocument defConfigXml = new XmlDocument();
        XmlNode defConfigRoot = null;


        string GetConfigString(string name)
        {
            if (configRoot == null)
            {
                configRoot = configXml.CreateElement("root");
                configRoot.InnerText = "";
                configXml.AppendChild(configRoot);
            }

            XmlNode node = configRoot.SelectSingleNode(name);
            if (node == null)
            {
                XmlNode defNode = defConfigRoot.SelectSingleNode(name);
                if (defNode == null)
                {
                    //Err
                    return null;
                }
                else
                {
                    string defValue = defNode.InnerText;
                    configRoot.AppendChild(configXml.CreateElement(name)).InnerText = defValue;
                    configXml.Save(configFile);
                    return defValue;
                }
            }
            else
            {
                return node.InnerText;
            }
        }

        int GetConfigInt(string name)
        {
            return int.Parse(GetConfigString(name));
        }

        double GetConfigDouble(string name)
        {
            return double.Parse(GetConfigString(name));
        }

        float GetConfigFloat(string name) { 
            return float.Parse(GetConfigString(name));
        }

        bool GetConfigBool(string name)
        {
            return bool.Parse(GetConfigString(name));
        }

        #endregion

        #region List
        public void LoadItemRecycleList()
        {
            string rawData = GetConfigString("ItemRecycleList");

            string[] ContentLines = rawData.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            itemRecycleFilter.Clear();
            foreach (string line in ContentLines)
            {
                string[] splitted = line.Replace(" ", "").Split('=');

                if (splitted.Length == 2)
                {
                    ItemId itemID = (ItemId)(Enum.Parse(typeof(ItemId), splitted[0]));
                    int count = int.Parse(splitted[1]);
                    itemRecycleFilter.Add(new KeyValuePair<ItemId, int>(itemID, count));
                }
            }

            Logger.Write($"Loading ttem recycle list...", LogLevel.Info);
        }

        private void LoadPokemonList(string listName, ICollection<PokemonId> result)
        {
            result.Clear();

            string rawData = GetConfigString(listName);

            Logger.Write($"Loading settings list: \"{listName}\"", LogLevel.Info);

            var content = Regex.Replace(rawData, @"\\/\*(.|\n)*?\*\/", ""); //todo: supposed to remove comment blocks

            StringReader tr = new StringReader(content);

            var pokemonName = tr.ReadLine();
            while (pokemonName != null)
            {
                PokemonId pokemon;
                if (Enum.TryParse<PokemonId>(pokemonName, out pokemon))
                {
                    result.Add((PokemonId)pokemon);
                }
                pokemonName = tr.ReadLine();
            }
        }
        #endregion
    }
}
