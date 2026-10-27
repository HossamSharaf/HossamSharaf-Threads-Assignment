using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public class PricingService
    {
        private const string Source = "PricingService";
        private readonly ProductRepository _productRepo;
        private readonly CustomerService _customerService;
        private readonly CouponRepository _couponRepo;
        private readonly HashSet<int> _failShippingForOrderIds;

        public PricingService(
            ProductRepository productRepo,
            CustomerService customerService,
            CouponRepository couponRepo,
            IEnumerable<int> failShippingForOrderIds)
        {
            _productRepo = productRepo;
            _customerService = customerService;
            _couponRepo = couponRepo;
            _failShippingForOrderIds = new HashSet<int>(failShippingForOrderIds ?? Array.Empty<int>());
        }

        public async Task CalculateTotalAsync(Order order, CancellationToken ct = default)
        {
            // 1. Database Query
            order.SubTotal = await CalculateSubTotalAsync(order, ct);

            // 2. Concurrent External APIs
            var discountTask = GetDiscountAsync(order, ct);
            var shippingTask = CalculateShippingCostAsync(order, ct);

            await Task.WhenAll(discountTask, shippingTask);

            order.Discount = await discountTask;
            order.ShippingCost = await shippingTask;
            order.TotalAmount = Round(order.SubTotal - order.Discount + order.ShippingCost);
        }

        private async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken ct)
        {
            await SimulatedLatency.SimulateDbQueryAsync(Source, $"Products for Order #{order.Id}", ct);
            decimal subTotal = 0m;
            foreach (var item in order.Items)
            {
                var product = await _productRepo.GetByIdAsync(item.ProductId, ct);
                if (product == null) throw new InvalidOperationException($"Product #{item.ProductId} missing.");
                item.UnitPrice = Round(product.Price);
                subTotal += item.UnitPrice * item.Quantity;
            }
            return Round(subTotal);
        }

        private async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct)
        {
            await SimulatedLatency.SimulateExternalApiAsync(Source, ct);
            var customer = await _customerService.GetCustomerAsync(order.CustomerId, ct);

            int discountPercent = customer.LoyaltyTier switch
            {
                LoyaltyTier.Gold => 10,
                LoyaltyTier.Silver => 5,
                _ => 0
            };

            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                var coupon = await _couponRepo.GetByCodeAsync(order.CouponCode, ct);
                if (coupon == null || coupon.ExpiryDate < DateTime.Today)
                {
                    // Strict exception as requested in 9.1
                    throw new InvalidCouponException($"Coupon '{order.CouponCode}' is invalid or expired.");
                }
                discountPercent += coupon.DiscountPercent;
            }

            discountPercent = Math.Min(discountPercent, 30);
            return Round(order.SubTotal * discountPercent / 100m);
        }

        private async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct)
        {
            await SimulatedLatency.SimulateExternalApiAsync(Source, ct);
            if (_failShippingForOrderIds.Contains(order.Id))
            {
                throw new ShippingProviderUnavailableException("Shipping provider unavailable");
            }

            if (order.SubTotal >= 1000m) return 0m;

            int totalQty = 0;
            decimal totalWeight = 0m;
            foreach (var item in order.Items)
            {
                var product = await _productRepo.GetByIdAsync(item.ProductId, ct);
                totalQty += item.Quantity;
                totalWeight += product!.WeightKg * item.Quantity;
            }

            return Round(5.00m + (1.50m * totalQty) + (2.00m * totalWeight));
        }

        private decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
