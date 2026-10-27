using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public static class DataSeeder
    {
        public static async Task ResetAsync()
        {
            // 1. مسح وإعادة إنشاء مجلدات البيانات
            if (Directory.Exists(DataFiles.DataFolder))
            {
                Directory.Delete(DataFiles.DataFolder, true);
            }
            Directory.CreateDirectory(DataFiles.DataFolder);
            Directory.CreateDirectory(DataFiles.InvoicesFolder);
            Directory.CreateDirectory(DataFiles.ReportsFolder);

            // 2. تجهيز بيانات العملاء (Customers)
            var customers = new List<Customer>
            {
                CreateCustomer(1, "Ahmed Ali", true, LoyaltyTier.Gold),
                CreateCustomer(2, "Sara Mohamed", true, LoyaltyTier.Silver),
                CreateCustomer(3, "Omar Hassan", true, LoyaltyTier.Bronze),
                CreateCustomer(4, "Mona Ibrahim", false, LoyaltyTier.Silver), // Inactive
                CreateCustomer(5, "Youssef Adel", true, LoyaltyTier.Gold),
                CreateCustomer(6, "Nour Khaled", true, LoyaltyTier.Bronze),
                CreateCustomer(7, "Karim Samir", true, LoyaltyTier.Silver),
                CreateCustomer(8, "Laila Mahmoud", true, LoyaltyTier.Gold),
                CreateCustomer(9, "Hassan Tarek", true, LoyaltyTier.Bronze),
                CreateCustomer(10, "Dina Fathy", true, LoyaltyTier.Silver)
            };

            // 3. تجهيز بيانات المنتجات (Products)
            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Laptop", Price = 1200.00m, StockQuantity = 10, WeightKg = 2.2m },
                new Product { Id = 2, Name = "Smartphone", Price = 800.00m, StockQuantity = 15, WeightKg = 0.4m },
                new Product { Id = 3, Name = "Wireless Mouse", Price = 25.00m, StockQuantity = 2, WeightKg = 0.1m },
                new Product { Id = 4, Name = "Mechanical Keyboard", Price = 90.00m, StockQuantity = 1, WeightKg = 0.9m },
                new Product { Id = 5, Name = "USB-C Hub", Price = 45.00m, StockQuantity = 30, WeightKg = 0.2m },
                new Product { Id = 6, Name = "Headphones", Price = 150.00m, StockQuantity = 12, WeightKg = 0.3m },
                new Product { Id = 7, Name = "27\" Monitor", Price = 350.00m, StockQuantity = 6, WeightKg = 5.0m },
                new Product { Id = 8, Name = "Webcam", Price = 70.00m, StockQuantity = 20, WeightKg = 0.2m }
            };

            // 4. تجهيز بيانات الكوبونات (Coupons)
            DateTime today = DateTime.Today;
            var coupons = new List<Coupon>
            {
                new Coupon { Code = "SAVE10", DiscountPercent = 10, ExpiryDate = today.AddDays(180) },
                new Coupon { Code = "WELCOME15", DiscountPercent = 15, ExpiryDate = today.AddDays(90) },
                new Coupon { Code = "FLASH20", DiscountPercent = 20, ExpiryDate = today.AddDays(7) },
                new Coupon { Code = "OLD20", DiscountPercent = 20, ExpiryDate = today.AddDays(-30) } // Expired
            };

            // 5. تجهيز بيانات الطلبات الواردة (Incoming Orders)
            var incomingOrders = new List<Order>
            {
                CreateOrder(1, 1, "SAVE10", new[] { (1, 1), (3, 1) }),
                CreateOrder(2, 2, null, new[] { (2, 1) }),
                CreateOrder(3, 3, "WELCOME15", new[] { (5, 2), (8, 1) }),
                CreateOrder(4, 4, null, new[] { (6, 1) }), // Customer inactive
                CreateOrder(5, 5, null, new[] { (7, 1) }),
                CreateOrder(6, 6, "FLASH20", new[] { (5, 1) }),
                CreateOrder(7, 7, null, new[] { (6, 1), (8, 1) }),
                CreateOrder(8, 8, "SAVE10", new[] { (1, 1) }),
                CreateOrder(9, 9, null, new[] { (3, 5) }), // Insufficient stock
                CreateOrder(10, 10, null, new[] { (4, 1) }), // Race for last keyboard
                CreateOrder(11, 1, null, new[] { (4, 1) }),  // Race for last keyboard
                CreateOrder(12, 2, "OLD20", new[] { (2, 1) }), // Expired coupon
                CreateOrder(13, 3, null, new[] { (6, 2) }),
                CreateOrder(14, 5, "WELCOME15", new[] { (8, 2), (5, 1) }),
                CreateOrder(15, 99, null, new[] { (2, 1) }), // Non-existent customer
                CreateOrder(16, 6, null, new[] { (7, 1), (5, 1) }),
                CreateOrder(17, 7, "SAVE10", new[] { (2, 1) }),
                CreateOrder(18, 8, "FAKE50", new[] { (6, 1) }), // Fake coupon
                CreateOrder(19, 9, null, new[] { (8, 1) }),
                CreateOrder(20, 10, "FLASH20", new[] { (1, 1), (6, 1) })
            };

            // 6. حفظ البيانات في الملفات باستخدام JsonFileStore اللي عملناه في 6.2
            await SeedFileAsync(DataFiles.CustomersFile, customers);
            await SeedFileAsync(DataFiles.ProductsFile, products);
            await SeedFileAsync(DataFiles.CouponsFile, coupons);
            await SeedFileAsync(DataFiles.IncomingOrdersFile, incomingOrders);

            // 7. تهيئة باقي الملفات كقوائم فارغة
            await SeedFileAsync(DataFiles.OrdersFile, new List<Order>());
            await SeedFileAsync(DataFiles.PaymentsFile, new List<PaymentTransaction>());
            await SeedFileAsync(DataFiles.EmailsFile, new List<EmailMessage>());
            await SeedFileAsync(DataFiles.EmailRetryFile, new List<EmailMessage>());
        }

        // ================= Helper Methods =================

        private static Customer CreateCustomer(int id, string name, bool isActive, LoyaltyTier tier)
        {
            // بناء الإيميل: مسح المسافات، استبدالها بنقطة، وتحويل الحروف لصغيرة
            string email = name.Replace(" ", ".").ToLower() + "@example.com";
            return new Customer
            {
                Id = id,
                Name = name,
                Email = email,
                IsActive = isActive,
                LoyaltyTier = tier
            };
        }

        // دالة مساعدة لإنشاء الطلب بناءً على شروط التاسك الصارمة
        private static Order CreateOrder(int orderId, int customerId, string? couponCode, params (int ProductId, int Quantity)[] itemsInfo)
        {
            var order = new Order
            {
                Id = orderId,
                CustomerId = customerId,
                CouponCode = couponCode,
                Status = OrderStatus.Pending, // "Every order starts with Status = Pending"
                CreatedAt = DateTime.Now,     // "CreatedAt = DateTime.Now at seeding"
                SubTotal = 0,                 // "money fields 0"
                Discount = 0,
                ShippingCost = 0,
                TotalAmount = 0,
                FailureReason = null          // "FailureReason = null"
            };

            foreach (var item in itemsInfo)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = 0             // "UnitPrice in incoming_orders.json is 0"
                });
            }

            return order;
        }

        private static async Task SeedFileAsync<T>(string fileName, List<T> data)
        {
            string fullPath = DataFiles.GetFilePath(fileName);
            await JsonFileStore.WriteAsync(fullPath, data);
            Console.WriteLine($"[Seeder] {fileName} -> {data.Count} records.");
        }
    }
}

