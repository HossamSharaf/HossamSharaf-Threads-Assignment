using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public static class SyncVsAsyncBenchmark
    {
        public static async Task RunComparisonAsync(CancellationToken ct = default)
        {
            var incomingRepo = new IncomingOrderRepository();
            var productRepo = new ProductRepository();
            var orderRepo = new OrderRepository(failSaveForOrderIds: Array.Empty<int>());

            var customerService = new CustomerService(new CustomerRepository());
            var inventoryService = new InventoryService(productRepo);
            var pricingService = new PricingService(productRepo, customerService, new CouponRepository(), failShippingForOrderIds: Array.Empty<int>());
            var paymentGateway = new PaymentGateway(timeoutOrderIds: Array.Empty<int>(), declineOrderIds: Array.Empty<int>());
            var emailService = new EmailService(failAttempts: new Dictionary<int, int>());

            // استخدام OrderProcessorV2 هنا
            var asyncProcessor = new OrderProcessorV2(
                customerService, inventoryService, pricingService,
                paymentGateway, orderRepo, emailService);

            var syncProcessor = new SyncOrderProcessor(productRepo, orderRepo);

            // ==========================================================
            // 1. SYNCHRONOUS RUN (Orders #1–#10)
            // ==========================================================
            await DataSeeder.ResetAsync();
            var syncOrders = (await incomingRepo.GetAllAsync(ct)).Take(10).ToList();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            int syncInitialThreads = Process.GetCurrentProcess().Threads.Count;
            int syncInitialThreadPool = ThreadPool.ThreadCount;
            var syncSw = Stopwatch.StartNew();

            var threads = new List<Thread>();
            foreach (var order in syncOrders)
            {
                var t = new Thread(() => syncProcessor.ProcessOrderSync(order));
                threads.Add(t);
                t.Start();
            }

            foreach (var t in threads)
            {
                t.Join();
            }

            syncSw.Stop();
            int syncMaxThreads = Process.GetCurrentProcess().Threads.Count;
            int syncThreadPoolEnd = ThreadPool.ThreadCount;

            var syncFinalOrders = (await orderRepo.GetAllAsync(ct)).Where(o => o.Id <= 10).ToList();
            int syncCompleted = syncFinalOrders.Count(o => o.Status == OrderStatus.Completed);
            int syncRejected = syncFinalOrders.Count(o => o.Status == OrderStatus.Rejected);

            // ==========================================================
            // 2. ASYNCHRONOUS RUN (Orders #1–#10)
            // ==========================================================
            await DataSeeder.ResetAsync();
            var asyncOrders = (await incomingRepo.GetAllAsync(ct)).Take(10).ToList();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            int asyncInitialThreads = Process.GetCurrentProcess().Threads.Count;
            int asyncInitialThreadPool = ThreadPool.ThreadCount;
            var asyncSw = Stopwatch.StartNew();

            var asyncTasks = asyncOrders.Select(order => asyncProcessor.ProcessOrderAsync(order, ct)).ToList();
            await Task.WhenAll(asyncTasks);

            asyncSw.Stop();
            int asyncMaxThreads = Process.GetCurrentProcess().Threads.Count;
            int asyncThreadPoolEnd = ThreadPool.ThreadCount;

            var asyncFinalOrders = (await orderRepo.GetAllAsync(ct)).Where(o => o.Id <= 10).ToList();
            int asyncCompleted = asyncFinalOrders.Count(o => o.Status == OrderStatus.Completed);
            int asyncRejected = asyncFinalOrders.Count(o => o.Status == OrderStatus.Rejected);

            // ==========================================================
            // 3. BENCHMARK SUMMARY & METRICS
            // ==========================================================
            string syncResults = $"{syncCompleted}/{syncRejected} (Expected 8/2)";
            string asyncResults = $"{asyncCompleted}/{asyncRejected} (Expected 8/2)";
            string syncTime = $"{syncSw.ElapsedMilliseconds} ms";
            string asyncTime = $"{asyncSw.ElapsedMilliseconds} ms";
            string syncProcessThreads = $"{syncInitialThreads} -> {syncMaxThreads}";
            string asyncProcessThreads = $"{asyncInitialThreads} -> {asyncMaxThreads}";
            string syncPoolThreads = $"{syncInitialThreadPool} -> {syncThreadPoolEnd}";
            string asyncPoolThreads = $"{asyncInitialThreadPool} -> {asyncThreadPoolEnd}";

            Console.WriteLine("\n=================== BENCHMARK RESULTS (Orders #1–#10) ===================");
            Console.WriteLine($"{"Metric",-25} | {"Sync Version",-22} | {"Async Version",-22}");
            Console.WriteLine(new string('-', 75));
            Console.WriteLine($"{"Completed / Rejected",-25} | {syncResults,-22} | {asyncResults,-22}");
            Console.WriteLine($"{"Execution Time",-25} | {syncTime,-22} | {asyncTime,-22}");
            Console.WriteLine($"{"OS Threads Created",-25} | {"10 dedicated threads",-22} | {"0 extra dedicated",-22}");
            Console.WriteLine($"{"Process Thread Count",-25} | {syncProcessThreads,-22} | {asyncProcessThreads,-22}");
            Console.WriteLine($"{"ThreadPool Thread Count",-25} | {syncPoolThreads,-22} | {asyncPoolThreads,-22}");
            Console.WriteLine("=========================================================================\n");
        }
    }
}