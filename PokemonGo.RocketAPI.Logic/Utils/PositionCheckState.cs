using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using PokemonGo.RocketAPI.Logic.Logging;
using PokemonGo.RocketAPI.Rpc;

namespace PokemonGo.RocketAPI.Logic.Utils
{
    public class PositionCheckState
    {
        private static readonly string ConfigsPath = Path.Combine(Directory.GetCurrentDirectory(), "Settings");
        private static readonly string LastcoordsFile = Path.Combine(ConfigsPath, "LastCoords.ini");

        public static void Execute()
        {
            if (!File.Exists(LastcoordsFile)) return;
            var latLngFromFile = LoadPositionFromDisk();
            if (latLngFromFile == null) return;
            var distanceInMeters = LocationUtils.CalculateDistanceInMeters(latLngFromFile.Item1, latLngFromFile.Item2, Logic._clientSettings.DefaultLatitude, Logic._clientSettings.DefaultLongitude);
            var lastModified = File.Exists(LastcoordsFile) ? (DateTime?)File.GetLastWriteTime(LastcoordsFile) : null;
            if (lastModified == null) return;
            var minutesSinceModified = (DateTime.Now - lastModified).HasValue ? (double?)((DateTime.Now - lastModified).Value.Minutes) : null;
            if (minutesSinceModified == null || minutesSinceModified < 30) return; // Shouldn't really be null, but can be 0 and that's bad for division.
            var kmph = (distanceInMeters / 1000) / (minutesSinceModified / 60);
            if (kmph < 80) // If speed required to get to the default location is < 80km/hr
            {
                File.Delete(LastcoordsFile);
                Logger.Write("Detected realistic Traveling , using UserSettings.settings", LogLevel.Warning);
                //Client.SetCoordinates(_client.Settings.DefaultLatitude, _client.Settings.DefaultLongitude, _client.Settings.DefaultAltitude);
            }
            else
            {
                Logger.Write("Not realistic Traveling at " + kmph + ", using last saved Coords.ini", LogLevel.Warning);
            }
        }

        public static Tuple<double, double> LoadPositionFromDisk()
        {
            if (!Directory.Exists(ConfigsPath))
                Directory.CreateDirectory(ConfigsPath);
            if(File.Exists(LastcoordsFile) && File.ReadAllText(LastcoordsFile).Contains(":"))
            {
                var latlngFromFile = File.ReadAllText(LastcoordsFile);
                var latlng = latlngFromFile.Split(':');
                if (latlng[0].Length != 0 && latlng[1].Length != 0)
                {
                    try
                    {
                        var latitude = Convert.ToDouble(latlng[0]);
                        var longitude = Convert.ToDouble(latlng[1]);

                        if (Math.Abs(latitude) <= 90 && Math.Abs(longitude) <= 180)
                        {
                            return new Tuple<double, double>(latitude, longitude);
                        }
                        Logger.Write("Coordinates in \"\\Settings\\LastCoords.ini\" file are invalid, using the default coordinates", LogLevel.Error);
                        return null;
                    }
                    catch (FormatException)
                    {
                        Logger.Write("Coordinates in \"\\Settings\\LastCoords.ini\" file are invalid, using the default coordinates", LogLevel.Error);
                        return null;
                    }
                }
            }

            return null;
        }
    }
}
