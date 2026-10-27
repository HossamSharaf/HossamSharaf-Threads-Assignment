using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public static class DataSeeder
    {
        public static async Task ResetAsync()
        {
            // 1. Delete and Recreate Data Folder
            if (Directory.Exists(DataFiles.DataFolder))
            {
                Directory.Delete(DataFiles.DataFolder, true);
            }
            Directory.CreateDirectory(DataFiles.DataFolder);
            Directory.CreateDirectory(DataFiles.InvoicesFolder);
            Directory.CreateDirectory(DataFiles.ReportsFolder);

            
            var customersSeed = new List<object>(); 
            var productsSeed = new List<object>();  
            var couponsSeed = new List<object>();
            var incomingOrdersSeed = new List<object>();

            // 3. Write data and print logs
            await SeedFileAsync(DataFiles.CustomersFile, customersSeed);
            await SeedFileAsync(DataFiles.ProductsFile, productsSeed);
            await SeedFileAsync(DataFiles.CouponsFile, couponsSeed);
            await SeedFileAsync(DataFiles.IncomingOrdersFile, incomingOrdersSeed);

            // Empty collections for the rest
            await SeedFileAsync<object>(DataFiles.OrdersFile, new List<object>());
            await SeedFileAsync<object>(DataFiles.PaymentsFile, new List<object>());
            await SeedFileAsync<object>(DataFiles.EmailsFile, new List<object>());
            await SeedFileAsync<object>(DataFiles.EmailRetryFile, new List<object>());
        }

        // Helper method to write and print the exact format requested
        private static async Task SeedFileAsync<T>(string fileName, List<T> data)
        {
            string fullPath = DataFiles.GetFilePath(fileName);
            await JsonFileStore.WriteAsync(fullPath, data);

            // Printing the format required: [Seeder] customers.json -> 10 records.
            Console.WriteLine($"[Seeder] {fileName} -> {data.Count} records.");
        }
    }
}

