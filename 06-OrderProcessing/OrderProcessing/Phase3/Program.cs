using ConsoleApp1.NewFolder;

namespace ConsoleApp1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();

            // التقاط حدث (Ctrl+C) لتفعيل الإلغاء بدلاً من إغلاق البرنامج فوراً
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("\n[System] Cancellation requested by user (Ctrl+C). Shutting down gracefully...");
                cts.Cancel();

                // منع نظام التشغيل من إنهاء العملية فوراً للسماح لمهام التراجع (Rollback) بالعمل
                e.Cancel = true;
            };

            try
            {
                // استدعاء المنسق (Runner) الخاص بالمرحلة الثالثة
                await Phase3Runner.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\n[System] Execution was successfully cancelled. All safe states maintained.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Fatal Error] An unexpected error crashed the system: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
    }
    
