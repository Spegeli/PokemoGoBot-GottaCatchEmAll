#region

using System;
using System.Linq;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Enums;
using System.Globalization;
using PokemonGo.RocketAPI.Helpers;
using POGOProtos.Networking.Responses;

#endregion


namespace PokemonGo.RocketAPI.Logic.Utils
{
    public class BotStats
    {
        public static string _exportStats;
        public static string _playerName;
        public static string CurrentLevelInfos;
        public static int Currentlevel = -1;

        public static int ExperienceThisSession;
        public static int ItemsRemovedThisSession;
        public static int PokemonCaughtThisSession;
        public static int PokemonTransferedThisSession;

        public static int TotalStardust;
        public static int TotalPokesInBag;

        public static int TotalPokesInPokedex;
        public static int TotalPokesInPokedexCaptured;

        public static float KmWalkedOnStart;
        public static float KmWalkedCurrent;

        public static DateTime InitSessionDateTime = DateTime.Now;
        public static TimeSpan Duration = DateTime.Now - InitSessionDateTime;

        public static string GetCurrentInfo()
        {
            var stats = Inventory.GetPlayerStats().Result;
            var output = string.Empty;
            var stat = stats.FirstOrDefault();
            if (stat == null) return output;

            KmWalkedCurrent = stat.KmWalked - KmWalkedOnStart;

            var ep = stat.NextLevelXp - stat.PrevLevelXp - (stat.Experience - stat.PrevLevelXp);
            var time = Math.Round(ep / (ExperienceThisSession / GetRuntime()), 2);
            var hours = 0.00;
            var minutes = 0.00;
            if (double.IsInfinity(time) == false && time > 0)
            {
                hours = Math.Truncate(TimeSpan.FromHours(time).TotalHours);
                minutes = TimeSpan.FromHours(time).Minutes;
            }

            return $"{stat.Level} (LvLUp in {hours}h {minutes}m | {stat.Experience - stat.PrevLevelXp - GetXpDiff(stat.Level)}/{stat.NextLevelXp - stat.PrevLevelXp - GetXpDiff(stat.Level)} XP)";
        }

        public static string GetUsername(GetPlayerResponse profile)
        {
            return _playerName = Logic._client.Settings.AuthType == AuthType.Ptc ? Logic._client.Settings.PtcUsername : profile.PlayerData.Username;
        }

        public static double GetRuntime()
        {
            return (DateTime.Now - InitSessionDateTime).TotalSeconds / 3600;
        }

        public static string FormatRuntime()
        {
            return (DateTime.Now - InitSessionDateTime).ToString(@"dd\.hh\:mm\:ss");
        }

        public static async Task GetPokemonCount()
        {
            var pokes = await Inventory.GetPokemons();
            TotalPokesInBag = pokes.Count();
        }

        public static async Task GetPokeDexCount()
        {
            var PokeDex = await Inventory.GetPokeDexItems();
            var _totalUniqueEncounters = PokeDex.Select(i => new { Pokemon = i.InventoryItemData.PokedexEntry.PokemonId, Captures = i.InventoryItemData.PokedexEntry.TimesCaptured });
            TotalPokesInPokedexCaptured = _totalUniqueEncounters.Count(i => i.Captures > 0);
            TotalPokesInPokedex = PokeDex.Count();
        }

        public static async Task UpdateConsoleTitle()
        {
            Console.Title = string.Format(
                "{0} - Runtime {1} - Lvl: {2:0} | EXP/H: {3:0} | P/H: {4:0} | Stardust: {5:0} | Transfered: {6:0} | Items Recycled: {7:0} | Pokemon: {8:0} | Pokedex: [Captured: {9:0} - Saw: {10:0}] | Km Walked this Session: {11:0.00} | Bot Version: {12:0}",
                _playerName, FormatRuntime(), GetCurrentInfo(), ExperienceThisSession / GetRuntime(),
                PokemonCaughtThisSession / GetRuntime(), TotalStardust, PokemonTransferedThisSession, ItemsRemovedThisSession, TotalPokesInBag, TotalPokesInPokedexCaptured, TotalPokesInPokedex, KmWalkedCurrent, GitChecker.CurrentVersion);
			await Task.Delay (0);
        }

        public static int GetXpDiff(int level)
        {
            if (level <= 0 || level > 40) return 0;
            int[] xpTable = { 0, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000,
                10000, 10000, 10000, 10000, 15000, 20000, 20000, 20000, 25000, 25000,
                50000, 75000, 100000, 125000, 150000, 190000, 200000, 250000, 300000, 350000,
                500000, 500000, 750000, 1000000, 1250000, 1500000, 2000000, 2500000, 3000000, 5000000};
            return xpTable[level - 1];
        }
    }
}