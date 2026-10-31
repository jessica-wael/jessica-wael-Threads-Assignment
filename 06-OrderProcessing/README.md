06 - Order Processing System (Mini Project)

Student name: Jessica Wael 
Group / ID: 3

Async order processing system on .NET 10 (net10.0). Every collection is a JSON file on disk, and every database call or network request is simulated with Task.Delay().

1. How to run
dotnet run                                  # numbered menu
dotnet run -- phase1 | phase2 | phase3 | phase4 | all
dotnet run -- phase3 --cancel-after 4000    # cancel the run after 4000 ms

Ctrl+C also cancels the run. The Data folder is created in bin/Debug/net10.0/Data by DataSeeder.ResetAsync(), which every phase calls first (it deletes the folder and recreates all files from the seed data). The final JSON files of each phase are copied to sample-output/.

Project structure

Folder	Role
Shared/Models	the models and enums
Shared/Storage	JsonFileStore (the only class that reads/writes JSON), DataFiles (file names), DataSeeder
Shared/Logging	Log (Info / Success / Error with time and thread id)
Shared/Exceptions	the six custom exceptions
Phase1	async repositories and OrderValidator (+ sync versions for comparison)
Phase2	pricing, race condition demo, concurrent processing, invoices, report
Phase3	full checkout: services, PaymentGateway, EmailService, OrderProcessor
Phase4	live checkout, customer dashboard, shipping quotes, fraud scoring, email re-send



2. Storage design
8 files: customers, products, coupons, incoming_orders, orders, payments, emails, email_retry (.json), plus the invoices/ and reports/ folders.
Each file is one JSON array. An empty collection is []. System.Text.Json only, with one shared JsonSerializerOptions (indented, camelCase, enums as text), UTF-8.
A missing file reads as an empty list. Invalid JSON throws DataFileCorruptedException with the file name.
Concurrent access: a SemaphoreSlim protects each file (not lock, because lock cannot contain await). Reading, changing and writing happen under the same lock, so no update is lost.
No half-written file: a write goes to a .tmp file first, then File.Move(overwrite: true) replaces the real file.



3. Performance (measured with Stopwatch, 12 logical processors)
Measurement	Time	Distinct threads
Phase 1 sync, 20 orders	34.9 s	1
Phase 4 - 10.1 live checkout, 20 orders	32.6 s	-
Phase 4 - 10.2 dashboard, customer #3 / #99	1439 ms / 1222 ms	-
Phase 4 - 10.3 shipping quote, orders #1-#5	1063, 1049, 783, 950, 580 ms	-
Phase 4 - 10.4 A fraud scoring, sequential	5566 ms	1
Phase 4 - 10.4 A fraud scoring, parallel	550 ms (10.12x)	12
Phase 4 - 10.4 B email re-send, 30 emails	8.4 s (max 4 at once)	4 (ThreadPool.ThreadCount 14)



4. Problems I found and how I solved them
Overselling the last keyboard: both orders read stock = 1 and both got it. I made check and subtract one protected step with SemaphoreSlim.
lock with await: not allowed, so I used SemaphoreSlim with WaitAsync / Release in a finally.
Half-written JSON: fixed by writing to a .tmp file and then replacing the real one.
Hidden exception in Phase 4: a null! processor made every order fail with a NullReferenceException that the catch hid as Failed. I build the services properly now.
Paid order must not look failed: a failed email never changes the status; after 4 attempts it goes to email_retry.json.
Payment captured but save failed (#13): I refund the payment, release the stock and set Failed.
Cancelled runs: reserved stock is released and captured payments are refunded, so nothing stays reserved or charged.

5. Answers to the questions of Phases 1-4
The answers are in the ANSWERS.md file of each phase