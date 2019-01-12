using System;

namespace SharpOSM
{
    /// <summary>
    /// Helper functions for Mercator coordinates conversions
    /// </summary>
    internal static class MercatorHelper
    {
        private const int EarthRadius = 6378137;
        private const double OriginShift_180 = Math.PI * EarthRadius / 180;
        private const double PI_360 = Math.PI / 360;
        private const double PI_180 = Math.PI / 180;

        /// <summary>
        /// ToMeters()
        /// Converts given lat/lon in WGS84 Datum to XY in Spherical Mercator Google/Bing EPSG:900913
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 ToMeters(Vector2 v)
        {
            return ToMeters(v.X, v.Y);
        }

        public static Vector2 ToMeters(double lat, double lon)
        {
            return new Vector2(
                OriginShift_180 * lon, 
                OriginShift_180 * Math.Log(Math.Tan((90 + lat) * PI_360)) / PI_180);
        }
    }
}
