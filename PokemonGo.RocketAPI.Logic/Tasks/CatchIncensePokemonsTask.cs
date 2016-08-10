using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Logic.Utils;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Responses;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

namespace PokemonGo.RocketAPI.Logic.Tasks
{
    public class CatchIncensePokemonsTask
    {
        public static async Task Execute()
        {
            if (!Logic._client.Settings.CatchIncensePokemon)
                return;

            var incensePokemon = await Logic._client.Map.GetIncensePokemons();

            if (incensePokemon.Result == GetIncensePokemonResponse.Types.Result.IncenseEncounterAvailable)
            {
                var pokemon = new MapPokemon
                {
                    EncounterId = incensePokemon.EncounterId,
                    ExpirationTimestampMs = incensePokemon.DisappearTimestampMs,
                    Latitude = incensePokemon.Latitude,
                    Longitude = incensePokemon.Longitude,
                    PokemonId = incensePokemon.PokemonId,
                    SpawnPointId = incensePokemon.EncounterLocation
                };

                if (Logic._client.Settings.UsePokemonToNotCatchList &&
                    Logic._client.Settings.PokemonsToNotCatch.Contains(pokemon.PokemonId))
                {
                    Logger.Write($"Pokemon - {pokemon.PokemonId} - is on ToNotCatch List");
                    return;
                }

                /*
                var distance = LocationUtils.CalculateDistanceInMeters(Logic._client.CurrentLatitude,
                    Logic._client.CurrentLongitude, pokemon.Latitude, pokemon.Longitude);
                await Task.Delay(distance > 100 ? 3000 : 500);
                */

                var encounter =
                    await Logic._client.Encounter.EncounterIncensePokemon(pokemon.EncounterId, pokemon.SpawnPointId);

                if (encounter.Result == IncenseEncounterResponse.Types.Result.IncenseEncounterSuccess)
                    await CatchPokemonTask.Execute(encounter, pokemon);
                else
                    Logger.Write($"Encounter problem: {encounter.Result}", LogLevel.Warning);
            }

            if (Logic._client.Settings.EvolvePokemon || Logic._client.Settings.EvolveOnlyPokemonAboveIV) await EvolvePokemonTask.Execute();
            if (Logic._client.Settings.TransferPokemon) await TransferPokemonTask.Execute();
        }
    }
}
