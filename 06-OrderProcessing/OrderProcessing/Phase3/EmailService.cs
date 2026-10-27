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

        // الدالة المطلوبة في الواجهة (Interface)
        public async Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct = default)
        {
            await SendConfirmationWithRetryAsync(order, customer, ct);
        }

        // تطبيق Task 9.5: 1 initial attempt + 3 retries (Total 4)
        private async Task SendConfirmationWithRetryAsync(Order order, Customer customer, CancellationToken ct)
        {
            int maxAttempts = 4;
            Exception? lastError = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await SimulatedLatency.SimulateExternalApiAsync(Source, ct);

                    // ==========================================
                    // محاكاة الأخطاء الوهمية بناءً على Task 9.2
                    // ==========================================
                    int simAttempts = 0;
                    await _lock.WaitAsync(ct);
                    try
                    {
                        if (!_currentAttempts.ContainsKey(order.Id)) _currentAttempts[order.Id] = 0;
                        _currentAttempts[order.Id]++;
                        simAttempts = _currentAttempts[order.Id];
                    }
                    finally { _lock.Release(); }

                    if (_failAttemptsConfig.TryGetValue(order.Id, out int maxFailsToSimulate) && simAttempts <= maxFailsToSimulate)
                    {
                        throw new SmtpUnavailableException("SmtpUnavailableException");
                    }

                    // ==========================================
                    // مسار النجاح (Success Path)
                    // ==========================================
                    var record = new EmailRecord
                    {
                        OrderId = order.Id,
                        CustomerEmail = customer.Email,
                        Subject = $"Order #{order.Id} Confirmation",
                        SentAt = DateTime.Now,
                        Attempts = attempt
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

                    Log.Success(Source, $"Confirmation email sent to {customer.Email} for Order #{order.Id} (Attempt {attempt}).");

                    return; // نعود بشكل طبيعي لأن المهمة تمت بنجاح
                }
                catch (Exception ex)
                {
                    lastError = ex;

                    // Log every failed attempt
                    Log.Error(Source, $"Email attempt {attempt} failed for Order #{order.Id}: {ex.Message}");

                    if (attempt < maxAttempts)
                    {
                        // فترات انتظار متصاعدة قبل إعادة المحاولة 1، 2، 3
                        int delayMs = attempt switch
                        {
                            1 => 500,  // Before retry 1
                            2 => 1000, // Before retry 2
                            3 => 2000, // Before retry 3
                            _ => 0
                        };

                        await Task.Delay(delayMs, ct);
                    }
                }
            }

            // ==========================================
            // إذا فشلت المحاولة الرابعة والأخيرة (Attempt 4)
            // ==========================================
            var retryRecord = new EmailRetryRecord
            {
                OrderId = order.Id,
                CustomerEmail = customer.Email,
                Attempts = maxAttempts,
                LastError = lastError?.Message ?? "Unknown Error"
            };

            // الحفظ في ملف email_retry.json
            await _lock.WaitAsync(ct);
            try
            {
                var retryFilePath = DataFiles.GetFilePath(DataFiles.EmailRetryFile);
                var retryRecords = await JsonFileStore.ReadAsync<EmailRetryRecord>(retryFilePath, ct);
                retryRecords.Add(retryRecord);
                await JsonFileStore.WriteAsync(retryFilePath, retryRecords, ct);
            }
            finally { _lock.Release(); }

            // طباعة الجملة المطلوبة بالنص في التاسك
            Log.Info(Source, $"Email for Order #{order.Id} queued for retry later");

            // return normally: لا نرمي أي Exception لكي يظل الأوردر في حالة Completed كما هو
        }
    }
}
