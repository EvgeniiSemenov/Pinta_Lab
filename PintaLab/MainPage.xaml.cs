using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace PintaLab
{
    public partial class MainPage : ContentPage
    {
        private DatabaseService _databaseService;

        public MainPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateUserInterface();
        }

        // Update UI based on login status
        private void UpdateUserInterface()
        {
            var userId = Preferences.Get("UserId", 0);
            var userName = Preferences.Get("UserName", "");

            if (userId > 0 && !string.IsNullOrEmpty(userName))
            {
                // User is logged in
                UserStatusLabel.Text = $"Tervetuloa, {userName}!";
                LogoutButton.IsVisible = true;
                LoginButton.IsVisible = false;
                MyOrdersButton.IsVisible = true;
            }
            else
            {
                // User is not logged in
                UserStatusLabel.Text = "Tervetuloa!";
                LogoutButton.IsVisible = false;
                LoginButton.IsVisible = true;
                MyOrdersButton.IsVisible = false;
            }
        }

        // Handle user logout
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Vahvista",
                "Haluatko varmasti kirjautua ulos?", "Kyllä", "Ei");

            if (confirm)
            {
                // Clear user preferences
                Preferences.Remove("UserId");
                Preferences.Remove("UserName");
                Preferences.Remove("IsAdmin");

                // Update UI
                UpdateUserInterface();
                await DisplayAlert("Onnistui", "Olet kirjautunut ulos", "OK");
            }
        }

        // Navigation methods
        private async void OnAboutClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//AboutPage");
        }

        private async void OnOrderClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//OrderPage");
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }

        private async void OnMyOrdersClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MyOrdersPage");
        }

        private async void OnPortfolioClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//PortfolioPage");
        }

        private async void OnContactsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ContactsPage");
        }

        // Admin panel access
        private async void OnAdminClicked(object sender, EventArgs e)
        {
            // Check if admin is already logged in
            if (Preferences.Get("IsAdmin", false))
            {
                await Shell.Current.GoToAsync("//AdminPage");
            }
            else
            {
                await Shell.Current.GoToAsync("//AdminLoginPage");
            }
        }
    }
}