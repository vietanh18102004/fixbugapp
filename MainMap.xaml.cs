using EcoLand.Models;
using EcoLand.Services;
using EcoLand.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace EcoLand.Views.Pages;

public partial class MainMap : ContentPage
{
    private readonly MapViewModel _vm;
    private readonly ApiServiceBase _api;

    private CancellationTokenSource _debounceCts;

    // Cache pin theo Id
    private readonly Dictionary<int, Pin> _pinCache = new();

    // Lưu center cũ để check khoảng cách
    private Location _lastCenter;

    private const double MinMoveKm = 0.15; // 150m

    public MainMap()
    {
        InitializeComponent();

        _api = new ApiServiceBase();
        _vm = new MapViewModel(null);
        BindingContext = _vm;

        //sự kiện lọc
        _vm.SearchLocationRequested += async () => await OnSearchLocation();
    }

    #region Lifecycle

    protected override void OnAppearing()
    {
        base.OnAppearing();
        InitMap();
    }

    #endregion

    //Set vị trí khi mở map

    #region Map Init

    //lấy vị trị của lần mở map cuôi cùng , nếu không có thì lấy vị trí mặc định
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (MainMapControl.VisibleRegion != null)
        {
            _lastCenter = MainMapControl.VisibleRegion.Center;
        }
    }


    //mở map theo vị trí
    private async void InitMap()
    {
        try
        {
            if (_lastCenter != null)
            {
                //Mở map - dùng vị trí
                var span = MapSpan.FromCenterAndRadius(_lastCenter, Distance.FromKilometers(0.3));
                MainMapControl.MoveToRegion(span);
                return;
            }

            //Lần đầu mở - xin quyền vị trí
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted)
            {
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                });

                if (location != null)
                {
                    var span = MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(0.3));
                    MainMapControl.MoveToRegion(span);
                    return;
                }
            }

            //K cấp quyền - mặc định Hà Nội
            var defaultCenter = new Location(20.9755582091911, 105.76256019621135);
            var defaultSpan = MapSpan.FromCenterAndRadius(defaultCenter, Distance.FromKilometers(0.3));
            MainMapControl.MoveToRegion(defaultSpan);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Không thể lấy vị trí: {ex.Message}");
            var defaultCenter = new Location(20.9755582091911, 105.76256019621135);
            var defaultSpan = MapSpan.FromCenterAndRadius(defaultCenter, Distance.FromKilometers(0.3));
            MainMapControl.MoveToRegion(defaultSpan);
        }
    }



    #endregion

    #region Map Events

    private async void Map_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Microsoft.Maui.Controls.Maps.Map.VisibleRegion))
            return;

        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(500, _debounceCts.Token);

            var region = MainMapControl.VisibleRegion;
            if (region == null)
                return;

            if (!ShouldReload(region))
                return;

            _lastCenter = region.Center;

            var data = await LoadMapPointsAsync(region);
            if (data == null)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdatePins(data);
            });
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Map load error: {ex.Message}");
        }
    }

    private void Pin_MarkerClicked(object sender, PinClickedEventArgs e)
    {
        if (sender is not Pin pin)
            return;

        var item = pin.BindingContext as MapPointDto;
        if (item == null) return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PushModalAsync(new PinDetailPage(item), false);
        });
    }

    #endregion

    #region Map Logic

    private bool ShouldReload(MapSpan region)
    {
        if (_lastCenter == null)
            return true;

        var distance = Location.CalculateDistance(
            _lastCenter,
            region.Center,
            DistanceUnits.Kilometers);

        return distance >= MinMoveKm;
    }

    private async Task<List<MapPointDto>> LoadMapPointsAsync(MapSpan region)
    {
        // Zoom quá xa thì không load
        //if (region.Radius.Kilometers > 3)
        //    return new List<MapPointDto>();

        var culture = CultureInfo.InvariantCulture;
        var center = region.Center;

        var resp = await _api.GetAsync<List<MapPointDto>>(
            "api/Feature/Points",
            new("page", 1),
            new("pageSize", 1000),
            new("MinLat", (center.Latitude - region.LatitudeDegrees / 2).ToString(culture)),
            new("MinLon", (center.Longitude - region.LongitudeDegrees / 2).ToString(culture)),
            new("MaxLat", (center.Latitude + region.LatitudeDegrees / 2).ToString(culture)),
            new("MaxLon", (center.Longitude + region.LongitudeDegrees / 2).ToString(culture)),
            new("CenterLat", center.Latitude.ToString(culture)),
            new("CenterLon", center.Longitude.ToString(culture)),
            new("RadiusKm", region.Radius.Kilometers.ToString(culture)),
            new("IdLt", 1),
            new("Sort", "")
        );

        return resp?.Data ?? new List<MapPointDto>();
    }

    private void UpdatePins(List<MapPointDto> data)
    {
        var newIds = data.Select(x => x.IdN).ToHashSet();

        // Remove pin không còn
        var removeIds = _pinCache.Keys
            .Where(id => !newIds.Contains(id))
            .ToList();

        foreach (var id in removeIds)
        {
            MainMapControl.Pins.Remove(_pinCache[id]);
            _pinCache.Remove(id);
        }

        // Add pin mới
        foreach (var item in data)
        {
            if (_pinCache.ContainsKey(item.IdN))
                continue;

            var pin = new Pin
            {
                Label = item.Header ?? string.Empty,
                Location = new Location(item.Lat, item.Lon),
                BindingContext = item
            };

            pin.MarkerClicked += Pin_MarkerClicked;

            _pinCache[item.IdN] = pin;
            MainMapControl.Pins.Add(pin);
        }
    }


    #endregion

    #region UI Actions

    private async void MainList(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void ClicktoSearch(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SearchPage());
    }

    private void NotifySwitch_Toggled(object sender, ToggledEventArgs e)
    {
        Console.WriteLine(e.Value ? "Thông báo: BẬT" : "Thông báo: TẮT");
    }



    #endregion

    #region Your location, search location

    //ghim tới vị trí hiện tại
    private async void OnLocateMeClicked(object sender, EventArgs e)
    {
        var location = await Geolocation.GetLastKnownLocationAsync();

        if (location != null)
        {
            MainMapControl.MoveToRegion(
                MapSpan.FromCenterAndRadius(
                    new Location(location.Latitude, location.Longitude),
                    Distance.FromMeters(200)
                ));
        }
    }

    //tìm kiếm,dùng geocoding lấy toạ độ, di chuyển tới toạ độ đã tìm
    private async Task OnSearchLocation()
    {
        var addressParts = new List<string>();
        if (_vm.SelectedWardLabel != "Xã/Phường")
            addressParts.Add(_vm.SelectedWardLabel);
        if (_vm.SelectedDistrictLabel != "Quận/Huyện")
            addressParts.Add(_vm.SelectedDistrictLabel);
        if (_vm.SelectedProvinceLabel != "Tỉnh/Thành phố")
            addressParts.Add(_vm.SelectedProvinceLabel);

        if (addressParts.Count == 0)
        {
            await DisplayAlertAsync("Thông báo", "Bạn phải chọn ít nhất tỉnh/thành phố trước khi tìm kiếm", "OK");
            return;
        }

        var address = string.Join(", ", addressParts);

        try
        {
            // Toàn quốc
            if (_vm.SelectedProvinceLabel.Equals("Việt Nam", StringComparison.OrdinalIgnoreCase))
            {
                // Tọa độ trung tâm Hà Nội
                var vnCenter = new Location(21.0278, 105.8342);

                // Bán kính 800km
                MainMapControl.MoveToRegion(
                    MapSpan.FromCenterAndRadius(
                        vnCenter,
                        Distance.FromKilometers(800)
                    ));
            }
            else
            {
                // Dùng MAUI Essentials Geocoding cho địa chỉ cụ thể
                var locations = await Geocoding.GetLocationsAsync(address);
                var location = locations?.FirstOrDefault();

                if (location != null)
                {
                    double zoomKm;
                    if (_vm.SelectedWardLabel != "Xã/Phường")
                        zoomKm = 0.5;
                    else if (_vm.SelectedDistrictLabel != "Quận/Huyện")
                        zoomKm = 4;
                    else
                        zoomKm = 20;

                    MainMapControl.MoveToRegion(
                        MapSpan.FromCenterAndRadius(
                            new Location(location.Latitude, location.Longitude),
                            Distance.FromKilometers(zoomKm)
                        ));
                }
                else
                {
                    await DisplayAlertAsync("Thông báo", "Không tìm thấy vị trí cho địa chỉ đã chọn", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Geocoding error: {ex.Message}");
            await DisplayAlertAsync("Thông báo", "Có lỗi khi tìm kiếm địa chỉ", "OK");
        }
    }


#endregion
    
}
