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
        public AuthType AuthType => (AuthType)Enum.Parse(typeof(AuthType), UserSettings.Default.AuthType, true);
        public string PtcUsername => UserSettings.Default.PtcUsername;
        public string PtcPassword => UserSettings.Default.PtcPassword;
        public double DefaultLatitude => UserSettings.Default.DefaultLatitude;
        public double DefaultLongitude => UserSettings.Default.DefaultLongitude;
        public double DefaultAltitude => UserSettings.Default.DefaultAltitude;
        public bool UseGPXPathing => UserSettings.Default.UseGPXPathing;
        public string GPXFile => UserSettings.Default.GPXFile;
        public double WalkingSpeedInKilometerPerHour => UserSettings.Default.WalkingSpeedInKilometerPerHour;
        public int MaxTravelDistanceInMeters => UserSettings.Default.MaxTravelDistanceInMeters;

        public bool UsePokemonToNotCatchList => UserSettings.Default.UsePokemonToNotCatchList;
        public bool EvolvePokemon => UserSettings.Default.EvolvePokemon;
        public bool EvolveOnlyPokemonAboveIV => UserSettings.Default.EvolveOnlyPokemonAboveIV;
        public float EvolveOnlyPokemonAboveIVValue => UserSettings.Default.EvolveOnlyPokemonAboveIVValue;
        public bool TransferPokemon => UserSettings.Default.TransferPokemon;
        public int TransferPokemonKeepDuplicateAmount => UserSettings.Default.TransferPokemonKeepDuplicateAmount;
        public bool NotTransferPokemonsThatCanEvolve => UserSettings.Default.NotTransferPokemonsThatCanEvolve;

        public float KeepMinIVPercentage => UserSettings.Default.KeepMinIVPercentage;
        public int KeepMinCP => UserSettings.Default.KeepMinCP;
        public bool useLuckyEggsWhileEvolving => UserSettings.Default.useLuckyEggsWhileEvolving;
        public bool PrioritizeIVOverCP => UserSettings.Default.PrioritizeIVOverCP;

        private ICollection<PokemonId> _pokemonsToEvolve;
        private ICollection<PokemonId> _pokemonsNotToTransfer;
        private ICollection<PokemonId> _pokemonsNotToCatch;

        public ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter => new[]
        {
            new KeyValuePair<ItemId, int>(ItemId.ItemUnknown, 0),
            new KeyValuePair<ItemId, int>(ItemId.ItemPokeBall, 25),
            new KeyValuePair<ItemId, int>(ItemId.ItemGreatBall, 50),
            new KeyValuePair<ItemId, int>(ItemId.ItemUltraBall, 75),
            new KeyValuePair<ItemId, int>(ItemId.ItemMasterBall, 100),

            new KeyValuePair<ItemId, int>(ItemId.ItemPotion, 0),
            new KeyValuePair<ItemId, int>(ItemId.ItemSuperPotion, 10),
            new KeyValuePair<ItemId, int>(ItemId.ItemHyperPotion, 25),
            new KeyValuePair<ItemId, int>(ItemId.ItemMaxPotion, 75),

            new KeyValuePair<ItemId, int>(ItemId.ItemRevive, 15),
            new KeyValuePair<ItemId, int>(ItemId.ItemMaxRevive, 50),

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

        public ICollection<PokemonId> PokemonsToEvolve
        {
            get
            {
                //Type of pokemons to evolve
                List<PokemonId> defaultPokemon = new List<PokemonId> {
                    PokemonId.Zubat, PokemonId.Pidgey, PokemonId.Rattata
                };
                _pokemonsToEvolve = _pokemonsToEvolve ?? LoadPokemonList("PokemonsToEvolve.ini", defaultPokemon);
                return _pokemonsToEvolve;
            }
        }

        public ICollection<PokemonId> PokemonsNotToTransfer
        {
            get
            {
                //Type of pokemons not to transfer
                List<PokemonId> defaultPokemon = new List<PokemonId> {
                    PokemonId.Dragonite, PokemonId.Charizard, PokemonId.Zapdos, PokemonId.Snorlax, PokemonId.Alakazam, PokemonId.Mew, PokemonId.Mewtwo
                };
                _pokemonsNotToTransfer = _pokemonsNotToTransfer ?? LoadPokemonList("PokemonsNotToTransfer.ini", defaultPokemon);
                return _pokemonsNotToTransfer;
            }
        }

        public ICollection<PokemonId> PokemonsNotToCatch
        {
            get
            {
                //Type of pokemons not to catch
                List<PokemonId> defaultPokemon = new List<PokemonId> {
                    PokemonId.Zubat, PokemonId.Pidgey, PokemonId.Rattata
                };
                _pokemonsNotToCatch = _pokemonsNotToCatch ?? LoadPokemonList("PokemonsNotToCatch.ini", defaultPokemon);
                return _pokemonsNotToCatch;
            }
        }

        private ICollection<PokemonId> LoadPokemonList(string filename, List<PokemonId> defaultPokemon)
        {
            ICollection<PokemonId> result = new List<PokemonId>();
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Configs");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, filename);

            if (!File.Exists(path))
            {
                Logger.Write($"File: \"Configs/{filename}\" not found, creating new...", LogLevel.Warning);
                using (var w = File.AppendText(path))
                {
                    defaultPokemon.ForEach(pokemon => w.WriteLine(pokemon.ToString()));
                    defaultPokemon.ForEach(pokemon => result.Add((PokemonId)pokemon));
                    w.Close();
                }
            }
            if (File.Exists(path))
            {
                Logger.Write($"Loading File: \"Configs/{filename}\"", LogLevel.Info);

                var content = string.Empty;
                using (StreamReader reader = new StreamReader(path))
                {
                    content = reader.ReadToEnd();
                    reader.Close();
                }
                content = Regex.Replace(content, @"\\/\*(.|\n)*?\*\/", ""); //todo: supposed to remove comment blocks

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
            return result;
        }
    }
}
