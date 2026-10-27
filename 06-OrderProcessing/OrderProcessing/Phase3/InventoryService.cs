using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public class InventoryService
    {
        private readonly ProductRepository _productRepo;

        public InventoryService(ProductRepository productRepo)
        {
            _productRepo = productRepo;
        }

        // تم التعديل لإرجاع Tuple يحتوي على حالة النجاح واسم المنتج الذي تسبب في الفشل
        public async Task<(bool Success, string FailedProductName)> ReserveStockAsync(Order order, CancellationToken ct = default)
        {
            var reservedItems = new List<(int ProductId, int Qty)>();

            foreach (var item in order.Items)
            {
                var product = await _productRepo.GetByIdAsync(item.ProductId, ct);
                string productName = product?.Name ?? $"Product #{item.ProductId}";

                bool reserved = await _productRepo.ReserveStockAsync(item.ProductId, item.Quantity, ct);
                if (!reserved)
                {
                    // Rollback
                    await ReleaseReservedItemsAsync(reservedItems, ct);
                    return (false, productName);
                }
                reservedItems.Add((item.ProductId, item.Quantity));
            }
            return (true, string.Empty);
        }

        public async Task ReleaseStockAsync(Order order, CancellationToken ct = default)
        {
            foreach (var item in order.Items)
            {
                await _productRepo.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
            }
        }

        private async Task ReleaseReservedItemsAsync(List<(int ProductId, int Qty)> items, CancellationToken ct)
        {
            foreach (var (productId, qty) in items)
            {
                await _productRepo.ReleaseStockAsync(productId, qty, ct);
            }
        }
    }
}
