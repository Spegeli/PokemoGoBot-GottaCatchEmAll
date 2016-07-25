using System;
using System.Globalization;

namespace PokemonGo.RocketAPI.Logic
{
    //Thanks to https://gist.github.com/atsushieno/377377
    public class GeoCoordinate : IEquatable<GeoCoordinate>
    {
        public static readonly GeoCoordinate Unknown = new GeoCoordinate();

        public GeoCoordinate()
        {
        }

        public GeoCoordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public GeoCoordinate(double latitude, double longitude, double altitude)
            : this(latitude, longitude)
        {
            Altitude = altitude;
        }

        public GeoCoordinate(double latitude, double longitude, double altitude, double horizontalAccuracy, double verticalAccuracy, double speed, double course)
            : this(latitude, longitude, altitude)
        {
            HorizontalAccuracy = horizontalAccuracy;
            VerticalAccuracy = verticalAccuracy;
            Speed = speed;
            Course = course;
        }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double HorizontalAccuracy { get; set; }
        public double VerticalAccuracy { get; set; }
        public double Speed { get; set; }
        public double Course { get; set; }

        public bool IsUnknown
        {
            get { return Object.ReferenceEquals(this, Unknown); }
        }

        public bool Equals(GeoCoordinate other)
        {
            return other != null && Latitude == other.Latitude && Longitude == other.Longitude;
        }

        public static bool operator ==(GeoCoordinate left, GeoCoordinate right)
        {
            if (Object.ReferenceEquals(left, null))
                return Object.ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(GeoCoordinate left, GeoCoordinate right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            var g = obj as GeoCoordinate;
            return g != null && Equals(g);
        }

        //Thanks to http://stackoverflow.com/a/13429321/1798015
        public double GetDistanceTo(GeoCoordinate other)
        {
            if (double.IsNaN(this.Latitude) || double.IsNaN(this.Longitude) || double.IsNaN(other.Latitude) || double.IsNaN(other.Longitude))
            {
                throw new ArgumentException(/*SR.GetString(*/"Argument_LatitudeOrLongitudeIsNotANumber"/*)*/);
            }
            else
            {
                double latitude = this.Latitude * 0.0174532925199433;
                double longitude = this.Longitude * 0.0174532925199433;
                double num = other.Latitude * 0.0174532925199433;
                double longitude1 = other.Longitude * 0.0174532925199433;
                double num1 = longitude1 - longitude;
                double num2 = num - latitude;
                double num3 = Math.Pow(Math.Sin(num2 / 2), 2) + Math.Cos(latitude) * Math.Cos(num) * Math.Pow(Math.Sin(num1 / 2), 2);
                double num4 = 2 * Math.Atan2(Math.Sqrt(num3), Math.Sqrt(1 - num3));
                double num5 = 6376500 * num4;
                return num5;
            }
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
