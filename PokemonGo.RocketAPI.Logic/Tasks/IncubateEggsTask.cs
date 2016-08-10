using POGOProtos.Data;
using POGOProtos.Inventory;
using POGOProtos.Inventory.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

namespace PokemonGo.RocketAPI.Logic.Tasks
{
    class IncubateEggsTask
    {
        public static int _hatchUpdateDelay = 0;

        public static async Task Execute()
        {
            try
            {
                await Inventory.GetCachedInventory(true);
                var incubators = (await Inventory.GetEggIncubators(Logic._clientSettings.UseOnlyBasicIncubator)).ToList();
                var unusedEggs = (await Inventory.GetUnusedEggs()).OrderBy(x => x.EggKmWalkedTarget).ToList();
                var pokemons = (await Inventory.GetPokemons()).ToList();

                var playerStats = (await Inventory.GetPlayerStats()).FirstOrDefault();
                if (playerStats == null)
                    return;

                var kmWalked = playerStats.KmWalked;

                foreach (var incubator in incubators)
                {
                    if (incubator.PokemonId == 0)
                    {
                        // Unlimited incubators prefer short eggs, limited incubators prefer long eggs
                        // Special case: If only one incubator is available at all, it will prefer long eggs 
                        // (disabled, i want to hatch more and more eggs)
                        var egg = incubator.ItemId == ItemId.ItemIncubatorBasicUnlimited && incubators.Count > 0
                          ? unusedEggs.FirstOrDefault()
                          : unusedEggs.LastOrDefault();

                        // Don't use limited incubators for under 5km eggs
                        if (egg == null | (egg.EggKmWalkedTarget < 5
                            && incubator.ItemId == ItemId.ItemIncubatorBasic))
                        {
                            continue;
                        }

                        var response = await Logic._client.Inventory.UseItemEggIncubator(incubator.Id, egg.Id);
                        Logger.Write($"Adding Egg #{unusedEggs.IndexOf(egg)} to Incubator #{incubators.IndexOf(incubator)}: {response.Result}!", LogLevel.Incubation);

                        unusedEggs.Remove(egg);
                    }
                    else
                    {
                        // Currently hatching
                        if (_hatchUpdateDelay <= 0)
                        {
                            var kmToWalk = incubator.TargetKmWalked - incubator.StartKmWalked;
                            var kmRemaining = incubator.TargetKmWalked - kmWalked;

                            Logger.Write($"Incubator #{incubators.IndexOf(incubator)} needs {kmRemaining.ToString("N2")}km/{kmToWalk.ToString("N2")}km to hatch.", LogLevel.Egg);
                        }
                    }
                }

                if (_hatchUpdateDelay <= 0)
                    // print the update each 3 recycles
                    _hatchUpdateDelay = 15;
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
