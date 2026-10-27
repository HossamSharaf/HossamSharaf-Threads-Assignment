==================================================
    E-Commerce Order Processing - PHASE 3
==================================================

=== PHASE 3: PRODUCTION-READY CHECKOUT ===
[System] Seeding fresh data for testing...

Starting 20 orders concurrently...

[18:45:01.120] [Thread  4] Order # 1 -> Step 1/6 Customer ... OK in 15 ms
[18:45:01.125] [Thread  7] Order # 5 -> Step 1/6 Customer ... OK in 20 ms
[18:45:01.130] [Thread 11] Order #15 -> Step 1/6 Customer ... REJECTED (Not Found) in 25 ms
[18:45:01.131] [Thread 11] Order #18 -> Step 1/6 Customer ... REJECTED (Inactive) in 26 ms
[18:45:01.140] [Thread  4] Order # 1 -> Step 2/6 Stock    ... OK in 100 ms
[18:45:01.145] [Thread  9] Order # 4 -> Step 2/6 Stock    ... REJECTED (Insufficient) in 95 ms
[18:45:01.240] [Thread  4] Order # 1 -> Step 3/6 Pricing  ... OK in 205 ms
[18:45:01.260] [Thread 14] Order #12 -> Step 3/6 Pricing  ... REJECTED (Invalid Coupon) in 220 ms
[18:45:01.850] [Thread  4] Order # 1 -> Step 4/6 Payment  ... OK (TXN-3F9A1C07B2E4) in 610 ms
[18:45:01.870] [Thread 12] Order # 8 -> Step 4/6 Payment  ... DECLINED, released in 625 ms
[18:45:02.100] [Thread  4] Order # 1 -> Step 5/6 Save     ... OK in 250 ms
[18:45:02.300] [Thread 15] Order #13 -> Step 5/6 Save     ... FAILED, compensated in 450 ms
[18:45:02.400] [Thread 21] EmailService -> Email attempt 1 failed for Order #3: SmtpUnavailableException
[18:45:02.600] [Thread  4] Order # 1 -> Step 6/6 Email    ... OK in 500 ms
[18:45:03.100] [Thread 21] EmailService -> Email attempt 2 failed for Order #3: SmtpUnavailableException
[18:45:04.125] [Thread  7] Order # 5 -> Step 4/6 Payment  ... TIMEOUT, released in 3001 ms
[18:45:04.600] [Thread 21] EmailService -> Confirmation email sent to cus3@test.com for Order #3 (Attempt 3).
[18:45:06.100] [Thread 18] EmailService -> Email attempt 1 failed for Order #17: SmtpUnavailableException
[18:45:06.600] [Thread 18] EmailService -> Email attempt 2 failed for Order #17: SmtpUnavailableException
[18:45:07.600] [Thread 18] EmailService -> Email attempt 3 failed for Order #17: SmtpUnavailableException
[18:45:09.600] [Thread 18] EmailService -> Email attempt 4 failed for Order #17: SmtpUnavailableException
[18:45:09.610] [Thread 18] EmailService -> Max retries reached for Order #17. Saved to Dead-Letter Queue (email_retry.json).
[18:45:09.615] [Thread 18] EmailService -> Email for Order #17 queued for retry later

========================= FINAL ORDERS SUMMARY =========================
Order  | Status         | Total     | Reason
-------------------------------------------------------------------------------------
1      | Completed      | 1500.00   | 
2      | Completed      | 350.50    | 
3      | Completed      | 900.00    | 
4      | Rejected       | -         | Insufficient stock for 'Mechanical Keyboard'
5      | PaymentFailed  | -         | Payment gateway timeout (3s)
6      | Completed      | 120.00    | 
7      | Completed      | 450.00    | 
8      | PaymentFailed  | -         | Card declined by issuer
9      | Rejected       | -         | Insufficient stock for 'Wireless Mouse'
10     | Completed      | 800.00    | 
11     | Rejected       | -         | Insufficient stock for 'Gaming Monitor'
12     | Rejected       | -         | Expired coupon code 'SUMMER20'
13     | Failed         | -         | Database save failed after payment; payment TXN-...
14     | Completed      | 250.00    | 
15     | Rejected       | -         | Customer #99 not found
16     | Completed      | 60.00     | 
17     | Completed      | 770.00    | 
18     | Rejected       | -         | Customer is inactive
19     | Completed      | 1150.00   | 
20     | Completed      | 85.00     | 
========================================================================

================ TASK 9.4 VERIFICATION ================
[orders.json] Total: 20 (Expected 20)
  - Completed: 11 (Expected 11)
  - Rejected: 6 (Expected 6)
  - PaymentFailed: 2 (Expected 2)
  - Failed: 1 (Expected 1)

[payments.json] Total: 13 (Expected 13)
  - Captured: 11 (Expected 11)
  - Declined: 1 (Expected 1)
  - Refunded: 1 (Expected 1)
  - Order #5 Exists? False (Expected False)

[emails.json] Total: 10 (Expected 10)
  - Order #3 Attempts: 3 (Expected 3)

[email_retry.json] Total: 1 (Expected 1)
  - Order #17 Attempts: 4 (Expected 4)
  - LastError Set? True

[products.json]
  - Mechanical Keyboard (ID 4) Stock: 0 (Expected 0)
=======================================================