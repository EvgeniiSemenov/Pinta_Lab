using System;
using System.Linq;
using System.Text;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PintaLab
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;
        private bool _isInitialized = false;
        private string _databasePath;

        public DatabaseService()
        {
            InitializeDatabasePath();
        }

        // Initialize database path based on project structure
        private void InitializeDatabasePath()
        {
            try
            {
                var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;

                if (string.IsNullOrEmpty(projectRoot))
                {
                    projectRoot = Environment.CurrentDirectory;
                }

                var databaseDir = Path.Combine(projectRoot, "Database");
                if (!Directory.Exists(databaseDir))
                {
                    Directory.CreateDirectory(databaseDir);
                }

                _databasePath = Path.Combine(databaseDir, "pintalab.db");
            }
            catch (Exception ex)
            {
                // Fallback to current directory if path resolution fails
                _databasePath = Path.Combine(Environment.CurrentDirectory, "pintalab.db");
            }
        }

        // Initialize database connection and create tables
        private async Task InitializeDatabase()
        {
            if (_isInitialized) return;

            try
            {
                var directory = Path.GetDirectoryName(_databasePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _database = new SQLiteAsyncConnection(_databasePath);

                // Create database tables
                await _database.CreateTableAsync<User>();
                await _database.CreateTableAsync<Order>();
                await _database.CreateTableAsync<Material>();
                await _database.CreateTableAsync<Handle>();
                await _database.CreateTableAsync<Hinge>();
                await _database.CreateTableAsync<OrderItemEntity>();

                // Create indexes for performance
                await _database.CreateIndexAsync<Order>(x => x.UserId);
                await _database.CreateIndexAsync<OrderItemEntity>(x => x.OrderId);

                _isInitialized = true;

                await InitializeCatalogData();
            }
            catch (Exception ex)
            {
                throw new Exception($"Database initialization failed: {ex.Message}");
            }
        }

        // Initialize default catalog data (materials, handles, hinges)
        private async Task InitializeCatalogData()
        {
            await InitializeDatabase();

            // Initialize materials if table is empty
            if (await _database.Table<Material>().CountAsync() == 0)
            {
                var materials = new List<Material>
                {
                    new Material { Name = "Tammi Luonnonvalkoinen", PricePerM3 = 0.40m, Category = "Laminaatti", IsWaterResistant = false },
                    new Material { Name = "Tammi Savu", PricePerM3 = 0.8m, Category = "Laminaatti", IsWaterResistant = false },
                    new Material { Name = "Puhdas valkoinen", PricePerM3 = 0.75m, Category = "Laminaatti", IsWaterResistant = false },
                    new Material { Name = "MDF Valkoinen maalattu", PricePerM3 = 0.50m, Category = "MDF", IsWaterResistant = false },
                    new Material { Name = "Kosteussuojattu laminaatti", PricePerM3 = 0.45m, Category = "Laminaatti", IsWaterResistant = true }
                };
                foreach (var material in materials)
                    await _database.InsertAsync(material);
            }

            // Initialize handles if table is empty
            if (await _database.Table<Handle>().CountAsync() == 0)
            {
                var handles = new List<Handle>
                {
                    new Handle { Name = "Ei kahvaa", Size = -1, Price = 0 },
                    new Handle { Name = "96mm kahva", Size = 96, Price = 8.50m },
                    new Handle { Name = "128mm kahva", Size = 128, Price = 11.75m },
                    new Handle { Name = "Oma koko", Size = 0, Price = 0 }
                };
                foreach (var handle in handles)
                    await _database.InsertAsync(handle);
            }

            // Initialize hinges if table is empty
            if (await _database.Table<Hinge>().CountAsync() == 0)
            {
                var hinges = new List<Hinge>
                {
                    new Hinge { Name = "Ei saranaa", Type = "Ei jyrsintää", Price = 0 },
                    new Hinge { Name = "Salice 110' upposarana", Type = "Jyrsintä", Price = 14.20m },
                    new Hinge { Name = "Salice saneeraus", Type = "Ei jyrsintää", Price = 11.80m },
                    new Hinge { Name = "Blum CLIP top", Type = "Jyrsintä", Price = 17.50m }
                };
                foreach (var hinge in hinges)
                    await _database.InsertAsync(hinge);
            }
        }

        // ORDERS - BASIC OPERATIONS

        // Save order with encryption for sensitive data
        public async Task<int> SaveOrderAsync(Order order)
        {
            await InitializeDatabase();

            // Encrypt sensitive data if not already encrypted
            if (!string.IsNullOrEmpty(order.CustomerName) && string.IsNullOrEmpty(order.CustomerNameEncrypted))
            {
                order.CustomerNameEncrypted = DataEncryptor.Encrypt(order.CustomerName);
                order.CustomerName = null;
            }

            if (!string.IsNullOrEmpty(order.CustomerEmail) && string.IsNullOrEmpty(order.CustomerEmailEncrypted))
            {
                order.CustomerEmailEncrypted = DataEncryptor.Encrypt(order.CustomerEmail);
                order.CustomerEmail = null;
            }

            if (!string.IsNullOrEmpty(order.CustomerPhone) && string.IsNullOrEmpty(order.CustomerPhoneEncrypted))
            {
                order.CustomerPhoneEncrypted = DataEncryptor.Encrypt(order.CustomerPhone);
                order.CustomerPhone = null;
            }

            if (order.Id != 0)
            {
                return await _database.UpdateAsync(order);
            }
            else
            {
                await _database.InsertAsync(order);
                return order.Id;
            }
        }

        public async Task<int> SaveOrderItemAsync(OrderItemEntity item)
        {
            await InitializeDatabase();
            return item.Id != 0 ?
                await _database.UpdateAsync(item) :
                await _database.InsertAsync(item);
        }

        public async Task<List<Order>> GetUserOrdersAsync(int userId)
        {
            await InitializeDatabase();
            return await _database.Table<Order>()
                .Where(o => o.UserId == userId)
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            await InitializeDatabase();
            return await _database.Table<Order>()
                .Where(o => o.Id == orderId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<OrderItemEntity>> GetOrderItemsAsync(int orderId)
        {
            await InitializeDatabase();
            return await _database.Table<OrderItemEntity>()
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();
        }

        // CATALOG - RETRIEVAL METHODS
        public async Task<List<Material>> GetMaterialsAsync()
        {
            await InitializeDatabase();
            return await _database.Table<Material>().ToListAsync();
        }

        public async Task<List<Handle>> GetHandlesAsync()
        {
            await InitializeDatabase();
            return await _database.Table<Handle>().ToListAsync();
        }

        public async Task<List<Hinge>> GetHingesAsync()
        {
            await InitializeDatabase();
            return await _database.Table<Hinge>().ToListAsync();
        }

        public async Task<Material> GetMaterialByIdAsync(int id)
        {
            await InitializeDatabase();
            return await _database.Table<Material>()
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<Handle> GetHandleByIdAsync(int id)
        {
            await InitializeDatabase();
            return await _database.Table<Handle>()
                .Where(h => h.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<Hinge> GetHingeByIdAsync(int id)
        {
            await InitializeDatabase();
            return await _database.Table<Hinge>()
                .Where(h => h.Id == id)
                .FirstOrDefaultAsync();
        }

        // USERS - MANAGEMENT METHODS

        // Save user with encryption for sensitive data
        public async Task<int> SaveUserAsync(User user)
        {
            await InitializeDatabase();

            // Encrypt sensitive data if not already encrypted
            if (!string.IsNullOrEmpty(user.Email) && string.IsNullOrEmpty(user.EmailEncrypted))
            {
                user.EmailEncrypted = DataEncryptor.Encrypt(user.Email);
                user.Email = null; // Clear unencrypted field
            }

            if (!string.IsNullOrEmpty(user.Name) && string.IsNullOrEmpty(user.NameEncrypted))
            {
                user.NameEncrypted = DataEncryptor.Encrypt(user.Name);
                user.Name = null; // Clear unencrypted field
            }

            return user.Id != 0 ?
                await _database.UpdateAsync(user) :
                await _database.InsertAsync(user);
        }

        // Get user by email with decryption
        public async Task<User> GetUserByEmailAsync(string email)
        {
            await InitializeDatabase();
            // For email search, encrypt the search value
            var encryptedEmail = DataEncryptor.Encrypt(email);
            var user = await _database.Table<User>()
                .Where(u => u.EmailEncrypted == encryptedEmail)
                .FirstOrDefaultAsync();

            // DECRYPT DATA WHEN RETRIEVING
            if (user != null)
            {
                if (!string.IsNullOrEmpty(user.EmailEncrypted))
                    user.Email = DataEncryptor.Decrypt(user.EmailEncrypted);
                if (!string.IsNullOrEmpty(user.NameEncrypted))
                    user.Name = DataEncryptor.Decrypt(user.NameEncrypted);
            }

            return user;
        }

        // Get user by ID with decryption
        public async Task<User> GetUserByIdAsync(int id)
        {
            await InitializeDatabase();
            var user = await _database.Table<User>()
                .Where(u => u.Id == id)
                .FirstOrDefaultAsync();

            // DECRYPT DATA WHEN RETRIEVING
            if (user != null)
            {
                if (!string.IsNullOrEmpty(user.EmailEncrypted))
                    user.Email = DataEncryptor.Decrypt(user.EmailEncrypted);
                if (!string.IsNullOrEmpty(user.NameEncrypted))
                    user.Name = DataEncryptor.Decrypt(user.NameEncrypted);
            }

            return user;
        }

        // GDPR - DATA PROTECTION METHODS

        // Anonymize user data for GDPR compliance
        public async Task<bool> DeleteUserDataAsync(int userId)
        {
            try
            {
                await InitializeDatabase();

                // Anonymize user data
                var user = await GetUserByIdAsync(userId);
                if (user != null)
                {
                    user.Email = $"anonymous_{userId}@gdpr.protected";
                    user.Name = "GDPR Deleted User";
                    user.PasswordHash = PasswordHasher.HashPassword(Guid.NewGuid().ToString());
                    user.GDPRConsent = false;
                    user.GDPRConsentDate = null;

                    await _database.UpdateAsync(user);
                }

                // Anonymize user's orders
                var userOrders = await _database.Table<Order>()
                    .Where(o => o.UserId == userId)
                    .ToListAsync();

                foreach (var order in userOrders)
                {
                    order.CustomerNameEncrypted = DataEncryptor.Encrypt($"GDPR Deleted User");
                    order.CustomerEmailEncrypted = DataEncryptor.Encrypt($"anonymous_{userId}@gdpr.protected");
                    order.CustomerPhoneEncrypted = DataEncryptor.Encrypt($"0000000000");
                    await _database.UpdateAsync(order);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // Export user data for GDPR compliance
        public async Task<string> ExportUserDataAsync(int userId)
        {
            try
            {
                await InitializeDatabase();

                var user = await GetUserByIdAsync(userId);
                var orders = await GetUserOrdersAsync(userId);

                var ordersList = new List<object>();

                if (orders != null)
                {
                    foreach (var order in orders)
                    {
                        ordersList.Add(new
                        {
                            order.Id,
                            order.TotalCost,
                            order.Status,
                            order.CreatedDate,
                            CustomerName = order.CustomerName,
                            CustomerEmail = order.CustomerEmail,
                            CustomerPhone = order.CustomerPhone
                        });
                    }
                }

                var exportData = new
                {
                    User = user != null ? new
                    {
                        user.Id,
                        user.Email,
                        user.Name,
                        user.CreatedDate,
                        user.GDPRConsent,
                        user.GDPRConsentDate
                    } : null,
                    Orders = ordersList,
                    ExportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                return System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // ADMINISTRATION - STATISTICS AND MANAGEMENT
        public async Task<AdminStatistics> GetAdminStatisticsAsync()
        {
            await InitializeDatabase();

            var stats = new AdminStatistics();

            try
            {
                // Get statistics using SQL queries
                stats.TotalOrders = await _database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders");
                stats.NewOrders = await _database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders WHERE Status = 'Uusi'");

                var revenueResult = await _database.ExecuteScalarAsync<object>("SELECT SUM(TotalCost) FROM Orders");
                stats.TotalRevenue = revenueResult != null && revenueResult != DBNull.Value ?
                    Convert.ToDecimal(revenueResult) : 0;
            }
            catch (Exception ex)
            {
                // Return default statistics on error
            }

            return stats;
        }

        // Get all orders with optional filters
        public async Task<List<Order>> GetAllOrdersWithFiltersAsync(string statusFilter = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            await InitializeDatabase();

            try
            {
                var allOrders = await _database.Table<Order>().ToListAsync();
                var filteredOrders = allOrders.AsEnumerable();

                // Filter orders by status
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Kaikki")
                {
                    filteredOrders = filteredOrders.Where(o => o.Status == statusFilter);
                }

                // Filter by date range
                if (fromDate.HasValue)
                {
                    filteredOrders = filteredOrders.Where(o => o.CreatedDate.Date >= fromDate.Value.Date);
                }

                if (toDate.HasValue)
                {
                    filteredOrders = filteredOrders.Where(o => o.CreatedDate.Date <= toDate.Value.Date);
                }

                return filteredOrders.OrderByDescending(o => o.CreatedDate).ToList();
            }
            catch (Exception ex)
            {
                return new List<Order>();
            }
        }

        // Delete order and its items
        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            try
            {
                await InitializeDatabase();

                // First delete order items
                await _database.ExecuteAsync("DELETE FROM OrderItems WHERE OrderId = ?", orderId);

                // Then delete the order itself
                await _database.ExecuteAsync("DELETE FROM Orders WHERE Id = ?", orderId);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // Get detailed order information with related data
        public async Task<OrderDetails> GetOrderDetailsAsync(int orderId)
        {
            await InitializeDatabase();

            try
            {
                // Get basic order information
                var order = await _database.Table<Order>()
                    .Where(o => o.Id == orderId)
                    .FirstOrDefaultAsync();

                if (order == null) return null;

                // Get order items with joins to materials, handles, and hinges
                var orderItemsQuery = @"
                    SELECT 
                        oi.*,
                        m.Name as MaterialName,
                        h.Name as HandleName,
                        hin.Name as HingeName
                    FROM OrderItems oi
                    LEFT JOIN Materials m ON oi.MaterialId = m.Id
                    LEFT JOIN Handles h ON oi.HandleId = h.Id
                    LEFT JOIN Hinges hin ON oi.HingeId = hin.Id
                    WHERE oi.OrderId = ?";

                var orderItems = await _database.QueryAsync<OrderItemDetail>(orderItemsQuery, orderId);

                return new OrderDetails
                {
                    Order = order,
                    OrderItems = orderItems
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // Update order status
        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            try
            {
                await InitializeDatabase();

                var order = await _database.Table<Order>()
                    .Where(o => o.Id == orderId)
                    .FirstOrDefaultAsync();

                if (order != null)
                {
                    order.Status = newStatus;
                    await _database.UpdateAsync(order);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // ADDITIONAL ADMIN METHODS
        public async Task<List<User>> GetAllUsersAsync()
        {
            await InitializeDatabase();
            return await _database.Table<User>().ToListAsync();
        }

        // Get popular materials statistics
        public async Task<List<MaterialStats>> GetPopularMaterialsAsync()
        {
            await InitializeDatabase();

            var query = @"
                SELECT 
                    m.Name as MaterialName,
                    COUNT(oi.Id) as UsageCount,
                    SUM(oi.Cost) as TotalRevenue
                FROM OrderItems oi
                JOIN Materials m ON oi.MaterialId = m.Id
                GROUP BY m.Name
                ORDER BY UsageCount DESC
                LIMIT 10";

            return await _database.QueryAsync<MaterialStats>(query);
        }

        // HELPER METHODS
        public async Task<SQLiteAsyncConnection> GetDatabaseConnection()
        {
            await InitializeDatabase();
            return _database;
        }

        internal object GetDatabasePath()
        {
            return _databasePath;
        }

        internal async Task<bool> DatabaseExists()
        {
            return File.Exists(_databasePath);
        }

        // Reset database for GDPR testing
        public async Task ResetDatabaseForGDPR()
        {
            try
            {
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                    _isInitialized = false;
                    await InitializeDatabase();
                }
            }
            catch (Exception ex)
            {
               
            }
        }

        // Simple data encryption utility
        public static class DataEncryptor
        {
            private static readonly string EncryptionKey = "your-secret-key-here-32-chars-long"; // Replace with real key

            public static string Encrypt(string plainText)
            {
                if (string.IsNullOrEmpty(plainText)) return plainText;

                // Simple implementation for example - replace with real encryption
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
            }

            public static string Decrypt(string encryptedText)
            {
                if (string.IsNullOrEmpty(encryptedText)) return encryptedText;

                try
                {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(encryptedText));
                }
                catch
                {
                    return encryptedText;
                }
            }
        }
    }
}