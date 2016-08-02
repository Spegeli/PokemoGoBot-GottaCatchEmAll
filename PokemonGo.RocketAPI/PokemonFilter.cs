using PokemonGo.RocketAPI.GeneratedCode;
using PokemonGo.RocketAPI.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI
{
    public struct PokemonFilterOption
    {
        public PokemonId PokemonID;
        public int MinimumCPToConsiderKeep;         //L
        public float MinimumIVToConsiderKeep;       //L
        public int MinimumCPToKeep;                 //U
        public float MinimumIVToKeep;               //U
    }

    public class PokemonFilter
    {
        public static List<PokemonData> GetMustTransferList(List<PokemonData> pokemons, ISettings settings)
        {
            List<PokemonData> ret = new List<PokemonData>();
            foreach(PokemonData pokemon in pokemons)
            {
                if (settings.PokemonTransferFilter.ContainsKey(pokemon.PokemonId))
                {
                    PokemonFilterOption filter = settings.PokemonTransferFilter[pokemon.PokemonId];

                    if (pokemon.Cp < filter.MinimumCPToConsiderKeep)
                    {
                        Logger.Write($"Transfered rubish {pokemon.PokemonId} for too low CP: {pokemon.Cp},{PokemonInfo.CalculatePokemonPerfection(pokemon)}");
                        ret.Add(pokemon);
                    } else if (PokemonInfo.CalculatePokemonPerfection(pokemon) < filter.MinimumIVToConsiderKeep)
                    {
                        Logger.Write($"Transfered rubish {pokemon.PokemonId} for too low IV: {pokemon.Cp},{PokemonInfo.CalculatePokemonPerfection(pokemon)}");
                        ret.Add(pokemon);
                    }
                        
                }
            }
            return ret;
        }

        public static List<PokemonData> GetCanTransferList(List<PokemonData> pokemons, ISettings settings)
        {
            List<PokemonData> ret = new List<PokemonData>();

            foreach (PokemonData pokemon in pokemons)
            {
                if (settings.UsePokemonToNotTransferList && settings.PokemonsToNotTransfer.Contains(pokemon.PokemonId))
                    continue;


                int CP = pokemon.Cp;
                double IV = PokemonInfo.CalculatePokemonPerfection(pokemon);

                if (settings.PokemonTransferFilter.ContainsKey(pokemon.PokemonId))
                {
                    PokemonFilterOption filter = settings.PokemonTransferFilter[pokemon.PokemonId];

                    if (CP < filter.MinimumCPToKeep || IV < filter.MinimumIVToKeep)
                        ret.Add(pokemon);

                    continue;
                }
                else
                {
                    if (settings.UseTransferPokemonKeepAboveCP && CP < settings.TransferPokemonKeepAboveCP)
                    {
                        ret.Add(pokemon);
                        continue;
                    }

                    if (settings.UseTransferPokemonKeepAboveIV && IV < settings.TransferPokemonKeepAboveIVPercentage)
                    {
                        ret.Add(pokemon);
                        continue;
                    }
                }
            }

            return ret;
        }
    }
}
