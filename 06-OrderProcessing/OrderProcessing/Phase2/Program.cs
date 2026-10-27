namespace ConsoleApp1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Log.Error("System", "Ctrl+C pressed. Cancelling execution...");
                cts.Cancel();
            };

            try
            {
                Console.WriteLine("=== Starting E-Commerce Order Processing System ===");

                // 1. تشغيل مسار Phase 2 بالكامل (يتضمن الـ Reset، تجربة الـ Race Condition، ومعالجة الطلبات المتوازية)
                await Phase2Runner.RunAsync(cts.Token);

                // 2. تشغيل تقرير الأداء النهائي (Task 8.6)
                Console.WriteLine("\nGenerating report...");

                var orderRepo = new OrderRepository();
                var productRepo = new ProductRepository();
                var reportGenerator = new ReportGenerator(orderRepo, productRepo);

                // بدء التقرير والانتظار حتى اكتماله
                await reportGenerator.GenerateAsync(cts.Token);

                Console.WriteLine("\n=== System Execution Completed Successfully ===");
            }
            catch (OperationCanceledException)
            {
                Log.Info("System", "Operation was successfully cancelled by the user.");
            }
            catch (Exception ex)
            {
                Log.Error("System", $"Fatal unhandled exception: {ex.Message}");
            }
        }
    }
    }
    
