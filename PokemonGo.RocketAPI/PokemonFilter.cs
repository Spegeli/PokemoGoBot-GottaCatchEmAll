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
                //Avoid transfer favourite pokemons
                if (pokemon.DeployedFortId == 0 && pokemon.Favorite != 0)
                    continue;

                if (settings.PokemonTransferFilter.ContainsKey(pokemon.PokemonId))
                {
                    PokemonFilterOption filter = settings.PokemonTransferFilter[pokemon.PokemonId];

                    if (pokemon.Cp < filter.MinimumCPToConsiderKeep)
                    {
                        Logger.Write($"Find rubbish {pokemon.PokemonId} with too low CP: {pokemon.Cp}, IV:{PokemonInfo.CalculatePokemonPerfection(pokemon)}",LogLevel.Transfer, ConsoleColor.White);
                        ret.Add(pokemon);
                    }
                    else if (PokemonInfo.CalculatePokemonPerfection(pokemon) < filter.MinimumIVToConsiderKeep)
                    {
                        Logger.Write($"Find rubbish {pokemon.PokemonId} with too low IV: {PokemonInfo.CalculatePokemonPerfection(pokemon)}, CP:{pokemon.Cp}", LogLevel.Transfer, ConsoleColor.White);
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
