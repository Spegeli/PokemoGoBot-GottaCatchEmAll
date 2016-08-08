#region

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Enums;
using System;
using System.Threading;
using PokemonGo.RocketAPI.Logging;
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
        private readonly Client _client;
        public static DateTime LastRefresh;
        public static GetInventoryResponse CachedInventory;
        private readonly string _exportPath = Path.Combine(Directory.GetCurrentDirectory(), "Export");

        public Inventory(Client client)
        {
            _client = client;
        }

        public async Task<IEnumerable<PokemonData>> GetPokemonToTransfer(bool keepPokemonsThatCanEvolve = false, bool prioritizeIVoverCp = false, IEnumerable<PokemonId> filter = null)
        {    
            var myPokemon = await GetPokemons();
            var pokemonList = myPokemon.Where(p => p.DeployedFortId == String.Empty && p.Favorite == 0).ToList();
            if (_client.Settings.UsePokemonToNotTransferList && filter != null)
                pokemonList = pokemonList.Where(p => !filter.Contains(p.PokemonId)).ToList();
            if (_client.Settings.UseTransferPokemonKeepAboveCP)
                pokemonList = pokemonList.Where(p => p.Cp < _client.Settings.TransferPokemonKeepAboveCP).ToList();
            if (_client.Settings.UseTransferPokemonKeepAboveIV)
                pokemonList = pokemonList.Where(p => PokemonInfo.CalculatePokemonPerfection(p) < _client.Settings.TransferPokemonKeepAboveIVPercentage).ToList();

            if (!keepPokemonsThatCanEvolve)
                return pokemonList
                    .GroupBy(p => p.PokemonId)
                    .Where(x => x.Any())
                    .SelectMany(
                        p =>
                            p.OrderByDescending(
                                x => (prioritizeIVoverCp) ? PokemonInfo.CalculatePokemonPerfection(x) : x.Cp)
                                .ThenBy(n => n.StaminaMax)
                                .Skip(_client.Settings.TransferPokemonKeepDuplicateAmount)
                                .ToList());


            var results = new List<PokemonData>();
            var pokemonsThatCanBeTransfered = pokemonList.GroupBy(p => p.PokemonId).Where(x => x.Count() > _client.Settings.TransferPokemonKeepDuplicateAmount).ToList();

            var myPokemonSettings = await GetPokemonSettings();
            var pokemonSettings = myPokemonSettings.ToList();

            var myPokemonFamilies = await GetPokemonFamilies();
            var pokemonFamilies = myPokemonFamilies.ToArray();

            foreach (var pokemon in pokemonsThatCanBeTransfered)
            {
                var settings = pokemonSettings.Single(x => x.PokemonId == pokemon.Key);
                var familyCandy = pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId);
                var amountToSkip = _client.Settings.TransferPokemonKeepDuplicateAmount;

                if (settings.CandyToEvolve > 0)
                {
                    var amountPossible = familyCandy.Candy_/settings.CandyToEvolve;
                    if (amountPossible > amountToSkip)
                        amountToSkip = amountPossible;
                }

                results.AddRange(pokemonList.Where(x => x.PokemonId == pokemon.Key)
                    .OrderByDescending(
                        x => (prioritizeIVoverCp) ? PokemonInfo.CalculatePokemonPerfection(x) : x.Cp)
                    .ThenBy(n => n.StaminaMax)
                    .Skip(amountToSkip)
                    .ToList());
            }

            return results;
        }

        public async Task<IEnumerable<PokemonData>> GetHighestsCp(int limit)
        {
            var myPokemon = await GetPokemons();
            var pokemons = myPokemon.ToList();
            return pokemons.OrderByDescending(x => x.Cp).ThenBy(n => n.StaminaMax).Take(limit);
        }

        public async Task<IEnumerable<PokemonData>> GetHighestsPerfect(int limit = 1000)
        {
            var myPokemon = await GetPokemons();
            var pokemons = myPokemon.ToList();
            return pokemons.OrderByDescending(PokemonInfo.CalculatePokemonPerfection).Take(limit);
        }

        public async Task<PokemonData> GetHighestPokemonOfTypeByCp(PokemonData pokemon)
        {
            var myPokemon = await GetPokemons();
            var pokemons = myPokemon.ToList();
            return pokemons.Where(x => x.PokemonId == pokemon.PokemonId)
                .OrderByDescending(x => x.Cp)
                .FirstOrDefault();
        }

        public async Task<PokemonData> GetHighestPokemonOfTypeByIv(PokemonData pokemon)
        {
            var myPokemon = await GetPokemons();
            var pokemons = myPokemon.ToList();
            return pokemons.Where(x => x.PokemonId == pokemon.PokemonId)
                .OrderByDescending(PokemonInfo.CalculatePokemonPerfection)
                .FirstOrDefault();
        }

        public async Task<int> GetItemAmountByType(ItemId type)
        {
            var pokeballs = await GetItems();
            return pokeballs.FirstOrDefault(i => i.ItemId == type)?.Count ?? 0;
        }

        public async Task<IEnumerable<ItemData>> GetItems()
        {
            var inventory = await GetCachedInventory(_client);
            return inventory.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Item)
                .Where(p => p != null);
        }

        public async Task<IEnumerable<ItemData>> GetItemsToRecycle(ISettings settings)
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

        public async Task<IEnumerable<PlayerStats>> GetPlayerStats()
        {
            var inventory = await GetCachedInventory(_client);
            return inventory.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.PlayerStats)
                .Where(p => p != null);
        }

        public async Task<IEnumerable<Candy>> GetPokemonFamilies()
        {
            var inventory = await GetCachedInventory(_client);

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

        public async Task<IEnumerable<PokemonData>> GetPokemons()
        {
            var inventory = await GetCachedInventory(_client);
            return
                inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PokemonData)
                    .Where(p => p != null && p.PokemonId > 0);
        }

        public async Task<IEnumerable<PokemonSettings>> GetPokemonSettings()
        {
            var templates = await _client.Download.GetItemTemplates();
            return
                templates.ItemTemplates.Select(i => i.PokemonSettings)
                    .Where(p => p != null && p.FamilyId != PokemonFamilyId.FamilyUnset);
        }


        public async Task<IEnumerable<PokemonData>> GetPokemonToEvolve(bool prioritizeIVoverCp = false, IEnumerable < PokemonId> filter = null)
        {
            var myPokemons = await GetPokemons();
            myPokemons = myPokemons.Where(p => p.DeployedFortId == string.Empty);
            if (_client.Settings.UsePokemonToEvolveList && filter != null)
                myPokemons = myPokemons.Where(p => filter.Contains(p.PokemonId));		
            if (_client.Settings.EvolveOnlyPokemonAboveIV)
                myPokemons = myPokemons.Where(p => PokemonInfo.CalculatePokemonPerfection(p) >= _client.Settings.EvolveOnlyPokemonAboveIVValue);
            myPokemons = prioritizeIVoverCp ? myPokemons.OrderByDescending(PokemonInfo.CalculatePokemonPerfection) : myPokemons.OrderByDescending(p => p.Cp);

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
                if (_client.Settings.EvolveKeepCandiesValue > 0)
                {
                    if (familyCandy.Candy_ <= _client.Settings.EvolveKeepCandiesValue) continue;
                    familiecandies = familyCandy.Candy_ - _client.Settings.EvolveKeepCandiesValue;
                    if (familiecandies - pokemonCandyNeededAlready > settings.CandyToEvolve)
                        pokemonToEvolve.Add(pokemon);
                }
                else if (familiecandies - pokemonCandyNeededAlready > settings.CandyToEvolve)
                    pokemonToEvolve.Add(pokemon);
            }

            return pokemonToEvolve;
        }

        public static async Task<GetInventoryResponse> GetCachedInventory(Client client, bool request = false)
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
                    CachedInventory = await client.Inventory.GetInventory();
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

        public async Task<List<FortData>> GetPokestops(bool path = false)
        {
            var mapObjects = await _client.Map.GetMapObjects();
            var pokeStops = mapObjects.Item1.MapCells.SelectMany(i => i.Forts)
                .Where(
                    i =>
                        i.Type == FortType.Checkpoint &&
                        i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime() &&
                        (path) ? (LocationUtils.CalculateDistanceInMeters(
                            _client.CurrentLatitude, _client.CurrentLongitude,
                            i.Latitude, i.Longitude) < 40)
                           : (LocationUtils.CalculateDistanceInMeters(
                                _client.Settings.DefaultLatitude, _client.Settings.DefaultLongitude,
                                i.Latitude, i.Longitude) < _client.Settings.MaxTravelDistanceInMeters) ||
                                _client.Settings.MaxTravelDistanceInMeters == 0
                      ).ToList();

            return pokeStops.OrderBy(
                        i =>
                            LocationUtils.CalculateDistanceInMeters(_client.CurrentLatitude,
                            _client.CurrentLongitude, i.Latitude, i.Longitude)).ToList();
        }

        public async Task ExportPokemonToCsv(PlayerData player, string filename = "PokeList.csv")
        {
            if (player == null)
                return;
            var stats = await GetPlayerStats();
            var stat = stats.FirstOrDefault();
            if (stat == null)
                return;

            if (!Directory.Exists(_exportPath))
                Directory.CreateDirectory(_exportPath);
            if (Directory.Exists(_exportPath))
            {
                try
                {
                    var pokelistFile = Path.Combine(_exportPath, $"Profile_{player.Username}_{filename}");
                    if (File.Exists(pokelistFile))
                        File.Delete(pokelistFile);
                    var ls = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                    const string header = "PokemonID,Name,NickName,Level,CP / MaxCP,IV Perfection in %,Attack 1,Attack 2,HP,Attk,Def,Stamina,Familie Candies,IsInGym,IsFavorite,previewLink";
                    File.WriteAllText(pokelistFile, $"{header.Replace(",", $"{ls}")}");

                    var allPokemon = await GetHighestsPerfect();
                    var myPokemonSettings = await GetPokemonSettings();
                    var pokemonSettings = myPokemonSettings.ToList();
                    var myPokemonFamilies = await GetPokemonFamilies();
                    var pokemonFamilies = myPokemonFamilies.ToArray();
                    var trainerLevel = stat.Level;
                    var expReq = new[] { 0, 1000, 3000, 6000, 10000, 15000, 21000, 28000, 36000, 45000, 55000, 65000, 75000, 85000, 100000, 120000, 140000, 160000, 185000, 210000, 260000, 335000, 435000, 560000, 710000, 900000, 1100000, 1350000, 1650000, 2000000, 2500000, 3000000, 3750000, 4750000, 6000000, 7500000, 9500000, 12000000, 15000000, 20000000 };
                    var expReqAtLevel = expReq[stat.Level - 1];

                    using (var w = File.AppendText(pokelistFile))
                    {
                        w.WriteLine("");
                        foreach (var pokemon in allPokemon)
                        {
                            var toEncode = $"{(int)pokemon.PokemonId}" + "," + trainerLevel + "," + PokemonInfo.GetLevel(pokemon) + "," + pokemon.Cp + "," + pokemon.Stamina;
                            //Generate base64 code to make it viewable here http://poke.isitin.org/#MTUwLDIzLDE3LDE5MDIsMTE4
                            var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(toEncode));

                            var isInGym = string.Empty;
                            //var isInGym = pokemon.DeployedFortId == 0 ? "Yes" : "No";
                            var isFavorite = pokemon.Favorite != 0 ? "Yes" : "No";

                            var settings = pokemonSettings.Single(x => x.PokemonId == pokemon.PokemonId);
                            var familiecandies = pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId).Candy_;
                            var perfection = PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00");
                            perfection = perfection.Replace(",", ls == "," ? "." : ",");
                            string contentPart1 = $"\"{(int)pokemon.PokemonId}\",\"{pokemon.PokemonId}\",\"{pokemon.Nickname}\",";
                            string contentPart2 = $",\"{pokemon.Cp} / {PokemonInfo.CalculateMaxCp(pokemon)}\",";
                            string contentPart3 = $",\"{pokemon.Move1}\",\"{pokemon.Move2}\",\"{pokemon.Stamina}\",\"{pokemon.IndividualAttack}\",\"{pokemon.IndividualDefense}\",\"{pokemon.IndividualStamina}\",\"{familiecandies}\",\"{isInGym}\",\"{isFavorite}\",http://poke.isitin.org/#{encoded}";
                            string content = $"{contentPart1.Replace(",", $"{ls}")}\"{PokemonInfo.GetLevel(pokemon)}\"{contentPart2.Replace(",", $"{ls}")}\"{perfection}\"{contentPart3.Replace(",", $"{ls}")}";
                            w.WriteLine($"{content}");
                        }
                        w.Close();
                    }
                    Logger.Write($"Export Player Infos and all Pokemon to \"\\Export\\Profile_{player.Username}_{filename}\"", LogLevel.Info);
                }
                catch
                {
                    Logger.Write("Export Player Infos and all Pokemons to CSV not possible. File seems be in use!", LogLevel.Warning);
                }
            }
        }
    }
}
