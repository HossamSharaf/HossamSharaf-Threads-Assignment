Assignment 03 — Answers
Student name: Hossam Shawky Sharaf Eldein
Group / ID: Group 3

### Q1: What is System.Threading.Monitor used for?
`System.Threading.Monitor` is a static class used to synchronize access to a block of code (critical section). It provides mutually exclusive access to a shared resource by acquiring a lock on a specific reference object, ensuring that only one thread can execute the protected code at a time to prevent race conditions.

### Q2: What does the C# lock statement compile to?
The `lock` keyword is syntactic sugar. Behind the scenes, the C# compiler transforms a `lock` statement into calls to `Monitor.Enter` and `Monitor.Exit`, wrapped safely inside a `try...finally` block. This guarantees that the lock is released even if an exception is thrown inside the critical section.

### Q3: List at least two differences between using lock and calling Monitor APIs directly.
1. **Safety and Readability:** The `lock` statement is safer and easier to read because it automatically generates the `try...finally` block. With `Monitor`, developers must manually write the `try...finally` block to ensure `Monitor.Exit` is called; failing to do so can cause deadlocks.
2. **Advanced Capabilities:** `lock` only supports blocking indefinitely until the lock is available. `Monitor` provides advanced methods (like timeouts and thread signaling) that `lock` does not support.

### Q4: When might a developer use Monitor.TryEnter, Monitor.Pulse, or Monitor.Wait instead of a plain lock?
- **`Monitor.TryEnter`**: Used when a developer wants to prevent a thread from waiting indefinitely. It allows specifying a timeout, so if the lock isn't acquired within the given time, the thread can back off and do other work, avoiding potential deadlocks.
- **`Monitor.Wait` & `Monitor.Pulse`**: Used for thread signaling (e.g., in Producer/Consumer patterns). A thread can call `Wait` to release the lock temporarily and sleep until a specific condition is met, and another thread can call `Pulse` to wake it up once that condition becomes true.

### Q5: Write the same critical section twice: once with lock, once with Monitor.Enter / Monitor.Exit.

```csharp
private readonly object _locker = new object();
private int _counter = 0;

// 1. Using the 'lock' statement (Simpler and safer)
public void IncrementWithLock()
{
    lock (_locker)
    {
        _counter++;
    }
}

// 2. Using 'Monitor' explicitly (What 'lock' compiles to)
public void IncrementWithMonitor()
{
    bool lockTaken = false;
    try
    {
        // Acquires the lock and sets lockTaken to true if successful
        Monitor.Enter(_locker, ref lockTaken); 
        
        // Critical Section
        _counter++;
    }
    finally
    {
        // Always ensures the lock is released if it was taken
        if (lockTaken)
        {
            Monitor.Exit(_locker);
        }
    }
}
```
## References
Link1 : https://medium.com/@kamlesh90/lock-vs-monitor-in-c-a16560c49a23
Link2 : https://dev.to/rasulhsn/introduction-to-monitor-class-in-c-422