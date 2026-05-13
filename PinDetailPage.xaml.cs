using EcoLand.Models;

namespace EcoLand.Views.Pages;

public partial class PinDetailPage : ContentPage
{
    private readonly MapPointDto _item;
    private double _screenHeight = 0;
    private double _panStartY = 0;

    // 3 nấc tính theo TranslationY
    // TranslationY = 0           → 3/3 full màn hình
    // TranslationY = H * 1/3     → 2/3 màn hình
    // TranslationY = H * 2/3     → 1/3 màn hình
    // TranslationY > H * 2/3     → tụt xuống đóng

    private double Snap3_3 => 0;                        // full
    private double Snap2_3 => _screenHeight * 1.0 / 3; // 2/3
    //private double Snap1_3 => _screenHeight * 2.0 / 3; // 1/3  ← mặc định

    public PinDetailPage(MapPointDto item)
    {
        InitializeComponent();
        _item = item;
        LoadData();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (height > 0 && _screenHeight == 0)
        {
            _screenHeight = height;

            // Bắt đầu ở nấc 2/3
            SheetPanel.TranslationY = Snap2_3;
        }
    }

    private async void OnSheetPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panStartY = SheetPanel.TranslationY;
                break;

            case GestureStatus.Running:
                var newY = _panStartY + e.TotalY;
                // Giới hạn từ full (0) đến hết màn hình
                SheetPanel.TranslationY = Math.Clamp(newY, Snap3_3, _screenHeight);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                await SnapToNearest();
                break;
        }
    }

    private async Task SnapToNearest()
    {
        var y = SheetPanel.TranslationY;

        // Dưới nấc 1/3 → tụt xuống đóng
        if (y > Snap2_3 + 30)
        {
            await CloseSheetAsync();
            return;
        }

        // Tìm nấc gần nhất trong 3 nấc
        var snaps = new[] { Snap3_3, Snap2_3};
        var nearest = snaps.OrderBy(s => Math.Abs(s - y)).First();

        await SheetPanel.TranslateToAsync(0, nearest, 250, Easing.CubicOut);
    }

    private async Task CloseSheetAsync()
    {
        await SheetPanel.TranslateToAsync(0, _screenHeight, 250, Easing.CubicIn);
        await Navigation.PopModalAsync(false);
    }

    private async void OnBackgroundTapped(object sender, EventArgs e)
    {
        await CloseSheetAsync();
    }

    private async void OnDetailClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync(false);

        // Lấy page hiện tại (MainMap) để push từ đó
        var mainPage = Application.Current?.Windows[0].Page;
        if (mainPage != null)
            await mainPage.Navigation.PushAsync(new PropertyDetail(_item.IdN));
    }



    private void OnCallClicked(object sender, EventArgs e)
    {
        if (PhoneDialer.Default.IsSupported && !string.IsNullOrEmpty(_item.Mobile))
            PhoneDialer.Default.Open(_item.Mobile);
    }

    private void LoadData()
    {
        PropertyImage.Source = _item.FirstImg;
        HeaderLabel.Text = _item.Header;
        TimeAgoLabel.Text = _item.TimeAgo;
        DistanceLabel.Text = $"Cách trung tâm {_item.Distance}";
        AddressLabel.Text = _item.FullAddress;
        TypeLabel.Text = _item.Loainha;
        BedroomLabel.Text = $"{_item.Sopn:N0} Phòng Ngủ";
        BathroomLabel.Text = $"{_item.Sopk:N0} WC";
        FullnameLabel.Text = _item.Fullname;
        AvatarImage.Source = _item.AvatarUrl;

        PriceLabel.FormattedText = new FormattedString
        {
            Spans =
            {
                new Span
                {
                    Text = _item.PriceDisplay,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = (Color)Application.Current.Resources["Primary"]
                },
                new Span
                {
                    Text = "  ·  ",
                    FontAttributes = FontAttributes.Bold,
                    TextColor = (Color)Application.Current.Resources["Primary"]
                },
                new Span
                {
                    Text = $"{_item.Dientich:N0} m²",
                    FontAttributes = FontAttributes.Bold,
                    TextColor = (Color)Application.Current.Resources["Primary"]
                },
                new Span
                {
                    Text = "  ~ ",
                    TextColor = (Color)Application.Current.Resources["Black"]
                },
                new Span
                {
                    Text = _item.PricePerM2,
                    FontAttributes = FontAttributes.Italic,
                    TextColor = (Color)Application.Current.Resources["Black"]
                },
            }
        };
    }
}