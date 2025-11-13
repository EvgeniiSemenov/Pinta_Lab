namespace PintaLab;

public partial class PortfolioPage : ContentPage
{///empty page
    public PortfolioPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}