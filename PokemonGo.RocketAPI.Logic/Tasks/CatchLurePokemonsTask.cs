using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Logic.Utils;
using POGOProtos.Enums;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

namespace PokemonGo.RocketAPI.Logic.Tasks
{
    public class CatchLurePokemonsTask
    {
        public static async Task Execute(FortData currentFortData)
        {
            Logger.Write("Looking for lured Pokemon...", LogLevel.Debug);

            var fortId = currentFortData.Id;
            var pokemonId = currentFortData.LureInfo.ActivePokemonId;

            if (Logic._client.Settings.UsePokemonToNotCatchList &&
                Logic._client.Settings.PokemonsToNotCatch.Contains(pokemonId))
            {
                Logger.Write($"Pokemon - {pokemonId} - is on ToNotCatch List", LogLevel.Warning);
                return;
            }

            var encounterId = currentFortData.LureInfo.EncounterId;
            
            var encounter = await Logic._client.Encounter.EncounterLurePokemon(encounterId, fortId);

            Logger.Write($"fortId: {fortId}", LogLevel.Debug);
            Logger.Write($"pokemonId: {pokemonId}", LogLevel.Debug);
            Logger.Write($"encounterId: {encounterId}", LogLevel.Debug);
            Logger.Write($"encounter: {encounter}", LogLevel.Debug);

            if (encounter.Result == DiskEncounterResponse.Types.Result.Success)
                await CatchPokemonTask.Execute(encounter, null, currentFortData, encounterId);
            else
            {
                if (encounter.Result.ToString().Contains("NotAvailable")) return;
                Logger.Write($"Encounter problem: Lure Pokemon {encounter.Result}", LogLevel.Warning);
            }
        }
    }
}
