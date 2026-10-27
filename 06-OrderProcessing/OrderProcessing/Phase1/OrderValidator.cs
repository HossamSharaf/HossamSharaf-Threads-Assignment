using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public class OrderValidator
    {
        private const string Source = "OrderValidator";

        // حقن الـ Repositories اللي هنحتاجها
        private readonly CustomerRepository _customerRepo;
        private readonly ProductRepository _productRepo;
        private readonly CouponRepository _couponRepo;
        private readonly OrderRepository _orderRepo;

        public OrderValidator(
            CustomerRepository customerRepo,
            ProductRepository productRepo,
            CouponRepository couponRepo,
            OrderRepository orderRepo)
        {
            _customerRepo = customerRepo;
            _productRepo = productRepo;
            _couponRepo = couponRepo;
            _orderRepo = orderRepo;
        }

        public async Task<bool> ValidateOrderAsync(Order order, CancellationToken ct = default)
        {
            Log.Info(Source, $"Validating Order #{order.Id}...");

            // 1. Customer exists
            var customer = await _customerRepo.GetByIdAsync(order.CustomerId, ct);
            if (customer == null)
            {
                return await RejectOrderAsync(order, $"Customer #{order.CustomerId} not found", ct);
            }

            // 2. Customer is active
            if (!customer.IsActive)
            {
                return await RejectOrderAsync(order, "Customer is inactive", ct);
            }

            // فحص المنتجات داخل الطلب
            foreach (var item in order.Items)
            {
                // 3. Every product exists
                var product = await _productRepo.GetByIdAsync(item.ProductId, ct);
                if (product == null)
                {
                    return await RejectOrderAsync(order, $"Product #{item.ProductId} not found", ct);
                }

                // 4. StockQuantity >= Quantity (checked, not reserved)
                if (product.StockQuantity < item.Quantity)
                {
                    return await RejectOrderAsync(order, $"Insufficient stock for '{product.Name}'", ct);
                }
            }

            // فحص الكوبون (إذا كان موجوداً)
            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                var coupon = await _couponRepo.GetByCodeAsync(order.CouponCode, ct);

                // 5. Coupon exists
                if (coupon == null)
                {
                    return await RejectOrderAsync(order, $"Coupon '{order.CouponCode}' does not exist", ct);
                }

                // 6. Coupon ExpiryDate >= DateTime.Today
                if (coupon.ExpiryDate < DateTime.Today)
                {
                    return await RejectOrderAsync(order, $"Coupon '{order.CouponCode}' has expired", ct);
                }
            }

            // ================== النجاح (Passed) ==================

            order.Status = OrderStatus.Validated;
            order.FailureReason = null; // تنظيف أي سبب فشل قديم إن وجد

            // "Both are saved to orders.json" - الحفظ في حالة النجاح
            await _orderRepo.SaveAsync(order, ct);

            Log.Success(Source, $"Order #{order.Id} passed validation.");
            return true;
        }

        // دالة مساعدة لتوحيد عملية الرفض وتقليل تكرار الكود
        private async Task<bool> RejectOrderAsync(Order order, string reason, CancellationToken ct)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = reason; // Exact text requested

            // "Both are saved to orders.json" - الحفظ في حالة الفشل
            await _orderRepo.SaveAsync(order, ct);

            Log.Error(Source, $"Order #{order.Id} rejected: {reason}");
            return false;
        }
    }
}
