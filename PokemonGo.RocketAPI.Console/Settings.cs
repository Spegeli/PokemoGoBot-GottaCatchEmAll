#region

using PokemonGo.RocketAPI.Enums;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using AllEnum;
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

        public float KeepMinIVPercentage => UserSettings.Default.KeepMinIVPercentage;
        public int KeepMinCP => UserSettings.Default.KeepMinCP;
        public double WalkingSpeedInKilometerPerHour => UserSettings.Default.WalkingSpeedInKilometerPerHour;
        public bool EvolveAllPokemonWithEnoughCandy => UserSettings.Default.EvolveAllPokemonWithEnoughCandy;
        public bool TransferDuplicatePokemon => UserSettings.Default.TransferDuplicatePokemon;
        public bool UsePokemonToNotCatchFilter => UserSettings.Default.UsePokemonToNotCatchFilter;
        public int KeepMinDuplicatePokemon => UserSettings.Default.KeepMinDuplicatePokemon;

        private ICollection<PokemonId> _pokemonsToEvolve;
        private ICollection<PokemonId> _pokemonsNotToTransfer;
        private ICollection<PokemonId> _pokemonsNotToCatch;

        public string GoogleRefreshToken
        {
            get { return UserSettings.Default.GoogleRefreshToken; }
            set
            {
                UserSettings.Default.GoogleRefreshToken = value;
                UserSettings.Default.Save();
            }
        }

        public ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter => new[]
        {
            new KeyValuePair<ItemId, int>(ItemId.ItemUnknown, 0),
            new KeyValuePair<ItemId, int>(ItemId.ItemPokeBall, 20),
            new KeyValuePair<ItemId, int>(ItemId.ItemGreatBall, 40),
            new KeyValuePair<ItemId, int>(ItemId.ItemUltraBall, 60),
            new KeyValuePair<ItemId, int>(ItemId.ItemMasterBall, 100),

            new KeyValuePair<ItemId, int>(ItemId.ItemPotion, 0),
            new KeyValuePair<ItemId, int>(ItemId.ItemSuperPotion, 0),
            new KeyValuePair<ItemId, int>(ItemId.ItemHyperPotion, 40),
            new KeyValuePair<ItemId, int>(ItemId.ItemMaxPotion, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemRevive, 20),
            new KeyValuePair<ItemId, int>(ItemId.ItemMaxRevive, 100),

            new KeyValuePair<ItemId, int>(ItemId.ItemLuckyEgg, 200),
            new KeyValuePair<ItemId, int>(ItemId.ItemIncenseOrdinary, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemIncenseSpicy, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemIncenseCool, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemIncenseFloral, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemTroyDisk, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemXAttack, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemXDefense, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemXMiracle, 100),

            new KeyValuePair<ItemId, int>(ItemId.ItemRazzBerry, 60),
            new KeyValuePair<ItemId, int>(ItemId.ItemBlukBerry, 60),
            new KeyValuePair<ItemId, int>(ItemId.ItemNanabBerry, 60),
            new KeyValuePair<ItemId, int>(ItemId.ItemWeparBerry, 60),
            new KeyValuePair<ItemId, int>(ItemId.ItemPinapBerry, 60),

            new KeyValuePair<ItemId, int>(ItemId.ItemSpecialCamera, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemIncubatorBasicUnlimited, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemIncubatorBasic, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemPokemonStorageUpgrade, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemItemStorageUpgrade, 100)
        };

        public ICollection<PokemonId> PokemonsToEvolve
        {
            get
            {
                //Type of pokemons to evolve
                _pokemonsToEvolve = _pokemonsToEvolve ?? LoadPokemonList("PokemonsToEvolve");
                return _pokemonsToEvolve;
            }
        }

        public ICollection<PokemonId> PokemonsNotToTransfer
        {
            get
            {
                //Type of pokemons not to transfer
                _pokemonsNotToTransfer = _pokemonsNotToTransfer ?? LoadPokemonList("PokemonsNotToTransfer");
                return _pokemonsNotToTransfer;
            }
        }

        public ICollection<PokemonId> PokemonsNotToCatch
        {
            get
            {
                //Type of pokemons not to catch
                _pokemonsNotToCatch = _pokemonsNotToCatch ?? LoadPokemonList("PokemonsNotToCatch");
                return _pokemonsNotToCatch;
            }
        }

        private ICollection<PokemonId> LoadPokemonList(string filename)
        {
            ICollection<PokemonId> result = new List<PokemonId>();
            string path = Directory.GetCurrentDirectory() + "\\Configs\\";
            if (!Directory.Exists(path))
            {
                DirectoryInfo di = Directory.CreateDirectory(path);
            }
            if (!File.Exists(path + filename + ".txt"))
            {
                string pokemonName = Properties.Resources.ResourceManager.GetString(filename);
                Logger.Normal($"File: {filename} not found, creating new...");
                File.WriteAllText(path + filename + ".txt", pokemonName);
            }
            if (File.Exists(path + filename + ".txt"))
            {
                string[] _locallist = File.ReadAllLines(path + filename + ".txt");
                foreach (string pokemonName in _locallist)
                {
                    var pokemon = Enum.Parse(typeof(PokemonId), pokemonName, true);
                    if (pokemonName != null) result.Add((PokemonId)pokemon);
                }
            }
            return result;
        }
    }
}
