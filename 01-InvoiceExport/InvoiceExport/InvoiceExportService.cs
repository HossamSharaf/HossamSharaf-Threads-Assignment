using System.Diagnostics;

namespace InvoiceExport;

public sealed class InvoiceExportService
{
    readonly OrderDatabase _db;
    readonly CustomerFileStore _customers;
    readonly InvoiceFileWriter _invoices;

    public InvoiceExportService(OrderDatabase db, CustomerFileStore customers, InvoiceFileWriter invoices)
    {
        _db = db;
        _customers = customers;
        _invoices = invoices;
    }

    public async Task ExportAllPending()
    {
        var watch = Stopwatch.StartNew();
        List<Order> orders = await _db.GetPendingOrders();

        // 1. بنعمل لستة فاضية نشيل فيها المهام (Tasks)
        List<Task> exportTasks = new List<Task>();

        foreach (Order order in orders)
        {
            // 2. بنضيف كل أوردر كـ Task مستقل في اللستة من غير ما نعمله await هنا
            exportTasks.Add(ProcessSingleOrderAsync(order));
        }

        // 3. بنستناهم كلهم يخلصوا مع بعض
        await Task.WhenAll(exportTasks);

        Console.WriteLine($"Export finished in {watch.Elapsed.TotalSeconds:F1} s");
    }

    // دالة مساعدة بتعمل شغل الأوردر الواحد
    private async Task ProcessSingleOrderAsync(Order order)
    {
        Order fresh = await _db.GetOrderById(order.Id)
            ?? throw new InvalidOperationException($"Order {order.Id} missing");

        CustomerProfile customer = await _customers.ReadProfile(fresh.CustomerId);
        string path = await _invoices.WriteInvoice(fresh, customer);
        Console.WriteLine($"Wrote {path}");
    }
}
