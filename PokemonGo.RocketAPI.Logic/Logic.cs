#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.GeneratedCode;
using PokemonGo.RocketAPI.Logic.Utils;
using PokemonGo.RocketAPI.Helpers;
using System.IO;
using System.Text;
using PokemonGo.RocketAPI.Logging;

#endregion


namespace PokemonGo.RocketAPI.Logic
{
    public class Logic
    {
        private readonly Client _client;
        private readonly ISettings _clientSettings;
        private readonly Inventory _inventory;
        private readonly Statistics _stats;
        private readonly Navigation _navigation;
        private GetPlayerResponse _playerProfile;

        private int recycleCounter = 0;

        public Logic(ISettings clientSettings)
        {
            _clientSettings = clientSettings;
            ResetCoords();
            _client = new Client(_clientSettings);
            _inventory = new Inventory(_client);
            _stats = new Statistics();
            _navigation = new Navigation(_client);
        }

        public async Task Execute()
        {
            Git.CheckVersion();

            if (_clientSettings.DefaultLatitude == 0 || _clientSettings.DefaultLongitude == 0)
            {
                Logger.Write($"Please change first Latitude and/or Longitude because currently your using default values!", LogLevel.Error);
                for (int i = 3; i > 0; i--)
                {
                    Logger.Write($"Script will auto closed in {i * 5} seconds!", LogLevel.Warning);
                    await Task.Delay(5000);
                }
                await Task.Delay(15000);
                System.Environment.Exit(1);
            } else
            {
                Logger.Write($"Make sure Lat & Lng is right. Exit Program if not! Lat: {_client.CurrentLat} Lng: {_client.CurrentLng}", LogLevel.Warning);
                for (int i = 3; i > 0; i--)
                {
                    Logger.Write($"Script will continue in {i * 5} seconds!", LogLevel.Warning);
                    await Task.Delay(5000);
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
                            await _client.DoGoogleLogin("GoogleAuth.ini");
                            break;
                        default:
                            Logger.Write("wrong AuthType");
                            Environment.Exit(0);
                            break;
                    }

                    await _client.SetServer();

                    await PostLoginExecute();
                }
                catch (Exception e)
                {
                    Logger.Write(e.Message + " from " + e.Source);
                    Logger.Write("Got an exception, trying automatic restart..", LogLevel.Error);
                    await Execute();
                }
                await Task.Delay(10000);
            }
        }

        public async Task PostLoginExecute()
        {
            Logger.Write($"Client logged in", LogLevel.Info);

            while (true)
            {
                await Inventory.getCachedInventory(_client);
                _playerProfile = await _client.GetProfile();
                var PlayerName = Statistics.GetUsername(_client, _playerProfile);
                _stats.UpdateConsoleTitle(_client, _inventory);
                var _currentLevelInfos = await Statistics._getcurrentLevelInfos(_inventory);

                Logger.Write("----------------------------", LogLevel.None, ConsoleColor.Yellow);
                if (_clientSettings.AuthType == AuthType.Ptc)
                    Logger.Write($"PTC Account: {PlayerName}\n", LogLevel.None, ConsoleColor.Cyan);
                Logger.Write($"Latitude: {_clientSettings.DefaultLatitude}", LogLevel.None, ConsoleColor.DarkGray);
                Logger.Write($"Longitude: {_clientSettings.DefaultLongitude}", LogLevel.None, ConsoleColor.DarkGray);
                Logger.Write("----------------------------", LogLevel.None, ConsoleColor.Yellow);
                Logger.Write("Your Account:\n");
                Logger.Write($"Name: {PlayerName}", LogLevel.None, ConsoleColor.DarkGray);
                Logger.Write($"Team: {_playerProfile.Profile.Team}", LogLevel.None, ConsoleColor.DarkGray);
                Logger.Write($"Level: {_currentLevelInfos}", LogLevel.None, ConsoleColor.DarkGray);
                Logger.Write($"Stardust: {_playerProfile.Profile.Currency.ToArray()[1].Amount}", LogLevel.None, ConsoleColor.DarkGray);
                Logger.Write("----------------------------", LogLevel.None, ConsoleColor.Yellow);
                await DisplayHighests();
                Logger.Write("----------------------------", LogLevel.None, ConsoleColor.Yellow);

                var PokemonsNotToTransfer = _clientSettings.PokemonsNotToTransfer;
                var PokemonsNotToCatch = _clientSettings.PokemonsNotToCatch;
                var PokemonsToEvolve = _clientSettings.PokemonsToEvolve;

                if (_clientSettings.EvolvePokemon || _clientSettings.EvolveOnlyPokemonAboveIV) await EvolvePokemon(_clientSettings.PokemonsToEvolve);
                if (_clientSettings.TransferPokemon) await TransferPokemon();
                await _inventory.ExportPokemonToCSV(_playerProfile.Profile);
                await RecycleItems();
                await ExecuteFarmingPokestopsAndPokemons(_clientSettings.UseGPXPathing);

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
                            if (
                                LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng,
                                    Convert.ToDouble(nextPoint.Lat), Convert.ToDouble(nextPoint.Lon)) > 5000)
                            {
                                Logger.Write(
                                    $"Your desired destination of {nextPoint.Lat}, {nextPoint.Lon} is too far from your current position of {_client.CurrentLat}, {_client.CurrentLng}",
                                    LogLevel.Error);
                                break;
                            }

                            Logger.Write(
                                $"Your desired destination is {nextPoint.Lat}, {nextPoint.Lon} your location is {_client.CurrentLat}, {_client.CurrentLng}",
                                LogLevel.Warning);

                            // Wasn't sure how to make this pretty. Edit as needed.
                            var mapObjects = await _client.GetMapObjects();
                            var pokeStops =
                                mapObjects.MapCells.SelectMany(i => i.Forts)
                                    .Where(
                                        i =>
                                            i.Type == FortType.Checkpoint &&
                                            i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime() &&
                                            ( // Make sure PokeStop is within 40 meters, otherwise we cannot hit them.
                                                LocationUtils.CalculateDistanceInMeters(
                                                    _client.CurrentLat, _client.CurrentLng,
                                                    i.Latitude, i.Longitude) < 40)
                                    );

                            var pokestopList = pokeStops.ToList();

                            while (pokestopList.Any())
                            {
                                pokestopList =
                                    pokestopList.OrderBy(
                                        i =>
                                            LocationUtils.CalculateDistanceInMeters(_client.CurrentLat,
                                                _client.CurrentLng, i.Latitude, i.Longitude)).ToList();
                                var pokeStop = pokestopList[0];
                                pokestopList.RemoveAt(0);

                                var distance = LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng, pokeStop.Latitude, pokeStop.Longitude);
                                var fortInfo = await _client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                                Logger.Write($"Name: {fortInfo.Name} in {distance:0.##} m distance", LogLevel.Pokestop);

                                var fortSearch =
                                    await _client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                                if (fortSearch.ExperienceAwarded > 0)
                                {
                                    _stats.AddExperience(fortSearch.ExperienceAwarded);
                                    _stats.UpdateConsoleTitle(_client, _inventory);
                                    string EggReward = fortSearch.PokemonDataEgg != null ? "1" : "0";
                                    Logger.Write($"XP: {fortSearch.ExperienceAwarded}, Gems: {fortSearch.GemsAwarded}, Eggs: {EggReward}, Items: {StringUtils.GetSummedFriendlyNameOfItemAwardList(fortSearch.ItemsAwarded)}", LogLevel.Pokestop);
                                    recycleCounter++;
                                }

                                await RandomHelper.RandomDelay(50, 200);
                                if (recycleCounter >= 5)
                                    await RecycleItems();
                            }

                            await
                                _navigation.HumanPathWalking(trackPoints.ElementAt(curTrkPt),
                                    _clientSettings.WalkingSpeedInKilometerPerHour, ExecuteCatchAllNearbyPokemons);

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
                    $"You're outside of your defined radius! Walking to start ({distanceFromStart}m away) in 5 seconds. Is your Coords.ini file correct?",
                    LogLevel.Warning);
                await Task.Delay(5000);
                Logger.Write("Moving to start location now.");
                var ToStart = await _navigation.HumanLikeWalking(
                    new GeoCoordinate(_clientSettings.DefaultLatitude, _clientSettings.DefaultLongitude),
                    _clientSettings.WalkingSpeedInKilometerPerHour, ExecuteCatchAllNearbyPokemons);
            }

            var mapObjects = await _client.GetMapObjects();

            var pokeStops =
                Navigation.pathByNearestNeighbour(
                mapObjects.MapCells.SelectMany(i => i.Forts)
                    .Where(
                        i =>
                            i.Type == FortType.Checkpoint &&
                            i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime() &&
                            (_clientSettings.MaxTravelDistanceInMeters == 0 ||
                            LocationUtils.CalculateDistanceInMeters(
                                _clientSettings.DefaultLatitude, _clientSettings.DefaultLongitude,
                                    i.Latitude, i.Longitude) < _clientSettings.MaxTravelDistanceInMeters))
                            .OrderBy(
                            i =>
                            LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng, i.Latitude, i.Longitude)).ToArray());
            var pokestopList = pokeStops.ToList();
            if (pokestopList.Count <= 0)
                Logger.Write("No usable PokeStops found in your area. Is your maximum distance too small?",
                    LogLevel.Warning);
            else
                Logger.Write($"Found {pokeStops.Count()} pokestops", LogLevel.None, ConsoleColor.Green);

            while (pokestopList.Any())
            {
                await ExecuteCatchAllNearbyPokemons();

                var pokeStop = pokestopList[0];
                pokestopList.RemoveAt(0);

                var distance = LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng, pokeStop.Latitude, pokeStop.Longitude);
                var fortInfo = await _client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                Logger.Write($"Name: {fortInfo.Name} in {distance:0.##} m distance", LogLevel.Pokestop);
                var update = await _navigation.HumanLikeWalking(new GeoCoordinate(pokeStop.Latitude, pokeStop.Longitude), _clientSettings.WalkingSpeedInKilometerPerHour, ExecuteCatchAllNearbyPokemons);

                var fortSearch = await _client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                if (fortSearch.ExperienceAwarded > 0)
                {
                    _stats.AddExperience(fortSearch.ExperienceAwarded);
                    _stats.UpdateConsoleTitle(_client, _inventory);
                    string EggReward = fortSearch.PokemonDataEgg != null ? "1" : "0";
                    Logger.Write($"XP: {fortSearch.ExperienceAwarded}, Gems: {fortSearch.GemsAwarded}, Eggs: {EggReward}, Items: {StringUtils.GetSummedFriendlyNameOfItemAwardList(fortSearch.ItemsAwarded)}", LogLevel.Pokestop);
                    recycleCounter++;
                }
                await RandomHelper.RandomDelay(50, 200);
                if (recycleCounter >= 5)
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
                var berries = inventoryBerries.Where(p => (ItemId)p.Item_ == bestBerry).FirstOrDefault();
                if (bestBerry != ItemId.ItemUnknown && probability.HasValue && probability.Value < 0.35)
                {
                    await _client.UseCaptureItem(pokemon.EncounterId, bestBerry, pokemon.SpawnpointId);
                    Logger.Write($"{bestBerry} used, remaining: {berries.Count}", LogLevel.Berry);
                    await RandomHelper.RandomDelay(50, 200);
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

                    string receivedXP = catchStatus == "CatchSuccess" 
                        ? $"and received XP {caughtPokemonResponse.Scores.Xp.Sum()}" 
                        : $"";

                    Logger.Write($"({catchStatus}) | {pokemon.PokemonId} Lvl {PokemonInfo.GetLevel(encounter?.WildPokemon?.PokemonData)} (CP {encounter?.WildPokemon?.PokemonData?.Cp}/{PokemonInfo.CalculateMaxCP(encounter?.WildPokemon?.PokemonData)} | {Math.Round(PokemonInfo.CalculatePokemonPerfection(encounter?.WildPokemon?.PokemonData)).ToString("0.00")} % perfect) | Chance: {(float)((int)(encounter?.CaptureProbability?.CaptureProbability_.First() * 100)) / 100} | {Math.Round(distance)}m dist | with a {returnRealBallName(bestPokeball)}Ball {receivedXP}", LogLevel.Pokemon);
                }

                attemptCounter++;
                await RandomHelper.RandomDelay(750, 1250);
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
                    LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng, i.Latitude, i.Longitude));

            if (_clientSettings.UsePokemonToNotCatchList)
            {
                ICollection<PokemonId> filter = _clientSettings.PokemonsNotToCatch;
                pokemons = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons).Where(p => !filter.Contains(p.PokemonId)).OrderBy(i => LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng, i.Latitude, i.Longitude));
            }

            if (pokemons != null && pokemons.Any())
                Logger.Write($"Found {pokemons.Count()} catchable Pokemon", LogLevel.None, ConsoleColor.Green);
            else
                return;

            foreach (var pokemon in pokemons)
            {
                var distance = LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng, pokemon.Latitude, pokemon.Longitude);
                await Task.Delay(distance > 100 ? 1000 : 100);

                var encounter = await _client.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnpointId);

                if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                    await CatchEncounter(encounter, pokemon);
                else
                    Logger.Write($"Encounter problem: {encounter.Status}", LogLevel.Warning);
                if (!Equals(pokemons.ElementAtOrDefault(pokemons.Count() - 1), pokemon))
                    // If pokemon is not last pokemon in list, create delay between catches, else keep moving.
                    await RandomHelper.RandomDelay(50, 200);
            }

            if (_clientSettings.EvolvePokemon || _clientSettings.EvolveOnlyPokemonAboveIV) await EvolvePokemon(_clientSettings.PokemonsToEvolve);
            if (_clientSettings.TransferPokemon) await TransferPokemon();
        }

        private async Task EvolvePokemon(IEnumerable<PokemonId> filter = null)
        {
            await Inventory.getCachedInventory(_client, true);
            var pokemonToEvolve = await _inventory.GetPokemonToEvolve(filter);
            if (pokemonToEvolve != null && pokemonToEvolve.Any())
            {
                Logger.Write($"{pokemonToEvolve.Count()} Pokemon:", LogLevel.Evolve);
                if (_clientSettings.useLuckyEggsWhileEvolving)
                    await UseLuckyEgg();
            }

            foreach (var pokemon in pokemonToEvolve)
            {
                var evolvePokemonOutProto = await _client.EvolvePokemon((ulong)pokemon.Id);

                Logger.Write(
                    evolvePokemonOutProto.Result == EvolvePokemonOut.Types.EvolvePokemonStatus.PokemonEvolvedSuccess
                        ? $"{pokemon.PokemonId} successfully for {evolvePokemonOutProto.ExpAwarded} xp"
                        : $"Failed: {pokemon.PokemonId}. EvolvePokemonOutProto.Result was {evolvePokemonOutProto.Result}, stopping evolving {pokemon.PokemonId}"
                    , LogLevel.Evolve);

                await Task.Delay(1000);
            }
        }

        private async Task TransferPokemon()
        {
            await Inventory.getCachedInventory(_client, true);
            var duplicatePokemons = await _inventory.GetPokemonToTransfer(_clientSettings.NotTransferPokemonsThatCanEvolve, _clientSettings.PrioritizeIVOverCP, _clientSettings.PokemonsNotToTransfer);
            if (duplicatePokemons != null && duplicatePokemons.Any())
                Logger.Write($"{duplicatePokemons.Count()} Pokemon:", LogLevel.Transfer);

            foreach (var duplicatePokemon in duplicatePokemons)
            {
                await _client.TransferPokemon(duplicatePokemon.Id);

                await Inventory.getCachedInventory(_client, true);
                var myPokemonSettings = await _inventory.GetPokemonSettings();
                var pokemonSettings = myPokemonSettings.ToList();
                var myPokemonFamilies = await _inventory.GetPokemonFamilies();
                var pokemonFamilies = myPokemonFamilies.ToArray();
                var settings = pokemonSettings.Single(x => x.PokemonId == duplicatePokemon.PokemonId);
                var familyCandy = pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId);
                var FamilyCandies = $"{familyCandy.Candy}";

                _stats.IncreasePokemonsTransfered();
                _stats.UpdateConsoleTitle(_client, _inventory);

                var bestPokemonOfType = _client.Settings.PrioritizeIVOverCP
                    ? await _inventory.GetHighestPokemonOfTypeByIV(duplicatePokemon)
                    : await _inventory.GetHighestPokemonOfTypeByCP(duplicatePokemon);
                Logger.Write($"{duplicatePokemon.PokemonId} (CP {duplicatePokemon.Cp} | {PokemonInfo.CalculatePokemonPerfection(duplicatePokemon).ToString("0.00")} % perfect) | (Best: {bestPokemonOfType.Cp} CP | {PokemonInfo.CalculatePokemonPerfection(bestPokemonOfType).ToString("0.00")} % perfect) | Family Candies: {FamilyCandies}", LogLevel.Transfer);
                await Task.Delay(500);
            }
        }

        private async Task RecycleItems()
        {
            await Inventory.getCachedInventory(_client, true);
            var items = await _inventory.GetItemsToRecycle(_clientSettings);
            if (items != null && items.Any())
                Logger.Write($"{items.Count()} {(items.Count() == 1 ? "Item" : "Items")}:", LogLevel.Recycling);

            foreach (var item in items)
            {
                await _client.RecycleItem((ItemId)item.Item_, item.Count);
                Logger.Write($"{item.Count}x {(ItemId)item.Item_}", LogLevel.Recycling);

                _stats.AddItemsRemoved(item.Count);
                _stats.UpdateConsoleTitle(_client, _inventory);

                await Task.Delay(100);
            }
            recycleCounter = 0;
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

            if (ultraBalls && (pokemonCp >= 1000 || (iV >= _clientSettings.KeepMinIVPercentage && proba < 0.40)))
                return MiscEnums.Item.ITEM_ULTRA_BALL;

            if (greatBalls && (pokemonCp >= 300 || (iV >= _clientSettings.KeepMinIVPercentage && proba < 0.50)))
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
            if (berries.Count == 0 || pokemonCp <= 350) return ItemId.ItemUnknown;

            var razzBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_RAZZ_BERRY);
            var blukBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_BLUK_BERRY);
            var nanabBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_NANAB_BERRY);
            var weparBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_WEPAR_BERRY);
            var pinapBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_PINAP_BERRY);

            if (pinapBerryCount > 0 && pokemonCp >= 2000)
                return ItemId.ItemPinapBerry;

            if (weparBerryCount > 0 && pokemonCp >= 1500)
                return ItemId.ItemWeparBerry;

            if (nanabBerryCount > 0 && (pokemonCp >= 1000 || (iV >= _clientSettings.KeepMinIVPercentage && proba < 0.40)))
                return ItemId.ItemNanabBerry;

            if (blukBerryCount > 0 && (pokemonCp >= 500 || (iV >= _clientSettings.KeepMinIVPercentage && proba < 0.50)))
                return ItemId.ItemBlukBerry;

            if (razzBerryCount > 0 && pokemonCp >= 150)
                return ItemId.ItemRazzBerry;

            return ItemId.ItemUnknown;
            //return berries.OrderBy(g => g.Key).First().Key;
        }

        private async Task DisplayHighests()
        {
            Logger.Write("====== DisplayHighestsCP ======", LogLevel.Info, ConsoleColor.Yellow);
            var highestsPokemonCp = await _inventory.GetHighestsCP(10);
            foreach (var pokemon in highestsPokemonCp)
                Logger.Write(
                    $"# CP {pokemon.Cp.ToString().PadLeft(4, ' ')}/{PokemonInfo.CalculateMaxCP(pokemon).ToString().PadLeft(4, ' ')} | ({PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00")}% perfect)\t| Lvl {PokemonInfo.GetLevel(pokemon).ToString("00")}\t NAME: '{pokemon.PokemonId}'",
                    LogLevel.Info, ConsoleColor.Yellow);
            Logger.Write("====== DisplayHighestsPerfect ======", LogLevel.Info, ConsoleColor.Yellow);
            var highestsPokemonPerfect = await _inventory.GetHighestsPerfect(10);
            foreach (var pokemon in highestsPokemonPerfect)
            {
                Logger.Write(
                    $"# CP {pokemon.Cp.ToString().PadLeft(4, ' ')}/{PokemonInfo.CalculateMaxCP(pokemon).ToString().PadLeft(4, ' ')} | ({PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00")}% perfect)\t| Lvl {PokemonInfo.GetLevel(pokemon).ToString("00")}\t NAME: '{pokemon.PokemonId}'",
                    LogLevel.Info, ConsoleColor.Yellow);
            }
        }

        /*
        private async Task LoadAndDisplayGpxFile()
        {
            var xmlString = File.ReadAllText(_clientSettings.GPXFile);
            var readgpx = new GpxReader(xmlString);
            foreach (var trk in readgpx.Tracks)
            {
                foreach (var trkseg in trk.Segments)
                {
                    foreach (var trpkt in trkseg.TrackPoints)
                    {
                        Console.WriteLine(trpkt.ToString());
                    }
                }
            }
            await Task.Delay(0);
        }
        */

        /*
        private GPXReader.trk GetGPXTrack(string gpxFile)
        {
            string xmlString = File.ReadAllText(_clientSettings.GPXFile);
            GPXReader Readgpx = new GPXReader(xmlString);
            return Readgpx.Tracks.ElementAt(0);
        }
        */

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
            var LuckyEgg = inventory.Where(p => (ItemId)p.Item_ == ItemId.ItemLuckyEgg).FirstOrDefault();

            if (LuckyEgg == null || LuckyEgg.Count <= 0)
                return;

            await _client.UseXpBoostItem(ItemId.ItemLuckyEgg);
            Logger.Write($"Used Lucky Egg, remaining: {LuckyEgg.Count - 1}", LogLevel.Egg);
        }

        /// <summary>
        /// Resets coords if someone could realistically get back to the default coords points since they were last updated (program was last run)
        /// </summary>
        private void ResetCoords(string filename = "LastCoords.ini")
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Configs", filename);
            if (!File.Exists(path)) return;
            Tuple<double, double> latLngFromFile = Client.GetLatLngFromFile();
            if (latLngFromFile == null) return;
            double distance = LocationUtils.CalculateDistanceInMeters(latLngFromFile.Item1, latLngFromFile.Item2, _clientSettings.DefaultLatitude, _clientSettings.DefaultLongitude);
            DateTime? lastModified = File.Exists(path) ? (DateTime?)File.GetLastWriteTime(path) : null;
            if (lastModified == null) return;
            double? hoursSinceModified = (DateTime.Now - lastModified).HasValue ? (double?)((DateTime.Now - lastModified).Value.Minutes / 60.0) : null;
            if (hoursSinceModified == null || hoursSinceModified < 1) return; // Shouldn't really be null, but can be 0 and that's bad for division.
            var kmph = (distance / 1000) / (hoursSinceModified ?? .1);
            if (kmph < 80) // If speed required to get to the default location is < 80km/hr
            {
                File.Delete(path);
                Logger.Write("Detected realistic Traveling , using UserSettings.settings", LogLevel.Warning);
            }
            else
            {
                Logger.Write("Not realistic Traveling at " + kmph + ", using last saved Coords.ini", LogLevel.Warning);
            }
        }
    }
}
 