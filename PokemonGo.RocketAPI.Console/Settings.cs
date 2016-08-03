#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.GeneratedCode;
using PokemonGo.RocketAPI.Logging;


#endregion


namespace PokemonGo.RocketAPI.Console
{
    public class Settings : ISettings
    {
        private string configs_path = Path.Combine(Directory.GetCurrentDirectory(), "Settings");

        public AuthType AuthType { get; set; }
        public string PtcUsername { get; set; }
        public string PtcPassword { get; set; }
        public string GoogleEmail { get; set; }
        public string GooglePassword { get; set; }
        public double DefaultLatitude { get; set; }
        public double DefaultLongitude { get; set; }
        public double DefaultAltitude { get; set; }
        public bool UseGPXPathing { get; set; }
        public string GPXFile { get; set; }
        public bool GPXIgnorePokestops { get; set; }
        public bool GPXIgnorePokemon { get; set; }

        public double WalkingSpeedInKilometerPerHour { get; set; }
        public int MaxTravelDistanceInMeters { get; set; }

        public int CapturePokemonDuration { get; set; }

        public bool UsePokemonToNotCatchList { get; set; }
        public bool UsePokemonToNotTransferList { get; set; }
        public bool EvolvePokemon { get; set; }
        public bool EvolveOnlyPokemonAboveIV { get; set; }
        public float EvolveOnlyPokemonAboveIVValue { get; set; }
        public int EvolveKeepCandiesValue { get; set; }
        public bool TransferPokemon { get; set; }
        public int TransferPokemonKeepDuplicateAmountMaxCP { get; set; }
        public int TransferPokemonKeepDuplicateAmountMaxIV { get; set; }
        public bool NotTransferPokemonsThatCanEvolve { get; set; }
        public bool UseTransferPokemonKeepAboveCP { get; set; }
        public int TransferPokemonKeepAboveCP { get; set; }
        public bool UseTransferPokemonKeepAboveIV { get; set; }
        public float TransferPokemonKeepAboveIVPercentage { get; set; }

        public bool UseLuckyEggs { get; set; }
        public bool UseIncense { get; set; }
        public bool PrioritizeIVOverCP { get; set; }

        public bool DebugMode { get; set; }

        private ICollection<PokemonId> _pokemonsToEvolve;
        private ICollection<PokemonId> _pokemonsToNotTransfer;
        private ICollection<PokemonId> _pokemonsToNotCatch;
        private ICollection<KeyValuePair<ItemId, int>> _ItemRecycleFilter;


        public Settings()
        {

            this.AuthType = (AuthType)Enum.Parse(typeof(AuthType), UserSettings.Default.AuthType, true);
            this.PtcUsername = UserSettings.Default.PtcUsername;
            this.PtcPassword = UserSettings.Default.PtcPassword;
            this.GoogleEmail = UserSettings.Default.GoogleEmail;
            this.GooglePassword = UserSettings.Default.GooglePassword;
            this.DefaultLatitude = UserSettings.Default.DefaultLatitude;
            this.DefaultLongitude = UserSettings.Default.DefaultLongitude;
            this.DefaultAltitude = UserSettings.Default.DefaultAltitude;
            this.UseGPXPathing = UserSettings.Default.UseGPXPathing;
            this.GPXFile = UserSettings.Default.GPXFile;
            this.GPXIgnorePokestops = UserSettings.Default.GPXIgnorePokestops;
            this.GPXIgnorePokemon = UserSettings.Default.GPXIgnorePokemon;

            this.WalkingSpeedInKilometerPerHour = UserSettings.Default.WalkingSpeedInKilometerPerHour;
            this.MaxTravelDistanceInMeters = UserSettings.Default.MaxTravelDistanceInMeters;

            this.UsePokemonToNotCatchList = UserSettings.Default.UsePokemonToNotCatchList;
            this.UsePokemonToNotTransferList = UserSettings.Default.UsePokemonToNotTransferList;
            this.EvolvePokemon = UserSettings.Default.EvolvePokemon;
            this.EvolveOnlyPokemonAboveIV = UserSettings.Default.EvolveOnlyPokemonAboveIV;
            this.EvolveOnlyPokemonAboveIVValue = UserSettings.Default.EvolveOnlyPokemonAboveIVValue;
            this.EvolveKeepCandiesValue = UserSettings.Default.EvolveKeepCandiesValue;

            this.TransferPokemon = UserSettings.Default.TransferPokemon;
            this.TransferPokemonKeepDuplicateAmountMaxCP = UserSettings.Default.TransferPokemonKeepDuplicateAmountMaxCP;
            this.TransferPokemonKeepDuplicateAmountMaxIV = UserSettings.Default.TransferPokemonKeepDuplicateAmountMaxIV;
            this.NotTransferPokemonsThatCanEvolve = UserSettings.Default.NotTransferPokemonsThatCanEvolve;
            this.UseTransferPokemonKeepAboveCP = UserSettings.Default.UseTransferPokemonKeepAboveCP;
            this.TransferPokemonKeepAboveCP = UserSettings.Default.TransferPokemonKeepAboveCP;
            this.UseTransferPokemonKeepAboveIV = UserSettings.Default.UseTransferPokemonKeepAboveIV;
            this.TransferPokemonKeepAboveIVPercentage = UserSettings.Default.TransferPokemonKeepAboveIVPercentage;

            this.UseLuckyEggs = UserSettings.Default.UseLuckyEggs;
            this.UseIncense = UserSettings.Default.UseIncense;
            this.PrioritizeIVOverCP = UserSettings.Default.PrioritizeIVOverCP;
            this.DebugMode = UserSettings.Default.DebugMode;
        }

        public ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter
        {
            get
            {
                if (_ItemRecycleFilter == null)
                {
                    _ItemRecycleFilter =
                    new[]{
                        new KeyValuePair<ItemId, int>(ItemId.ItemUnknown, 0),
                        new KeyValuePair<ItemId, int>(ItemId.ItemPokeBall, 25),
                        new KeyValuePair<ItemId, int>(ItemId.ItemGreatBall, 50),
                        new KeyValuePair<ItemId, int>(ItemId.ItemUltraBall, 75),
                        new KeyValuePair<ItemId, int>(ItemId.ItemMasterBall, 100),

                        new KeyValuePair<ItemId, int>(ItemId.ItemPotion, 0),
                        new KeyValuePair<ItemId, int>(ItemId.ItemSuperPotion, 10),
                        new KeyValuePair<ItemId, int>(ItemId.ItemHyperPotion, 25),
                        new KeyValuePair<ItemId, int>(ItemId.ItemMaxPotion, 25),

                        new KeyValuePair<ItemId, int>(ItemId.ItemRevive, 15),
                        new KeyValuePair<ItemId, int>(ItemId.ItemMaxRevive, 25),

                        new KeyValuePair<ItemId, int>(ItemId.ItemLuckyEgg, 200),

                        new KeyValuePair<ItemId, int>(ItemId.ItemIncenseOrdinary, 100),
                        new KeyValuePair<ItemId, int>(ItemId.ItemIncenseSpicy, 100),
                        new KeyValuePair<ItemId, int>(ItemId.ItemIncenseCool, 100),
                        new KeyValuePair<ItemId, int>(ItemId.ItemIncenseFloral, 100),

                        new KeyValuePair<ItemId, int>(ItemId.ItemTroyDisk, 100),
                        new KeyValuePair<ItemId, int>(ItemId.ItemXAttack, 100),
                        new KeyValuePair<ItemId, int>(ItemId.ItemXDefense, 100),
                        new KeyValuePair<ItemId, int>(ItemId.ItemXMiracle, 100),

                        new KeyValuePair<ItemId, int>(ItemId.ItemRazzBerry, 20),
                        new KeyValuePair<ItemId, int>(ItemId.ItemBlukBerry, 10),
                        new KeyValuePair<ItemId, int>(ItemId.ItemNanabBerry, 10),
                        new KeyValuePair<ItemId, int>(ItemId.ItemWeparBerry, 30),
                        new KeyValuePair<ItemId, int>(ItemId.ItemPinapBerry, 30),

                        new KeyValuePair<ItemId, int>(ItemId.ItemSpecialCamera, 100),
                        new KeyValuePair<ItemId, int>(ItemId.ItemIncubatorBasicUnlimited, 100),
                        new KeyValuePair<ItemId, int>(ItemId.ItemIncubatorBasic, 100),
                        new KeyValuePair<ItemId, int>(ItemId.ItemPokemonStorageUpgrade, 100),
                        new KeyValuePair<ItemId, int>(ItemId.ItemItemStorageUpgrade, 100),
                        };
                }
                return _ItemRecycleFilter;
            }
            set
            {
                _ItemRecycleFilter = value;
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
            set
            {
                _pokemonsToEvolve = value;
            }
        }

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
            set
            {
                _pokemonsToNotTransfer = value;
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
            set
            {
                _pokemonsToNotCatch = value;
            }
        }

        private ICollection<PokemonId> LoadPokemonList(string filename, List<PokemonId> defaultPokemon)
        {
            ICollection<PokemonId> result = new List<PokemonId>();
            if (!Directory.Exists(this.configs_path))
                Directory.CreateDirectory(this.configs_path);
            var pokemonlistFile = Path.Combine(this.configs_path, filename);
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
