using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ConsoleApp1
{
    public static class Phase2Runner
    {
        public static async Task RunAsync(CancellationToken ct = default)
        {
            var totalStopwatch = Stopwatch.StartNew();

            // 1. ResetAsync() → race demo → ResetAsync() again → pricing comparison on order #1
            await DataSeeder.ResetAsync();
            await RaceConditionDemo.RunAsync(ct);

            await DataSeeder.ResetAsync();

            var customerRepo = new CustomerRepository();
            var productRepo = new ProductRepository();
            var couponRepo = new CouponRepository();
            var orderRepo = new OrderRepository();
            var incomingRepo = new IncomingOrderRepository();

            var incomingOrders = await incomingRepo.GetAllAsync(ct);
            var order1 = incomingOrders.Find(o => o.Id == 1);
            if (order1 != null)
            {
                var tempPricer = new OrderPricer(customerRepo, productRepo, couponRepo, new[] { 7 });
                await tempPricer.PriceOrderAsync(order1, ct); // تجربة على الطائر بدون حفظ
            }

            // 2. Validate all 20 orders concurrently and save each one
            var validator = new OrderValidator(customerRepo, productRepo, couponRepo, orderRepo);
            var validationTasks = incomingOrders.Select(order => validator.ValidateOrderAsync(order, ct));
            await Task.WhenAll(validationTasks);

            var validatedOrders = await orderRepo.GetByStatusAsync(OrderStatus.Validated, ct);

            // 3. Start ProcessValidatedOrderAsync for every Validated order into a List<Task>
            var pricer = new OrderPricer(customerRepo, productRepo, couponRepo, failShippingForOrderIds: new[] { 7 });
            var invoiceGenerator = new InvoiceGenerator();
            var processor = new OrderProcessorV2(productRepo, orderRepo, pricer, invoiceGenerator);

            var processingTasks = new List<Task>();
            foreach (var order in validatedOrders)
            {
                processingTasks.Add(processor.ProcessValidatedOrderAsync(order, ct));
            }

            // Print the Status of task[0] immediately (after creation)
            Console.WriteLine($"\n[0ms] Task[0] Status: {processingTasks[0].Status}");

            // 4. await Task.Delay(1000) and print the Status of every task (while running)
            await Task.Delay(1000, ct);
            Console.WriteLine("\n[1000ms] Status of all processing tasks:");
            for (int i = 0; i < processingTasks.Count; i++)
            {
                Console.WriteLine($"- Order task {i} (Order #{validatedOrders[i].Id}): {processingTasks[i].Status}");
            }

            // 5. Await each task in a foreach. A failing order must not stop the others:
            Console.WriteLine("\n>>> Awaiting processing tasks individually... <<<");
            for (int i = 0; i < processingTasks.Count; i++)
            {
                var task = processingTasks[i];
                var order = validatedOrders[i];

                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    // A failing order must not stop the others: print status, mark Failed, release stock, save, and continue
                    Log.Error("Phase2Runner", $"Order #{order.Id} failed: {ex.Message}");

                    order.Status = OrderStatus.Failed;
                    order.FailureReason = ex.Message;

                    // إرجاع المخزون المحجوز لهذا الطلب الفاشل
                    foreach (var item in order.Items)
                    {
                        await productRepo.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
                    }

                    await orderRepo.SaveAsync(order, ct);
                }
            }

            // 6. Print the final Status of every task and the total time.
            totalStopwatch.Stop();
            Console.WriteLine("\n===== Final Task Statuses =====");
            for (int i = 0; i < processingTasks.Count; i++)
            {
                Console.WriteLine($"- Order #{validatedOrders[i].Id}: Final Task Status = {processingTasks[i].Status}");
            }
            Console.WriteLine($"Total Phase 2 Time: {totalStopwatch.Elapsed.TotalSeconds:F1} seconds");

            // 7. Read orders.json and confirm it has exactly 20 records
            var finalOrders = await orderRepo.GetAllAsync(ct);
            Console.WriteLine($"\norders.json final records count: {finalOrders.Count} (Expected: 20)");

            int completedCount = finalOrders.Count(o => o.Status == OrderStatus.Completed);
            int rejectedCount = finalOrders.Count(o => o.Status == OrderStatus.Rejected);
            int failedCount = finalOrders.Count(o => o.Status == OrderStatus.Failed);
            Console.WriteLine($"Breakdown -> Completed: {completedCount}, Rejected: {rejectedCount}, Failed: {failedCount}");
            Console.WriteLine("Expected -> Completed: 13, Rejected: 6, Failed: 1 (#7, Shipping provider unavailable)");
        }
    }
}