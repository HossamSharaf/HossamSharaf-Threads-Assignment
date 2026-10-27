using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public class PaymentGateway : IPaymentGateway
    {
        private const string Source = "PaymentGateway";
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public async Task<PaymentRecord> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct = default)
        {
            Log.Info(Source, $"Initiating payment for Order #{orderId}...");

            // Passing ct to every Task.Delay inside ProcessPaymentAsync
            await Task.Delay(200, ct);
            await Task.Delay(300, ct);
            await Task.Delay(Random.Shared.Next(500, 1500), ct);

            string txnId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper();

            // Simulate 10% decline chance (or customize based on your needs)
            string status = Random.Shared.Next(1, 11) > 9 ? "Declined" : "Captured";

            var record = new PaymentRecord
            {
                TransactionId = txnId,
                OrderId = orderId,
                Amount = amount,
                Status = status
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

        public async Task RefundAsync(string transactionId, CancellationToken ct = default)
        {
            await SimulatedLatency.SimulateExternalApiAsync(Source, ct);

            await _lock.WaitAsync(ct);
            try
            {
                var filePath = DataFiles.GetFilePath(DataFiles.PaymentsFile);
                var records = await JsonFileStore.ReadAsync<PaymentRecord>(filePath, ct);
                var record = records.FirstOrDefault(r => r.TransactionId == transactionId);

                if (record != null)
                {
                    record.Status = "Refunded";
                    await JsonFileStore.WriteAsync(filePath, records, ct);
                    Log.Success(Source, $"Transaction {transactionId} has been Refunded.");
                }
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
