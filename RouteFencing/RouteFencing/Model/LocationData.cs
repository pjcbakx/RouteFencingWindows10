using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace RouteFencing.Model
{
    class LocationData
    {
        public String name { get; }
        private BasicGeoposition position;

        public LocationData(String name, double latitude, double longitude)
        {
            this.name = name;
            this.position = new BasicGeoposition();
            this.position.Longitude = longitude;
            this.position.Latitude = latitude;
        }

        public BasicGeoposition getPosition()
        { return position; }
    }
}
