using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public static class DataSeeder
    {
        public static async Task ResetAsync()
        {
            if (Directory.Exists(DataFiles.DataFolder))
            {
                Directory.Delete(DataFiles.DataFolder, true); 
            }

            Directory.CreateDirectory(DataFiles.DataFolder);
            Directory.CreateDirectory(DataFiles.InvoicesFolder);
            Directory.CreateDirectory(DataFiles.ReportsFolder);

            string emptyCollectionJson = "[]";

            string[] filesToInitialize =
            {
                DataFiles.CustomersFile,
                DataFiles.ProductsFile,
                DataFiles.CouponsFile,
                DataFiles.IncomingOrdersFile,
                DataFiles.OrdersFile,
                DataFiles.PaymentsFile,
                DataFiles.EmailsFile,
                DataFiles.EmailRetryFile
            };

            foreach (var file in filesToInitialize)
            {
                string fullPath = DataFiles.GetFilePath(file);
                await File.WriteAllTextAsync(fullPath, emptyCollectionJson);
            }

             
        }
    }
}

