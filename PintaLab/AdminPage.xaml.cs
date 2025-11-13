using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Globalization;

namespace PintaLab
{
    public partial class AdminPage : ContentPage
    {
        private DatabaseService _databaseService;

        public AdminPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAllOrders();
        }

        // Load and display all orders
        private async Task LoadAllOrders()
        {
            try
            {
                var orders = await _databaseService.GetAllOrdersWithFiltersAsync();

                if (orders != null && orders.Any())
                {
                    OrdersCollectionView.ItemsSource = orders;
                    UpdateStatistics(orders);
                }
                else
                {
                    OrdersCollectionView.ItemsSource = new List<Order>();
                    UpdateStatistics(new List<Order>());
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Virhe", $"Tilausten lataus epäonnistui: {ex.Message}", "OK");
            }
        }

        // Update admin panel statistics
        private void UpdateStatistics(List<Order> orders)
        {
            try
            {
                if (orders != null && orders.Any())
                {
                    var totalOrders = orders.Count;
                    var totalAmount = orders.Sum(o => o.TotalCost);

                    TotalOrdersLabel.Text = totalOrders.ToString();
                    TotalRevenueLabel.Text = totalAmount.ToString("0.00") + " €";
                }
                else
                {
                    TotalOrdersLabel.Text = "0";
                    TotalRevenueLabel.Text = "0.00 €";
                }
            }
            catch (Exception ex)
            {
                TotalOrdersLabel.Text = "0";
                TotalRevenueLabel.Text = "0.00 €";
            }
        }

        // Show order details
        private async void OnViewOrderClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Order order)
            {
                await ShowOrderDetails(order.Id);
            }
        }

        // Delete order after confirmation
        private async void OnDeleteOrderClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Order order)
            {
                bool confirm = await DisplayAlert("Vahvista poisto",
                    $"Haluatko varmasti poistaa tilauksen {order.Id}?",
                    "Kyllä", "Ei");

                if (confirm)
                {
                    var success = await _databaseService.DeleteOrderAsync(order.Id);
                    if (success)
                    {
                        await DisplayAlert("Onnistui", "Tilaus poistettu", "OK");
                        await LoadAllOrders();
                    }
                    else
                    {
                        await DisplayAlert("Virhe", "Tilauksen poisto epäonnistui", "OK");
                    }
                }
            }
        }

        // Update order status
        private async void OnUpdateStatusClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Order order)
            {
                var newStatus = await DisplayActionSheet(
                    $"Muuta tilauksen {order.Id} tilaa",
                    "Peruuta",
                    null,
                    "Uusi", "Käsittelyssä", "Valmis", "Toimitettu", "Peruutettu");

                if (newStatus != "Peruuta" && !string.IsNullOrEmpty(newStatus))
                {
                    var success = await _databaseService.UpdateOrderStatusAsync(order.Id, newStatus);
                    if (success)
                    {
                        await DisplayAlert("Onnistui", $"Tilauksen {order.Id} tila päivitetty: {newStatus}", "OK");
                        await LoadAllOrders();
                    }
                    else
                    {
                        await DisplayAlert("Virhe", "Tilan päivitys epäonnistui", "OK");
                    }
                }
            }
        }

        // Show detailed order information
        private async Task ShowOrderDetails(int orderId)
        {
            var details = await _databaseService.GetOrderDetailsAsync(orderId);
            if (details != null)
            {
                var message = $"Tilaus #{details.Order.Id}\n" +
                             $"Asiakas: {details.Order.CustomerName}\n" +
                             $"Sähköposti: {details.Order.CustomerEmail}\n" +
                             $"Puhelin: {details.Order.CustomerPhone}\n" +
                             $"Summa: {details.Order.TotalCost:0.00} €\n" +
                             $"Tila: {details.Order.Status}\n" +
                             $"Päivämäärä: {details.Order.FormattedCreatedDate}\n" +
                             $"Tuotteita: {details.OrderItems.Count} kpl\n\n" +
                             "Tuotteet:\n";

                foreach (var item in details.OrderItems)
                {
                    message += $"- {item.CabinetType} ({item.Room}) {item.Leveys}x{item.Korkeus}x{item.Paksuus}mm - {item.Cost:0.00} €\n";
                }

                await DisplayAlert("Tilauksen tiedot", message, "Sulje");
            }
            else
            {
                await DisplayAlert("Virhe", "Tilauksen tietoja ei löytynyt", "OK");
            }
        }

        // Return to main page
        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}