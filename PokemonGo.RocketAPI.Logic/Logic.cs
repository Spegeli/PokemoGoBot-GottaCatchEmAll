#region

using System;
using System.Collections.Generic;
using System.Device.Location;
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

                if (_clientSettings.EvolveAllPokemonWithEnoughCandy) await EvolveAllPokemonWithEnoughCandy(_clientSettings.PokemonsToEvolve);
                if (_clientSettings.TransferDuplicatePokemon) await TransferDuplicatePokemon();
                await PokemonToCSV();
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

        private async Task ExecuteFarmingPokestopsAndPokemons(bool path = false)
        {
            if (!path)
                await ExecuteFarmingPokestopsAndPokemons();
            else
            {
                //bool onTrack = true;
                List<GPXReader.trk> Tracks = GetGPXTracks(_clientSettings.GPXFile);
                int curTrkPt = 0;
                int maxTrkPt = 0;
                int curTrk = 0;
                int maxTrk = Tracks.Count - 1;
                int curTrkSeg = 0;
                int maxTrkSeg = 0;
                while (curTrk <= maxTrk)
                {
                    var Track = Tracks.ElementAt(curTrk);
                    var trackSegments = Track.Segments;
                    maxTrkSeg = trackSegments.Count - 1;
                    while (curTrkSeg <= maxTrkSeg)
                    {
                        List<GPXReader.trkpt> TrackPoints = Track.Segments.ElementAt(0).TrackPoints;
                        maxTrkPt = TrackPoints.Count - 1;
                        var mapObjects = await _client.GetMapObjects();
                        while (curTrkPt <= maxTrkPt)
                        {
                            GPXReader.trkpt nextPoint = TrackPoints.ElementAt(curTrkPt);
                            if (LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng, Convert.ToDouble(nextPoint.Lat), Convert.ToDouble(nextPoint.Lon)) > 5000)
                            {
                                Logger.Write($"Your desired destination of {nextPoint.Lat}, {nextPoint.Lon} is too far from your current position of {_client.CurrentLat}, {_client.CurrentLng}", LogLevel.Error);
                                break;
                            }

                            Logger.Write($"Your desired destination is {nextPoint.Lat}, your location is {nextPoint.Lon} {_client.CurrentLat}, {_client.CurrentLng}", LogLevel.Warning);

                            // Wasn't sure how to make this pretty. Edit as needed.
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
                                pokestopList = pokestopList.OrderBy(i => LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng, i.Latitude, i.Longitude)).ToList();
                                var pokeStop = pokestopList[0];
                                pokestopList.RemoveAt(0);

                                var fortInfo = await _client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                                var fortSearch = await _client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                                if (fortSearch.ExperienceAwarded > 0)
                                {
                                    _stats.AddExperience(fortSearch.ExperienceAwarded);
                                    _stats.UpdateConsoleTitle(_client, _inventory);
                                    //todo: fix egg crash
                                    Logger.Write($"XP: {fortSearch.ExperienceAwarded}, Gems: {fortSearch.GemsAwarded}, Items: {StringUtils.GetSummedFriendlyNameOfItemAwardList(fortSearch.ItemsAwarded)}", LogLevel.Pokestop);
                                }

                                await Task.Delay(1000);
                                await RecycleItems();
                                if (_clientSettings.TransferDuplicatePokemon) await TransferDuplicatePokemon();


                            }

                            var update = await _navigation.HumanPathWalking(TrackPoints.ElementAt(curTrkPt), _clientSettings.WalkingSpeedInKilometerPerHour, ExecuteCatchAllNearbyPokemons);

                            if (curTrkPt >= maxTrkPt)
                                curTrkPt = 0;
                            else
                                curTrkPt++;

                        }//end trkpts
                        if (curTrkSeg >= maxTrkSeg)
                            curTrkSeg = 0;
                        else
                            curTrkSeg++;
                    }//end trksegs
                    if (curTrk >= maxTrkSeg)
                        curTrk = 0;
                    else
                        curTrk++;
                }//end tracks
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
                Logger.Write($"You're outside of your defined radius! Walking to start in 5 seconds...", LogLevel.Warning);
                await Task.Delay(5000);
                var update = await _navigation.HumanLikeWalking(
                    new GeoCoordinate(_clientSettings.DefaultLatitude, _clientSettings.DefaultLongitude),
                    _clientSettings.WalkingSpeedInKilometerPerHour, null);
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
            {
                Logger.Write($"No PokeStops within {_clientSettings.MaxTravelDistanceInMeters}m. Choose a different GPS location or increase MaxTravelDistanceInMeters.", LogLevel.Error);
                return;
            }
            Logger.Write($"Found {pokeStops.Count()} pokestops", LogLevel.None, ConsoleColor.Green);

            foreach (var pokeStop in pokeStops)
            {
                await ExecuteCatchAllNearbyPokemons();

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
            int attemptCounter = 1;
            do
            {
                var bestBerry = await GetBestBerry(encounter?.WildPokemon);
                var inventoryBerries = await _inventory.GetItems();
                var probability = encounter?.CaptureProbability?.CaptureProbability_?.FirstOrDefault();

                var bestPokeball = await GetBestBall(encounter?.WildPokemon);
                if (bestPokeball == MiscEnums.Item.ITEM_UNKNOWN)
                {
                    Logger.Write($"You don't own any Pokeballs :( - We missed a {pokemon.PokemonId} with CP {encounter?.WildPokemon?.PokemonData?.Cp}", LogLevel.Warning);
                    return;
                }

                var berries = inventoryBerries.Where(p => (ItemId)p.Item_ == bestBerry).FirstOrDefault();
                if (bestBerry != ItemId.ItemUnknown && probability.HasValue && probability.Value < 0.35 && PokemonInfo.CalculatePokemonPerfection(encounter?.WildPokemon?.PokemonData) >= _clientSettings.KeepMinIVPercentage)
                {
                    var useRaspberry = await _client.UseCaptureItem(pokemon.EncounterId, bestBerry, pokemon.SpawnpointId);
                    Logger.Write($"{bestBerry} used, remaining: {berries.Count}", LogLevel.Berry);
                    await RandomHelper.RandomDelay(50, 200);
                }

                var distance = LocationUtils.CalculateDistanceInMeters(_client.CurrentLat, _client.CurrentLng, pokemon.Latitude, pokemon.Longitude);
                caughtPokemonResponse = await _client.CatchPokemon(pokemon.EncounterId, pokemon.SpawnpointId, pokemon.Latitude, pokemon.Longitude, bestPokeball);

                if (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess)
                {
                    foreach (int xp in caughtPokemonResponse.Scores.Xp)
                        _stats.AddExperience(xp);
                    _stats.IncreasePokemons();
                    var profile = await _client.GetProfile();
                    _stats.GetStardust(profile.Profile.Currency.ToArray()[1].Amount);
                }
                _stats.UpdateConsoleTitle(_client, _inventory);

                if (encounter?.CaptureProbability?.CaptureProbability_ != null)
                {
                    string catchStatus = attemptCounter > 1 ? $"{caughtPokemonResponse.Status} Attempt #{attemptCounter}" : $"{caughtPokemonResponse.Status}";
                    string receivedXP = catchStatus == "CatchSuccess" ? $"and received XP {caughtPokemonResponse.Scores.Xp.Sum()}" : $"";
                    Logger.Write($"({catchStatus}) | {pokemon.PokemonId} Lvl {PokemonInfo.GetLevel(encounter?.WildPokemon?.PokemonData)} (CP {encounter?.WildPokemon?.PokemonData?.Cp}/{PokemonInfo.CalculateMaxCP(encounter?.WildPokemon?.PokemonData)} | {Math.Round(PokemonInfo.CalculatePokemonPerfection(encounter?.WildPokemon?.PokemonData)).ToString("0.00")} % perfect) | Chance: {(float)((int)(encounter?.CaptureProbability?.CaptureProbability_.First() * 100)) / 100} | {Math.Round(distance)}m dist | with {bestPokeball} {receivedXP}", LogLevel.Pokemon);
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

            if (_clientSettings.UsePokemonToNotCatchFilter)
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
                if (pokemons.ElementAtOrDefault(pokemons.Count() - 1) != pokemon)
                    await RandomHelper.RandomDelay(50, 200);
            }

            if (_clientSettings.EvolveAllPokemonWithEnoughCandy) await EvolveAllPokemonWithEnoughCandy(_clientSettings.PokemonsToEvolve);
            if (_clientSettings.TransferDuplicatePokemon) await TransferDuplicatePokemon();
        }

        private async Task EvolveAllPokemonWithEnoughCandy(IEnumerable<PokemonId> filter = null)
        {
            await Inventory.getCachedInventory(_client, true);
            var pokemonToEvolve = await _inventory.GetPokemonToEvolve(filter);
            if (pokemonToEvolve != null && pokemonToEvolve.Any())
                Logger.Write($"{pokemonToEvolve.Count()} Pokemon:", LogLevel.Evolve);

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

        private async Task TransferDuplicatePokemon(bool keepPokemonsThatCanEvolve = false)
        {
            await Inventory.getCachedInventory(_client, true);
            var duplicatePokemons = await _inventory.GetDuplicatePokemonToTransfer(keepPokemonsThatCanEvolve, _clientSettings.PrioritizeIVOverCP, _clientSettings.PokemonsNotToTransfer);
            if (duplicatePokemons != null && duplicatePokemons.Any())
                Logger.Write($"{duplicatePokemons.Count()} Pokemon:", LogLevel.Transfer);

            foreach (var duplicatePokemon in duplicatePokemons)
            {
                var transfer = await _client.TransferPokemon(duplicatePokemon.Id);

                _stats.IncreasePokemonsTransfered();
                _stats.UpdateConsoleTitle(_client, _inventory);

                var bestPokemonOfType = await _inventory.GetHighestPokemonOfTypeByCP(duplicatePokemon);
                Logger.Write($"{duplicatePokemon.PokemonId} (CP {duplicatePokemon.Cp} | {PokemonInfo.CalculatePokemonPerfection(duplicatePokemon).ToString("0.00")} % perfect) | (Best: {bestPokemonOfType.Cp} CP | {PokemonInfo.CalculatePokemonPerfection(bestPokemonOfType).ToString("0.00")} % perfect)", LogLevel.Transfer);
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
                var transfer = await _client.RecycleItem((ItemId)item.Item_, item.Count);
                Logger.Write($"{item.Count}x {(ItemId)item.Item_}", LogLevel.Recycling);

                _stats.AddItemsRemoved(item.Count);
                _stats.UpdateConsoleTitle(_client, _inventory);

                await Task.Delay(100);
            }
            recycleCounter = 0;
        }

        private async Task<MiscEnums.Item> GetBestBall(WildPokemon pokemon)
        {
            var pokemonCp = pokemon?.PokemonData?.Cp;

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

            if (masterBalls && pokemonCp >= 2000)
                return MiscEnums.Item.ITEM_MASTER_BALL;
            else if (ultraBalls && pokemonCp >= 2000)
                return MiscEnums.Item.ITEM_ULTRA_BALL;
            else if (greatBalls && pokemonCp >= 2000)
                return MiscEnums.Item.ITEM_GREAT_BALL;

            if (ultraBalls && pokemonCp >= 1000)
                return MiscEnums.Item.ITEM_ULTRA_BALL;
            else if (greatBalls && pokemonCp >= 1000)
                return MiscEnums.Item.ITEM_GREAT_BALL;

            if (greatBalls && pokemonCp >= 500)
                return MiscEnums.Item.ITEM_GREAT_BALL;

            return balls.OrderBy(g => g.Key).First().Key;
        }

        private async Task<ItemId> GetBestBerry(WildPokemon pokemon)
        {
            var pokemonCp = pokemon?.PokemonData?.Cp;

            var items = await _inventory.GetItems();
            var berries = items.Where(i => (ItemId)i.Item_ == ItemId.ItemRazzBerry
                                        || (ItemId)i.Item_ == ItemId.ItemBlukBerry
                                        || (ItemId)i.Item_ == ItemId.ItemNanabBerry
                                        || (ItemId)i.Item_ == ItemId.ItemWeparBerry
                                        || (ItemId)i.Item_ == ItemId.ItemPinapBerry).GroupBy(i => ((ItemId)i.Item_)).ToList();
            if (berries.Count == 0 || pokemonCp <= 350) return ItemId.ItemUnknown;

            var razzBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_RAZZ_BERRY);
            var blukBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_BLUK_BERRY);
            var nanabBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_NANAB_BERRY);
            var weparBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_WEPAR_BERRY);
            var pinapBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_PINAP_BERRY);

            if (pinapBerryCount > 0 && pokemonCp >= 2000)
                return ItemId.ItemPinapBerry;
            else if (weparBerryCount > 0 && pokemonCp >= 2000)
                return ItemId.ItemWeparBerry;
            else if (nanabBerryCount > 0 && pokemonCp >= 2000)
                return ItemId.ItemNanabBerry;
            else if (nanabBerryCount > 0 && pokemonCp >= 2000)
                return ItemId.ItemBlukBerry;

            if (weparBerryCount > 0 && pokemonCp >= 1500)
                return ItemId.ItemWeparBerry;
            else if (nanabBerryCount > 0 && pokemonCp >= 1500)
                return ItemId.ItemNanabBerry;
            else if (blukBerryCount > 0 && pokemonCp >= 1500)
                return ItemId.ItemBlukBerry;

            if (nanabBerryCount > 0 && pokemonCp >= 1000)
                return ItemId.ItemNanabBerry;
            else if (blukBerryCount > 0 && pokemonCp >= 1000)
                return ItemId.ItemBlukBerry;

            if (blukBerryCount > 0 && pokemonCp >= 500)
                return ItemId.ItemBlukBerry;

            return berries.OrderBy(g => g.Key).First().Key;
        }

        private async Task DisplayPlayerLevelInTitle(bool updateOnly = false)
        {
            _playerProfile = _playerProfile.Profile != null ? _playerProfile : await _client.GetProfile();
            var playerName = _playerProfile.Profile.Username ?? "";
            var playerStats = await _inventory.GetPlayerStats();
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

        private async Task DisplayHighests()
        {
            Logger.Write($"====== DisplayHighestsCP ======", LogLevel.Info, ConsoleColor.Yellow);
            var highestsPokemonCP = await _inventory.GetHighestsCP(10);
            foreach (var pokemon in highestsPokemonCP)
                Logger.Write($"# CP {pokemon.Cp.ToString().PadLeft(4, ' ')}/{PokemonInfo.CalculateMaxCP(pokemon).ToString().PadLeft(4, ' ')} | ({PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00")}% perfect)\t| Lvl {PokemonInfo.GetLevel(pokemon).ToString("00")}\t NAME: '{pokemon.PokemonId}'");
            Logger.Write($"====== DisplayHighestsPerfect ======", LogLevel.Info, ConsoleColor.Yellow);
            var highestsPokemonPerfect = await _inventory.GetHighestsPerfect(10);
            foreach (var pokemon in highestsPokemonPerfect)
            {
                Logger.Write($"# CP {pokemon.Cp.ToString().PadLeft(4, ' ')}/{PokemonInfo.CalculateMaxCP(pokemon).ToString().PadLeft(4, ' ')} | ({PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00")}% perfect)\t| Lvl {PokemonInfo.GetLevel(pokemon).ToString("00")}\t NAME: '{pokemon.PokemonId}'");
            }
        }

        private async Task PokemonToCSV(string filename = "PokeList.csv")
        {
            string path = Directory.GetCurrentDirectory() + "\\Export\\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            if (Directory.Exists(path))
            {
                try
                {
                    if (File.Exists(path + filename))
                        File.Delete(path + filename);
                    if (!File.Exists(path + filename))
                    {
                        var AllPokemon = await _inventory.GetHighestsPerfect(1000);
                        var csvExportPokemonAll = new StringBuilder();
                        var columnnames = string.Format("{0};{1};{2};{3};{4}", "#", "NAME", "CP", "PERFECTION","CANDY");
                        csvExportPokemonAll.AppendLine(columnnames);
                        var myPokemonSettings = await _inventory.GetPokemonSettings();
                        var pokemonSettings = myPokemonSettings.ToList();
                        var myPokemonFamilies = await _inventory.GetPokemonFamilies();
                        var pokemonFamilies = myPokemonFamilies.ToArray();
                        foreach (var pokemon in AllPokemon)
                        {
                            var settings = pokemonSettings.Single(x => x.PokemonId == pokemon.PokemonId);
                            var familyCandy = pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId);
                            int POKENUMBER = (int)pokemon.PokemonId;
                            var NAME = $"{pokemon.PokemonId}";
                            var CP = $"{pokemon.Cp}";
                            var CANDY = $"{familyCandy.Candy}";
                            string PERFECTION = PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00");
                            var pokedata = string.Format("{0};{1};{2};{3};{4}", POKENUMBER, NAME, CP, PERFECTION,CANDY);
                            csvExportPokemonAll.AppendLine(pokedata);
                        }
                        Logger.Write($"Export all Pokemon to \"\\Export\\{filename}\"", LogLevel.Info);
                        File.WriteAllText(path + filename, csvExportPokemonAll.ToString());
                    }
                }
                catch
                {
                    Logger.Write("Export all Pokemons to CSV not possible. File seems be in use!", LogLevel.Warning);
                }
            }
        }

        private async Task LoadAndDisplayGPXFile()
        {
            string xmlString = File.ReadAllText(_clientSettings.GPXFile);
            GPXReader Readgpx = new GPXReader(xmlString);
            foreach (GPXReader.trk trk in Readgpx.Tracks)
            {
                foreach (GPXReader.trkseg trkseg in trk.Segments)
                {
                    foreach (GPXReader.trkpt trpkt in trkseg.TrackPoints)
                    {
                        Console.WriteLine(trpkt.ToString());
                    }
                }
            }
            await Task.Delay(0);
        }

        private GPXReader.trk GetGPXTrack(string gpxFile)
        {
            string xmlString = File.ReadAllText(_clientSettings.GPXFile);
            GPXReader Readgpx = new GPXReader(xmlString);
            return Readgpx.Tracks.ElementAt(0);
        }

        private List<GPXReader.trk> GetGPXTracks(string gpxFile)
        {
            string xmlString = File.ReadAllText(_clientSettings.GPXFile);
            GPXReader Readgpx = new GPXReader(xmlString);
            return Readgpx.Tracks;
        }

    }
}