#region

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Enums;
using System;
using System.Threading;
using System.IO;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Logic.Utils;
using POGOProtos.Data;
using POGOProtos.Data.Player;
using POGOProtos.Enums;
using POGOProtos.Inventory;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;
using POGOProtos.Settings.Master;

#endregion


namespace PokemonGo.RocketAPI.Logic
{
    public class Inventory
    {
        public static DateTime LastRefresh;
        public static GetInventoryResponse CachedInventory;

        public static async Task<IEnumerable<PokemonData>> GetPokemonToEvolve(IEnumerable<PokemonId> filter = null)
        {
            var myPokemons = await GetPokemons();
            myPokemons = myPokemons.Where(p => p.DeployedFortId == string.Empty);
            if (Logic._client.Settings.UsePokemonToEvolveList && filter != null)
                myPokemons = myPokemons.Where(p => filter.Contains(p.PokemonId));
            if (Logic._client.Settings.EvolveOnlyPokemonAboveIV)
                myPokemons = myPokemons.Where(p => PokemonInfo.CalculatePokemonPerfection(p) >= Logic._client.Settings.EvolveOnlyPokemonAboveIVValue);
            myPokemons = myPokemons.OrderByDescending(x => PokemonInfo.CalculatePokemonRanking(x, Logic._clientSettings.PrioritizeFactor));

            var pokemons = myPokemons.ToList();

            var myPokemonSettings = await GetPokemonSettings();
            var pokemonSettings = myPokemonSettings.ToList();

            var myPokemonFamilies = await GetPokemonFamilies();
            var pokemonFamilies = myPokemonFamilies.ToArray();

            var pokemonToEvolve = new List<PokemonData>();
            foreach (var pokemon in pokemons)
            {
                var settings = pokemonSettings.Single(x => x.PokemonId == pokemon.PokemonId);
                var familyCandy = pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId);

                //Don't evolve if we can't evolve it
                if (settings.EvolutionIds.Count == 0)
                    continue;

                var pokemonCandyNeededAlready =
                    pokemonToEvolve.Count(
                        p => pokemonSettings.Single(x => x.PokemonId == p.PokemonId).FamilyId == settings.FamilyId) *
                        settings.CandyToEvolve;

                var familiecandies = familyCandy.Candy_;
                if (Logic._client.Settings.EvolveKeepCandiesValue > 0)
                {
                    if (familyCandy.Candy_ <= Logic._client.Settings.EvolveKeepCandiesValue) continue;
                    familiecandies = familyCandy.Candy_ - Logic._client.Settings.EvolveKeepCandiesValue;
                    if (familiecandies - pokemonCandyNeededAlready > settings.CandyToEvolve)
                        pokemonToEvolve.Add(pokemon);
                }
                else if (familiecandies - pokemonCandyNeededAlready > settings.CandyToEvolve)
                    pokemonToEvolve.Add(pokemon);
            }

            return pokemonToEvolve;
        }

        public static async Task<IEnumerable<PokemonData>> GetPokemonToTransfer(bool keepPokemonsThatCanEvolve = false, IEnumerable<PokemonId> filter = null, IDictionary<PokemonId, TransferFilter> transferFilter = null)
        {    
            IEnumerable<PokemonData> myPokemons = await GetPokemons();
            IEnumerable<ulong> keepPokemonsList = new List<ulong>();

            // Keep best pokemon by new ranking indicator (cp^factor * (iv / 100)) and duplication amount
            keepPokemonsList = keepPokemonsList.Union(myPokemons.GroupBy(p => p.PokemonId)
                .SelectMany(
                    p =>
                        p.OrderByDescending(x => PokemonInfo.CalculatePokemonRanking(x, Logic._clientSettings.PrioritizeFactor))
                            .ThenBy(n => n.StaminaMax)
                            .Take(Logic._client.Settings.TransferPokemonKeepDuplicationAmount)
                            .Select(n => n.Id)
                            .ToList()));

            // All pokemon that are not in my favourites list and are not currently deployed to a fort
            keepPokemonsList = keepPokemonsList.Union(myPokemons.Where(p => p.DeployedFortId != String.Empty || p.Favorite != 0).Select(n => n.Id).ToList());

            // Do we want to keep any that can evolve?
            if (keepPokemonsThatCanEvolve)
            {
                List<ulong> keepEvolveList = new List<ulong>();
                var pokemonsThatCanBeTransfered = myPokemons.GroupBy(p => p.PokemonId).ToList();

                var myPokemonSettings = await GetPokemonSettings();
                var pokemonSettings = myPokemonSettings.ToList();

                var myPokemonFamilies = await GetPokemonFamilies();
                var pokemonFamilies = myPokemonFamilies.ToArray();

                foreach (var pokemon in pokemonsThatCanBeTransfered)
                {
                    var individualPokemonsettings = pokemonSettings.Single(x => x.PokemonId == pokemon.Key);
                    var familyCandy = pokemonFamilies.Single(x => individualPokemonsettings.FamilyId == x.FamilyId);
                    int amountToSkip = 0;

                    if (individualPokemonsettings.CandyToEvolve > 0)
                    {
                        var amountPossible = familyCandy.Candy_ / individualPokemonsettings.CandyToEvolve;
                        if (amountPossible > amountToSkip)
                            amountToSkip = amountPossible;
                    }

                    keepEvolveList.AddRange(myPokemons.Where(x => x.PokemonId == pokemon.Key)
                        .OrderByDescending(x => PokemonInfo.CalculatePokemonRanking(x, Logic._clientSettings.PrioritizeFactor))
                        .ThenBy(n => n.StaminaMax)
                        .Take(amountToSkip)
                        .Select(n => n.Id)
                        .ToList());
                }
                // Add the list of pokemons to keep for evolving
                keepPokemonsList = keepPokemonsList.Union(keepEvolveList);
            }

            // Keep any that are on my NotToTransfer list
            if (Logic._client.Settings.UsePokemonToNotTransferList && filter != null)
                keepPokemonsList = keepPokemonsList.Union(myPokemons.Where(p => filter.Contains(p.PokemonId)).Select(n => n.Id).ToList());

            List<ulong> keepIndividualPokemonsList = new List<ulong>();
            foreach (var pokemon in myPokemons)
            {
                var individualFilter = transferFilter.Where(p => pokemon.PokemonId == p.Key).Select(p => p.Value).FirstOrDefault();
                
                // Keep any that have CP higher than my KeepAboveCP setting
                int _keepCP = Logic._client.Settings.TransferPokemonKeepCP;
                if (individualFilter != null)
                    _keepCP = individualFilter.KeepCP;
                if (_keepCP > 0 && _keepCP <= pokemon.Cp)
                    keepIndividualPokemonsList.Add(pokemon.Id);
            
                // Keep any that have higher IV than my KeepAboveIV setting
                float _keepIV = Logic._client.Settings.TransferPokemonKeepIV;
                int _keepIVMinCP = Logic._clientSettings.TransferPokemonKeepIVMinCP;
                if (individualFilter != null) { 
                    _keepIV = individualFilter.KeepIV;
                    _keepIVMinCP = individualFilter.KeepIVMinCP;
                }
                if (_keepIV > 0 && _keepIV <= PokemonInfo.CalculatePokemonPerfection(pokemon) && _keepIVMinCP <= pokemon.Cp)
                    keepIndividualPokemonsList.Add(pokemon.Id);
            }
            // Add all with good CP and good IV filter to keep list
            keepPokemonsList = keepPokemonsList.Union(keepIndividualPokemonsList);

            // Remove any that are not in my Keep list
            IEnumerable<PokemonData> pokemonList = myPokemons.Where(p => !keepPokemonsList.Contains(p.Id)).OrderBy(p => p.PokemonId).ToList();

            return pokemonList.OrderBy(p => p.PokemonId);
        }

        public static async Task<IEnumerable<PokemonData>> GetHighestsCp(int limit)
        {
            var myPokemon = await GetPokemons();
            var pokemons = myPokemon.ToList();
            return pokemons.OrderByDescending(x => x.Cp).ThenBy(n => n.StaminaMax).Take(limit);
        }

        public static async Task<IEnumerable<PokemonData>> GetHighestsPerfect(int limit = 1000)
        {
            var myPokemon = await GetPokemons();
            var pokemons = myPokemon.ToList();
            return pokemons.OrderByDescending(PokemonInfo.CalculatePokemonPerfection).Take(limit);
        }

        public static async Task<PokemonData> GetBestPokemonOfType(PokemonData pokemon)
        {
            var myPokemon = await GetPokemons();
            var pokemons = myPokemon.ToList();
            return pokemons.Where(x => x.PokemonId == pokemon.PokemonId)
                .OrderByDescending(x => PokemonInfo.CalculatePokemonRanking(x, Logic._clientSettings.PrioritizeFactor))
                .FirstOrDefault();
        }

        public static async Task<int> GetItemAmountByType(ItemId type)
        {
            var pokeballs = await GetItems();
            return pokeballs.FirstOrDefault(i => i.ItemId == type)?.Count ?? 0;
        }

        public static async Task<IEnumerable<ItemData>> GetItems()
        {
            var inventory = await GetCachedInventory();
            return inventory.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Item)
                .Where(p => p != null);
        }

        public static async Task<IEnumerable<ItemData>> GetItemsToRecycle(ISettings settings)
        {
            var myItems = await GetItems();
            ICollection<KeyValuePair<ItemId, int>> itemRecycleFilter = settings.ItemRecycleFilter(myItems);

            return myItems
                .Where(x => itemRecycleFilter.Any(f => f.Key == x.ItemId && x.Count > f.Value))
                .Select(
                    x =>
                        new ItemData
                        {
                            ItemId = x.ItemId,
                            Count = x.Count - itemRecycleFilter.Single(f => f.Key == (ItemId)x.ItemId).Value,
                            Unseen = x.Unseen
                        });
        }

        public static async Task<IEnumerable<PlayerStats>> GetPlayerStats()
        {
            var inventory = await GetCachedInventory();
            return inventory.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.PlayerStats)
                .Where(p => p != null);
        }

        public static async Task<IEnumerable<Candy>> GetPokemonFamilies()
        {
            var inventory = await GetCachedInventory();

            var families = from item in inventory.InventoryDelta.InventoryItems
                           where item.InventoryItemData?.Candy != null
                           where item.InventoryItemData?.Candy.FamilyId != PokemonFamilyId.FamilyUnset
                           group item by item.InventoryItemData?.Candy.FamilyId into family
                           select new Candy
                           {
                               FamilyId = family.First().InventoryItemData.Candy.FamilyId,
                               Candy_ = family.First().InventoryItemData.Candy.Candy_
                           };


            return families.ToList();
        }

        public static async Task<IEnumerable<PokemonData>> GetPokemons()
        {
            var inventory = await GetCachedInventory();
            return
                inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PokemonData)
                    .Where(p => p != null && p.PokemonId > 0);
        }

        public static async Task<IEnumerable<PokemonSettings>> GetPokemonSettings()
        {
            var templates = await Logic._client.Download.GetItemTemplates();
            return
                templates.ItemTemplates.Select(i => i.PokemonSettings)
                    .Where(p => p != null && p.FamilyId != PokemonFamilyId.FamilyUnset);
        }

        public static async Task<List<InventoryItem>> GetPokeDexItems()
        {
            var inventory = await Logic._client.Inventory.GetInventory();

            return (from items in inventory.InventoryDelta.InventoryItems
                    where items.InventoryItemData?.PokedexEntry != null
                    select items).ToList();
        }

        public static async Task<IEnumerable<EggIncubator>> GetEggIncubators()
        {
            var inventory = await GetCachedInventory();
            return
                inventory.InventoryDelta.InventoryItems
                    .Where(x => x.InventoryItemData.EggIncubators != null)
                    .SelectMany(i => i.InventoryItemData.EggIncubators.EggIncubator)
                    .Where(i => i != null);
        }

        public static async Task<IEnumerable<PokemonData>> GetEggs()
        {
            var inventory = await GetCachedInventory();
            return
                inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PokemonData)
                    .Where(p => p != null && p.IsEgg);
        }

        public static async Task<GetInventoryResponse> GetCachedInventory(bool request = false)
        {
            var now = DateTime.UtcNow;
            var ss = new SemaphoreSlim(10);

            if (LastRefresh.AddSeconds(30).Ticks > now.Ticks && request == false)
            {
                return CachedInventory;
            }
            await ss.WaitAsync();
            try
            {
                LastRefresh = now;
                //_cachedInventory = await _client.GetInventory();

                try
                {
                    CachedInventory = await Logic._client.Inventory.GetInventory();
                }
                catch
                {
                    // ignored
                }

                return CachedInventory;
            }
            finally
            {
                ss.Release();
            }
        }

        public static async Task<List<FortData>> GetPokestops(bool gpxpathing = false)
        {
            var mapObjects = await Logic._client.Map.GetMapObjects();

            var pokeStops = mapObjects.Item1.MapCells.SelectMany(i => i.Forts)
                .Where(
                    i =>
                        i.Type == FortType.Checkpoint &&
                        i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()
                ).ToList();

            if (gpxpathing)
                pokeStops = pokeStops.Where(p => LocationUtils.CalculateDistanceInMeters(
                    Logic._client.CurrentLatitude, Logic._client.CurrentLongitude,
                    p.Latitude, p.Longitude) < 40).ToList();
            else
                pokeStops = pokeStops.Where(p => LocationUtils.CalculateDistanceInMeters(
                    Logic._client.Settings.DefaultLatitude, Logic._client.Settings.DefaultLongitude,
                    p.Latitude, p.Longitude) < Logic._client.Settings.MaxTravelDistanceInMeters ||
                                                 Logic._client.Settings.MaxTravelDistanceInMeters == 0).ToList();

            return pokeStops.OrderBy(
                i =>
                    LocationUtils.CalculateDistanceInMeters(Logic._client.CurrentLatitude,
                        Logic._client.CurrentLongitude, i.Latitude, i.Longitude)).ToList();
        }

    }
}
