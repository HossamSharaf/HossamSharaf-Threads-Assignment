using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public class SyncCustomerRepository
    {
        private const string Source = "SyncCustomerRepository";

        public Customer? GetById(int id)
        {
            Log.Info(Source, "Opening connection...");
            Thread.Sleep(200);

            Log.Info(Source, $"Executing query for Customer #{id}...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var customers = JsonFileStore.Read<Customer>(DataFiles.GetFilePath(DataFiles.CustomersFile));
            return customers.FirstOrDefault(c => c.Id == id);
        }
    }

    // ================= 2. Sync Product Repository =================
    public class SyncProductRepository
    {
        private const string Source = "SyncProductRepository";

        public Product? GetById(int id)
        {
            Log.Info(Source, "Opening connection...");
            Thread.Sleep(200);

            Log.Info(Source, $"Executing query for Product #{id}...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var products = JsonFileStore.Read<Product>(DataFiles.GetFilePath(DataFiles.ProductsFile));
            return products.FirstOrDefault(p => p.Id == id);
        }
    }

    // ================= 3. Sync Coupon Repository =================
    public class SyncCouponRepository
    {
        private const string Source = "SyncCouponRepository";

        public Coupon? GetByCode(string code)
        {
            Log.Info(Source, "Opening connection...");
            Thread.Sleep(200);

            Log.Info(Source, $"Executing query for Coupon '{code}'...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var coupons = JsonFileStore.Read<Coupon>(DataFiles.GetFilePath(DataFiles.CouponsFile));
            return coupons.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
        }
    }
}
