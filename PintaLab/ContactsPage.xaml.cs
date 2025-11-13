using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.Communication;

namespace PintaLab
{
    public partial class ContactsPage : ContentPage
    {
        public ContactsPage()
        {
            InitializeComponent();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }

        private async void OnSendMessageClicked(object sender, EventArgs e)
        {
            try
            {
                // Get form data
                string name = NameEntry?.Text?.Trim() ?? "";
                string email = EmailEntry?.Text?.Trim() ?? "";
                string message = MessageEditor?.Text?.Trim() ?? "";

                // Validate required fields
                if (string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(message))
                {
                    await DisplayAlert("Virhe", "Täytä kaikki kentät ennen viestin lähettämistä.", "OK");
                    return;
                }

                // Validate email format
                if (!IsValidEmail(email))
                {
                    await DisplayAlert("Virhe", "Syötä kelvollinen sähköpostiosoite.", "OK");
                    return;
                }

                // Create email message
                string subject = $"Yhteydenottopyyntö: {name}";
                string body = $@"
Yhteydenottolomakkeen tiedot:

Nimi: {name}
Sähköposti: {email}
Viesti: {message}

Lähetetty: {DateTime.Now:dd.MM.yyyy HH:mm}
                ";

                // Send email
                var emailMessage = new EmailMessage
                {
                    Subject = subject,
                    Body = body,
                    To = new List<string> { "evgeny.semenov14@gmail.com" }
                };

                await Email.ComposeAsync(emailMessage);

            }
            catch (FeatureNotSupportedException)
            {
                await DisplayAlert("Virhe", "Sähköpostin lähetys ei ole tuettu tällä laitteella.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Virhe", $"Viestin lähettämisessä tapahtui virhe: {ex.Message}", "OK");
            }
        }

        // Check if email address is valid
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var address = new System.Net.Mail.MailAddress(email);
                return address.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}