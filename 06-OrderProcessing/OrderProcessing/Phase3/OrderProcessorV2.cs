using ConsoleApp1.NewFolder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ConsoleApp1
{
    public class OrderProcessorV2
    {
        private readonly CustomerService _customerService;
        private readonly InventoryService _inventoryService;
        private readonly PricingService _pricingService;
        private readonly IPaymentGateway _paymentGateway;
        private readonly OrderRepository _orderRepo;
        private readonly IEmailService _emailService;

        public OrderProcessorV2(
            CustomerService customerService,
            InventoryService inventoryService,
            PricingService pricingService,
            IPaymentGateway paymentGateway,
            OrderRepository orderRepo,
            IEmailService emailService)
        {
            _customerService = customerService;
            _inventoryService = inventoryService;
            _pricingService = pricingService;
            _paymentGateway = paymentGateway;
            _orderRepo = orderRepo;
            _emailService = emailService;
        }

        public async Task<bool> ProcessOrderAsync(Order order, CancellationToken ct = default)
        {
            var stepSw = new Stopwatch();
            Customer? customer = null;

            // ================== STEP 1: Customer ==================
            stepSw.Restart();
            try
            {
                customer = await _customerService.GetCustomerAsync(order.CustomerId, ct);
                if (!customer.IsActive)
                {
                    await RejectOrderAsync(order, "Customer is inactive", ct);
                    LogStep(order.Id, 1, "Customer", "REJECTED (Inactive)", stepSw.ElapsedMilliseconds);
                    return false;
                }
            }
            catch (CustomerNotFoundException ex)
            {
                await RejectOrderAsync(order, ex.Message, ct);
                LogStep(order.Id, 1, "Customer", "REJECTED (Not Found)", stepSw.ElapsedMilliseconds);
                return false;
            }
            LogStep(order.Id, 1, "Customer", "OK", stepSw.ElapsedMilliseconds);

            // ================== STEP 2: Stock ==================
            stepSw.Restart();
            var (stockOk, failName) = await _inventoryService.ReserveStockAsync(order, ct);
            if (!stockOk)
            {
                await RejectOrderAsync(order, $"Insufficient stock for '{failName}'", ct);
                LogStep(order.Id, 2, "Stock", "REJECTED (Insufficient)", stepSw.ElapsedMilliseconds);
                return false;
            }
            LogStep(order.Id, 2, "Stock", "OK", stepSw.ElapsedMilliseconds);

            // ================== STEP 3: Pricing ==================
            stepSw.Restart();
            try
            {
                await _pricingService.CalculateTotalAsync(order, ct);
            }
            catch (InvalidCouponException ex)
            {
                await _inventoryService.ReleaseStockAsync(order, ct);
                await RejectOrderAsync(order, ex.Message, ct);
                LogStep(order.Id, 3, "Pricing", "REJECTED (Invalid Coupon)", stepSw.ElapsedMilliseconds);
                return false;
            }
            LogStep(order.Id, 3, "Pricing", "OK", stepSw.ElapsedMilliseconds);

            // ================== STEP 4: Payment ==================
            stepSw.Restart();
            PaymentRecord payment = null;

            // ربط الـ Cancellation الأساسي مع TimeOut مدته 3 ثواني
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(3000);

            try
            {
                payment = await _paymentGateway.ProcessPaymentAsync(order.Id, order.TotalAmount, timeoutCts.Token);
                if (!payment.Success)
                {
                    await _inventoryService.ReleaseStockAsync(order, ct);
                    await FailOrderAsync(order, OrderStatus.PaymentFailed, "Card declined by issuer", ct);
                    LogStep(order.Id, 4, "Payment", "DECLINED, released", stepSw.ElapsedMilliseconds);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                // إذا كان الإلغاء بسبب الـ Timeout (3 ثواني) وليس بسبب المستخدم
                if (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
                {
                    await _inventoryService.ReleaseStockAsync(order, ct);
                    await FailOrderAsync(order, OrderStatus.PaymentFailed, "Payment gateway timeout (3s)", ct);
                    LogStep(order.Id, 4, "Payment", "TIMEOUT, released", stepSw.ElapsedMilliseconds);
                    return false;
                }

                // إلغاء كلي للنظام (Ctrl+C)
                throw;
            }
            LogStep(order.Id, 4, "Payment", $"OK ({payment.TransactionId})", stepSw.ElapsedMilliseconds);

            // ================== STEP 5: Save ==================
            stepSw.Restart();
            order.TransactionId = payment.TransactionId;
            order.Status = OrderStatus.Completed;
            order.FailureReason = null;

            try
            {
                await _orderRepo.SaveAsync(order, ct);
                LogStep(order.Id, 5, "Save", "OK", stepSw.ElapsedMilliseconds);
            }
            catch (DatabaseWriteException)
            {
                // Compensation: استرجاع الأموال وفك حجز المخزون
                await _paymentGateway.RefundAsync(payment.TransactionId, ct);
                await _inventoryService.ReleaseStockAsync(order, ct);

                await FailOrderAsync(order, OrderStatus.Failed, $"Database save failed after payment; payment {payment.TransactionId} refunded", ct);
                LogStep(order.Id, 5, "Save", "FAILED, compensated", stepSw.ElapsedMilliseconds);
                return false;
            }

            // ================== STEP 6: Email ==================
            stepSw.Restart();
            try
            {
                // Task 9.5 will implement Retry logic inside this or the service.
                // For now, it just attempts to send and does not fail the order status if it crashes.
                await _emailService.SendConfirmationAsync(order, customer, ct);
                LogStep(order.Id, 6, "Email", "OK", stepSw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                LogStep(order.Id, 6, "Email", $"FAILED ({ex.GetType().Name})", stepSw.ElapsedMilliseconds);
            }

            return true;
        }

        // ================== Helper Methods ==================
        private async Task RejectOrderAsync(Order order, string reason, CancellationToken ct)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = reason;
            await _orderRepo.SaveAsync(order, ct);
        }

        private async Task FailOrderAsync(Order order, OrderStatus status, string reason, CancellationToken ct)
        {
            order.Status = status;
            order.FailureReason = reason;
            await _orderRepo.SaveAsync(order, ct);
        }

        private void LogStep(int orderId, int stepNum, string stepName, string resultMessage, long ms)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            int threadId = Environment.CurrentManagedThreadId;
            Console.WriteLine($"[{time}] [Thread {threadId,2}] Order #{orderId,2} -> Step {stepNum}/6 {stepName,-8} ... {resultMessage} in {ms} ms");
        }
    }
}
