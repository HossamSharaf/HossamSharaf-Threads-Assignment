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
