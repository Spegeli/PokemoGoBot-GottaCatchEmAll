using System;
using System.Collections.Generic;
using PokemonGo.RocketAPI.Logging;

namespace PokemonGo.RocketAPI.Console
{
    public struct CommandRecord
    {
        public string commandName, helpText;
        public bool avaliableBeforeLogin;
        public CommandHost.CommandHandler handler;
    }

    public class CommandHost
    {
        public delegate void CommandHandler(Logic.Logic logic, string param);
        Dictionary<String, CommandRecord> commandMap;

        private Logic.Logic logic;

        public CommandHost(Logic.Logic logic)
        {
            commandMap = new Dictionary<String, CommandRecord>();
            this.logic = logic;

            RegisterCommand("help", "Show avaliable commands", Command_Help, true);
            RegisterCommand("clear", "Clear the console display", Command_Clear, true);
            RegisterCommand("console", "Toggle the console mode, no general information will be displayed in this mode", Command_Console, true);
            RegisterCommand("logger", "Enable or disable the logger, if enabled, the log will be saved to disk", Command_Logger, true);

            RegisterCommand("pokemon", "View, rank, export list, remove duplicated pokemons, format: pokemon <highest|export|removedupe>", Command_Pokemon);
            RegisterCommand("items", "View items, recycle items, format: items [recycle]", Command_Items);
            RegisterCommand("settings", "View settings, format: [reload]", Command_Settings);
            RegisterCommand("location", "Retrieve/Set the current location", Command_Location);
        }

        #region Core
        public void RegisterCommand(string commandName, string helpText, CommandHandler handler, bool avaliableBeforeLogin = false)
        {
            CommandRecord record = new CommandRecord();
            record.commandName = commandName;
            record.helpText = helpText;
            record.handler = handler;
            record.avaliableBeforeLogin = avaliableBeforeLogin;
            commandMap.Add(commandName, record);
        }

        public bool InvokeCommand(string inputLine)
        {
            inputLine = inputLine.TrimStart();

            int commandNameLength = 0;

            for (; commandNameLength < inputLine.Length && inputLine[commandNameLength] != ' '; commandNameLength++) ;

            string commandName = inputLine.Substring(0, commandNameLength);
            string param = inputLine.Substring(commandNameLength).TrimStart();

            if (commandName == "quit")
            {
                return false;
            }else if (commandMap.ContainsKey(commandName))
            {
                CommandRecord record = commandMap[commandName];

                if (!(record.avaliableBeforeLogin) && !(logic.loggedIn))
                {
                    Logger.Write($"Command {commandName} is not avaliable before login!", LogLevel.Error | LogLevel.Console);
                    return true;
                }

                CommandHandler handler = record.handler;
                handler.Invoke(logic, param);
                return true;
            }
            else
            {
                Logger.Write("Unknown command " + commandName, LogLevel.Console);
                return true;
            }
        }

        void Command_Help(Logic.Logic logic, string param)
        {
            string helpText = "Avaliable commands: \r\nquit: Exit the program";
            foreach (string s in commandMap.Keys){
                helpText += "\r\n" + s + ": ";
                helpText += commandMap[s].helpText;
            }
            Logger.Write(helpText, LogLevel.Console);
        }

        void Command_Clear(Logic.Logic logic, string param)
        {
            System.Console.Clear();
        }

        void Command_Logger(Logic.Logic logic, string param)
        {
            if (param.StartsWith("enable"))
                Logger.logToFile = true;
            else if (param.StartsWith("disable"))
                Logger.logToFile = false;
        

            Logger.Write("\"Log to file\" function is " + (Logger.logToFile? "enabled" : "disabled") + "\r\n Command format: logger <enable|disable>", LogLevel.Console);
        }

        void Command_Console(Logic.Logic logic, string param)
        {
            Logger.consoleMode = !Logger.consoleMode;
            
            if (Logger.consoleMode)
            {
                Logger.Write("Returned to normal mode", LogLevel.Console);
            }
            else
            {
                Logger.Write("Entered console mode, type console again to return to normal mode", LogLevel.Console);
            }
        }
        #endregion

        void Command_Pokemon(Logic.Logic logic, string param)
        {
            int commandNameLength = 0;
            for (; commandNameLength < param.Length && param[commandNameLength] != ' '; commandNameLength++) ;
            string commandName = param.Substring(0, commandNameLength);

            if (commandName.StartsWith("highest"))
            {
                logic.DisplayHighests(LogLevel.Console).Wait();
            }
            else if (commandName.StartsWith("export"))
            {
                logic.ExportPokemonData().Wait();
                Logger.Write("All pokemon data has been exported to a csv file!", LogLevel.Console);
            }
            else if (commandName.StartsWith("removedupe"))
            {
                Logger.Write("Start removing duplicated pokemons", LogLevel.Console);
                logic.TransferPokemon(LogLevel.Console).Wait();
                Logger.Write("Finished removing duplicated pokemons", LogLevel.Console);
            }
            else
            {
                logic.ListPokemon().Wait();
            }
        }

        void Command_Items(Logic.Logic logic, string param)
        {
            int commandNameLength = 0;
            for (; commandNameLength < param.Length && param[commandNameLength] != ' '; commandNameLength++) ;
            string commandName = param.Substring(0, commandNameLength);

            if (commandName.StartsWith("recycle"))
            {
                Logger.Write("Start recycling items", LogLevel.Console);
                logic.RecycleItems(LogLevel.Console).Wait();
                Logger.Write("Finished recycling items", LogLevel.Console);
            }
            else
            {
                logic.ListItems().Wait();
            }
        }

        void Command_Location(Logic.Logic logic, string param)
        {
            logic.LocationCommandHandler(param);
        }

        void Command_Settings(Logic.Logic logic, string param)
        {
            logic.SettingCommandHandler(param);
        }
    }
}
