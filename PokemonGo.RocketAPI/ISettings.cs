#region

using PokemonGo.RocketAPI.Enums;
using System.Collections.Generic;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;

#endregion


namespace PokemonGo.RocketAPI
{
    public interface ISettings
    {
        AuthType AuthType { get; }
        string PtcPassword { get; }
        string PtcUsername { get; }
        string GoogleEmail { get; }
        string GooglePassword { get; }
        double DefaultLatitude { get; }
        double DefaultLongitude { get; }
        double DefaultAltitude { get; }
        bool UseGPXPathing { get; }
        string GPXFile { get; }
        bool GPXIgnorePokestops { get; }
        double WalkingSpeedInKilometerPerHour { get; }
        int MaxTravelDistanceInMeters { get; }
        bool UseTeleportInsteadOfWalking { get; }

        bool UsePokemonToNotCatchList { get; }
        bool UsePokemonToNotTransferList { get; }
        bool UsePokemonToEvolveList { get; }
        bool CatchPokemon { get; }
        bool CatchIncensePokemon { get; }
        bool CatchLuredPokemon { get; }
        bool EvolvePokemon { get; }
        bool EvolveOnlyPokemonAboveIV { get; }
        float EvolveOnlyPokemonAboveIVValue { get; }
        int EvolveKeepCandiesValue { get; }

        bool TransferPokemon { get; }
        bool NotTransferPokemonsThatCanEvolve { get; }
        int TransferPokemonKeepCP { get; }
        float TransferPokemonKeepIV { get; }
        int TransferPokemonKeepIVMinCP { get; }
        int TransferPokemonKeepDuplicationAmount { get; }
        float PrioritizeFactor { get; }

        bool HatchEggs { get; }
        bool UseOnlyBasicIncubator { get; }
        bool UseLuckyEggs { get; }
        int ExportPokemonToCsvEveryMinutes { get; }
        bool DebugMode { get; }

        ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter(IEnumerable<ItemData> myItems);
        ICollection<PokemonId> PokemonsToEvolve { get; }
        ICollection<PokemonId> PokemonsToNotTransfer { get; }
        ICollection<PokemonId> PokemonsToNotCatch { get; }
        Dictionary<PokemonId, TransferFilter> PokemonsTransferFilter { get; }
    }

    public class TransferFilter
    {
        public TransferFilter(int keepCP, float keepIV, int keepIVMinCP)
        {
            KeepCP = keepCP;
            KeepIV = keepIV;
            KeepIVMinCP = keepIVMinCP;
        }

        public int KeepCP { get; }
        public float KeepIV { get; }
        public int KeepIVMinCP { get; }
    }

}