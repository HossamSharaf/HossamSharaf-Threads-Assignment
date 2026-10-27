namespace ConsoleApp1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();

            // 2. معالجة الـ Ctrl+C للـ Graceful Shutdown
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // نمنع إغلاق البرنامج التلقائي
                Log.Error("System", "Ctrl+C pressed. Cancelling Phase 1...");
                cts.Cancel();
            };

            // 3. قراءة أرجومنت الإلغاء بالوقت --cancel-after (Requirement 6.6 R3)
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--cancel-after" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int cancelAfterMs))
                    {
                        Log.Info("System", $"Auto-cancel scheduled after {cancelAfterMs} ms");
                        cts.CancelAfter(cancelAfterMs);
                    }
                }
            }

            try
            {
                // 4. تشغيل Phase 1
                await Phase1Runner.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Log.Info("System", "Phase 1 run was cancelled.");
            }
            catch (Exception ex)
            {
                Log.Error("System", $"Unhandled exception: {ex.Message}");
            }
        }
    }
    }
