using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;

namespace RouteFencing.Model
{
    class LocationData
    {
        public String name { get; }
        private BasicGeoposition position;
        public Geofence geofence;
        public int standardGeofenceRadius;
        public int geofenceRadius;

        public LocationData(String name, double latitude, double longitude, int geofenceradius)
        {
            this.name = name;
            this.position = new BasicGeoposition();
            this.position.Longitude = longitude;
            this.position.Latitude = latitude;
            standardGeofenceRadius = geofenceradius;
        }

        public BasicGeoposition getPosition()
        { return position; }

        #region Geofence functions
        public void MakeNewGeofence(int radius)
        {
            Geocircle circle = new Geocircle(position, radius);
            MonitoredGeofenceStates monitoredStates =
                MonitoredGeofenceStates.Entered |
                MonitoredGeofenceStates.Exited |
                MonitoredGeofenceStates.Removed;
            TimeSpan dwellTime = TimeSpan.FromSeconds(1);
            geofenceRadius = radius;
            geofence = new Geofence(name, circle, monitoredStates, false, dwellTime);
        }

        private BasicGeoposition GetAtDistanceBearing(BasicGeoposition point, double distance, double bearing)
        {
            double degreesToRadian = Math.PI / 180.0;
            double radianToDegrees = 180.0 / Math.PI;
            double earthRadius = 6378137.0;

            double latA = point.Latitude * degreesToRadian;
            double lonA = point.Longitude * degreesToRadian;
            double angularDistance = distance / earthRadius;
            double trueCourse = bearing * degreesToRadian;

            double lat = Math.Asin(
                Math.Sin(latA) * Math.Cos(angularDistance) +
                Math.Cos(latA) * Math.Sin(angularDistance) * Math.Cos(trueCourse));

            double dlon = Math.Atan2(
                Math.Sin(trueCourse) * Math.Sin(angularDistance) * Math.Cos(latA),
                Math.Cos(angularDistance) - Math.Sin(latA) * Math.Sin(lat));

            double lon = ((lonA + dlon + Math.PI) % (Math.PI * 2)) - Math.PI;

            BasicGeoposition result = new BasicGeoposition { Latitude = lat * radianToDegrees, Longitude = lon * radianToDegrees };

            return result;
        }

        public IList<BasicGeoposition> GetCirclePoints(double radius)
        {
            int nrOfPoints = 50;
            double angle = 360.0 / nrOfPoints;
            List<BasicGeoposition> locations = new List<BasicGeoposition>();
            for (int i = 0; i <= nrOfPoints; i++)
            {
                locations.Add(GetAtDistanceBearing(position, radius, angle * i));
            }
            return locations;
        }
        #endregion
    }
}
