using ConsoleApp1.NewFolder;
using ConsoleApp1.NewFolder1;

namespace ConsoleApp1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();

            // التقاط حدث (Ctrl+C) لإيقاف العمليات بأمان إذا لزم الأمر
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("\n[System] Cancellation requested by user (Ctrl+C). Shutting down gracefully...");
                cts.Cancel();
                e.Cancel = true;
            };

            try
            {
                // 1. Task 10.1: Live Checkout Progress (Task.WhenEach)
                await Task101Runner.RunLiveCheckoutAsync(cts.Token);

                // 2. Task 10.2: Customer Dashboard (Task.WhenAll & AggregateException)
                await Task102Runner.RunDashboardTestAsync(cts.Token);

                // 3. Task 10.3: Fastest Shipping Quote (Task.WhenAny & Time Budgeting)
                await Task103Runner.RunFastestQuoteTestAsync(cts.Token);

                // 4. Task 10.4: Fraud Scoring (CPU-bound Parallel.For) & Email Re-send (I/O-bound Parallel.ForEachAsync)
                await Task104Runner.RunAsync(cts.Token);

                Console.WriteLine("\n==================================================");
                Console.WriteLine(" All Phase 10 tasks completed successfully!");
                Console.WriteLine("==================================================");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\n[System] Execution was successfully cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Fatal Error] An unexpected error crashed the program: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        
    }
    }
    }
    
