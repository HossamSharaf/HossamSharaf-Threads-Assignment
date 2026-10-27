Assignment 05 — Answers
Student name: Hossam Shawky Sharaf Eldein
Group / ID: Group 3

### R1: Describe the failures you see
When running the original code, two primary failures occur:
1. **InvalidOperationException:** Thrown inside `EmailWorker` because a standard `Queue<T>` is used. Multiple threads pass the `if (EmailJobs.Count > 0)` check simultaneously, but when they attempt to `Dequeue`, the queue might be empty, crashing the program.
2. **ArgumentException & Lost Updates:** Inside `RecordView`, multiple threads check `!Views.ContainsKey()` simultaneously and attempt to `Add` the same key, causing an exception. Furthermore, updating the dictionary via `Views[product] + 1` causes lost updates due to race conditions.

### R2: Fix the paths under concurrency
- I replaced the standard `Dictionary` with `ConcurrentDictionary` and the standard `Queue` with `ConcurrentQueue`.
- For view counting, I used `Views.AddOrUpdate(product, 1, (key, oldValue) => oldValue + 1);` which atomically adds or increments the view count.
- For the email job path, I replaced the unsafe `Dequeue` and `Count` checks with `EmailJobs.TryDequeue(out string result);`, which safely attempts to remove and return an object without throwing exceptions if the queue is empty.

### R3: Why making a collection safer does not automatically make the business rule safe
Using a thread-safe collection prevents internal memory corruption or internal structural crashes. However, it does not prevent logic flaws like the "Check-Then-Act" race condition. For example, using a `ConcurrentDictionary` but writing `if (!dict.ContainsKey(key)) dict.Add(key, val);` is still unsafe. Two threads can bypass the condition simultaneously, breaking the business rule. To make the business rule safe, developers must use the atomic operations inherently provided by the concurrent collection (like `AddOrUpdate` or `TryDequeue`) to ensure the check and the mutation happen as one indivisible step.