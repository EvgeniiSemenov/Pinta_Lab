using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace PintaLab
{
    public partial class OrderConfirmationPage : ContentPage
    {
        private DatabaseService _databaseService;
        private int _currentUserId;
        private User _currentUser;

        public OrderConfirmationPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _currentUserId = Preferences.Get("UserId", 0);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadOrderData();
            await CheckUserAuthentication();
        }

        // Load and display order summary
        private async Task LoadOrderData()
        {
            if (App.PendingOrderData != null)
            {
                // Update order summary
                ItemsCountLabel.Text = $"{App.PendingOrderData.OrderItems.Count} kpl";
                TotalCostLabel.Text = $"{App.PendingOrderData.TotalCost:0.00} €";

                // Load order items
                OrderItemsCollectionView.ItemsSource = App.PendingOrderData.OrderItems;
            }
        }

        // Check if user is authenticated and load their data
        private async Task CheckUserAuthentication()
        {
            if (_currentUserId > 0)
            {
                // User is logged in - load their data
                _currentUser = await _databaseService.GetUserByIdAsync(_currentUserId);

                if (_currentUser != null)
                {
                    // Show auto-fill button
                    AutoFillFrame.IsVisible = true;

                    // Auto-fill email
                    EmailEntry.Text = _currentUser.Email;

                    // Auto-fill name if available in database
                    if (!string.IsNullOrEmpty(_currentUser.Name))
                    {
                        var nameParts = _currentUser.Name.Split(' ');
                        if (nameParts.Length >= 2)
                        {
                            FirstNameEntry.Text = nameParts[0];
                            LastNameEntry.Text = string.Join(" ", nameParts.Skip(1));
                        }
                        else
                        {
                            FirstNameEntry.Text = _currentUser.Name;
                        }
                    }
                }
            }
            else
            {
                AutoFillFrame.IsVisible = false;
            }
        }

        // Auto-fill form with user data
        private void OnAutoFillClicked(object sender, EventArgs e)
        {
            if (_currentUser != null)
            {
                // Fill all fields with user data
                EmailEntry.Text = _currentUser.Email;

                if (!string.IsNullOrEmpty(_currentUser.Name))
                {
                    var nameParts = _currentUser.Name.Split(' ');
                    if (nameParts.Length >= 2)
                    {
                        FirstNameEntry.Text = nameParts[0];
                        LastNameEntry.Text = string.Join(" ", nameParts.Skip(1));
                    }
                    else
                    {
                        FirstNameEntry.Text = _currentUser.Name;
                    }
                }

                // Show success message
                DisplayAlert("Onnistui", "Tiedot täytetty automaattisesti", "OK");
            }
        }

        // Submit order to database
        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            if (!ValidateInputs())
            {
                await DisplayAlert("Virhe", "Täytä kaikki pakolliset kentät", "OK");
                return;
            }

            try
            {
                // Create new order (encryption will happen in SaveOrderAsync)
                var order = new Order
                {
                    UserId = _currentUserId,
                    TotalCost = App.PendingOrderData.TotalCost,
                    Status = "Uusi",
                    CustomerName = $"{FirstNameEntry.Text} {LastNameEntry.Text}", // Will be encrypted in SaveOrderAsync
                    CustomerEmail = EmailEntry.Text, // Will be encrypted in SaveOrderAsync
                    CustomerPhone = PhoneEntry.Text, // Will be encrypted in SaveOrderAsync
                    CreatedDate = DateTime.Now
                };

                // Save order to database (encryption happens in SaveOrderAsync)
                var orderId = await _databaseService.SaveOrderAsync(order);

                // Save order items (remains unchanged)
                foreach (var item in App.PendingOrderData.OrderItems)
                {
                    var material = App.PendingOrderData.Materials.FirstOrDefault(m => m.Name == item.Material);
                    var handle = App.PendingOrderData.Handles.FirstOrDefault(h => h.Name == item.HandleParams);
                    var hinge = App.PendingOrderData.Hinges.FirstOrDefault(h => h.Name == item.HingeParams);

                    var orderItem = new OrderItemEntity
                    {
                        OrderId = orderId,
                        Room = item.Room,
                        CabinetType = item.CabinetType,
                        FrontType = item.FrontType,
                        Leveys = item.Leveys,
                        Korkeus = item.Korkeus,
                        Paksuus = item.Paksuus,
                        Katisyys = item.Katisyys,
                        MaterialId = material?.Id ?? 0,
                        HandleId = handle?.Id ?? 0,
                        HingeId = hinge?.Id ?? 0,
                        Cost = item.Cost,
                        CreatedDate = DateTime.Now
                    };

                    await _databaseService.SaveOrderItemAsync(orderItem);
                }

                await DisplayAlert("Onnistui", "Tilaus lähetetty onnistuneesti!", "OK");

                // Clear temporary data and navigate to main page
                App.PendingOrderData = null;
                await Shell.Current.GoToAsync("//MainPage");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Virhe", $"Tilauksen lähetys epäonnistui: {ex.Message}", "OK");
            }
        }

        // Validate form inputs
        private bool ValidateInputs()
        {
            return !string.IsNullOrWhiteSpace(FirstNameEntry.Text) &&
                   !string.IsNullOrWhiteSpace(LastNameEntry.Text) &&
                   !string.IsNullOrWhiteSpace(EmailEntry.Text) &&
                   !string.IsNullOrWhiteSpace(PhoneEntry.Text);
        }

        // Navigate back to main page
        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//OrderPage");
        }
    }
}