# Assignment 04 — Answers

Student name:  Jessica Wael Khalaf
Group / ID:  3

## Why Expected and Actual disagree

because "Views++" is not one step ,it's 3 : read , add 1 then write so 8 threads runs at the smae time
two threads may read the same value before either one updates it, so one update can be lost.
-

## What you changed

I used "InerLocked.Increment(ref Views)" instead of "Views++" and it's an atomic operation that ensures only one thread can read and write "Views" at a time,
it's like "Lock" but for one atomic operation on one variable
-

## When would you protect the update differently

Using "Lock" when we need to protect multiple operation that must work together.(Condition + Operation)

Using "InterLocked" when we need to protect simple operation like incrementing or decrementing a varialble

Using Semaphore when we want to limit how many threads can access a resource at the same time
-
