using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public class OrderProcessorV2
    {
        private const string Source = "OrderProcessorV2";

        private readonly ProductRepository _productRepo;
        private readonly OrderRepository _orderRepo;
        private readonly OrderPricer _pricer;
        private readonly InvoiceGenerator _invoiceGenerator;

        public OrderProcessorV2(
            ProductRepository productRepo,
            OrderRepository orderRepo,
            OrderPricer pricer,
            InvoiceGenerator invoiceGenerator)
        {
            _productRepo = productRepo;
            _orderRepo = orderRepo;
            _pricer = pricer;
            _invoiceGenerator = invoiceGenerator;
        }

        public async Task<bool> ProcessValidatedOrderAsync(Order order, CancellationToken ct = default)
        {
            Log.Info(Source, $"Processing Validated Order #{order.Id}...");

            var reservedItems = new List<(int ProductId, int Qty)>();

            try
            {
                // 1. حجز المخزون لكل عنصر بالترتيب باستخدام ReserveStockAsync الآمنة
                foreach (var item in order.Items)
                {
                    var product = await _productRepo.GetByIdAsync(item.ProductId, ct);
                    string productName = product?.Name ?? $"Product #{item.ProductId}";

                    bool reserved = await _productRepo.ReserveStockAsync(item.ProductId, item.Quantity, ct);
                    if (!reserved)
                    {
                        // فك الحجز عن العناصر التي حُجزت مسبقاً لهذا الطلب
                        await RollbackReservedStockAsync(reservedItems, ct);

                        order.Status = OrderStatus.Rejected;
                        order.FailureReason = $"Insufficient stock for '{productName}'";
                        await _orderRepo.SaveAsync(order, ct);

                        Log.Error(Source, $"Order #{order.Id} rejected: {order.FailureReason}");
                        return false;
                    }

                    reservedItems.Add((item.ProductId, item.Quantity));
                }

                // 2. التسعير المتزامن
                await _pricer.PriceOrderAsync(order, ct);

                // 3. توليد الفاتورة
                await _invoiceGenerator.GenerateAsync(order, ct);

                // 4. تعيين الحالة إلى Completed والحفظ
                order.Status = OrderStatus.Completed;
                order.FailureReason = null;
                await _orderRepo.SaveAsync(order, ct);

                Log.Success(Source, $"Order #{order.Id} completed successfully.");
                return true;
            }
            catch (Exception)
            {
                // عند حدوث استثناء (مثل فشل الشحن للأوردر #7)، نقوم بإرجاع المخزون المحجوز
                await RollbackReservedStockAsync(reservedItems, ct);

                // إعادة رمي الاستثناء عمداً لأن التاسك تشترط: "does not catch exceptions"
                throw;
            }
        }

        private async Task RollbackReservedStockAsync(List<(int ProductId, int Qty)> reservedItems, CancellationToken ct)
        {
            foreach (var (productId, qty) in reservedItems)
            {
                await _productRepo.ReleaseStockAsync(productId, qty, ct);
            }
        }
    }
}
