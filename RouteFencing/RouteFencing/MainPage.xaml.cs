using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
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
        private bool locationAccess = false;

        public MainPage()
        {
            this.InitializeComponent();

            geolocator = new Geolocator();

            Summary.Text = "Locating your current position...";

            GeofenceMonitor.Current.GeofenceStateChanged += OnGeofenceStateChanged;
            geolocator.StatusChanged += Geolocator_StatusChanged;
            geolocator.PositionChanged += PositionChanged;
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

        private async void Geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            //Waiting to get the location
            //If the location settings are changed between the proces, this will detect the change and will get the current location
            await this.CallOnUiThreadAsync(async () =>
            {
                switch (args.Status)
                {
                    case PositionStatus.Ready:
                        await this.getCurrentLocation();
                        
                        break;
                    case PositionStatus.Initializing:
                        break;
                    case PositionStatus.NoData:
                    case PositionStatus.Disabled:
                    case PositionStatus.NotInitialized:
                    case PositionStatus.NotAvailable:
                    default:
                        await this.getCurrentLocation();
                        break;
                }
            });
        }

        private async Task CallOnUiThreadAsync(DispatchedHandler handler) => await
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, handler);

        async private void PositionChanged(Geolocator sender, PositionChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //Updating current location
                currentLocation = e.Position.Coordinate;

                //Updating mapicon of the current location
                InputMap.MapElements.Clear();
                GeofenceMonitor.Current.Geofences.Clear();
                AddMapIcon(currentLocation, "You are here");
            });
            
        }

        private async Task getCurrentLocation()
        {
            //Request the current location with the geolocator
            var accessStatus = await Geolocator.RequestAccessAsync();

            //Check the response type
            switch (accessStatus)
            {
                case GeolocationAccessStatus.Allowed:
                    //Getting location is enabled
                    Summary.Text = "Locating your current position...";
                    pos = await geolocator.GetGeopositionAsync();
                    currentLocation = pos.Coordinate;
                    Summary.Text = "Location found";

                    //Zooming to current location
                    InputMap.Center = currentLocation.Point;
                    InputMap.ZoomLevel = 8;
                    break;
                case GeolocationAccessStatus.Denied:
                    //Getting location is disabled, show a link to the settings
                    Summary.Text = "";
                    Hyperlink link = new Hyperlink();

                    Summary.Inlines.Add(new Run()
                    {
                        Text = "Access to current location denied."
                    });
                    Summary.Inlines.Add(new LineBreak());
                    Summary.Inlines.Add(new Run()
                    {
                        Text = "Check your "
                    });

                    link.Inlines.Add(new Run()
                    {
                        Text = "location settings",
                        Foreground = new SolidColorBrush(Colors.White)
                    });
                    link.NavigateUri = new Uri("ms-settings:privacy-location");

                    Summary.Inlines.Add(link);
                    break;
                case GeolocationAccessStatus.Unspecified:
                    break;
            }
        }

        public async void OnGeofenceStateChanged(GeofenceMonitor sender, object e)
        {
            var reports = sender.ReadReports();

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (GeofenceStateChangeReport report in reports)
                {
                    GeofenceState state = report.NewState;

                    Geofence geofence = report.Geofence;

                    if (state == GeofenceState.Removed)
                    {
                        // remove the geofence from the geofences collection
                        GeofenceMonitor.Current.Geofences.Remove(geofence);
                    }
                    else if (state == GeofenceState.Entered)
                    {
                        // Your app takes action based on the entered event

                        // NOTE: You might want to write your app to take particular
                        // action based on whether the app has internet connectivity.

                    }
                    else if (state == GeofenceState.Exited)
                    {
                        // Your app takes action based on the exited event

                        // NOTE: You might want to write your app to take particular
                        // action based on whether the app has internet connectivity.
                    }
                }
            });
        }
    }
}
