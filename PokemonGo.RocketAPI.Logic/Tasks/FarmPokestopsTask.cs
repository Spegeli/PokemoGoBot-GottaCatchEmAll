using System;
using System.Collections.Generic;
//using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Helpers;
using PokemonGo.RocketAPI.Logic.Utils;
using PokemonGo.RocketAPI.Logic;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

namespace PokemonGo.RocketAPI.Logic.Tasks
{
    public class FarmPokestopsTask
    {
        public static async Task Execute()
        {
            var distanceFromStart = LocationUtils.CalculateDistanceInMeters(
                Logic._client.Settings.DefaultLatitude, Logic._client.Settings.DefaultLongitude,
                Logic._client.CurrentLatitude, Logic._client.CurrentLongitude);

            // Edge case for when the client somehow ends up outside the defined radius
            if (Logic._client.Settings.MaxTravelDistanceInMeters != 0 &&
                distanceFromStart > Logic._client.Settings.MaxTravelDistanceInMeters)
            {
                Logger.Write(
                    $"You're outside of your defined radius! Walking to Default Coords ({distanceFromStart:0.##}m away). Is your LastCoords.ini file correct?",
                    LogLevel.Warning);
                await Navigation.HumanLikeWalking(
                    new GeoUtils(Logic._client.Settings.DefaultLatitude, Logic._client.Settings.DefaultLongitude),
                    async () =>
                    {
                        // Catch normal map Pokemon
                        await CatchMapPokemonsTask.Execute();
                        //Catch Incense Pokemon
                        await CatchIncensePokemonsTask.Execute();
                        return true;
                    });
            }

            var pokestops = await Inventory.GetPokestops();

            if (pokestops == null || !pokestops.Any())
                Logger.Write("No usable PokeStops found in your area. Reasons: Softbanned - Server Issues - MaxTravelDistanceInMeters too small",
                    LogLevel.Warning);
            else
                Logger.Write($"Found {pokestops.Count()} {(pokestops.Count() == 1 ? "Pokestop" : "Pokestops")}", LogLevel.Info);

            while (pokestops.Any())
            {
                if (Logic._client.Settings.ExportPokemonToCsvEveryMinutes > 0 && ExportPokemonToCsv._lastExportTime.AddMinutes(Logic._client.Settings.ExportPokemonToCsvEveryMinutes).Ticks < DateTime.Now.Ticks)
                {
                    var _playerProfile = await Logic._client.Player.GetPlayer();
                    await ExportPokemonToCsv.Execute(_playerProfile.PlayerData);
                }
                if (Logic._client.Settings.UseLuckyEggs)
                    await UseLuckyEggTask.Execute();
                if (Logic._client.Settings.CatchIncensePokemon)
                    await UseIncenseTask.Execute();

                var pokestopwithcooldown = pokestops.Where(p => p.CooldownCompleteTimestampMs > DateTime.UtcNow.ToUnixTime()).FirstOrDefault();
                if (pokestopwithcooldown != null)
                    pokestops.Remove(pokestopwithcooldown);

                var pokestop =
                    pokestops.Where(p => p.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime())
                        .OrderBy(
                            i =>
                                LocationUtils.CalculateDistanceInMeters(Logic._client.CurrentLatitude,
                                    Logic._client.CurrentLongitude, i.Latitude, i.Longitude)).First();

                var lured = string.Empty;
                var distance = LocationUtils.CalculateDistanceInMeters(Logic._client.CurrentLatitude, Logic._client.CurrentLongitude, pokestop.Latitude, pokestop.Longitude);
                if (distance > 100)
                {
                    var lurePokestop = pokestops.FirstOrDefault(x => x.LureInfo != null);
                    if (lurePokestop != null)
                    {
                        distance = LocationUtils.CalculateDistanceInMeters(Logic._client.CurrentLatitude, Logic._client.CurrentLongitude, pokestop.Latitude, pokestop.Longitude);
                        if (distance < 200)
                        {
                            lured = " is Lured";
                            pokestop = lurePokestop;
                        }
                        else
                            pokestops.Remove(pokestop);
                    }
                } else
                    pokestops.Remove(pokestop);

                var fortInfo = await Logic._client.Fort.GetFort(pokestop.Id, pokestop.Latitude, pokestop.Longitude);
                var latlngDebug = string.Empty;
                if (Logic._client.Settings.DebugMode)
                    latlngDebug = $" | Latitude: {pokestop.Latitude} - Longitude: {pokestop.Longitude}";
                Logger.Write($"Name: {fortInfo.Name} in {distance:0.##} m distance{lured}{latlngDebug}", LogLevel.Pokestop);

                if (Logic._client.Settings.UseTeleportInsteadOfWalking)
                {
                    await
                        Logic._client.Player.UpdatePlayerLocation(pokestop.Latitude, pokestop.Longitude,
                            Logic._client.Settings.DefaultAltitude);
                    await RandomHelper.RandomDelay(500);
                    Logger.Write($"Using Teleport instead of Walking!", LogLevel.Navigation);
                }
                else
                {
                    await
                        Navigation.HumanLikeWalking(new GeoUtils(pokestop.Latitude, pokestop.Longitude),
                            async () =>
                            {
                                // Catch normal map Pokemon
                                await CatchMapPokemonsTask.Execute();
                                //Catch Incense Pokemon
                                await CatchIncensePokemonsTask.Execute();
                                return true;
                            });
                }

                //Catch Lure Pokemon
                if (pokestop.LureInfo != null && Logic._client.Settings.CatchLuredPokemon)
                {
                    await CatchLurePokemonsTask.Execute(pokestop);
                }

                var timesZeroXPawarded = 0;
                var fortTry = 0;      //Current check
                const int retryNumber = 45; //How many times it needs to check to clear softban
                const int zeroCheck = 5; //How many times it checks fort before it thinks it's softban
                do
                {
                    var fortSearch = await Logic._client.Fort.SearchFort(pokestop.Id, pokestop.Latitude, pokestop.Longitude);

                    if (fortSearch.ExperienceAwarded > 0 && timesZeroXPawarded > 0) timesZeroXPawarded = 0;
                    if (fortSearch.ExperienceAwarded == 0)
                    {
                        if (fortSearch.Result == FortSearchResponse.Types.Result.InCooldownPeriod)
                        {
                            Logger.Write("Pokestop is on Cooldown", LogLevel.Debug);
                            break;
                        }

                        timesZeroXPawarded++;
                        if (timesZeroXPawarded > zeroCheck)
                        {
                            fortTry += 1;

                            if (Logic._client.Settings.DebugMode)
                                Logger.Write(
                                    $"Seems your Soft-Banned. Trying to Unban via Pokestop Spins. Retry {fortTry} of {retryNumber - zeroCheck}",
                                    LogLevel.Warning);

                            await RandomHelper.RandomDelay(450);
                        }
                    }
                    else if (fortSearch.ExperienceAwarded != 0)
                    {
                        BotStats.ExperienceThisSession += fortSearch.ExperienceAwarded;
                        await BotStats.UpdateConsoleTitle();
                        Logger.Write($"XP: {fortSearch.ExperienceAwarded}, Gems: {fortSearch.GemsAwarded}, Items: {StringUtils.GetSummedFriendlyNameOfItemAwardList(fortSearch.ItemsAwarded)}", LogLevel.Pokestop);
                        RecycleItemsTask._recycleCounter++;
                        HatchEggsTask._hatchUpdateDelay++;
                        break; //Continue with program as loot was succesfull.
                    }
                } while (fortTry < retryNumber - zeroCheck); //Stop trying if softban is cleaned earlier or if 40 times fort looting failed.

                if (RecycleItemsTask._recycleCounter >= 5)
                    await RecycleItemsTask.Execute();
                if (HatchEggsTask._hatchUpdateDelay >= 15)
                    await HatchEggsTask.Execute();
            }
        }
    }
}
