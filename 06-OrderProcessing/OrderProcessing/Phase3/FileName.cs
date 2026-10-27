using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public static class FireAndForgetDemo
    {
        public static async Task RunDemoAsync()
        {
            var orderRepo = new OrderRepository();
            var emailService = new EmailService();

            var dummyOrder = new Order { Id = 999, CustomerId = 1 };
            var dummyCustomer = new Customer { Id = 1, Email = "demo@example.com" };

            Console.WriteLine("\n==================================================");
            Console.WriteLine(" TASK 9.7: FIRE-AND-FORGET DEMO");
            Console.WriteLine("==================================================\n");

            // التجربة الأولى: النسخة المكسورة
            Console.WriteLine(">>> 1. Running Broken Version (Fire-and-Forget) <<<");
            await ProcessOrderBrokenAsync(orderRepo, emailService, dummyOrder, dummyCustomer);

            // انتظار بسيط عشان ندي فرصة لرسالة الإيميل تظهر في الكونسول قبل ما نبدأ التجربة التانية
            await Task.Delay(2000);

            // التجربة الثانية: النسخة المصححة
            Console.WriteLine("\n>>> 2. Running Fixed Version (Awaited) <<<");
            await ProcessOrderFixedAsync(orderRepo, emailService, dummyOrder, dummyCustomer);
        }

        // ==========================================
        // 1. النسخة المكسورة (Fire-and-Forget)
        // ==========================================
#pragma warning disable CS4014
        private static async Task ProcessOrderBrokenAsync(
            OrderRepository orderRepo,
            EmailService emailService,
            Order order,
            Customer customer)
        {
            await orderRepo.SaveAsync(order);

            // لاحظ عدم وجود await هنا
            emailService.SendConfirmationAsync(order, customer);

            Console.WriteLine("Order processing finished (Broken)");
        }
#pragma warning restore CS4014

        // ==========================================
        // 2. النسخة المصححة (Properly Awaited)
        // ==========================================
        private static async Task ProcessOrderFixedAsync(
            OrderRepository orderRepo,
            EmailService emailService,
            Order order,
            Customer customer)
        {
            await orderRepo.SaveAsync(order);

            // تم إضافة await هنا
            await emailService.SendConfirmationAsync(order, customer);

            Console.WriteLine("Order processing finished (Fixed)");
        }
    }
}
