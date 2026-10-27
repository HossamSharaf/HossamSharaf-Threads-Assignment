using ConsoleApp1.NewFolder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ConsoleApp1.NewFolder1
{
    public static class Task104Runner
    {
        // دالة حساب نقاط الاحتيال (CPU-bound)
        private static int Score(int orderId)
        {
            long h = orderId;
            for (int i = 0; i < 100_000; i++)
                h = (h * 31 + i) % 1_000_000_007;
            return (int)(h % 100);
        }

        public static async Task RunAsync(CancellationToken ct = default)
        {
            Console.WriteLine("\n=== TASK 10.4: FRAUD SCORING & EMAIL RE-SEND ===");

            // ==========================================================
            // PART A: Fraud Scoring (CPU-bound)
            // ==========================================================
            Console.WriteLine("\n[Part A] Running CPU-bound Fraud Scoring for 10,000 orders...");

            // R1: Sequential Loop
            var seqSw = Stopwatch.StartNew();
            var seqFlagged = new List<int>();
            var seqThreads = new ConcurrentDictionary<int, byte>();

            for (int id = 1; id <= 10_000; id++)
            {
                seqThreads.TryAdd(Environment.CurrentManagedThreadId, 0);
                if (Score(id) >= 90)
                {
                    seqFlagged.Add(id);
                }
            }
            seqSw.Stop();
            seqFlagged.Sort();

            // R2 & R3 & R4: Parallel Loop using Parallel.For (Built-in concurrency limit via MaxDegreeOfParallelism)
            var parSw = Stopwatch.StartNew();
            var parFlaggedBag = new ConcurrentBag<int>();
            var parThreads = new ConcurrentDictionary<int, byte>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct
            };

            try
            {
                Parallel.For(1, 10_001, parallelOptions, id =>
                {
                    parThreads.TryAdd(Environment.CurrentManagedThreadId, 0);
                    if (Score(id) >= 90)
                    {
                        parFlaggedBag.Add(id);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Part A] Parallel scoring was cancelled.");
                return;
            }
            parSw.Stop();

            var parFlagged = parFlaggedBag.OrderBy(x => x).ToList();
            double speedUp = (double)seqSw.ElapsedMilliseconds / Math.Max(1, parSw.ElapsedMilliseconds);

            Console.WriteLine("\n--- Part A Results ---");
            Console.WriteLine($"Flagged Count:             {parFlagged.Count} (Expected 1005)");
            Console.WriteLine($"Sequential Time:           {seqSw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Parallel Time:             {parSw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Speed-up:                  {speedUp:F2}x");
            Console.WriteLine($"Distinct Threads Used:     {parThreads.Count}");
            Console.WriteLine($"Environment.ProcessorCount:{Environment.ProcessorCount}");
            Console.WriteLine($"First 5 Flagged Ids:       {string.Join(", ", parFlagged.Take(5))} (Expected 4, 8, 12, 29, 33)");


            // ==========================================================
            // PART B: Email Re-send (I/O-bound)
            // ==========================================================
            Console.WriteLine("\n[Part B] Running I/O-bound Email Re-send (30 test emails, 1001-1030)...");

            var orderIds = Enumerable.Range(1001, 30).ToList();
            var emailService = new EmailService();

            int currentConcurrentSends = 0;
            int maxConcurrentSends = 0;

            var ioSw = Stopwatch.StartNew();

            var parallelIoOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 4, // R1: at most 4 running at the same time
                CancellationToken = ct
            };

            try
            {
                // استخدام Parallel.ForEachAsync المقيد بالـ MaxDegreeOfParallelism بدون الحاجة لـ semaphores يدوية
                await Parallel.ForEachAsync(orderIds, parallelIoOptions, async (orderId, token) =>
                {
                    int current = Interlocked.Increment(ref currentConcurrentSends);

                    // تتبع أقصى عدد من المهام المتزامنة التي تم تشغيلها في نفس اللحظة
                    int snapshot;
                    do
                    {
                        snapshot = maxConcurrentSends;
                        if (current <= snapshot) break;
                    } while (Interlocked.CompareExchange(ref maxConcurrentSends, current, snapshot) != snapshot);

                    try
                    {
                        var dummyOrder = new Order { Id = orderId, CustomerId = 1, TotalAmount = 250.00m };
                        var dummyCustomer = new Customer { Id = 1, Email = $"user{orderId}@ecommerce.com" };

                        // محاكاة استجابة الشبكة للإيميل (تأخير زمني قدره ثانية واحدة لكل إيميل)
                        await Task.Delay(1000, token);
                        await emailService.SendConfirmationAsync(dummyOrder, dummyCustomer, token);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref currentConcurrentSends);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Part B] Email re-send was cancelled.");
            }

            ioSw.Stop();

            Console.WriteLine("\n--- Part B Results ---");
            Console.WriteLine($"Total Time:                {ioSw.ElapsedMilliseconds} ms (~{ioSw.ElapsedMilliseconds / 1000.0:F1} s)");
            Console.WriteLine($"Highest Concurrent Sends:  {maxConcurrentSends} (Expected exactly 4)");
            Console.WriteLine("==================================================\n");
        }
    }
}
