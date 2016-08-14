#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Logic.Logging;
using PokemonGo.RocketAPI.Logic.Utils;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;

#endregion


namespace PokemonGo.RocketAPI.Console
{
    public class Settings : ISettings
    {
        [XmlIgnore]
        private readonly string _configsPath = Path.Combine(Directory.GetCurrentDirectory(), "Settings");

        public AuthType AuthType
        {
            get { return (AuthType)Enum.Parse(typeof(AuthType), UserSettings.Default.AuthType, true); }
            set { UserSettings.Default.AuthType = value.ToString(); }
        }

        public string PtcUsername
        {
            get { return UserSettings.Default.PtcUsername; }
            set { UserSettings.Default.PtcUsername = value; }
        }

        public string PtcPassword
        {
            get { return UserSettings.Default.PtcPassword; }
            set { UserSettings.Default.PtcPassword = value; }
        }

        public string GoogleEmail
        {
            get { return UserSettings.Default.GoogleEmail; }
            set { UserSettings.Default.GoogleEmail = value; }
        }

        public string GooglePassword
        {
            get { return UserSettings.Default.GooglePassword; }
            set { UserSettings.Default.GooglePassword = value; }
        }

        public bool UseProxy
        {
            get { return UserSettings.Default.UseProxy; }
            set { UserSettings.Default.UseProxy = value; }
        }

        public string ProxyAddress
        {
            get { return UserSettings.Default.ProxyAddress; }
            set { UserSettings.Default.ProxyAddress = value; }
        }

        public int ProxyPort
        {
            get { return UserSettings.Default.ProxyPort; }
            set { UserSettings.Default.ProxyPort = value; }
        }

        public string ProxyUsername
        {
            get { return UserSettings.Default.ProxyUsername; }
            set { UserSettings.Default.ProxyUsername = value; }
        }

        public string ProxyPassword
        {
            get { return UserSettings.Default.ProxyPassword; }
            set { UserSettings.Default.ProxyPassword = value; }
        }

        public double DefaultLatitude
        {
            get { return UserSettings.Default.DefaultLatitude; }
            set { UserSettings.Default.DefaultLatitude = value; }
        }

        public double DefaultLongitude
        {
            get { return UserSettings.Default.DefaultLongitude; }
            set { UserSettings.Default.DefaultLongitude = value; }
        }

        public double DefaultAltitude
        {
            get { return UserSettings.Default.DefaultAltitude; }
            set { UserSettings.Default.DefaultAltitude = value; }
        }

        public bool UseGPXPathing
        {
            get { return UserSettings.Default.UseGPXPathing; }
            set { UserSettings.Default.UseGPXPathing = value; }
        }

        public string GPXFile
        {
            get { return UserSettings.Default.GPXFile; }
            set { UserSettings.Default.GPXFile = value; }
        }

        public bool GPXIgnorePokestops
        {
            get { return UserSettings.Default.GPXIgnorePokestops; }
            set { UserSettings.Default.GPXIgnorePokestops = value; }
        }

        public double WalkingSpeedInKilometerPerHour
        {
            get { return UserSettings.Default.WalkingSpeedInKilometerPerHour; }
            set { UserSettings.Default.WalkingSpeedInKilometerPerHour = value; }
        }

        public int MaxTravelDistanceInMeters
        {
            get { return UserSettings.Default.MaxTravelDistanceInMeters; }
            set { UserSettings.Default.MaxTravelDistanceInMeters = value; }
        }

        public bool UseTeleportInsteadOfWalking
        {
            get { return UserSettings.Default.UseTeleportInsteadOfWalking; }
            set { UserSettings.Default.UseTeleportInsteadOfWalking = value; }
        }

        public bool UsePokemonToNotCatchList
        {
            get { return UserSettings.Default.UsePokemonToNotCatchList; }
            set { UserSettings.Default.UsePokemonToNotCatchList = value; }
        }

        public bool UsePokemonToNotTransferList
        {
            get { return UserSettings.Default.UsePokemonToNotTransferList; }
            set { UserSettings.Default.UsePokemonToNotTransferList = value; }
        }

        public bool UsePokemonToEvolveList
        {
            get { return UserSettings.Default.UsePokemonToEvolveList; }
            set { UserSettings.Default.UsePokemonToEvolveList = value; }
        }

        public bool CatchPokemon
        {
            get { return UserSettings.Default.CatchPokemon; }
            set { UserSettings.Default.CatchPokemon = value; }
        }

        public bool CatchIncensePokemon
        {
            get { return UserSettings.Default.CatchIncensePokemon; }
            set { UserSettings.Default.CatchIncensePokemon = value; }
        }

        public bool CatchLuredPokemon
        {
            get { return UserSettings.Default.CatchLuredPokemon; }
            set { UserSettings.Default.CatchLuredPokemon = value; }
        }

        public bool EvolvePokemon
        {
            get { return UserSettings.Default.EvolvePokemon; }
            set { UserSettings.Default.EvolvePokemon = value; }
        }

        public bool EvolveOnlyPokemonAboveIV
        {
            get { return UserSettings.Default.EvolveOnlyPokemonAboveIV; }
            set { UserSettings.Default.EvolveOnlyPokemonAboveIV = value; }
        }

        public float EvolveOnlyPokemonAboveIVValue
        {
            get { return UserSettings.Default.EvolveOnlyPokemonAboveIVValue; }
            set { UserSettings.Default.EvolveOnlyPokemonAboveIVValue = value; }
        }

        public int EvolveKeepCandiesValue
        {
            get { return UserSettings.Default.EvolveKeepCandiesValue; }
            set { UserSettings.Default.EvolveKeepCandiesValue = value; }
        }

        public bool TransferPokemon
        {
            get { return UserSettings.Default.TransferPokemon; }
            set { UserSettings.Default.TransferPokemon = value; }
        }

        public bool NotTransferPokemonsThatCanEvolve
        {
            get { return UserSettings.Default.NotTransferPokemonsThatCanEvolve; }
            set { UserSettings.Default.NotTransferPokemonsThatCanEvolve = value; }
        }

        public bool UseTransferPokemonKeepAllAboveCP
        {
            get { return UserSettings.Default.UseTransferPokemonKeepAllAboveCP; }
            set { UserSettings.Default.UseTransferPokemonKeepAllAboveCP = value; }
        }

        public int TransferPokemonKeepAllAboveCPValue
        {
            get { return UserSettings.Default.TransferPokemonKeepAllAboveCPValue; }
            set { UserSettings.Default.TransferPokemonKeepAllAboveCPValue = value; }
        }

        public bool UseTransferPokemonKeepAllAboveIV
        {
            get { return UserSettings.Default.UseTransferPokemonKeepAllAboveIV; }
            set { UserSettings.Default.UseTransferPokemonKeepAllAboveIV = value; }
        }

        public float TransferPokemonKeepAllAboveIVValue
        {
            get { return UserSettings.Default.TransferPokemonKeepAllAboveIVValue; }
            set { UserSettings.Default.TransferPokemonKeepAllAboveIVValue = value; }
        }

        public int TransferPokemonKeepAmountHighestCP
        {
            get { return UserSettings.Default.TransferPokemonKeepAmountHighestCP; }
            set { UserSettings.Default.TransferPokemonKeepAmountHighestCP = value; }
        }

        public int TransferPokemonKeepAmountHighestIV
        {
            get { return UserSettings.Default.TransferPokemonKeepAmountHighestIV; }
            set { UserSettings.Default.TransferPokemonKeepAmountHighestIV = value; }
        }

        public bool UseLuckyEggs
        {
            get { return UserSettings.Default.UseLuckyEggs; }
            set { UserSettings.Default.UseLuckyEggs = value; }
        }

        public bool HatchEggs
        {
            get { return UserSettings.Default.HatchEggs; }
            set { UserSettings.Default.HatchEggs = value; }
        }

        public bool UseOnlyBasicIncubator
        {
            get { return UserSettings.Default.UseOnlyBasicIncubator; }
            set { UserSettings.Default.UseOnlyBasicIncubator = value; }
        }

        public bool PrioritizeIVOverCP
        {
            get { return UserSettings.Default.PrioritizeIVOverCP; }
            set { UserSettings.Default.PrioritizeIVOverCP = value; }
        }

        public int ExportPokemonToCsvEveryMinutes
        {
            get { return UserSettings.Default.ExportPokemonToCsvEveryMinutes; }
            set { UserSettings.Default.ExportPokemonToCsvEveryMinutes = value; }
        }

        public bool DebugMode
        {
            get { return UserSettings.Default.DebugMode; }
            set { UserSettings.Default.DebugMode = value; }
        }

        public string DevicePackageName
        {
            get { return UserSettings.Default.DevicePackageName; }
            set { UserSettings.Default.DevicePackageName = value; }
        }

        [XmlIgnore]
        public string DeviceId = "8525f5d8201f78b5";
        [XmlIgnore]
        public string AndroidBoardName = "msm8996";
        [XmlIgnore]
        public string AndroidBootloader = "1.0.0.0000";
        [XmlIgnore]
        public string DeviceBrand = "HTC";
        [XmlIgnore]
        public string DeviceModel = "HTC 10";
        [XmlIgnore]
        public string DeviceModelIdentifier = "pmewl_00531";
        [XmlIgnore]
        public string DeviceModelBoot = "qcom";
        [XmlIgnore]
        public string HardwareManufacturer = "HTC";
        [XmlIgnore]
        public string HardwareModel = "HTC 10";
        [XmlIgnore]
        public string FirmwareBrand = "pmewl_00531";
        [XmlIgnore]
        public string FirmwareTags = "release - keys";
        [XmlIgnore]
        public string FirmwareType = "user";
        [XmlIgnore]
        public string FirmwareFingerprint = "htc/pmewl_00531/htc_pmewl:6.0.1/MMB29M/770927.1:user/release-keys";

        [XmlIgnore]
        private ICollection<PokemonId> _pokemonsToEvolve;

        [XmlIgnore]
        private ICollection<PokemonId> _pokemonsToNotTransfer;

        [XmlIgnore]
        private ICollection<PokemonId> _pokemonsToNotCatch;

        [XmlIgnore]
        private readonly SortedList<int, ItemId> _inventoryBalls = new SortedList<int, ItemId>();
        private readonly SortedList<int, ItemId> _inventoryBerries = new SortedList<int, ItemId>();
        private readonly SortedList<int, ItemId> _inventoryPotions = new SortedList<int, ItemId>();
        private const int MaxBalls = 210;
        private const int MaxBerries = 20;
        private const int MaxPotions = 20;
        public Settings()
        {
            _inventoryBalls.Add(1, ItemId.ItemMasterBall);
            _inventoryBalls.Add(2, ItemId.ItemUltraBall);
            _inventoryBalls.Add(3, ItemId.ItemGreatBall);
            _inventoryBalls.Add(4, ItemId.ItemPokeBall);

            _inventoryPotions.Add(1, ItemId.ItemMaxPotion);
            _inventoryPotions.Add(2, ItemId.ItemHyperPotion);
            _inventoryPotions.Add(3, ItemId.ItemSuperPotion);
            _inventoryPotions.Add(4, ItemId.ItemPotion);

            _inventoryBerries.Add(0, ItemId.ItemPinapBerry);
            _inventoryBerries.Add(1, ItemId.ItemWeparBerry);
            _inventoryBerries.Add(2, ItemId.ItemNanabBerry);
            _inventoryBerries.Add(3, ItemId.ItemBlukBerry);
            _inventoryBerries.Add(4, ItemId.ItemRazzBerry);
        }
        private IDictionary<ItemId, int> _itemRecycleFilter;
        public ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter(IEnumerable<ItemData> myItems)
        {
            if (_itemRecycleFilter == null)
            {
                _itemRecycleFilter = new Dictionary<ItemId, int>
                {
                    {ItemId.ItemUnknown, 0},
                    {ItemId.ItemRevive, 15},
                    {ItemId.ItemMaxRevive, 25},
                    {ItemId.ItemLuckyEgg, 200},
                    {ItemId.ItemIncenseOrdinary, 100},
                    {ItemId.ItemIncenseSpicy, 100},
                    {ItemId.ItemIncenseCool, 100},
                    {ItemId.ItemIncenseFloral, 100},
                    {ItemId.ItemTroyDisk, 100},
                    {ItemId.ItemXAttack, 100},
                    {ItemId.ItemXDefense, 100},
                    {ItemId.ItemXMiracle, 100},
                    {ItemId.ItemSpecialCamera, 100},
                    {ItemId.ItemIncubatorBasicUnlimited, 100},
                    {ItemId.ItemIncubatorBasic, 100},
                    {ItemId.ItemPokemonStorageUpgrade, 100},
                    {ItemId.ItemItemStorageUpgrade, 100}
                };
            }

            CalculateGroupAmounts(_inventoryBalls, MaxBalls, myItems);
            CalculateGroupAmounts(_inventoryBerries, MaxBerries, myItems);
            CalculateGroupAmounts(_inventoryPotions, MaxPotions, myItems);

            return _itemRecycleFilter;
        }
        private void CalculateGroupAmounts(SortedList<int, ItemId> inventoryGroup, int maxQty, IEnumerable<ItemData> myItems)
        {
            var amountRemaining = maxQty;
            var amountToKeep = 0;
            foreach (KeyValuePair<int, ItemId> listItem in inventoryGroup)
            {
                ItemId itemId = listItem.Value;
                int itemQty = 0;

                ItemData item = myItems.FirstOrDefault(x => x.ItemId == itemId);
                if (item != null)
                {
                    itemQty = myItems.FirstOrDefault(x => x.ItemId == itemId).Count;
                }

                amountToKeep = amountRemaining >= itemQty ? amountRemaining : Math.Min(itemQty, amountRemaining);

                amountRemaining = amountRemaining - itemQty;

                if (amountRemaining < 0)
                {
                    amountRemaining = 0;
                }

                try
                {
                    _itemRecycleFilter[itemId] = amountToKeep;  // Update the filter with amounts to keep
                }
                catch
                {
                    // ignored
                }
            }
        }

        [XmlIgnore]
        public ICollection<PokemonId> PokemonsToEvolve
        {
            get
            {
                //Type of pokemons to evolve
                var defaultPokemon = new List<PokemonId> {
                    PokemonId.Zubat, PokemonId.Pidgey, PokemonId.Rattata
                };
                _pokemonsToEvolve = _pokemonsToEvolve ?? LoadPokemonList("PokemonsToEvolve.ini", defaultPokemon);
                return _pokemonsToEvolve;
            }
        }

        [XmlIgnore]
        public ICollection<PokemonId> PokemonsToNotTransfer
        {
            get
            {
                //Type of pokemons not to transfer
                var defaultPokemon = new List<PokemonId> {
                    PokemonId.Dragonite, PokemonId.Charizard, PokemonId.Zapdos, PokemonId.Snorlax, PokemonId.Alakazam, PokemonId.Mew, PokemonId.Mewtwo
                };
                _pokemonsToNotTransfer = _pokemonsToNotTransfer ?? LoadPokemonList("PokemonsToNotTransfer.ini", defaultPokemon);
                return _pokemonsToNotTransfer;
            }
        }

        [XmlIgnore]
        public ICollection<PokemonId> PokemonsToNotCatch
        {
            get
            {
                //Type of pokemons not to catch
                var defaultPokemon = new List<PokemonId> {
                    PokemonId.Zubat, PokemonId.Pidgey, PokemonId.Rattata
                };
                _pokemonsToNotCatch = _pokemonsToNotCatch ?? LoadPokemonList("PokemonsToNotCatch.ini", defaultPokemon);
                return _pokemonsToNotCatch;
            }
        }

        private ICollection<PokemonId> LoadPokemonList(string filename, List<PokemonId> defaultPokemon)
        {
            ICollection<PokemonId> result = new List<PokemonId>();
            if (!Directory.Exists(_configsPath))
                Directory.CreateDirectory(_configsPath);
            var pokemonlistFile = Path.Combine(_configsPath, filename);
            if (!File.Exists(pokemonlistFile))
            {
                Logger.Write($"Settings File: \"{filename}\" not found, creating new...", LogLevel.Warning);
                using (var w = File.AppendText(pokemonlistFile))
                {
                    defaultPokemon.ForEach(pokemon => w.WriteLine(pokemon.ToString()));
                    defaultPokemon.ForEach(pokemon => result.Add(pokemon));
                    w.Close();
                }
            }

            if (File.Exists(pokemonlistFile))
            {
                Logger.Write($"Loading Settings File: \"{filename}\"", LogLevel.Info);

                string content;
                using (var reader = new StreamReader(pokemonlistFile))
                {
                    content = reader.ReadToEnd();
                    reader.Close();
                }
                content = Regex.Replace(content, @"\\/\*(.|\n)*?\*\/", ""); //todo: supposed to remove comment blocks

                var tr = new StringReader(content);

                var pokemonName = tr.ReadLine();
                while (pokemonName != null)
                {
                    PokemonId pokemon;
                    if (Enum.TryParse(pokemonName, out pokemon))
                    {
                        result.Add(pokemon);
                    }
                    pokemonName = tr.ReadLine();
                }
            }
            return result;
        }
    }
}
