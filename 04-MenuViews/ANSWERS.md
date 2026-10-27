Assignment 04 — Answers
Student name: Hossam Shawky Sharaf Eldein
Group / ID:   Group 3

### R2: Why the numbers disagree (Expected vs Actual)
The `Views++` operation is not atomic. Under the hood, it translates to three steps by the CPU:
1. Read the current value.
2. Increment the value.
3. Write the new value back.
When multiple threads execute this simultaneously, they read the same initial value, increment it, and overwrite each other's results. This "Lost Update" race condition causes the `Actual` count to be significantly lower than the `Expected` count.

### R3: Fix RegisterView
I chose `Interlocked.Increment(ref Views);` because it is the optimal tool for a single counter. It performs the read-modify-write sequence as a single, indivisible atomic operation at the hardware level, completely preventing the race condition without the overhead of a `lock`.

### R4: When would you protect this kind of update differently?
I would use a `lock` statement instead of `Interlocked` in cases such as:
1. **Conditions (Check-Then-Act):** If the update depends on a condition (e.g., `if (Views < Max)`), a `lock` is required to synchronize both the check and the update together.
2. **Multiple Variables:** If I need to mutate two or more shared variables simultaneously, since `Interlocked` can only perform atomic operations on a single variable at a time.