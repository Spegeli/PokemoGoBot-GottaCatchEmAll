using System;

namespace PokemonGo.RocketAPI.Exceptions
{
    [Serializable]
    public class AccountNotVerifiedException : Exception
    {
        public AccountNotVerifiedException(string message) : base(message)
        {
            
        }
    }
}