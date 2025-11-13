using Microsoft.Maui.Controls;

namespace PintaLab
{
    public partial class AdminLoginPage : ContentPage
    {
        public AdminLoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            // Check if password is entered
            if (string.IsNullOrEmpty(PasswordEntry.Text))
            {
                await DisplayAlert("Virhe", "Syötä salasana", "OK");
                return;
            }

            // Verify admin password
            if (PasswordEntry.Text == App.AdminPassword)
            {
                // Save admin login status
                Preferences.Set("IsAdmin", true);
                await DisplayAlert("Onnistui", "Ylläpitokirjautuminen onnistui", "OK");
                await Shell.Current.GoToAsync("//AdminPage");
            }
            else
            {
                await DisplayAlert("Virhe", "Väärä salasana", "OK");
                PasswordEntry.Text = "";
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}