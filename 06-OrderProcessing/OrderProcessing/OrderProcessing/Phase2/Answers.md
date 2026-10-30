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

3- 