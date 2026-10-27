using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public class EmailService : IEmailService
    {
        private const string Source = "EmailService";
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        // قاموس لتخزين عدد مرات الفشل المطلوبة لكل أوردر
        private readonly Dictionary<int, int> _failAttemptsConfig;

        // قاموس لتتبع عدد المحاولات الحالية التي حدثت لكل أوردر
        private readonly Dictionary<int, int> _currentAttempts = new();

        public EmailService(Dictionary<int, int>? failAttempts = null)
        {
            _failAttemptsConfig = failAttempts ?? new Dictionary<int, int>();
        }

        public async Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct = default)
        {
            await SimulatedLatency.SimulateExternalApiAsync(Source, ct);

            int currentAttemptCount = 1;

            // قفل برمجي لتحديث عداد المحاولات بشكل آمن (Concurrency safe)
            await _lock.WaitAsync(ct);
            try
            {
                if (!_currentAttempts.ContainsKey(order.Id))
                {
                    _currentAttempts[order.Id] = 0;
                }
                _currentAttempts[order.Id]++;
                currentAttemptCount = _currentAttempts[order.Id];
            }
            finally
            {
                _lock.Release();
            }

            // Order #3 (fails twice) & Order #17 (fails max times)
            if (_failAttemptsConfig.TryGetValue(order.Id, out int maxFailsToSimulate))
            {
                if (currentAttemptCount <= maxFailsToSimulate)
                {
                    Log.Error(Source, $"SMTP simulation failure for Order #{order.Id} (Attempt {currentAttemptCount})");
                    throw new SmtpUnavailableException("SmtpUnavailableException");
                }
            }

            // إذا تخطى المحاولات الفاشلة، يرسل الإيميل بنجاح
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
