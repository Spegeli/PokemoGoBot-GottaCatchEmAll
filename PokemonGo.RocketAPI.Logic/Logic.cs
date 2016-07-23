#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using AllEnum;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.GeneratedCode;
using PokemonGo.RocketAPI.Logic.Utils;
using PokemonGo.RocketAPI.Helpers;
using System.Collections;

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
                Logger.Error($"Please change first Latitude and/or Longitude because currently your using default values!");
                Logger.Error($"Window will be auto closed in 15 seconds!");
                await Task.Delay(15000);
                System.Environment.Exit(1);
            }else
            {
                Logger.Error($"Make sure Lat & Lng is right. Exit Program if not! Lat: {_client.CurrentLat} Lng: {_client.CurrentLng}");
                for (int i = 3; i > 0; i--)
                {
                    Logger.Error($"Script will continue in {i*5} seconds!");
                    await Task.Delay(5000);
                }
            }

            Logger.Normal(ConsoleColor.DarkGreen, $"Logging in via: {_clientSettings.AuthType}");
            while (true)
            {
                try
                {
                    if (_clientSettings.AuthType == AuthType.Ptc)
                        await _client.DoPtcLogin(_clientSettings.PtcUsername, _clientSettings.PtcPassword);
                    else if (_clientSettings.AuthType == AuthType.Google)
                        await _client.DoGoogleLogin();

                    await _client.SetServer();

                    await PostLoginExecute();
                }
                catch (AccessTokenExpiredException ex)
                {
                    Logger.Error($"Access token expired");
                    Logger.Error($"{ex}");
                }
                catch (TaskCanceledException ex)
                {
                    Logger.Error("Task Canceled Exception - Restarting");
                    Logger.Error($"{ex}");
                    await Execute();
                }
                catch (UriFormatException ex)
                {
                    Logger.Error("UriFormatException - Restarting");
                    Logger.Error($"{ex}");
                    await Execute();
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Logger.Error("ArgumentOutOfRangeException - Restarting");
                    Logger.Error($"{ex}");
                    await Execute();
                }
                catch (ArgumentNullException ex)
                {
                    Logger.Error("ArgumentNullException - Restarting");
                    Logger.Error($"{ex}");
                    await Execute();
                }
                catch (NullReferenceException ex)
                {
                    Logger.Error("NullReferenceException - Restarting");
                    Logger.Error($"{ex}");
                    await Execute();
                }
                catch (InvalidResponseException ex)
                {
                    Logger.Error("InvalidResponseException - Restarting");
                    Logger.Error($"{ex}");
                    await Execute();
                }
                catch (AggregateException ex)
                {
                    Logger.Error("AggregateException - Restarting");
                    Logger.Error($"{ex}");
                    await Execute();
                }
                await Task.Delay(10000);
            }
        }

        public async Task PostLoginExecute()
        { 
            Logger.Normal(ConsoleColor.DarkGreen, $"Client logged in");

            while (true)
            {
                    _playerProfile = await _client.GetProfile();

                    _stats.updateConsoleTitle(_inventory);

                    var _currentLevelInfos = await Statistics._getcurrentLevelInfos(_inventory);

                    Logger.Normal(ConsoleColor.Yellow, "----------------------------");
                    if (_clientSettings.AuthType == AuthType.Ptc)
                        Logger.Normal(ConsoleColor.Cyan, $"PTC Account: {_clientSettings.PtcUsername}\n");
                    //Logger.Normal(ConsoleColor.Cyan, "Password: " + _clientSettings.PtcPassword + "\n");
                    Logger.Normal(ConsoleColor.DarkGray, $"Latitude: {_clientSettings.DefaultLatitude}");
                    Logger.Normal(ConsoleColor.DarkGray, $"Longitude: {_clientSettings.DefaultLongitude}");
                    Logger.Normal(ConsoleColor.Yellow, "----------------------------");
                    Logger.Normal(ConsoleColor.DarkGray, "Your Account:\n");
                    Logger.Normal(ConsoleColor.DarkGray, $"Name: {_playerProfile.Profile.Username}");
                    Logger.Normal(ConsoleColor.DarkGray, $"Team: {_playerProfile.Profile.Team}");
                    Logger.Normal(ConsoleColor.DarkGray, $"Level: {_currentLevelInfos}");
                    Logger.Normal(ConsoleColor.DarkGray, $"Stardust: {_playerProfile.Profile.Currency.ToArray()[1].Amount}");
                    Logger.Normal(ConsoleColor.Yellow, "----------------------------");
                    await DisplayHighests();
                    Logger.Normal(ConsoleColor.Yellow, "----------------------------");

                    var PokemonsNotToTransfer = _clientSettings.PokemonsNotToTransfer;
                    var PokemonsNotToCatch = _clientSettings.PokemonsNotToCatch;
                    var PokemonsToEvolve = _clientSettings.PokemonsToEvolve;

                    if (_clientSettings.EvolveAllPokemonWithEnoughCandy) await EvolveAllPokemonWithEnoughCandy(_clientSettings.PokemonsToEvolve);
                    if (_clientSettings.TransferDuplicatePokemon) await TransferDuplicatePokemon();
                    await RecycleItems();
                    await ExecuteFarmingPokestopsAndPokemons();

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

        public static float CalculatePokemonPerfection(PokemonData poke)
        {
            return (poke.IndividualAttack * 2 + poke.IndividualDefense + poke.IndividualStamina) / (4.0f * 15.0f) * 100.0f;
        }

        public async Task RepeatAction(int repeat, Func<Task> action)
        {
            for (int i = 0; i < repeat; i++)
                await action();
        }

        private async Task ExecuteFarmingPokestopsAndPokemons()
        {
            var mapObjects = await _client.GetMapObjects();
            //var pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts).Where(i => i.Type == FortType.Checkpoint && i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()).OrderBy(i => LocationUtils.CalculateDistanceInMeters(new Navigation.Location(_client.CurrentLat, _client.CurrentLng), new Navigation.Location(i.Latitude, i.Longitude)));
            var pokeStops = Navigation.pathByNearestNeighbour(mapObjects.MapCells.SelectMany(i => i.Forts).Where(i => i.Type == FortType.Checkpoint && i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()).OrderBy(i => LocationUtils.CalculateDistanceInMeters(new Navigation.Location(_client.CurrentLat, _client.CurrentLng), new Navigation.Location(i.Latitude, i.Longitude))).ToArray());
            Logger.Normal(ConsoleColor.Green, $"Found {pokeStops.Count()} pokestops");

            foreach (var pokeStop in pokeStops)
            {
                await ExecuteCatchAllNearbyPokemons();
                if (_clientSettings.EvolveAllPokemonWithEnoughCandy) await EvolveAllPokemonWithEnoughCandy(_clientSettings.PokemonsToEvolve);
                if (_clientSettings.TransferDuplicatePokemon) await TransferDuplicatePokemon();
                
                var update = await _navigation.HumanLikeWalking(new Navigation.Location(pokeStop.Latitude, pokeStop.Longitude), _clientSettings.WalkingSpeedInKilometerPerHour, ExecuteCatchAllNearbyPokemons);
                var distance = Navigation.DistanceBetween2Coordinates(_client.CurrentLat, _client.CurrentLng, pokeStop.Latitude, pokeStop.Longitude);
                var fortInfo = await _client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                var fortSearch = await _client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                _stats.addExperience(fortSearch.ExperienceAwarded);
                _stats.updateConsoleTitle(_inventory);

                Logger.Normal(ConsoleColor.Cyan, $"(POKESTOP) Name: {fortInfo.Name} in {Math.Round(distance)}m distance");
                if (fortSearch.ExperienceAwarded > 0)
                    Logger.Normal(ConsoleColor.Cyan, $"(POKESTOP) XP: {fortSearch.ExperienceAwarded}, Gems: { fortSearch.GemsAwarded}, Eggs: {fortSearch.PokemonDataEgg} Items: {StringUtils.GetSummedFriendlyNameOfItemAwardList(fortSearch.ItemsAwarded)}");

                await RandomHelper.RandomDelay(500,1000);
                await RecycleItems();
            }
        }

        private async Task ExecuteCatchAllNearbyPokemons()
        {
            var mapObjects = await _client.GetMapObjects();
            
            //var pokemons = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons).OrderBy(i => LocationUtils.CalculateDistanceInMeters(new Navigation.Location(_client.CurrentLat, _client.CurrentLng), new Navigation.Location(i.Latitude, i.Longitude)));
            var pokemons = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons).OrderBy(i => LocationUtils.CalculateDistanceInMeters(new Navigation.Location(_client.CurrentLat, _client.CurrentLng), new Navigation.Location(i.Latitude, i.Longitude)));
            if (_clientSettings.UsePokemonToNotCatchFilter)
            {
                ICollection<PokemonId> filter = _clientSettings.PokemonsNotToCatch;
                pokemons = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons).Where(p => !filter.Contains(p.PokemonId)).OrderBy(i => LocationUtils.CalculateDistanceInMeters(new Navigation.Location(_client.CurrentLat, _client.CurrentLng), new Navigation.Location(i.Latitude, i.Longitude)));
            }

            if (pokemons != null && pokemons.Any())
                Logger.Normal(ConsoleColor.Green, $"Found {pokemons.Count()} catchable Pokemon");
            
            foreach (var pokemon in pokemons)
            {
                var distance = Navigation.DistanceBetween2Coordinates(_client.CurrentLat, _client.CurrentLng, pokemon.Latitude, pokemon.Longitude);
                await Task.Delay(distance > 100 ? 5000 : 500);

                var encounter = await _client.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnpointId);

                if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                    await CatchEncounter(encounter, pokemon);
                else
                    Logger.Normal($"Encounter problem: {encounter.Status}");
            }
            await RandomHelper.RandomDelay(500, 1000);
        }

        private async Task CatchEncounter(EncounterResponse encounter, MapPokemon pokemon)
        {
            CatchPokemonResponse caughtPokemonResponse;
            do
            {
                var bestBerry = await GetBestBerry(encounter?.WildPokemon);
                var inventoryBerries = await _inventory.GetItems();
                var berries = inventoryBerries.Where(p => (ItemId)p.Item_ == bestBerry).FirstOrDefault(); ;
                var probability = encounter?.CaptureProbability?.CaptureProbability_?.FirstOrDefault();
                if (bestBerry != AllEnum.ItemId.ItemUnknown && probability.HasValue && probability.Value < 0.35 && CalculatePokemonPerfection(encounter?.WildPokemon?.PokemonData) >= _clientSettings.KeepMinIVPercentage)
                {
                    var useRaspberry = await _client.UseCaptureItem(pokemon.EncounterId, bestBerry, pokemon.SpawnpointId);
                    Logger.Normal($"(BERRY) {bestBerry} used, remaining: {berries.Count}");
                    await RandomHelper.RandomDelay(500, 1000);
                }

                var bestPokeball = await GetBestBall(encounter?.WildPokemon);
                if (bestPokeball == MiscEnums.Item.ITEM_UNKNOWN)
                {
                    Logger.Normal($"(POKEBATTLE) You don't own any Pokeballs :( - We missed a {pokemon.PokemonId} with CP {encounter?.WildPokemon?.PokemonData?.Cp}");
                    return;
                }

                var distance = Navigation.DistanceBetween2Coordinates(_client.CurrentLat, _client.CurrentLng, pokemon.Latitude, pokemon.Longitude);
                caughtPokemonResponse = await _client.CatchPokemon(pokemon.EncounterId, pokemon.SpawnpointId, pokemon.Latitude, pokemon.Longitude, bestPokeball);

                if (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess)
                {
                    foreach (int xp in caughtPokemonResponse.Scores.Xp)
                        _stats.addExperience(xp);
                    _stats.increasePokemons();
                    var profile = await _client.GetProfile();
                    _stats.getStardust(profile.Profile.Currency.ToArray()[1].Amount);
                }
                _stats.updateConsoleTitle(_inventory);
                Logger.Normal(ConsoleColor.Yellow,
                    caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess
                    ? $"(POKEBATTLE) Caught {pokemon.PokemonId} (CP {encounter?.WildPokemon?.PokemonData?.Cp} | {Math.Round(CalculatePokemonPerfection(encounter?.WildPokemon?.PokemonData)).ToString("0.00")} % perfect) | Chance: {(float)((int)(encounter?.CaptureProbability?.CaptureProbability_.First() * 100)) / 100} | {Math.Round(distance)}m distance | with {bestPokeball} and received XP {caughtPokemonResponse.Scores.Xp.Sum()}"
                    : $"(POKEBATTLE) Missed/Escaped {pokemon.PokemonId} (CP {encounter?.WildPokemon?.PokemonData?.Cp} | {Math.Round(CalculatePokemonPerfection(encounter?.WildPokemon?.PokemonData)).ToString("0.00")} % perfect) | Chance: {(float)((int)(encounter?.CaptureProbability?.CaptureProbability_.First() * 100)) / 100} {caughtPokemonResponse.Status} | {Math.Round(distance)}m distance | using a {bestPokeball}.."
                    );
                await RandomHelper.RandomDelay(1750, 2250);
            }
            while (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchMissed || caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchEscape);
        }

        private async Task EvolveAllPokemonWithEnoughCandy(IEnumerable<PokemonId> filter = null)
        {
            var pokemonToEvolve = await _inventory.GetPokemonToEvolve(filter);
            if (pokemonToEvolve != null && pokemonToEvolve.Any())
                Logger.Normal($"(EVOLVE) {pokemonToEvolve.Count()} Pokemon:");

            foreach (var pokemon in pokemonToEvolve)
            {
                var evolvePokemonOutProto = await _client.EvolvePokemon((ulong)pokemon.Id);

                Logger.Normal(
                    evolvePokemonOutProto.Result == EvolvePokemonOut.Types.EvolvePokemonStatus.PokemonEvolvedSuccess
                        ? $"(EVOLVE) {pokemon.PokemonId} successfully for {evolvePokemonOutProto.ExpAwarded} xp"
                        : $"(EVOLVE) Failed: {pokemon.PokemonId}. EvolvePokemonOutProto.Result was {evolvePokemonOutProto.Result}, stopping evolving {pokemon.PokemonId}"
                    );

                await Task.Delay(3000);
            }
        }

        private async Task TransferDuplicatePokemon(bool keepPokemonsThatCanEvolve = false)
        {
            var duplicatePokemons = await _inventory.GetDuplicatePokemonToTransfer(keepPokemonsThatCanEvolve, _clientSettings.KeepMinDuplicatePokemon, _clientSettings.PokemonsNotToTransfer);
            // Currently not returns the correct value
            //if (duplicatePokemons != null && duplicatePokemons.Any())
            //    Logger.Normal(ConsoleColor.DarkYellow, $"(TRANSFER) {duplicatePokemons.Count()} Pokemon:");

            foreach (var duplicatePokemon in duplicatePokemons)
            {
                if (CalculatePokemonPerfection(duplicatePokemon) >= _clientSettings.KeepMinIVPercentage || duplicatePokemon.Cp > _clientSettings.KeepMinCP)
                    continue;

                var transfer = await _client.TransferPokemon(duplicatePokemon.Id);

                _stats.increasePokemonsTransfered();
                _stats.updateConsoleTitle(_inventory);

                var bestPokemonOfType = await _inventory.GetHighestPokemonOfTypeByCP(duplicatePokemon);
                Logger.Normal(ConsoleColor.DarkYellow, $"(TRANSFER) {duplicatePokemon.PokemonId} (CP {duplicatePokemon.Cp} | {CalculatePokemonPerfection(duplicatePokemon).ToString("0.00")} % perfect) | (Best: {bestPokemonOfType.Cp} CP | {CalculatePokemonPerfection(bestPokemonOfType).ToString("0.00")} % perfect)");
                await Task.Delay(500);
            }
        }

        private async Task RecycleItems()
        {
            var items = await _inventory.GetItemsToRecycle(_clientSettings);
            if (items != null && items.Any())
                Logger.Normal(ConsoleColor.DarkCyan, $"(RECYCLE) {items.Count()} Items:");

            foreach (var item in items)
            {
                var transfer = await _client.RecycleItem((ItemId)item.Item_, item.Count);
                Logger.Normal(ConsoleColor.DarkCyan, $"(RECYCLED) {item.Count}x {(ItemId)item.Item_}");

                _stats.addItemsRemoved(item.Count);
                _stats.updateConsoleTitle(_inventory);

                await Task.Delay(500);
            }
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

        private async Task<AllEnum.ItemId> GetBestBerry(WildPokemon pokemon)
        {
            var pokemonCp = pokemon?.PokemonData?.Cp;

            var items = await _inventory.GetItems();
            var berries = items.Where(i => (AllEnum.ItemId)i.Item_ == AllEnum.ItemId.ItemRazzBerry
                                        || (AllEnum.ItemId)i.Item_ == AllEnum.ItemId.ItemBlukBerry
                                        || (AllEnum.ItemId)i.Item_ == AllEnum.ItemId.ItemNanabBerry
                                        || (AllEnum.ItemId)i.Item_ == AllEnum.ItemId.ItemWeparBerry
                                        || (AllEnum.ItemId)i.Item_ == AllEnum.ItemId.ItemPinapBerry).GroupBy(i => ((AllEnum.ItemId)i.Item_)).ToList();
            if (berries.Count == 0 || pokemonCp <= 350) return AllEnum.ItemId.ItemUnknown;

            var razzBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_RAZZ_BERRY);
            var blukBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_BLUK_BERRY);
            var nanabBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_NANAB_BERRY);
            var weparBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_WEPAR_BERRY);
            var pinapBerryCount = await _inventory.GetItemAmountByType(MiscEnums.Item.ITEM_PINAP_BERRY);

            if (pinapBerryCount > 0 && pokemonCp >= 2000)
                return AllEnum.ItemId.ItemPinapBerry;
            else if (weparBerryCount > 0 && pokemonCp >= 2000)
                return AllEnum.ItemId.ItemWeparBerry;
            else if (nanabBerryCount > 0 && pokemonCp >= 2000)
                return AllEnum.ItemId.ItemNanabBerry;
            else if (nanabBerryCount > 0 && pokemonCp >= 2000)
                return AllEnum.ItemId.ItemBlukBerry;

            if (weparBerryCount > 0 && pokemonCp >= 1500)
                return AllEnum.ItemId.ItemWeparBerry;
            else if (nanabBerryCount > 0 && pokemonCp >= 1500)
                return AllEnum.ItemId.ItemNanabBerry;
            else if (blukBerryCount > 0 && pokemonCp >= 1500)
                return AllEnum.ItemId.ItemBlukBerry;

            if (nanabBerryCount > 0 && pokemonCp >= 1000)
                return AllEnum.ItemId.ItemNanabBerry;
            else if (blukBerryCount > 0 && pokemonCp >= 1000)
                return AllEnum.ItemId.ItemBlukBerry;

            if (blukBerryCount > 0 && pokemonCp >= 500)
                return AllEnum.ItemId.ItemBlukBerry;

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
                    Logger.Normal(message);
            }
            if (updateOnly == false)
                await Task.Delay(5000);
        }

        private async Task DisplayHighests()
        {
            Logger.Normal($"====== DisplayHighestsCP ======");
            var highestsPokemonCP = await _inventory.GetHighestsCP(5);
            foreach (var pokemon in highestsPokemonCP)
                Logger.Normal($"# CP {pokemon.Cp}\t| ({CalculatePokemonPerfection(pokemon).ToString("0.00")}\t% perfect) NAME: '{pokemon.PokemonId}'");
            Logger.Normal($"====== DisplayHighestsPerfect ======");
            var highestsPokemonPerfect = await _inventory.GetHighestsPerfect(5);
            foreach (var pokemon in highestsPokemonPerfect)
            {
                Logger.Normal($"# CP {pokemon.Cp}\t| ({CalculatePokemonPerfection(pokemon).ToString("0.00")}\t% perfect) NAME: '{pokemon.PokemonId}'");
            }
        }
    
    }
}
