#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Logic.Utils;
using PokemonGo.RocketAPI.Helpers;
using System.IO;
using PokemonGo.RocketAPI.Exceptions;
using System.Diagnostics;
using System.Linq.Expressions;
using Google.Protobuf;
using PokemonGo.RocketAPI.Logic.Tasks;
using PokemonGo.RocketAPI.Rpc;
using POGOProtos.Data;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Responses;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

#endregion


namespace PokemonGo.RocketAPI.Logic
{
    public class Logic
    {
        public static Client _client;
        public static ISettings _clientSettings;
        public static Inventory _inventory;
        public static BotStats _stats;
        public static Navigation _navigation;
        private GetPlayerResponse _playerProfile;

        public readonly string ConfigsPath = Path.Combine(Directory.GetCurrentDirectory(), "Settings");

        private bool _isInitialized = false;

        public Logic(ISettings clientSettings)
        {
            _clientSettings = clientSettings;
            PositionCheckState.Execute();
            _client = new Client(_clientSettings);
            _inventory = new Inventory();
            _stats = new BotStats();
            _navigation = new Navigation();
        }

        public async Task Execute()
        {
            if (!_isInitialized)
            {
                GitChecker.CheckVersion();

                var latLngFromFile = PositionCheckState.LoadPositionFromDisk();
                if (latLngFromFile != null && Math.Abs(latLngFromFile.Item1) > 0 && Math.Abs(latLngFromFile.Item2) > 0)
                    _client.Player.SetCoordinates(latLngFromFile.Item1, latLngFromFile.Item2,
                        _client.Settings.DefaultAltitude);
                else
                    _client.Player.SetCoordinates(_client.Settings.DefaultLatitude, _client.Settings.DefaultLongitude,
                        _client.Settings.DefaultAltitude);

                if (Math.Abs(_clientSettings.DefaultLatitude) <= 0  || Math.Abs(_clientSettings.DefaultLongitude) <= 0)
                {
                    Logger.Write($"Please change first Latitude and/or Longitude because currently your using default values!", LogLevel.Error);
                    for (int i = 3; i > 0; i--)
                    {
                        Logger.Write($"Bot will close in {i * 5} seconds!", LogLevel.Warning);
                        await Task.Delay(5000);
                    }
                    Environment.Exit(1);
                }
                else
                {
                    Logger.Write($"Make sure Lat & Lng is right. Exit Program if not! Lat: {_client.CurrentLatitude} Lng: {_client.CurrentLongitude}", LogLevel.Warning);
                    for (int i = 3; i > 0; i--)
                    {
                        Logger.Write($"Bot will continue in {i * 5} seconds!", LogLevel.Warning);
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
                            await _client.Login.DoLogin();
                            break;
                        case AuthType.Google:
                            await _client.Login.DoLogin();
                            break;
                        default:
                            Logger.Write("wrong AuthType");
                            Environment.Exit(0);
                            break;
                    }
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
                    await _client.Login.DoLogin();
                    break;
                case AuthType.Google:
                    await _client.Login.DoLogin();
                    break;
            }
        }

        public async Task PostLoginExecute()
        {
            Logger.Write($"Client logged in", LogLevel.Info);

            while (true)
            {
                if (!_isInitialized)
                {
                    await Inventory.GetCachedInventory();
                    _playerProfile = await _client.Player.GetPlayer();
                    BotStats.UpdateConsoleTitle();

                    var stats = await Inventory.GetPlayerStats();
                    var stat = stats.FirstOrDefault();
                    if (stat != null) BotStats.KmWalkedOnStart = stat.KmWalked;

                    Logger.Write("----------------------------", LogLevel.None, ConsoleColor.Yellow);
                    if (_clientSettings.AuthType == AuthType.Ptc)
                        Logger.Write($"PTC Account: {BotStats.GetUsername(_playerProfile)}\n", LogLevel.None, ConsoleColor.Cyan);
                    Logger.Write($"Latitude: {_clientSettings.DefaultLatitude}", LogLevel.None, ConsoleColor.DarkGray);
                    Logger.Write($"Longitude: {_clientSettings.DefaultLongitude}", LogLevel.None, ConsoleColor.DarkGray);
                    Logger.Write("----------------------------", LogLevel.None, ConsoleColor.Yellow);
                    Logger.Write("Your Account:\n");
                    Logger.Write($"Name: {BotStats.GetUsername(_playerProfile)}", LogLevel.None, ConsoleColor.DarkGray);
                    Logger.Write($"Team: {_playerProfile.PlayerData.Team}", LogLevel.None, ConsoleColor.DarkGray);
                    Logger.Write($"Level: {BotStats.GetCurrentInfo()}", LogLevel.None, ConsoleColor.DarkGray);
                    Logger.Write($"Stardust: {_playerProfile.PlayerData.Currencies.ToArray()[1].Amount}", LogLevel.None, ConsoleColor.DarkGray);
                    Logger.Write("----------------------------", LogLevel.None, ConsoleColor.Yellow);
                    await DisplayHighests();
                    Logger.Write("----------------------------", LogLevel.None, ConsoleColor.Yellow);

                    var pokemonsToNotTransfer = _clientSettings.PokemonsToNotTransfer;
                    var pokemonsToNotCatch = _clientSettings.PokemonsToNotCatch;
                    var pokemonsToEvolve = _clientSettings.PokemonsToEvolve;

                    await RecycleItemsTask.Execute();
                    if (_client.Settings.UseLuckyEggs) await UseLuckyEggTask.Execute();
                    if (_client.Settings.EvolvePokemon || _client.Settings.EvolveOnlyPokemonAboveIV) await EvolvePokemonTask.Execute();
                    if (_client.Settings.TransferPokemon) await TransferPokemonTask.Execute();
                    await ExportPokemonToCsv.Execute(_playerProfile.PlayerData);
                    if (_clientSettings.HatchEggs) await HatchEggsTask.Execute();
                }
                _isInitialized = true;
                await Main();

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

        private async Task Main()
        {
            if (_clientSettings.UseGPXPathing)
            {
                if (_clientSettings.MakeMeHuman)
                {
                    FarmPokestopsGPXTask.MakeMeHuman = true;
                    FarmPokestopsGPXTask.MaxGPXNoise = _clientSettings.MaxGPXNoise;
                }

                await FarmPokestopsGPXTask.Execute();
            }
            else
                await FarmPokestopsTask.Execute();
        }

        private async Task DisplayHighests()
        {
            Logger.Write("====== DisplayHighestsCP ======", LogLevel.Info, ConsoleColor.Yellow);
            var highestsPokemonCp = await Inventory.GetHighestsCp(15);
            string space = " ";
            foreach (var pokemon in highestsPokemonCp)
            {
                if (PokemonInfo.CalculatePokemonPerfection(pokemon) > 100)
                    space = "\t";

                Logger.Write(
                    $"# CP {pokemon.Cp.ToString().PadLeft(4, ' ')}/{PokemonInfo.CalculateMaxCp(pokemon).ToString().PadLeft(4, ' ')} | ({PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00")}% perfect){space}| Lvl {PokemonInfo.GetLevel(pokemon).ToString("00")}\t NAME: '{pokemon.PokemonId}'",
                    LogLevel.Info, ConsoleColor.Yellow);
            }

            Logger.Write("====== DisplayHighestsPerfect ======", LogLevel.Info, ConsoleColor.Yellow);
            var highestsPokemonPerfect = await Inventory.GetHighestsIv(15);
            foreach (var pokemon in highestsPokemonPerfect)
            {
                if (PokemonInfo.CalculatePokemonPerfection(pokemon) > 100)
                    space = "\t";

                Logger.Write(
                    $"# CP {pokemon.Cp.ToString().PadLeft(4, ' ')}/{PokemonInfo.CalculateMaxCp(pokemon).ToString().PadLeft(4, ' ')} | ({PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00")}% perfect){space}| Lvl {PokemonInfo.GetLevel(pokemon).ToString("00")}\t NAME: '{pokemon.PokemonId}'",
                    LogLevel.Info, ConsoleColor.Yellow);
            }
        }
    }
}
 