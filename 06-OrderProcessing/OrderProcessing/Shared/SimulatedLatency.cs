using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public static class SimulatedLatency
    {
        // 1. DB Query (Get Methods)
        public static async Task SimulateDbQueryAsync(string source, string queryDetail, CancellationToken ct = default)
        {
            Log.Info(source, "Opening connection...");
            await Task.Delay(200, ct);

            Log.Info(source, $"Executing query for {queryDetail}...");
            int queryDelay = Random.Shared.Next(300, 601); // 300 to 600 ms
            await Task.Delay(queryDelay, ct);
        }

        // 2. DB Write Steps (Save, Reserve, Release)
        public static async Task SimulateDbWriteOpenAndTransactionAsync(string source, CancellationToken ct = default)
        {
            Log.Info(source, "Opening connection...");
            await Task.Delay(200, ct);

            Log.Info(source, "Beginning transaction...");
            await Task.Delay(100, ct);

            Log.Info(source, "Inserting/Updating data...");
            await Task.Delay(400, ct);
            // ملاحظة: كتابة الملف (File Write) بتحصل هنا بالظبط بعد التأخير ده وقبل الـ Commit
        }

        public static async Task SimulateDbWriteCommitAsync(string source, CancellationToken ct = default)
        {
            Log.Info(source, "Committing transaction...");
            await Task.Delay(200, ct);
        }

        // 3. External API (Discount, Shipping)
        public static async Task SimulateExternalApiAsync(string source, CancellationToken ct = default)
        {
            Log.Info(source, "Performing DNS lookup and TLS handshake...");
            await Task.Delay(150, ct);

            Log.Info(source, "Sending request...");
            await Task.Delay(300, ct);

            Log.Info(source, "Waiting for API response...");
            int responseDelay = Random.Shared.Next(500, 1501); // 500 to 1500 ms
            await Task.Delay(responseDelay, ct);
        }

        // 4. Payment Gateway
        public static async Task SimulatePaymentGatewayAsync(string source, CancellationToken ct = default)
        {
            Log.Info(source, "Authorizing payment with bank...");
            await Task.Delay(800, ct);

            Log.Info(source, "Performing fraud check...");
            await Task.Delay(500, ct);

            Log.Info(source, "Capturing payment amount...");
            await Task.Delay(700, ct);
        }

        // 5. Email (SMTP)
        public static async Task SimulateEmailSmtpAsync(string source, CancellationToken ct = default)
        {
            Log.Info(source, "Rendering email template...");
            await Task.Delay(200, ct);

            Log.Info(source, "Connecting to SMTP server...");
            await Task.Delay(300, ct);

            Log.Info(source, "Sending email via SMTP...");
            await Task.Delay(500, ct);
        }
    }
}
