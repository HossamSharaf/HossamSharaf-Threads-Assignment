using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public class OrderPricer
    {
        private const string Source = "OrderPricer";

        private readonly CustomerRepository _customerRepo;
        private readonly ProductRepository _productRepo;
        private readonly CouponRepository _couponRepo;
        private readonly HashSet<int> _failShippingForOrderIds;

        public OrderPricer(
            CustomerRepository customerRepo,
            ProductRepository productRepo,
            CouponRepository couponRepo,
            IEnumerable<int> failShippingForOrderIds)
        {
            _customerRepo = customerRepo;
            _productRepo = productRepo;
            _couponRepo = couponRepo;
            _failShippingForOrderIds = new HashSet<int>(failShippingForOrderIds);
        }

        // ======================= 1. SubTotal Calculation (DB Query) =======================
        private async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken ct = default)
        {
            await SimulatedLatency.SimulateDbQueryAsync(Source, $"Products for Order #{order.Id}", ct);

            decimal subTotal = 0m;
            foreach (var item in order.Items)
            {
                var product = await _productRepo.GetByIdAsync(item.ProductId, ct);
                if (product == null) throw new InvalidOperationException($"Product #{item.ProductId} not found.");

                item.UnitPrice = Round(product.Price);
                subTotal += (item.UnitPrice * item.Quantity);
            }

            return Round(subTotal);
        }

        // ======================= 2. Discount Calculation (External API) =======================
        private async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct = default)
        {
            await SimulatedLatency.SimulateExternalApiAsync(Source, ct);

            var customer = await _customerRepo.GetByIdAsync(order.CustomerId, ct);
            if (customer == null) throw new InvalidOperationException("Customer not found during pricing.");

            int loyaltyDiscount = customer.LoyaltyTier switch
            {
                LoyaltyTier.Gold => 10,
                LoyaltyTier.Silver => 5,
                _ => 0
            };

            int couponDiscount = 0;
            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                var coupon = await _couponRepo.GetByCodeAsync(order.CouponCode, ct);
                if (coupon != null && coupon.ExpiryDate >= DateTime.Today)
                {
                    couponDiscount = coupon.DiscountPercent;
                }
            }

            // Capped at 30%
            int totalDiscountPercent = Math.Min(loyaltyDiscount + couponDiscount, 30);

            return Round(order.SubTotal * totalDiscountPercent / 100m);
        }

        // ======================= 3. Shipping Cost Calculation (External API) =======================
        private async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct = default)
        {
            Log.Info(Source, "Shipping API: Performing DNS lookup and TLS handshake...");
            await Task.Delay(150, ct);

            // محاكاة تعطل خدمة الشحن للأوردات المحددة فوراً بعد الـ Handshake
            if (_failShippingForOrderIds.Contains(order.Id))
            {
                throw new ShippingProviderUnavailableException("Shipping provider unavailable");
            }

            Log.Info(Source, "Shipping API: Sending request...");
            await Task.Delay(300, ct);
            Log.Info(Source, "Shipping API: Waiting for API response...");
            await Task.Delay(Random.Shared.Next(500, 1501), ct);

            if (order.SubTotal >= 1000m)
            {
                return 0m;
            }

            int totalQuantity = 0;
            decimal totalWeight = 0m;

            foreach (var item in order.Items)
            {
                var product = await _productRepo.GetByIdAsync(item.ProductId, ct);
                if (product != null)
                {
                    totalQuantity += item.Quantity;
                    totalWeight += (product.WeightKg * item.Quantity);
                }
            }

            decimal shipping = 5.00m + (1.50m * totalQuantity) + (2.00m * totalWeight);
            return Round(shipping);
        }

        // ======================= 4. Concurrent Orchestration (Task.WhenAll) =======================
        public async Task PriceOrderAsync(Order order, CancellationToken ct = default)
        {
            // SubTotal يجب أن يُحسب أولاً لأنه أساس للخصم والشحن
            order.SubTotal = await CalculateSubTotalAsync(order, ct);

            // تشغيل الخصم والشحن في نفس الوقت (Concurrently)
            var discountTask = GetDiscountAsync(order, ct);
            var shippingTask = CalculateShippingCostAsync(order, ct);

            await Task.WhenAll(discountTask, shippingTask);

            order.Discount = await discountTask;
            order.ShippingCost = await shippingTask;

            // TotalAmount = SubTotal - Discount + ShippingCost
            order.TotalAmount = Round(order.SubTotal - order.Discount + order.ShippingCost);
        }

        // ======================= 5. Sequential Orchestration (للمقارنة) =======================
        public async Task PriceOrderSequentialAsync(Order order, CancellationToken ct = default)
        {
            order.SubTotal = await CalculateSubTotalAsync(order, ct);
            order.Discount = await GetDiscountAsync(order, ct);
            order.ShippingCost = await CalculateShippingCostAsync(order, ct);

            order.TotalAmount = Round(order.SubTotal - order.Discount + order.ShippingCost);
        }

        private decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
