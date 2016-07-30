#region

using PokemonGo.RocketAPI.Enums;
using System.Collections.Generic;
using PokemonGo.RocketAPI.GeneratedCode;

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
        bool GPXIgnorePokemon { get; }
        double WalkingSpeedInKilometerPerHour { get; }
        int MaxTravelDistanceInMeters { get; }

        bool UsePokemonToNotCatchList { get; }
        bool UsePokemonToNotTransferList { get; }
        bool EvolvePokemon { get; }
        bool EvolveOnlyPokemonAboveIV { get; }
        float EvolveOnlyPokemonAboveIVValue { get; }
        bool TransferPokemon { get; }
        int TransferPokemonKeepDuplicateAmount { get; }
        bool NotTransferPokemonsThatCanEvolve { get; }
        bool UseTransferPokemonKeepAboveCP { get; }
        int TransferPokemonKeepAboveCP { get; }
        bool UseTransferPokemonKeepAboveIV { get; }
        float TransferPokemonKeepAboveIVPercentage { get; }

        bool PrioritizeIVOverCP { get; }
        bool UseLuckyEggs { get; }
        bool UseIncense { get; }
        bool DebugMode { get; }
        bool SkipPokeStopHit { get; }

        ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter { get; }
        ICollection<PokemonId> PokemonsToEvolve { get; }
        ICollection<PokemonId> PokemonsToNotTransfer { get; }
        ICollection<PokemonId> PokemonsToNotCatch { get; }
    }
}
