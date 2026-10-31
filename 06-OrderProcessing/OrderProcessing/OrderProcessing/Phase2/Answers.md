## Phase 2 Answers


Questions ::
1 What is the difference between starting a task and awaiting it?
2 Does every Task create a new thread? Does Task.Delay() use a thread while waiting?
3 What happens if you call .Result or .Wait() on an unfinished task instead of await? Why is it
dangerous? Write a small separate demo method — the only place .Result / .Wait() is allowed.
4 What happens to an exception thrown inside a task that is never awaited?
5 When should you use Task.Run(), and when should you avoid it?
6 What is the difference between Task and Task<T>?
7 Why can a lock block not contain an await?
8 Why did the unprotected stock reservation oversell, and how did you prevent it?


Answers::

1- Starting a task without await makes the code continue immediately while the task runs in the background 
but await pauses the current method until the task completes and only then does the code move to the next line

2- No, not every task create a new thread and Task.Delay() doesn't bolck the thread while it waits , the thread goes back to the threadPool and can run another task until the delay end.

3-  .Result or .Wait() make the current thread is blocked and waits until the task finishes. 
It is dangerous because the thread does nothing while it waits. In apps with a UI thread or a synchronization context, 
it can cause a deadlock: the task needs that thread to finish, and the thread waits for the task, so both wait forever. await does not have this problem because the thread is free while it waits.


4- the exception is not seen, It stays inside the task and the program continues as if nothing happened. When we await the task, the exception is thrown at the await line and we can catch it
It was caught because I awaited its task inside try/catch, so I could mark it Failed and release the stock.

5- we use Task.Run() for CPU-bound work which means the CPU is busy calculatin. Don't use it when we only wait, like database calls or Task.Delay. 
in that case async already frees the thread, so Task.Run only wastes one more thread.


6- Task is an operation with no result. 
Task<T> is an operation that gives a result of type T when it finishes. like Task<bool> => gives true or false.

7- A lock must be released by the same thread that took it. 
After an await, the code can continue on another thread and C# does not allow await inside a lock

8- Order #10 and order #11 both read the stock as 1 before any of them wrote. 
Both saw one keyboard, so both reserved it, and the stock became 0. 
I fixed it with SemaphoreSlim and now the second order waits for the first one
