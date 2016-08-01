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
        private static string _currentFile = string.Empty;
        private static readonly string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Logs");

        //private static Logger _logger;

        /// <summary>
        /// Set the logger. All future requests to <see cref="Write(string,LogLevel,ConsoleColor)"/> will use that logger, any old will be unset.
        /// </summary>
        public static void SetLogger()
		{
            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }
            _currentFile = DateTime.Now.ToString("yyyy-MM-dd - HH.mm.ss");
            Log($"Initializing Rocket logger @ {DateTime.Now}...");
        }

        /// <summary>
        ///     Log a specific message to the logger setup by <see cref="SetLogger()" /> .
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
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (INFO) {message}");
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (ATTENTION) {message}");
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (ERROR) {message}");
                    break;
                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (DEBUG) {message}");
                    break;
                case LogLevel.Navigation:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (NAVIGATION) {message}");
                    break;
                case LogLevel.Pokestop:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (POKESTOP) {message}");
                    break;
                case LogLevel.Pokemon:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (PKMN) {message}");
                    break;
                case LogLevel.Transfer:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (TRANSFER) {message}");
                    break;
                case LogLevel.Evolve:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (EVOLVE) {message}");
                    break;
                case LogLevel.Berry:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (BERRY) {message}");
                    break;
                case LogLevel.Egg:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (EGG) {message}");
                    break;
                case LogLevel.Incense:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (INSENCE) {message}");
                    break;
                case LogLevel.Recycling:
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (RECYCLING) {message}");
                    break;
                case LogLevel.None:
                    Console.ForegroundColor = color;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");
                    break;
            }
            Log(string.Concat($"[{DateTime.Now.ToString("HH:mm:ss")}] ", message));
        }

        private static void Log(string message)
        {
            // maybe do a new log rather than appending?
            using (var log = File.AppendText(System.IO.Path.Combine(Path, _currentFile + ".txt")))
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
        Incense = 12,
        Recycling = 13
    }
}