using RouteFencing.Model;
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
using Windows.Services.Maps;
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
        private List<LocationData> routeLocations;

        public MainPage()
        {
            this.InitializeComponent();

            geolocator = new Geolocator();
            routeLocations = new List<LocationData>();
            routeLocations.Add(new LocationData("Lovensdijkstraat 63", 51.58541489, 4.79325905));
            routeLocations.Add(new LocationData("Hogeschoollaan", 51.58405327, 4.79573339));

            Summary.Text = "Locating your current position...";

            GeofenceMonitor.Current.GeofenceStateChanged += OnGeofenceStateChanged;
            geolocator.StatusChanged += Geolocator_StatusChanged;
            geolocator.PositionChanged += PositionChanged;
        }

        private void AddMapIcon(LocationData location)
        {
            MapIcon mapIcon = new MapIcon();
            mapIcon.Location = new Geopoint(location.getPosition());
            mapIcon.NormalizedAnchorPoint = new Point(0.5, 1.0);
            mapIcon.Title = location.getName();
            InputMap.MapElements.Add(mapIcon);

            // Geofence
            BasicGeoposition pos = new BasicGeoposition();
            pos.Latitude = mapIcon.Location.Position.Latitude;
            pos.Longitude = mapIcon.Location.Position.Longitude;
            Geocircle circle = new Geocircle(pos, 35);
            MonitoredGeofenceStates monitoredStates =
                MonitoredGeofenceStates.Entered |
                MonitoredGeofenceStates.Exited |
                MonitoredGeofenceStates.Removed;
            TimeSpan dwellTime = TimeSpan.FromSeconds(1);
            var geofence = new Windows.Devices.Geolocation.Geofencing.Geofence(location.getName(), circle, monitoredStates, false, dwellTime);
            GeofenceMonitor.Current.Geofences.Add(geofence);
        }

        private void AddMapIcon(Geocoordinate location, String name)
        {
            //MapIcon
            MapIcon mapIcon = new MapIcon();
            mapIcon.Location = location.Point;
            mapIcon.NormalizedAnchorPoint = new Point(0.5, 1.0);
            mapIcon.Title = name;
            InputMap.MapElements.Add(mapIcon);

            // Geofence
            BasicGeoposition pos = new BasicGeoposition();
            pos.Latitude = mapIcon.Location.Position.Latitude;
            pos.Longitude = mapIcon.Location.Position.Longitude;
            Geocircle circle = new Geocircle(pos, 35);
            MonitoredGeofenceStates monitoredStates =
                MonitoredGeofenceStates.Entered |
                MonitoredGeofenceStates.Exited |
                MonitoredGeofenceStates.Removed;
            TimeSpan dwellTime = TimeSpan.FromSeconds(1);
            var geofence = new Windows.Devices.Geolocation.Geofencing.Geofence(name, circle, monitoredStates, false, dwellTime);
            GeofenceMonitor.Current.Geofences.Add(geofence);
        }

        private async void getRouteWithCurrentLocation(Geopoint startLoc, LocationData endLoc)
        {
            BasicGeoposition endLocation = endLoc.getPosition();
            Geopoint endPoint = new Geopoint(endLocation);

            GetRouteAndDirections(startLoc, endPoint, true, Colors.Red);
        }

        private async void getRoute(LocationData startLoc, LocationData endLoc)
        {
            BasicGeoposition startLocation = startLoc.getPosition();
            Geopoint startPoint = new Geopoint(startLocation);

            BasicGeoposition endLocation = endLoc.getPosition();
            Geopoint endPoint = new Geopoint(endLocation);

            GetRouteAndDirections(startPoint, endPoint, false, Colors.Orange);
        }

        private async void GetRouteAndDirections(Geopoint startPoint, Geopoint endPoint, bool startIsGPS, Color color)
        {
            // Get the route between the points.
            MapRouteFinderResult routeResult =
                await MapRouteFinder.GetDrivingRouteAsync(
                startPoint,
                endPoint,
                MapRouteOptimization.Time,
                MapRouteRestrictions.None);

            if (routeResult.Status == MapRouteFinderStatus.Success)
            {
                if (startIsGPS)
                {
                    Summary.Text = "";
                    Summary.Inlines.Add(new Run()
                    {
                        Text = "Totale geschatte tijd in minuten: " + routeResult.Route.EstimatedDuration.TotalMinutes.ToString()
                    });
                    Summary.Inlines.Add(new LineBreak());
                    Summary.Inlines.Add(new Run()
                    {
                        Text = "Totale lengte in kilometers: "
                            + (routeResult.Route.LengthInMeters / 1000).ToString()
                    });
                }
            }
            else
            {
                Summary.Text = "Er is een probleem opgetreden: " + routeResult.Status.ToString();
            }

            // Tekent de route op de map.
            if (routeResult.Status == MapRouteFinderStatus.Success)
            {
                MapRouteView viewOfRoute = new MapRouteView(routeResult.Route);
                viewOfRoute.RouteColor = color;
                viewOfRoute.OutlineColor = Colors.Black;

                InputMap.Routes.Add(viewOfRoute);

                await InputMap.TrySetViewBoundsAsync(routeResult.Route.BoundingBox, null, Windows.UI.Xaml.Controls.Maps.MapAnimationKind.None);
            }
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
            //Event only gets called when geolocator has a location found
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //Updating current location
                currentLocation = e.Position.Coordinate;

                //Updating mapicon of the current location
                GeofenceMonitor.Current.Geofences.Clear();


                for (int i = 0; i < InputMap.MapElements.Count; i++)
                {
                    MapIcon icon = (MapIcon)InputMap.MapElements[i];
                    if (icon.Title.Equals("You are here"))
                    {
                        InputMap.MapElements.Remove(icon);
                    }
                }

                AddMapIcon(currentLocation, "You are here");

                foreach (LocationData data in routeLocations)
                {
                    AddMapIcon(data);
                }

                if(InputMap.Routes.Count == 0)
                    getRouteWithCurrentLocation(currentLocation.Point, routeLocations[0]);
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
