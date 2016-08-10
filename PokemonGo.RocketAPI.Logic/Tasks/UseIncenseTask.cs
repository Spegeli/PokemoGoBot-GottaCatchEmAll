using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Logic;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

namespace PokemonGo.RocketAPI.Logic.Tasks
{
    public class UseIncenseTask
    {
        private static DateTime _lastIncenseTime;

        public static async Task Execute()
        {
            var currentAmountOfIncense = await Inventory.GetItemAmountByType(ItemId.ItemIncenseOrdinary);
            if (currentAmountOfIncense <= 0 || _lastIncenseTime.AddMinutes(30).Ticks > DateTime.Now.Ticks)
                return;

            var UseIncense = await Logic._client.Inventory.UseIncense(ItemId.ItemIncenseOrdinary);
            if (UseIncense.Result == UseIncenseResponse.Types.Result.IncenseAlreadyActive)
                return;

            if (UseIncense.Result == UseIncenseResponse.Types.Result.Success)
            {
                _lastIncenseTime = DateTime.Now;
                Logger.Write($"Used Incense, remaining: {currentAmountOfIncense - 1}", LogLevel.Incense);
            }
            else if (UseIncense.Result == UseIncenseResponse.Types.Result.NoneInInventory)
            {
                Logger.Write($"No Incense Available", LogLevel.Debug);
            }
            else if (UseIncense.Result == UseIncenseResponse.Types.Result.IncenseAlreadyActive || (UseIncense.AppliedIncense == null))
            {
                Logger.Write($"Incense Already Active", LogLevel.Debug);
            }
        }
    }
}
