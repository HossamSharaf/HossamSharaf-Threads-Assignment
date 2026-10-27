using ConsoleApp1.NewFolder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ConsoleApp1.NewFolder1
{
    public static class Task101Runner
    {
        public static async Task RunLiveCheckoutAsync(CancellationToken ct = default)
        {
            Console.WriteLine("\n=== TASK 10.1: LIVE CHECKOUT PROGRESS ===");
            await DataSeeder.ResetAsync();

            // 1. إعادة إعداد الخدمات بنفس أعطال Task 9.2
            var productRepo = new ProductRepository();
            var orderRepo = new OrderRepository(failSaveForOrderIds: new[] { 13 });
            var incomingRepo = new IncomingOrderRepository();

            var customerService = new CustomerService(new CustomerRepository());
            var inventoryService = new InventoryService(productRepo);
            var pricingService = new PricingService(productRepo, customerService, new CouponRepository(), failShippingForOrderIds: Array.Empty<int>());

            var paymentGateway = new PaymentGateway(
                timeoutOrderIds: new[] { 5 },
                declineOrderIds: new[] { 8 }
            );

            var emailService = new EmailService(
                failAttempts: new Dictionary<int, int> {
                    { 3, 2 },
                    { 17, int.MaxValue }
                }
            );

            var processor = new OrderProcessorV2(
                customerService, inventoryService, pricingService,
                paymentGateway, orderRepo, emailService);

            var incomingOrders = await incomingRepo.GetAllAsync(ct);
            int total = incomingOrders.Count;
            int counter = 0;

            Console.WriteLine("\nStarting all 20 orders at once... Monitoring live progress:\n");

            
            var processingTasks = incomingOrders.Select(async order =>
            {
                var sw = Stopwatch.StartNew();
                await processor.ProcessOrderAsync(order, ct);
                sw.Stop();
                return (Order: order, ElapsedSeconds: sw.Elapsed.TotalSeconds);
            }).ToList();

            await foreach (var completedTask in Task.WhenEach(processingTasks))
            {
                var result = await completedTask;
                var order = result.Order;

                int current = Interlocked.Increment(ref counter);

                string statusMsg = order.Status == OrderStatus.Completed
                    ? "Completed"
                    : $"{order.Status} ({order.FailureReason})";

                Console.WriteLine($"[{current,2}/{total}] Order #{order.Id,-2} -> {statusMsg} after {result.ElapsedSeconds:F1} s");
            }

            var finalOrders = await orderRepo.GetAllAsync(ct);
            int completed = finalOrders.Count(o => o.Status == OrderStatus.Completed);
            int rejected = finalOrders.Count(o => o.Status == OrderStatus.Rejected);
            int paymentFailed = finalOrders.Count(o => o.Status == OrderStatus.PaymentFailed);
            int failed = finalOrders.Count(o => o.Status == OrderStatus.Failed);

            Console.WriteLine("\n================ TASK 10.1 SUMMARY ================");
            Console.WriteLine($"Completed:     {completed,-2} (Expected 11)");
            Console.WriteLine($"Rejected:      {rejected,-2} (Expected 6)");
            Console.WriteLine($"PaymentFailed: {paymentFailed,-2} (Expected 2)");
            Console.WriteLine($"Failed:        {failed,-2} (Expected 1)");
            Console.WriteLine("===================================================\n");
        }
    }
}
