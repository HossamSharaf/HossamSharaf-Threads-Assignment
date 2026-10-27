using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ConsoleApp1
{
    public static class Phase1Runner
    {
        public static async Task RunAsync(CancellationToken ct = default)
        {
            // ---------------------------------------------------------
            // 1. Reset & Sync Run (R2)
            // ---------------------------------------------------------
            await DataSeeder.ResetAsync();

            var incomingRepo = new IncomingOrderRepository();
            var incomingOrdersSync = await incomingRepo.GetAllAsync(ct);

            var syncValidator = new SyncOrderValidator(
                new SyncCustomerRepository(),
                new SyncProductRepository(),
                new SyncCouponRepository());

            var syncStopwatch = Stopwatch.StartNew();

            foreach (var order in incomingOrdersSync)
            {
                if (ct.IsCancellationRequested) break;
                syncValidator.ValidateOrder(order); // Results NOT saved to disk
            }

            syncStopwatch.Stop();
            double syncSeconds = syncStopwatch.Elapsed.TotalSeconds;

            // ---------------------------------------------------------
            // 2. Reset & Async Sequential Run (R2)
            // ---------------------------------------------------------
            await DataSeeder.ResetAsync();

            var incomingOrdersAsync = await incomingRepo.GetAllAsync(ct);

            var customerRepo = new CustomerRepository();
            var productRepo = new ProductRepository();
            var couponRepo = new CouponRepository();
            var orderRepo = new OrderRepository();

            var asyncValidator = new OrderValidator(customerRepo, productRepo, couponRepo, orderRepo);

            var asyncStopwatch = Stopwatch.StartNew();

            foreach (var order in incomingOrdersAsync)
            {
                if (ct.IsCancellationRequested) break;
                // ValidateOrderAsync handles validation and saves to orders.json
                await asyncValidator.ValidateOrderAsync(order, ct);
            }

            asyncStopwatch.Stop();
            double asyncSeconds = asyncStopwatch.Elapsed.TotalSeconds;

            // ---------------------------------------------------------
            // 3. Summary & Verification from orders.json (R3)
            // ---------------------------------------------------------
            var allOrders = await orderRepo.GetAllAsync(ct);
            var validatedOrders = await orderRepo.GetByStatusAsync(OrderStatus.Validated, ct);
            var rejectedOrders = await orderRepo.GetByStatusAsync(OrderStatus.Rejected, ct);

            Console.WriteLine();
            Console.WriteLine("===== Validation Summary =====");
            Console.WriteLine($"Total Orders: {allOrders.Count}");
            Console.WriteLine($"Validated: {validatedOrders.Count}");
            Console.WriteLine($"Rejected: {rejectedOrders.Count}");

            // طباعة الطلبات المرفوضة مرتبة حسب المعرّف
            foreach (var rejected in rejectedOrders.OrderBy(o => o.Id))
            {
                Console.WriteLine($"- Order #{rejected.Id}: {rejected.FailureReason}");
            }

            Console.WriteLine($"Sync Time: {syncSeconds:F1} seconds");
            Console.WriteLine($"Async Time: {asyncSeconds:F1} seconds");
            Console.WriteLine($"orders.json now contains {allOrders.Count} records.");
        }
    }
}
