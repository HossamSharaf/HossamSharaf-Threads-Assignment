using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public class InvoiceGenerator
    {
        private const string Source = "InvoiceGenerator";

        public async Task GenerateAsync(Order order, CancellationToken ct = default)
        {
            Log.Info(Source, $"Generating invoice PDF/text for Order #{order.Id}...");

            // محاكاة عمل معالجة ثقيلة على المعالج (CPU-bound) مثل ضغط وتنسيق الـ PDF
            await Task.Run(() =>
            {
                long dummySum = 0;
                for (int i = 0; i < 20_000_000; i++)
                {
                    dummySum += i;
                }
            }, ct);

            var dir = Path.Combine("Data", "invoices");
            Directory.CreateDirectory(dir);

            var filePath = Path.Combine(dir, $"invoice_{order.Id}.txt");

            var sb = new StringBuilder();
            sb.AppendLine($"=== INVOICE FOR ORDER #{order.Id} ===");
            sb.AppendLine($"Customer ID: {order.CustomerId}");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("-----------------------------------------");
            foreach (var item in order.Items)
            {
                sb.AppendLine($"Product #{item.ProductId} | Qty: {item.Quantity} | Unit Price: {item.UnitPrice:C}");
            }
            sb.AppendLine("-----------------------------------------");
            sb.AppendLine($"SubTotal:    {order.SubTotal:C}");
            sb.AppendLine($"Discount:    -{order.Discount:C}");
            sb.AppendLine($"Shipping:    {order.ShippingCost:C}");
            sb.AppendLine($"Total Amount: {order.TotalAmount:C}");
            sb.AppendLine("=========================================");

            await File.WriteAllTextAsync(filePath, sb.ToString(), ct);
            Log.Success(Source, $"Invoice generated successfully for Order #{order.Id}.");
        }
    }
}
