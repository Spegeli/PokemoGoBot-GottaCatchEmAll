#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Logic.Logging;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;

#endregion


namespace PokemonGo.RocketAPI.Console
{
    public class Settings : ISettings
    {
        public readonly string ConfigsPath = Path.Combine(Directory.GetCurrentDirectory(), "Settings");

        public AuthType AuthType => (AuthType)Enum.Parse(typeof(AuthType), UserSettings.Default.AuthType, true);
        public string PtcUsername => UserSettings.Default.PtcUsername;
        public string PtcPassword => UserSettings.Default.PtcPassword;
        public string GoogleEmail => UserSettings.Default.GoogleEmail;
        public string GooglePassword => UserSettings.Default.GooglePassword;
        public double DefaultLatitude => UserSettings.Default.DefaultLatitude;
        public double DefaultLongitude => UserSettings.Default.DefaultLongitude;
        public double DefaultAltitude => UserSettings.Default.DefaultAltitude;
        public bool UseGPXPathing => UserSettings.Default.UseGPXPathing;
        public string GPXFile => UserSettings.Default.GPXFile;
        public bool GPXIgnorePokestops => UserSettings.Default.GPXIgnorePokestops;

        public double WalkingSpeedInKilometerPerHour => UserSettings.Default.WalkingSpeedInKilometerPerHour;
        public int MaxTravelDistanceInMeters => UserSettings.Default.MaxTravelDistanceInMeters;
        public bool UseTeleportInsteadOfWalking => UserSettings.Default.UseTeleportInsteadOfWalking;

        public bool UsePokemonToNotCatchList => UserSettings.Default.UsePokemonToNotCatchList;
        public bool UsePokemonToNotTransferList => UserSettings.Default.UsePokemonToNotTransferList;
        public bool UsePokemonToEvolveList => UserSettings.Default.UsePokemonToEvolveList;

        public bool CatchPokemon => UserSettings.Default.CatchPokemon;
        public bool CatchIncensePokemon => UserSettings.Default.CatchIncensePokemon;
        public bool CatchLuredPokemon => UserSettings.Default.CatchLuredPokemon;
        public bool EvolvePokemon => UserSettings.Default.EvolvePokemon;
        public bool EvolveOnlyPokemonAboveIV => UserSettings.Default.EvolveOnlyPokemonAboveIV;
        public float EvolveOnlyPokemonAboveIVValue => UserSettings.Default.EvolveOnlyPokemonAboveIVValue;
        public int EvolveKeepCandiesValue => UserSettings.Default.EvolveKeepCandiesValue;

        public bool TransferPokemon => UserSettings.Default.TransferPokemon;
        public bool NotTransferPokemonsThatCanEvolve => UserSettings.Default.NotTransferPokemonsThatCanEvolve;
        public bool UseTransferPokemonKeepAllAboveCP => UserSettings.Default.UseTransferPokemonKeepAllAboveCP;
        public int TransferPokemonKeepAllAboveCP => UserSettings.Default.TransferPokemonKeepAllAboveCP;
        public bool UseTransferPokemonKeepAllAboveIV => UserSettings.Default.UseTransferPokemonKeepAllAboveIV;
        public float TransferPokemonKeepAllAboveIV => UserSettings.Default.TransferPokemonKeepAllAboveIV;
        public int TransferPokemonKeepAmountHighestCP => UserSettings.Default.TransferPokemonKeepAmountHighestCP;
        public int TransferPokemonKeepAmountHighestIV => UserSettings.Default.TransferPokemonKeepAmountHighestIV;

        public bool UseLuckyEggs => UserSettings.Default.UseLuckyEggs;
        public bool IncubateEggs => UserSettings.Default.IncubateEggs;
        public bool UseOnlyBasicIncubator => UserSettings.Default.UseOnlyBasicIncubator;
        public bool PrioritizeIVOverCP => UserSettings.Default.PrioritizeIVOverCP;
        public bool DebugMode => UserSettings.Default.DebugMode;

        private ICollection<PokemonId> _pokemonsToEvolve;
        private ICollection<PokemonId> _pokemonsToNotTransfer;
        private ICollection<PokemonId> _pokemonsToNotCatch;

        // Create our group of inventory items
        private readonly SortedList<int, ItemId> _inventoryBalls = new SortedList<int, ItemId>();
        private readonly SortedList<int, ItemId> _inventoryBerries = new SortedList<int, ItemId>();
        private readonly SortedList<int, ItemId> _inventoryPotions = new SortedList<int, ItemId>();

        //TODO: make these configurable settings
        // Set our maximum value for all items in this group
        private const int MaxBalls = 100;
        private const int MaxBerries = 20;
        private const int MaxPotions = 30;

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

                    // These will be overwritten by the CalculateGroupAmounts calculations below
                    /*
                    {ItemId.ItemPokeBall, 25},
                    {ItemId.ItemGreatBall, 50},
                    {ItemId.ItemUltraBall, 75},
                    {ItemId.ItemMasterBall, 100},
                    {ItemId.ItemPotion, 0},
                    {ItemId.ItemSuperPotion, 10},
                    {ItemId.ItemHyperPotion, 25},
                    {ItemId.ItemMaxPotion, 25},
                    {ItemId.ItemRazzBerry, 20},
                    {ItemId.ItemBlukBerry, 10},
                    {ItemId.ItemNanabBerry, 10},
                    {ItemId.ItemWeparBerry, 30},
                    {ItemId.ItemPinapBerry, 30},
                    */

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

            // Calculate how many balls of each type we should keep
            CalculateGroupAmounts(_inventoryBalls, MaxBalls, myItems);

            // Calculate how many berries of each type we should keep
            CalculateGroupAmounts(_inventoryBerries, MaxBerries, myItems);

            // Calculate how many potions of each type we should keep
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

        public ICollection<PokemonId> PokemonsToNotTransfer
        {
            get
            {
                //Type of pokemons not to transfer
                var defaultPokemon = new List<PokemonId> {
                    PokemonId.Farfetchd, PokemonId.Kangaskhan, PokemonId.Tauros, PokemonId.MrMime , PokemonId.Dragonite, PokemonId.Charizard, PokemonId.Zapdos, PokemonId.Snorlax, PokemonId.Alakazam, PokemonId.Mew, PokemonId.Mewtwo
                };
                _pokemonsToNotTransfer = _pokemonsToNotTransfer ?? LoadPokemonList("PokemonsToNotTransfer.ini", defaultPokemon);
                return _pokemonsToNotTransfer;
            }
        }

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
            if (!Directory.Exists(ConfigsPath))
                Directory.CreateDirectory(ConfigsPath);
            var pokemonlistFile = Path.Combine(ConfigsPath, filename);
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
