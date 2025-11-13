using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;

namespace PintaLab
{
    public partial class OrderPage : ContentPage
    {
        public ObservableCollection<OrderItem> OrderItems { get; set; }
        private DatabaseService _databaseService;
        private int _currentUserId;
        private List<Material> _materials;
        private List<Handle> _handles;
        private List<Hinge> _hinges;

        public OrderPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _currentUserId = Preferences.Get("UserId", 0);
            OrderItems = new ObservableCollection<OrderItem>();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCatalogData();
        }

        // Load all catalog data from database: materials, handles, and hinges
        private async Task LoadCatalogData()
        {
            try
            {
                // Load materials, handles, and hinges from database
                _materials = await _databaseService.GetMaterialsAsync();
                _handles = await _databaseService.GetHandlesAsync();
                _hinges = await _databaseService.GetHingesAsync();

                // Fill pickers with available options
                MaterialPicker.ItemsSource = _materials?.Select(m => m.Name).ToList();
                HandleSizePicker.ItemsSource = _handles?.Select(h => h.Name).ToList();
                HingeTypePicker.ItemsSource = _hinges?.Select(h => h.Name).ToList();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Virhe", $"Luettelon lataus epäonnistui: {ex.Message}", "OK");
            }
        }

        // Handle adding new product to order
        private void OnAddClicked(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            try
            {
                // Get selected materials, handles, and hinges
                var selectedMaterial = _materials?.FirstOrDefault(m => m.Name == MaterialPicker.SelectedItem?.ToString());
                var selectedHandle = GetSelectedHandle();
                var selectedHinge = GetSelectedHinge();

                if (selectedMaterial == null || selectedHandle == null || selectedHinge == null) return;

                // Create new order item with all information
                var newItem = new OrderItem
                {
                    Id = OrderItems.Count + 1,
                    Room = RoomPicker.SelectedItem?.ToString() ?? "",
                    CabinetType = CabinetTypePicker.SelectedItem?.ToString() ?? "",
                    FrontType = FrontTypePicker.SelectedItem?.ToString() ?? "",
                    Leveys = int.Parse(LeveysEntry.Text),
                    Korkeus = int.Parse(KorkeusEntry.Text),
                    Paksuus = int.Parse(PaksuusEntry.Text),
                    Katisyys = HandednessPicker.SelectedItem?.ToString() ?? "",
                    Material = selectedMaterial.Name,
                    HandleParams = selectedHandle.Name,
                    HingeParams = selectedHinge.Name,
                    Cost = CalculateCost(selectedMaterial, selectedHandle, selectedHinge),
                    // Set alternating color based on even/odd ID
                    ItemColor = (OrderItems.Count + 1) % 2 == 0 ? Color.FromArgb("#F8F9FA") : Colors.White
                };

                // Add new item to order list
                OrderItems.Add(newItem);
                UpdateUI();
                ClearForm();
            }
            catch (Exception ex)
            {
                DisplayAlert("Virhe", $"Kohteen lisäys epäonnistui: {ex.Message}", "OK");
            }
        }

        // Calculate product total cost based on material, size, and accessories
        private decimal CalculateCost(Material material, Handle handle, Hinge hinge)
        {
            var leveys = int.Parse(LeveysEntry.Text);
            var korkeus = int.Parse(KorkeusEntry.Text);
            var paksuus = int.Parse(PaksuusEntry.Text);

            // Calculate volume in cubic meters (mm -> m³ conversion)
            var volume = (decimal)(paksuus * leveys * korkeus) / 1000000;
            var materialCost = volume * material.PricePerM3;
            var hardwareCost = handle.Price + hinge.Price;

            // Round final result to two decimals
            return Math.Round(materialCost + hardwareCost, 2);
        }

        // Navigate to confirmation page when order is ready
        private async void OnContinueToConfirmationClicked(object sender, EventArgs e)
        {
            if (OrderItems.Count == 0)
            {
                await DisplayAlert("Virhe", "Lisää vähintään yksi kohde tilaukseen", "OK");
                return;
            }

            // Save order data to temporary storage for confirmation
            App.PendingOrderData = new OrderData
            {
                UserId = _currentUserId,
                TotalCost = OrderItems.Sum(item => item.Cost),
                OrderItems = new List<OrderItem>(OrderItems),
                Materials = _materials,
                Handles = _handles,
                Hinges = _hinges
            };

            await Shell.Current.GoToAsync("//OrderConfirmationPage");
        }

        // HELPER METHODS

        // Get selected handle based on picker selection
        private Handle GetSelectedHandle()
        {
            var selectedName = HandleSizePicker.SelectedItem?.ToString();
            return _handles?.FirstOrDefault(h => h.Name == selectedName) ?? _handles?.First();
        }

        // Get selected hinge based on picker selection
        private Hinge GetSelectedHinge()
        {
            var selectedName = HingeTypePicker.SelectedItem?.ToString();
            return _hinges?.FirstOrDefault(h => h.Name == selectedName) ?? _hinges?.First();
        }

        // Validate that all required fields are filled correctly
        private bool ValidateInputs() =>
            RoomPicker.SelectedIndex != -1 &&
            CabinetTypePicker.SelectedIndex != -1 &&
            FrontTypePicker.SelectedIndex != -1 &&
            HandednessPicker.SelectedIndex != -1 &&
            MaterialPicker.SelectedIndex != -1 &&
            HandleSizePicker.SelectedIndex != -1 &&
            HingeTypePicker.SelectedIndex != -1 &&
            int.TryParse(LeveysEntry.Text, out _) &&
            int.TryParse(KorkeusEntry.Text, out _) &&
            int.TryParse(PaksuusEntry.Text, out _);

        // Update UI state based on order content
        private void UpdateUI()
        {
            var hasItems = OrderItems.Count > 0;

            // Update visibility based on order content
            EmptyStateLayout.IsVisible = !hasItems;
            OrderItemsCollectionView.IsVisible = hasItems;
            ContinueButton.IsVisible = hasItems;

            // Update total cost
            TotalCostLabel.Text = $"{OrderItems.Sum(i => i.Cost):0.00} €";

            // Update alternating colors for all products
            for (int i = 0; i < OrderItems.Count; i++)
            {
                OrderItems[i].ItemColor = (i + 1) % 2 == 0 ? Color.FromArgb("#F8F9FA") : Colors.White;
            }

            // Refresh view by resetting ItemsSource
            OrderItemsCollectionView.ItemsSource = null;
            OrderItemsCollectionView.ItemsSource = OrderItems;
        }

        // Clear all form fields and reset to default state
        private void ClearForm()
        {
            // Reset all pickers
            RoomPicker.SelectedIndex = -1;
            CabinetTypePicker.SelectedIndex = -1;
            FrontTypePicker.SelectedIndex = -1;
            HandednessPicker.SelectedIndex = -1;
            MaterialPicker.SelectedIndex = -1;
            HandleSizePicker.SelectedIndex = -1;
            HingeTypePicker.SelectedIndex = -1;

            // Clear dimension fields
            LeveysEntry.Text = KorkeusEntry.Text = PaksuusEntry.Text = "";

            // Restore picker titles and colors to default
            UpdateAllPickerTitles();
            ResetPickerColors();
        }

        // Remove product from order when delete button is clicked
        private void OnRemoveItemClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is OrderItem item)
            {
                OrderItems.Remove(item);
                UpdateUI();
            }
        }

        // Navigate back to main page
        private async void OnBackClicked(object sender, EventArgs e) =>
            await Shell.Current.GoToAsync("//MainPage");

        // Handle picker selection changes
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (sender is Picker picker)
            {
                var selectedValue = picker.SelectedItem?.ToString();

                if (!string.IsNullOrEmpty(selectedValue))
                {
                    // Update picker title to selected value
                    picker.Title = selectedValue;

                    // Provide visual feedback for selection
                    picker.BackgroundColor = Color.FromArgb("#E8F4F3");
                    picker.TextColor = Colors.Black;

                    // Update selected value display label
                    UpdateSelectedValueLabel(picker, selectedValue);
                }
            }
        }

        // Update label below each picker to show selected value
        private void UpdateSelectedValueLabel(Picker picker, string selectedValue)
        {
            switch (picker)
            {
                case Picker p when p == RoomPicker:
                    RoomSelectedLabel.Text = $"Valittu: {selectedValue}";
                    RoomSelectedLabel.IsVisible = true;
                    break;
                case Picker p when p == CabinetTypePicker:
                    CabinetTypeSelectedLabel.Text = $"Valittu: {selectedValue}";
                    CabinetTypeSelectedLabel.IsVisible = true;
                    break;
                case Picker p when p == FrontTypePicker:
                    FrontTypeSelectedLabel.Text = $"Valittu: {selectedValue}";
                    FrontTypeSelectedLabel.IsVisible = true;
                    break;
                case Picker p when p == HandednessPicker:
                    HandednessSelectedLabel.Text = $"Valittu: {selectedValue}";
                    HandednessSelectedLabel.IsVisible = true;
                    break;
                case Picker p when p == MaterialPicker:
                    MaterialSelectedLabel.Text = $"Valittu: {selectedValue}";
                    MaterialSelectedLabel.IsVisible = true;
                    break;
                case Picker p when p == HandleSizePicker:
                    HandleSizeSelectedLabel.Text = $"Valittu: {selectedValue}";
                    HandleSizeSelectedLabel.IsVisible = true;
                    break;
                case Picker p when p == HingeTypePicker:
                    HingeTypeSelectedLabel.Text = $"Valittu: {selectedValue}";
                    HingeTypeSelectedLabel.IsVisible = true;
                    break;
            }
        }

        // Restore all picker titles and labels to default state
        private void UpdateAllPickerTitles()
        {
            // Restore all picker titles to defaults
            RoomPicker.Title = "Valitse huone";
            CabinetTypePicker.Title = "Valitse tyyppi";
            FrontTypePicker.Title = "Valitse tyyppi";
            HandednessPicker.Title = "Valitse kätisyys";
            MaterialPicker.Title = "Valitse materiaali";
            HandleSizePicker.Title = "Valitse koko";
            HingeTypePicker.Title = "Valitse tyyppi";

            // Hide all selected value labels
            RoomSelectedLabel.IsVisible = false;
            CabinetTypeSelectedLabel.IsVisible = false;
            FrontTypeSelectedLabel.IsVisible = false;
            HandednessSelectedLabel.IsVisible = false;
            MaterialSelectedLabel.IsVisible = false;
            HandleSizeSelectedLabel.IsVisible = false;
            HingeTypeSelectedLabel.IsVisible = false;
        }

        // Handle dimension field text changes
        private void OnDimensionChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry && !string.IsNullOrEmpty(entry.Text))
            {
                entry.BackgroundColor = Color.FromArgb("#E8F4F3");
            }
            else if (sender is Entry entryEmpty && string.IsNullOrEmpty(entryEmpty.Text))
            {
                entryEmpty.BackgroundColor = Color.FromArgb("#F8F9FA");
            }
        }

        // Reset all picker colors to default values
        private void ResetPickerColors()
        {
            var defaultBackground = Color.FromArgb("#F8F9FA");
            var defaultTextColor = Color.FromArgb("#495057");

            // Set default colors for all pickers
            RoomPicker.BackgroundColor = defaultBackground;
            RoomPicker.TextColor = defaultTextColor;

            CabinetTypePicker.BackgroundColor = defaultBackground;
            CabinetTypePicker.TextColor = defaultTextColor;

            FrontTypePicker.BackgroundColor = defaultBackground;
            FrontTypePicker.TextColor = defaultTextColor;

            HandednessPicker.BackgroundColor = defaultBackground;
            HandednessPicker.TextColor = defaultTextColor;

            MaterialPicker.BackgroundColor = defaultBackground;
            MaterialPicker.TextColor = defaultTextColor;

            HandleSizePicker.BackgroundColor = defaultBackground;
            HandleSizePicker.TextColor = defaultTextColor;

            HingeTypePicker.BackgroundColor = defaultBackground;
            HingeTypePicker.TextColor = defaultTextColor;
        }
    }
}