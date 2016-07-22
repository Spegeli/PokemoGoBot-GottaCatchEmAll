using System;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.Helpers
{
    public static class Git
    {
        public static void CheckVersion()
        {
            try
            {
                var match =
                    new Regex(
                        @"\[assembly\: AssemblyVersion\(""(\d{1,})\.(\d{1,})\.(\d{1,})\.(\d{1,})""\)\]")
                        .Match(DownloadServerVersion());

                if (!match.Success) return;
                var gitVersion =
                    new Version(
                        $"{match.Groups[1]}.{match.Groups[2]}.{match.Groups[3]}.{match.Groups[4]}");
                if (gitVersion <= Assembly.GetExecutingAssembly().GetName().Version)
                {
                    Logger.Normal(
                        "Awesome! You have already got the newest version! " +
                        Assembly.GetExecutingAssembly().GetName().Version);
                    return;
                }


                Logger.Normal(
                    "There is a new Version available: https://github.com/Spegeli/Pokemon-Go-Rocket-API");
                Thread.Sleep(1000);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static string DownloadServerVersion()
        {
            //test
            using (var wC = new WebClient())
                return
                    wC.DownloadString(
                        "https://raw.githubusercontent.com/Spegeli/Pokemon-Go-Rocket-API/master/PokemonGo.RocketAPI/Properties/AssemblyInfo.cs");
        }
    }
}