using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ConsoleApp1
{
    public class ReportGenerator
    {
        private const string Source = "ReportGenerator";

        private readonly OrderRepository _orderRepo;
        private readonly ProductRepository _productRepo;

        public ReportGenerator(OrderRepository orderRepo, ProductRepository productRepo)
        {
            _orderRepo = orderRepo;
            _productRepo = productRepo;
        }

        public async Task GenerateAsync(CancellationToken ct = default)
        {
            Log.Info(Source, "Generating execution report...");

            // 1. تحميل كافة الطلبات عبر OrderRepository
            var orders = await _orderRepo.GetAllAsync(ct);

            // 2. إحصائيات الحالات
            int totalOrders = orders.Count;
            int completedCount = orders.Count(o => o.Status == OrderStatus.Completed);
            int rejectedCount = orders.Count(o => o.Status == OrderStatus.Rejected);
            int failedCount = orders.Count(o => o.Status == OrderStatus.Failed);
            int paymentFailedCount = orders.Count(o => o.Status == OrderStatus.PaymentFailed);

            // 3. الإيرادات ومتوسط قيمة الطلب (للـ Completed فقط)
            var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed).ToList();
            decimal totalRevenue = completedOrders.Sum(o => o.TotalAmount);
            decimal averageOrderValue = completedCount > 0 ? Math.Round(totalRevenue / completedCount, 2, MidpointRounding.AwayFromZero) : 0m;

            // 4. تحديد أعلى منتج مبيعاً (TopSellingProduct)
            // تجميع الكميات لكل منتج عبر الطلبات المكتملة فقط
            var productSales = completedOrders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(i => i.Quantity) })
                .ToList();

            string topSellingProductName = "None";
            if (productSales.Any())
            {
                // أعلى كمية، وفي حالة التساوي نختار أقل ProductId
                var topSale = productSales
                    .OrderByDescending(p => p.TotalQuantity)
                    .ThenBy(p => p.ProductId)
                    .First();

                // قراءة تفاصيل المنتج من ProductRepository (مع محاكاة التأخير)
                var product = await _productRepo.GetByIdAsync(topSale.ProductId, ct);
                topSellingProductName = product != null ? $"{product.Name}, {topSale.TotalQuantity} units" : $"Product #{topSale.ProductId}, {topSale.TotalQuantity} units";
            }

            // 5. بناء هيكل التقرير
            var reportData = new
            {
                GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TotalOrders = totalOrders,
                CompletedOrders = completedCount,
                RejectedOrders = rejectedCount,
                FailedOrders = failedCount,
                PaymentFailedOrders = paymentFailedCount,
                TotalRevenue = totalRevenue,
                AverageOrderValue = averageOrderValue,
                TopSellingProduct = topSellingProductName
            };

            // 6. حفظ النتيجة إلى Data/reports/report_{yyyyMMdd_HHmmss}.json
            var dir = Path.Combine("Data", "reports");
            Directory.CreateDirectory(dir);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filePath = Path.Combine(dir, $"report_{timestamp}.json");

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(reportData, options);

            await File.WriteAllTextAsync(filePath, jsonString, ct);
            Log.Success(Source, $"Report successfully generated and saved to {filePath}");

            // طباعة التقرير في الكونسول كما طلبته التاسك
            Console.WriteLine("\n===== EXECUTION REPORT =====");
            Console.WriteLine($"Total Orders: {totalOrders}");
            Console.WriteLine($"Completed: {completedCount}");
            Console.WriteLine($"Rejected: {rejectedCount}");
            Console.WriteLine($"Failed: {failedCount}");
            Console.WriteLine($"Payment Failed: {paymentFailedCount}");
            Console.WriteLine($"Total Revenue: {totalRevenue:C}");
            Console.WriteLine($"Average Order Value: {averageOrderValue:C}");
            Console.WriteLine($"Top Selling Product: {topSellingProductName}");
            Console.WriteLine("============================\n");
        }
    }
}
