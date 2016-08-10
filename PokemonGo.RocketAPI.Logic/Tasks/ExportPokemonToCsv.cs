using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using POGOProtos.Data;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

namespace PokemonGo.RocketAPI.Logic.Tasks
{
    public class ExportPokemonToCsv
    {
        private static readonly string _exportPath = Path.Combine(Directory.GetCurrentDirectory(), "Export");

        public static async Task Execute(PlayerData player, string filename = "PokeList.csv")
        {
            if (player == null)
                return;
            var stats = await Inventory.GetPlayerStats();
            var stat = stats.FirstOrDefault();
            if (stat == null)
                return;

            if (!Directory.Exists(_exportPath))
                Directory.CreateDirectory(_exportPath);
            if (Directory.Exists(_exportPath))
            {
                try
                {
                    var pokelistFile = Path.Combine(_exportPath, $"Profile_{player.Username}_{filename}");
                    if (File.Exists(pokelistFile))
                        File.Delete(pokelistFile);
                    var ls = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                    const string header = "PokemonID,Name,NickName,Level,CP / MaxCP,IV Perfection in %,Attack 1,Attack 2,HP,Attk,Def,Stamina,Familie Candies,IsInGym,IsFavorite,previewLink";
                    File.WriteAllText(pokelistFile, $"{header.Replace(",", ls == "," ? "." : ",")}");

                    var allPokemon = await Inventory.GetHighestsPerfect();
                    var myPokemonSettings = await Inventory.GetPokemonSettings();
                    var pokemonSettings = myPokemonSettings.ToList();
                    var myPokemonFamilies = await Inventory.GetPokemonFamilies();
                    var pokemonFamilies = myPokemonFamilies.ToArray();
                    var trainerLevel = stat.Level;
                    var expReq = new[] { 0, 1000, 3000, 6000, 10000, 15000, 21000, 28000, 36000, 45000, 55000, 65000, 75000, 85000, 100000, 120000, 140000, 160000, 185000, 210000, 260000, 335000, 435000, 560000, 710000, 900000, 1100000, 1350000, 1650000, 2000000, 2500000, 3000000, 3750000, 4750000, 6000000, 7500000, 9500000, 12000000, 15000000, 20000000 };
                    var expReqAtLevel = expReq[stat.Level - 1];

                    using (var w = File.AppendText(pokelistFile))
                    {
                        w.WriteLine("");
                        foreach (var pokemon in allPokemon)
                        {
                            var toEncode = $"{(int)pokemon.PokemonId}" + "," + trainerLevel + "," + PokemonInfo.GetLevel(pokemon) + "," + pokemon.Cp + "," + pokemon.Stamina;
                            //Generate base64 code to make it viewable here http://poke.isitin.org/#MTUwLDIzLDE3LDE5MDIsMTE4
                            var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(toEncode));

                            var isInGym = pokemon.DeployedFortId != string.Empty ? "Yes" : "No";
                            var isFavorite = pokemon.Favorite != 0 ? "Yes" : "No";

                            var settings = pokemonSettings.Single(x => x.PokemonId == pokemon.PokemonId);
                            var familiecandies = pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId).Candy_;
                            
                            var perfection = PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00");
                            perfection = perfection.Replace(",", ls == "," ? "." : ",");
                            string contentPart1 = $"\"{(int)pokemon.PokemonId}\",\"{pokemon.PokemonId}\",\"{pokemon.Nickname}\",";
                            string contentPart2 = $",\"{pokemon.Cp} / {PokemonInfo.CalculateMaxCp(pokemon)}\",";
                            string contentPart3 = $",\"{pokemon.Move1}\",\"{pokemon.Move2}\",\"{pokemon.Stamina}\",\"{pokemon.IndividualAttack}\",\"{pokemon.IndividualDefense}\",\"{pokemon.IndividualStamina}\",\"{familiecandies}\",\"{isInGym}\",\"{isFavorite}\",http://poke.isitin.org/#{encoded}";
                            string content = $"{contentPart1.Replace(",", $"{ls}")}\"{PokemonInfo.GetLevel(pokemon)}\"{contentPart2.Replace(",", $"{ls}")}\"{perfection}\"{contentPart3.Replace(",", $"{ls}")}";

                            w.WriteLine($"{content}");
                        }
                        w.Close();
                    }
                    Logger.Write($"Export Player Infos and all Pokemon to \"\\Export\\Profile_{player.Username}_{filename}\"", LogLevel.Info);
                }
                catch
                {
                    Logger.Write("Export Player Infos and all Pokemons to CSV not possible. File seems be in use!", LogLevel.Warning);
                }
            }
        }
    }
}
