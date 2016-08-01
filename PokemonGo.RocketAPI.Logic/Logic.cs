#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.GeneratedCode;
using PokemonGo.RocketAPI.Logic.Utils;
using PokemonGo.RocketAPI.Helpers;
using System.IO;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Logging;
using System.Diagnostics;

#endregion


namespace PokemonGo.RocketAPI.Logic
{
    public class Logic
    {
        private readonly Client _client;
        private readonly ISettings _clientSettings;
        private readonly Inventory _inventory;
        private readonly BotStats _stats;
        private readonly Navigation _navigation;
        private GetPlayerResponse _playerProfile;
        private static DateTime _lastLuckyEggTime;
        private static DateTime _lastIncenseTime;
        private readonly string _configsPath = Path.Combine(Directory.GetCurrentDirectory(), "Settings");

        private int _recycleCounter = 0;
        private bool _isInitialized = false;

        public Logic(ISettings clientSettings)
        {
            _clientSettings = clientSettings;
            ResetCoords();
            _client = new Client(_clientSettings);
            _inventory = new Inventory(_client);
            _stats = new BotStats();
            _navigation = new Navigation(_client);
        }

        public async Task Execute()
        {
            if (!_isInitialized)
            {
                GitChecker.CheckVersion();

                if (Math.Abs(_clientSettings.DefaultLatitude) <= 0  || Math.Abs(_clientSettings.DefaultLongitude) <= 0)
                {
                    Logger.Write($"Please change first Latitude and/or Longitude because currently your using default values!", LogLevel.Error);
                    Environment.Exit(1);
                }
                else
                {
                    Logger.Write($"Make sure Lat & Lng is right. Exit Program if not! Lat: {_client.CurrentLat} Lng: {_client.CurrentLng}", LogLevel.Warning);
                    for (int i = 3; i > 0; i--)
                    {
                        Logger.Write($"Script will continue in {i * 5} seconds!", LogLevel.Warning);
                        await Task.Delay(5000);
                    }
                }
            }
            Logger.Write($"Logging in via: {_clientSettings.AuthType}", LogLevel.Info);
            while (true)
            {
                try
                {
                    switch (_clientSettings.AuthType)
                    {
                        case AuthType.Ptc:
                            await _client.DoPtcLogin(_clientSettings.PtcUsername, _clientSettings.PtcPassword);
                            break;
                        case AuthType.Google:
                            await _client.DoGoogleLogin(_clientSettings.GoogleEmail, _clientSettings.GooglePassword);
                            break;
                        default:
                            Logger.Write("wrong AuthType");
                            Environment.Exit(0);
                            break;
                    }

                    await _client.SetServer();

                    await PostLoginExecute();
                }
                catch (AccountNotVerifiedException)
                {
                    Logger.Write("Account not verified! Exiting...", LogLevel.Error);
                    await Task.Delay(5000);
                    Environment.Exit(0);
                }
                catch (GoogleException e)
                {
                    if (e.Message.Contains("NeedsBrowser"))
                    {
                        Logger.Write("As you have Google Two Factor Auth enabled, you will need to insert an App Specific Password into the UserSettings.", LogLevel.Error);
                        Logger.Write("Opening Google App-Passwords. Please make a new App Password (use Other as Device)", LogLevel.Error);
                        await Task.Delay(7000);
                        try
                        {
                            Process.Start("https://security.google.com/settings/security/apppasswords");
                        }
                        catch (Exception)
                        {
                            Logger.Write("https://security.google.com/settings/security/apppasswords");
                            throw;
                        }
                    }
                    Logger.Write("Make sure you have entered the right Email & Password.", LogLevel.Error);
                    await Task.Delay(5000);
                    Environment.Exit(0);
                }
                catch (InvalidProtocolBufferException ex) when (ex.Message.Contains("SkipLastField"))
                {
                    Logger.Write("Connection refused. Your IP might have been Blacklisted by Niantic. Exiting..", LogLevel.Error);
                    await Task.Delay(5000);
                    Environment.Exit(0);
                }
                catch (Exception e)
                {
                    Logger.Write(e.Message + " from " + e.Source);
                    Logger.Write("Error, trying automatic restart..", LogLevel.Error);
                    await Execute();
                }
                await Task.Delay(10000);
            }
        }

        public async Task RefreshTokens()
        {
            switch (_clientSettings.AuthType)
            {
                case AuthType.Ptc:
                    await _client.DoPtcLogin(_clientSettings.PtcUsername, _clientSettings.PtcPassword);
                    break;
                case AuthType.Google:
                    await _client.DoGoogleLogin(_clientSettings.GoogleEmail, _clientSettings.GooglePassword);
                    break;
            }

            await _client.SetServer();
        }

        public async Task PostLoginExecute()
        {
            Logger.Write($"Client logged in", LogLevel.Info);

            while (true)
            {
                if (!_isInitialized)
                {
                    await Inventory.GetCachedInventory(_client);
                    _playerProfile = await _client.GetProfile();
                    var playerName = BotStats.GetUsername(_client, _playerProfile);
                    _stats.UpdateConsoleTitle(_client, _inventory);
                    var currentLevelInfos = await BotStats._getcurrentLevelInfos(_inventory);

                    var stats = await _inventory.GetPlayerStats();
                    var stat = stats.FirstOrDefault();
                    if (stat != null) BotStats.KmWalkedOnStart = stat.KmWalked;

                    Logger.Write("----------------------------", LogLevel.None, ConsoleColor.Yellow);
                    if (_clientSettings.AuthType == AuthType.Ptc)
                        Logger.Write($"PTC Account: {playerName}\n", LogLevel.None, ConsoleColor.Cyan);
                    Logger.Write($"Latitude: {_clientSettings.DefaultLatitude}", LogLevel.None, ConsoleColor.DarkGray);
                    Logger.Write($"Longitude: {_clientSettings.DefaultLongitude}", LogLevel.None, ConsoleColor.DarkGray);
                    Logger.Write("----------------------------", LogLevel.None, ConsoleColor.Yellow);
                    Logger.Write("Your Account:\n");
                    Logger.Write($"Name: {playerName}", LogLevel.None, ConsoleColor.DarkGray);
                    Logger.Write($"Team: {_playerProfile.Profile.Team}", LogLevel.None, ConsoleColor.DarkGray);
                    Logger.Write($"Level: {currentLevelInfos}", LogLevel.None, ConsoleColor.DarkGray);
                    Logger.Write($"Stardust: {_playerProfile.Profile.Currency.ToArray()[1].Amount}", LogLevel.None, ConsoleColor.DarkGray);
                    Logger.Write("----------------------------", LogLevel.None, ConsoleColor.Yellow);
                    await DisplayHighests();
                    Logger.Write("----------------------------", LogLevel.None, ConsoleColor.Yellow);

                    var pokemonsToNotTransfer = _clientSettings.PokemonsToNotTransfer;
                    var pokemonsToNotCatch = _clientSettings.PokemonsToNotCatch;
                    var pokemonsToEvolve = _clientSettings.PokemonsToEvolve;

                    if (_clientSettings.UseLuckyEggs)
                        await UseLuckyEgg();
                    if (_clientSettings.EvolvePokemon || _clientSettings.EvolveOnlyPokemonAboveIV) await EvolvePokemon();
                    if (_clientSettings.TransferPokemon) await TransferPokemon();
                    await _inventory.ExportPokemonToCsv(_playerProfile.Profile);
                    await RecycleItems();
                }
                _isInitialized = true;
                await ExecuteFarmingPokestopsAndPokemons(_clientSettings.UseGPXPathing);

                await RefreshTokens();
                /*
                * Example calls below
                *
                var profile = await _client.GetProfile();
                var settings = await _client.GetSettings();
                var mapObjects = await _client.GetMapObjects();
                var inventory = await _client.GetInventory();
                var pokemons = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon).Where(p => p != null && p?.PokemonId > 0);
                */

                await Task.Delay(10000);
            }
        }

        private async Task ExecuteFarmingPokestopsAndPokemons(bool path)
        {
            if (!path)
                await ExecuteFarmingPokestopsAndPokemons();
            else
            {
                var tracks = GetGpxTracks();
                var curTrkPt = 0;
                var curTrk = 0;
                var maxTrk = tracks.Count - 1;
                var curTrkSeg = 0;
                while (curTrk <= maxTrk)
                {
                    var track = tracks.ElementAt(curTrk);
                    var trackSegments = track.Segments;
                    var maxTrkSeg = trackSegments.Count - 1;
                    while (curTrkSeg <= maxTrkSeg)
                    {
                        var trackPoints = track.Segments.ElementAt(0).TrackPoints;
                        var maxTrkPt = trackPoints.Count - 1;
                        while (curTrkPt <= maxTrkPt)
                        {
                            var nextPoint = trackPoints.ElementAt(curTrkPt);
                            var distanceCheck = LocationUtils.CalculateDistanceInMeters(_client.CurrentLat,
                                _client.CurrentLng, Convert.ToDouble(nextPoint.Lat), Convert.ToDouble(nextPoint.Lon));

                            if (distanceCheck > 5000)
                            {
                                Logger.Write(
                                    $"Your desired destination of {nextPoint.Lat}, {nextPoint.Lon} is too far from your current position of {_client.CurrentLat}, {_client.CurrentLng}",
                                    LogLevel.Error);
                                break;
                            }

                            Logger.Write(
                                $"Your desired destination is {nextPoint.Lat}, {nextPoint.Lon} your location is {_client.CurrentLat}, {_client.CurrentLng}",
                                LogLevel.Warning);

                            if (_clientSettings.UseLuckyEggs)
                                await UseLuckyEgg();
                            if (_clientSettings.UseIncense)
                                await UseIncense();

                            if (!_clientSettings.GPXIgnorePokemon)
                                await ExecuteCatchAllNearbyPokemons();

                            if (!_clientSettings.GPXIgnorePokestops)
                            {
                                var pokeStops = await _inventory.GetPokestops();
                                var pokestopList = pokeStops.ToList();

                                while (pokestopList.Any())
                                {
                                    pokestopList =
                                        pokestopList.OrderBy(
                                            i =>
                                                LocationUtils.CalculateDistanceInMeters(_client.CurrentLat,
                                                    _client.CurrentLng, i.Latitude, i.Longitude)).ToList();
                                    var pokeStop = pokestopList.First();
                                    pokestopList.Remove(pokeStop);

                                    var distance = LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng, pokeStop.Latitude, pokeStop.Longitude);
                                    var fortInfo = await _client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                                    Logger.Write($"Name: {fortInfo.Name} in {distance:0.##} m distance", LogLevel.Pokestop);
                                    if (_client.Settings.DebugMode)
                                        Logger.Write($"Latitude: {pokeStop.Latitude} - Longitude: {pokeStop.Longitude}", LogLevel.Debug);

                                    var timesZeroXPawarded = 0;
                                    var fortTry = 0;      //Current check
                                    const int retryNumber = 50; //How many times it needs to check to clear softban
                                    const int zeroCheck = 5; //How many times it checks fort before it thinks it's softban
                                    do
                                    {
                                        var fortSearch = await _client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                                        if (fortSearch.ExperienceAwarded > 0 && timesZeroXPawarded > 0) timesZeroXPawarded = 0;
                                        if (fortSearch.ExperienceAwarded == 0)
                                        {
                                            timesZeroXPawarded++;

                                            if (timesZeroXPawarded <= zeroCheck) continue;
                                            if ((int)fortSearch.CooldownCompleteTimestampMs != 0)
                                            {
                                                break; // Check if successfully looted, if so program can continue as this was "false alarm".
                                            }
                                            fortTry += 1;

                                            if (_client.Settings.DebugMode)
                                                Logger.Write($"Seems your Soft-Banned. Trying to Unban via Pokestop Spins. Retry {fortTry} of {retryNumber}", LogLevel.Warning);

                                            await RandomHelper.RandomDelay(75, 100);
                                        }
                                        else
                                        {
                                            _stats.AddExperience(fortSearch.ExperienceAwarded);
                                            _stats.UpdateConsoleTitle(_client, _inventory);
                                            var eggReward = fortSearch.PokemonDataEgg != null ? "1" : "0";
                                            Logger.Write($"XP: {fortSearch.ExperienceAwarded}, Gems: {fortSearch.GemsAwarded}, Eggs: {eggReward}, Items: {StringUtils.GetSummedFriendlyNameOfItemAwardList(fortSearch.ItemsAwarded)}", LogLevel.Pokestop);
                                            _recycleCounter++;
                                            break; //Continue with program as loot was succesfull.
                                        }
                                    } while (fortTry < retryNumber - zeroCheck); //Stop trying if softban is cleaned earlier or if 40 times fort looting failed.

                                    if (_recycleCounter >= 5)
                                        await RecycleItems();
                                }
                            }

                            if (!_clientSettings.GPXIgnorePokemon)
                                await
                                    _navigation.HumanPathWalking(trackPoints.ElementAt(curTrkPt),
                                    _clientSettings.WalkingSpeedInKilometerPerHour, ExecuteCatchAllNearbyPokemons);
                            else
                                await
                                    _navigation.HumanPathWalking(trackPoints.ElementAt(curTrkPt),
                                    _clientSettings.WalkingSpeedInKilometerPerHour, null);

                            if (curTrkPt >= maxTrkPt)
                                curTrkPt = 0;
                            else
                                curTrkPt++;
                        } //end trkpts
                        if (curTrkSeg >= maxTrkSeg)
                            curTrkSeg = 0;
                        else
                            curTrkSeg++;
                    } //end trksegs
                    if (curTrk >= maxTrkSeg)
                        curTrk = 0;
                    else
                        curTrk++;
                } //end tracks
            }
        }

        private async Task ExecuteFarmingPokestopsAndPokemons()
        {
            var distanceFromStart = LocationUtils.CalculateDistanceInMeters(
                _clientSettings.DefaultLatitude, _clientSettings.DefaultLongitude,
                _client.CurrentLat, _client.CurrentLng);

            // Edge case for when the client somehow ends up outside the defined radius
            if (_clientSettings.MaxTravelDistanceInMeters != 0 &&
                distanceFromStart > _clientSettings.MaxTravelDistanceInMeters)
            {
                Logger.Write(
                    $"You're outside of your defined radius! Walking to start ({distanceFromStart:0.##}m away) in 5 seconds. Is your LastCoords.ini file correct?",
                    LogLevel.Warning);
                await Task.Delay(5000);
                Logger.Write("Moving to start location now.");
                await _navigation.HumanLikeWalking(
                    new GeoUtils(_clientSettings.DefaultLatitude, _clientSettings.DefaultLongitude),
                    _clientSettings.WalkingSpeedInKilometerPerHour, ExecuteCatchAllNearbyPokemons);
            }

            var pokeStops = await _inventory.GetPokestops();
            var pokestopList = pokeStops.ToList();
            if (pokestopList.Count <= 0)
                Logger.Write("No usable PokeStops found in your area. Is your maximum distance too small?",
                    LogLevel.Warning);
            else
                Logger.Write($"Found {pokeStops.Count()} {(pokeStops.Count() == 1 ? "Pokestop" : "Pokestops")}", LogLevel.Info);

            while (pokestopList.Any())
            {
                if (_clientSettings.UseLuckyEggs)
                    await UseLuckyEgg();
                if (_clientSettings.UseIncense)
                    await UseIncense();

                await ExecuteCatchAllNearbyPokemons();

                pokestopList =
                    pokestopList.OrderBy(
                        i =>
                            LocationUtils.CalculateDistanceInMeters(_client.CurrentLat,
                                _client.CurrentLng, i.Latitude, i.Longitude)).ToList();
                var pokeStop = pokestopList.First();
                pokestopList.Remove(pokeStop);

                var distance = LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng, pokeStop.Latitude, pokeStop.Longitude);
                var fortInfo = await _client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                var latlngDebug = string.Empty;
                if (_clientSettings.DebugMode)
                    latlngDebug = $"| Latitude: {pokeStop.Latitude} - Longitude: {pokeStop.Longitude}";

                Logger.Write($"Name: {fortInfo.Name} in {distance:0.##} m distance {latlngDebug}", LogLevel.Pokestop);

                if (_clientSettings.UseTeleportInsteadOfWalking)
                {
                    await
                        _client.UpdatePlayerLocation(pokeStop.Latitude, pokeStop.Longitude,
                            _clientSettings.DefaultAltitude);
                    Logger.Write($"Using Teleport instead of Walking!", LogLevel.Debug);
                }
                else
                {
                    await
                        _navigation.HumanLikeWalking(new GeoUtils(pokeStop.Latitude, pokeStop.Longitude),
                            _clientSettings.WalkingSpeedInKilometerPerHour, ExecuteCatchAllNearbyPokemons);
                }

                var timesZeroXPawarded = 0;
                var fortTry = 0;      //Current check
                const int retryNumber = 45; //How many times it needs to check to clear softban
                const int zeroCheck = 5; //How many times it checks fort before it thinks it's softban
                do
                {
                    var fortSearch = await _client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                    if (fortSearch.ExperienceAwarded > 0 && timesZeroXPawarded > 0) timesZeroXPawarded = 0;
                    if (fortSearch.ExperienceAwarded == 0)
                    {
                        timesZeroXPawarded++;

                        if (timesZeroXPawarded <= zeroCheck) continue;
                        if ((int)fortSearch.CooldownCompleteTimestampMs != 0)
                        {
                            break; // Check if successfully looted, if so program can continue as this was "false alarm".
                        }
                        fortTry += 1;

                        if (_client.Settings.DebugMode)
                            Logger.Write($"Seems your Soft-Banned. Trying to Unban via Pokestop Spins. Retry {fortTry} of {retryNumber-zeroCheck}", LogLevel.Warning);

                        await RandomHelper.RandomDelay(75, 100);
                    }
                    else
                    {
                        _stats.AddExperience(fortSearch.ExperienceAwarded);
                        _stats.UpdateConsoleTitle(_client, _inventory);
                        var eggReward = fortSearch.PokemonDataEgg != null ? "1" : "0";
                        Logger.Write($"XP: {fortSearch.ExperienceAwarded}, Gems: {fortSearch.GemsAwarded}, Eggs: {eggReward}, Items: {StringUtils.GetSummedFriendlyNameOfItemAwardList(fortSearch.ItemsAwarded)}", LogLevel.Pokestop);
                        _recycleCounter++;
                        break; //Continue with program as loot was succesfull.
                    }
                } while (fortTry < retryNumber - zeroCheck); //Stop trying if softban is cleaned earlier or if 40 times fort looting failed.

                if (_recycleCounter >= 5)
                    await RecycleItems();
            }
        }

        private async Task CatchEncounter(EncounterResponse encounter, MapPokemon pokemon)
        {
            CatchPokemonResponse caughtPokemonResponse;
            var attemptCounter = 1;
            do
            {
                var probability = encounter?.CaptureProbability?.CaptureProbability_?.FirstOrDefault();
                var bestPokeball = await GetBestBall(encounter);
                if (bestPokeball == MiscEnums.Item.ITEM_UNKNOWN)
                {
                    Logger.Write($"You don't own any Pokeballs :( - We missed a {pokemon.PokemonId} with CP {encounter?.WildPokemon?.PokemonData?.Cp}", LogLevel.Warning);
                    return;
                }
                var bestBerry = await GetBestBerry(encounter);
                var inventoryBerries = await _inventory.GetItems();
                var berries = inventoryBerries.FirstOrDefault(p => (ItemId)p.Item_ == bestBerry);
                if (bestBerry != ItemId.ItemUnknown && probability.HasValue && probability.Value < 0.35)
                {
                    await _client.UseCaptureItem(pokemon.EncounterId, bestBerry, pokemon.SpawnpointId);
                    berries.Count--;
                    Logger.Write($"{bestBerry} used, remaining: {berries.Count}", LogLevel.Berry);
                }

                var distance = LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng, pokemon.Latitude, pokemon.Longitude);
                caughtPokemonResponse = await _client.CatchPokemon(pokemon.EncounterId, pokemon.SpawnpointId, pokemon.Latitude, pokemon.Longitude, bestPokeball);

                if (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess)
                {
                    foreach (var xp in caughtPokemonResponse.Scores.Xp)
                        _stats.AddExperience(xp);
                    _stats.IncreasePokemons();
                    var profile = await _client.GetProfile();
                    _stats.GetStardust(profile.Profile.Currency.ToArray()[1].Amount);
                }
                _stats.UpdateConsoleTitle(_client, _inventory);

                if (encounter?.CaptureProbability?.CaptureProbability_ != null)
                {
                    Func<MiscEnums.Item, string> returnRealBallName = a =>
                    {
                        switch (a)
                        {
                            case MiscEnums.Item.ITEM_POKE_BALL:
                                return "Poke";
                            case MiscEnums.Item.ITEM_GREAT_BALL:
                                return "Great";
                            case MiscEnums.Item.ITEM_ULTRA_BALL:
                                return "Ultra";
                            case MiscEnums.Item.ITEM_MASTER_BALL:
                                return "Master";
                            default:
                                return "Unknown";
                        }
                    };
                    var catchStatus = attemptCounter > 1
                        ? $"{caughtPokemonResponse.Status} Attempt #{attemptCounter}"
                        : $"{caughtPokemonResponse.Status}";

                    var receivedXp = caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess
                        ? $"and received XP {caughtPokemonResponse.Scores.Xp.Sum()}" 
                        : $"";

                    Logger.Write($"({catchStatus}) | {pokemon.PokemonId} - Lvl {PokemonInfo.GetLevel(encounter?.WildPokemon?.PokemonData)} [CP {encounter?.WildPokemon?.PokemonData?.Cp}/{PokemonInfo.CalculateMaxCp(encounter?.WildPokemon?.PokemonData)} | IV: {PokemonInfo.CalculatePokemonPerfection(encounter?.WildPokemon?.PokemonData).ToString("0.00")}% perfect] | Chance: {(float)((int)(encounter?.CaptureProbability?.CaptureProbability_.First() * 100)) / 100} | {distance:0.##}m dist | with a {returnRealBallName(bestPokeball)}Ball {receivedXp}", LogLevel.Pokemon);
                }

                attemptCounter++;
            }
            while (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchMissed || caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchEscape);
        }

        private async Task ExecuteCatchAllNearbyPokemons()
        {
            var mapObjects = await _client.GetMapObjects();

            var pokemons =
                mapObjects.MapCells.SelectMany(i => i.CatchablePokemons)
                .OrderBy(
                    i =>
                    LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng, i.Latitude, i.Longitude)).ToList();
            if (_clientSettings.UsePokemonToNotCatchList)
            {
                ICollection<PokemonId> filter = _clientSettings.PokemonsToNotCatch;
                pokemons = pokemons.Where(p => !filter.Contains(p.PokemonId)).ToList();
           }

            if (pokemons.Any())
                Logger.Write($"Found {pokemons.Count()} catchable Pokemon", LogLevel.Info);
            else
                return;

            foreach (var pokemon in pokemons)
            {
                var encounter = await _client.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnpointId);

                if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                    await CatchEncounter(encounter, pokemon);
                else
                    Logger.Write($"Encounter problem: {encounter.Status}", LogLevel.Warning);
            }

            if (_clientSettings.EvolvePokemon || _clientSettings.EvolveOnlyPokemonAboveIV) await EvolvePokemon();
            if (_clientSettings.TransferPokemon) await TransferPokemon();
        }

        private async Task EvolvePokemon()
        {
            await Inventory.GetCachedInventory(_client, true);
            var pokemonToEvolve = await _inventory.GetPokemonToEvolve(_clientSettings.PrioritizeIVOverCP, _clientSettings.PokemonsToEvolve);
            if (pokemonToEvolve != null && pokemonToEvolve.Any())
                Logger.Write($"Found {pokemonToEvolve.Count()} Pokemon for Evolve:", LogLevel.Info);

            foreach (var pokemon in pokemonToEvolve)
            {
                var evolvePokemonOutProto = await _client.EvolvePokemon(pokemon.Id);

                await Inventory.GetCachedInventory(_client, true);

                Logger.Write(
                    evolvePokemonOutProto.Result == EvolvePokemonOut.Types.EvolvePokemonStatus.PokemonEvolvedSuccess
                        ? $"{pokemon.PokemonId} successfully for {evolvePokemonOutProto.ExpAwarded} xp"
                        : $"Failed: {pokemon.PokemonId}. EvolvePokemonOutProto.Result was {evolvePokemonOutProto.Result}, stopping evolving {pokemon.PokemonId}"
                    , LogLevel.Evolve);
            }
        }

        private async Task TransferPokemon()
        {
            await Inventory.GetCachedInventory(_client, true);
            var pokemonToTransfer = await _inventory.GetPokemonToTransfer(_clientSettings.NotTransferPokemonsThatCanEvolve, _clientSettings.PrioritizeIVOverCP, _clientSettings.PokemonsToNotTransfer);
            if (pokemonToTransfer != null && pokemonToTransfer.Any())
                Logger.Write($"Found {pokemonToTransfer.Count()} Pokemon for Transfer:", LogLevel.Info);

            foreach (var pokemon in pokemonToTransfer)
            {
                await _client.TransferPokemon(pokemon.Id);

                await Inventory.GetCachedInventory(_client, true);
                var myPokemonSettings = await _inventory.GetPokemonSettings();
                var pokemonSettings = myPokemonSettings.ToList();
                var myPokemonFamilies = await _inventory.GetPokemonFamilies();
                var pokemonFamilies = myPokemonFamilies.ToArray();
                var settings = pokemonSettings.Single(x => x.PokemonId == pokemon.PokemonId);
                var familyCandy = pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId);
                var familyCandies = $"{familyCandy.Candy}";

                _stats.IncreasePokemonsTransfered();
                _stats.UpdateConsoleTitle(_client, _inventory);

                var bestPokemonOfType = _client.Settings.PrioritizeIVOverCP
                    ? await _inventory.GetHighestPokemonOfTypeByIv(pokemon)
                    : await _inventory.GetHighestPokemonOfTypeByCp(pokemon);
                var bestPokemonInfo = "NONE";
                if (bestPokemonOfType != null)
                    bestPokemonInfo = $"CP: {bestPokemonOfType.Cp}/{PokemonInfo.CalculateMaxCp(bestPokemonOfType)} | IV: {PokemonInfo.CalculatePokemonPerfection(bestPokemonOfType).ToString("0.00")}% perfect";

                Logger.Write($"{pokemon.PokemonId} [CP {pokemon.Cp}/{PokemonInfo.CalculateMaxCp(pokemon)} | IV: { PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00")}% perfect] | Best: [{bestPokemonInfo}] | Family Candies: {familyCandies}", LogLevel.Transfer);
            }
        }

        private async Task RecycleItems()
        {
            await Inventory.GetCachedInventory(_client, true);
            var items = await _inventory.GetItemsToRecycle(_clientSettings);
            if (items != null && items.Any())
                Logger.Write($"Found {items.Count()} Recyclable {(items.Count() == 1 ? "Item" : "Items")}:", LogLevel.Info);

            foreach (var item in items)
            {
                await _client.RecycleItem((ItemId)item.Item_, item.Count);
                Logger.Write($"{item.Count}x {(ItemId)item.Item_}", LogLevel.Recycling);

                _stats.AddItemsRemoved(item.Count);
                _stats.UpdateConsoleTitle(_client, _inventory);
            }
            _recycleCounter = 0;
        }

        private async Task<MiscEnums.Item> GetBestBall(EncounterResponse encounter)
        {
            var pokemonCp = encounter?.WildPokemon?.PokemonData?.Cp;
            var iV = Math.Round(PokemonInfo.CalculatePokemonPerfection(encounter?.WildPokemon?.PokemonData));
            var proba = encounter?.CaptureProbability?.CaptureProbability_.First();

            var items = await _inventory.GetItems();
            var balls = items.Where(i => ((MiscEnums.Item)i.Item_ == MiscEnums.Item.ITEM_POKE_BALL
                                      || (MiscEnums.Item)i.Item_ == MiscEnums.Item.ITEM_GREAT_BALL
                                      || (MiscEnums.Item)i.Item_ == MiscEnums.Item.ITEM_ULTRA_BALL
                                      || (MiscEnums.Item)i.Item_ == MiscEnums.Item.ITEM_MASTER_BALL) && i.Count > 0).GroupBy(i => ((MiscEnums.Item)i.Item_)).ToList();
            if (balls.Count == 0) return MiscEnums.Item.ITEM_UNKNOWN;

            var pokeBalls = balls.Any(g => g.Key == MiscEnums.Item.ITEM_POKE_BALL);
            var greatBalls = balls.Any(g => g.Key == MiscEnums.Item.ITEM_GREAT_BALL);
            var ultraBalls = balls.Any(g => g.Key == MiscEnums.Item.ITEM_ULTRA_BALL);
            var masterBalls = balls.Any(g => g.Key == MiscEnums.Item.ITEM_MASTER_BALL);

            if (masterBalls && pokemonCp >= 1500)
                return MiscEnums.Item.ITEM_MASTER_BALL;

            if (ultraBalls && (pokemonCp >= 1000 || (iV >= _clientSettings.TransferPokemonKeepAboveIVPercentage && proba < 0.40)))
                return MiscEnums.Item.ITEM_ULTRA_BALL;

            if (greatBalls && (pokemonCp >= 300 || (iV >= _clientSettings.TransferPokemonKeepAboveIVPercentage && proba < 0.50)))
                return MiscEnums.Item.ITEM_GREAT_BALL;

            return balls.OrderBy(g => g.Key).First().Key;
        }

        private async Task<ItemId> GetBestBerry(EncounterResponse encounter)
        {
            var pokemonCp = encounter?.WildPokemon?.PokemonData?.Cp;
            var iV = Math.Round(PokemonInfo.CalculatePokemonPerfection(encounter?.WildPokemon?.PokemonData));
            var proba = encounter?.CaptureProbability?.CaptureProbability_.First();

            var items = await _inventory.GetItems();
            var berries = items.Where(i => ((ItemId)i.Item_ == ItemId.ItemRazzBerry
                                        || (ItemId)i.Item_ == ItemId.ItemBlukBerry
                                        || (ItemId)i.Item_ == ItemId.ItemNanabBerry
                                        || (ItemId)i.Item_ == ItemId.ItemWeparBerry
                                        || (ItemId)i.Item_ == ItemId.ItemPinapBerry) && i.Count > 0).GroupBy(i => ((ItemId)i.Item_)).ToList();
            if (berries.Count == 0 || pokemonCp < 150) return ItemId.ItemUnknown;

            var razzBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_RAZZ_BERRY);
            var blukBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_BLUK_BERRY);
            var nanabBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_NANAB_BERRY);
            var weparBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_WEPAR_BERRY);
            var pinapBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_PINAP_BERRY);

            if (pinapBerryCount > 0 && pokemonCp >= 2000)
                return ItemId.ItemPinapBerry;

            if (weparBerryCount > 0 && pokemonCp >= 1500)
                return ItemId.ItemWeparBerry;

            if (nanabBerryCount > 0 && (pokemonCp >= 1000 || (iV >= _clientSettings.TransferPokemonKeepAboveIVPercentage && proba < 0.40)))
                return ItemId.ItemNanabBerry;

            if (blukBerryCount > 0 && (pokemonCp >= 500 || (iV >= _clientSettings.TransferPokemonKeepAboveIVPercentage && proba < 0.50)))
                return ItemId.ItemBlukBerry;

            if (razzBerryCount > 0 && pokemonCp >= 300)
                return ItemId.ItemRazzBerry;

            return berries.OrderBy(g => g.Key).First().Key;
        }

        private async Task DisplayHighests()
        {
            Logger.Write("====== DisplayHighestsCP ======", LogLevel.Info, ConsoleColor.Yellow);
            var highestsPokemonCp = await _inventory.GetHighestsCp(10);
            foreach (var pokemon in highestsPokemonCp)
                Logger.Write(
                    $"# CP {pokemon.Cp.ToString().PadLeft(4, ' ')}/{PokemonInfo.CalculateMaxCp(pokemon).ToString().PadLeft(4, ' ')} | ({PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00")}% perfect)\t| Lvl {PokemonInfo.GetLevel(pokemon).ToString("00")}\t NAME: '{pokemon.PokemonId}'",
                    LogLevel.Info, ConsoleColor.Yellow);
            Logger.Write("====== DisplayHighestsPerfect ======", LogLevel.Info, ConsoleColor.Yellow);
            var highestsPokemonPerfect = await _inventory.GetHighestsPerfect(10);
            foreach (var pokemon in highestsPokemonPerfect)
            {
                Logger.Write(
                    $"# CP {pokemon.Cp.ToString().PadLeft(4, ' ')}/{PokemonInfo.CalculateMaxCp(pokemon).ToString().PadLeft(4, ' ')} | ({PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00")}% perfect)\t| Lvl {PokemonInfo.GetLevel(pokemon).ToString("00")}\t NAME: '{pokemon.PokemonId}'",
                    LogLevel.Info, ConsoleColor.Yellow);
            }
        }

        private List<GpxReader.Trk> GetGpxTracks()
        {
            var xmlString = File.ReadAllText(_clientSettings.GPXFile);
            var readgpx = new GpxReader(xmlString);
            return readgpx.Tracks;
        }

        /*
        private async Task DisplayPlayerLevelInTitle(bool updateOnly = false)
        {
            _playerProfile = _playerProfile.Profile != null ? _playerProfile : await _client.GetProfile();
            var playerName = _playerProfile.Profile.Username ?? "";
            var playrStats = await _inventory.GetPlayerStats();
            var playerStat = playerStats.FirstOrDefault();
            if (playerStat != null)
            {
                var message =
                    $" {playerName} | Level {playerStat.Level:0} - ({playerStat.Experience - playerStat.PrevLevelXp:0} / {playerStat.NextLevelXp - playerStat.PrevLevelXp:0} XP)";
                Console.Title = message;
                if (updateOnly == false)
                    Logger.Write(message);
            }
            if (updateOnly == false)
                await Task.Delay(5000);
        }
        */

        public async Task UseLuckyEgg()
        {
            var inventory = await _inventory.GetItems();
            var LuckyEgg = inventory.FirstOrDefault(p => (ItemId)p.Item_ == ItemId.ItemLuckyEgg);

            if (LuckyEgg == null || LuckyEgg.Count <= 0 || _lastLuckyEggTime.AddMinutes(30).Ticks > DateTime.Now.Ticks)
                return;

            _lastLuckyEggTime = DateTime.Now;
            await _client.UseXpBoostItem(ItemId.ItemLuckyEgg);
            Logger.Write($"Used Lucky Egg, remaining: {LuckyEgg.Count - 1}", LogLevel.Egg);
        }

        public async Task UseIncense()
        {
            var inventory = await _inventory.GetItems();
            var worstIncense = inventory.FirstOrDefault(p => (ItemId)p.Item_ == ItemId.ItemIncenseOrdinary);

            if (worstIncense == null || worstIncense.Count <= 0 || _lastIncenseTime.AddMinutes(30).Ticks > DateTime.Now.Ticks)
                return;

            _lastIncenseTime = DateTime.Now;
            await _client.UseIncenseItem(ItemId.ItemIncenseOrdinary);
            Logger.Write($"Used Ordinary Incense, remaining: {worstIncense.Count - 1}", LogLevel.Incense);
        }

        /// <summary>
        /// Resets coords if someone could realistically get back to the default coords points since they were last updated (program was last run)
        /// </summary>
        private void ResetCoords(string filename = "LastCoords.ini")
        {
            var lastcoordsFile = Path.Combine(_configsPath, filename);
            if (!File.Exists(lastcoordsFile)) return;
            var latLngFromFile = Client.GetLatLngFromFile();
            if (latLngFromFile == null) return;
            var distanceInMeters = LocationUtils.CalculateDistanceInMeters(latLngFromFile.Item1, latLngFromFile.Item2, _clientSettings.DefaultLatitude, _clientSettings.DefaultLongitude);
            var lastModified = File.Exists(lastcoordsFile) ? (DateTime?)File.GetLastWriteTime(lastcoordsFile) : null;
            if (lastModified == null) return;
            var minutesSinceModified = (DateTime.Now - lastModified).HasValue ? (double?)((DateTime.Now - lastModified).Value.Minutes) : null;
            if (minutesSinceModified == null || minutesSinceModified < 30) return; // Shouldn't really be null, but can be 0 and that's bad for division.
            var kmph = (distanceInMeters / 1000) / (minutesSinceModified / 60);
            if (kmph < 80) // If speed required to get to the default location is < 80km/hr
            {
                File.Delete(lastcoordsFile);
                Logger.Write("Detected realistic Traveling , using UserSettings.settings", LogLevel.Warning);
                //Client.SetCoordinates(_client.Settings.DefaultLatitude, _client.Settings.DefaultLongitude, _client.Settings.DefaultAltitude);
            }
            else
            {
                Logger.Write("Not realistic Traveling at " + kmph + ", using last saved Coords.ini", LogLevel.Warning);
            }
        }
    }
}
 