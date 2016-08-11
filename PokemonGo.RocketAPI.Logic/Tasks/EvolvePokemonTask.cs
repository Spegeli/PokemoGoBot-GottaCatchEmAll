using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using POGOProtos.Networking.Responses;
using PokemonGo.RocketAPI.Logic;
using PokemonGo.RocketAPI.Logic.Utils;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

namespace PokemonGo.RocketAPI.Logic.Tasks
{
    public class EvolvePokemonTask
    {
        public static async Task Execute()
        {
            await Inventory.GetCachedInventory(true);
            var pokemonToEvolve = await Inventory.GetPokemonToEvolve(Logic._client.Settings.PokemonsToEvolve);
            if (pokemonToEvolve == null || !pokemonToEvolve.Any())
                return;

            Logger.Write($"Found {pokemonToEvolve.Count()} Pokemon for Evolve:", LogLevel.Debug);
            foreach (var pokemon in pokemonToEvolve)
            {
                var evolvePokemonOutProto = await Logic._client.Inventory.EvolvePokemon(pokemon.Id);

                await Inventory.GetCachedInventory(true);

                Logger.Write(evolvePokemonOutProto.Result == EvolvePokemonResponse.Types.Result.Success
                        ? $"{pokemon.PokemonId} successfully for {evolvePokemonOutProto.ExperienceAwarded} xp"
                        : $"Failed: {pokemon.PokemonId}. EvolvePokemonOutProto.Result was {evolvePokemonOutProto.Result}, stopping evolving {pokemon.PokemonId}"
                    , LogLevel.Evolve);

                if (evolvePokemonOutProto.Result == EvolvePokemonResponse.Types.Result.Success)
                    BotStats.ExperienceThisSession += evolvePokemonOutProto.ExperienceAwarded;
            }
            await BotStats.GetPokeDexCount();
            BotStats.UpdateConsoleTitle();
        }

    }
}
