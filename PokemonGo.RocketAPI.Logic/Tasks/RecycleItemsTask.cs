using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Logic.Utils;
using POGOProtos.Inventory.Item;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

namespace PokemonGo.RocketAPI.Logic.Tasks
{
    public class RecycleItemsTask
    {
        public static int _recycleCounter = 0;

        public static async Task Execute()
        {
            await Inventory.GetCachedInventory(true);
            var items = await Inventory.GetItemsToRecycle(Logic._clientSettings);
            if (items == null || !items.Any())
                return;

            Logger.Write($"Found {items.Count()} Recyclable {(items.Count() == 1 ? "Item" : "Items")}:", LogLevel.Debug);
            foreach (var item in items)
            {
                await Logic._client.Inventory.RecycleItem(item.ItemId, item.Count);
                Logger.Write($"{item.Count}x {item.ItemId}", LogLevel.Recycling);

                BotStats.ItemsRemovedThisSession += item.Count;
            }

            await BotStats.UpdateConsoleTitle();
            _recycleCounter = 0;
        }
    }
}
