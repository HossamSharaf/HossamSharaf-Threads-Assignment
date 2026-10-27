using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ConsoleApp1.NewFolder1
{
    public interface IShippingCarrier
    {
        string Name { get; }
        Task<(string Carrier, decimal Price, long ElapsedMs)> GetQuoteAsync(Order order, CancellationToken ct);
    }

    public class ShippingCarrier : IShippingCarrier
    {
        public string Name { get; }
        private readonly int _minDelay;
        private readonly int _maxDelay;
        private readonly decimal _factor;
        private readonly HashSet<int> _failForOrderIds;

        public ShippingCarrier(string name, int minDelay, int maxDelay, decimal factor, IEnumerable<int>? failForOrderIds = null)
        {
            Name = name;
            _minDelay = minDelay;
            _maxDelay = maxDelay;
            _factor = factor;
            _failForOrderIds = new HashSet<int>(failForOrderIds ?? Array.Empty<int>());
        }

        public async Task<(string Carrier, decimal Price, long ElapsedMs)> GetQuoteAsync(Order order, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            // 150 ms handshake
            await Task.Delay(150, ct);

            // CairoExpress throws for even order ids
            if (_failForOrderIds.Contains(order.Id))
            {
                throw new ShippingProviderUnavailableException($"{Name} failed for Order #{order.Id}");
            }

            // Random time in its range
            int randomDelay = Random.Shared.Next(_minDelay, _maxDelay + 1);
            await Task.Delay(randomDelay, ct);

            sw.Stop();

            // Price calculation (Formula from phase 8.1 multiplied by factor)
            // افتراض معادلة حساب السعر الأساسية أو استبدالها بقيمة منطقية بناءً على وزن/مجموع الأوردر
            decimal basePrice = order.TotalAmount > 0 ? order.TotalAmount * 0.05m : 50.0m;
            decimal finalPrice = Math.Round(basePrice * _factor, 2);

            return (Name, finalPrice, sw.ElapsedMilliseconds);
        }
    }

    public class ShippingQuoteService
    {
        private readonly List<IShippingCarrier> _carriers;

        public ShippingQuoteService(IEnumerable<IShippingCarrier> carriers)
        {
            _carriers = carriers.ToList();
        }

        public async Task<(string Carrier, decimal Price, long ElapsedMs, List<string> CancelledCarriers)> GetFastestQuoteAsync(Order order, CancellationToken ct = default)
        {
            // R6: Cancelling caller's ct cancels every carrier immediately
            using var budgetCts = new CancellationTokenSource(TimeSpan.FromSeconds(1.2)); // R4: Time budget 1.2 s
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, budgetCts.Token);

            var runningTasks = new Dictionary<Task<(string Carrier, decimal Price, long ElapsedMs)>, IShippingCarrier>();

            // R1: Ask all three carriers at the same time
            foreach (var carrier in _carriers)
            {
                var task = carrier.GetQuoteAsync(order, combinedCts.Token);
                runningTasks[task] = carrier;
            }

            var cancelledCarriers = new List<string>();
            var failedCarriersCount = 0;

            // R2: Use the first successful quote. If it failed, keep waiting for the next.
            while (runningTasks.Count > 0)
            {
                Task<(string Carrier, decimal Price, long ElapsedMs)> finishedTask;
                try
                {
                    finishedTask = await Task.WhenAny(runningTasks.Keys);
                }
                catch (OperationCanceledException) when (budgetCts.IsCancellationRequested && !ct.IsCancellationRequested)
                {
                    // R4: Time budget expired (1.2s) without a successful quote
                    foreach (var kvp in runningTasks)
                    {
                        kvp.Value.GetCancellationTokenSourceCancelQuietly(); // أو إلغاء المهام
                        cancelledCarriers.Add(kvp.Value.Name);
                        Console.WriteLine($"[{kvp.Value.Name}] cancelled");
                    }
                    return ("Flat rate", 25.00m, 1200, cancelledCarriers);
                }
                catch (OperationCanceledException)
                {
                    // R6: User/Caller cancellation
                    throw;
                }

                var carrier = runningTasks[finishedTask];
                runningTasks.Remove(finishedTask);

                if (finishedTask.IsCompletedSuccessfully)
                {
                    // WINNER FOUND!
                    var result = await finishedTask;

                    // R3: Cancel every carrier still running
                    foreach (var kvp in runningTasks)
                    {
                        cancelledCarriers.Add(kvp.Value.Name);
                        Console.WriteLine($"[{kvp.Value.Name}] cancelled");
                    }
                    combinedCts.Cancel(); // إلغاء البقية

                    return (result.Carrier, result.Price, result.ElapsedMs, cancelledCarriers);
                }
                else
                {
                    // The carrier failed, log/track and keep waiting for the next (R2)
                    failedCarriersCount++;
                }
            }

            // R5: If all three carriers fail within the budget
            if (failedCarriersCount >= _carriers.Count)
            {
                throw new ShippingProviderUnavailableException("All carriers failed");
            }

            // Fallback
            return ("Flat rate", 25.00m, 0, cancelledCarriers);
        }
    }

    internal static class ExtensionHelpers
    {
        public static void GetCancellationTokenSourceCancelQuietly(this IShippingCarrier carrier) { }
    }

}
