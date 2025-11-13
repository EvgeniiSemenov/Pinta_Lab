using System;
using SQLite;
using Microsoft.Maui.Graphics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PintaLab
{
    // UI MODELS - For display in the interface
    public class OrderItem
    {
        public int Id { get; set; }
        public string Room { get; set; }
        public string CabinetType { get; set; }
        public string FrontType { get; set; }
        public int Leveys { get; set; }
        public int Korkeus { get; set; }
        public int Paksuus { get; set; }
        public string Katisyys { get; set; }
        public string Material { get; set; }
        public string HandleParams { get; set; }
        public string HingeParams { get; set; }
        public decimal Cost { get; set; }
        public Color ItemColor { get; set; }

        // Display properties
        public string Dimensions => $"{Leveys}x{Korkeus}x{Paksuus}";
        public string Handedness => Katisyys;
        public string DisplayName => $"{FrontType} {CabinetType} {Katisyys}";
    }

    // DATABASE MODELS
    [Table("Users")]
    public class User
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string EmailEncrypted { get; set; }
    public string NameEncrypted { get; set; }  
    public string PasswordHash { get; set; }
    public bool GDPRConsent { get; set; }
    public DateTime? GDPRConsentDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}

    [Table("Orders")]
    public class Order
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int UserId { get; set; }
        public decimal TotalCost { get; set; }
        public string Status { get; set; } = "Uusi";
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Encrypted customer data
        public string CustomerNameEncrypted { get; set; }
        public string CustomerEmailEncrypted { get; set; }
        public string CustomerPhoneEncrypted { get; set; }

        // Encryption handling (not stored in database)
        [Ignore]
        public string CustomerName
        {
            get => DataEncryptor.Decrypt(CustomerNameEncrypted);
            set => CustomerNameEncrypted = DataEncryptor.Encrypt(value);
        }

        [Ignore]
        public string CustomerEmail
        {
            get => DataEncryptor.Decrypt(CustomerEmailEncrypted);
            set => CustomerEmailEncrypted = DataEncryptor.Encrypt(value);
        }

        [Ignore]
        public string CustomerPhone
        {
            get => DataEncryptor.Decrypt(CustomerPhoneEncrypted);
            set => CustomerPhoneEncrypted = DataEncryptor.Encrypt(value);
        }

        // Formatted date for display
        [Ignore]
        public string FormattedCreatedDate
        {
            get
            {
                if (CreatedDate == DateTime.MinValue || CreatedDate.Year <= 1)
                    return "Ei päivämäärää";

                return CreatedDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            }
        }
    }

    [Table("OrderItems")]
    public class OrderItemEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public string Room { get; set; }
        public string CabinetType { get; set; }
        public string FrontType { get; set; }
        public int Leveys { get; set; }
        public int Korkeus { get; set; }
        public int Paksuus { get; set; }
        public string Katisyys { get; set; }
        public int MaterialId { get; set; }
        public int HandleId { get; set; }
        public int HingeId { get; set; }
        public decimal Cost { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    [Table("Materials")]
    public class Material
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
        public decimal PricePerM3 { get; set; }
        public string Category { get; set; }
        public bool IsWaterResistant { get; set; }
    }

    [Table("Handles")]
    public class Handle
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
        public int Size { get; set; }
        public decimal Price { get; set; }
    }

    [Table("Hinges")]
    public class Hinge
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
        public string Type { get; set; }
        public decimal Price { get; set; }
    }

    // DATA MODELS FOR CALCULATIONS AND DISPLAY
    public class OrderData
    {
        public int UserId { get; set; }
        public decimal TotalCost { get; set; }
        public List<OrderItem> OrderItems { get; set; }
        public List<Material> Materials { get; set; }
        public List<Handle> Handles { get; set; }
        public List<Hinge> Hinges { get; set; }
    }

    public class AdminStatistics
    {
        public int TotalOrders { get; set; }
        public int NewOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalUsers { get; set; }
    }

    public class OrderDetails
    {
        public Order Order { get; set; }
        public List<OrderItemDetail> OrderItems { get; set; }
    }

    public class OrderItemDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Room { get; set; }
        public string CabinetType { get; set; }
        public string FrontType { get; set; }
        public int Leveys { get; set; }
        public int Korkeus { get; set; }
        public int Paksuus { get; set; }
        public string Katisyys { get; set; }
        public string MaterialName { get; set; }
        public string HandleName { get; set; }
        public string HingeName { get; set; }
        public decimal Cost { get; set; }
        public string CreatedDate { get; set; }
    }

    public class MaterialStats
    {
        public string MaterialName { get; set; }
        public int UsageCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class OrderStatusUpdate
    {
        public int OrderId { get; set; }
        public string NewStatus { get; set; }
        public DateTime UpdateDate { get; set; } = DateTime.Now;
    }

    // SECURITY HELPERS
    public static class PasswordHasher
    {
        // Password hashing
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password + "pintalab_gdpr_salt_2024");
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // Password verification
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return HashPassword(password) == hashedPassword;
        }
    }

    public static class DataEncryptor
    {
        private static readonly string Key = "pintalab-32-byte-encryption-key!!";

        // Data encryption
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(Key.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16];

            var encryptor = aes.CreateEncryptor();
            var bytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(encryptor.TransformFinalBlock(bytes, 0, bytes.Length));
        }

        // Data decryption
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(Key.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];

                var decryptor = aes.CreateDecryptor();
                var bytes = Convert.FromBase64String(cipherText);
                return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(bytes, 0, bytes.Length));
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}