using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public class SyncOrderValidator
    {
        private const string Source = "SyncOrderValidator";

        private readonly SyncCustomerRepository _customerRepo;
        private readonly SyncProductRepository _productRepo;
        private readonly SyncCouponRepository _couponRepo;

        public SyncOrderValidator(
            SyncCustomerRepository customerRepo,
            SyncProductRepository productRepo,
            SyncCouponRepository couponRepo)
        {
            _customerRepo = customerRepo;
            _productRepo = productRepo;
            _couponRepo = couponRepo;
        }

        public bool ValidateOrder(Order order)
        {
            Log.Info(Source, $"[Sync] Validating Order #{order.Id}...");

            // 1. Customer exists
            var customer = _customerRepo.GetById(order.CustomerId);
            if (customer == null)
            {
                return RejectOrder(order, $"Customer #{order.CustomerId} not found");
            }

            // 2. Customer is active
            if (!customer.IsActive)
            {
                return RejectOrder(order, "Customer is inactive");
            }

            // 3 & 4. Products exist and stock check
            foreach (var item in order.Items)
            {
                var product = _productRepo.GetById(item.ProductId);
                if (product == null)
                {
                    return RejectOrder(order, $"Product #{item.ProductId} not found");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    return RejectOrder(order, $"Insufficient stock for '{product.Name}'");
                }
            }

            // 5 & 6. Coupon validation
            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                var coupon = _couponRepo.GetByCode(order.CouponCode);
                if (coupon == null)
                {
                    return RejectOrder(order, $"Coupon '{order.CouponCode}' does not exist");
                }

                if (coupon.ExpiryDate < DateTime.Today)
                {
                    return RejectOrder(order, $"Coupon '{order.CouponCode}' has expired");
                }
            }

            order.Status = OrderStatus.Validated;
            order.FailureReason = null;
            Log.Success(Source, $"[Sync] Order #{order.Id} passed validation.");
            return true;
        }

        private bool RejectOrder(Order order, string reason)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = reason;
            Log.Error(Source, $"[Sync] Order #{order.Id} rejected: {reason}");
            return false;
        }
    }
}
