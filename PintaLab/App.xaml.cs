namespace PintaLab
{
    public partial class App : Application
    {
        // Stores order data temporarily before saving
        public static OrderData PendingOrderData { get; set; }

        // Admin password for administrative access
        public const string AdminPassword = "q5c!mI9E";

        public App()
        {
            InitializeComponent();

            // Clear preferences on app start
            Preferences.Clear();

            MainPage = new AppShell();
        }
    }
}