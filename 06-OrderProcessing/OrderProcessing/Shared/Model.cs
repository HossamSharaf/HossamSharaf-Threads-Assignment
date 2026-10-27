using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public enum LoyaltyTier { Bronze, Silver, Gold }
    public enum OrderStatus { Pending, Validated, Rejected, PaymentFailed, Completed, Failed }
    public enum PaymentStatus { Captured, Declined, Refunded }

    // ================= Classes =================

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public LoyaltyTier LoyaltyTier { get; set; }

        public Customer() { }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; } // Money = decimal
        public int StockQuantity { get; set; }
        public decimal WeightKg { get; set; }

        public Product() { }
    }

    public class Coupon
    {
        public string Code { get; set; } = string.Empty;
        public int DiscountPercent { get; set; }
        public DateTime ExpiryDate { get; set; }

        public Coupon() { }
    }

    public class OrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } // Money = decimal

        public OrderItem() { }
    }

    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>(); // Initialize to avoid null
        public string? CouponCode { get; set; } // Nullable (?) as requested
        public decimal SubTotal { get; set; } // Money
        public decimal Discount { get; set; } // Money
        public decimal ShippingCost { get; set; } // Money
        public decimal TotalAmount { get; set; } // Money
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? FailureReason { get; set; } // Nullable (?)
        public string? TransactionId { get; set; } // Nullable (?)

        public Order() { }
    }

    public class PaymentTransaction
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; } // Money
        public PaymentStatus Status { get; set; }
        public string? TransactionId { get; set; } // Nullable (?)
        public DateTime CreatedAt { get; set; }

        public PaymentTransaction() { }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; } // Nullable (?)
        public PaymentStatus Status { get; set; }
        public string? Message { get; set; } // Nullable (?)

        public PaymentResult() { }
    }

    public class EmailMessage
    {
        public Guid Id { get; set; }
        public int OrderId { get; set; }
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime? SentAt { get; set; } // Nullable because it might not be sent yet
        public int Attempts { get; set; }
        public string? LastError { get; set; } // Nullable (?)

        public EmailMessage() { }
    }

    public class Report
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; } // Money
        public int CompletedCount { get; set; }
        public int RejectedCount { get; set; }
        public int FailedCount { get; set; }
        public int PaymentFailedCount { get; set; }
        public string? TopSellingProduct { get; set; }
        public int TopSellingQuantity { get; set; }
        public decimal AverageOrderValue { get; set; } // Money
        public DateTime GeneratedAt { get; set; }

        public Report() { }
    }
}
