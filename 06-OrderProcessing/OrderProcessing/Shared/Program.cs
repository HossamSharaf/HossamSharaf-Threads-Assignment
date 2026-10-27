namespace ConsoleApp1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();

            // 2. التحكم في زرار Ctrl+C عشان ميقفلش البرنامج فجأة (R2)
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // بنمنع نظام التشغيل إنه يقتل البرنامج فوراً
                Log.Error("System", "Ctrl+C pressed. Initiating graceful shutdown...");
                cts.Cancel();    // بنبلغ كل الـ Tasks إننا عايزين نوقف
            };

            // 3. قراءة الـ Timeout من سطر الأوامر (R3)
            // e.g., dotnet run -- phase3 --cancel-after 4000
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--cancel-after" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int cancelAfterMs))
                    {
                        Log.Info("System", $"Auto-cancel scheduled in {cancelAfterMs} ms");
                        cts.CancelAfter(cancelAfterMs); // بيفعل الإلغاء تلقائياً بعد الوقت ده
                    }
                }
            }

            int finishedOrders = 0;
            int cancelledOrders = 0;

            try
            {
                // هنفترض إننا بنادي على الـ Processor اللي بيعالج الطلبات
                var processor = new OrderProcessor();

                // بنبعت الـ Token (cts.Token) لكل دالة (R1)
                (finishedOrders, cancelledOrders) = await processor.RunPhaseAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // ده هيحصل لو الـ Processor نفسه رمى الإيرور لبره
                Log.Info("System", "Main run caught cancellation.");
            }
            finally
            {
                // 4. طباعة السطر النهائي زي ما التاسك طلبت بالظبط (R5)
                if (cts.IsCancellationRequested)
                {
                    Console.WriteLine($"Run cancelled — {finishedOrders} orders finished, {cancelledOrders} cancelled.");
                }
                else
                {
                    Console.WriteLine($"Run completed — {finishedOrders} orders finished, {cancelledOrders} cancelled.");
                }
            }
        }
    }
}
