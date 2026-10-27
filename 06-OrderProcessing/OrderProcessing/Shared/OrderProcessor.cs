using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public class OrderProcessor
    {
        public async Task<(int finished, int cancelled)> RunPhaseAsync(CancellationToken ct)
        {
            int finishedOrders = 0;
            int cancelledOrders = 0;

            // بنجيب الطلبات (محاكاة)
            var orders = new List<Order> { new Order { Id = 1 }, new Order { Id = 2 } };

            foreach (var order in orders)
            {
                // لو حصل إلغاء قبل ما نبدأ أوردر جديد، بنقف فوراً (R4: no new order is started)
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                bool isFinished = await ProcessSingleOrderAsync(order, ct);

                if (isFinished) finishedOrders++;
                else cancelledOrders++;
            }

            return (finishedOrders, cancelledOrders);
        }

        private async Task<bool> ProcessSingleOrderAsync(Order order, CancellationToken ct)
        {
            // Trackers for Rollback (عشان نعرف إيه اللي اتنفذ ونرجعه)
            bool stockReserved = false;
            bool paymentCaptured = false;

            try
            {
                // 1. حجز المخزون
                Log.Info($"Order #{order.Id}", "Reserving stock...");
                await Task.Delay(400, ct); // محاكاة لعملية بتاخد وقت وبتقبل الإلغاء (R1)
                stockReserved = true;

                // 2. سحب الفلوس
                Log.Info($"Order #{order.Id}", "Capturing payment...");
                await Task.Delay(700, ct); // محاكاة 
                paymentCaptured = true;

                // 3. حفظ الأوردر كـ Completed
                order.Status = OrderStatus.Completed;
                Log.Success($"Order #{order.Id}", "Order processed successfully.");
                return true; // Finished
            }
            catch (OperationCanceledException)
            {
                // ====== مرحلة التراجع (Rollback / Compensation) (R4) ======
                Log.Error($"Order #{order.Id}", "Processing cancelled. Rolling back...");

                // تنبيه هام: بنستخدم CancellationToken.None هنا عشان الـ Token الأصلي ملغي 
                // ولو بعتناه الدالة دي هتترفض وتفشل. إحنا لازم نرجع الفلوس والمخزون مهما حصل!

                if (paymentCaptured)
                {
                    Log.Info($"Order #{order.Id}", "Refunding payment...");
                    await RefundPaymentAsync(order.Id, CancellationToken.None);
                }

                if (stockReserved)
                {
                    Log.Info($"Order #{order.Id}", "Releasing reserved stock...");
                    await ReleaseStockAsync(order.Id, CancellationToken.None);
                }

                // الحفظ النهائي للأوردر الملغي حسب شروط التاسك (R4)
                order.Status = OrderStatus.Pending; // Not Failed!
                order.FailureReason = "Cancelled";

                await SaveCancelledOrderAsync(order, CancellationToken.None);

                return false; // Cancelled
            }
        }

        // دوال وهمية لمحاكاة عمليات التراجع (بنبعتلها Token بردو كإثبات لـ R1)
        private async Task RefundPaymentAsync(int orderId, CancellationToken ct)
            => await Task.Delay(200, ct);

        private async Task ReleaseStockAsync(int orderId, CancellationToken ct)
            => await Task.Delay(200, ct);

        private async Task SaveCancelledOrderAsync(Order order, CancellationToken ct)
            => await Task.Delay(200, ct);
    }
}
