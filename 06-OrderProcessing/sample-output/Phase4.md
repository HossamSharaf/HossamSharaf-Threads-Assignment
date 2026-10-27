==================================================
    E-Commerce Order Processing - PHASE 10 DEMOS
==================================================


=== TASK 10.1: LIVE CHECKOUT PROGRESS ===

Starting all 20 orders at once... Monitoring live progress:

[ 1/20] Order #15 -> REJECTED (Customer #99 not found) after 0.2 s
[ 2/20] Order #18 -> REJECTED (Customer is inactive) after 0.2 s
[ 3/20] Order # 4 -> REJECTED (Insufficient stock for 'Mechanical Keyboard') after 0.3 s
[ 4/20] Order #12 -> REJECTED (Invalid coupon code 'SUMMER20') after 0.4 s
[ 5/20] Order # 8 -> PaymentFailed (Card declined by issuer) after 0.8 s
[ 6/20] Order # 5 -> PaymentFailed (Payment gateway timeout (3s)) after 3.0 s
[ 7/20] Order #13 -> Failed (Database save failed after payment; payment TXN-8A4F... refunded) after 1.2 s
[ 8/20] Order # 2 -> Completed after 1.1 s
[ 9/20] Order # 1 -> Completed after 1.3 s
[10/20] Order # 3 -> Completed after 1.4 s
[11/20] Order # 6 -> Completed after 1.1 s
[12/20] Order # 7 -> Completed after 1.2 s
[13/20] Order #10 -> Completed after 1.3 s
[14/20] Order #14 -> Completed after 1.1 s
[15/20] Order #16 -> Completed after 1.0 s
[16/20] Order #17 -> Completed after 1.4 s
[17/20] Order #19 -> Completed after 1.2 s
[18/20] Order #20 -> Completed after 1.1 s
[19/20] Order # 9 -> REJECTED (Insufficient stock for 'Wireless Mouse') after 0.3 s
[20/20] Order #11 -> REJECTED (Insufficient stock for 'Gaming Monitor') after 0.3 s

================ TASK 10.1 SUMMARY =================
Completed:     11 (Expected 11)
Rejected:      6  (Expected 6)
PaymentFailed: 2  (Expected 2)
Failed:        1  (Expected 1)
===================================================


=== TASK 10.2: CUSTOMER DASHBOARD DEMO ===

--- Testing Customer #3 (Expected Success) ---
Customer: #3 Omar Hassan
Orders count: 2
  - Order #3 (Completed)
  - Order #13 (Failed)
Payments count: 2
  - Payment for Order #3 (Captured)
  - Payment for Order #13 (Refunded)
Emails count: 1
  - Email for Order #3
Dashboard for Customer #3 loaded successfully in 165 ms.

--- Testing Customer #99 (Expected Multiple Failures) ---
Dashboard loading failed for Customer #99. Errors: Customer #99 not found; Payments store timeout
Captured multiple failures successfully (R4):
  - Customer #99 not found
  - Payments store timeout
==================================================


=== TASK 10.3: FASTEST SHIPPING QUOTE DEMO ===

Order  | Winning Carrier | Price    | Time (ms)  | Cancelled Carriers
----------------------------------------------------------------------
#1     | CairoExpress    | 237.50   | 465        | FastShip, NileCourier
#2     | FastShip        | 250.00   | 720        | CairoExpress, NileCourier
#3     | CairoExpress    | 237.50   | 480        | FastShip, NileCourier
#4     | FastShip        | 250.00   | 650        | CairoExpress, NileCourier
#5     | CairoExpress    | 237.50   | 410        | FastShip, NileCourier

--- Testing R5: All Carriers Fail for Order #6 ---
Successfully caught expected exception (R5): All carriers failed
==================================================


=== TASK 10.4: FRAUD SCORING & EMAIL RE-SEND ===

[Part A] Running CPU-bound Fraud Scoring for 10,000 orders...

--- Part A Results ---
Flagged Count:             1005 (Expected 1005)
Sequential Time:           285 ms
Parallel Time:             78 ms
Speed-up:                  3.65x
Distinct Threads Used:     8
Environment.ProcessorCount:8
First 5 Flagged Ids:       4, 8, 12, 29, 33 (Expected 4, 8, 12, 29, 33)

[Part B] Running I/O-bound Email Re-send (30 test emails, 1001-1030)...

--- Part B Results ---
Total Time:                8120 ms (~8.1 s)
Highest Concurrent Sends:  4 (Expected exactly 4)
==================================================


==================================================
 All Phase 10 tasks completed successfully!
==================================================

Press any key to exit...