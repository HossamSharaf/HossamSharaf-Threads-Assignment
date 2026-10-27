using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public class EmailService : IEmailService
    {
        private const string Source = "EmailService";
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        private readonly Dictionary<int, int> _failAttemptsConfig;
        private readonly Dictionary<int, int> _currentAttempts = new();

        public EmailService(Dictionary<int, int>? failAttempts = null)
        {
            _failAttemptsConfig = failAttempts ?? new Dictionary<int, int>();
        }

        public async Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct = default)
        {
            int maxAttempts = 4;
            int currentAttempt = 0;
            Exception? lastError = null;

            while (currentAttempt < maxAttempts)
            {
                currentAttempt++;
                try
                {
                    await SimulatedLatency.SimulateExternalApiAsync(Source, ct);

                    int simAttempts = 0;
                    await _lock.WaitAsync(ct);
                    try
                    {
                        if (!_currentAttempts.ContainsKey(order.Id)) _currentAttempts[order.Id] = 0;
                        _currentAttempts[order.Id]++;
                        simAttempts = _currentAttempts[order.Id];
                    }
                    finally { _lock.Release(); }

                    // محاكاة الفشل (Order #3 يفشل مرتين، Order #17 يفشل دائماً)
                    if (_failAttemptsConfig.TryGetValue(order.Id, out int maxFailsToSimulate) && simAttempts <= maxFailsToSimulate)
                    {
                        throw new SmtpUnavailableException($"SMTP Unavailable on attempt {currentAttempt}");
                    }

                    // النجاح: حفظ في emails.json والخروج
                    var record = new EmailRecord
                    {
                        OrderId = order.Id,
                        CustomerEmail = customer.Email,
                        Subject = $"Order #{order.Id} Confirmation",
                        SentAt = DateTime.Now,
                        Attempts = currentAttempt
                    };

                    await _lock.WaitAsync(ct);
                    try
                    {
                        var filePath = DataFiles.GetFilePath(DataFiles.EmailsFile);
                        var records = await JsonFileStore.ReadAsync<EmailRecord>(filePath, ct);
                        records.Add(record);
                        await JsonFileStore.WriteAsync(filePath, records, ct);
                    }
                    finally { _lock.Release(); }

                    Log.Success(Source, $"Email sent to {customer.Email} for Order #{order.Id} on Attempt {currentAttempt}");
                    return; // نجحنا، نخرج من الدالة
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Log.Error(Source, $"Email attempt {currentAttempt} failed for Order #{order.Id}: {ex.Message}");

                    if (currentAttempt < maxAttempts)
                    {
                        await Task.Delay(500, ct); // انتظار بسيط قبل المحاولة القادمة
                    }
                }
            }

            // إذا وصلنا هنا، يعني أن جميع المحاولات الـ 4 فشلت (مثل Order #17) -> نحفظ في email_retry.json
            var retryRecord = new EmailRetryRecord
            {
                OrderId = order.Id,
                CustomerEmail = customer.Email,
                Attempts = currentAttempt,
                LastError = lastError?.Message ?? "Unknown Error"
            };

            await _lock.WaitAsync(ct);
            try
            {
                var retryFilePath = DataFiles.GetFilePath(DataFiles.EmailRetryFile);
                var retryRecords = await JsonFileStore.ReadAsync<EmailRetryRecord>(retryFilePath, ct);
                retryRecords.Add(retryRecord);
                await JsonFileStore.WriteAsync(retryFilePath, retryRecords, ct);
            }
            finally { _lock.Release(); }

            Log.Error(Source, $"Max retries reached for Order #{order.Id}. Saved to Dead-Letter Queue (email_retry.json).");
        }
    }
}
