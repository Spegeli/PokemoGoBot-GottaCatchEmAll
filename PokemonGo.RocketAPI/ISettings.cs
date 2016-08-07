#region

using PokemonGo.RocketAPI.Enums;
using System.Collections.Generic;
using PokemonGo.RocketAPI.GeneratedCode;

#endregion


namespace PokemonGo.RocketAPI
{
    public interface ISettings
    {
        AuthType AuthType { get; set; }
        string PtcPassword { get; set; }
        string PtcUsername { get; set; }
        string GoogleEmail { get; set; }
        string GooglePassword { get; set; }
        double DefaultLatitude { get; set; }
        double DefaultLongitude { get; set; }
        double DefaultAltitude { get; set; }
        bool UseGPXPathing { get; set; }
        string GPXFile { get; set; }
        bool GPXIgnorePokestops { get; set; }
        double WalkingSpeedInKilometerPerHour { get; set; }
        int MaxTravelDistanceInMeters { get; set; }
        bool UseTeleportInsteadOfWalking { get; set; }

        bool UsePokemonToNotCatchList { get; set; }
        bool UsePokemonToNotTransferList { get; set; }

        bool CatchPokemon { get; set; }

        bool EvolvePokemon { get; set; }
        bool EvolveOnlyPokemonAboveIV { get; set; }
        float EvolveOnlyPokemonAboveIVValue { get; set; }
        int EvolveKeepCandiesValue { get; set; }

        bool TransferPokemon { get; set; }
        int TransferPokemonKeepDuplicateAmountMaxCP { get; set; }
        int TransferPokemonKeepDuplicateAmountMaxIV { get; set; }
        bool NotTransferPokemonsThatCanEvolve { get; set; }
        bool UseTransferPokemonKeepAboveCP { get; set; }
        int TransferPokemonKeepAboveCP { get; set; }
        bool UseTransferPokemonKeepAboveIV { get; set; }
        float TransferPokemonKeepAboveIVPercentage { get; set; }

        bool PrioritizeIVOverCP { get; set; }
        bool UseLuckyEggs { get; set; }
        bool UseIncense { get; set; }
        bool DebugMode { get; set; }

        ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter { get; set; }
        ICollection<PokemonId> PokemonsToEvolve { get; set; }
        ICollection<PokemonId> PokemonsToNotTransfer { get; set; }
        ICollection<PokemonId> PokemonsToNotCatch { get; set; }
    }
}