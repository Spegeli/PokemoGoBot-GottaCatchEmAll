using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Logic.Utils;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

namespace PokemonGo.RocketAPI.Logic.Tasks
{
    public class TransferPokemonTask
    {
        public static async Task Execute()
        {
            await Inventory.GetCachedInventory(true);
            var pokemonToTransfer = await Inventory.GetPokemonToTransfer(Logic._clientSettings.NotTransferPokemonsThatCanEvolve, Logic._clientSettings.PrioritizeIVOverCP, Logic._clientSettings.PokemonsToNotTransfer);
            if (pokemonToTransfer != null && pokemonToTransfer.Any())
                Logger.Write($"Found {pokemonToTransfer.Count()} Pokemon for Transfer:", LogLevel.Info);

            foreach (var pokemon in pokemonToTransfer)
            {
                await Logic._client.Inventory.TransferPokemon(pokemon.Id);

                await Inventory.GetCachedInventory(true);
                var myPokemonSettings = await Inventory.GetPokemonSettings();
                var pokemonSettings = myPokemonSettings.ToList();
                var myPokemonFamilies = await Inventory.GetPokemonFamilies();
                var pokemonFamilies = myPokemonFamilies.ToArray();
                var settings = pokemonSettings.Single(x => x.PokemonId == pokemon.PokemonId);
                var familyCandy = pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId);
                var familyCandies = $"{familyCandy.Candy_}";

                BotStats.PokemonTransferedThisSession += 1;

                var bestPokemonOfType = Logic._client.Settings.PrioritizeIVOverCP
                    ? await Inventory.GetHighestPokemonOfTypeByIv(pokemon)
                    : await Inventory.GetHighestPokemonOfTypeByCp(pokemon);
                var bestPokemonInfo = "NONE";
                if (bestPokemonOfType != null)
                    bestPokemonInfo = $"CP: {bestPokemonOfType.Cp}/{PokemonInfo.CalculateMaxCp(bestPokemonOfType)} | IV: {PokemonInfo.CalculatePokemonPerfection(bestPokemonOfType).ToString("0.00")}% perfect";

                Logger.Write($"{pokemon.PokemonId} [CP {pokemon.Cp}/{PokemonInfo.CalculateMaxCp(pokemon)} | IV: { PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00")}% perfect] | Best: [{bestPokemonInfo}] | Family Candies: {familyCandies}", LogLevel.Transfer);
            }

            await BotStats.GetPokemonCount();
            await BotStats.UpdateConsoleTitle();
        }
    }
}
