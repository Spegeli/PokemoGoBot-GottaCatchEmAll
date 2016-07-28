#region

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.GeneratedCode;
using System.Collections.Concurrent;
using System;
using System.Threading;
using PokemonGo.RocketAPI.Logging;
using System.IO;

#endregion


namespace PokemonGo.RocketAPI.Logic
{
    public class Inventory
    {
        private readonly Client _client;
        public static DateTime _lastRefresh;
        public static GetInventoryResponse _cachedInventory;
        private string export_path = Path.Combine(Directory.GetCurrentDirectory(), "Export");

        public Inventory(Client client)
        {
            _client = client;
        }

        public async Task<IEnumerable<PokemonData>> GetPokemonToTransfer(bool keepPokemonsThatCanEvolve = false, bool prioritizeIVoverCP = false, IEnumerable<PokemonId> filter = null)
        {
            var myPokemon = await GetPokemons();
            var pokemonList = myPokemon.Where(p => p.DeployedFortId == 0 && p.Favorite == 0).ToList();
            if (_client.Settings.UsePokemonToNotTransferList && filter != null)
                pokemonList = pokemonList.Where(p => !filter.Contains(p.PokemonId)).ToList();
            if (_client.Settings.UseTransferPokemonKeepAboveCP)
                pokemonList = pokemonList.Where(p => p.Cp < _client.Settings.TransferPokemonKeepAboveCP).ToList();
            if (_client.Settings.UseTransferPokemonKeepAboveIV)
                pokemonList = pokemonList.Where(p => PokemonInfo.CalculatePokemonPerfection(p) < _client.Settings.TransferPokemonKeepAboveIVPercentage).ToList();

            if (keepPokemonsThatCanEvolve)
            {
                var results = new List<PokemonData>();
                var pokemonsThatCanBeTransfered = pokemonList.GroupBy(p => p.PokemonId)
                    .Where(x => x.Count() > _client.Settings.TransferPokemonKeepDuplicateAmount).ToList();

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
                        var amountPossible = familyCandy.Candy / settings.CandyToEvolve;
                        if (amountPossible > amountToSkip)
                            amountToSkip = amountPossible;
                    }

                    if (prioritizeIVoverCP)
                    {
                        results.AddRange(pokemonList.Where(x => x.PokemonId == pokemon.Key)
                            .OrderByDescending(PokemonInfo.CalculatePokemonPerfection)
                            .ThenBy(n => n.StaminaMax)
                            .Skip(amountToSkip)
                            .ToList());
                    }
                    else
                    {
                        results.AddRange(pokemonList.Where(x => x.PokemonId == pokemon.Key)
                            .OrderByDescending(x => x.Cp)
                            .ThenBy(n => n.StaminaMax)
                            .Skip(amountToSkip)
                            .ToList());
                    }
                }

                return results;
            }
            if (prioritizeIVoverCP)
            {
                return pokemonList
                .GroupBy(p => p.PokemonId)
                .Where(x => x.Count() > 1)
                .SelectMany(
                    p =>
                        p.OrderByDescending(PokemonInfo.CalculatePokemonPerfection)
                            .ThenBy(n => n.StaminaMax)
                            .Skip(_client.Settings.TransferPokemonKeepDuplicateAmount)
                            .ToList());
            }
            else
            {
                return pokemonList
                .GroupBy(p => p.PokemonId)
                .Where(x => x.Count() > 1)
                .SelectMany(
                    p =>
                        p.OrderByDescending(x => x.Cp)
                            .ThenBy(n => n.StaminaMax)
                            .Skip(_client.Settings.TransferPokemonKeepDuplicateAmount)
                            .ToList());
            }
        }

        public async Task<IEnumerable<PokemonData>> GetHighestsCP(int limit)
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

        public async Task<PokemonData> GetHighestPokemonOfTypeByCP(PokemonData pokemon)
        {
            var myPokemon = await GetPokemons();
            var pokemons = myPokemon.ToList();
            return pokemons.Where(x => x.PokemonId == pokemon.PokemonId)
                .OrderByDescending(x => x.Cp)
                .FirstOrDefault();
        }

        public async Task<PokemonData> GetHighestPokemonOfTypeByIV(PokemonData pokemon)
        {
            var myPokemon = await GetPokemons();
            var pokemons = myPokemon.ToList();
            return pokemons.Where(x => x.PokemonId == pokemon.PokemonId)
                .OrderByDescending(PokemonInfo.CalculatePokemonPerfection)
                .FirstOrDefault();
        }

        public async Task<int> GetItemAmountByType(MiscEnums.Item type)
        {
            var pokeballs = await GetItems();
            return pokeballs.FirstOrDefault(i => (MiscEnums.Item)i.Item_ == type)?.Count ?? 0;
        }

        public async Task<IEnumerable<Item>> GetItems()
        {
            var inventory = await getCachedInventory(_client);
            return inventory.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Item)
                .Where(p => p != null);
        }

        public async Task<IEnumerable<Item>> GetItemsToRecycle(ISettings settings)
        {
            var myItems = await GetItems();

            return myItems
                .Where(x => settings.ItemRecycleFilter.Any(f => f.Key == (ItemId)x.Item_ && x.Count > f.Value))
                .Select(
                    x =>
                        new Item
                        {
                            Item_ = x.Item_,
                            Count = x.Count - settings.ItemRecycleFilter.Single(f => f.Key == (ItemId)x.Item_).Value,
                            Unseen = x.Unseen
                        });
        }

        public async Task<IEnumerable<PlayerStats>> GetPlayerStats()
        {
            var inventory = await getCachedInventory(_client);
            return inventory.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.PlayerStats)
                .Where(p => p != null);
        }

        public async Task<IEnumerable<PokemonFamily>> GetPokemonFamilies()
        {
            var inventory = await getCachedInventory(_client);
            return
                inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PokemonFamily)
                    .Where(p => p != null && p.FamilyId != PokemonFamilyId.FamilyUnset);
        }

        public async Task<IEnumerable<PokemonData>> GetPokemons()
        {
            var inventory = await getCachedInventory(_client);
            return
                inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                    .Where(p => p != null && p.PokemonId > 0);
        }

        public async Task<IEnumerable<PokemonSettings>> GetPokemonSettings()
        {
            var templates = await _client.GetItemTemplates();
            return
                templates.ItemTemplates.Select(i => i.PokemonSettings)
                    .Where(p => p != null && p.FamilyId != PokemonFamilyId.FamilyUnset);
        }


        public async Task<IEnumerable<PokemonData>> GetPokemonToEvolve(IEnumerable<PokemonId> filter = null)
        {
            var myPokemons = await GetPokemons();
            myPokemons = myPokemons.Where(p => p.DeployedFortId == 0).OrderByDescending(p => p.Cp); //Don't evolve pokemon in gyms
            if (filter != null)
                myPokemons = myPokemons.Where(p => filter.Contains(p.PokemonId));		

            if (_client.Settings.EvolveOnlyPokemonAboveIV)
                myPokemons = myPokemons.Where(p => PokemonInfo.CalculatePokemonPerfection(p) >= _client.Settings.EvolveOnlyPokemonAboveIVValue);

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
                if (familyCandy.Candy - pokemonCandyNeededAlready > settings.CandyToEvolve)
                    pokemonToEvolve.Add(pokemon);
            }

            return pokemonToEvolve;
        }

        public static async Task<GetInventoryResponse> getCachedInventory(Client _client, bool request = false)
        {
            var now = DateTime.UtcNow;
            var ss = new SemaphoreSlim(10);

            if (_lastRefresh.AddSeconds(30).Ticks > now.Ticks && request == false)
            {
                return _cachedInventory;
            }
            await ss.WaitAsync();
            try
            {
                _lastRefresh = now;
                _cachedInventory = await _client.GetInventory();
                return _cachedInventory;
            }
            finally
            {
                ss.Release();
            }
        }

        public async Task ExportPokemonToCSV(Profile player, string filename = "PokeList.csv")
        {
            if (player == null)
                return;
            var stats = await GetPlayerStats();
            var stat = stats.FirstOrDefault();
            if (stat == null)
                return;

            if (!Directory.Exists(export_path))
                Directory.CreateDirectory(export_path);
            if (Directory.Exists(export_path))
            {
                try
                {
                    string pokelist_file = Path.Combine(export_path, $"Profile_{player.Username}_{filename}");
                    if (File.Exists(pokelist_file))
                        File.Delete(pokelist_file);
                    string ls = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                    string header = "PokemonID,Name,NickName,CP / MaxCP,IV Perfection in %,Attack 1,Attack 2,HP,Attk,Def,Stamina,Familie Candies,IsInGym,IsFavorite,previewLink";
                    File.WriteAllText(pokelist_file, $"{header.Replace(",", $"{ls}")}");

                    var AllPokemon = await GetHighestsPerfect();
                    var myPokemonSettings = await GetPokemonSettings();
                    var pokemonSettings = myPokemonSettings.ToList();
                    var myPokemonFamilies = await GetPokemonFamilies();
                    var pokemonFamilies = myPokemonFamilies.ToArray();
                    int trainerLevel = stat.Level;
                    int[] exp_req = new[] { 0, 1000, 3000, 6000, 10000, 15000, 21000, 28000, 36000, 45000, 55000, 65000, 75000, 85000, 100000, 120000, 140000, 160000, 185000, 210000, 260000, 335000, 435000, 560000, 710000, 900000, 1100000, 1350000, 1650000, 2000000, 2500000, 3000000, 3750000, 4750000, 6000000, 7500000, 9500000, 12000000, 15000000, 20000000 };
                    int exp_req_at_level = exp_req[stat.Level - 1];

                    using (var w = File.AppendText(pokelist_file))
                    {
                        w.WriteLine("");
                        foreach (var pokemon in AllPokemon)
                        {
                            string toEncode = $"{(int)pokemon.PokemonId}" + "," + trainerLevel + "," + PokemonInfo.GetLevel(pokemon) + "," + pokemon.Cp + "," + pokemon.Stamina;
                            //Generate base64 code to make it viewable here http://poke.isitin.org/#MTUwLDIzLDE3LDE5MDIsMTE4
                            var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(toEncode));

                            string IsInGym = string.Empty;
                            string IsFavorite = string.Empty;
                            /*if (pokemon.DeployedFortId == 0)
                                IsInGym = "Yes";
                            else
                                IsInGym = "No";
                            */
                            if (pokemon.Favorite != 0)
                                IsFavorite = "Yes";
                            else
                                IsFavorite = "No";

                            var settings = pokemonSettings.Single(x => x.PokemonId == pokemon.PokemonId);
                            var familiecandies = pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId).Candy;
                            string perfection = PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00");
                            perfection = perfection.Replace(",", ls == "," ? "." : ",");
                            string content_part1 = $"{(int)pokemon.PokemonId},{pokemon.PokemonId},{pokemon.Nickname},{pokemon.Cp}/{PokemonInfo.CalculateMaxCP(pokemon)},";
                            string content_part2 = $",{pokemon.Move1},{pokemon.Move2},{pokemon.Stamina},{pokemon.IndividualAttack},{pokemon.IndividualDefense},{pokemon.IndividualStamina},{familiecandies},{IsInGym},{IsFavorite},http://poke.isitin.org/#{encoded}";
                            string content = $"{content_part1.Replace(",", $"{ls}")}{perfection}{content_part2.Replace(",", $"{ls}")}";
                            w.WriteLine($"{content}");

                        }
                        w.Close();
                    }
                    Logger.Write($"Export Player Infos and all Pokemon to \"\\Export\\{filename}\"", LogLevel.Info);
                }
                catch
                {
                    Logger.Write("Export Player Infos and all Pokemons to CSV not possible. File seems be in use!", LogLevel.Warning);
                }
            }
        }

    }
}
