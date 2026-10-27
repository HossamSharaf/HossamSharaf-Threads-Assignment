using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public class SyncOrderProcessor
    {
        private const string Source = "SyncOrderProcessor";
        private readonly ProductRepository _productRepo;
        private readonly OrderRepository _orderRepo;

        public SyncOrderProcessor(ProductRepository productRepo, OrderRepository orderRepo)
        {
            _productRepo = productRepo;
            _orderRepo = orderRepo;
        }

        public bool ProcessOrderSync(Order order)
        {
            // Simulate blocking DB/API delays with Thread.Sleep
            Thread.Sleep(100); // Customer fetch delay

            // Stock Reservation
            foreach (var item in order.Items)
            {
                Thread.Sleep(150); // DB read delay
                if (item.Quantity > 5) // Domain rule simulation for orders #4 and #9 failure
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = "Insufficient stock";
                    _orderRepo.SaveAsync(order).GetAwaiter().GetResult();
                    return false;
                }
            }

            // Pricing & External APIs (Sequential & Blocking)
            Thread.Sleep(300); // Pricing API delay
            Thread.Sleep(400); // Shipping API delay

            // Payment
            Thread.Sleep(500); // Payment Gateway delay

            order.Status = OrderStatus.Completed;
            order.FailureReason = null;

            // Sync wait on save
            _orderRepo.SaveAsync(order).GetAwaiter().GetResult();

            return true;
        }
    }
}
