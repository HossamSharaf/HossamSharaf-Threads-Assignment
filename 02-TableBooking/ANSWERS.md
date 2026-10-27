Assignment 02 — Answers
Student name: Hossam Shawky Sharaf Eldein
Group / ID: Group 3

Problems with the current design
- The system suffers from a "Race Condition" due to multiple threads accessing shared variables (`TablesLeft` and `Booked`) concurrently without synchronization.
- Multiple threads pass the `if (TablesLeft <= 0)` check simultaneously during the `Thread.Sleep(20)` window before any thread decrements the table count.
- This simultaneous execution results in corrupted state data, specifically overselling, where the booked count exceeds the maximum limit and available tables drop into negative values.

What you changed
- Introduced a `private readonly object _locker = new object();` at the class level to serve as a synchronization lock.
- Wrapped the critical section (the condition check, the delay, and the variable mutations) entirely inside a `lock (_locker)` block.
- **Why:** This enforces mutual exclusion, ensuring that only one thread can access and modify the booking state at any given time. While threads must wait for the lock to be released, it strictly prevents the race condition and guarantees data accuracy.

### Console Output Before:
The execution output demonstrates the race condition, ending with a corrupted state where tables are oversold[cite: 3].
```text
Customer-22 booked a table. Left=-16
Customer-21 booked a table. Left=-9
Customer-30 booked a table. Left=-10
Customer-38 booked a table. Left=-1
Customer-33 booked a table. Left=-12
Customer-32 booked a table. Left=-1
Customer-39 booked a table. Left=-1
Customer-24 booked a table. Left=-13
Customer-19 booked a table. Left=-14

Booked=20, TablesLeft=-16
```

### Console Output After:
```text
Customer-03 booked a table. Left=4
Customer-01 booked a table. Left=3
Customer-02 booked a table. Left=2
Customer-04 booked a table. Left=1
Customer-05 booked a table. Left=0

Booked=5, TablesLeft=0
```