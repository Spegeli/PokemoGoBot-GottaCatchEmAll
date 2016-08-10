using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

namespace PokemonGo.RocketAPI.Logic.Tasks
{
    public class UseLuckyEggTask
    {
        private static DateTime _lastLuckyEggTime;

        public static async Task Execute()
        {
            var currentAmountOfLuckyEggs = await Inventory.GetItemAmountByType(ItemId.ItemLuckyEgg);
            if (currentAmountOfLuckyEggs <= 0 || _lastLuckyEggTime.AddMinutes(30).Ticks > DateTime.Now.Ticks)
                return;

            var UseEgg = await Logic._client.Inventory.UseItemXpBoost();
            if (UseEgg.Result == UseItemXpBoostResponse.Types.Result.ErrorXpBoostAlreadyActive)
                return;

            if (UseEgg.Result == UseItemXpBoostResponse.Types.Result.Success)
            {
                _lastLuckyEggTime = DateTime.Now;
                Logger.Write($"Used Lucky Egg, remaining: {currentAmountOfLuckyEggs - 1}", LogLevel.Egg);
            }
            else if (UseEgg.Result == UseItemXpBoostResponse.Types.Result.ErrorNoItemsRemaining)
            {
                Logger.Write($"No Eggs Available", LogLevel.Debug);
            }
            else if (UseEgg.Result == UseItemXpBoostResponse.Types.Result.ErrorXpBoostAlreadyActive || (UseEgg.AppliedItems == null))
            {
                Logger.Write($"Lucky Egg Already Active", LogLevel.Debug);
            }
        }
    }
}
