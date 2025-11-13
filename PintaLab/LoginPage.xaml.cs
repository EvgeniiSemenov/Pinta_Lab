using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace PintaLab
{
    public partial class LoginPage : ContentPage
    {
        private DatabaseService _databaseService;
        private bool _isRegisterMode = false;

        public string FormTitle => _isRegisterMode ? "Luo uusi tili" : "Kirjaudu sisään";

        public LoginPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Auto-fill last used email
            var lastEmail = Preferences.Get("LastUserEmail", "");
            if (!string.IsNullOrEmpty(lastEmail))
            {
                EmailEntry.Text = lastEmail;
            }

            // Always check and update login state
            CheckAndUpdateLoginState();
            UpdateUI();
        }

        // Check and update complete login state
        private void CheckAndUpdateLoginState()
        {
            var userId = Preferences.Get("UserId", 0);
            var isLoggedIn = userId > 0;

            // Update visibility of all sections
            LoginFormFrame.IsVisible = !isLoggedIn;
            LoggedInPanel.IsVisible = isLoggedIn;
            GDPRManagementFrame.IsVisible = isLoggedIn;

            if (isLoggedIn)
            {
                // Show user name
                var userName = Preferences.Get("UserName", "");
                WelcomeLabel.Text = $"Tervetuloa, {userName}!";
            }
        }

        private void OnEmailTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateUI();
        }

        // Switch between login and register modes
        private void OnSwitchModeClicked(object sender, EventArgs e)
        {
            _isRegisterMode = !_isRegisterMode;

            // Hide GDPR management in register mode
            if (_isRegisterMode)
            {
                GDPRManagementFrame.IsVisible = false;
            }
            else
            {
                // Show GDPR management if user is logged in
                CheckAndUpdateLoginState();
            }

            UpdateUI();

            // Clear password when switching modes
            if (_isRegisterMode)
            {
                PasswordEntry.Text = "";
                NameEntry.Text = "";
                GDPRCheckBox.IsChecked = false;
            }
        }

        // Update UI element visibility and state
        private void UpdateUI()
        {
            var hasEmail = !string.IsNullOrEmpty(EmailEntry.Text);

            if (_isRegisterMode)
            {
                MainActionButton.Text = "Luo tili";
                SwitchModeButton.Text = "← Takaisin kirjautumiseen";
                NameSection.IsVisible = true;
                GDPRFrame.IsVisible = true;
                MainActionButton.IsEnabled = hasEmail && GDPRCheckBox.IsChecked;
            }
            else
            {
                MainActionButton.Text = "Kirjaudu sisään";
                SwitchModeButton.Text = "Luo uusi tili";
                NameSection.IsVisible = false;
                GDPRFrame.IsVisible = false;
                MainActionButton.IsEnabled = hasEmail;
            }

            OnPropertyChanged(nameof(FormTitle));
        }

        // Handle GDPR checkbox tap
        private void OnGDPRLabelTapped(object sender, EventArgs e)
        {
            GDPRCheckBox.IsChecked = !GDPRCheckBox.IsChecked;
        }

        private void OnGDPRCheckChanged(object sender, CheckedChangedEventArgs e)
        {
            if (_isRegisterMode)
            {
                MainActionButton.IsEnabled = !string.IsNullOrEmpty(EmailEntry.Text) && e.Value;
            }
        }

        // Execute main action (login or register)
        private async void OnMainActionClicked(object sender, EventArgs e)
        {
            if (_isRegisterMode)
            {
                await RegisterUser();
            }
            else
            {
                await LoginUser();
            }
        }

        // Handle user login
        private async Task LoginUser()
        {
            if (string.IsNullOrWhiteSpace(EmailEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await DisplayAlert("Virhe", "Täytä sähköposti ja salasana", "OK");
                return;
            }

            try
            {
                // Save email for next login
                Preferences.Set("LastUserEmail", EmailEntry.Text);

                var user = await _databaseService.GetUserByEmailAsync(EmailEntry.Text);

                if (user != null && PasswordHasher.VerifyPassword(PasswordEntry.Text, user.PasswordHash))
                {
                    if (!user.GDPRConsent)
                    {
                        // Switch to register mode to get GDPR consent
                        _isRegisterMode = true;
                        NameEntry.Text = user.Name;
                        GDPRCheckBox.IsChecked = false;
                        UpdateUI();

                        await DisplayAlert("Tietosuojaseloste",
                            "Sinun täytyy hyväksyä päivitetty tietosuojaseloste jatkaaksesi.", "OK");
                        return;
                    }

                    // Login successful - save user data
                    Preferences.Set("UserId", user.Id);
                    Preferences.Set("UserEmail", user.Email);
                    Preferences.Set("UserName", user.Name);

                    // UPDATE COMPLETE LOGIN STATE
                    CheckAndUpdateLoginState();

                    await DisplayAlert("Onnistui", "Kirjautuminen onnistui!", "OK");
                }
                else
                {
                    await DisplayAlert("Virhe", "Väärä sähköposti tai salasana", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Virhe", $"Kirjautuminen epäonnistui: {ex.Message}", "OK");
            }
        }

        // Handle new user registration
        private async Task RegisterUser()
        {
            if (string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
                string.IsNullOrWhiteSpace(NameEntry.Text))
            {
                await DisplayAlert("Virhe", "Täytä kaikki kentät", "OK");
                return;
            }

            if (!GDPRCheckBox.IsChecked)
            {
                await DisplayAlert("Virhe", "Hyväksy tietosuojaseloste jatkaaksesi", "OK");
                return;
            }

            try
            {
                var existingUser = await _databaseService.GetUserByEmailAsync(EmailEntry.Text);
                if (existingUser != null)
                {
                    // Update GDPR consent for existing user
                    if (!existingUser.GDPRConsent)
                    {
                        existingUser.GDPRConsent = true;
                        existingUser.GDPRConsentDate = DateTime.Now;
                        existingUser.Name = NameEntry.Text;
                        existingUser.PasswordHash = PasswordHasher.HashPassword(PasswordEntry.Text);

                        await _databaseService.SaveUserAsync(existingUser);

                        // Save user data
                        Preferences.Set("UserId", existingUser.Id);
                        Preferences.Set("UserEmail", existingUser.Email);
                        Preferences.Set("UserName", existingUser.Name);

                        // UPDATE COMPLETE LOGIN STATE
                        CheckAndUpdateLoginState();

                        await DisplayAlert("Onnistui", "Tietosuojaseloste hyväksytty! Tili päivitetty.", "OK");
                        return;
                    }
                    else
                    {
                        await DisplayAlert("Virhe", "Sähköposti on jo käytössä", "OK");
                        return;
                    }
                }

                // Create new user
                var newUser = new User
                {
                    Email = EmailEntry.Text,
                    PasswordHash = PasswordHasher.HashPassword(PasswordEntry.Text),
                    Name = NameEntry.Text,
                    GDPRConsent = true,
                    GDPRConsentDate = DateTime.Now
                };

                await _databaseService.SaveUserAsync(newUser);

                // Auto-login and save data
                Preferences.Set("UserId", newUser.Id);
                Preferences.Set("UserEmail", newUser.Email);
                Preferences.Set("UserName", newUser.Name);

                // UPDATE COMPLETE LOGIN STATE
                CheckAndUpdateLoginState();

                await DisplayAlert("Onnistui", "Tili luotu onnistuneesti!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Virhe", $"Rekisteröinti epäonnistui: {ex.Message}", "OK");
            }
        }

        // Navigate to order page
        private async void OnGoToOrderClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//OrderPage");
        }

        // Logout user
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Kirjaudu ulos",
                "Haluatko varmasti kirjautua ulos?",
                "Kyllä", "Ei");

            if (confirm)
            {
                Preferences.Clear();
                CheckAndUpdateLoginState();
                _isRegisterMode = false;
                UpdateUI();

                await DisplayAlert("Onnistui", "Olet kirjautunut ulos", "OK");
            }
        }

        // Delete user data according to GDPR
        private async void OnDeleteDataClicked(object sender, EventArgs e)
        {
            var userId = Preferences.Get("UserId", 0);
            if (userId == 0)
            {
                await DisplayAlert("Virhe", "Kirjaudu sisään poistaaksesi tietosi", "OK");
                return;
            }

            var confirm = await DisplayAlert("Vahvista poisto",
                "Haluatko varmasti poistaa kaikki henkilötietosi? Tätä toimintoa ei voi perua.",
                "Kyllä, poista", "Peruuta");

            if (confirm)
            {
                var success = await _databaseService.DeleteUserDataAsync(userId);
                if (success)
                {
                    await DisplayAlert("Onnistui", "Kaikki tietosi on poistettu", "OK");
                    Preferences.Clear();

                    // Update UI
                    CheckAndUpdateLoginState();
                    _isRegisterMode = false;
                    UpdateUI();
                }
                else
                {
                    await DisplayAlert("Virhe", "Tietojen poisto epäonnistui", "OK");
                }
            }
        }

        // Back to main page
        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}