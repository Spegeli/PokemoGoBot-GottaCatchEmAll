#region

using System;
using System.Globalization;

#endregion


namespace PokemonGo.RocketAPI.Logic.Utils
{
    //Thanks to https://gist.github.com/atsushieno/377377
    public class GeoUtils : IEquatable<GeoUtils>
    {
        public static readonly GeoUtils Unknown = new GeoUtils();

        public GeoUtils()
        {
        }

        public GeoUtils(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public GeoUtils(double latitude, double longitude, double altitude)
            : this(latitude, longitude)
        {
            Altitude = altitude;
        }

        public GeoUtils(double latitude, double longitude, double altitude, double horizontalAccuracy, double verticalAccuracy, double speed, double course)
            : this(latitude, longitude, altitude)
        {
            HorizontalAccuracy = horizontalAccuracy;
            VerticalAccuracy = verticalAccuracy;
            Speed = speed;
            Course = course;
        }

        public double Latitude { get; }
        public double Longitude { get; }
        public double Altitude { get; set; }
        public double HorizontalAccuracy { get; set; }
        public double VerticalAccuracy { get; set; }
        public double Speed { get; set; }
        public double Course { get; set; }

        public bool IsUnknown => ReferenceEquals(this, Unknown);

        public bool Equals(GeoUtils other)
        {
            return other != null && Latitude == other.Latitude && Longitude == other.Longitude;
        }

        public static bool operator ==(GeoUtils left, GeoUtils right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.Equals(right);
        }

        public static bool operator !=(GeoUtils left, GeoUtils right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            var g = obj as GeoUtils;
            return g != null && Equals(g);
        }

        //Thanks to http://stackoverflow.com/a/13429321/1798015
        public double GetDistanceTo(GeoUtils other)
        {
            if (double.IsNaN(Latitude) || double.IsNaN(Longitude) || double.IsNaN(other.Latitude) || double.IsNaN(other.Longitude))
            {
                throw new ArgumentException(/*SR.GetString(*/"Argument_LatitudeOrLongitudeIsNotANumber"/*)*/);
            }
            var latitude = Latitude * 0.0174532925199433;
            var longitude = Longitude * 0.0174532925199433;
            var num = other.Latitude * 0.0174532925199433;
            var longitude1 = other.Longitude * 0.0174532925199433;
            var num1 = longitude1 - longitude;
            var num2 = num - latitude;
            var num3 = Math.Pow(Math.Sin(num2 / 2), 2) + Math.Cos(latitude) * Math.Cos(num) * Math.Pow(Math.Sin(num1 / 2), 2);
            var num4 = 2 * Math.Atan2(Math.Sqrt(num3), Math.Sqrt(1 - num3));
            var num5 = 6376500 * num4;
            return num5;
        }

        public override int GetHashCode()
        {
            return (Latitude * 100 + Longitude).GetHashCode();
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "({0},{1})", Latitude, Longitude);
        }
    }
}
