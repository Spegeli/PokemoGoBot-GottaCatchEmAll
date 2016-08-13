#region

using System;
//using System.Device.Location;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Logic.Utils;
using PokemonGo.RocketAPI.Helpers;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

#endregion


namespace PokemonGo.RocketAPI.Logic
{
    public class Navigation
    {
        private const double SpeedDownTo = 10 / 3.6;

        public static async Task<PlayerUpdateResponse> HumanLikeWalking(GeoUtils targetLocation,Func<Task<bool>> functionExecutedWhileWalking)
        {
            double walkingSpeedInKilometersPerHour = 0;
            if (Logic._client.Settings.WalkingSpeedInKilometerPerHour == 0)
            {
                Random random = new Random();
                walkingSpeedInKilometersPerHour = random.NextDouble() * (8 - 4) + 4;
            }
            else
            {
                walkingSpeedInKilometersPerHour = Logic._client.Settings.WalkingSpeedInKilometerPerHour;
            }
            var speedInMetersPerSecond = walkingSpeedInKilometersPerHour / 3.6;

            var sourceLocation = new GeoUtils(Logic._client.CurrentLatitude, Logic._client.CurrentLongitude);
            var distanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation);
            Logger.Write($"Distance to target location: {distanceToTarget:0.##} meters. Will take {distanceToTarget / speedInMetersPerSecond:0.##} seconds!", LogLevel.Navigation);

            var nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation);
            var nextWaypointDistance = speedInMetersPerSecond;
            var waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing);

            //Initial walking
            var requestSendDateTime = DateTime.Now;
            PlayerUpdateResponse result;
            await Logic._client.Player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude, Logic._client.Settings.DefaultAltitude);

            do
            {
                var millisecondsUntilGetUpdatePlayerLocationResponse = (DateTime.Now - requestSendDateTime).TotalMilliseconds;

                sourceLocation = new GeoUtils(Logic._client.CurrentLatitude, Logic._client.CurrentLongitude);
                var currentDistanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation);
                Logger.Write($"Distance to target location: {currentDistanceToTarget:0.##} meters. Will take {currentDistanceToTarget / speedInMetersPerSecond:0.##} seconds!", LogLevel.Debug);

                nextWaypointDistance = Math.Min(currentDistanceToTarget,
                    millisecondsUntilGetUpdatePlayerLocationResponse / 1000 * speedInMetersPerSecond);
                nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation);
                waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing);

                requestSendDateTime = DateTime.Now;
                result =
                    await
                        Logic._client.Player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude,
                            Logic._client.Settings.DefaultAltitude);

                if (functionExecutedWhileWalking != null)
                    await functionExecutedWhileWalking();// look for pokemon

            } while (LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation) >= 35);

            return result;
        }

        public static async Task<PlayerUpdateResponse> HumanPathWalking(GpxReader.Trkpt trk, Func<Task<bool>> functionExecutedWhileWalking)
        {
            var targetLocation = new GeoUtils(Convert.ToDouble(trk.Lat), Convert.ToDouble(trk.Lon));

            double walkingSpeedInKilometersPerHour = 0;
            if ( Logic._client.Settings.WalkingSpeedInKilometerPerHour == 0 )
            {
                Random random = new Random();
                walkingSpeedInKilometersPerHour = random.NextDouble() * (8 - 4) + 4;
            }
            else
            {
                walkingSpeedInKilometersPerHour = Logic._client.Settings.WalkingSpeedInKilometerPerHour;
            }
            
            var speedInMetersPerSecond = walkingSpeedInKilometersPerHour / 3.6;

            var sourceLocation = new GeoUtils(Logic._client.CurrentLatitude, Logic._client.CurrentLongitude);
            var distanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation);
            Logger.Write($"Distance to target location: {distanceToTarget:0.##} meters. Will take {distanceToTarget/speedInMetersPerSecond:0.##} seconds!", LogLevel.Navigation);

            var nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation);
            var nextWaypointDistance = speedInMetersPerSecond;
            var waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing, Convert.ToDouble(trk.Ele));

            //Initial walking
            var requestSendDateTime = DateTime.Now;
            PlayerUpdateResponse result;
            await Logic._client.Player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude, Logic._client.Settings.DefaultAltitude);

            do
            {
                var millisecondsUntilGetUpdatePlayerLocationResponse = (DateTime.Now - requestSendDateTime).TotalMilliseconds;

                sourceLocation = new GeoUtils(Logic._client.CurrentLatitude, Logic._client.CurrentLongitude);
                var currentDistanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation);
                Logger.Write($"Distance to target location: {currentDistanceToTarget:0.##} meters. Will take {currentDistanceToTarget / speedInMetersPerSecond:0.##} seconds!", LogLevel.Debug);

                nextWaypointDistance = Math.Min(currentDistanceToTarget,
                    millisecondsUntilGetUpdatePlayerLocationResponse / 1000 * speedInMetersPerSecond);
                nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation);
                waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing);

                requestSendDateTime = DateTime.Now;
                result =
                    await
                        Logic._client.Player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude,
                            waypoint.Altitude);

                if (functionExecutedWhileWalking != null)
                    await functionExecutedWhileWalking();// look for pokemon

            } while (LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation) >= 35);

            return result;
        }

        public static FortData[] PathByNearestNeighbour(FortData[] pokeStops)
        {
            for (var i = 1; i < pokeStops.Length - 1; i++)
            {
                var closest = i + 1;
                var cloestDist = LocationUtils.CalculateDistanceInMeters(pokeStops[i].Latitude, pokeStops[i].Longitude, pokeStops[closest].Latitude, pokeStops[closest].Longitude);
                for (var j = closest; j < pokeStops.Length; j++)
                {
                    var initialDist = cloestDist;
                    var newDist = LocationUtils.CalculateDistanceInMeters(pokeStops[i].Latitude, pokeStops[i].Longitude, pokeStops[j].Latitude, pokeStops[j].Longitude);
                    if (!(initialDist > newDist)) continue;
                    cloestDist = newDist;
                    closest = j;
                }
                var tmpPok = pokeStops[closest];
                pokeStops[closest] = pokeStops[i + 1];
                pokeStops[i + 1] = tmpPok;
            }

            return pokeStops;
        }
    }
}