#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Logging;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using System.Collections.ObjectModel;
using System.Linq;

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

        public bool EvolvePokemon => UserSettings.Default.EvolvePokemon;
        public bool EvolveOnlyPokemonAboveIV => UserSettings.Default.EvolveOnlyPokemonAboveIV;
        public float EvolveOnlyPokemonAboveIVValue => UserSettings.Default.EvolveOnlyPokemonAboveIVValue;
        public int EvolveKeepCandiesValue => UserSettings.Default.EvolveKeepCandiesValue;

        public bool TransferPokemon => UserSettings.Default.TransferPokemon;
        public int TransferPokemonKeepDuplicateAmount => UserSettings.Default.TransferPokemonKeepDuplicateAmount;
        public bool NotTransferPokemonsThatCanEvolve => UserSettings.Default.NotTransferPokemonsThatCanEvolve;
        public bool UseTransferPokemonKeepAboveCP => UserSettings.Default.UseTransferPokemonKeepAboveCP;
        public int TransferPokemonKeepAboveCP => UserSettings.Default.TransferPokemonKeepAboveCP;
        public bool UseTransferPokemonKeepAboveIV => UserSettings.Default.UseTransferPokemonKeepAboveIV;
        public float TransferPokemonKeepAboveIVPercentage => UserSettings.Default.TransferPokemonKeepAboveIVPercentage;

        public bool UseLuckyEggs => UserSettings.Default.UseLuckyEggs;
        public bool UseIncense => UserSettings.Default.UseIncense;
        public bool PrioritizeIVOverCP => UserSettings.Default.PrioritizeIVOverCP;
        public bool DebugMode => UserSettings.Default.DebugMode;

        private ICollection<PokemonId> _pokemonsToEvolve;
        private ICollection<PokemonId> _pokemonsToNotTransfer;
        private ICollection<PokemonId> _pokemonsToNotCatch;

        // Create our group of inventory items
        private SortedList<int, ItemId> inventoryBalls = new SortedList<int, ItemId>();
        private SortedList<int, ItemId> inventoryBerries = new SortedList<int, ItemId>();
        private SortedList<int, ItemId> inventoryPotions = new SortedList<int, ItemId>();

        //TODO: make these configurable settings
        // Set our maximum value for all items in this group
        int maxBalls = 180;
        int maxBerries = 40;
        int maxPotions = 30;

        public Settings()
        {
            inventoryBalls.Add(1, ItemId.ItemMasterBall);
            inventoryBalls.Add(2, ItemId.ItemUltraBall);
            inventoryBalls.Add(3, ItemId.ItemGreatBall);
            inventoryBalls.Add(4, ItemId.ItemPokeBall);

            inventoryBerries.Add(0, ItemId.ItemPinapBerry);
            inventoryBerries.Add(1, ItemId.ItemWeparBerry);
            inventoryBerries.Add(2, ItemId.ItemNanabBerry);
            inventoryBerries.Add(3, ItemId.ItemBlukBerry);
            inventoryBerries.Add(4, ItemId.ItemRazzBerry);

            inventoryPotions.Add(1, ItemId.ItemMaxPotion);
            inventoryPotions.Add(2, ItemId.ItemHyperPotion);
            inventoryPotions.Add(3, ItemId.ItemSuperPotion);
            inventoryPotions.Add(4, ItemId.ItemPotion);
        }

        private IDictionary<ItemId, int> _itemRecycleFilter;
        public ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter(IEnumerable<ItemData> myItems)
        {
            if (_itemRecycleFilter == null)
            {
                _itemRecycleFilter = new Dictionary<ItemId, int>();

                _itemRecycleFilter.Add(ItemId.ItemUnknown, 0);

                // These will be overwritten by the CalculateGroupAmounts calculations below 
                _itemRecycleFilter.Add(ItemId.ItemPokeBall, 25);
                _itemRecycleFilter.Add(ItemId.ItemGreatBall, 50);
                _itemRecycleFilter.Add(ItemId.ItemUltraBall, 75);
                _itemRecycleFilter.Add(ItemId.ItemMasterBall, 100);

                // These will be overwritten by the CalculateGroupAmounts calculations below
                _itemRecycleFilter.Add(ItemId.ItemPotion, 0);
                _itemRecycleFilter.Add(ItemId.ItemSuperPotion, 10);
                _itemRecycleFilter.Add(ItemId.ItemHyperPotion, 25);
                _itemRecycleFilter.Add(ItemId.ItemMaxPotion, 25);

                _itemRecycleFilter.Add(ItemId.ItemRevive, 15);
                _itemRecycleFilter.Add(ItemId.ItemMaxRevive, 25);

                _itemRecycleFilter.Add(ItemId.ItemLuckyEgg, 200);

                _itemRecycleFilter.Add(ItemId.ItemIncenseOrdinary, 100);
                _itemRecycleFilter.Add(ItemId.ItemIncenseSpicy, 100);
                _itemRecycleFilter.Add(ItemId.ItemIncenseCool, 100);
                _itemRecycleFilter.Add(ItemId.ItemIncenseFloral, 100);

                _itemRecycleFilter.Add(ItemId.ItemTroyDisk, 100);
                _itemRecycleFilter.Add(ItemId.ItemXAttack, 100);
                _itemRecycleFilter.Add(ItemId.ItemXDefense, 100);
                _itemRecycleFilter.Add(ItemId.ItemXMiracle, 100);

                // These will be overwritten by the CalculateGroupAmounts calculations below
                _itemRecycleFilter.Add(ItemId.ItemRazzBerry, 20);
                _itemRecycleFilter.Add(ItemId.ItemBlukBerry, 10);
                _itemRecycleFilter.Add(ItemId.ItemNanabBerry, 10);
                _itemRecycleFilter.Add(ItemId.ItemWeparBerry, 30);
                _itemRecycleFilter.Add(ItemId.ItemPinapBerry, 30);

                _itemRecycleFilter.Add(ItemId.ItemSpecialCamera, 100);
                _itemRecycleFilter.Add(ItemId.ItemIncubatorBasicUnlimited, 100);
                _itemRecycleFilter.Add(ItemId.ItemIncubatorBasic, 100);
                _itemRecycleFilter.Add(ItemId.ItemPokemonStorageUpgrade, 100);
                _itemRecycleFilter.Add(ItemId.ItemItemStorageUpgrade, 100);

            }

            // Calculate how many balls of each type we should keep
            CalculateGroupAmounts(inventoryBalls, maxBalls, myItems);

            // Calculate how many berries of each type we should keep
            CalculateGroupAmounts(inventoryBerries, maxBerries, myItems);

            // Calculate how many potions of each type we should keep
            CalculateGroupAmounts(inventoryPotions, maxPotions, myItems);

            return _itemRecycleFilter;
        }

        private void CalculateGroupAmounts(SortedList<int, ItemId> inventoryGroup, int maxQty, IEnumerable<ItemData> myItems)
        {
            int amountRemaining = maxQty;
            int amountToKeep = 0;
            foreach (KeyValuePair<int, ItemId> listItem in inventoryGroup)
            {
                ItemId itemId = listItem.Value;
                int itemQty = 0;

                ItemData item = myItems.FirstOrDefault(x => x.ItemId == itemId);
                if (item != null)
                {
                    itemQty = myItems.FirstOrDefault(x => x.ItemId == itemId).Count;
                }



                if (amountRemaining >= itemQty)
                {
                    amountToKeep = amountRemaining;
                }
                else
                {
                    amountToKeep = Math.Min(itemQty, amountRemaining);
                }

                amountRemaining = amountRemaining - itemQty;

                if (amountRemaining < 0)
                {
                    amountRemaining = 0;
                }

                try
                {
                    _itemRecycleFilter[itemId] = amountToKeep;  // Update the filter with amounts to keep
                }
                catch (Exception ex)
                { }

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
