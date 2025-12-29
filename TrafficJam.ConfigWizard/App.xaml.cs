namespace TrafficJam.ConfigWizard;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Resolve MainPage from DI container
        var mainPage = Handler?.MauiContext?.Services.GetRequiredService<MainPage>()
                       ?? throw new InvalidOperationException("Could not resolve MainPage from DI");

        return new Window(mainPage) { Title = "Traffic Config Wizard" };
    }
}
