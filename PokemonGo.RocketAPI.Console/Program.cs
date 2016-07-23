#region

using System;
using System.Threading;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Exceptions;

#endregion


namespace PokemonGo.RocketAPI.Console
{
    internal class Program
    {

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException
                += delegate (object sender, UnhandledExceptionEventArgs eargs)
                {
                    Exception exception = (Exception)eargs.ExceptionObject;
                    System.Console.WriteLine("Unhandled exception: " + exception);
                    Environment.Exit(1);
                };
                
            Logger.SetLogger(new Logging.ConsoleLogger(LogLevel.Info));

            Task.Run(() =>
            {
                try
                {
                    new Logic.Logic(new Settings()).Execute().Wait();
                }
                catch (PtcOfflineException)
                {
                    Logger.Error("PTC Servers are probably down OR your credentials are wrong. Try google");
                    Logger.Error("Trying again in 60 seconds...");
                    Thread.Sleep(60000);
                    new Logic.Logic(new Settings()).Execute().Wait();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Unhandled exception: {ex}");
                }
            });
             System.Console.ReadLine();
        }
    }
}
