using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public static class DataFiles
    {
        
        public static string DataFolder => Path.Combine(AppContext.BaseDirectory, "Data");

        public static string InvoicesFolder => Path.Combine(DataFolder, "invoices");
        public static string ReportsFolder => Path.Combine(DataFolder, "reports");

        public const string CustomersFile = "customers.json";
        public const string ProductsFile = "products.json";
        public const string CouponsFile = "coupons.json";
        public const string IncomingOrdersFile = "incoming_orders.json";
        public const string OrdersFile = "orders.json";
        public const string PaymentsFile = "payments.json";
        public const string EmailsFile = "emails.json";
        public const string EmailRetryFile = "email_retry.json";

        public static string GetFilePath(string fileName)
        {
            return Path.Combine(DataFolder, fileName);
        }
    }
}
