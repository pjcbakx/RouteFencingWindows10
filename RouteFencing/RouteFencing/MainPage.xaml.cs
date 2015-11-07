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
        private Geolocator geolocator;
        private Geocoordinate currentLocation;
        private List<LocationData> routeLocations;
        private LocationData chosenRouteLocation;

        private String nameCurrentLocation = "You are here";

        public MainPage()
        {
            this.InitializeComponent();
            geolocator = new Geolocator();
            Summary.Text = "Locating your current position...";
            Error.Text = "";

            //Fill an list with hardcoded sample locations
            routeLocations = new List<LocationData>();
            routeLocations.Add(new LocationData("Lovensdijkstraat 63", 51.58541489, 4.79325905));
            routeLocations.Add(new LocationData("Hogeschoollaan", 51.58405327, 4.79573339));
            routeLocations.Add(new LocationData("Utrecht", 52.090737, 5.121420));

            locationList.ItemsSource = routeLocations;
            locationList.SelectedIndex = 0;

            GeofenceMonitor.Current.Geofences.Clear();

            //Setup the events
            GeofenceMonitor.Current.GeofenceStateChanged += OnGeofenceStateChanged;
            geolocator.StatusChanged += Geolocator_StatusChanged;
            geolocator.PositionChanged += PositionChanged;
        }

        #region Functions to draw on the map

        private void AddMapIcon(Geocoordinate location, String name)
        {
            //Add an mapicon for the given location
            MapIcon mapIcon = new MapIcon();
            mapIcon.Location = location.Point;
            mapIcon.NormalizedAnchorPoint = new Point(0.5, 1.0);
            mapIcon.Title = name;
            InputMap.MapElements.Add(mapIcon);
        }

        private void AddMapIcon(LocationData location)
        {
            //Add an mapicon for the given location
            MapIcon mapIcon = new MapIcon();
            mapIcon.Location = new Geopoint(location.getPosition());
            mapIcon.NormalizedAnchorPoint = new Point(0.5, 1.0);
            mapIcon.Title = location.name;
            InputMap.MapElements.Add(mapIcon);

            //Make the geofence of the given locationdata
            location.MakeNewGeofence(Int32.Parse(GeofenceRadius.Text));
            GeofenceMonitor.Current.Geofences.Add(location.geofence);

            if ((bool)GeofenceDraw.IsChecked)
                drawGeofence(chosenRouteLocation, Int32.Parse(GeofenceRadius.Text));
        }

        private void drawGeofence(LocationData location, double radius)
        {
            var strokeColor = Colors.DarkBlue;
            strokeColor.A = 100;
            var fillColor = Colors.Blue;
            fillColor.A = 100;

            List<BasicGeoposition> poslist = new List<BasicGeoposition>();
            poslist.Add(location.getPosition());

            MapPolygon circlePolygon = new MapPolygon
            {
                FillColor = fillColor,
                StrokeColor = strokeColor,
                StrokeThickness = 3,
                StrokeDashed = true,
                ZIndex = 1,
                Path = new Geopath(location.GetCirclePoints(radius))
            };

            InputMap.MapElements.Add(circlePolygon);
        }

        private async void getRouteWithCurrentLocation(Geopoint startLoc, LocationData endLoc)
        {
            BasicGeoposition endLocation = endLoc.getPosition();
            Geopoint endPoint = new Geopoint(endLocation);

            GetRouteAndDirections(startLoc, endPoint, Colors.Red);
        }

        private async void GetRouteAndDirections(Geopoint startPoint, Geopoint endPoint, Color color)
        {
            // Get the route between the points.
            MapRouteFinderResult routeResult =
                await MapRouteFinder.GetDrivingRouteAsync(
                startPoint,
                endPoint,
                MapRouteOptimization.Time,
                MapRouteRestrictions.None);

            //Check if making the route is completed
            if (routeResult.Status == MapRouteFinderStatus.Success)
            {
                Summary.Text = "";
                Summary.Inlines.Add(new Run()
                {
                    Text = "Total time: " + routeResult.Route.EstimatedDuration.TotalMinutes.ToString() + " min"
                });
                Summary.Inlines.Add(new LineBreak());
                Summary.Inlines.Add(new Run()
                {
                    Text = "Total length: " + (routeResult.Route.LengthInMeters / 1000).ToString() + " km"
                });
            }
            else
            {
                Summary.Text = "Er is een probleem opgetreden: " + routeResult.Status.ToString();
            }

            //Draw the route on the map
            if (routeResult.Status == MapRouteFinderStatus.Success)
            {
                MapRouteView viewOfRoute = new MapRouteView(routeResult.Route);
                viewOfRoute.RouteColor = color;
                viewOfRoute.OutlineColor = Colors.Black;

                InputMap.Routes.Add(viewOfRoute);

                await InputMap.TrySetViewBoundsAsync(routeResult.Route.BoundingBox, null, Windows.UI.Xaml.Controls.Maps.MapAnimationKind.None);
            }
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
                    Geoposition pos = await geolocator.GetGeopositionAsync();
                    currentLocation = pos.Coordinate;
                    Summary.Text = "Location found, make an route";

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
                    Summary.Text = "An unexpected problem occured";
                    break;
            }
        }
        #endregion

        #region Geolocator/GeofenceMonitor events

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
                        try { await this.getCurrentLocation(); }
                        catch { Summary.Text = "No internet connection"; }
                        
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
                for (int i = 0; i < InputMap.MapElements.Count; i++)
                {
                    if (InputMap.MapElements[i] is MapIcon)
                    {
                        MapIcon icon = (MapIcon)InputMap.MapElements[i];
                        if (icon.Title.Equals(nameCurrentLocation))
                        {
                            InputMap.MapElements.Remove(icon);
                        }
                    }
                }

                AddMapIcon(currentLocation, nameCurrentLocation);
            });        
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
                        LocationData geofenceLocation = null;

                        foreach (LocationData data in routeLocations)
                        {
                            if (data.name.Equals(geofence.Id))
                                geofenceLocation = data;
                        }

                        if(geofenceLocation != null)
                        {
                            Summary.Text = "Geofence entered: " + geofenceLocation.name;
                        }

                    }
                    else if (state == GeofenceState.Exited)
                    {
                        // Your app takes action based on the exited event

                        // NOTE: You might want to write your app to take particular
                        // action based on whether the app has internet connectivity.

                        LocationData geofenceLocation = null;

                        foreach (LocationData data in routeLocations)
                        {
                            if (data.name.Equals(geofence.Id))
                                geofenceLocation = data;
                        }

                        Summary.Text = "Geofence exited: " + geofenceLocation.name;
                        InputMap.Routes.Clear();
                    }
                }
            });
        }
        #endregion

        #region Buttons
        private void GetRouteButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentLocation != null)
            {
                Summary.Text = "Making an route, please wait....";
                Error.Text = "";
                chosenRouteLocation = (LocationData)locationList.SelectedItem;

                GeofenceMonitor.Current.Geofences.Clear();
                InputMap.Routes.Clear();

                //Remove all the mapelements except the MapIcon of the current location
                MapIcon currentLocationIcon = null;

                for (int i = 0; i < InputMap.MapElements.Count; i++)
                {
                    if (InputMap.MapElements[i] is MapIcon)
                    {
                        MapIcon icon = (MapIcon)InputMap.MapElements[i];
                        if (icon.Title.Equals(nameCurrentLocation))
                        {
                            currentLocationIcon = icon;
                            break;
                        }
                    }
                }
                InputMap.MapElements.Clear();
                InputMap.MapElements.Add(currentLocationIcon);

                //Get the route and show the chosen location and the route on the map
                getRouteWithCurrentLocation(currentLocation.Point, chosenRouteLocation);
                AddMapIcon(chosenRouteLocation);
            }
            else
            {
                Error.Text = "Current location unknown";
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (currentLocation != null)
            {
                int radius = Int32.Parse(GeofenceRadius.Text);
                if(radius > 1000000)
                {
                    Error.Text = "Radius is too big";
                }
                else
                {
                    if ((bool)GeofenceDraw.IsChecked && chosenRouteLocation != null)
                        drawGeofence(chosenRouteLocation, radius);
                    else
                    {
                        for (int i = 0; i < InputMap.MapElements.Count; i++)
                        {
                            if (InputMap.MapElements[i] is MapPolygon)
                            {
                                InputMap.MapElements.Remove(InputMap.MapElements[i]);
                            }
                        }
                    }
                }
            }
            else
            {
                Error.Text = "Current location unknown";
            }
        }
    }
    #endregion
}
