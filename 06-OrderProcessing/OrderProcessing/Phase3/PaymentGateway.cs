using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public class PaymentGateway : IPaymentGateway
    {
        private const string Source = "PaymentGateway";
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        // قوائم الأعطال
        private readonly HashSet<int> _timeoutOrderIds;
        private readonly HashSet<int> _declineOrderIds;

        public PaymentGateway(IEnumerable<int>? timeoutOrderIds = null, IEnumerable<int>? declineOrderIds = null)
        {
            _timeoutOrderIds = new HashSet<int>(timeoutOrderIds ?? Array.Empty<int>());
            _declineOrderIds = new HashSet<int>(declineOrderIds ?? Array.Empty<int>());
        }

        public async Task<PaymentRecord> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct = default)
        {
            Log.Info(Source, $"Initiating payment for Order #{orderId}...");

            // Order #5: Payment timeout simulation (5000ms instead of ~700ms)
            if (_timeoutOrderIds.Contains(orderId))
            {
                await Task.Delay(5000, ct);
            }
            else
            {
                // Normal processing delay
                await Task.Delay(200, ct);
                await Task.Delay(500, ct);
            }

            string txnId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper();

            bool success = true;
            string status = "Captured";
            string message = "Payment successful";

            // Order #8: Payment declined simulation
            if (_declineOrderIds.Contains(orderId))
            {
                success = false;
                status = "Declined";
                message = "Card declined by issuer";
            }

            var record = new PaymentRecord
            {
                TransactionId = txnId,
                OrderId = orderId,
                Amount = amount,
                Status = status,
                Success = success,
                Message = message
            };

            await _lock.WaitAsync(ct);
            try
            {
                var filePath = DataFiles.GetFilePath(DataFiles.PaymentsFile);
                var records = await JsonFileStore.ReadAsync<PaymentRecord>(filePath, ct);
                records.Add(record);
                await JsonFileStore.WriteAsync(filePath, records, ct);
            }
            finally
            {
                _lock.Release();
            }

            Log.Info(Source, $"Payment {status} for Order #{orderId} (TXN: {txnId})");
            return record;
        }

        // Refund remains the same...
        public async Task RefundAsync(string transactionId, CancellationToken ct = default) { /* نفس كود 9.1 */ }
    }
}