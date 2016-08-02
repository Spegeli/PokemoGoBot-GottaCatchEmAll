#region

using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Logging;

#endregion


namespace PokemonGo.RocketAPI.Console
{
    internal class Program
    {
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException
                += delegate (object sender, UnhandledExceptionEventArgs eargs)
                {
                    Exception exception = (Exception)eargs.ExceptionObject;
                    System.Console.WriteLine(@"Unhandled exception: " + exception);
                    Environment.Exit(1);
                };

            ServicePointManager.ServerCertificateValidationCallback = Validator;
            Logger.SetLogger();

            Logic.Logic logic = new Logic.Logic(new Settings());

            Task.Run(() =>
            {
                try
                {
                    logic.Execute().Wait();
                }
                catch (PtcOfflineException)
                {
                    Logger.Write("PTC Servers are probably down OR your credentials are wrong. Try google", LogLevel.Error);
                    Logger.Write("Trying again in 60 seconds...");
                    Thread.Sleep(60000);
                    logic = new Logic.Logic(new Settings());
                    logic.Execute().Wait();
                }
                catch (AccountNotVerifiedException)
                {
                    Logger.Write("Account not verified. - Exiting");
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Logger.Write($"Unhandled exception: {ex}", LogLevel.Error);
                    logic = new Logic.Logic(new Settings());
                    logic.Execute().Wait();
                }
            });


            Logger.Write("Please check your settings before logging in!", LogLevel.Console | LogLevel.Warning);
            Logger.Write("Command system is introduced! type console to enter console mode!", LogLevel.Console);

            CommandHost cmdHost = new CommandHost(logic);


            bool running = cmdHost.InvokeCommand(System.Console.ReadLine());
            while (running)
            {
                System.Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write(">");
                running = cmdHost.InvokeCommand(System.Console.ReadLine());
            }
        }

        public static bool Validator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
    }
}