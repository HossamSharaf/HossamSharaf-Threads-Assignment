using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public class EmailService : IEmailService
    {
        private const string Source = "EmailService";
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public async Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct = default)
        {
            await SimulatedLatency.SimulateExternalApiAsync(Source, ct);

            var record = new EmailRecord
            {
                OrderId = order.Id,
                CustomerEmail = customer.Email,
                Subject = $"Order #{order.Id} Confirmation",
                SentAt = DateTime.Now
            };

            await _lock.WaitAsync(ct);
            try
            {
                var filePath = DataFiles.GetFilePath(DataFiles.EmailsFile);
                var records = await JsonFileStore.ReadAsync<EmailRecord>(filePath, ct);
                records.Add(record);
                await JsonFileStore.WriteAsync(filePath, records, ct);

                Log.Success(Source, $"Confirmation email sent to {customer.Email} for Order #{order.Id}.");
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
