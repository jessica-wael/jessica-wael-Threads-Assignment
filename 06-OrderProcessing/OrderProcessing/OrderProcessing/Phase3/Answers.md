## Phase 3 Answers

### Task 9.6 - Sync vs Async

1- Does async make one order faster?
no, One order still does the steps one by one: customer, reserve, price, payment, save, email. Async only frees the thread while it waits.

2- Why does async scale better?
Async doesn't hold a thread while waiting. The sync version needed 10 threads for 10 orders, and they were all sleeping. 

3- What happens to sync with 1,000 orders?
It needs 1,000 threads. Each thread uses memory and the computer wastes time switching between them. The app can become very slow or crash.
Async would still use only a few threads.

4- Why is async important for databases, files and APIs? 
They are slow, and the program just waits for them and the thread can do other work while it waits, so one server can handle many requests with few threads.

### Task 9.5 - Email retry

If sending the email fails, the code tries again up to 3 times, waiting 500 ms, then 1000 ms, then 2000 ms. If the 4th try also fails, the email is saved in email_retry.json with Attempts and LastError.
The method then returns normally, so a paid order never looks failed because of an email problem.

### Task 9.7 - Fire-and-forget

1- The problem: SendConfirmationAsync is called without await. The method does not wait for the email, so it finishes while the email is still running.
2- In my BEFORE output, "Order processing finished" is printed right after "Render template", before "Connect to SMTP" and "Send". So the method said it was finished, but the email was not. If the console app exits at that moment, the email is cut and lost.
3- If the email throws an exception, nobody awaits the task, so nobody sees the error. The order looks successful, but the email never arrived, and no log shows why.
4- The fix is to add await before SendConfirmationAsync. In my AFTER output, "Order processing finished" is printed last, after "Send". Now the method waits for the email, and we can catch an exception.

When is await necessary? 
When we need the result, or need to know that the work finished or failed.

When is fire-and-forget acceptable? 
Almost never. Only for work where losing it is fine, like a simple debug message, and even then we should catch its exceptions.

Why is async void dangerous? 
Nobody can await it, and an exception inside it can crash the whole app.

Why is async void acceptable for event handlers? 
Because an event handler must return void. It is the only case where we have no choice.


