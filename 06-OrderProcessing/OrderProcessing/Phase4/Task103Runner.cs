using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder1
{
    public static class Task103Runner
    {
        public static async Task RunFastestQuoteTestAsync(CancellationToken ct = default)
        {
            Console.WriteLine("\n=== TASK 10.3: FASTEST SHIPPING QUOTE DEMO ===");

            // إعداد الشركات الثلاث بالشروط المحددة
            var carriersNormal = new List<IShippingCarrier>
            {
                new ShippingCarrier("FastShip", 500, 1500, 1.00m),
                new ShippingCarrier("CairoExpress", 300, 2000, 0.95m, failForOrderIds: new[] { 2, 4 }), // فشل للأزواج كاختبار
                new ShippingCarrier("NileCourier", 800, 1200, 1.10m)
            };

            var quoteServiceNormal = new ShippingQuoteService(carriersNormal);

            // R7: Run it for orders #1–#5
            Console.WriteLine($"\n{"Order",-6} | {"Winning Carrier",-15} | {"Price",-8} | {"Time (ms)",-10} | {"Cancelled Carriers"}");
            Console.WriteLine(new string('-', 70));

            for (int i = 1; i <= 5; i++)
            {
                var dummyOrder = new Order { Id = i, TotalAmount = 500.00m };
                try
                {
                    var (carrier, price, elapsed, cancelled) = await quoteServiceNormal.GetFastestQuoteAsync(dummyOrder, ct);
                    string cancelledStr = cancelled.Count > 0 ? string.Join(", ", cancelled) : "None";
                    Console.WriteLine($"#{i,-5} | {carrier,-15} | {price,-8:F2} | {elapsed,-10} | {cancelledStr}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"#{i,-5} | Error: {ex.Message}");
                }
            }

            // R5: Separate run where every carrier is configured to fail for order #6
            Console.WriteLine("\n--- Testing R5: All Carriers Fail for Order #6 ---");
            var carriersAllFail = new List<IShippingCarrier>
            {
                new ShippingCarrier("FastShip", 500, 1500, 1.00m, failForOrderIds: new[] { 6 }),
                new ShippingCarrier("CairoExpress", 300, 2000, 0.95m, failForOrderIds: new[] { 6 }),
                new ShippingCarrier("NileCourier", 800, 1200, 1.10m, failForOrderIds: new[] { 6 })
            };
            var quoteServiceAllFail = new ShippingQuoteService(carriersAllFail);
            var failOrder = new Order { Id = 6, TotalAmount = 500.00m };

            try
            {
                await quoteServiceAllFail.GetFastestQuoteAsync(failOrder, ct);
            }
            catch (ShippingProviderUnavailableException ex)
            {
                Console.WriteLine($"Successfully caught expected exception (R5): {ex.Message}");
            }

            Console.WriteLine("=================================================\n");
        }
    }
}
