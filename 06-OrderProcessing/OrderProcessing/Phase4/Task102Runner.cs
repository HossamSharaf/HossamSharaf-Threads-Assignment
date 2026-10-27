using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder1
{
    public static class Task102Runner
    {
        public static async Task RunDashboardTestAsync(CancellationToken ct = default)
        {
            Console.WriteLine("\n=== TASK 10.2: CUSTOMER DASHBOARD DEMO ===");

            // حقن الفشل للعميل رقم 99 (R5)
            var dashboardService = new CustomerDashboardService(failPaymentsForCustomerIds: new[] { 99 });

            // ==========================================
            // الحالة الأولى: العميل رقم 3 (Omar Hassan)
            // ==========================================
            Console.WriteLine("\n--- Testing Customer #3 (Expected Success) ---");
            try
            {
                var dashboard3 = await dashboardService.LoadAsync(3, ct);

                Console.WriteLine($"Customer: #{dashboard3.Customer?.Id} {dashboard3.Customer?.Name}");
                Console.WriteLine($"Orders count: {dashboard3.Orders.Count}");
                foreach (var o in dashboard3.Orders)
                    Console.WriteLine($"  - Order #{o.Id} ({o.Status})");

                Console.WriteLine($"Payments count: {dashboard3.Payments.Count}");
                foreach (var p in dashboard3.Payments)
                    Console.WriteLine($"  - Payment for Order #{p.OrderId} ({p.Status})");

                Console.WriteLine($"Emails count: {dashboard3.Emails.Count}");
                foreach (var e in dashboard3.Emails)
                    Console.WriteLine($"  - Email for Order #{e.OrderId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            // ==========================================
            // الحالة الثانية: العميل رقم 99 (Multiple Failures)
            // ==========================================
            Console.WriteLine("\n--- Testing Customer #99 (Expected Multiple Failures) ---");
            try
            {
                await dashboardService.LoadAsync(99, ct);
            }
            catch (AggregateException aggEx)
            {
                Console.WriteLine("Captured multiple failures successfully (R4):");
                foreach (var inner in aggEx.InnerExceptions)
                {
                    Console.WriteLine($"  - {inner.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }

            Console.WriteLine("==================================================\n");
        }
    }
}
