using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using POGOProtos.Data;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

namespace PokemonGo.RocketAPI.Logic.Tasks
{
    public class RenamePokemonTask
    {
        public static async Task Execute()
        {
            await Inventory.GetCachedInventory(true);
            var pokemonToRename = await Inventory.GetPokemonToRename();
            if (pokemonToRename == null || !pokemonToRename.Any())
                return;

            var renamedPokemon = new List<PokemonData>();

            var IVRegex = new Regex("{IV}", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            var HPRegex = new Regex("{HP}", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            var NameRegex = new Regex("{NAME}", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            foreach (var pokemon in pokemonToRename)
            {
                var newNickname = Logic._client.Settings.RenamePokemonTemplate;

                if (IVRegex.IsMatch(newNickname))
                {
                    var perfection = Math.Round(PokemonInfo.CalculatePokemonPerfection(pokemon));
                    newNickname = IVRegex.Replace(newNickname, perfection.ToString());
                }

                if (HPRegex.IsMatch(newNickname))
                {
                    newNickname = HPRegex.Replace(newNickname, pokemon.StaminaMax.ToString());
                }

                if (NameRegex.IsMatch(newNickname))
                {
                    var name = pokemon.PokemonId.ToString();
                    var maxNameLength = 12 - (newNickname.Length - 6);
                    if (name.Length > maxNameLength)
                        name = name.Substring(0, maxNameLength);
                    newNickname = NameRegex.Replace(newNickname, name);
                }

                if (newNickname.Length > 12)
                    newNickname = newNickname.Substring(0, 12);

                var oldNickname = (pokemon.Nickname.Length != 0) ? pokemon.Nickname : pokemon.PokemonId.ToString();

                if (newNickname != oldNickname)
                {
                    pokemon.Nickname = newNickname;
                    renamedPokemon.Add(pokemon);
                }
            }

            if (renamedPokemon == null || !renamedPokemon.Any())
                return;

            Logger.Write($"Found {renamedPokemon.Count()} Pokemon for Rename:", LogLevel.Debug);

            foreach (var pokemon in renamedPokemon)
            {
                await Logic._client.Inventory.NicknamePokemon(pokemon.Id, pokemon.Nickname);
                Logger.Write($"{pokemon.PokemonId} [CP {pokemon.Cp}/{PokemonInfo.CalculateMaxCp(pokemon)} | IV: { PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00")}% perfect] | Nickname : {pokemon.Nickname}", LogLevel.Rename);
            }
        }
    }
}
