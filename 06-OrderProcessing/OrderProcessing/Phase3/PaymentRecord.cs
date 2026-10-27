using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public interface IPaymentGateway
    {
        Task<PaymentRecord> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct = default);
        Task RefundAsync(string transactionId, CancellationToken ct = default);
    }
    public class PaymentRecord
    {
        public string TransactionId { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty; // "Captured", "Declined", "Refunded"
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

}
