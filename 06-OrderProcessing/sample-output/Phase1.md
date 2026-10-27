[Seeder] customers.json -> 10 records.
[Seeder] products.json -> 8 records.
[Seeder] coupons.json -> 4 records.
[Seeder] incoming_orders.json -> 20 records.
[Seeder] orders.json -> 0 records.
[Seeder] payments.json -> 0 records.
[Seeder] emails.json -> 0 records.
[Seeder] email_retry.json -> 0 records.

===== Validation Summary =====
Total Orders: 20
Validated: 15
Rejected: 5
- Order #4: Customer is inactive
- Order #9: Insufficient stock for 'Wireless Mouse'
- Order #12: Coupon 'OLD20' has expired
- Order #15: Customer #99 not found
- Order #18: Coupon 'FAKE50' does not exist
Sync Time: 35.0 seconds
Async Time: 53.5 seconds
orders.json now contains 20 records.