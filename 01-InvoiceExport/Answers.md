# Assignment 01 

## Answers

Student name: Hossam Shawky Sharaf Eldein
Group / ID: Group 3

Problems with the current design
- The system relies on synchronous programming for I/O operations (fetching orders from the database, reading customer files, and writing invoices).
- This causes "Thread Blocking" when handling a large number of orders. Threads are forced to wait idly for the I/O operations to finish instead of executing other work, which severely impacts performance and increases execution time.

What you changed
- Converted the system to use Asynchronous programming by replacing synchronous methods with `async` and `await` (e.g., `Task.Delay`, `File.ReadAllTextAsync`, and `File.WriteAllTextAsync`).
- Implemented concurrent task execution to process multiple orders at the same time while keeping the exact business flow (load order → read customer → write invoice).
- **Why:** To eliminate thread blocking so that threads remain available and responsive during I/O wait times. Executing tasks concurrently significantly reduced the total processing time.

### Console Output Before:
```text
Starting invoice export...
Customer files: D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\Data
Invoice output: D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices

Wrote D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1001.txt
Wrote D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1002.txt
Wrote D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1003.txt
Wrote D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1004.txt
Wrote D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1005.txt
Export finished in 5.9 s
```
### Console Output After:
```text
Starting invoice export...
Customer files: D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\Data
Invoice output: D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices

Wrote D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1004.txt
Wrote D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1001.txt
Wrote D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1002.txt
Wrote D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1003.txt
Wrote D:\Clone\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1005.txt
Export finished in 1.8 s
```