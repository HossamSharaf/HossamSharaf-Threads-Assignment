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
            await DataSeeder.ResetAsync();

            var customerRepo = new CustomerRepository();
            var productRepo = new ProductRepository();
            var couponRepo = new CouponRepository();
            var incomingRepo = new IncomingOrderRepository();

            var allOrders = await incomingRepo.GetAllAsync(ct);
            var order1 = allOrders.Find(o => o.Id == 1);

            if (order1 == null)
            {
                Console.WriteLine("Order #1 not found.");
                return;
            }

            // Pass { 7 } in Phase 2 as required
            var pricer = new OrderPricer(customerRepo, productRepo, couponRepo, new[] { 7 });

            // 1. Test Sequential Pricing on Order #1
            Console.WriteLine("--- Testing Sequential Pricing on Order #1 ---");
            var seqStopwatch = Stopwatch.StartNew();
            await pricer.PriceOrderSequentialAsync(order1, ct);
            seqStopwatch.Stop();
            Console.WriteLine($"Sequential Time: {seqStopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Values: Sub={order1.SubTotal}, Disc={order1.Discount}, Ship={order1.ShippingCost}, Total={order1.TotalAmount}");

            // 2. Test Concurrent Pricing on Order #1
            Console.WriteLine("\n--- Testing Concurrent Pricing on Order #1 ---");
            var conStopwatch = Stopwatch.StartNew();
            await pricer.PriceOrderAsync(order1, ct);
            conStopwatch.Stop();
            Console.WriteLine($"Concurrent Time: {conStopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Values: Sub={order1.SubTotal}, Disc={order1.Discount}, Ship={order1.ShippingCost}, Total={order1.TotalAmount}");
        }
    }
}
