#region

using System;
using System.IO;
using System.Text;

#endregion


namespace PokemonGo.RocketAPI.Logging
{
    /// <summary>
    /// Generic logger which can be used across the projects.
    /// Logger should be set to properly log.
    /// </summary>
    public class Logger
    {
        static string _currentFile = string.Empty;
        static string path = Path.Combine(Directory.GetCurrentDirectory(), "Logs");

        //private static Logger _logger;

        /// <summary>
        /// Set the logger. All future requests to <see cref="Write(string, LogLevel)"/> will use that logger, any old will be unset.
        /// </summary>
        /// <param name="logger"></param>
        public static void SetLogger()
		{
            if (!Directory.Exists(path))
            {
                DirectoryInfo di = Directory.CreateDirectory(path);
            }
            _currentFile = DateTime.Now.ToString("yyyy-MM-dd - HH.mm.ss");
            Log($"Initializing Rocket logger @ {DateTime.Now}...");
        }

        /// <summary>
        ///     Log a specific message to the logger setup by <see cref="SetLogger(ILogger)" /> .
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">Optional level to log. Default <see cref="LogLevel.Info" />.</param>
        /// <param name="color">Optional. Default is automatic color.</param>
        public static void Write(string message, LogLevel level = LogLevel.None, ConsoleColor color = ConsoleColor.White)
        {
            Console.OutputEncoding = Encoding.Unicode;

            switch (level)
            {
                case LogLevel.Info:
                    System.Console.ForegroundColor = ConsoleColor.DarkGreen;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (INFO) {message}");
                    break;
                case LogLevel.Warning:
                    System.Console.ForegroundColor = ConsoleColor.DarkYellow;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (ATTENTION) {message}");
                    break;
                case LogLevel.Error:
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (ERROR) {message}");
                    break;
                case LogLevel.Debug:
                    System.Console.ForegroundColor = ConsoleColor.Gray;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (DEBUG) {message}");
                    break;
                case LogLevel.Navigation:
                    System.Console.ForegroundColor = ConsoleColor.DarkCyan;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (NAVIGATION) {message}");
                    break;
                case LogLevel.Pokestop:
                    System.Console.ForegroundColor = ConsoleColor.Cyan;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (POKESTOP) {message}");
                    break;
                case LogLevel.Pokemon:
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (PKMN) {message}");
                    break;
                case LogLevel.Transfer:
                    System.Console.ForegroundColor = ConsoleColor.White;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (TRANSFER) {message}");
                    break;
                case LogLevel.Evolve:
                    System.Console.ForegroundColor = ConsoleColor.White;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (EVOLVE) {message}");
                    break;
                case LogLevel.Berry:
                    System.Console.ForegroundColor = ConsoleColor.Magenta;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (BERRY) {message}");
                    break;
                case LogLevel.Egg:
                    System.Console.ForegroundColor = ConsoleColor.White;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (EGG) {message}");
                    break;
                case LogLevel.Recycling:
                    System.Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (RECYCLING) {message}");
                    break;
                case LogLevel.None:
                    System.Console.ForegroundColor = color;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");
                    break;
                default:
                    System.Console.ForegroundColor = ConsoleColor.White;
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");
                    break;
            }
            Log(string.Concat($"[{DateTime.Now.ToString("HH:mm:ss")}] ", message));
        }

        private static void Log(string message)
        {
            // maybe do a new log rather than appending?
            using (var log = File.AppendText(Path.Combine(path, _currentFile + ".txt")))
            {
                log.WriteLine(message);
                log.Flush();
            }
        }
    }

    public enum LogLevel
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Debug = 4,
        Navigation = 5,
        Pokestop = 6,
        Pokemon = 7,
        Transfer = 8,
        Evolve = 9,
        Berry = 10,
        Egg = 11,
        Recycling = 12
    }
}