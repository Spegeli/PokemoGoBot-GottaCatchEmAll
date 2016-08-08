#region

using System;
using System.IO;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.HttpClient;
using POGOProtos.Networking.Envelopes;

#endregion


namespace PokemonGo.RocketAPI
{
    public class Client
    {
        public Rpc.Login Login;
        public Rpc.Player Player;
        public Rpc.Download Download;
        public Rpc.Inventory Inventory;
        public Rpc.Map Map;
        public Rpc.Fort Fort;
        public Rpc.Encounter Encounter;
        public Rpc.Misc Misc;

        public ISettings Settings { get; }
        public string AuthToken { get; set; }

        public double CurrentLatitude { get; internal set; }
        public double CurrentLongitude { get; internal set; }
        public double CurrentAltitude { get; internal set; }

        public AuthType AuthType => Settings.AuthType;

        internal readonly PokemonHttpClient PokemonHttpClient = new PokemonHttpClient();
        internal string ApiUrl { get; set; }
        internal AuthTicket AuthTicket { get; set; }

        public Client(ISettings settings)
        {
            Settings = settings;

            Login = new Rpc.Login(this);
            Player = new Rpc.Player(this);
            Download = new Rpc.Download(this);
            Inventory = new Rpc.Inventory(this);
            Map = new Rpc.Map(this);
            Fort = new Rpc.Fort(this);
            Encounter = new Rpc.Encounter(this);
            Misc = new Rpc.Misc(this);
        }

        public async Task Incubate(float kmWalked, List<EggIncubator> incubators, List<PokemonData> unusedEggs, List<PokemonData> pokemons, int updateCounter)
        {
            foreach (var incubator in incubators)
            {
                if (incubator.PokemonId == 0)
                {
                    // Unlimited incubators prefer short eggs, limited incubators prefer long eggs
                    // Special case: If only one incubator is available at all, it will prefer long eggs 
                    // (disabled, i want to hatch more and more eggs)
                    var egg = incubator.ItemId == ItemId.ItemIncubatorBasicUnlimited && incubators.Count > 0
                      ? unusedEggs.FirstOrDefault()
                      : unusedEggs.LastOrDefault();

                    // Don't use limited incubators for under 5km eggs
                    if (egg == null | (egg.EggKmWalkedTarget < 5 && incubator.UsesRemaining > 0))
                    {
                        continue;
                    }

                    var response = await Inventory.UseItemEggIncubator(incubator.Id, egg.Id);
                    unusedEggs.Remove(egg);

                    Logger.Write($"Egg #{unusedEggs.IndexOf(egg)} was successfully added to Incubator #{incubators.IndexOf(incubator)}.", LogLevel.Incubation);
                }
                else
                {
                    // Currently hatching
                    if (updateCounter <= 0) {
                        var kmToWalk = incubator.TargetKmWalked - incubator.StartKmWalked;
                        var kmRemaining = incubator.TargetKmWalked - kmWalked;

                        Logger.Write($"Incubator #{incubators.IndexOf(incubator)} needs {kmRemaining.ToString("N2")}km/{kmToWalk.ToString("N2")}km to hatch.", LogLevel.Egg);
                    }
                }
            }
        }

    }
}
