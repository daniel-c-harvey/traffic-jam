using TrafficJam.ConfigWizard.Models;
using TrafficJam.ConfigWizard.Services;
using TrafficJam.ConfigWizard.ViewModels;

namespace TrafficJam.ConfigWizard;

public partial class MainPage : ContentPage
{
    private readonly NetworkViewModel _viewModel;

    // .NET 10: Constructor injection via DI container
    public MainPage(NetworkViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Populate pickers with enum values
        NodeTypePicker.ItemsSource = Enum.GetValues<NodeType>();
        ControlTypePicker.ItemsSource = Enum.GetValues<ControlType>();
        RoadTypePicker.ItemsSource = Enum.GetValues<RoadType>();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Save Configuration"
            });

            if (result != null)
            {
                ConfigService.Save(result.FullPath, _viewModel);
                await DisplayAlertAsync("Saved", $"Configuration saved to {result.FullPath}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private async void OnLoadClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Load Configuration",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, [".json"] },
                    { DevicePlatform.iOS, ["public.json"] },
                    { DevicePlatform.Android, ["application/json"] },
                    { DevicePlatform.MacCatalyst, ["public.json"] }
                })
            });

            if (result != null)
            {
                var config = ConfigService.Load(result.FullPath);
                _viewModel.LoadFrom(config);
                await DisplayAlertAsync("Loaded", $"Configuration loaded from {result.FullPath}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}
