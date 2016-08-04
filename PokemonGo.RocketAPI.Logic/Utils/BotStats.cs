#region

using System;
using System.Linq;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.GeneratedCode;
using PokemonGo.RocketAPI.Enums;
using System.Globalization;
using PokemonGo.RocketAPI.Helpers;
using PokemonGo.RocketAPI.Logging;

#endregion


namespace PokemonGo.RocketAPI.Logic.Utils
{
    internal class BotStats
    {
        public static int TotalExperience;
        public static int TotalPokemons;
        public static int TotalItemsRemoved;
        public static int TotalPokemonsTransfered;
        public static int TotalStardust;
        public static string CurrentLevelInfos;
        public static int Currentlevel = -1;
        public static string PlayerName;
        public static int TotalPokesInBag;
        public static int TotalPokesInPokedex;
        public static float KmWalkedOnStart;
        public static float KmWalkedCurrent;

        public static DateTime InitSessionDateTime = DateTime.Now;
        public static TimeSpan Duration = DateTime.Now - InitSessionDateTime;

        public static async Task<string> _getcurrentLevelInfos(Inventory inventory)
        {
            var stats = await inventory.GetPlayerStats();
            var output = string.Empty;
            var stat = stats.FirstOrDefault();
            if (stat == null) return output;

            var ep = stat.NextLevelXp - stat.PrevLevelXp - (stat.Experience - stat.PrevLevelXp);
            var time = Math.Round(ep / (TotalExperience / _getSessionRuntime()), 2);
            var hours = 0.00;
            var minutes = 0.00;
            if (double.IsInfinity(time) == false && time > 0)
            {
                hours = Math.Truncate(TimeSpan.FromHours(time).TotalHours);
                minutes = TimeSpan.FromHours(time).Minutes;
            }

            return $"{stat.Level} (LvLUp in {hours}h {minutes}m | {stat.Experience - stat.PrevLevelXp - GetXpDiff(stat.Level)}/{stat.NextLevelXp - stat.PrevLevelXp - GetXpDiff(stat.Level)} XP)";
        }

        public static string GetUsername(Client client, GetPlayerResponse profile)
        {
            return PlayerName = client.Settings.AuthType == AuthType.Ptc ? client.Settings.PtcUsername : profile.Profile.Username;
        }

        public static double _getSessionRuntime()
        {
            return (DateTime.Now - InitSessionDateTime).TotalSeconds/3600;
        }

        public static string _getSessionRuntimeInTimeFormat()
        {
            return (DateTime.Now - InitSessionDateTime).ToString(@"dd\.hh\:mm\:ss");
        }

        public void AddExperience(int xp)
        {
            TotalExperience += xp;
        }

        public void AddItemsRemoved(int count)
        {
            TotalItemsRemoved += count;
        }

        public void GetStardust(int stardust)
        {
            TotalStardust = stardust;
        }

        public void IncreasePokemons()
        {
            TotalPokemons += 1;
        }

        public void IncreasePokemonsTransfered()
        {
            TotalPokemonsTransfered += 1;
        }

        public async void UpdateConsoleTitle(Client client, Inventory _inventory)
        {
            //appears to give incorrect info?		
            var pokes = await _inventory.GetPokemons();
            TotalPokesInBag = pokes.Count();

            var inventory = await Inventory.GetCachedInventory(client);
            TotalPokesInPokedex = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PokedexEntry).Where(x => x != null && x.TimesCaptured >= 1).OrderBy(k => k.PokedexEntryNumber).ToArray().Length;

            CurrentLevelInfos = await _getcurrentLevelInfos(_inventory);

            var stats = await _inventory.GetPlayerStats();
            var stat = stats.FirstOrDefault();
            if (stat != null) KmWalkedCurrent = stat.KmWalked-KmWalkedOnStart;

            Console.Title = ToString();
        }

        public override string ToString()
        {
            return
                string.Format(
                    "{0} - Runtime {1} - Lvl: {2:0} | EXP/H: {3:0} | P/H: {4:0} | Stardust: {5:0} | Transfered: {6:0} | Items Recycled: {7:0} | Pokemon: {8:0} | Pokedex: {9:0}/151 | Km Walked this Session: {10:0.00} | Bot Version: {11:0}",
                    PlayerName, _getSessionRuntimeInTimeFormat(), CurrentLevelInfos, TotalExperience / _getSessionRuntime(),
                    TotalPokemons / _getSessionRuntime(), TotalStardust, TotalPokemonsTransfered, TotalItemsRemoved, TotalPokesInBag, TotalPokesInPokedex, KmWalkedCurrent, GitChecker.CurrentVersion);
        }

        public static int GetXpDiff(int level)
        {
            if (level <= 0 || level > 40) return 0;
            int[] xpTable = { 0, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000,
                10000, 10000, 10000, 10000, 15000, 20000, 20000, 20000, 25000, 25000,
                50000, 75000, 100000, 125000, 150000, 190000, 200000, 250000, 300000, 350000,
                500000, 500000, 750000, 1000000, 1250000, 1500000, 2000000, 2500000, 1000000, 1000000};
            return xpTable[level - 1];
        }
    }
}