## Phase 1 Answers


Questions::
1-Why are the sync and async times almost the same when orders are awaited one after another?
2 Is the thread ID before an await always the same as after it? Why? Use your own log lines as evidence.
3 What does the thread do while Task.Delay() is waiting? And while Thread.Sleep() is waiting?
4 Why should a repository method return Task<T> instead of T?
5 What is the difference between File.ReadAllText and File.ReadAllTextAsync? Does the async
version occupy a thread while the disk is reading?

 
Answers::
1- Because we wait each order to finish before start the next order  in the two versions so the total time almost the same for the checking work and the extra time in async come from saving each order to orders.json.
the async doesn't make one wait shorter. it only helps if we do other work while we wait but the sync blocks the thread while waiting and doesn't save orders .

2- no, the thread id is not always the seme after an await, when the awaited operation finishes the method continues on any free thread from the threadpool.
in mu Log file, Saving order #2 ran on the thread 10 and the next line "Confirming transaction...",ran on thread 13. in the sync run, everything stayed on thraed 10

3- with "Task.Delay()" the thread can go back to the threadpool and can do another work unitl the delay time is finished. 
and with "Thread.Sleep()" blocks the thread and no other work can usd it until the time is over.

4- The result is not ready when the method returns so the method returns the Task of T, The Task means the result will come later a
and we use await to wait it and the thread is not blocked 

5- "File.ReadAllText" it blocks the thread until the file is read, 
but "File.ReadAllTextAsync" starts reading and return the Task at once
