# Assignment 01 — Answers

Student name:  Jessica Wael Khalaf

Group / ID: 3 

## Problems with the current design

1- Blocking:: 
in OrderDatabase, GetPendingOrders and GetOrderById uses Thread.Sleep() and it 
makes the thread wait without doing anything, which wastes time and resources.

2- Sequential processibg :: 
In ExportAllPendingAsync, each order is proccesed after the previous one finishes. 


-

## What you changed
1- Change Thread.Sleep() with await Task.Delay, and make methods async Task
2- Change ExportAllPendingAsync to start all orders at the same time using Select with a async lambda then called ToList(),
so all tasks start immediately and awaited them afterward.
-


The Result Before::
Export finished in 6.0 s

and After::
Export finished in 1.9 s

