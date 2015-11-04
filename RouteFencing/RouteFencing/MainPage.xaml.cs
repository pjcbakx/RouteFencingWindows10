using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RouteFencing
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Geocoordinate currentLocation;
        Geolocator geolocator;
        Geoposition pos;

        public MainPage()
        {
            this.InitializeComponent();

            geolocator = new Geolocator();
            getCurrentLocation();
        }

        private async void getCurrentLocation()
        {

            var accessStatus = await Geolocator.RequestAccessAsync();

            switch (accessStatus)
            {
                case GeolocationAccessStatus.Allowed:
                    pos = await geolocator.GetGeopositionAsync();
                    currentLocation = pos.Coordinate;
                    geolocator.PositionChanged += PositionChanged;
                    break;
                case GeolocationAccessStatus.Denied:
                    break;
                case GeolocationAccessStatus.Unspecified:
                    break;
            }
        }

        async private void PositionChanged(Geolocator sender, PositionChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                currentLocation = e.Position.Coordinate;

                InputMap.MapElements.Clear();
                GeofenceMonitor.Current.Geofences.Clear();
                AddMapIcon(currentLocation, "U bent hier");
            });
        }

        private void AddMapIcon(Geocoordinate location, String name)
        {
            //MapIcon
            MapIcon MapIcon = new MapIcon();
            MapIcon.Location = location.Point;
            MapIcon.NormalizedAnchorPoint = new Point(0.5, 1.0);
            MapIcon.Title = name;
            InputMap.MapElements.Add(MapIcon);

            // Geofence
            BasicGeoposition pos = new BasicGeoposition();
            pos.Latitude = MapIcon.Location.Position.Latitude;
            pos.Longitude = MapIcon.Location.Position.Longitude;
            Geocircle circle = new Geocircle(pos, 35);
            MonitoredGeofenceStates monitoredStates =
                MonitoredGeofenceStates.Entered |
                MonitoredGeofenceStates.Exited |
                MonitoredGeofenceStates.Removed;
            TimeSpan dwellTime = TimeSpan.FromSeconds(1);
            var geofence = new Windows.Devices.Geolocation.Geofencing.Geofence(name, circle, monitoredStates, false, dwellTime);
            GeofenceMonitor.Current.Geofences.Add(geofence);
        }
    }
}
