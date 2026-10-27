using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public static class RaceConditionDemo
    {
        public static async Task RunAsync(CancellationToken ct = default)
        {
            var productRepo = new ProductRepository();
            int keyboardId = 4; // Mechanical Keyboard (Product #4)

            Console.WriteLine("\n==================================================");
            Console.WriteLine(" RACE CONDITION DEMO: THE LAST MECHANICAL KEYBOARD");
            Console.WriteLine("==================================================\n");

            // -------------------------------------------------------------------
            // 1. Unsafe Demo (Both orders get the keyboard - Overselling bug)
            // -------------------------------------------------------------------
            Console.WriteLine(">>> 1. RUNNING UNSAFE RESERVATION (Expecting BOTH to succeed) <<<");
            await ResetKeyboardStockToOneAsync(keyboardId, ct);

            // بدء الحجز للطلبين في نفس الوقت بدون حماية
            var unsafeOrder10 = productRepo.ReserveStockAsync(keyboardId, 1, ct);
            var unsafeOrder11 = productRepo.ReserveStockAsync(keyboardId, 1, ct);

            var unsafeResults = await Task.WhenAll(unsafeOrder10, unsafeOrder11);
            int unsafeStock = await GetCurrentStockAsync(keyboardId, ct);

            Console.WriteLine($"[UNSAFE RESULTS]");
            Console.WriteLine($"- Order #10 Result: {unsafeResults[0]}");
            Console.WriteLine($"- Order #11 Result: {unsafeResults[1]}");
            Console.WriteLine($"- Remaining Stock in File: {unsafeStock}");
            if (unsafeResults[0] && unsafeResults[1])
            {
                Console.WriteLine("🚨 BUG PROVEN: Both orders got the last keyboard (Overselling occurred)!\n");
            }

            // -------------------------------------------------------------------
            // 2. Safe Demo (One true, one false, stock 0)
            // -------------------------------------------------------------------
            Console.WriteLine(">>> 2. RUNNING SAFE RESERVATION (Expecting ONE true, ONE false) <<<");
            await ResetKeyboardStockToOneAsync(keyboardId, ct);

            // بدء الحجز للطلبين باستخدام القفل الآمن
            var safeOrder10 = productRepo.ReserveStockAsync(keyboardId, 1, ct);
            var safeOrder11 = productRepo.ReserveStockAsync(keyboardId, 1, ct);

            var safeResults = await Task.WhenAll(safeOrder10, safeOrder11);
            int safeStock = await GetCurrentStockAsync(keyboardId, ct);

            Console.WriteLine($"[SAFE RESULTS]");
            Console.WriteLine($"- Order #10 Result: {safeResults[0]}");
            Console.WriteLine($"- Order #11 Result: {safeResults[1]}");
            Console.WriteLine($"- Remaining Stock in File: {safeStock}");
            if (safeResults[0] != safeResults[1] && safeStock == 0)
            {
                Console.WriteLine("✅ FIXED: Exactly one order succeeded, one failed, and stock is 0.\n");
            }
        }

        private static async Task ResetKeyboardStockToOneAsync(int productId, CancellationToken ct)
        {
            await DataSeeder.ResetAsync();
            var filePath = DataFiles.GetFilePath(DataFiles.ProductsFile);
            var products = await JsonFileStore.ReadAsync<Product>(filePath, ct);
            var keyboard = products.First(p => p.Id == productId);

            keyboard.StockQuantity = 1; // تعيين المخزون ليصبح 1 بالضبط
            await JsonFileStore.WriteAsync(filePath, products, ct);

            Console.WriteLine($"[Setup] Product #{productId} ({keyboard.Name}) stock reset to 1.");
        }

        private static async Task<int> GetCurrentStockAsync(int productId, CancellationToken ct)
        {
            var products = await JsonFileStore.ReadAsync<Product>(DataFiles.GetFilePath(DataFiles.ProductsFile), ct);
            return products.First(p => p.Id == productId).StockQuantity;
        }
    }
}
