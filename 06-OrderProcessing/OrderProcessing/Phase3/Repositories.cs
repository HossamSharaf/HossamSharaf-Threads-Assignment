using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public class CustomerRepository
    {
        private const string Source = "CustomerRepository";

        public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            await SimulatedLatency.SimulateDbQueryAsync(Source, $"Customer #{id}", ct);

            var customers = await JsonFileStore.ReadAsync<Customer>(DataFiles.GetFilePath(DataFiles.CustomersFile), ct);
            return customers.FirstOrDefault(c => c.Id == id);
        }
    }

    // ================= 2. Product Repository =================
    public class ProductRepository
    {
        private const string Source = "ProductRepository";

        // قفل برمجي لحماية المخزون ومنع الـ Race Condition (Task 8.3)
        private static readonly SemaphoreSlim _businessLock = new SemaphoreSlim(1, 1);

        public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            await SimulatedLatency.SimulateDbQueryAsync(Source, $"Product #{id}", ct);

            var products = await JsonFileStore.ReadAsync<Product>(DataFiles.GetFilePath(DataFiles.ProductsFile), ct);
            return products.FirstOrDefault(p => p.Id == id);
        }

        public async Task<bool> ReserveStockAsync(int productId, int qty, CancellationToken ct = default)
        {
            await _businessLock.WaitAsync(ct);
            try
            {
                // 1. فتح الاتصال وبدء الـ Transaction والتأخير الخاص بـ Insert/Update
                await SimulatedLatency.SimulateDbWriteOpenAndTransactionAsync(Source, ct);

                // 2. قراءة الملف
                var filePath = DataFiles.GetFilePath(DataFiles.ProductsFile);
                var products = await JsonFileStore.ReadAsync<Product>(filePath, ct);
                var product = products.FirstOrDefault(p => p.Id == productId);

                // 3. التحقق من المخزون
                if (product == null || product.StockQuantity < qty)
                {
                    Log.Error(Source, $"Failed to reserve {qty} units of Product #{productId}. Insufficient stock or missing.");
                    return false;
                }

                // 4. خصم الكمية وحفظ الملف
                product.StockQuantity -= qty;
                await JsonFileStore.WriteAsync(filePath, products, ct);

                // 5. إتمام الـ Transaction
                await SimulatedLatency.SimulateDbWriteCommitAsync(Source, ct);
                Log.Success(Source, $"Reserved {qty} units of Product #{productId}. Remaining: {product.StockQuantity}");

                return true;
            }
            finally
            {
                _businessLock.Release();
            }
        }

        public async Task ReleaseStockAsync(int productId, int qty, CancellationToken ct = default)
        {
            await _businessLock.WaitAsync(ct);
            try
            {
                await SimulatedLatency.SimulateDbWriteOpenAndTransactionAsync(Source, ct);

                var filePath = DataFiles.GetFilePath(DataFiles.ProductsFile);
                var products = await JsonFileStore.ReadAsync<Product>(filePath, ct);
                var product = products.FirstOrDefault(p => p.Id == productId);

                if (product != null)
                {
                    product.StockQuantity += qty;
                    await JsonFileStore.WriteAsync(filePath, products, ct);
                }

                await SimulatedLatency.SimulateDbWriteCommitAsync(Source, ct);
                Log.Info(Source, $"Released {qty} units back to Product #{productId}.");
            }
            finally
            {
                _businessLock.Release();
            }
        }
    }

    // ================= 3. Coupon Repository =================
    public class CouponRepository
    {
        private const string Source = "CouponRepository";

        public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            await SimulatedLatency.SimulateDbQueryAsync(Source, $"Coupon '{code}'", ct);

            var coupons = await JsonFileStore.ReadAsync<Coupon>(DataFiles.GetFilePath(DataFiles.CouponsFile), ct);

            // "case-insensitive match" - تجاهل حالة الأحرف
            return coupons.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
        }
    }

    // ================= 4. Order Repository =================
    
        public class OrderRepository
        {
            private const string Source = "OrderRepository";
            private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

            public async Task SaveAsync(Order order, CancellationToken ct = default)
            {
                await _lock.WaitAsync(ct);
                try
                {
                    await SimulatedLatency.SimulateDbWriteOpenAndTransactionAsync(Source, ct);

                    var filePath = DataFiles.GetFilePath(DataFiles.OrdersFile);
                    var orders = await JsonFileStore.ReadAsync<Order>(filePath, ct);

                    int index = orders.FindIndex(o => o.Id == order.Id);
                    if (index >= 0)
                    {
                        orders[index] = order; // Update
                    }
                    else
                    {
                        orders.Add(order); // Insert
                    }

                    await JsonFileStore.WriteAsync(filePath, orders, ct);
                    await SimulatedLatency.SimulateDbWriteCommitAsync(Source, ct);
                }
                finally
                {
                    _lock.Release();
                }
            }

            public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
            {
                await SimulatedLatency.SimulateDbQueryAsync(Source, "All Orders", ct);
                return await JsonFileStore.ReadAsync<Order>(DataFiles.GetFilePath(DataFiles.OrdersFile), ct);
            }
        }
    

    // ================= 5. Incoming Order Repository =================
    public class IncomingOrderRepository
    {
        private const string Source = "IncomingOrderRepository";

        public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
        {
            await SimulatedLatency.SimulateDbQueryAsync(Source, "All Incoming Orders", ct);

            return await JsonFileStore.ReadAsync<Order>(DataFiles.GetFilePath(DataFiles.IncomingOrdersFile), ct);
        }
    }
}
