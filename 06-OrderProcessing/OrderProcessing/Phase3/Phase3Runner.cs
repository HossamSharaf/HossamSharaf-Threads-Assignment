using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public static class Phase3Runner
    {
        public static async Task RunAsync(CancellationToken ct = default)
        {
            Console.WriteLine("=== PHASE 3: PRODUCTION-READY CHECKOUT ===");
            await DataSeeder.ResetAsync();

            // 1. Repositories
            var customerRepo = new CustomerRepository();
            var productRepo = new ProductRepository();
            var couponRepo = new CouponRepository();
            var incomingRepo = new IncomingOrderRepository();

            // Order 13: DB save fails after payment
            var orderRepo = new OrderRepository(failSaveForOrderIds: new[] { 13 });

            // 2. Base Services
            var customerService = new CustomerService(customerRepo);
            var inventoryService = new InventoryService(productRepo);
            var pricingService = new PricingService(productRepo, customerService, couponRepo, failShippingForOrderIds: new[] { 7 });

            // 3. External Services (Configured for Task 9.2 Failures)
            var paymentGateway = new PaymentGateway(
                timeoutOrderIds: new[] { 5 },   // Order 5: 3s Timeout
                declineOrderIds: new[] { 8 }    // Order 8: Declined
            );

            var emailService = new EmailService(
                failAttempts: new Dictionary<int, int> {
                    { 3, 2 },                 // Order 3: Fails twice then succeeds
                    { 17, int.MaxValue }      // Order 17: Always fails
                }
            );

            // 4. Orchestrator
            var processor = new OrderProcessorV2(
                customerService, inventoryService, pricingService,
                paymentGateway, orderRepo, emailService);

            // 5. Run all 20 concurrently
            var incomingOrders = await incomingRepo.GetAllAsync(ct);
            Console.WriteLine($"\nStarting {incomingOrders.Count} orders concurrently...\n");

            var tasks = new List<Task>();
            foreach (var order in incomingOrders)
            {
                // لا ننتظر هنا، بل نبدأ جميع المهام ونضيفها للقائمة
                tasks.Add(processor.ProcessOrderAsync(order, ct));
            }

            // انتظار انتهاء جميع الطلبات (إذا فشل أحدهم كـ Code لن يوقف الباقي لأنه يتم التقاطه داخل الـ Processor)
            foreach (var task in tasks)
            {
                try { await task; } catch { /* Ignore general system crashes during await */ }
            }

            // 6. Print Final Table
            Console.WriteLine("\n========================= FINAL ORDERS SUMMARY =========================");
            Console.WriteLine($"{"Order",-6} | {"Status",-14} | {"Total",-9} | {"Reason"}");
            Console.WriteLine(new string('-', 85));

            var finalOrders = await orderRepo.GetAllAsync(ct);
            var sortedOrders = finalOrders.OrderBy(o => o.Id).ToList();

            foreach (var o in sortedOrders)
            {
                string total = o.TotalAmount > 0 ? o.TotalAmount.ToString("F2") : "-";
                string reason = o.FailureReason ?? "";

                // اقتطاع الـ Reason لو طويل عشان شكل الجدول
                if (reason.Length > 50) reason = reason.Substring(0, 47) + "...";

                Console.WriteLine($"{o.Id,-6} | {o.Status,-14} | {total,-9} | {reason}");
            }
            Console.WriteLine("========================================================================");

        }
        private static async Task VerifyTask94EndStateAsync(CancellationToken ct)
        {
            Console.WriteLine("\n================ TASK 9.4 VERIFICATION ================");

            // 1. Orders.json
            var orders = await JsonFileStore.ReadAsync<Order>(DataFiles.GetFilePath(DataFiles.OrdersFile), ct);
            int completed = orders.Count(o => o.Status == OrderStatus.Completed);
            int rejected = orders.Count(o => o.Status == OrderStatus.Rejected);
            int paymentFailed = orders.Count(o => o.Status == OrderStatus.PaymentFailed);
            int failed = orders.Count(o => o.Status == OrderStatus.Failed);

            Console.WriteLine($"[orders.json] Total: {orders.Count} (Expected 20)");
            Console.WriteLine($"  - Completed: {completed} (Expected 11)");
            Console.WriteLine($"  - Rejected: {rejected} (Expected 6)");
            Console.WriteLine($"  - PaymentFailed: {paymentFailed} (Expected 2)");
            Console.WriteLine($"  - Failed: {failed} (Expected 1)");

            // 2. Payments.json
            var payments = await JsonFileStore.ReadAsync<PaymentRecord>(DataFiles.GetFilePath(DataFiles.PaymentsFile), ct);
            int captured = payments.Count(p => p.Status == "Captured");
            int declined = payments.Count(p => p.Status == "Declined");
            int refunded = payments.Count(p => p.Status == "Refunded");
            bool hasOrder5 = payments.Any(p => p.OrderId == 5);

            Console.WriteLine($"\n[payments.json] Total: {payments.Count} (Expected 13)");
            Console.WriteLine($"  - Captured: {captured} (Expected 11)");
            Console.WriteLine($"  - Declined: {declined} (Expected 1)");
            Console.WriteLine($"  - Refunded: {refunded} (Expected 1)");
            Console.WriteLine($"  - Order #5 Exists? {hasOrder5} (Expected False)");

            // 3. Emails.json
            var emails = await JsonFileStore.ReadAsync<EmailRecord>(DataFiles.GetFilePath(DataFiles.EmailsFile), ct);
            var order3Email = emails.FirstOrDefault(e => e.OrderId == 3);

            Console.WriteLine($"\n[emails.json] Total: {emails.Count} (Expected 10)");
            Console.WriteLine($"  - Order #3 Attempts: {order3Email?.Attempts} (Expected 3)");

            // 4. Email_retry.json
            var emailRetries = await JsonFileStore.ReadAsync<EmailRetryRecord>(DataFiles.GetFilePath(DataFiles.EmailRetryFile), ct);
            var order17Retry = emailRetries.FirstOrDefault(e => e.OrderId == 17);

            Console.WriteLine($"\n[email_retry.json] Total: {emailRetries.Count} (Expected 1)");
            Console.WriteLine($"  - Order #17 Attempts: {order17Retry?.Attempts} (Expected 4)");
            Console.WriteLine($"  - LastError Set? {!string.IsNullOrEmpty(order17Retry?.LastError)}");

            // 5. Products.json (Mechanical Keyboard Stock)
            var products = await JsonFileStore.ReadAsync<Product>(DataFiles.GetFilePath(DataFiles.ProductsFile), ct);
            var keyboard = products.FirstOrDefault(p => p.Id == 4);

            Console.WriteLine($"\n[products.json]");
            Console.WriteLine($"  - Mechanical Keyboard (ID 4) Stock: {keyboard?.StockQuantity} (Expected 0)");
            Console.WriteLine("=======================================================\n");
        }
    }
}
