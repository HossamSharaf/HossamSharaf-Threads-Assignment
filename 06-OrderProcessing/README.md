# Phase 1 — Data Access & Sequential Validation (Q&A)

This document contains the answers to the Phase 1 technical evaluation questions for the **E-Commerce Order Processing System**.

---

### 1. Why are the sync and async times almost the same when orders are awaited one after another?
Because execution remains **strictly sequential**. 

When using `await` inside a standard loop (such as `foreach (var order in orders) await ValidateOrderAsync(order)`), the runtime forces each order to complete all of its asynchronous operations before starting the next order. Even though `await` non-blockingly frees the thread back to the thread pool during delays, the total execution time remains the cumulative sum of all individual order delays because no two orders are being processed concurrently (no `Task.WhenAll` or `Parallel.ForEachAsync` is used yet).

---

### 2. Is the thread ID before an await always the same as after it? Why? Use your own log lines as evidence.
**No, it is not guaranteed to be the same.**

When an `await` statement encounters an uncompleted task (such as `Task.Delay` or asynchronous I/O), the executing thread yields control and returns to the .NET `ThreadPool` to handle other application work. When the awaited operation completes, the runtime scheduler assigns **any available thread** from the thread pool to execute the remaining code continuation (a behavior known as *Thread Hopping*).

**Evidence from Log Lines:**
```text
[15:38:06.945] [Thread 2] Order #1 -> Reserving stock...
[15:38:07.364] [Thread 9] Order #1 -> Capturing payment...
```
### 3. What does the thread do while Task.Delay() is waiting? And while Thread.Sleep() is waiting?
While Task.Delay() is waiting (Non-Blocking): The executing thread is immediately returned to the ThreadPool. It is free to execute other pending tasks, serve HTTP requests, or perform background work. Zero managed threads remain assigned or blocked during the timer duration.

While Thread.Sleep() is waiting (Blocking): The executing thread is frozen in place by the Operating System scheduler (Thread State: WaitSleepJoin). It remains allocated in memory doing zero productive work while actively holding onto system thread resources.

### 4. Why should a repository method return Task<T> instead of T?
Returning Task<T> signals that an operation is asynchronous and non-blocking.

Because repository methods interact with external I/O (disks, databases, or network calls) that take time to respond, returning Task<T> allows the calling method to await the result without stalling the current thread. Additionally, it empowers the caller to invoke multiple repository methods concurrently (e.g., Task.WhenAll(customerTask, productTask)) to optimize execution performance.

### 5. What is the difference between File.ReadAllText and File.ReadAllTextAsync? Does the async version occupy a thread while the disk is reading?
File.ReadAllText (Synchronous): Synchronously blocks the calling thread completely while the operating system reads bytes from physical disk media into memory.

File.ReadAllTextAsync (Asynchronous): Initiates an asynchronous read request with the OS kernel and returns an uncompleted Task, immediately releasing the calling thread back to the thread pool.

Does the async version occupy a thread while the disk is reading?No, zero managed threads are occupied. The underlying OS utilizes hardware-level I/O Completion Ports (IOCP) on Windows (or epoll / io_uring on Linux). The hardware controller executes the physical disk read, issuing a hardware interrupt upon completion to push a callback back onto the .NET ThreadPool to resume code execution.

### Why does `Task.Run()` fit for the CPU-bound invoice generation step, but NOT for database/I-O calls?

1. **For CPU-Bound Operations (e.g., PDF Rendering / Heavy Computations):**
   - Operations like rendering a complex PDF layout, cryptographic hashing, or running a 20,000,000-iteration loop consume continuous CPU cycles. 
   - If executed synchronously on an asynchronous request thread (such as an ASP.NET request thread or a parallel worker loop), it will **block the execution thread** from doing other work, spiking CPU usage and degrading throughput.
   - Using `Task.Run()` offloads this heavy computational workload to a background thread pool worker, keeping the main calling thread free to handle other asynchronous continuations or incoming orders.

2. **Why NOT for Database or File I/O Calls?**
   - Database queries, network requests, and disk reads/writes are **I/O-bound** operations, not CPU-bound.
   - .NET's underlying drivers and file system APIs already provide native asynchronous methods (`File.WriteAllTextAsync`, ADO.NET async execution, etc.) that interact directly with the OS kernel and hardware I/O Completion Ports (IOCP) **without consuming a CPU thread** while waiting for data.
   - Wrapping an existing I/O-bound call in `Task.Run()` (often referred to as an "Async-over-Sync" anti-pattern or unnecessary thread-pool dispatch) forces a worker thread to sit idle just waiting for the I/O to finish, which actually **wastes thread resources** and reduces overall system scalability.

   # Phase 2: Concurrency, Thread Safety, and High-Performance Order Processing

This document outlines the architectural concepts, concurrency mechanisms, and answers to core asynchronous programming questions implemented during Phase 2 of the E-Commerce Order Processing System.

---

## Phase 2 Conceptual Questions & Answers

### 1. What is the difference between starting a task and awaiting it?
* **Starting a Task:** When a task is initiated (e.g., via `Task.Run()`, or when an async method starts executing and returns an un-awaited `Task`), it schedules the operation to run asynchronously on the Thread Pool (or starts I/O operations). The calling thread **does not block**; it continues executing subsequent lines of code immediately.
* **Awaiting a Task:** Using the `await` keyword pauses the execution of the current async method until the awaited task completes. Crucially, instead of blocking the underlying physical thread, `await` yields control back to the caller/synchronization context, freeing the thread to do other work. Once the task finishes, the remainder of the async method resumes.

### 2. Does every Task create a new thread? Does Task.Delay() use a thread while waiting?
* **No, tasks do not equal threads.** A `Task` represents an *asynchronous operation* or a *future result*, not a dedicated thread. Tasks are lightweight objects managed by the .NET Task Scheduler and executed on the Thread Pool. Multiple tasks can run sequentially or concurrently on the same thread.
* **No, `Task.Delay()` does not consume a thread while waiting.** It registers a timer with the .NET thread-pool timer queue and immediately frees up the calling thread. No thread is blocked or held in an idle state during the delay period; once the time elapses, a thread-pool thread resumes execution.

### 3. What happens if you call `.Result` or `.Wait()` on an unfinished task instead of `await`? Why is it dangerous?
* Calling `.Result` or `.Wait()` synchronously blocks the calling thread until the task completes. 
* **The Danger (Deadlocks):** If called on a thread that has a SynchronizationContext (such as GUI threads in WPF/WinForms or older ASP.NET contexts) that restricts execution to a single thread, blocking that thread while the task tries to complete back on that same context causes a **deadlock**. The task cannot finish because the thread it needs is blocked waiting for it.
* **Allowed Demo Method (Deadlock / Sync-Over-Async Demo):**
```csharp
public static void UnsafeBlockDemo()
{
    // DANGEROUS: Using .Result or .Wait() synchronously blocks the thread.
    // This is only acceptable in top-level console entry points where no synchronization context exists, 
    // or strictly for standalone sync wrappers.
    Task<int> heavyTask = Task.Run(async () => {
        await Task.Delay(100);
        return 42;
    });

    // Blocking the thread via .Result
    int result = heavyTask.Result; 
    Console.WriteLine($"Result obtained via blocking: {result}");
}
```
### 4. What happens to an exception thrown inside a task that is never awaited?
In modern .NET (Core / 5+), if a task throws an unhandled exception and that task is garbage collected without its exception ever being observed (via await, .Result, or accessing .Exception), it triggers the UnobservedTaskException event. Historically this would crash the application, but currently, it is generally swallowed by the GC (though depending on runtime configurations, unobserved exceptions can still lead to process termination in strict environments). It is a dangerous practice as failures fail silently.

### 5. When should you use Task.Run(), and when should you avoid it?
When to use: For CPU-bound operations (e.g., heavy mathematical computations, image/PDF rendering, compression loops) that would otherwise block the thread. Task.Run() offloads these workloads to the Thread Pool.

When to avoid: For I/O-bound operations (database queries, network requests, file reading/writing). Wrappers like File.WriteAllTextAsync or database async methods are natively asynchronous and interact with hardware/OS completion ports without needing a thread. Wrapping I/O calls in Task.Run() ("Async-over-Sync" anti-pattern) wastes a thread pool thread just to wait idly.

### 6. What is the difference between Task and Task<T>?
Task represents an asynchronous operation that does not return a value (analogous to a void method).

Task<T> represents an asynchronous operation that ultimately produces a result of type T (analogous to a method returning T).

### 7. Why can a lock block not contain an await?
The C# lock statement is syntactic sugar for Monitor.Enter() and Monitor.Exit(), which rely on thread-affinity: the exact same thread that acquires the lock must release it.

Because an await expression can resume execution on an entirely different thread from the thread pool, using await inside a lock block would violate this constraint, causing compilation errors or severe runtime synchronization bugs (e.g., releasing a lock from a thread that doesn't own it). Instead, asynchronous coordination requires constructs like SemaphoreSlim.WaitAsync().

### 8. Why did the unprotected stock reservation oversell, and how did you prevent it?
Why it oversold: Without protection, two concurrent orders (e.g., #10 and #11) read the product stock file at the exact same moment, both saw a stock quantity of 1, both passed the validation check, both decremented the value to 0 (or -1), and both saved the file. This race condition led to selling the same item twice (Overselling).

How it was prevented: We implemented an asynchronous synchronization primitive, SemaphoreSlim(1, 1), acting as a business lock around the critical section inside ProductRepository. When an order enters ReserveStockAsync, it acquires the lock asynchronously (await _businessLock.WaitAsync()). Subsequent concurrent requests must wait until the active thread finishes reading, validating, updating, and releasing the lock via _businessLock.Release(). Thus, the second request reads the updated stock (0), safely rejecting the reservation.
