using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace PintaLab
{
    public partial class MyOrdersPage : ContentPage
    {
        private DatabaseService _databaseService;
        private int _currentUserId;

        public MyOrdersPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _currentUserId = Preferences.Get("UserId", 0);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadOrders();
        }

        // Load user's orders from database
        private async Task LoadOrders()
        {
            if (_currentUserId == 0)
            {
                await DisplayAlert("Virhe", "Kirjaudu sis‰‰n n‰hd‰ksesi tilaukset", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            try
            {
                var orders = await _databaseService.GetUserOrdersAsync(_currentUserId);

                if (orders != null && orders.Any())
                {
                    // Show orders
                    OrdersCollectionView.ItemsSource = orders;
                    OrdersCollectionView.IsVisible = true;
                    EmptyStateLayout.IsVisible = false;
                }
                else
                {
                    // Show empty state
                    OrdersCollectionView.IsVisible = false;
                    EmptyStateLayout.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Virhe", $"Tilausten lataus ep‰onnistui: {ex.Message}", "OK");
            }
        }

        // Navigate back to main page
        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }

        // Navigate to order creation page
        private async void OnCreateOrderClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//OrderPage");
        }
    }
}