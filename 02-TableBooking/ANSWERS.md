# Assignment 02 — Answers

Student name:  Jessica Wael Khalaf 
Group / ID:  3

## What is wrong
we have 5 tables and 40 customers ask for one at the same time, each customer first asks (is there a table?) and gets yes.
Then the table gets taken. so the problem is :: many customers can ask before the first one takes the table. So all of them get "yes",
and more than 5 tables can be booked.


-

## How you fixed it
by adding a lock, so only one customer can check and book at a time and other customers wait until the lock is free
so the result:: the first 5 customers get a table and the others gets (sorry, no tables left) and the final output is always Booked=5 and TablesLeft=0.
-

