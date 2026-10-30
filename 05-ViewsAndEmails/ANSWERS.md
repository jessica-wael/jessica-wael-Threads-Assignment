# Assignment 05 — Answers

Student name:  Jessica Wael Khalaf 
Group / ID:  3

#Queue<T> is for one thread at a time. Request threads + worker threads = lost, duplicated or crashing jobs.

#ConcurrentQueue: many producers, many workers, no lock needed. TryDequeue = check + take in one step.

## Failures you observed
the Dictionary crashed because several threads were changing it at the same time and 
the Queue also crashed because two workers could take the last job at the same time


## What you changed
I changed the Dictionary to a ConcurrentDictionary and used AddOrUpdate to add or update each count in one step
and changed the Queue to a ConcurrentQueue, and used TryDequeue so each job is taken by only one worker.
I removed the try/catch, because TryDequeue doesn’t throw an exception when the queue is empty
-

## Collection safety vs business-rule safety
Collection safety means the collection can be used by multiple threads without causing problems
Business-rule safety means the program gives the correct result, like counting each view once or sending each email once





